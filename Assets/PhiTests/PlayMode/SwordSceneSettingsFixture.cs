using System; // 시험 설정 정리 인터페이스
using System.Collections.Generic; // 복제 설정 목록
using System.Reflection; // 씬 설정 참조 접근
using Phi; // 전투 설정과 무기 종류
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 씬 로드 이벤트

internal sealed class SwordSceneSettingsFixture : IDisposable // 검 전용 씬 설정 격리
{
    private readonly List<PrototypeSettings> copies = new List<PrototypeSettings>(); // 시험 중 생성된 설정 복제본
    private readonly int slimeCount; // 시험에 필요한 적 수
    public PrototypeSettings Current { get; private set; } // 현재 씬에 연결된 복제 설정

    public SwordSceneSettingsFixture(int slimeCount) // 검 전투 시험 설정 준비
    {
        this.slimeCount = slimeCount; // 시험 적 수 보관
        SceneManager.sceneLoaded += ConfigureLoadedScene; // 씬 시작 전 설정 연결
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
        Current = null; // 현재 시험 설정 해제
    }

    private void ConfigureLoadedScene(Scene scene, LoadSceneMode mode) // 새 씬의 전투 시작 설정 연결
    {
        if (scene.name != "CombatPrototype") // 대상 전투 씬 확인
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
            PrototypeSettings original = (PrototypeSettings)field.GetValue(bootstrap); // 저장된 원본 설정 참조
            if (original == null) // 저장된 설정 참조 누락 여부
            {
                return; // 누락된 설정을 시험에서 숨기지 않음
            }
            Current = UnityEngine.Object.Instantiate(original); // 원본을 보존한 시험 설정 복제
            copies.Add(Current); // 시험 후 제거할 복제 설정 등록
            SkillSettingsTestFixture.UseRecommended(Current); // 검 시험의 유효한 스킬 조합
            Current.skeletonCount = 0; // 궁수 간섭 제외
            Current.orcCount = 0; // 오크 간섭 제외
            Current.wolfCount = 0; // 늑대 간섭 제외
            Current.startingWeapon = WeaponType.Sword; // 검을 장착한 전투 시작
            Current.playerHealth = 100; // 검 시험의 플레이어 체력
            Current.playerAttack = 10; // 검 시험의 기본 공격력
            Current.playerDefense = 5; // 검 시험의 플레이어 방어력
            Current.playerSpeed = 4f; // 이동 시험의 플레이어 속도
            Current.swordAttack = 12; // 검 시험의 무기 공격력
            Current.swordInterval = 0.55f; // 검 시험의 공격 간격
            Current.swordRange = 1.7f; // 검 시험의 공격 사거리
            Current.swordAngle = 90f; // 검 시험의 공격 각도
            Current.swordTargets = 3; // 검 시험의 최대 대상 수
            Current.slimeHealth = 50; // 검 시험의 적 체력
            Current.slimeAttack = 8; // 피격 시험의 적 공격력
            Current.slimeDefense = 2; // 검 시험의 적 방어력
            Current.slimeSpeed = 0f; // 시험 중 적의 접근 이동 방지
            Current.slimeAttackRange = 0.9f; // 공격 시험의 시작 거리
            Current.slimeAttackInterval = 1.2f; // 공격 시험의 적 공격 간격
            Current.slimeWindup = 0.4f; // 공격 시험의 예고 시간
            Current.slimeDashDistance = 0.8f; // 공격 시험의 돌진 거리
            Current.slimeDashSpeed = 5f; // 공격 시험의 돌진 속도
            Current.slimeHitDistance = 0.65f; // 공격 시험의 적중 거리
            Current.slimeCount = slimeCount; // 시험에 필요한 적 수
            field.SetValue(bootstrap, Current); // 새 씬의 전투 시작 전 복제 설정 연결
            return; // 대상 전투 구성 요소 설정 완료
        }
    }
}
