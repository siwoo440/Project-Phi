using UnityEngine; // 유니티 자료형

namespace Phi // 전투 설정 영역
{
    public enum WeaponType // 첫 전투 무기 선택
    {
        Sword, // 기본 검
        Bow, // 기본 활
        Staff // 기본 지팡이
    }

    [CreateAssetMenu(menuName = "Phi/Prototype Settings")] // 설정 에셋 생성 메뉴
    public sealed class PrototypeSettings : ScriptableObject // 전투 조정 데이터
    {
        public int playerHealth = 100; // 플레이어 최대 체력
        public int playerAttack = 10; // 플레이어 기본 공격력
        public int playerDefense = 5; // 플레이어 기본 방어력
        public float playerSpeed = 4f; // 플레이어 이동속도
        public int playerMana = 50; // 플레이어 최대 마나
        public float manaRegen = 3f; // 초당 마나 회복량
        public int playerStamina = 100; // 플레이어 최대 스태미나
        public float staminaRegen = 15f; // 초당 스태미나 회복량
        public float staminaRegenDelay = 1f; // 회피 종료 후 재생 대기시간
        public int dodgeCost = 25; // 회피 스태미나 비용
        public float dodgeCooldown = 1.2f; // 회피 재사용 대기시간
        public float dodgeDuration = 0.25f; // 임시 회피 이동 시간
        public float dodgeDistance = 2f; // 임시 회피 이동 거리
        public float dodgeInvulnerability = 0.25f; // 임시 회피 무적 시간
        public float interactionRange = 1.5f; // 임시 상호작용 선택 반경
        [HideInInspector] public float mapHalfWidth = 10f; // 이전 가로 맵 설정 보존
        [HideInInspector] public float mapHalfHeight = 6f; // 이전 세로 맵 설정 보존
        public const float BoundaryThickness = 0.12f; // 공간 테두리 두께
        public const float PlayerRadius = 0.35f; // 플레이어 도형 반폭
        public const float SlimeRadius = 0.425f; // 슬라임 예고 중 최대 도형 반폭
        public const float MinimumMapHalfSize = SlimeRadius + BoundaryThickness; // 도형과 테두리를 담는 최소 반경
        public WeaponType startingWeapon = WeaponType.Sword; // 전투 시작 무기 선택
        public SkillId startingSkillQ = SkillId.Fireball; // 시험용 Q 시작 스킬
        public SkillId startingSkillE = SkillId.WhirlwindSlash; // 시험용 E 시작 스킬
        public SkillId startingSkillR = SkillId.HealingLight; // 시험용 R 시작 스킬
        public int fireballCost = 10; // 화염탄 마나 비용
        public float fireballCooldown = 5f; // 화염탄 재사용 시간
        public float fireballMultiplier = 1.4f; // 화염탄 공격 배율
        public float fireballRange = 7f; // 임시 화염탄 사거리
        public float fireballSpeed = 14f; // 임시 화염탄 속도
        public int whirlwindCost = 14; // 회오리 베기 마나 비용
        public float whirlwindCooldown = 9f; // 회오리 베기 재사용 시간
        public float whirlwindMultiplier = 1.3f; // 회오리 베기 공격 배율
        public float whirlwindRadius = 2f; // 임시 회오리 베기 반경
        public int healingCost = 18; // 치유의 빛 마나 비용
        public float healingCooldown = 20f; // 치유의 빛 재사용 시간
        public float healingFraction = 0.15f; // 최대 체력 대비 회복 비율
        public int swordAttack = 12; // 검 공격력
        public float swordInterval = 0.55f; // 검 공격 간격
        public float swordRange = 1.7f; // 검 공격 사거리
        public float swordAngle = 90f; // 검 공격 각도
        public int swordTargets = 3; // 검 최대 대상 수
        public int bowAttack = 10; // 활 공격력
        public float bowInterval = 0.70f; // 활 공격 간격
        public float bowRange = 7f; // 화살 최대 이동 거리
        public float bowProjectileSpeed = 14f; // 화살 이동속도
        public int staffAttack = 8; // 지팡이 공격력
        public float staffInterval = 0.90f; // 지팡이 공격 간격
        public float staffRange = 5.5f; // 마법탄 최대 이동 거리
        public float staffProjectileSpeed = 9f; // 마법탄 이동속도
        public float staffExplosionRadius = 1.1f; // 마법탄 폭발 반경
        public int staffExplosionTargets = 4; // 폭발 최대 대상 수
        public int slimeHealth = 50; // 슬라임 최대 체력
        public int slimeAttack = 8; // 슬라임 공격력
        public int slimeDefense = 2; // 슬라임 방어력
        public float slimeSpeed = 1.5f; // 임시 슬라임 이동속도
        public float slimeAttackRange = 0.9f; // 임시 슬라임 공격 거리
        public float slimeAttackInterval = 1.2f; // 임시 슬라임 공격 간격
        public float slimeWindup = 0.4f; // 임시 슬라임 공격 예고 시간
        public float slimeDashDistance = 0.8f; // 임시 슬라임 돌진 거리
        public float slimeDashSpeed = 5f; // 임시 슬라임 돌진 속도
        public float slimeHitDistance = 0.65f; // 임시 슬라임 적중 거리
        public int slimeCount = 3; // 임시 시험 적 수
        public int goblinCount = 0; // 기존 설정의 고블린 추가 없음
        public int goblinHealth = 40; // 고블린 최대 체력
        public int goblinAttack = 10; // 고블린 공격력
        public int goblinDefense = 1; // 고블린 방어력
        public float goblinSpeed = 2.5f; // 임시 고블린 이동속도
        public float goblinAttackRange = 1f; // 임시 고블린 공격 거리
        public float goblinWindup = 0.35f; // 임시 고블린 공격 예고 시간
        public float goblinAttackInterval = 1f; // 임시 고블린 공격 간격
        public float goblinAttackAngle = 90f; // 임시 고블린 베기 각도
        public int skeletonCount = 0; // 기존 설정의 궁수 추가 없음
        public int skeletonHealth = 35; // 스켈레톤 궁수 체력
        public int skeletonAttack = 9; // 스켈레톤 궁수 공격력
        public int skeletonDefense = 1; // 스켈레톤 궁수 방어력
        public float skeletonSpeed = 1.8f; // 임시 궁수 이동속도
        public float skeletonKeepDistance = 3f; // 임시 궁수 유지 거리
        public float skeletonAttackRange = 6f; // 임시 궁수 공격 거리
        public float skeletonWindup = 0.5f; // 임시 궁수 공격 예고
        public float skeletonAttackInterval = 1.5f; // 임시 궁수 공격 간격
        public float skeletonProjectileSpeed = 8f; // 임시 적 화살 속도
        public float skeletonProjectileRange = 6f; // 임시 적 화살 사거리
        public int orcCount = 0; // 기존 설정의 오크 추가 없음
        public int orcHealth = 100; // 오크 전사 체력
        public int orcAttack = 15; // 오크 전사 공격력
        public int orcDefense = 5; // 오크 전사 방어력
        public float orcSpeed = 1f; // 임시 오크 이동속도
        public float orcAttackRange = 1.4f; // 임시 오크 강타 거리
        public float orcWindup = 0.7f; // 임시 오크 공격 예고
        public float orcAttackInterval = 1.8f; // 임시 오크 공격 간격
        public float orcAttackAngle = 120f; // 임시 오크 강타 각도
        public int wolfCount = 0; // 기존 설정의 늑대 추가 없음
        public int wolfHealth = 45; // 늑대 체력
        public int wolfAttack = 9; // 늑대 공격력
        public int wolfDefense = 1; // 늑대 방어력
        public float wolfSpeed = 2.2f; // 임시 늑대 이동속도
        public float wolfAttackRange = 2.5f; // 임시 늑대 도약 시작 거리
        public float wolfWindup = 0.45f; // 임시 늑대 도약 예고
        public float wolfAttackInterval = 1.8f; // 임시 늑대 공격 간격
        public float wolfLeapDistance = 2.5f; // 임시 늑대 도약 거리
        public float wolfLeapSpeed = 7f; // 임시 늑대 도약 속도
        public float wolfHitDistance = 0.65f; // 임시 늑대 적중 거리
    }
}






