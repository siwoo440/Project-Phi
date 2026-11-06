using UnityEngine; // 전투 위치와 시간 계산

namespace Phi // 도약형 적 행동 영역
{
    internal sealed class Wolf // 경로 예고 후 도약하는 적
    {
        public Fighter Fighter // 늑대 전투 대상
        {
            get; // 늑대 체력 상태 읽기
        }
        private readonly Transform path; // 도약 경로 예고 표시
        private readonly System.Func<Vector2, Vector2, float, bool, Vector2> move; // 공통 장애물 이동
        private float nextAttack; // 다음 공격 가능 시각
        private float windupUntil; // 도약 예고 종료 시각
        private float leapRemaining; // 남은 도약 거리
        private Vector2 leapDirection; // 예고 시 고정한 도약 방향
        private bool hitDuringLeap; // 한 도약의 중복 피해 방지

        public Wolf(Fighter fighter, Transform path, System.Func<Vector2, Vector2, float, bool, Vector2> move) // 늑대 행동 상태 생성
        {
            Fighter = fighter; // 늑대 전투 상태 보관
            this.path = path; // 경로 표시 보관
            this.move = move; // 공통 장애물 이동 보관
            path.gameObject.SetActive(false); // 평상시 경로 표시 숨김
            Fighter.Died += HidePath; // 사망 시 경로 표시 정리
        }

