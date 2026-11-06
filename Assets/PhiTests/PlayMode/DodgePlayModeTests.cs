using System; // 자료형 변환
using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 장애물 목록
using System.Reflection; // 설정과 실행 상태 확인
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.EventSystems; // 선택된 UI 확인
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 장착 상태와 자원 표시
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class DodgePlayModeTests // 실제 회피 입력과 전투 검증
{
    private Keyboard keyboard; // 가상 키보드
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings trial; // 독립 시험 설정
    private PrototypeBootstrap bootstrap; // 전투 구성 요소
    private SceneMapTestFixture mapFixture; // 시험용 씬 맵 인스턴스
    private UnityAction<Scene, LoadSceneMode> sceneCallback; // 독립 설정 연결 콜백
    private readonly List<GameObject> obstacles = new List<GameObject>(); // 시험 장애물 목록
    private bool previousRunInBackground; // 기존 실행 설정
    private float previousCaptureDeltaTime; // 기존 프레임 시간 고정값
    private float previousContactOffset; // 기존 물리 접촉 여유
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 입력 경로
    private float trialCooldown; // 시험용 회피 대기시간

    [UnitySetUp] // 각 시험 준비
    public IEnumerator SetUp() // 독립 맵과 가상 입력 준비
    {
        trialCooldown = 1.2f; // 각 시험의 기본 회피 대기시간 복원
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousCaptureDeltaTime = Time.captureDeltaTime; // 기존 프레임 시간 고정값 보관
        previousContactOffset = Physics2D.defaultContactOffset; // 기존 물리 접촉 여유 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        mapFixture = new SceneMapTestFixture(); // 씬 맵의 독립 시험 배치
        yield return LoadBattle(); // 독립 설정으로 전투 시작
        PlaceSlime(1, new Vector2(-4f, 3f)); // 첫 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 4f)); // 두 번째 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -3f)); // 세 번째 적의 간섭 제거
    }

    [UnityTearDown] // 각 시험 정리
    public IEnumerator TearDown() // 시험 자원과 전역 설정 복원
    {
        if (sceneCallback != null) // 씬 로드 콜백 존재 여부
        {
            SceneManager.sceneLoaded -= sceneCallback; // 남은 시험 콜백 해제
        }
        if (keyboard != null && keyboard.added) // 가상 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
        }
        if (mouse != null && mouse.added) // 가상 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        foreach (GameObject obstacle in obstacles) // 시험 장애물 순회
        {
            if (obstacle != null) // 시험 장애물 존재 여부
            {
                Object.Destroy(obstacle); // 시험 장애물 제거
            }
        }
        if (mapFixture != null) // 맵 시험 격리 존재 여부
        {
            mapFixture.Dispose(); // 맵 이벤트와 그림 정리
        }
        if (trial != null) // 독립 설정 존재 여부
        {
            Object.Destroy(trial); // 시험 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        Time.captureDeltaTime = previousCaptureDeltaTime; // 기존 프레임 시간 고정값 복원
        Physics2D.defaultContactOffset = previousContactOffset; // 기존 물리 접촉 여유 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 대각선 회피 사례
    public IEnumerator DiagonalDodgeUsesNormalizedMovementDirection() // 이동 입력과 회피 거리 검증
    {
        Vector3 start = Player().transform.position; // 회피 전 플레이어 위치
        Press(Key.W, Key.D, Key.LeftShift); // 같은 프레임의 대각선 이동과 회피
        yield return null; // 회피 시작 대기
        Press(); // 이동과 회피 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 이동 종료 대기
        Vector2 delta = Player().transform.position - start; // 실제 회피 이동량
        Assert.That(delta.magnitude, Is.EqualTo(2f).Within(0.1f)); // 대각선 회피 거리 정규화
        Assert.That(delta.x, Is.EqualTo(delta.y).Within(0.05f)); // 대각선 두 축의 이동 비율
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.1f)); // 한 번의 회피 비용
    }

    [UnityTest] // 이동 입력 없는 조준 반대 사례
    public IEnumerator StationaryDodgeUsesLatestMouseAimOppositeDirection() // 같은 프레임 조준과 회피 방향 검증
    {
        yield return AimAt(new Vector2(4f, 0f)); // 이전 오른쪽 조준
        Vector3 start = Player().transform.position; // 회피 전 위치
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(0f, 4f, 0f)); // 변경할 위쪽 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 같은 프레임의 위쪽 조준
        Press(Key.RightShift); // 이동 입력 없는 오른쪽 Shift
        yield return null; // 새 조준과 회피 처리 대기
        Press(); // Shift 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Vector2 delta = Player().transform.position - start; // 회피 이동량
        Assert.That(delta.y, Is.EqualTo(-2f).Within(0.12f)); // 최신 위쪽 조준의 아래 방향
        Assert.That(delta.x, Is.EqualTo(0f).Within(0.1f)); // 이전 오른쪽 조준 무시
    }

    [UnityTest] // Shift 홀드 사례
    public IEnumerator HoldingShiftDoesNotRepeatDodge() // 긴 누름의 단일 비용 검증
    {
        yield return AimAt(new Vector2(-4f, 0f)); // 오른쪽 회피를 위한 조준
        Press(Key.LeftShift); // Shift 누름 유지
        yield return new WaitForSeconds(1.3f); // 쿨다운이 끝날 때까지 대기
        Assert.That(Stamina(), Is.EqualTo(75f).Within(1.5f)); // 한 번의 비용과 초기 재생만 적용
        Assert.That(Player().transform.position.x, Is.EqualTo(2f).Within(0.1f)); // 한 번의 회피 거리
    }

    [UnityTest] // UI 편집과 버튼 선택 사례
    public IEnumerator TextEditingBlocksDodgeButSelectedButtonAllowsIt() // 실제 텍스트 편집 포커스 구분
    {
        GameObject fieldObject = new GameObject("Dodge Test Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 시험용 입력 필드
        fieldObject.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform, false); // 기존 캔버스의 입력 필드
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 입력 문자열 표시
        textObject.transform.SetParent(fieldObject.transform, false); // 입력 필드의 자식 글자
        Text text = textObject.GetComponent<Text>(); // 문자열 표시 구성 요소
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 시험용 기본 글꼴
        InputField input = fieldObject.GetComponent<InputField>(); // 시험 입력 필드
        input.textComponent = text; // 입력과 표시 글자 연결
        obstacles.Add(fieldObject); // 시험 후 입력 필드 정리
        EventSystem.current.SetSelectedGameObject(fieldObject); // 입력 필드 선택
        input.ActivateInputField(); // 실제 텍스트 편집 시작
        yield return null; // 입력 필드 포커스 반영 대기
        yield return null; // 입력 필드 활성화 대기
        Assert.That(input.isFocused, Is.True); // 실제 텍스트 편집 상태 확인
        Press(Key.LeftShift); // 편집 중 회피 키
        yield return null; // 입력 처리 대기
        Assert.That(Stamina(), Is.EqualTo(100f)); // 편집 중 회피 비용 보존
        Press(); // 키 해제
        input.DeactivateInputField(); // 텍스트 편집 종료
        yield return null; // 편집 종료와 키 해제 반영
        Assert.That(input.isFocused, Is.False); // 실제 편집 포커스 해제 확인
        EventSystem.current.SetSelectedGameObject(GameObject.Find("Equip Bow")); // 일반 버튼 선택
        Press(Key.LeftShift); // 버튼 선택 상태에서 회피 키
        yield return null; // 입력 처리 대기
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.1f)); // 일반 버튼 선택 중 회피 허용
    }

    [UnityTest] // 공통 피해와 무적 사례
    public IEnumerator DodgeBlocksDamageOnlyDuringInvulnerability() // 회피 전·중·후 피해 검증
    {
        Fighter player = Player(); // 플레이어 전투 상태
        player.TakeDamage(10); // 회피 전 공통 피해
        Assert.That(player.Health, Is.EqualTo(90)); // 회피 전 정상 피해
        Press(Key.LeftShift); // 회피 시작 입력
        yield return null; // 회피 시작 처리 대기
        Assert.That(Invulnerable(player), Is.True); // 무적 상태 시작 확인
        player.TakeDamage(10); // 회피 중 공통 피해
        Assert.That(player.Health, Is.EqualTo(90)); // 무적 구간의 피해 차단
        Press(); // 회피 키 해제
        yield return new WaitForSeconds(0.35f); // 무적 종료 대기
        Assert.That(Invulnerable(player), Is.False); // 무적 상태 종료 확인
        player.TakeDamage(10); // 회피 후 공통 피해
        Assert.That(player.Health, Is.EqualTo(80)); // 회피 후 정상 피해 복원
    }

    [UnityTest] // 긴 발동 프레임과 후속 프레임 사례
    public IEnumerator DodgeDoesNotConsumePreviousLongFrameBeforeItStarts() // 입력 직전 시간과 회피 이후 시간 구분
    {
        Time.captureDeltaTime = 0.5f; // 회피 지속시간보다 긴 결정적 프레임 시간
        yield return null; // 시간 고정값을 이전 프레임에 적용
        Fighter player = Player(); // 플레이어 전투 상태
        Press(Key.D, Key.LeftShift); // 긴 프레임의 끝에서 회피 시작
        yield return null; // 회피 시작 프레임 처리
        Assert.That(Invulnerable(player), Is.True); // 발동 전 경과 시간으로 무적 종료 방지
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.01f)); // 발동 프레임의 회피 비용
        player.TakeDamage(10); // 회피 시작 프레임의 공통 피해
        Assert.That(player.Health, Is.EqualTo(100)); // 회피 시작 프레임의 무적 적용
        Press(); // 이동과 회피 키 해제
        yield return null; // 회피 이후의 긴 프레임 처리
        Assert.That(Invulnerable(player), Is.False); // 후속 프레임의 정상 무적 종료
        Assert.That(player.transform.position.x, Is.EqualTo(2f).Within(0.1f)); // 지속시간으로 제한된 총 회피 거리
        player.TakeDamage(10); // 무적 종료 후 공통 피해
        Assert.That(player.Health, Is.EqualTo(90)); // 종료 후 피해 허용
    }

    [UnityTest] // 이전 회피 종료 프레임의 새 회피 사례
    public IEnumerator DodgeCanRestartOnEndingFrameWhenCooldownHasExpired() // 종료 경계에서의 새 회피와 무적 유지
    {
        PrototypeSettings previous = trial; // 이전 시험 설정
        trialCooldown = 0.1f; // 이동보다 짧은 시험 대기시간
        yield return LoadBattle(); // 짧은 대기시간으로 독립 전투 재시작
        Object.Destroy(previous); // 이전 시험 설정 제거
        Time.captureDeltaTime = 0.3f; // 회피 이동보다 긴 결정적 프레임 시간
        yield return null; // 시간 고정값 적용 대기
        Press(Key.D, Key.LeftShift); // 첫 번째 오른쪽 회피
        yield return null; // 첫 회피 시작 프레임
        Press(Key.D); // Shift 해제 이벤트
        Press(Key.D, Key.LeftShift); // 이전 회피 종료 프레임의 새 Shift 입력
        yield return null; // 첫 회피 종료와 두 번째 회피 시작
        Assert.That(Stamina(), Is.EqualTo(50f).Within(0.01f)); // 두 회피 비용 차감
        Assert.That(Invulnerable(Player()), Is.True); // 새 회피의 무적 유지
        Assert.That(Player().transform.position.x, Is.EqualTo(2f).Within(0.1f)); // 첫 회피 이동 완료
        Press(); // 회피와 이동 키 해제
        yield return null; // 두 번째 회피 이동 종료
        Assert.That(Player().transform.position.x, Is.EqualTo(4f).Within(0.1f)); // 두 번째 회피의 이동 유지
    }

    [UnityTest] // 슬라임 실제 돌진 사례
    public IEnumerator SlimeDashRespectsDodgeInvulnerabilityAndHitsAfterwards() // 적 행동에서 공통 무적 경로 연결 검증
    {
        Fighter player = Player(); // 플레이어 전투 상태
        Fighter slime = PlaceSlime(1, new Vector2(-0.5f, 0f)); // 가까운 시험 슬라임
        Press(Key.LeftShift); // 회피 시작 입력
        yield return null; // 회피 무적 상태 반영
        Assert.That(Invulnerable(player), Is.True); // 무적 시작 확인
        ForceSlimeDash(slime, player); // 무적 중 실제 슬라임 돌진 처리
        Assert.That(player.Health, Is.EqualTo(100)); // 슬라임 돌진의 피해 차단
        Press(); // 회피 키 해제
        yield return new WaitForSeconds(0.35f); // 회피와 무적 종료 대기
        Assert.That(Invulnerable(player), Is.False); // 무적 종료 확인
        ForceSlimeDash(slime, player); // 무적 종료 후 새 슬라임 돌진
        Assert.That(player.Health, Is.EqualTo(95)); // 공격력 8과 방어력 5의 정상 피해
    }

    [UnityTest] // 씬 경계 사례
    public IEnumerator DodgeRespectsExistingSceneBoundary() // 실제 맵 벽과 플레이어 크기 제한
    {
        Fighter player = Player(); // 플레이어 전투 상태
        player.transform.position = new Vector3(9.4f, 0f, 0f); // 오른쪽 맵 경계 근처
        Press(Key.D, Key.LeftShift); // 맵 바깥을 향한 회피
        yield return null; // 회피 시작 대기
        Press(); // 입력 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(9.5301f)); // 플레이어 전체의 경계 이탈 방지
        Assert.That(GameObject.Find("Boundary Right"), Is.Not.Null); // 저장된 씬 경계 유지
    }

    [UnityTest] // 내부 벽 사례
    public IEnumerator DodgeStopsAtNonTriggerObstacleWithoutTunneling() // 회피 이동 경로의 벽 충돌 검증
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 이동 경로의 얇은 벽
        Press(Key.D, Key.LeftShift); // 벽 방향 회피
        yield return null; // 회피 시작 대기
        Press(); // 입력 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.61f)); // 플레이어 사각형과 벽의 겹침 방지
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.1f)); // 벽 충돌에도 회피 비용 유지
    }

    [UnityTest] // 긴 이동 프레임의 벽 충돌 사례
    public IEnumerator LongDodgeFrameCannotTunnelThroughThinWall() // 한 번에 전 거리를 이동하는 벽 판정
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 이동 경로의 얇은 벽
        Time.captureDeltaTime = 0.5f; // 회피 전체보다 긴 결정적 프레임 시간
        yield return null; // 시간 고정값 적용 대기
        Press(Key.D, Key.LeftShift); // 벽을 향한 회피 시작
        yield return null; // 회피 시작 프레임
        Press(); // 회피 키 해제
        yield return null; // 회피 전체를 포함하는 후속 프레임
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.61f)); // 긴 프레임에서도 벽 통과 방지
    }

    [UnityTest] // 대각선 플레이어 모서리 사례
    public IEnumerator DiagonalDodgeStopsWhenPlayerCornerReachesSmallWall() // 실제 사각 도형의 연속 충돌 검증
    {
        CreateObstacle(new Vector2(0.8f, 0.6f), new Vector2(0.1f, 0.1f), false); // 대각선 경로 옆의 작은 벽
        Press(Key.W, Key.D, Key.LeftShift); // 벽을 향한 대각선 회피
        yield return null; // 회피 시작 처리
        Press(); // 이동과 회피 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.405f)); // 사각형 오른쪽 모서리의 벽 통과 방지
        Assert.That(Player().transform.position.y, Is.LessThanOrEqualTo(0.405f)); // 대각선 이동의 벽 충돌 지점
    }

    [UnityTest] // 벽 접촉 후 반대 방향 사례
    public IEnumerator PlayerCanDodgeAwayAfterTouchingWall() // 단순 접촉으로 인한 영구 정지 방지
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 첫 회피를 막는 벽
        Press(Key.D, Key.LeftShift); // 벽을 향한 회피 시작
        yield return null; // 회피 입력 대기
        Press(); // 모든 키 해제
        yield return new WaitForSeconds(1.25f); // 회피와 대기시간 종료 대기
        float contact = Player().transform.position.x; // 벽 앞의 플레이어 위치
        Assert.That(contact, Is.LessThanOrEqualTo(0.61f)); // 첫 회피의 벽 충돌 확인
        Press(Key.A, Key.LeftShift); // 벽에서 멀어지는 회피 시작
        yield return null; // 두 번째 회피 입력 처리
        Press(); // 두 번째 입력 해제
        yield return new WaitForSeconds(0.35f); // 두 번째 회피 종료 대기
        Assert.That(Stamina(), Is.LessThan(55f)); // 두 번째 회피 입력의 비용 차감 확인
        Assert.That(Player().transform.position.x, Is.LessThan(contact - 1.8f)); // 접촉에서 벗어나는 회피 허용
    }

    [UnityTest] // 기하학적 접촉점의 벽 반대 방향 사례
    public IEnumerator PlayerCanDodgeAwayFromExactWallContact() // 정확한 벽 접촉과 실제 겹침 구분
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 오른쪽 장애물
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 사각 도형이 벽에 정확히 접하는 위치
        Physics2D.SyncTransforms(); // 접촉 좌표의 물리 반영
        Press(Key.A, Key.LeftShift); // 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThan(-1.2f)); // 단순 접촉에서 벗어나는 이동 허용
    }

    [UnityTest] // 기하학적 접촉점의 벽 방향 사례
    public IEnumerator PlayerCannotDodgeIntoWallFromExactContact() // 접촉 상태에서 벽 관통 방지
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 오른쪽 장애물
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 사각 도형이 벽에 정확히 접하는 위치
        Physics2D.SyncTransforms(); // 접촉 좌표의 물리 반영
        Press(Key.D, Key.LeftShift); // 접촉한 벽 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.6001f)); // 접촉한 벽 안쪽 이동 방지
    }

    [UnityTest] // 미세한 실제 침투 사례
    public IEnumerator SlightGeometricOverlapStillBlocksDodge() // 접촉 여유 보정 이후 실제 겹침 확인
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 오른쪽 사각 벽
        Player().transform.position = new Vector3(0.605f, 0f, 0f); // 벽 안으로 0.005 침투한 시작점
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 실제 겹침에서 벗어나는 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(0.605f).Within(0.001f)); // 실제 시작 겹침의 이동 차단
    }

    [UnityTest] // 원형 벽의 정확한 접촉 사례
    public IEnumerator PlayerCanDodgeAwayFromCircleContact() // 원형 장애물의 접촉 탈출 검증
    {
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 왼쪽 끝 x=0.95인 원형 벽
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 원형 벽에 정확히 닿는 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 원형 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThan(-1.2f)); // 원형 벽 접촉에서 탈출 허용
    }

    [UnityTest] // 원형 벽의 접근 차단 사례
    public IEnumerator PlayerCannotDodgeIntoCircleFromContact() // 접촉한 원형 장애물 관통 방지
    {
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 왼쪽 끝 x=0.95인 원형 벽
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 원형 벽에 정확히 닿는 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.D, Key.LeftShift); // 원형 벽 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return null; // 첫 회피 이동 처리
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.6001f)); // 원형 벽 안쪽 이동 방지
    }

    [UnityTest] // 원형 벽의 작은 양수 간격 사례
    public IEnumerator DodgeCannotEnterCircleFromSmallPositiveGap() // 접촉 이전 짧은 거리의 원형 벽 충돌
    {
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 왼쪽 끝 x=0.95인 원형 벽
        Player().transform.position = new Vector3(0.599f, 0f, 0f); // 원형 벽과 0.001 간격인 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.D, Key.LeftShift); // 원형 벽 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThanOrEqualTo(0.6001f)); // 실제 접촉 지점을 넘는 이동 방지
    }

    [UnityTest] // 원형 벽의 미세한 실제 침투 사례
    public IEnumerator SlightCircleOverlapStillBlocksDodge() // 원형 접촉 여유 보정의 실제 겹침 검증
    {
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 왼쪽 끝 x=0.95인 원형 벽
        Player().transform.position = new Vector3(0.605f, 0f, 0f); // 원형 벽 안으로 0.005 침투
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 실제 겹침에서 벗어나는 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(0.605f).Within(0.001f)); // 실제 시작 겹침의 이동 차단
    }

    [UnityTest] // 큰 접촉 여유의 사각 벽 사례
    public IEnumerator BoxContactAllowsEscapeWithChangedGlobalOffset() // 전역 접촉 여유 변경 시 정확 접촉 검증
    {
        Physics2D.defaultContactOffset = 0.02f; // 더 큰 전역 접촉 여유
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 변경된 설정의 사각 벽
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 벽에 정확히 접하는 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThan(-1.2f)); // 변경된 여유에서도 접촉 탈출 허용
    }

    [UnityTest] // 큰 접촉 여유의 사각 벽 미세 침투 사례
    public IEnumerator BoxOverlapIsBlockedWithChangedGlobalOffset() // 전역 접촉 여유 변경 시 실제 겹침 검증
    {
        Physics2D.defaultContactOffset = 0.02f; // 더 큰 전역 접촉 여유
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), false); // 변경된 설정의 사각 벽
        Player().transform.position = new Vector3(0.605f, 0f, 0f); // 벽 안으로 0.005 침투한 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(0.605f).Within(0.001f)); // 변경된 여유에서도 실제 겹침 차단
    }

    [UnityTest] // 큰 접촉 여유의 원형 벽 사례
    public IEnumerator CircleContactAllowsEscapeWithChangedGlobalOffset() // 전역 접촉 여유 변경 시 원형 접촉 검증
    {
        Physics2D.defaultContactOffset = 0.02f; // 더 큰 전역 접촉 여유
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 변경된 설정의 원형 벽
        Player().transform.position = new Vector3(0.6f, 0f, 0f); // 원형 벽에 정확히 접하는 위치
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.LessThan(-1.2f)); // 변경된 여유에서도 원형 접촉 탈출 허용
    }

    [UnityTest] // 큰 접촉 여유의 원형 벽 미세 침투 사례
    public IEnumerator CircleOverlapIsBlockedWithChangedGlobalOffset() // 전역 접촉 여유 변경 시 원형 실제 겹침 검증
    {
        Physics2D.defaultContactOffset = 0.02f; // 더 큰 전역 접촉 여유
        CreateCircleObstacle(new Vector2(1.45f, 0f), 0.5f); // 변경된 설정의 원형 벽
        Player().transform.position = new Vector3(0.605f, 0f, 0f); // 원형 벽 안으로 0.005 침투
        Physics2D.SyncTransforms(); // 시험 좌표 물리 반영
        Press(Key.A, Key.LeftShift); // 벽 반대 방향 회피
        yield return null; // 회피 시작 처리
        Press(); // 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(0.605f).Within(0.001f)); // 변경된 여유에서도 원형 실제 겹침 차단
    }

    [UnityTest] // 트리거 장애물 사례
    public IEnumerator DodgeIgnoresTriggerObstacle() // 비차단 트리거의 통과 검증
    {
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 2f), true); // 이동 경로의 트리거
        Press(Key.D, Key.LeftShift); // 트리거 방향 회피
        yield return null; // 회피 시작 대기
        Press(); // 입력 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(2f).Within(0.1f)); // 트리거를 통과한 회피 거리
    }

    [UnityTest] // 출발점 겹침 사례
    public IEnumerator DodgeDoesNotTeleportOutOfOverlappingObstacle() // 벽 안에서 시작한 회피 차단
    {
        CreateObstacle(Vector2.zero, new Vector2(0.2f, 0.2f), false); // 플레이어와 겹치는 장애물
        Press(Key.D, Key.LeftShift); // 장애물과 겹친 시작점의 회피
        yield return null; // 회피 시작 대기
        Press(); // 입력 해제
        yield return new WaitForSeconds(0.35f); // 회피 종료 대기
        Assert.That(Player().transform.position.x, Is.EqualTo(0f).Within(0.01f)); // 겹친 출발점의 순간 이동 방지
    }

    [UnityTest] // 동시 회피와 공격 사례
    public IEnumerator DodgeTakesPriorityAndShortAttackDoesNotFireAfterwards() // 같은 프레임 스킬·공격 취소 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(1f, 0f)); // 검 사거리 안의 적
        yield return AimAt(new Vector2(4f, 0f)); // 적 방향 조준
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 공격 방향의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 짧은 좌클릭 시작
        Press(Key.LeftShift, Key.Q); // 같은 프레임의 회피와 스킬
        yield return null; // 회피 우선순위 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 공격 버튼 해제
        Press(); // 회피와 스킬 키 해제
        yield return new WaitForSeconds(0.4f); // 회피 및 추가 공격 처리 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 취소된 검 공격의 지연 발사 방지
        Assert.That(SkillMana(), Is.EqualTo(50f).Within(0.1f)); // 동시 스킬의 마나 보존
        Assert.That(GameObject.Find("Fireball"), Is.Null); // 동시 스킬 투사체 미생성
    }

    [UnityTest] // 회피 중 홀드 공격 사례
    public IEnumerator HeldAttackResumesAfterDodgeWithoutHittingDuringIt() // 회피 종료 후 유지 입력 처리
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 회피 도착 후 검 범위의 적
        yield return AimAt(new Vector2(4f, 0f)); // 적 방향 조준
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 공격 방향 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 공격 버튼 유지 시작
        Press(Key.D, Key.LeftShift); // 적을 향한 회피 시작
        yield return null; // 회피 시작 처리
        Press(); // 회피와 이동 키만 해제
        yield return new WaitForSeconds(0.12f); // 회피 중간 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 회피 중 홀드 공격 차단
        yield return new WaitForSeconds(0.25f); // 회피 종료 후 공격 대기
        Assert.That(target.Health, Is.EqualTo(29)); // 유지한 공격의 종료 후 정상 발동
    }

    [UnityTest] // 회피 전 발사된 투사체 사례
    public IEnumerator ExistingFireballContinuesDuringDodge() // 이전 투사체와 스킬 대기시간 보존
    {
        SetNumber("fireballSpeed", 1f); // 회피 동안 남아 있는 느린 화염탄
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 화염탄의 첫 적
        yield return AimAt(new Vector2(4f, 0f)); // 적 방향 조준
        Press(Key.Q); // 회피 전 화염탄 시전
        yield return null; // 화염탄 발사 처리
        Assert.That(GameObject.Find("Fireball"), Is.Not.Null); // 회피 전 투사체 존재 확인
        Assert.That(SkillCooldown(0), Is.GreaterThan(4f)); // 기존 스킬 대기시간 시작 확인
        Press(Key.LeftShift); // 화염탄 비행 중 회피
        yield return null; // 회피 시작 처리
        Assert.That(GameObject.Find("Fireball"), Is.Not.Null); // 회피 중 투사체 보존
        Assert.That(SkillCooldown(0), Is.GreaterThan(4f)); // 회피 중 기존 스킬 대기시간 보존
        Press(); // 키 해제
        yield return new WaitForSeconds(2.2f); // 느린 화염탄의 적중 대기
        Assert.That(target.Health, Is.EqualTo(21)); // 회피와 무관한 기존 화염탄 피해
    }

    [UnityTest] // 사망과 재시작 사례
    public IEnumerator DeathAndRestartResetDodgeAndStamina() // 사망 중 입력과 새 전투 상태 검증
    {
        Press(Key.LeftShift); // 회피 시작
        yield return null; // 회피 처리 대기
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.1f)); // 재시작 전 회피 비용
        Press(); // 회피 키 해제
        yield return new WaitForSeconds(0.35f); // 무적 종료 대기
        Player().TakeDamage(100); // 플레이어 사망
        Press(Key.LeftShift); // 사망 중 회피 입력
        yield return null; // 사망 및 입력 처리
        Assert.That(Stamina(), Is.EqualTo(75f).Within(0.1f)); // 사망 중 스태미나 보존
        Press(); // 입력 해제
        PrototypeSettings previous = trial; // 이전 시험 설정
        yield return LoadBattle(); // 동일 조건의 새 전투
        Object.Destroy(previous); // 이전 시험 설정 제거
        Assert.That(Stamina(), Is.EqualTo(100f)); // 스태미나 초기화
        Assert.That(DodgeValue("RemainingCooldown"), Is.EqualTo(0f)); // 이전 회피 대기시간 초기화
        Assert.That(Invulnerable(Player()), Is.False); // 새 전투의 무적 해제
    }

    private IEnumerator LoadBattle() // 독립 설정으로 전투 씬 로드
    {
        sceneCallback = (scene, mode) => // 씬 초기화 전 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 씬의 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 전투 설정 참조
            PrototypeSettings asset = (PrototypeSettings)field.GetValue(bootstrap); // 원본 사용자 설정 에셋
            trial = Object.Instantiate(asset); // 원본을 보존할 독립 설정
            trial.skeletonCount = 0; // 궁수 간섭 제외
            trial.orcCount = 0; // 오크 간섭 제외
            trial.wolfCount = 0; // 늑대 간섭 제외
            trial.playerHealth = 100; // 시험 플레이어 체력
            trial.playerAttack = 10; // 시험 기본 공격력
            trial.playerDefense = 5; // 시험 플레이어 방어력
            trial.swordAttack = 12; // 시험 검 공격력
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.slimeCount = 3; // 시험 적 수
            trial.slimeHealth = 50; // 시험 적 체력
            trial.slimeDefense = 2; // 시험 적 방어력
            trial.slimeAttack = 8; // 시험 슬라임 공격력
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            SkillSettingsTestFixture.UseRecommended(trial); // 유효한 시험 스킬 조합
            SetNumber("playerStamina", 100f); // 시험 최대 스태미나
            SetNumber("staminaRegen", 15f); // 시험 초당 스태미나 회복
            SetNumber("staminaRegenDelay", 1f); // 시험 회복 대기시간
            SetNumber("dodgeCost", 25f); // 시험 회피 비용
            SetNumber("dodgeCooldown", trialCooldown); // 시험 회피 대기시간
            SetNumber("dodgeDuration", 0.25f); // 시험 회피 이동 시간
            SetNumber("dodgeDistance", 2f); // 시험 회피 이동 거리
            SetNumber("dodgeInvulnerability", 0.25f); // 시험 무적 시간
            field.SetValue(bootstrap, trial); // 시작 전 독립 설정 연결
        };
        SceneManager.sceneLoaded += sceneCallback; // 시험 설정 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= sceneCallback; // 시험 설정 콜백 해제
        sceneCallback = null; // 중복 해제 방지
    }

    private void SetNumber(string name, float value) // 시험 설정의 수치 변경
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 수치 설정 필드
        if (field != null) // 구현 전 시험과 이후 설정 구분
        {
            field.SetValue(trial, Convert.ChangeType(value, field.FieldType)); // 독립 설정에 시험 수치 적용
        }
    }

    private static Fighter Player() // 현재 플레이어 전투 상태
    {
        return GameObject.Find("Player").GetComponent<Fighter>(); // 시험 씬의 플레이어
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적 체력 상태
    }

    private void CreateObstacle(Vector2 position, Vector2 size, bool trigger) // 시험 장애물 생성
    {
        GameObject wall = new GameObject("Dodge Test Wall"); // 시험 장애물 오브젝트
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = position; // 시험 벽의 월드 위치
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>(); // 시험 충돌체
        collider.size = size; // 시험 벽 크기
        collider.isTrigger = trigger; // 트리거 여부
        obstacles.Add(wall); // 시험 후 정리 대상 등록
        Physics2D.SyncTransforms(); // 물리 검사에 위치 반영
    }

    private void CreateCircleObstacle(Vector2 position, float radius) // 원형 시험 장애물 생성
    {
        GameObject wall = new GameObject("Dodge Test Circle"); // 원형 시험 장애물 오브젝트
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = position; // 시험 원의 월드 위치
        CircleCollider2D collider = wall.AddComponent<CircleCollider2D>(); // 원형 시험 충돌체
        collider.radius = radius; // 원형 장애물 반지름
        obstacles.Add(wall); // 시험 후 정리 대상 등록
        Physics2D.SyncTransforms(); // 물리 검사에 위치 반영
    }

    private void ForceSlimeDash(Fighter slime, Fighter player) // 실제 슬라임 돌진 처리 호출
    {
        IList slimes = (IList)typeof(PrototypeBootstrap).GetField("slimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap); // 실제 슬라임 상태 목록
        object state = slimes[0]; // 첫 슬라임의 행동 상태
        Type type = state.GetType(); // 슬라임 행동 자료형
        slime.transform.position = player.transform.position + Vector3.left * 0.5f; // 플레이어 앞의 돌진 출발점
        type.GetField("dashDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, Vector2.right); // 플레이어 방향 돌진
        type.GetField("dashRemaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, 1f); // 충분한 돌진 거리
        type.GetField("hitDuringDash", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, false); // 새 돌진의 적중 상태 초기화
        trial.slimeDashSpeed = 10000f; // 한 번의 처리로 지나는 돌진 속도
        trial.slimeHitDistance = 1f; // 안정적인 시험 적중 거리
        object map = typeof(PrototypeBootstrap).GetField("mapRegion", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap); // 실제 씬 경계 상태
        type.GetMethod("Tick").Invoke(state, new object[] { player, trial, map }); // 실제 슬라임 행동 갱신
    }

    private IEnumerator AimAt(Vector2 world) // 화면 포인터로 시험 위치 조준
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 가상 마우스 이동
        yield return null; // 조준 입력 반영 대기
    }

    private void Press(params Key[] keys) // 지정된 키의 동시 누름
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 가상 키 입력
    }

    private object DodgeState() // 현재 회피 실행 상태
    {
        PropertyInfo property = typeof(PrototypeBootstrap).GetProperty("DodgeState"); // 공개 읽기 속성
        Assert.That(property, Is.Not.Null, "전투 회피 상태가 필요합니다."); // 실행 상태 속성 확인
        object value = property.GetValue(bootstrap); // 현재 회피 상태
        Assert.That(value, Is.Not.Null); // 초기화된 상태 확인
        return value; // 확인된 회피 상태
    }

    private float DodgeValue(string name) // 회피 실행 상태의 숫자 값
    {
        object state = DodgeState(); // 현재 회피 상태
        PropertyInfo property = state.GetType().GetProperty(name); // 지정된 읽기 속성
        Assert.That(property, Is.Not.Null); // 읽기 속성 확인
        return Convert.ToSingle(property.GetValue(state)); // 숫자 상태 값
    }

    private float Stamina() // 현재 스태미나 값
    {
        return DodgeValue("Stamina"); // 회피 상태의 현재 자원
    }

    private float SkillMana() // 현재 스킬 마나 값
    {
        object state = typeof(PrototypeBootstrap).GetProperty("SkillState").GetValue(bootstrap); // 현재 스킬 실행 상태
        return Convert.ToSingle(state.GetType().GetProperty("Mana").GetValue(state)); // 현재 마나
    }

    private float SkillCooldown(int slot) // 현재 스킬 슬롯의 남은 대기시간
    {
        object state = typeof(PrototypeBootstrap).GetProperty("SkillState").GetValue(bootstrap); // 현재 스킬 실행 상태
        return Convert.ToSingle(state.GetType().GetMethod("RemainingCooldown").Invoke(state, new object[] { slot })); // 슬롯의 남은 시간
    }

    private static bool Invulnerable(Fighter player) // 플레이어의 공통 무적 상태
    {
        PropertyInfo property = typeof(Fighter).GetProperty("IsInvulnerable"); // 공개 무적 상태
        Assert.That(property, Is.Not.Null); // 무적 상태 속성 확인
        return (bool)property.GetValue(player); // 플레이어 무적 여부
    }
}
