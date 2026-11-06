using UnityEngine; // 전투 위치와 물리 계산

namespace Phi // 중장갑 적 행동 영역
{
    internal sealed class OrcWarrior // 도끼로 강타하는 중장갑 적
    {
        public Fighter Fighter // 오크 전투 대상
        {
            get; // 오크 체력 상태 읽기
        }
        private readonly Transform axe; // 공격 예고용 도끼 표시
        private readonly System.Func<Vector2, Vector2, float, bool, Vector2> move; // 공통 장애물 이동
        private float nextAttack; // 다음 공격 가능 시각
        private float windupUntil; // 강타 예고 종료 시각
        private Vector2 attackDirection; // 예고 시 고정한 강타 방향

        public OrcWarrior(Fighter fighter, Transform axe, System.Func<Vector2, Vector2, float, bool, Vector2> move) // 오크 행동 상태 생성
        {
            Fighter = fighter; // 오크 전투 상태 보관
            this.axe = axe; // 도끼 표시 보관
            this.move = move; // 공통 장애물 이동 보관
            Fighter.Died += HideAxe; // 사망 시 도끼 표시 정리
        }

        public void Tick(Fighter player, PrototypeSettings settings, MapRegion mapRegion, ContactFilter2D filter, RaycastHit2D[] hits) // 느린 접근과 부채꼴 강타
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지의 방향
            if (windupUntil > 0f) // 강타 예고 진행 여부
            {
                if (Time.time >= windupUntil) // 예고 완료 시각 확인
                {
                    windupUntil = 0f; // 강타 예고 종료
                    axe.localRotation = Quaternion.identity; // 기본 도끼 자세 복원
                    Physics2D.SyncTransforms(); // 플레이어와 장애물 위치 반영
                    if (CombatMath.InSwordSector(Fighter.transform.position, attackDirection, player.transform.position, Mathf.Max(0f, settings.orcAttackRange), Mathf.Max(0f, settings.orcAttackAngle)) && Physics2D.Linecast(Fighter.transform.position, player.transform.position, filter, hits) == 0) // 강타 범위와 벽 가림 확인
                    {
                        player.TakeDamage(CombatMath.Damage(settings.orcAttack, 1f, player.Defense, false)); // 공통 피해식과 회피 무적 적용
                    }
                }
                return; // 예고 중 접근과 중복 공격 차단
            }
            float range = Mathf.Max(0f, settings.orcAttackRange); // 음수 없는 강타 거리
            if (offset.magnitude <= range && Time.time >= nextAttack) // 공격 거리와 간격 확인
            {
                attackDirection = offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector2.right; // 예고 당시의 고정 강타 방향
                windupUntil = Time.time + Mathf.Max(0.01f, settings.orcWindup); // 도끼 예고 종료 시각
                nextAttack = Time.time + Mathf.Max(0.01f, Mathf.Max(settings.orcAttackInterval, settings.orcWindup)); // 다음 강타 가능 시각
                axe.localRotation = Quaternion.Euler(0f, 0f, -70f); // 도끼를 높이 드는 임시 예고
                return; // 예고 시작 프레임의 이동 중단
            }
            if (offset.magnitude > range * 0.8f) // 근접 거리 밖 확인
            {
                float step = Mathf.Min(Mathf.Max(0f, settings.orcSpeed) * Time.deltaTime, offset.magnitude); // 플레이어 방향 접근 거리
                Fighter.transform.position = move(Fighter.transform.position, offset.normalized * step, PrototypeSettings.SlimeRadius, true); // 씬 경계 안의 느린 접근
            }
        }

        private void HideAxe(Fighter fighter) // 사망 시 도끼 표시 정리
        {
            if (axe != null) // 도끼 표시 존재 여부
            {
                axe.gameObject.SetActive(false); // 사망 후 도끼 표시 숨김
            }
        }
    }
}
