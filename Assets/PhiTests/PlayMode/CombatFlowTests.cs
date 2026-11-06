using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // UI 검사 결과 목록
using System.Reflection; // 시험용 설정 연결
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.EventSystems; // UI 포인터 검사
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 마우스 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 재시작 버튼

public class CombatFlowTests // 전투 흐름 검증
{
    private Mouse mouse; // 시험용 마우스
    private PrototypeSettings settings; // 시험용 임시 설정
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험 전 준비
    public IEnumerator SetUp() // 시험 씬 준비
    {
        previousRunInBackground = Application.runInBackground; // 기존 백그라운드 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 에디터 입력 경로 보관
        Application.runInBackground = true; // 비활성 테스트의 프레임 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 장치의 포커스 초기화 방지
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 가상 입력의 게임 경로 지정
        mouse = InputSystem.AddDevice<Mouse>(); // 독립 가상 마우스
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
        yield return null; // 전투 객체 생성 대기
        yield return null; // UI 배치 대기
        settings = ScriptableObject.CreateInstance<PrototypeSettings>(); // 독립 설정 생성
        settings.slimeSpeed = 0f; // 타격 시험 중 대상 위치 유지
        typeof(PrototypeBootstrap).GetField("runtimeSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Object.FindFirstObjectByType<PrototypeBootstrap>(), settings); // 실제 전투에 임시 설정 연결
    }

    [UnityTearDown] // 각 시험 후 정리
    public IEnumerator TearDown() // 가상 입력 장치 제거
    {
        if (mouse != null && mouse.added) // 가상 장치 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 시험 장치 제거
        }
        if (settings != null) // 임시 설정 존재 여부
        {
            Object.Destroy(settings); // 임시 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 에디터 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 포커스 처리 설정 복원
        Application.runInBackground = previousRunInBackground; // 백그라운드 실행 설정 복원
        yield return null; // 프레임 정리 대기
    }

    [UnityTest] // 검 타격과 재시작 사례
    public IEnumerator SwordKillsSlimeAndRestartResetsCombat() // 실제 전투 흐름 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter slime = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 첫 슬라임 전투 상태
        GameObject.Find("Slime 2").transform.position = new Vector3(-20f, 0f, 0f); // 다른 적의 간섭 제거
        GameObject.Find("Slime 3").transform.position = new Vector3(0f, -20f, 0f); // 다른 적의 간섭 제거
        slime.transform.position = new Vector3(1.4f, 0f, 0f); // 검 사거리 안의 대상
        Vector2 screenTarget = Camera.main.WorldToScreenPoint(new Vector3(2f, 0f, 0f)); // 정면 마우스 위치
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenTarget }.WithButton(MouseButton.Left)); // 첫 공격 누름
        yield return null; // 첫 공격 처리 대기
        PointerEventData pointer = new PointerEventData(EventSystem.current); // 포인터 검사 정보
        pointer.position = mouse.position.ReadValue(); // 실제 입력 좌표
        List<RaycastResult> hits = new List<RaycastResult>(); // UI 검사 결과
        EventSystem.current.RaycastAll(pointer, hits); // 입력 좌표의 UI 검사
        PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
        float nextAttack = (float)typeof(PrototypeBootstrap).GetField("nextAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap); // 실제 다음 공격 시각
        string diagnostics = $"screen={Screen.width}x{Screen.height}, pixelRect={Camera.main.pixelRect}, target={screenTarget}, actual={mouse.position.ReadValue()}, current={Mouse.current == mouse}, pressed={mouse.leftButton.isPressed}, pressedFrame={mouse.leftButton.wasPressedThisFrame}, focus={Application.isFocused}, uiHits={hits.Count}, nextAttack={nextAttack}, time={Time.time}"; // 실패 원인 진단 정보
        Assert.That(slime.Health, Is.EqualTo(29), diagnostics); // 첫 공격 피해 21
        yield return new WaitForSeconds(0.60f); // 두 번째 공격 간격
        Assert.That(slime.Health, Is.EqualTo(8)); // 홀드 반복 피해 21
        yield return new WaitForSeconds(0.60f); // 세 번째 공격 간격
        Assert.That(slime.Dead, Is.True); // 세 번째 공격 사망
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenTarget }); // 마우스 버튼 해제
        player.TakeDamage(99); // 적 공격으로 사망할 체력
        GameObject.Find("Slime 2").transform.position = new Vector3(-0.75f, 0f, 0f); // 슬라임 공격 거리 배치
        yield return new WaitForSeconds(0.90f); // 예고와 돌진 대기
        Assert.That(player.Dead, Is.True); // 슬라임 공격으로 플레이어 사망
        Button restart = GameObject.Find("Restart").GetComponent<Button>(); // 실제 재시작 버튼
        restart.onClick.Invoke(); // 버튼에 연결된 재시작 동작
        yield return null; // 새 씬 초기화 대기
        yield return null; // 전투 객체 생성 대기
        Assert.That(GameObject.Find("Player").GetComponent<Fighter>().Health, Is.EqualTo(100)); // 플레이어 체력 초기화
        Assert.That(GameObject.Find("Slime 1").GetComponent<Fighter>().Health, Is.EqualTo(50)); // 슬라임 체력 초기화
    }

    [UnityTest] // UI 클릭 차단 사례
    public IEnumerator UiPressRemainsBlockedWhenDraggedIntoWorld() // UI 클릭과 드래그 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter slime = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 공격 대상 전투 상태
        GameObject.Find("Slime 2").transform.position = new Vector3(-20f, 0f, 0f); // 다른 적의 간섭 제거
        GameObject.Find("Slime 3").transform.position = new Vector3(0f, -20f, 0f); // 다른 적의 간섭 제거
        RectTransform panel = GameObject.Find("Health Panel").GetComponent<RectTransform>(); // 체력 패널 위치
        Vector2 uiPosition = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center)); // 체력 패널 중앙 좌표
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // 화면 중앙 좌표
        Vector2 worldPosition = Vector2.Lerp(screenCenter, uiPosition, 0.45f); // 같은 방향의 UI 바깥 좌표
        Vector3 targetWorld = Camera.main.ScreenToWorldPoint(new Vector3(worldPosition.x, worldPosition.y, 10f)); // 조준점 월드 좌표
        Vector2 direction = ((Vector2)(targetWorld - player.transform.position)).normalized; // 화면 중앙에서 조준 방향
        slime.transform.position = player.transform.position + (Vector3)(direction * 1.4f); // 검 사거리 안의 대상
        InputSystem.QueueStateEvent(mouse, new MouseState { position = uiPosition }); // 마우스의 UI 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = uiPosition }.WithButton(MouseButton.Left)); // UI 위에서 클릭 시작
        yield return null; // UI 클릭 처리 대기
        Assert.That(slime.Health, Is.EqualTo(50)); // UI 클릭의 전투 차단
        InputSystem.QueueStateEvent(mouse, new MouseState { position = worldPosition }.WithButton(MouseButton.Left)); // 누른 채 전투 화면으로 이동
        yield return null; // 드래그 처리 대기
        Assert.That(slime.Health, Is.EqualTo(50)); // UI에서 시작한 드래그 차단
        InputSystem.QueueStateEvent(mouse, new MouseState { position = worldPosition }); // 마우스 버튼 해제
        yield return null; // 버튼 해제 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = worldPosition }.WithButton(MouseButton.Left)); // 전투 화면에서 새 클릭
        yield return null; // 전투 클릭 처리 대기
        Assert.That(slime.Health, Is.EqualTo(29)); // UI 바깥 클릭의 정상 공격
    }

    [UnityTest] // 마우스 재시작 사례
    public IEnumerator RestartButtonRespondsToMouseAndResetsCombat() // 실제 UI 클릭 재시작 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        GameObject.Find("Slime 1").transform.position = new Vector3(0.75f, 0f, 0f); // 공격할 슬라임 배치
        GameObject.Find("Slime 2").transform.position = new Vector3(-20f, 0f, 0f); // 다른 적의 간섭 제거
        GameObject.Find("Slime 3").transform.position = new Vector3(0f, -20f, 0f); // 다른 적의 간섭 제거
        player.TakeDamage(99); // 한 번의 적 공격으로 사망할 체력
        yield return new WaitForSeconds(0.90f); // 슬라임 예고와 공격 대기
        Assert.That(player.Dead, Is.True); // 적 공격으로 플레이어 사망
        RectTransform button = GameObject.Find("Restart").GetComponent<RectTransform>(); // 표시된 재시작 버튼
        Vector2 buttonPosition = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center)); // 버튼 중앙 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition }); // 버튼으로 마우스 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition }.WithButton(MouseButton.Left)); // 버튼 누름
        yield return null; // 버튼 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = buttonPosition }); // 버튼 놓기
        yield return null; // 버튼 클릭 처리 대기
        yield return null; // 재시작 씬 초기화 대기
        Fighter restartedPlayer = GameObject.Find("Player").GetComponent<Fighter>(); // 재생성된 플레이어
        Assert.That(restartedPlayer, Is.Not.SameAs(player)); // 새 전투 객체 생성
        Assert.That(restartedPlayer.Health, Is.EqualTo(100)); // 플레이어 체력 초기화
        Assert.That(GameObject.Find("Slime 1").GetComponent<Fighter>().Health, Is.EqualTo(50)); // 슬라임 체력 초기화
    }
}
