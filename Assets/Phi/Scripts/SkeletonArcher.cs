using System; // 적 화살 발사 동작
using UnityEngine; // 전투 위치와 시간 계산

namespace Phi // 원거리 적 행동 영역
{
    internal sealed class SkeletonArcher // 활을 사용하는 원거리 적
    {
        public Fighter Fighter // 궁수 전투 대상
        {
            get; // 궁수 체력 상태 읽기
        }
        private readonly Transform bow; // 공격 예고용 활 표시
        private readonly System.Func<Vector2, Vector2, float, bool, Vector2> move; // 공통 장애물 이동
        private float nextAttack; // 다음 공격 가능 시각
        private float windupUntil; // 활시위 예고 종료 시각
        private Vector2 shotDirection; // 예고 시 고정한 화살 방향

        public SkeletonArcher(Fighter fighter, Transform bow, System.Func<Vector2, Vector2, float, bool, Vector2> move) // 궁수 행동 상태 생성
        {
            Fighter = fighter; // 궁수 전투 상태 보관
            this.bow = bow; // 활 표시 보관
            this.move = move; // 공통 장애물 이동 보관
            Fighter.Died += HideBow; // 사망 시 활 표시 정리
        }

        public void Tick(Fighter player, PrototypeSettings settings, MapRegion mapRegion, Action<Vector2, Vector2, int, float, float> shoot) // 거리 유지와 화살 발사
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지의 방향
            if (windupUntil > 0f) // 활시위 예고 진행 여부
            {
                if (Time.time >= windupUntil) // 예고 완료 시각 확인
                {
                    windupUntil = 0f; // 공격 예고 종료
                    bow.localRotation = Quaternion.identity; // 기본 활 자세 복원
                    shoot(Fighter.transform.position, shotDirection, settings.skeletonAttack, settings.skeletonProjectileSpeed, settings.skeletonProjectileRange); // 고정 방향의 적 화살 발사
                }
                return; // 예고 중 이동과 중복 발사 방지
            }
            float distance = offset.magnitude; // 플레이어까지의 거리
            float keep = Mathf.Max(0f, settings.skeletonKeepDistance); // 음수 없는 유지 거리
            float range = Mathf.Max(0f, settings.skeletonAttackRange); // 음수 없는 공격 거리
            if (distance < keep && distance > 0.0001f) // 플레이어가 너무 가까운 경우
            {
                float step = Mathf.Min(Mathf.Max(0f, settings.skeletonSpeed) * Time.deltaTime, keep - distance); // 유지 거리를 넘지 않는 후퇴 거리
                Fighter.transform.position = move(Fighter.transform.position, -offset.normalized * step, PrototypeSettings.SlimeRadius, true); // 씬 경계 안의 거리 유지
                return; // 후퇴 중 공격 생략
            }
            if (distance <= range && Time.time >= nextAttack) // 공격 거리와 간격 확인
            {
                shotDirection = offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector2.right; // 예고 당시의 고정 발사 방향
                windupUntil = Time.time + Mathf.Max(0.01f, settings.skeletonWindup); // 활시위 예고 종료 시각
                nextAttack = Time.time + Mathf.Max(0.01f, Mathf.Max(settings.skeletonAttackInterval, settings.skeletonWindup)); // 다음 공격 가능 시각
                bow.localRotation = Quaternion.Euler(0f, 0f, -60f); // 활을 당기는 임시 예고
                return; // 예고 시작 프레임의 이동 중단
            }
            if (distance > range && distance > 0.0001f) // 공격 거리 밖 확인
            {
                float step = Mathf.Min(Mathf.Max(0f, settings.skeletonSpeed) * Time.deltaTime, distance - range); // 공격 거리까지의 접근량
                Fighter.transform.position = move(Fighter.transform.position, offset.normalized * step, PrototypeSettings.SlimeRadius, true); // 씬 경계 안의 접근
            }
        }

        private void HideBow(Fighter fighter) // 사망 시 활 표시 정리
        {
            if (bow != null) // 활 표시 존재 여부
            {
                bow.gameObject.SetActive(false); // 사망 후 활 표시 숨김
            }
        }
    }
}
