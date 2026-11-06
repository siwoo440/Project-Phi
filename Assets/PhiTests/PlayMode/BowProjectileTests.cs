using System; // 무기 열거형 변환
using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 장애물 목록
using System.Reflection; // 시험 설정 연결
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 형식
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 마우스 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class BowProjectileTests // 활 공격과 투사체 검증
{
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings trial; // 독립 전투 설정
    private SceneMapTestFixture mapFixture; // 씬 맵 시험 격리
    private FieldInfo weaponField; // 시작 무기 설정 필드
    private FieldInfo speedField; // 화살 속도 설정 필드
    private readonly List<GameObject> obstacles = new List<GameObject>(); // 시험 장애물 목록
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private bool previousQueriesHitTriggers; // 기존 트리거 검사 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 활 전투와 시험 맵 준비
    {
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousQueriesHitTriggers = Physics2D.queriesHitTriggers; // 기존 트리거 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        mapFixture = new SceneMapTestFixture(); // 씬 맵의 독립 시험 배치
        weaponField = typeof(PrototypeSettings).GetField("startingWeapon"); // 시작 무기 설정 확인
        speedField = typeof(PrototypeSettings).GetField("bowProjectileSpeed"); // 화살 속도 설정 확인
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            trial = ScriptableObject.CreateInstance<PrototypeSettings>(); // 원본을 보존할 시험 설정
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            if (weaponField != null) // 시작 무기 필드 존재 여부
            {
                weaponField.SetValue(trial, Enum.Parse(weaponField.FieldType, "Bow")); // 시작 무기를 활로 지정
            }
            PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 씬 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, trial); // 시작 전 시험 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 씬 로드 콜백 해제
        Assert.That(weaponField, Is.Not.Null, "시작 무기 선택 설정이 필요합니다."); // 활 선택 지원 확인
        Assert.That(speedField, Is.Not.Null, "화살 속도 설정이 필요합니다."); // 조정 가능한 화살 속도 확인
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 시험 자원과 전역 상태 복원
    {
        if (mouse != null && mouse.added) // 가상 장치 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        foreach (GameObject obstacle in obstacles) // 시험 장애물 검사
        {
            if (obstacle != null) // 장애물 존재 여부
            {
                Object.Destroy(obstacle); // 시험 장애물 제거
            }
        }
        obstacles.Clear(); // 시험 장애물 목록 초기화
        if (mapFixture != null) // 시험 맵 격리 존재 여부
        {
            mapFixture.Dispose(); // 시험 맵 이벤트와 그림 정리
        }
        if (trial != null) // 임시 설정 존재 여부
        {
            Object.Destroy(trial); // 임시 설정 제거
        }
        Physics2D.queriesHitTriggers = previousQueriesHitTriggers; // 기존 트리거 설정 복원
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 직선 첫 적 사례
    public IEnumerator ArrowDamagesOnlyFirstEnemyForNineteen() // 활 피해와 비관통 검증
    {
        Fighter first = PlaceSlime(1, new Vector2(2f, 0f)); // 가까운 첫 적
        Fighter second = PlaceSlime(2, new Vector2(4f, 0f)); // 첫 적 뒤의 두 번째 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 오른쪽으로 활 발사
        yield return new WaitForSeconds(0.4f); // 첫 적까지 이동 대기
        Assert.That(first.Health, Is.EqualTo(31)); // 첫 대상 19 피해
        Assert.That(second.Health, Is.EqualTo(50)); // 두 번째 대상 피해 방지
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 첫 적중 후 화살 소멸
    }

    [UnityTest] // 발사 방향 고정 사례
    public IEnumerator LaunchedArrowKeepsItsOriginalDirection() // 발사 후 마우스 이동 검증
    {
        Fighter right = PlaceSlime(1, new Vector2(3f, 0f)); // 최초 조준 방향의 적
        Fighter up = PlaceSlime(2, new Vector2(0f, 3f)); // 변경된 조준 방향의 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PressTowards(new Vector2(4f, 0f)); // 오른쪽으로 활 발사
        yield return null; // 첫 발사 처리 대기
        ReleaseAt(new Vector2(0f, 4f)); // 마우스를 위로 옮기고 버튼 해제
        yield return new WaitForSeconds(0.4f); // 화살 이동 대기
        Assert.That(right.Health, Is.EqualTo(31)); // 최초 방향의 적에게 19 피해
        Assert.That(up.Health, Is.EqualTo(50)); // 이동한 마우스 방향 무시
    }

    [UnityTest] // 앞쪽 벽과 적 사례
    public IEnumerator ObstacleBeforeEnemyStopsArrow() // 벽 뒤 적의 피해 방지
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 벽 뒤에 있는 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1.5f, 0f), 8, false); // 적 앞의 장애물
        PressTowards(new Vector2(4f, 0f)); // 벽 방향으로 활 발사
        yield return new WaitForSeconds(0.3f); // 벽까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 벽 뒤 적의 피해 차단
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 벽 충돌 후 화살 소멸
    }

    [UnityTest] // 화살 가장자리 스침 사례
    public IEnumerator ArrowEdgeCannotPassThroughWall() // 표시 크기에 닿는 벽 차단 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 얇은 벽 뒤의 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        GameObject wall = CreateObstacle(new Vector2(1.5f, 0.08f), 8, false); // 화살 중심에서 벗어난 벽
        wall.GetComponent<BoxCollider2D>().size = new Vector2(0.1f, 0.1f); // 벽 끝과 화살 가장자리 겹침
        PressTowards(new Vector2(4f, 0f)); // 벽 방향으로 활 발사
        yield return new WaitForSeconds(0.3f); // 벽까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 화살 가장자리에 닿는 벽의 피해 차단
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 벽 접촉 후 화살 소멸
    }

    [UnityTest] // 벽 앞의 적 사례
    public IEnumerator EnemyBeforeObstacleReceivesArrow() // 가까운 적 우선 충돌 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 벽 앞에 있는 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(3f, 0f), 8, false); // 적 뒤의 장애물
        PressTowards(new Vector2(4f, 0f)); // 적과 벽 방향으로 활 발사
        yield return new WaitForSeconds(0.3f); // 적까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(31)); // 벽보다 가까운 적의 피해
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 적 충돌 후 화살 소멸
    }

    [UnityTest] // 적과 벽의 거리 동률 사례
    public IEnumerator WallWinsWhenEnemyAndObstacleAreEquidistant() // 동시 충돌의 벽 우선 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 벽과 같은 거리에 닿는 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1.725f, 0f), 8, false); // 적의 왼쪽 면과 맞춘 벽
        PressTowards(new Vector2(3f, 0f)); // 벽과 적 방향으로 발사
        yield return new WaitForSeconds(0.3f); // 충돌 거리까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 거리 동률에서 벽 우선
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 벽 충돌 후 화살 소멸
    }

    [UnityTest] // 트리거와 무관 계층 사례
    public IEnumerator TriggersAndOtherLayersDoNotStopArrow() // 무관한 충돌체 무시 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 뒤쪽의 살아 있는 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1f, 0f), 0, false); // 무관한 계층의 충돌체
        CreateObstacle(new Vector2(2f, 0f), 8, true); // 장애물 계층의 트리거
        Physics2D.queriesHitTriggers = true; // 전역 트리거 검사가 켜진 조건
        PressTowards(new Vector2(4f, 0f)); // 적 방향으로 활 발사
        yield return new WaitForSeconds(0.4f); // 적까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(31)); // 트리거와 무관 계층 통과
    }

    [UnityTest] // 사망한 적 사례
    public IEnumerator DeadEnemyDoesNotBlockLivingEnemy() // 사망한 적을 통과하는 최초 생존 적 판정
    {
        Fighter dead = PlaceSlime(1, new Vector2(2f, 0f)); // 앞쪽의 사망 대상
        Fighter living = PlaceSlime(2, new Vector2(4f, 0f)); // 뒤쪽의 생존 대상
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        dead.TakeDamage(50); // 앞쪽 적 사망 처리
        PressTowards(new Vector2(5f, 0f)); // 두 적 방향으로 활 발사
        yield return new WaitForSeconds(0.4f); // 생존 적까지 이동 대기
        Assert.That(living.Health, Is.EqualTo(31)); // 최초 생존 적에게 19 피해
    }

    [UnityTest] // 긴 프레임 이동 사례
    public IEnumerator HighSpeedArrowCannotSkipFirstEnemy() // 한 번에 먼 거리를 이동하는 충돌 검증
    {
        speedField.SetValue(trial, 1000f); // 큰 이동 거리를 만드는 시험 속도
        Fighter first = PlaceSlime(1, new Vector2(3f, 0f)); // 빠른 화살 앞의 첫 적
        Fighter second = PlaceSlime(2, new Vector2(5f, 0f)); // 첫 적 뒤의 두 번째 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PressTowards(new Vector2(6f, 0f)); // 두 적 방향으로 활 발사
        yield return new WaitForSeconds(0.15f); // 고속 이동과 충돌 처리 대기
        Assert.That(first.Health, Is.EqualTo(31)); // 첫 적 통과 방지
        Assert.That(second.Health, Is.EqualTo(50)); // 첫 적 뒤의 피해 방지
    }

    [UnityTest] // 최대 사거리 사례
    public IEnumerator ArrowExpiresAtRangeWithoutHittingBeyondIt() // 사거리 밖 충돌 방지
    {
        speedField.SetValue(trial, 1000f); // 한 번에 사거리를 넘는 시험 속도
        Fighter target = PlaceSlime(1, new Vector2(7.7f, 0f)); // 충돌체 앞면도 사거리 밖인 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        PressTowards(new Vector2(9f, 0f)); // 사거리 밖 적을 향한 발사
        yield return new WaitForSeconds(0.15f); // 사거리 도달 처리 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 사거리 밖 적의 피해 방지
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 사거리 도달 후 화살 소멸
    }

    [UnityTest] // 씬 맵 경계 사례
    public IEnumerator ArrowStopsAtExistingSceneBoundary() // 씬 벽 바깥으로 화살 이동 방지
    {
        GameObject.Find("Player").transform.position = new Vector3(9f, 0f, 0f); // 오른쪽 씬 경계 근처의 플레이어
        PlaceSlime(1, new Vector2(-4f, 0f)); // 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 1f)); // 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -1f)); // 적의 간섭 제거
        PressTowards(new Vector2(12f, 0f)); // 씬 오른쪽 경계 방향 발사
        yield return new WaitForSeconds(0.25f); // 경계 도달 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 씬 경계에서 화살 소멸
        Assert.That(GameObject.Find("Boundary Right"), Is.Not.Null); // 기존 씬 경계 보존
    }

    [UnityTest] // 홀드 공격 간격 사례
    public IEnumerator HoldingAttackUsesBowInterval() // 활 연속 발사 간격 검증
    {
        speedField.SetValue(trial, 1f); // 동시에 남는 느린 시험 화살
        PlaceSlime(1, new Vector2(-4f, 0f)); // 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 1f)); // 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -1f)); // 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 오른쪽 공격 유지
        yield return new WaitForSeconds(0.5f); // 두 번째 발사 전 대기
        Assert.That(CountArrows(), Is.EqualTo(1)); // 0.70초 이전의 단일 발사
        yield return new WaitForSeconds(0.3f); // 두 번째 발사 가능 시각 대기
        Assert.That(CountArrows(), Is.EqualTo(2)); // 공격 간격 후 반복 발사
    }

    [UnityTest] // UI 클릭 차단 사례
    public IEnumerator UiClickDoesNotFireBow() // UI 입력과 활 공격 분리 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 이후 공격할 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        RectTransform panel = GameObject.Find("Health Panel").GetComponent<RectTransform>(); // 체력 UI 영역
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center)); // UI 중앙 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // UI 영역의 클릭
        yield return new WaitForSeconds(0.1f); // UI 클릭 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Null); // UI 클릭의 화살 발사 차단
        Assert.That(target.Health, Is.EqualTo(50)); // UI 클릭의 피해 차단
        ReleaseAt(new Vector2(3f, 0f)); // 버튼 해제와 전투 화면 이동
        yield return null; // 버튼 해제 처리 대기
        PressTowards(new Vector2(3f, 0f)); // 전투 화면에서 새 공격
        yield return new WaitForSeconds(0.3f); // 화살 적중 처리 대기
        Assert.That(target.Health, Is.EqualTo(31)); // UI 밖 공격의 정상 피해
    }

    [UnityTest] // 재시작 상태 사례
    public IEnumerator SceneRestartRemovesExistingArrows() // 재시작 시 투사체 상태 초기화
    {
        speedField.SetValue(trial, 1f); // 씬 전환 전 유지되는 시험 화살
        PlaceSlime(1, new Vector2(-4f, 0f)); // 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 1f)); // 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -1f)); // 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 시험 화살 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Not.Null); // 발사된 화살 존재
        ReleaseAt(new Vector2(5f, 0f)); // 재시작 전 마우스 버튼 해제
        yield return null; // 버튼 해제 처리 대기
        SceneManager.LoadScene("CombatPrototype"); // 전투 씬 재시작
        yield return null; // 씬 전환 대기
        yield return null; // 새 전투 초기화 대기
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 이전 전투의 화살 제거
    }

    [UnityTest] // 플레이어 사망 사례
    public IEnumerator PlayerDeathRemovesExistingArrows() // 사망 후 남은 화살 정리 검증
    {
        speedField.SetValue(trial, 1f); // 사망 전 유지되는 느린 시험 화살
        PlaceSlime(1, new Vector2(-4f, 0f)); // 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 1f)); // 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -1f)); // 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 시험 화살 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Not.Null); // 발사된 화살 존재
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 플레이어 사망 처리
        yield return null; // 사망 후 화살 정리 대기
        yield return null; // 표시 오브젝트 제거 대기
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 사망 후 이전 화살 제거
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적 확인
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적의 체력 상태
    }

    private GameObject CreateObstacle(Vector2 position, int layer, bool trigger) // 시험 장애물 생성
    {
        GameObject obstacle = new GameObject("Test Obstacle"); // 시험 장애물 오브젝트
        obstacle.layer = layer; // 지정된 충돌 계층
        obstacle.transform.position = position; // 장애물 위치
        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>(); // 장애물 충돌체
        collider.size = new Vector2(0.1f, 1f); // 시험 장애물 크기
        collider.isTrigger = trigger; // 트리거 여부
        obstacles.Add(obstacle); // 시험 후 정리 대상 등록
        return obstacle; // 생성된 시험 장애물
    }

    private void PressTowards(Vector2 world) // 지정 방향의 마우스 누름
    {
        Vector2 position = Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // 좌클릭 입력
    }

    private void ReleaseAt(Vector2 world) // 지정 방향의 마우스 놓기
    {
        Vector2 position = Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 해제 입력
    }

    private static int CountArrows() // 현재 화살 수 확인
    {
        int count = 0; // 현재 화살 수
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) // 활성 씬 오브젝트 검사
        {
            if (item.name == "Arrow") // 화살 이름 확인
            {
                count++; // 화살 수 증가
            }
        }
        return count; // 확인된 화살 수
    }
}
