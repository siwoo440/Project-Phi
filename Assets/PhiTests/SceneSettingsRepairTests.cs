using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using Phi.Editor; // 씬 설정 복구 도구
using UnityEditor; // 직렬화된 설정 참조
using UnityEngine; // 유니티 자료형

public class SceneSettingsRepairTests // 씬 설정 복구 검증
{
    private GameObject root; // 시험용 씬 오브젝트
    private PrototypeBootstrap bootstrap; // 시험용 전투 구성 요소
    private PrototypeSettings fallback; // 기본 설정 에셋 대체본
    private PrototypeSettings custom; // 사용자 지정 설정 대체본

    [SetUp] // 각 시험 전 준비
    public void SetUp() // 독립 시험 자원 준비
    {
        root = new GameObject("Settings Repair Test"); // 시험용 오브젝트 생성
        bootstrap = root.AddComponent<PrototypeBootstrap>(); // 전투 구성 요소 추가
        fallback = ScriptableObject.CreateInstance<PrototypeSettings>(); // 기본 설정 생성
        custom = ScriptableObject.CreateInstance<PrototypeSettings>(); // 사용자 설정 생성
    }

    [TearDown] // 각 시험 후 정리
    public void TearDown() // 시험 자원 제거
    {
        Object.DestroyImmediate(root); // 시험용 오브젝트 제거
        Object.DestroyImmediate(fallback); // 기본 설정 제거
        Object.DestroyImmediate(custom); // 사용자 설정 제거
    }

    [Test] // 설정 누락 사례
    public void MissingSettingsAreRestored() // 누락된 참조 복구 검증
    {
        Assert.That(PrototypeSceneBuilder.EnsureSettingsReference(bootstrap, fallback), Is.True); // 참조 복구 결과 확인
        Assert.That(ReadSettings(), Is.SameAs(fallback)); // 기본 설정 연결 확인
    }

    [Test] // 사용자 설정 보존 사례
    public void ExistingSettingsArePreserved() // 기존 참조 보존 검증
    {
        SerializedObject serialized = new SerializedObject(bootstrap); // 구성 요소 편집기
        serialized.FindProperty("settings").objectReferenceValue = custom; // 사용자 지정 참조 연결
        serialized.ApplyModifiedPropertiesWithoutUndo(); // 사용자 참조 적용
        Assert.That(PrototypeSceneBuilder.EnsureSettingsReference(bootstrap, fallback), Is.False); // 불필요한 복구 방지 확인
        Assert.That(ReadSettings(), Is.SameAs(custom)); // 사용자 지정 설정 보존 확인
    }

    private PrototypeSettings ReadSettings() // 현재 설정 참조 확인
    {
        SerializedObject serialized = new SerializedObject(bootstrap); // 구성 요소 편집기
        return (PrototypeSettings)serialized.FindProperty("settings").objectReferenceValue; // 저장된 설정 참조
    }
}
