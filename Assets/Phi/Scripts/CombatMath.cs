using UnityEngine; // 유니티 자료형

namespace Phi // 전투 계산 영역
{
    public static class CombatMath // 공통 전투 계산
    {
        public static int Damage(float attack, float multiplier, float defense, bool critical) // 단계별 피해 계산
        {
            int normal = Mathf.Max(1, Mathf.FloorToInt(attack * multiplier - defense * 0.5f)); // 최소 일반 피해
            return critical ? Mathf.FloorToInt(normal * 1.5f) : normal; // 치명타 배율 적용
        }

        public static Vector2 NormalizeMovement(Vector2 input) // 대각선 속도 제한
        {
            return Vector2.ClampMagnitude(input, 1f); // 입력 크기 제한
        }

        public static bool InSwordSector(Vector2 origin, Vector2 direction, Vector2 target, float range, float angleDegrees) // 검 공격 범위 검사
        {
            Vector2 offset = target - origin; // 대상까지의 방향
            if (offset.sqrMagnitude > range * range || direction.sqrMagnitude < 0.000001f) // 거리와 조준 방향 검사
            {
                return false; // 공격 범위 밖
            }
            if (offset.sqrMagnitude < 0.000001f) // 같은 위치 검사
            {
                return true; // 같은 위치 적중
            }
            return Vector2.Angle(direction, offset) <= angleDegrees * 0.5f; // 부채꼴 각도 검사
        }
    }
}
