using System; // 시험 설정 정리 인터페이스
using System.Collections.Generic; // 복제 설정 목록
using System.Reflection; // 씬 설정 참조 접근
using Phi; // 전투 설정과 스킬 식별자
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 씬 로드 이벤트

internal sealed class SkillSettingsTestFixture : IDisposable // 재시작 씬의 스킬 설정 격리
{
    private readonly List<PrototypeSettings> copies = new List<PrototypeSettings>(); // 시험 중 생성된 설정 복제본

    public SkillSettingsTestFixture() // 재시작 설정 격리 준비
    {
        SceneManager.sceneLoaded += ConfigureLoadedScene; // 씬 시작 전 설정 연결
    }

    public static void UseRecommended(PrototypeSettings settings) // 시험용 유효 스킬 조합 설정
    {
        settings.startingSkillQ = SkillId.Fireball; // 시험 Q 슬롯
        settings.startingSkillE = SkillId.WhirlwindSlash; // 시험 E 슬롯
        settings.startingSkillR = SkillId.HealingLight; // 시험 R 슬롯
    }

    public void Dispose() // 시험용 설정 정리
    {
        SceneManager.sceneLoaded -= ConfigureLoadedScene; // 씬 로드 이벤트 해제
        foreach (PrototypeSettings copy in copies) // 복제된 시험 설정 순회
        {
            if (copy != null) // 복제 설정 존재 여부
            {
                UnityEngine.Object.Destroy(copy); // 복제 설정 제거
            }
        }
        copies.Clear(); // 시험 설정 목록 초기화
    }

    private void ConfigureLoadedScene(Scene scene, LoadSceneMode mode) // 새 씬의 복제 설정 연결
    {
        if (scene.name != "CombatPrototype") // 시험 전투 씬 여부
        {
            return; // 다른 씬 설정 보존
        }
        foreach (GameObject root in scene.GetRootGameObjects()) // 새 씬 루트 검색
        {
            PrototypeBootstrap bootstrap = root.GetComponentInChildren<PrototypeBootstrap>(true); // 새 씬의 전투 구성 요소
            if (bootstrap == null) // 전투 구성 요소 부재 여부
            {
                continue; // 다음 씬 루트 검사
            }
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 원본 설정 참조 필드
            PrototypeSettings source = (PrototypeSettings)field.GetValue(bootstrap); // 씬에 저장된 설정 참조
            if (source == null) // 설정 참조 누락 여부
            {
                return; // 누락된 설정을 시험에서 숨기지 않음
            }
            PrototypeSettings copy = UnityEngine.Object.Instantiate(source); // 원본을 보존한 시험 설정 복제
            copies.Add(copy); // 시험 후 제거할 복제본 등록
            UseRecommended(copy); // 유효한 스킬 조합 적용
            copy.skeletonCount = 0; // 궁수 간섭 제외
            copy.orcCount = 0; // 오크 간섭 제외
            copy.wolfCount = 0; // 늑대 간섭 제외
            field.SetValue(bootstrap, copy); // 전투 시작 전 복제 설정 연결
            return; // 대상 전투 구성 요소 설정 완료
        }
    }
}
