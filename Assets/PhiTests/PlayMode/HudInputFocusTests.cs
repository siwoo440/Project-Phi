using System; // 동적 구성 요소 자료형
using System.Collections; // 프레임 단위 시험
using System.Reflection; // 공격 대기시간 조회
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.EventSystems; // UI 포커스
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 시험 씬 로드
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 입력 필드와 글자
using Object = UnityEngine.Object; // 유니티 객체 구분

public class HudInputFocusTests // HUD와 UI 포커스의 전투 입력 검증
{
    private Mouse mouse; // 시험 마우스
    private Keyboard keyboard; // 시험 키보드
    private SceneMapTestFixture map; // 독립 시험 맵
    private SwordSceneSettingsFixture settings; // 독립 시험 설정
    private bool oldBackground; // 기존 백그라운드 실행
    private InputSettings.BackgroundBehavior oldBehavior; // 기존 장치 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 전투 준비
    public IEnumerator SetUp() // 실제 전투 씬과 입력 장치 생성
    {
        oldBackground = Application.runInBackground; // 기존 실행 설정 보관
        oldBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        oldEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 비활성 시험 실행 허용
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 유지
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 경로
        mouse = InputSystem.AddDevice<Mouse>(); // 독립 마우스
        keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 키보드
        map = new SceneMapTestFixture(); // 시험 맵 인스턴스
        settings = new SwordSceneSettingsFixture(0); // 적이 없는 독립 전투
        SceneManager.sceneLoaded += ConfigureFocusSettings; // 전투 시작 전 시험 수치 설정
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬
        yield return null; // 전투 생성 대기
        yield return null; // UI 배치 대기
    }

