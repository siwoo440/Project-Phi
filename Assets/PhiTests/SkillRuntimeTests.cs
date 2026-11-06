using System; // 자료형과 열거형 변환
using System.Reflection; // 실행 상태 동적 확인
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 설정 자료형
using UnityEngine; // 설정 에셋 생성
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class SkillRuntimeTests // 마나와 독립 대기시간 검증
{
    private PrototypeSettings settings; // 독립 시험 설정
    private object state; // 시험 스킬 실행 상태

    [SetUp] // 각 시험의 준비
    public void SetUp() // 기획 수치의 독립 실행 상태
    {
        settings = ScriptableObject.CreateInstance<PrototypeSettings>(); // 사용자 에셋과 분리된 설정
        Type type = typeof(PrototypeSettings).Assembly.GetType("Phi.SkillRuntime"); // 스킬 실행 상태 자료형
        Assert.That(type, Is.Not.Null, "스킬 실행 상태가 필요합니다."); // 실행 상태 존재 확인
        state = Activator.CreateInstance(type, new object[] { settings }); // 시작 수치로 상태 생성
    }

    [TearDown] // 각 시험의 정리
    public void TearDown() // 임시 설정 제거
    {
        if (settings != null) // 임시 설정 존재 여부
        {
            Object.DestroyImmediate(settings); // 시험 설정 제거
        }
    }

    [Test] // 기본 마나와 회복 사례
    public void ManaRegeneratesFractionallyAndStopsAtMaximum() // 소수 회복과 최대치 검증
    {
        Assert.That(Mana(), Is.EqualTo(50f)); // 기본 최대 마나
        Assert.That(Use(0, SkillId.Fireball), Is.True); // 화염탄 비용 사용
        Assert.That(Mana(), Is.EqualTo(40f)); // 즉시 10 마나 소모
        Tick(0.5f); // 반 초의 회복 시간
        Assert.That(Mana(), Is.EqualTo(41.5f).Within(0.0001f)); // 초당 3 마나 회복
        Tick(100f); // 최대치를 초과하는 회복 시간
        Assert.That(Mana(), Is.EqualTo(50f)); // 최대 마나 초과 방지
    }

    [Test] // 각 슬롯의 독립 대기시간 사례
    public void EachSkillConsumesItsOwnCostAndCooldown() // 세 스킬 비용과 대기시간 검증
    {
        Assert.That(Use(0, SkillId.Fireball), Is.True); // Q 화염탄 사용
        Assert.That(Use(1, SkillId.WhirlwindSlash), Is.True); // E 회오리 사용
        Assert.That(Use(2, SkillId.HealingLight), Is.True); // R 치유 사용
        Assert.That(Mana(), Is.EqualTo(8f)); // 각각 10·14·18 마나 소모
        Assert.That(Cooldown(0), Is.EqualTo(5f)); // 화염탄 5초 대기
        Assert.That(Cooldown(1), Is.EqualTo(9f)); // 회오리 9초 대기
        Assert.That(Cooldown(2), Is.EqualTo(20f)); // 치유 20초 대기
        Tick(5f); // 화염탄 대기 종료 시간
        Assert.That(Cooldown(0), Is.EqualTo(0f).Within(0.0001f)); // Q 재사용 가능
        Assert.That(Cooldown(1), Is.EqualTo(4f).Within(0.0001f)); // E 대기 유지
        Assert.That(Cooldown(2), Is.EqualTo(15f).Within(0.0001f)); // R 대기 유지
    }

    [Test] // 실패 시 자원 보존 사례
    public void CooldownInsufficientManaAndUnsupportedSkillDoNotConsume() // 실패 조건의 상태 보존
    {
        Assert.That(Use(0, SkillId.Fireball), Is.True); // 화염탄 최초 사용
        Assert.That(Use(0, SkillId.Fireball), Is.False); // 대기 중 재사용 차단
        Assert.That(Mana(), Is.EqualTo(40f)); // 대기 중 재입력의 마나 보존
        Assert.That(Use(1, SkillId.IceShard), Is.False); // 미구현 스킬 사용 차단
        Assert.That(Cooldown(1), Is.EqualTo(0f)); // 미구현 스킬 대기시간 미시작
        Assert.That(Use(2, SkillId.HealingLight, false), Is.False); // 발동 조건 불충족 차단
        Assert.That(Mana(), Is.EqualTo(40f)); // 실패 시 마나 보존
        Assert.That(Cooldown(2), Is.EqualTo(0f)); // 실패 시 대기시간 미시작
        Assert.That(Use(1, SkillId.WhirlwindSlash), Is.True); // 두 번째 스킬 사용
        Assert.That(Use(2, SkillId.HealingLight), Is.True); // 세 번째 스킬 사용
        Tick(5f); // Q 대기 종료 및 마나 일부 회복
        Assert.That(Use(0, SkillId.Fireball), Is.True); // 회복된 마나로 재사용
        Assert.That(Use(0, SkillId.Fireball), Is.False); // 재사용 대기 중 추가 입력 차단
    }

    [Test] // 비용 경계 사례
    public void ManaMustMeetExactCostBeforeCooldownStarts() // 비용과 현재 마나의 경계 검증
    {
        SetMaximumMana(9f); // 비용보다 부족한 최대 마나
        Assert.That(Use(0, SkillId.Fireball), Is.False); // 부족한 마나의 발동 차단
        Assert.That(Mana(), Is.EqualTo(9f)); // 부족 시 마나 보존
        Assert.That(Cooldown(0), Is.EqualTo(0f)); // 부족 시 대기시간 미시작
        SetMaximumMana(10f); // 비용과 같은 최대 마나
        Assert.That(Use(0, SkillId.Fireball), Is.True); // 정확한 비용으로 발동
        Assert.That(Mana(), Is.EqualTo(0f)); // 정확한 비용 전액 차감
        Assert.That(Cooldown(0), Is.EqualTo(5f)); // 성공 시 대기시간 시작
    }

    private void SetMaximumMana(float value) // 시험용 최대 마나와 상태 재생성
    {
        FieldInfo field = typeof(PrototypeSettings).GetField("playerMana"); // 최대 마나 설정 필드
        Assert.That(field, Is.Not.Null); // 설정 필드 존재 확인
        field.SetValue(settings, Convert.ChangeType(value, field.FieldType)); // 시험용 최대 마나 설정
        state = Activator.CreateInstance(state.GetType(), new object[] { settings }); // 변경된 설정으로 독립 상태 생성
    }

    private float Mana() // 현재 마나 읽기
    {
        return Convert.ToSingle(state.GetType().GetProperty("Mana").GetValue(state)); // 실수 마나 값
    }

    private float Cooldown(int slot) // 슬롯별 남은 대기시간 읽기
    {
        return Convert.ToSingle(state.GetType().GetMethod("RemainingCooldown").Invoke(state, new object[] { slot })); // 남은 시간 값
    }

    private void Tick(float elapsed) // 시험 시간 전달
    {
        state.GetType().GetMethod("Tick").Invoke(state, new object[] { elapsed }); // 결정적 경과 시간 적용
    }

    private bool Use(int slot, SkillId id, bool allowed = true) // 지정 슬롯의 스킬 사용
    {
        return (bool)state.GetType().GetMethod("TryUse").Invoke(state, new object[] { slot, id, allowed }); // 사용 성공 여부
    }
}
