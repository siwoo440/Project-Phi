using UnityEngine; // 유니티 자료형

namespace Phi // 전투 설정 영역
{
    [CreateAssetMenu(menuName = "Phi/Prototype Settings")] // 설정 에셋 생성 메뉴
    public sealed class PrototypeSettings : ScriptableObject // 전투 조정 데이터
    {
        public int playerHealth = 100; // 플레이어 최대 체력
        public int playerAttack = 10; // 플레이어 기본 공격력
        public int playerDefense = 5; // 플레이어 기본 방어력
        public float playerSpeed = 4f; // 플레이어 이동속도
        [Min(MinimumMapHalfSize)] public float mapHalfWidth = 10f; // 시험 공간 가로 반경
        [Min(MinimumMapHalfSize)] public float mapHalfHeight = 6f; // 시험 공간 세로 반경
        public const float BoundaryThickness = 0.12f; // 공간 테두리 두께
        public const float PlayerRadius = 0.35f; // 플레이어 도형 반폭
        public const float SlimeRadius = 0.425f; // 슬라임 예고 중 최대 도형 반폭
        public const float MinimumMapHalfSize = SlimeRadius + BoundaryThickness; // 도형과 테두리를 담는 최소 반경
        public int swordAttack = 12; // 검 공격력
        public float swordInterval = 0.55f; // 검 공격 간격
        public float swordRange = 1.7f; // 검 공격 사거리
        public float swordAngle = 90f; // 검 공격 각도
        public int swordTargets = 3; // 검 최대 대상 수
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

        public Vector2 ClampPosition(Vector2 position, float radius) // 도형 전체의 시험 공간 내부 좌표 계산
        {
            float width = Mathf.Max(MinimumMapHalfSize, mapHalfWidth); // 최소 가로 반경
            float height = Mathf.Max(MinimumMapHalfSize, mapHalfHeight); // 최소 세로 반경
            float margin = Mathf.Max(0f, radius) + BoundaryThickness; // 도형과 테두리 여유
            float limitX = Mathf.Max(0f, width - margin); // 가로 중심 이동 한계
            float limitY = Mathf.Max(0f, height - margin); // 세로 중심 이동 한계
            return new Vector2(Mathf.Clamp(position.x, -limitX, limitX), Mathf.Clamp(position.y, -limitY, limitY)); // 도형 전체의 사방 경계 제한
        }
    }
}




