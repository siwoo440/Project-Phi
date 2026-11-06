using System; // 자료형과 열거형 변환
using System.Collections; // 프레임 단위 시험
using System.Reflection; // 시험 설정과 실행 상태 접근
using System.Text.RegularExpressions; // 슬롯 오류 로그 검사
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 형식
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 무기 버튼과 스킬 안내
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class SkillLoadoutPlayModeTests // 전투 시작 스킬 슬롯 검증
{
    private Mouse mouse; // 가상 마우스
    private Keyboard keyboard; // 가상 키보드
    private PrototypeSettings asset; // 씬에 연결된 원본 설정 에셋
    private PrototypeSettings trial; // 원본을 보존할 시험 설정
    private PrototypeBootstrap bootstrap; // 현재 전투 구성 요소
    private SceneMapTestFixture mapFixture; // 씬 맵 시험 격리
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 시험 입력과 맵 격리 준비
    {
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        mapFixture = new SceneMapTestFixture(); // 씬 맵의 독립 시험 배치
        yield return null; // 시험 준비 프레임
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 시험 자원과 전역 상태 복원
    {
        if (mouse != null && mouse.added) // 가상 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        if (keyboard != null && keyboard.added) // 가상 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
        }
        if (mapFixture != null) // 시험 맵 격리 존재 여부
        {
            mapFixture.Dispose(); // 시험 맵 이벤트와 그림 정리
        }
        if (trial != null) // 임시 설정 존재 여부
        {
            Object.Destroy(trial); // 시험 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 사용자 지정 조합과 무기 전환 사례
    public IEnumerator CustomLoadoutIsIndependentOfWeaponAndSettings() // 시작 슬롯 복사와 에셋 보존 검증
    {
        yield return LoadCustom("IceShard", "Shield", "Teleport", false); // 서로 다른 사용자 지정 조합
        string originalQ = ReadSetting(asset, "startingSkillQ"); // 원본 Q 설정값
        string originalE = ReadSetting(asset, "startingSkillE"); // 원본 E 설정값
        string originalR = ReadSetting(asset, "startingSkillR"); // 원본 R 설정값
        AssertLoadout("IceShard", "Shield", "Teleport"); // 사용자 지정 슬롯 유지
        Text status = SkillStatus(); // 현재 스킬 안내 글자
        StringAssert.Contains("NOT IMPLEMENTED", status.text); // 발동 미구현 안내
        StringAssert.Contains("ICE SHARD", status.text); // Q 후보 이름 안내
        StringAssert.Contains("SHIELD", status.text); // E 후보 이름 안내
        StringAssert.Contains("TELEPORT", status.text); // R 후보 이름 안내
        SetSetting(trial, "startingSkillQ", "Fireball"); // 실행 후 시험 설정의 Q 변경
        AssertLoadout("IceShard", "Shield", "Teleport"); // 실행 상태의 독립 복사 유지
        yield return ClickButton("Equip Bow"); // 실제 활 버튼 클릭
        AssertLoadout("IceShard", "Shield", "Teleport"); // 무기 교체와 독립된 스킬 슬롯
        Assert.That(ReadSetting(asset, "startingSkillQ"), Is.EqualTo(originalQ)); // 원본 Q 설정 불변
        Assert.That(ReadSetting(asset, "startingSkillE"), Is.EqualTo(originalE)); // 원본 E 설정 불변
        Assert.That(ReadSetting(asset, "startingSkillR"), Is.EqualTo(originalR)); // 원본 R 설정 불변
    }

    [UnityTest] // 빈 슬롯과 중복 상태 사례
    public IEnumerator InvalidSlotsReportSpecificErrorsWithoutStoppingWeaponCombat() // 오류 슬롯만 비활성화하고 기본 전투 유지
    {
        yield return LoadCustom("None", "IceShard", "IceShard", true); // 빈 Q와 중복 R의 시험 조합
        object loadout = CurrentLoadout(); // 현재 실행 장착 상태
        Assert.That(Read(loadout, "Q").ToString(), Is.EqualTo("None")); // 빈 Q의 비활성화
        Assert.That(Read(loadout, "E").ToString(), Is.EqualTo("IceShard")); // 유효한 E 유지
        Assert.That(Read(loadout, "R").ToString(), Is.EqualTo("None")); // 뒤쪽 중복 R의 비활성화
        Assert.That(Read(loadout, "QError").ToString(), Is.EqualTo("Empty")); // Q의 빈 슬롯 오류
        Assert.That(Read(loadout, "RError").ToString(), Is.EqualTo("Duplicate")); // R의 중복 오류
        StringAssert.Contains("Q: EMPTY", SkillStatus().text); // Q 오류의 게임 화면 안내
        StringAssert.Contains("R: DUPLICATE", SkillStatus().text); // R 오류의 게임 화면 안내
        Fighter target = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 검 공격 대상
        target.transform.position = new Vector3(1f, 0f, 0f); // 검 사거리 안에 배치
        GameObject.Find("Slime 2").transform.position = new Vector3(-4f, 0f, 0f); // 다른 적의 간섭 제거
        GameObject.Find("Slime 3").transform.position = new Vector3(-4f, 1f, 0f); // 다른 적의 간섭 제거
        Vector2 world = Camera.main.WorldToScreenPoint(new Vector3(3f, 0f, 0f)); // 검 조준 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 실제 검 공격 입력
        yield return null; // 검 공격 처리 대기
        Assert.That(target.Health, Is.EqualTo(29)); // 슬롯 오류 중에도 기존 검 전투 유지
    }

    [UnityTest] // 목록 밖 직렬화 값 사례
    public IEnumerator UnknownSlotIsReportedWithoutChangingConfiguredValue() // 잘못된 스킬 값과 에셋 보존 검증
    {
        yield return LoadCustom("Fireball", "999", "HealingLight", true, "스킬 장착 오류.*E"); // 목록 밖 E의 시험 조합
        object loadout = CurrentLoadout(); // 현재 실행 장착 상태
        Assert.That(Read(loadout, "Q").ToString(), Is.EqualTo("Fireball")); // 유효한 Q 유지
        Assert.That(Read(loadout, "E").ToString(), Is.EqualTo("None")); // 목록 밖 E 비활성화
        Assert.That(Read(loadout, "R").ToString(), Is.EqualTo("HealingLight")); // 유효한 R 유지
        Assert.That(Read(loadout, "EError").ToString(), Is.EqualTo("Unknown")); // E의 목록 밖 오류
        Assert.That(ReadSetting(trial, "startingSkillE"), Is.EqualTo("999")); // 잘못된 설정값의 자동 변경 방지
        StringAssert.Contains("E: UNKNOWN", SkillStatus().text); // E 오류의 게임 화면 안내
    }

    [UnityTest] // 미구현 입력 사례
    public IEnumerator SkillKeysDoNotActivateUnimplementedEffects() // Q/E/R 입력의 미구현 상태 유지
    {
        yield return LoadCustom("IceShard", "Shield", "Teleport", false); // 미구현 스킬 시험 조합
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter target = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 주변 적의 전투 상태
        int playerHealth = player.Health; // 입력 전 플레이어 체력
        int enemyHealth = target.Health; // 입력 전 적 체력
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q, Key.E, Key.R)); // 미구현 스킬 키 입력
        yield return new WaitForSeconds(0.1f); // 입력 처리 대기
        Assert.That(player.Health, Is.EqualTo(playerHealth)); // 미구현 치유 효과 차단
        Assert.That(target.Health, Is.EqualTo(enemyHealth)); // 미구현 피해 효과 차단
        StringAssert.Contains("NOT IMPLEMENTED", SkillStatus().text); // 미구현 상태 표시 유지
    }

    [UnityTest] // 재시작 상태 사례
    public IEnumerator RestartCopiesConfiguredSlotsAgain() // 출정 설정의 재시작 복원 검증
    {
        yield return LoadCustom("IceShard", "Shield", "Teleport", false); // 사용자 지정 시작 조합
        PrototypeBootstrap previous = bootstrap; // 이전 전투 구성 요소
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 다시 시작한 씬의 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, trial); // 같은 시작 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 재시작 설정 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 재시작
        yield return null; // 씬 전환 대기
        yield return null; // 새 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 재시작 콜백 해제
        Assert.That(bootstrap, Is.Not.SameAs(previous)); // 새 전투 상태 생성
        AssertLoadout("IceShard", "Shield", "Teleport"); // 시작 설정으로 슬롯 복원
    }

    private IEnumerator LoadCustom(string q, string e, string r, bool expectError, string errorPattern = "스킬 장착 오류.*Q.*R") // 시험 조합으로 전투 씬 로드
    {
        Type idType = typeof(PrototypeSettings).Assembly.GetType("Phi.SkillId"); // 스킬 식별자 자료형
        Assert.That(idType, Is.Not.Null, "공용 스킬 식별자가 필요합니다."); // 공용 스킬 자료형 확인
        Assert.That(typeof(PrototypeSettings).GetField("startingSkillQ"), Is.Not.Null, "Q 시작 설정이 필요합니다."); // Q 설정 확인
        Assert.That(typeof(PrototypeSettings).GetField("startingSkillE"), Is.Not.Null, "E 시작 설정이 필요합니다."); // E 설정 확인
        Assert.That(typeof(PrototypeSettings).GetField("startingSkillR"), Is.Not.Null, "R 시작 설정이 필요합니다."); // R 설정 확인
        if (expectError) // 잘못된 시험 조합 여부
        {
            LogAssert.Expect(LogType.Error, new Regex(errorPattern)); // 슬롯별 오류 보고 기대
        }
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 씬 전투 구성 요소
            FieldInfo settingsField = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 참조 필드
            asset = (PrototypeSettings)settingsField.GetValue(bootstrap); // 원본 설정 에셋 보관
            trial = Object.Instantiate(asset); // 원본을 보존할 시험 설정 복제
            trial.skeletonCount = 0; // 궁수 간섭 제외
            trial.orcCount = 0; // 오크 간섭 제외
            trial.wolfCount = 0; // 늑대 간섭 제외
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.playerAttack = 10; // 시험 플레이어 공격력
            trial.slimeCount = 3; // 시험 슬라임 수
            trial.slimeHealth = 50; // 시험 슬라임 체력
            trial.slimeDefense = 2; // 시험 슬라임 방어력
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            trial.swordAttack = 12; // 시험 검 공격력
            trial.swordRange = 1.7f; // 시험 검 사거리
            trial.swordAngle = 90f; // 시험 검 부채꼴 각도
            trial.swordTargets = 3; // 시험 검 최대 대상 수
            SetSetting(trial, "startingSkillQ", q); // 시험 Q 설정
            SetSetting(trial, "startingSkillE", e); // 시험 E 설정
            SetSetting(trial, "startingSkillR", r); // 시험 R 설정
            settingsField.SetValue(bootstrap, trial); // 시작 전 시험 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 시험 씬 설정 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 시험 설정 콜백 해제
    }

    private static void SetSetting(PrototypeSettings settings, string name, string value) // 시험 설정의 스킬 값 변경
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 설정 필드 확인
        field.SetValue(settings, Enum.Parse(field.FieldType, value)); // 시험 설정에 열거형 값 적용
    }

    private static string ReadSetting(PrototypeSettings settings, string name) // 설정의 스킬 값 읽기
    {
        return typeof(PrototypeSettings).GetField(name).GetValue(settings).ToString(); // 저장된 시작 스킬 이름
    }

    private object CurrentLoadout() // 현재 전투의 독립 스킬 상태
    {
        PropertyInfo property = typeof(PrototypeBootstrap).GetProperty("EquippedSkills"); // 실행 슬롯 공개 읽기 속성
        Assert.That(property, Is.Not.Null, "독립 실행 스킬 상태가 필요합니다."); // 실행 상태 속성 확인
        object loadout = property.GetValue(bootstrap); // 실제 실행 장착 상태
        Assert.That(loadout, Is.Not.Null); // 실행 장착 상태 존재 확인
        return loadout; // 확인된 실행 상태
    }

    private void AssertLoadout(string q, string e, string r) // 실행 슬롯의 세 값 확인
    {
        object loadout = CurrentLoadout(); // 현재 실행 장착 상태
        Assert.That(Read(loadout, "Q").ToString(), Is.EqualTo(q)); // 현재 Q 슬롯
        Assert.That(Read(loadout, "E").ToString(), Is.EqualTo(e)); // 현재 E 슬롯
        Assert.That(Read(loadout, "R").ToString(), Is.EqualTo(r)); // 현재 R 슬롯
    }

    private static object Read(object value, string name) // 공개 실행 상태 속성 읽기
    {
        PropertyInfo property = value.GetType().GetProperty(name); // 슬롯별 공개 속성
        Assert.That(property, Is.Not.Null, name + " 속성이 필요합니다."); // 슬롯 속성 확인
        return property.GetValue(value); // 슬롯 속성 값
    }

    private static Text SkillStatus() // 게임 화면의 스킬 안내 확인
    {
        GameObject status = GameObject.Find("Skill Status"); // 스킬 상태 안내 오브젝트
        Assert.That(status, Is.Not.Null, "스킬 미구현 안내가 필요합니다."); // 안내 오브젝트 존재 확인
        Text text = status.GetComponent<Text>(); // 안내 글자 구성 요소
        Assert.That(text, Is.Not.Null); // 안내 글자 존재 확인
        return text; // 스킬 안내 글자
    }

    private IEnumerator ClickButton(string name) // 실제 UI 포인터 클릭
    {
        GameObject button = GameObject.Find(name); // 이름에 맞는 무기 버튼
        Assert.That(button, Is.Not.Null); // 버튼 존재 확인
        RectTransform rect = button.GetComponent<RectTransform>(); // 버튼의 화면 배치
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // 버튼 중앙 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 위 포인터 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // 버튼 누름
        yield return null; // 버튼 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 놓기
        yield return null; // 버튼 클릭 처리 대기
    }
}
