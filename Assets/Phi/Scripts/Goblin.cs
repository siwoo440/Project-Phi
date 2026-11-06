using UnityEngine; // 전투 위치와 물리 계산

namespace Phi // 근접 적 행동 영역
{
    internal sealed class Goblin // 빠른 근접 적의 전투 상태
    {
        public Fighter Fighter // 고블린 전투 대상
        {
            get; // 고블린 체력 상태 읽기
        }
        private readonly Transform weapon; // 예고에 사용할 무기 표시
        private readonly System.Func<Vector2, Vector2, float, bool, Vector2> move; // 공통 장애물 이동
        private float nextAttack; // 다음 공격 가능 시각
        private float windupUntil; // 공격 예고 종료 시각
        private Vector2 attackDirection; // 예고 시작 시 고정한 베기 방향

        public Goblin(Fighter fighter, Transform weapon, System.Func<Vector2, Vector2, float, bool, Vector2> move) // 고블린 행동 상태 생성
        {
            Fighter = fighter; // 고블린 전투 상태 보관
            this.weapon = weapon; // 무기 표시 보관
            this.move = move; // 공통 장애물 이동 보관
            Fighter.Died += HideWeapon; // 사망 시 무기 표시 제거
        }

        public void Tick(Fighter player, PrototypeSettings settings, MapRegion mapRegion, ContactFilter2D filter, RaycastHit2D[] hits, bool canSeePlayer) // 빠른 접근과 근접 베기 처리
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지의 방향
            if (windupUntil > 0f) // 공격 예고 진행 여부
            {
                if (Time.time >= windupUntil) // 예고 완료 시각 확인
                {
                    windupUntil = 0f; // 공격 예고 종료
                    weapon.localRotation = Quaternion.identity; // 기본 무기 자세 복원
                    Physics2D.SyncTransforms(); // 플레이어와 장애물 위치 반영
                    if (CombatMath.InSwordSector(Fighter.transform.position, attackDirection, player.transform.position, Mathf.Max(0f, settings.goblinAttackRange), Mathf.Max(0f, settings.goblinAttackAngle)) && Physics2D.Linecast(Fighter.transform.position, player.transform.position, filter, hits) == 0) // 베기 범위와 장애물 확인
                    {
                        player.TakeDamage(CombatMath.Damage(settings.goblinAttack, 1f, player.Defense, false)); // 공통 피해식과 회피 무적 적용
                    }
                }
                return; // 예고 중 접근과 중복 공격 차단
            }
            float range = Mathf.Max(0f, settings.goblinAttackRange); // 음수 없는 근접 공격 거리
            if (canSeePlayer && offset.magnitude <= range && Time.time >= nextAttack) // 시야와 공격 거리 및 간격 확인
            {
                attackDirection = offset.sqrMagnitude > 0.000001f ? offset.normalized : Vector2.right; // 예고 당시의 고정 베기 방향
                windupUntil = Time.time + Mathf.Max(0.01f, settings.goblinWindup); // 베기 예고 종료 시각
                nextAttack = Time.time + Mathf.Max(0.01f, settings.goblinAttackInterval, settings.goblinWindup); // 다음 공격 가능 시각
                weapon.localRotation = Quaternion.Euler(0f, 0f, -60f); // 무기를 들어 올리는 예고
                return; // 예고 시작 프레임의 이동 중단
            }
            if (!canSeePlayer || offset.magnitude > range * 0.8f) // 시야가 막히거나 근접 거리 밖인지 확인
            {
                float step = Mathf.Min(Mathf.Max(0f, settings.goblinSpeed) * Time.deltaTime, offset.magnitude); // 플레이어 방향 접근 거리
                Fighter.transform.position = move(Fighter.transform.position, offset.normalized * step, PrototypeSettings.SlimeRadius, true); // 씬 경계 안의 빠른 접근
            }
        }

        private void HideWeapon(Fighter fighter) // 사망 전환의 무기 표시 정리
        {
            if (weapon != null) // 남아 있는 무기 확인
            {
                weapon.gameObject.SetActive(false); // 사망 시 무기 표시 숨김
            }
        }
    }
}
