using System; // 자료형 변환
using System.Reflection; // 실행 상태 동적 확인
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 설정 자료형
using UnityEngine; // 설정 에셋 생성
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class DodgeRuntimeTests // 스태미나와 회피 상태 검증
{
    private PrototypeSettings settings; // 독립 시험 설정
    private object state; // 시험 회피 실행 상태

    [SetUp] // 각 시험 준비
    public void SetUp() // 기본값의 독립 실행 상태 생성
    {
        settings = ScriptableObject.CreateInstance<PrototypeSettings>(); // 사용자 에셋과 분리된 설정
        Type type = typeof(PrototypeSettings).Assembly.GetType("Phi.DodgeRuntime"); // 회피 실행 상태 자료형
        Assert.That(type, Is.Not.Null, "회피 실행 상태가 필요합니다."); // 실행 상태 존재 확인
        state = Activator.CreateInstance(type, new object[] { settings }); // 설정에서 상태 생성
    }

    [TearDown] // 각 시험 정리
    public void TearDown() // 임시 설정 제거
    {
        if (settings != null) // 시험 설정 존재 여부
        {
            Object.DestroyImmediate(settings); // 시험 설정 제거
        }
    }

    [Test] // 기본값과 회피 진입 사례
    public void DodgeConsumesTwentyFiveAndStartsDurationAndCooldown() // 비용·시간·무적 초기 상태 검증
    {
        Assert.That(Value("Stamina"), Is.EqualTo(100f)); // 기본 최대 스태미나
        Assert.That(Value("MaximumStamina"), Is.EqualTo(100f)); // 실행 상태의 최대 스태미나
        Assert.That(StartDodge(), Is.True); // 첫 회피 성공
        Assert.That(Value("Stamina"), Is.EqualTo(75f)); // 25 스태미나 차감
        Assert.That(Value("RemainingCooldown"), Is.EqualTo(1.2f).Within(0.0001f)); // 1.2초 대기시간 시작
        Assert.That(Flag("IsDodging"), Is.True); // 회피 이동 상태 시작
        Assert.That(Flag("IsInvulnerable"), Is.True); // 임시 무적 구간 시작
        Assert.That(StartDodge(), Is.False); // 회피 중 중복 시도 차단
        Assert.That(Value("Stamina"), Is.EqualTo(75f)); // 실패한 재입력의 비용 보존
        Tick(0.24f); // 종료 직전 시간
        Assert.That(Flag("IsDodging"), Is.True); // 0.25초 전 이동 유지
        Assert.That(Flag("IsInvulnerable"), Is.True); // 종료 직전 무적 유지
        Tick(0.02f); // 회피 종료를 넘는 시간
        Assert.That(Flag("IsDodging"), Is.False); // 이동 시간 종료
        Assert.That(Flag("IsInvulnerable"), Is.False); // 무적 시간 종료
    }

    [Test] // 대기시간 경계 사례
    public void CooldownPreventsReuseUntilOnePointTwoSeconds() // 재사용 경계와 실패 상태 검증
    {
        Assert.That(StartDodge(), Is.True); // 첫 회피 성공
        Tick(1.19f); // 대기시간 종료 직전
        Assert.That(StartDodge(), Is.False); // 1.2초 이전 재사용 차단
        Assert.That(Value("Stamina"), Is.EqualTo(75f).Within(0.0001f)); // 대기 중 재입력의 비용 보존
        Tick(0.01f); // 대기시간 경계 도달
        Assert.That(Value("RemainingCooldown"), Is.EqualTo(0f).Within(0.0001f)); // 음수 없는 대기시간 종료
        Assert.That(StartDodge(), Is.True); // 경계 이후 회피 허용
        Assert.That(Value("Stamina"), Is.EqualTo(50f).Within(0.0001f)); // 두 번째 비용 차감
    }

    [Test] // 재생 지연과 긴 프레임 사례
    public void RegenerationBeginsOneSecondAfterDodgeAndUsesOnlyRemainingTime() // 경계 이후 경과분의 재생 검증
    {
        Assert.That(StartDodge(), Is.True); // 회피로 스태미나 소모
        Tick(1.0f); // 이동 종료 후 0.75초 경과
        Assert.That(Value("Stamina"), Is.EqualTo(75f).Within(0.0001f)); // 재생 대기 중 자원 유지
        Tick(0.75f); // 대기 경계 이후 0.5초 포함
        Assert.That(Value("Stamina"), Is.EqualTo(82.5f).Within(0.0001f)); // 실제 회복 시간만큼 초당 15 회복
        Tick(20f); // 최대치를 넘는 재생 시간
        Assert.That(Value("Stamina"), Is.EqualTo(100f)); // 최대 스태미나 초과 방지
    }

    [Test] // 비용과 최대치 경계 사례
    public void ExactCostSucceedsAndInsufficientStaminaPreservesCooldown() // 부족한 자원의 시전 방지
    {
        SetMaximum(24f); // 비용보다 작은 최대 스태미나
        Assert.That(StartDodge(), Is.False); // 부족한 자원의 회피 차단
        Assert.That(Value("Stamina"), Is.EqualTo(24f)); // 실패 시 자원 보존
        Assert.That(Value("RemainingCooldown"), Is.EqualTo(0f)); // 실패 시 대기시간 미시작
        SetMaximum(25f); // 비용과 같은 최대 스태미나
        Assert.That(StartDodge(), Is.True); // 정확한 비용으로 회피 성공
        Assert.That(Value("Stamina"), Is.EqualTo(0f)); // 정확한 비용 전액 차감
        Tick(1.2f); // 대기시간이 끝나고 재생은 시작 전인 시점
        Assert.That(StartDodge(), Is.False); // 회복 전 부족한 자원 차단
        Assert.That(Value("RemainingCooldown"), Is.EqualTo(0f).Within(0.0001f)); // 실패 시 대기시간 보존
    }

    private void SetMaximum(float amount) // 시험용 최대 자원과 상태 재생성
    {
        FieldInfo field = typeof(PrototypeSettings).GetField("playerStamina"); // 최대 스태미나 설정
        Assert.That(field, Is.Not.Null); // 새 설정 필드 확인
        field.SetValue(settings, Convert.ChangeType(amount, field.FieldType)); // 시험용 최대치 적용
        state = Activator.CreateInstance(state.GetType(), new object[] { settings }); // 독립 실행 상태 재생성
    }

    private float Value(string name) // 실수 상태 읽기
    {
        PropertyInfo property = state.GetType().GetProperty(name); // 지정 읽기 속성
        Assert.That(property, Is.Not.Null, name + " 속성이 필요합니다."); // 속성 존재 확인
        return Convert.ToSingle(property.GetValue(state)); // 실행 상태 값
    }

    private bool Flag(string name) // 참 또는 거짓 상태 읽기
    {
        PropertyInfo property = state.GetType().GetProperty(name); // 지정 읽기 속성
        Assert.That(property, Is.Not.Null, name + " 속성이 필요합니다."); // 속성 존재 확인
        return (bool)property.GetValue(state); // 실행 상태 값
    }

    private bool StartDodge() // 회피 시도
    {
        MethodInfo method = state.GetType().GetMethod("TryDodge"); // 회피 시작 메서드
        Assert.That(method, Is.Not.Null); // 회피 시작 기능 확인
        return (bool)method.Invoke(state, null); // 회피 성공 여부
    }

    private void Tick(float elapsed) // 결정적 경과 시간 적용
    {
        MethodInfo method = state.GetType().GetMethod("Tick"); // 시간 갱신 메서드
        Assert.That(method, Is.Not.Null); // 시간 갱신 기능 확인
        method.Invoke(state, new object[] { elapsed }); // 지정된 시간 진행
    }
}
