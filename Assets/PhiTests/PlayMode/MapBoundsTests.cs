using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 슬라임 목록 접근
using System.Reflection; // 전투 상태 시험 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 키보드 상태
using UnityEngine.SceneManagement; // 시험 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험

public class MapBoundsTests // 시험 공간 경계 검증
{
    private Keyboard keyboard; // 가상 키보드
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings settings; // 시험용 전투 설정
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 시험 전투 구성
    {
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬 열기
        yield return null; // 전투 생성 대기
        yield return null; // 전투 초기화 대기
        settings = ScriptableObject.CreateInstance<PrototypeSettings>(); // 독립 전투 설정 생성
        settings.slimeSpeed = 0f; // 이동 시험 중 적 정지
        typeof(PrototypeBootstrap).GetField("runtimeSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Object.FindFirstObjectByType<PrototypeBootstrap>(), settings); // 전투 설정 연결
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 가상 장치와 설정 복원
    {
        if (keyboard != null && keyboard.added) // 가상 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
        }
        if (mouse != null && mouse.added) // 가상 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        if (settings != null) // 시험용 설정 존재 여부
        {
            Object.Destroy(settings); // 시험용 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 포커스 처리 복원
        Application.runInBackground = previousRunInBackground; // 실행 설정 복원
        yield return null; // 정리 프레임 대기
    }

    [UnityTest] // 경계 표시 사례
    public IEnumerator BoundaryShapesMatchPlayableArea() // 시험 공간 테두리 위치 검증
    {
        Transform top = GameObject.Find("Boundary Top").transform; // 위쪽 테두리
        Transform bottom = GameObject.Find("Boundary Bottom").transform; // 아래쪽 테두리
        Transform left = GameObject.Find("Boundary Left").transform; // 왼쪽 테두리
        Transform right = GameObject.Find("Boundary Right").transform; // 오른쪽 테두리
        Assert.That(top.position.y + top.localScale.y * 0.5f, Is.EqualTo(6f).Within(0.0001f)); // 위쪽 경계 내부 위치
        Assert.That(bottom.position.y - bottom.localScale.y * 0.5f, Is.EqualTo(-6f).Within(0.0001f)); // 아래쪽 경계 내부 위치
        Assert.That(left.position.x - left.localScale.x * 0.5f, Is.EqualTo(-10f).Within(0.0001f)); // 왼쪽 경계 내부 위치
        Assert.That(right.position.x + right.localScale.x * 0.5f, Is.EqualTo(10f).Within(0.0001f)); // 오른쪽 경계 내부 위치
        yield return null; // 시험 프레임 정리
    }
    [UnityTest] // 플레이어 경계 사례
    public IEnumerator PlayerMovementRemainsInsideEveryEdgeAndDiagonal() // 사방과 대각선 이동 제한
    {
        Transform player = GameObject.Find("Player").transform; // 플레이어 위치
        player.position = new Vector3(9.50f, 5.50f, 0f); // 오른쪽 위 경계 근처
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W)); // 오른쪽 위 이동 입력
        yield return new WaitForSeconds(0.1f); // 이동 반영 대기
        Assert.That(player.position.x, Is.InRange(9.5001f, 9.5301f)); // 실제 오른쪽 이동과 도형 경계 제한
        Assert.That(player.position.y, Is.InRange(5.5001f, 5.5301f)); // 실제 위쪽 이동과 도형 경계 제한
        player.position = new Vector3(-9.50f, -5.50f, 0f); // 왼쪽 아래 경계 근처
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S)); // 왼쪽 아래 이동 입력
        yield return new WaitForSeconds(0.1f); // 이동 반영 대기
        Assert.That(player.position.x, Is.InRange(-9.5301f, -9.5001f)); // 실제 왼쪽 이동과 도형 경계 제한
        Assert.That(player.position.y, Is.InRange(-5.5301f, -5.5001f)); // 실제 아래쪽 이동과 도형 경계 제한
    }

    [UnityTest] // 경계 설정 변경 사례
    public IEnumerator PlayerMovementUsesConfiguredBounds() // 설정된 공간 크기 적용
    {
        FieldInfo width = typeof(PrototypeSettings).GetField("mapHalfWidth"); // 가로 반경 설정 필드
        FieldInfo height = typeof(PrototypeSettings).GetField("mapHalfHeight"); // 세로 반경 설정 필드
        Assert.That(width, Is.Not.Null); // 가로 경계 설정 존재
        Assert.That(height, Is.Not.Null); // 세로 경계 설정 존재
        width.SetValue(settings, 2f); // 가로 반경 변경
        height.SetValue(settings, 1f); // 세로 반경 변경
        Transform player = GameObject.Find("Player").transform; // 플레이어 위치
        player.position = new Vector3(1.50f, 0.50f, 0f); // 변경된 오른쪽 위 경계 근처
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W)); // 대각선 이동 입력
        yield return new WaitForSeconds(0.1f); // 이동 반영 대기
        Assert.That(player.position.x, Is.InRange(1.5001f, 1.5301f)); // 변경된 가로 경계와 실제 이동
        Assert.That(player.position.y, Is.InRange(0.5001f, 0.5301f)); // 변경된 세로 경계와 실제 이동
    }
    [UnityTest] // 카메라 시야 경계 사례
    public IEnumerator CameraViewportRemainsInsideTheMap() // 화면비를 고려한 카메라 범위 검증
    {
        Transform player = GameObject.Find("Player").transform; // 플레이어 위치
        player.position = new Vector3(9f, 5f, 0f); // 오른쪽 위 방향 배치
        yield return null; // 카메라 추적 대기
        Camera camera = Camera.main; // 전투 카메라
        float halfHeight = camera.orthographicSize; // 카메라 세로 반경
        float halfWidth = halfHeight * camera.aspect; // 카메라 가로 반경
        Assert.That(camera.transform.position.x + halfWidth, Is.LessThanOrEqualTo(10.0001f)); // 화면 오른쪽 경계
        Assert.That(camera.transform.position.x - halfWidth, Is.GreaterThanOrEqualTo(-10.0001f)); // 화면 왼쪽 경계
        Assert.That(camera.transform.position.y + halfHeight, Is.LessThanOrEqualTo(6.0001f)); // 화면 위쪽 경계
        Assert.That(camera.transform.position.y - halfHeight, Is.GreaterThanOrEqualTo(-6.0001f)); // 화면 아래쪽 경계
        Assert.That(camera.orthographicSize, Is.GreaterThan(0f)); // 유효한 화면 크기
    }
    [UnityTest] // 넓은 공간 카메라 추적 사례
    public IEnumerator CameraFollowsActualPlayerMovementAwayFromBounds() // 경계와 떨어진 이동의 카메라 추적 검증
    {
        typeof(PrototypeSettings).GetField("mapHalfWidth").SetValue(settings, 20f); // 넓은 가로 반경
        typeof(PrototypeSettings).GetField("mapHalfHeight").SetValue(settings, 12f); // 넓은 세로 반경
        Transform player = GameObject.Find("Player").transform; // 플레이어 위치
        player.position = Vector3.zero; // 중앙 시작 위치
        yield return null; // 카메라 초기 추적 대기
        Camera camera = Camera.main; // 시험 카메라
        Vector3 before = camera.transform.position; // 이동 전 카메라 위치
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W)); // 플레이어 대각 이동 입력
        yield return new WaitForSeconds(0.2f); // 실제 이동과 추적 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 입력 해제
        yield return null; // 이동 종료와 카메라 갱신 대기
        Assert.That(player.position.x, Is.GreaterThan(0.05f)); // 실제 오른쪽 이동
        Assert.That(player.position.y, Is.GreaterThan(0.05f)); // 실제 위쪽 이동
        Assert.That(camera.transform.position.x, Is.GreaterThan(before.x + 0.05f)); // 카메라의 오른쪽 추적
        Assert.That(camera.transform.position.y, Is.GreaterThan(before.y + 0.05f)); // 카메라의 위쪽 추적
        Assert.That(camera.transform.position.x, Is.EqualTo(player.position.x).Within(0.001f)); // 경계 안의 가로 추적 일치
        Assert.That(camera.transform.position.y, Is.EqualTo(player.position.y).Within(0.001f)); // 경계 안의 세로 추적 일치
        Vector3 viewportPosition = camera.WorldToViewportPoint(player.position); // 플레이어의 화면 좌표
        Assert.That(viewportPosition.x, Is.InRange(0f, 1f)); // 화면 안의 가로 위치
        Assert.That(viewportPosition.y, Is.InRange(0f, 1f)); // 화면 안의 세로 위치
    }
    [UnityTest] // 조준과 체력 표시 경계 사례
    public IEnumerator AimAndHealthIndicatorsStayInsideTheBoundary() // 전투 표시의 테두리 이탈 방지
    {
        Transform player = GameObject.Find("Player").transform; // 플레이어 도형
        Transform slime = GameObject.Find("Slime 1").transform; // 슬라임 도형
        player.position = new Vector3(9.53f, 0f, 0f); // 오른쪽 안전 경계의 플레이어
        slime.position = new Vector3(0f, 5.455f, 0f); // 위쪽 안전 경계의 슬라임
        slime.localScale = new Vector3(0.85f, 0.85f, 1f); // 공격 예고 중 최대 도형 크기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width - 1f, Screen.height * 0.5f) }); // 오른쪽 화면 조준
        yield return null; // 조준 방향 갱신 대기
        yield return null; // 표시 위치 갱신 대기
        Transform aim = GameObject.Find("Aim").transform; // 조준 표시 도형
        Assert.That(aim.right.x, Is.GreaterThan(0.99f)); // 마우스 방향과 조준 표시 일치
        Vector3 aimEnd = aim.position + aim.right * (aim.localScale.x * 0.5f); // 조준 표시 끝 좌표
        Assert.That(aimEnd.x, Is.LessThanOrEqualTo(9.8801f)); // 오른쪽 테두리 안의 조준 끝
        Assert.That(aimEnd.y, Is.InRange(-5.8801f, 5.8801f)); // 세로 테두리 안의 조준 끝
        player.position = new Vector3(9.53f, 5.53f, 0f); // 위쪽 안전 경계의 플레이어
        yield return null; // 체력 표시 위치 갱신 대기
        SpriteRenderer playerBar = player.Find("Health Bar").GetComponent<SpriteRenderer>(); // 플레이어 체력 막대
        SpriteRenderer slimeBar = slime.Find("Health Bar").GetComponent<SpriteRenderer>(); // 슬라임 체력 막대
        Assert.That(playerBar.bounds.max.y, Is.LessThanOrEqualTo(5.8801f)); // 위쪽 테두리 안의 플레이어 체력 표시
        Assert.That(slimeBar.bounds.max.y, Is.LessThanOrEqualTo(5.8801f)); // 위쪽 테두리 안의 슬라임 체력 표시
    }
    [UnityTest] // 슬라임 이동 경계 사례
    public IEnumerator SlimeApproachAndDashStopAtBoundaryWithoutHittingBeyondIt() // 접근과 돌진 경로 제한
    {
        PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter slime = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 첫 슬라임 상태
        IList slimes = (IList)typeof(PrototypeBootstrap).GetField("slimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap); // 실제 슬라임 목록
        object state = slimes[0]; // 첫 슬라임 행동 상태
        MethodInfo tick = state.GetType().GetMethod("Tick"); // 실제 슬라임 행동 메서드
        player.transform.position = new Vector3(15f, 0f, 0f); // 경계 밖 시험 목표
        slime.transform.position = new Vector3(9.5f, 0f, 0f); // 오른쪽 경계 근처의 적
        settings.slimeSpeed = 100000f; // 접근 거리 초과 시험 속도
        tick.Invoke(state, new object[] { player, settings }); // 슬라임 접근 처리
        Assert.That(slime.transform.position.x, Is.LessThanOrEqualTo(9.4551f)); // 슬라임 전체 도형의 접근 제한
        player.transform.position = new Vector3(10.8f, 0f, 0f); // 경계 바깥 피해 시험 대상
        slime.transform.position = new Vector3(9f, 0f, 0f); // 돌진 시작 위치
        settings.slimeDashSpeed = 100000f; // 한 프레임 돌진 시험 속도
        settings.slimeHitDistance = 0.1f; // 경계 바깥 피해 구분 거리
        state.GetType().GetField("dashDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, Vector2.right); // 오른쪽 돌진 방향
        state.GetType().GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, 3f); // 경계를 넘는 돌진 거리
        tick.Invoke(state, new object[] { player, settings }); // 슬라임 돌진 처리
        Assert.That(slime.transform.position.x, Is.LessThanOrEqualTo(9.4551f)); // 슬라임 전체 도형의 돌진 제한
        Assert.That(player.Health, Is.EqualTo(100)); // 실제 돌진 경로 밖 피해 차단
        yield return null; // 시험 프레임 정리
    }
}







