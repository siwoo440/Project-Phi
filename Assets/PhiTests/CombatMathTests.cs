using NUnit.Framework; // 테스트 도구
using UnityEngine; // 벡터 자료형
using Phi; // 전투 계산

public class CombatMathTests // 전투 계산 검증
{
    [Test] // 일반 피해 사례
    public void SwordDealsTwentyOneDamageToSlime() // 검과 슬라임 피해 검증
    {
        Assert.That(CombatMath.Damage(22f, 1f, 2f, false), Is.EqualTo(21)); // 기획 기준 피해량
    }

    [Test] // 피해 최솟값 사례
    public void DamageIsAtLeastOne() // 최소 피해 검증
    {
        Assert.That(CombatMath.Damage(1f, 1f, 100f, false), Is.EqualTo(1)); // 방어력이 높은 대상
    }

    [Test] // 치명타 버림 사례
    public void CriticalDamageUsesFlooredNormalDamage() // 단계별 버림 검증
    {
        Assert.That(CombatMath.Damage(2.9f, 1f, 0f, true), Is.EqualTo(3)); // 일반 피해 버림 후 치명타
    }

    [Test] // 방향 정규화 사례
    public void DiagonalMovementIsNotFaster() // 대각선 이동속도 검증
    {
        Vector2 movement = CombatMath.NormalizeMovement(new Vector2(1f, 1f)); // 대각선 입력
        Assert.That(movement.magnitude, Is.EqualTo(1f).Within(0.0001f)); // 최대 이동 크기
        Assert.That(movement.x, Is.EqualTo(movement.y).Within(0.0001f)); // 두 축의 동일 크기
    }

    [Test] // 정지 입력 사례
    public void ZeroMovementRemainsZero() // 정지 입력 검증
    {
        Assert.That(CombatMath.NormalizeMovement(Vector2.zero), Is.EqualTo(Vector2.zero)); // 정지 상태 유지
    }

    [Test] // 검 범위 내부 사례
    public void SwordSectorIncludesTargetAhead() // 정면 적중 검증
    {
        Assert.That(CombatMath.InSwordSector(Vector2.zero, Vector2.right, new Vector2(1f, 0.5f), 1.7f, 90f), Is.True); // 사거리와 각도 내부
    }

    [Test] // 검 범위 외부 사례
    public void SwordSectorExcludesTargetOutsideAngle() // 각도 밖 제외 검증
    {
        Assert.That(CombatMath.InSwordSector(Vector2.zero, Vector2.right, new Vector2(0.5f, 1f), 1.7f, 90f), Is.False); // 부채꼴 바깥 대상
    }

    [Test] // 검 사거리 외부 사례
    public void SwordSectorExcludesTargetBeyondRange() // 사거리 밖 제외 검증
    {
        Assert.That(CombatMath.InSwordSector(Vector2.zero, Vector2.right, new Vector2(1.8f, 0f), 1.7f, 90f), Is.False); // 사거리 바깥 대상
    }
}