        public void Tick(Fighter player, PrototypeSettings settings, MapRegion mapRegion) // 추격과 고정 방향 도약
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지의 방향
            if (leapRemaining > 0f) // 도약 진행 여부
            {
                Vector2 start = Fighter.transform.position; // 도약 이동 전 위치
                float requested = Mathf.Min(leapRemaining, Mathf.Max(0.01f, settings.wolfLeapSpeed) * Time.deltaTime); // 이번 프레임의 도약 거리
                float step = Mathf.Min(requested, BoundaryDistance(start, leapDirection, mapRegion.Interior)); // 씬 경계 안의 직선 이동 거리
                Vector2 end = move(start, leapDirection * step, PrototypeSettings.SlimeRadius, false); // 벽과 경계 안의 도약 도착점
                Fighter.transform.position = end; // 경계 안의 늑대 위치
                leapRemaining = step < requested - 0.000001f || (end - start).sqrMagnitude < step * step - 0.000001f ? 0f : Mathf.Max(0f, leapRemaining - step); // 벽 충돌 또는 남은 도약 거리
                Vector2 movement = end - start; // 실제 도약 이동 벡터
                float fraction = movement.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)player.transform.position - start, movement) / movement.sqrMagnitude) : 0f; // 이동 경로상 최근접 위치
                Vector2 closest = start + movement * fraction; // 플레이어와 가장 가까운 도약 경로점
                if (!hitDuringLeap && Vector2.Distance(closest, player.transform.position) <= Mathf.Max(0f, settings.wolfHitDistance)) // 도약 경로의 플레이어 적중
                {
                    player.TakeDamage(CombatMath.Damage(settings.wolfAttack, 1f, player.Defense, false)); // 공통 피해식과 회피 무적 적용
                    hitDuringLeap = true; // 같은 도약의 중복 피해 방지
                }
                return; // 도약 중 다른 행동 중지
            }
            if (windupUntil > 0f) // 도약 예고 진행 여부
            {
                if (Time.time >= windupUntil) // 예고 완료 시각 확인
                {
                    windupUntil = 0f; // 도약 예고 종료
                    Fighter.transform.localScale = new Vector3(0.7f, 0.7f, 1f); // 도약 시작 시 기본 자세 복원
                    path.gameObject.SetActive(false); // 도약 시작 시 경로 표시 숨김
                    leapRemaining = Mathf.Max(0f, settings.wolfLeapDistance); // 고정 방향의 도약 거리
                    hitDuringLeap = false; // 새 도약의 적중 상태 초기화
                }
                return; // 예고 중 이동과 중복 공격 차단
            }
            float range = Mathf.Max(0f, settings.wolfAttackRange); // 음수 없는 도약 시작 거리
            if (offset.magnitude <= range && Time.time >= nextAttack) // 도약 거리와 간격 확인
            {
                leapDirection = offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector2.right; // 예고 당시의 고정 도약 방향
                windupUntil = Time.time + Mathf.Max(0.01f, settings.wolfWindup); // 도약 예고 종료 시각
                nextAttack = Time.time + Mathf.Max(0.01f, Mathf.Max(settings.wolfAttackInterval, settings.wolfWindup)); // 다음 도약 가능 시각
                float planned = Mathf.Min(Mathf.Max(0f, settings.wolfLeapDistance), BoundaryDistance(Fighter.transform.position, leapDirection, mapRegion.Interior)); // 씬 경계 안의 도약 거리
                Vector2 previewEnd = move(Fighter.transform.position, leapDirection * planned, PrototypeSettings.SlimeRadius, false); // 벽 앞의 예고 종료 위치
                float preview = Vector2.Distance(Fighter.transform.position, previewEnd); // 실제 도약 가능한 예고 길이
                Fighter.transform.localScale = new Vector3(0.7f, 0.5f, 1f); // 도약 전 몸을 낮추는 예고
                Vector2 localPath = new Vector2(leapDirection.x * preview / 0.7f, leapDirection.y * preview / 0.5f); // 낮아진 본체 기준의 경로 벡터
                path.localPosition = (Vector3)(localPath * 0.5f); // 늑대 중심에서 경로 중앙 위치
                path.localScale = new Vector3(localPath.magnitude, 0.08f / 0.7f, 1f); // 실제 도약 거리에 맞춘 예고 길이
                path.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(localPath.y, localPath.x) * Mathf.Rad2Deg); // 고정 도약 방향 표시
                path.gameObject.SetActive(true); // 도약 전 경로 표시
                return; // 예고 시작 프레임의 이동 중단
            }
            if (offset.magnitude > range * 0.8f) // 도약 거리 밖 확인
            {
                float step = Mathf.Min(Mathf.Max(0f, settings.wolfSpeed) * Time.deltaTime, offset.magnitude); // 플레이어 방향 추격 거리
                Fighter.transform.position = move(Fighter.transform.position, offset.normalized * step, PrototypeSettings.SlimeRadius, true); // 씬 경계 안의 추격
            }
        }

        private static float BoundaryDistance(Vector2 start, Vector2 direction, Rect area) // 고정 방향의 씬 경계까지 거리
        {
            float distance = float.PositiveInfinity; // 초기 경계 거리
            float margin = PrototypeSettings.SlimeRadius; // 적 도형의 안전 반폭
            if (direction.x > 0.000001f) // 오른쪽 도약 여부
            {
                distance = Mathf.Min(distance, (area.xMax - margin - start.x) / direction.x); // 오른쪽 경계 거리
            }
            if (direction.x < -0.000001f) // 왼쪽 도약 여부
            {
                distance = Mathf.Min(distance, (area.xMin + margin - start.x) / direction.x); // 왼쪽 경계 거리
            }
            if (direction.y > 0.000001f) // 위쪽 도약 여부
            {
                distance = Mathf.Min(distance, (area.yMax - margin - start.y) / direction.y); // 위쪽 경계 거리
            }
            if (direction.y < -0.000001f) // 아래쪽 도약 여부
            {
                distance = Mathf.Min(distance, (area.yMin + margin - start.y) / direction.y); // 아래쪽 경계 거리
            }
            return Mathf.Max(0f, distance); // 음수 경계 거리 방지
        }

        private void HidePath(Fighter fighter) // 사망 시 경로 표시 정리
        {
            if (path != null) // 경로 표시 존재 여부
            {
                path.gameObject.SetActive(false); // 사망 후 경로 표시 숨김
            }
        }
    }
}