    [UnityTearDown] // 각 시험 자원 정리
    public IEnumerator TearDown() // 장치와 전역 설정 복원
    {
        if (mouse != null && mouse.added) // 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 시험 마우스 제거
        }
        if (keyboard != null && keyboard.added) // 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
        }
        SceneManager.sceneLoaded -= ConfigureFocusSettings; // 시험 설정 이벤트 해제
        settings.Dispose(); // 설정 복제본 정리
        map.Dispose(); // 시험 맵 정리
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = oldBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = oldBackground; // 기존 실행 설정 복원
        yield return null; // 제거 대기
    }

    [UnityTest] // 스킬 및 상호작용 HUD 클릭 사례
    public IEnumerator HudPressAndDragNeverStartsAttack() // HUD 클릭과 드래그 차단
    {
        Vector2 world = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // UI 밖 전투 화면
        foreach (string name in new[] { "Skill Status", "Interaction Status" }) // 표시 HUD 순회
        {
            RectTransform rect = GameObject.Find(name).GetComponent<RectTransform>(); // HUD 표시 영역
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // HUD 중앙 좌표
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // HUD로 포인터 이동
            yield return null; // 포인터 반영 대기
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // HUD 클릭
            yield return null; // 클릭 처리 대기
            Assert.That(NextAttack(), Is.EqualTo(0f), name); // HUD 클릭의 공격 차단
            InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 누른 채 월드로 이동
            yield return null; // 드래그 반영 대기
            Assert.That(NextAttack(), Is.EqualTo(0f), name); // HUD에서 시작한 드래그 차단
            InputSystem.QueueStateEvent(mouse, new MouseState { position = world }); // 마우스 놓기
            yield return null; // 해제 반영 대기
        }
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 새 월드 클릭
        yield return null; // 공격 처리 대기
        Assert.That(NextAttack(), Is.GreaterThan(0f)); // 별도 월드 클릭의 공격 허용
    }

    [UnityTest] // 텍스트 편집 중 전투 입력 사례
    public IEnumerator TextEditingBlocksMovementAndMouseAttack() // 입력 필드의 전투 입력 차단
    {
        GameObject fieldObject = new GameObject("Focus Test Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 시험 입력 필드
        fieldObject.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform, false); // 전투 캔버스에 연결
        fieldObject.GetComponent<Image>().raycastTarget = false; // 포인터 차단 없이 포커스만 시험
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 입력 글자
        textObject.transform.SetParent(fieldObject.transform, false); // 입력 필드 자식 연결
        Text text = textObject.GetComponent<Text>(); // 글자 구성 요소
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 글꼴
        text.raycastTarget = false; // 글자 대신 포커스 차단만 검증
        InputField input = fieldObject.GetComponent<InputField>(); // 입력 필드 구성 요소
        input.textComponent = text; // 글자와 입력 연결
        EventSystem.current.SetSelectedGameObject(fieldObject); // 편집 대상 선택
        input.ActivateInputField(); // 실제 편집 시작
        yield return null; // 포커스 적용 대기
        yield return null; // 입력 필드 활성 대기
        Assert.That(input.isFocused, Is.True); // 실제 텍스트 포커스
        Vector3 before = GameObject.Find("Player").transform.position; // 입력 전 플레이어 위치
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 편집 중 이동 키
        yield return null; // 입력 처리 대기
        Assert.That(GameObject.Find("Player").transform.position, Is.EqualTo(before)); // 편집 중 이동 차단
        Vector2 world = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // UI 밖 전투 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 편집 중 마우스 클릭
        yield return null; // 클릭 처리 대기
        Assert.That(NextAttack(), Is.EqualTo(0f)); // 편집 중 공격 차단
        Object.Destroy(fieldObject); // 시험 입력 필드 제거
    }

    [UnityTest] // 여러 화면의 전투 포커스 사례
    public IEnumerator ModalFocusBlocksCombatUntilAllScreensCloseAndClickIsReleased() // 중첩 포커스와 마우스 홀드 차단
    {
        Type type = typeof(PrototypeBootstrap).Assembly.GetType("Phi.CombatInputFocus"); // 화면 포커스 구성 요소
        Assert.That(type, Is.Not.Null, "화면 전투 입력 포커스가 필요합니다."); // 포커스 계약 확인
        GameObject first = new GameObject("First Modal"); // 첫 화면
        GameObject second = new GameObject("Second Modal"); // 두 번째 화면
        first.AddComponent(type); // 첫 화면 포커스 획득
        second.AddComponent(type); // 두 번째 화면 포커스 획득
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 전투 플레이어
        Vector3 before = player.transform.position; // 차단 전 위치
        PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 전투 입력 처리기
        float mana = battle.SkillState.Mana; // 차단 전 마나
        float stamina = battle.DodgeState.Stamina; // 차단 전 스태미나
        GameObject loot = new GameObject("Focus Test Loot"); // 상호작용 차단 대상
        loot.transform.position = player.transform.position + new Vector3(0.5f, 0f, 0f); // 플레이어 근처 배치
        Interactable target = loot.AddComponent<Interactable>(); // 공통 상호작용 대상
        target.Configure(InteractionKind.Loot, "TEST"); // 시험 전리품 설정
        Vector2 world = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // UI 밖 전투 좌표
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Q, Key.LeftShift, Key.F)); // 이동과 스킬 및 상호작용 입력
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 화면 포커스 중 공격
        yield return null; // 입력 처리 대기
        Assert.That(player.transform.position, Is.EqualTo(before)); // 포커스 중 이동 차단
        Assert.That(battle.SkillState.Mana, Is.EqualTo(mana)); // 포커스 중 스킬 차단
        Assert.That(battle.DodgeState.Stamina, Is.EqualTo(stamina)); // 포커스 중 회피 차단
        Assert.That(target.IsAvailable, Is.True); // 포커스 중 전리품 수집 차단
        Assert.That(NextAttack(), Is.EqualTo(0f)); // 포커스 중 공격 차단
        first.SetActive(false); // 첫 화면 비활성화
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키보드 해제
        yield return null; // 포커스 변경 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 남은 화면 포커스의 스킬 입력
        yield return null; // 입력 처리 대기
        Assert.That(battle.SkillState.Mana, Is.EqualTo(mana)); // 남은 화면의 차단 유지
        Object.Destroy(second); // 마지막 화면 파괴
        yield return null; // 파괴 반영 대기
        yield return null; // 홀드 유지 확인
        Assert.That(NextAttack(), Is.EqualTo(0f)); // 포커스 해제 후 기존 홀드 차단
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }); // 기존 클릭 해제
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키보드 해제
        yield return null; // 해제 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 새 공격 클릭
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F, Key.Q)); // 포커스 해제 후 상호작용과 스킬
        yield return null; // 새 입력 처리 대기
        Assert.That(NextAttack(), Is.GreaterThan(0f)); // 모든 포커스 해제 후 공격 허용
        Assert.That(battle.CollectedLootCount, Is.EqualTo(1)); // 포커스 해제 후 수집 허용
        Assert.That(battle.SkillState.Mana, Is.LessThan(mana - 9f)); // 포커스 해제 후 스킬 허용
        Object.Destroy(loot); // 시험 전리품 제거
        Object.Destroy(first); // 첫 시험 화면 제거
    }

    [UnityTest] // 공격 중 화면 포커스 획득 사례
    public IEnumerator OpeningModalDuringHeldAttackRequiresNewClickAfterClose() // 기존 홀드의 화면 종료 후 재개 방지
    {
        Type type = typeof(PrototypeBootstrap).Assembly.GetType("Phi.CombatInputFocus"); // 화면 포커스 구성 요소
        Assert.That(type, Is.Not.Null); // 포커스 계약 확인
        Vector2 world = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // UI 밖 전투 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 전투 중 홀드 시작
        yield return null; // 첫 공격 처리 대기
        float firstAttack = NextAttack(); // 첫 공격의 다음 가능 시각
        Assert.That(firstAttack, Is.GreaterThan(0f)); // 실제 첫 공격 확인
        GameObject screen = new GameObject("Held Attack Modal"); // 시험 화면
        screen.AddComponent(type); // 홀드 중 화면 포커스 획득
        yield return new WaitForSeconds(0.7f); // 일반 공격 간격 경과
        Assert.That(NextAttack(), Is.EqualTo(firstAttack)); // 포커스 중 반복 공격 차단
        Object.Destroy(screen); // 화면 닫기
        yield return null; // 화면 제거 반영
        yield return null; // 남아 있는 홀드 처리
        Assert.That(NextAttack(), Is.EqualTo(firstAttack)); // 화면 닫힌 뒤 기존 홀드 차단
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }); // 마우스 놓기
        yield return null; // 해제 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 별도 새 클릭
        yield return null; // 새 공격 처리 대기
        Assert.That(NextAttack(), Is.GreaterThan(firstAttack)); // 새 클릭 이후 공격 재개
    }

    private void ConfigureFocusSettings(Scene scene, LoadSceneMode mode) // 사용자 설정과 분리된 시험 수치
    {
        if (scene.name != "CombatPrototype" || settings.Current == null) // 전투 씬과 복제 설정 확인
        {
            return; // 다른 씬과 누락된 설정 보존
        }
        settings.Current.playerMana = 50; // 시험 최대 마나
        settings.Current.manaRegen = 3f; // 시험 마나 회복량
        settings.Current.fireballCost = 10; // 시험 화염탄 비용
        settings.Current.playerStamina = 100; // 시험 최대 스태미나
        settings.Current.dodgeCost = 25; // 시험 회피 비용
        settings.Current.interactionRange = 1.5f; // 시험 상호작용 반경
    }

    private static float NextAttack() // 실제 공격에 따른 대기시간 조회
    {
        PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 현재 전투 구성 요소
        return (float)typeof(PrototypeBootstrap).GetField("nextAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle); // 다음 공격 가능 시각
    }
}
