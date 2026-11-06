using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 장애물 목록
using System.Reflection; // 시험 설정 연결
using NUnit.Framework; // 시험 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 이벤트
using UnityEngine.InputSystem; // 가상 키보드
using UnityEngine.InputSystem.LowLevel; // 키보드 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 로드
using UnityEngine.TestTools; // 플레이 모드 시험
using Object = UnityEngine.Object; // 유니티 객체 구분

public class ObstacleMovementTests // 플레이어와 적의 벽 이동 검증
{
    private readonly List<GameObject> walls = new List<GameObject>(); // 시험 후 제거할 장애물
    private SceneMapTestFixture map; // 독립 씬 맵 시험 설정
    private PrototypeSettings trial; // 원본을 보존할 전투 설정
    private Keyboard keyboard; // 가상 키보드
    private UnityAction<Scene, LoadSceneMode> loaded; // 씬 로드 설정 콜백
    private bool oldBackground; // 기존 백그라운드 설정
    private InputSettings.BackgroundBehavior oldBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior; // 기존 에디터 입력 설정

    [UnitySetUp] // 각 시험의 전투 준비
    public IEnumerator SetUp() // 독립 설정과 씬 로드
    {
        oldBackground = Application.runInBackground; // 기존 실행 설정 보관
        oldBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        oldEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 시험 중 백그라운드 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 유지
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 입력 경로
        keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 가상 키보드
        map = new SceneMapTestFixture(); // 저장된 씬 맵의 시험 복제
        loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 필드
            trial = Object.Instantiate((PrototypeSettings)field.GetValue(battle)); // 사용자 설정 복제
            trial.playerHealth = 10000; // 적 시험의 충분한 체력
            trial.playerSpeed = 20f; // 벽 통과를 확인할 이동 속도
            trial.slimeCount = 1; // 슬라임 시험 개수
            trial.goblinCount = 1; // 고블린 시험 개수
            trial.skeletonCount = 1; // 궁수 시험 개수
            trial.orcCount = 1; // 오크 시험 개수
            trial.wolfCount = 1; // 늑대 시험 개수
            trial.slimeSpeed = 20f; // 슬라임 시험 속도
            trial.goblinSpeed = 20f; // 고블린 시험 속도
            trial.skeletonSpeed = 20f; // 궁수 시험 속도
            trial.orcSpeed = 20f; // 오크 시험 속도
            trial.wolfSpeed = 20f; // 늑대 시험 속도
            trial.slimeAttackRange = 0.01f; // 추격 시험의 공격 방지
            trial.goblinAttackRange = 0.01f; // 추격 시험의 공격 방지
            trial.skeletonAttackRange = 0f; // 추격 시험의 발사 방지
            trial.skeletonKeepDistance = 0f; // 궁수 접근 시험 거리
            trial.orcAttackRange = 0.01f; // 추격 시험의 공격 방지
            trial.wolfAttackRange = 0.01f; // 추격 시험의 공격 방지
            SkillSettingsTestFixture.UseRecommended(trial); // 시험 스킬 조합
            field.SetValue(battle, trial); // 전투 시작 전 복제 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
        yield return null; // 전투 생성 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 설정 콜백 해제
        loaded = null; // 중복 해제 방지
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 입력과 시험 상태 복원
    {
        if (loaded != null) // 남은 콜백 확인
        {
            SceneManager.sceneLoaded -= loaded; // 씬 이벤트 해제
        }
        foreach (GameObject wall in walls) // 시험 장애물 순회
        {
            Object.Destroy(wall); // 시험 장애물 제거
        }
        walls.Clear(); // 시험 장애물 목록 초기화
        if (keyboard != null && keyboard.added) // 가상 키보드 확인
        {
            InputSystem.RemoveDevice(keyboard); // 가상 입력 장치 제거
        }
        if (trial != null) // 복제 설정 확인
        {
            Object.Destroy(trial); // 시험 설정 제거
        }
        map.Dispose(); // 씬 맵 시험 설정 정리
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = oldBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = oldBackground; // 기존 백그라운드 설정 복원
        yield return null; // 프레임 정리 대기
    }

    [UnityTest] // 실제 씬에 저장된 내부 벽 사례
    public IEnumerator SavedSceneWallBlocksPlayerMovement() // 하이어라키 벽의 실제 이동 차단
    {
        DisableEnemiesExcept(""); // 적의 시험 간섭 제거
        Transform wall = GameObject.Find("Map").transform.Find("Map Floor/Internal Wall"); // 저장된 씬의 내부 벽
        Assert.That(wall, Is.Not.Null); // 하이어라키 벽 존재 확인
        wall.gameObject.SetActive(true); // 실제 씬 벽 활성화
        Physics2D.SyncTransforms(); // 벽 충돌체의 물리 반영
        BoxCollider2D collider = wall.GetComponent<BoxCollider2D>(); // 씬에 저장된 벽 충돌체
        float stop = collider.bounds.min.x - PrototypeSettings.PlayerRadius; // 벽 앞 플레이어 중심 한계
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 실제 플레이어
        player.transform.position = new Vector3(stop - 1f, wall.position.y, 0f); // 벽 왼쪽의 플레이어
        Press(Key.D); // 저장된 벽을 향한 이동
        yield return new WaitForSeconds(0.2f); // 벽 도달 대기
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(stop + 0.001f)); // 실제 씬 벽의 이동 차단
        Press(); // 가상 입력 해제
    }

    [UnityTest] // 플레이어의 고속 벽 이동 사례
    public IEnumerator PlayerStopsAtWallThenMovesAlongAndAway() // 벽 통과 방지와 접촉 후 이동
    {
        DisableEnemiesExcept(""); // 적의 시험 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 실제 플레이어
        CreateWall(new Vector2(1.2f, 0f), new Vector2(0.2f, 10f), false); // 플레이어 오른쪽 벽
        Press(Key.D); // 벽을 향한 이동
        yield return new WaitForSeconds(0.2f); // 벽까지 이동 대기
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(0.751f)); // 플레이어 도형의 벽 통과 방지
        Press(Key.D, Key.W); // 벽 방향과 접선 방향 입력
        yield return new WaitForSeconds(0.1f); // 벽을 따라 이동 대기
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(0.751f)); // 대각선의 벽 통과 방지
        Assert.That(player.transform.position.y, Is.GreaterThan(0.5f)); // 접촉면을 따른 이동 허용
        Press(Key.A); // 벽 반대 방향 입력
        yield return new WaitForSeconds(0.1f); // 벽에서 멀어지는 이동 대기
        Assert.That(player.transform.position.x, Is.LessThan(0.4f)); // 접촉면에서 벗어나는 이동 허용
        Press(); // 가상 입력 해제
    }

    [UnityTest] // 트리거 장애물 통과 사례
    public IEnumerator PlayerIgnoresTriggerObstacle() // 트리거의 이동 차단 제외
    {
        DisableEnemiesExcept(""); // 적의 시험 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 실제 플레이어
        CreateWall(new Vector2(1.2f, 0f), new Vector2(0.2f, 10f), true); // 트리거 시험 벽
        Press(Key.D); // 트리거를 향한 이동
        yield return new WaitForSeconds(0.2f); // 트리거 통과 대기
        Assert.That(player.transform.position.x, Is.GreaterThan(1.5f)); // 트리거 통과 허용
        Press(); // 가상 입력 해제
    }

    [UnityTest] // 슬라임의 벽 추격 사례
    public IEnumerator SlimeApproachStopsAtWall() // 슬라임 벽 이동 검증
    {
        yield return CheckEnemyApproach("Slime 1"); // 실제 슬라임 추격 검사
    }

    [UnityTest] // 고블린의 벽 추격 사례
    public IEnumerator GoblinApproachStopsAtWall() // 고블린 벽 이동 검증
    {
        yield return CheckEnemyApproach("Goblin 1"); // 실제 고블린 추격 검사
    }

    [UnityTest] // 궁수의 벽 접근 사례
    public IEnumerator SkeletonApproachStopsAtWall() // 궁수 벽 이동 검증
    {
        yield return CheckEnemyApproach("Skeleton 1"); // 실제 궁수 접근 검사
    }

    [UnityTest] // 오크의 벽 추격 사례
    public IEnumerator OrcApproachStopsAtWall() // 오크 벽 이동 검증
    {
        yield return CheckEnemyApproach("Orc 1"); // 실제 오크 추격 검사
    }

    [UnityTest] // 늑대의 벽 추격 사례
    public IEnumerator WolfApproachStopsAtWall() // 늑대 벽 이동 검증
    {
        yield return CheckEnemyApproach("Wolf 1"); // 실제 늑대 추격 검사
    }

    [UnityTest] // 슬라임의 벽 돌진 사례
    public IEnumerator SlimeDashStopsAtWallAndCannotHitBehindIt() // 슬라임 돌진과 벽 뒤 피해 검증
    {
        DisableEnemiesExcept("Slime 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 시험 슬라임
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 벽 뒤의 플레이어
        enemy.transform.position = new Vector3(-1f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(1f, 0f, 0f); // 벽 오른쪽의 플레이어
        trial.slimeSpeed = 0f; // 돌진 전 추격 방지
        trial.slimeAttackRange = 3f; // 시험 돌진 시작 거리
        trial.slimeWindup = 0.05f; // 짧은 시험 공격 예고
        trial.slimeAttackInterval = 5f; // 중복 시험 공격 방지
        trial.slimeDashDistance = 3f; // 벽 너머를 향한 돌진 거리
        trial.slimeDashSpeed = 30f; // 벽 통과를 확인할 돌진 속도
        CreateWall(Vector2.zero, new Vector2(0.2f, 8f), false); // 두 대상 사이의 벽
        yield return new WaitForSeconds(0.4f); // 예고와 돌진 대기
        Assert.That(enemy.transform.position.x, Is.LessThanOrEqualTo(-0.45f)); // 벽 앞의 돌진 정지
        Assert.That(player.Health, Is.EqualTo(10000)); // 벽 뒤의 플레이어 피해 방지
    }

    [UnityTest] // 늑대의 벽 도약 사례
    public IEnumerator WolfLeapStopsAtWallAndCannotHitBehindIt() // 늑대 도약과 벽 뒤 피해 검증
    {
        DisableEnemiesExcept("Wolf 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Wolf 1").GetComponent<Fighter>(); // 시험 늑대
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 벽 뒤의 플레이어
        enemy.transform.position = new Vector3(-1f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(1f, 0f, 0f); // 벽 오른쪽의 플레이어
        trial.wolfSpeed = 0f; // 도약 전 추격 방지
        trial.wolfAttackRange = 3f; // 시험 도약 시작 거리
        trial.wolfWindup = 0.05f; // 짧은 시험 공격 예고
        trial.wolfAttackInterval = 5f; // 중복 시험 공격 방지
        trial.wolfLeapDistance = 3f; // 벽 너머를 향한 도약 거리
        trial.wolfLeapSpeed = 30f; // 벽 통과를 확인할 도약 속도
        CreateWall(Vector2.zero, new Vector2(0.2f, 8f), false); // 두 대상 사이의 벽
        yield return new WaitForSeconds(0.4f); // 예고와 도약 대기
        Assert.That(enemy.transform.position.x, Is.LessThanOrEqualTo(-0.45f)); // 벽 앞의 도약 정지
        Assert.That(player.Health, Is.EqualTo(10000)); // 벽 뒤의 플레이어 피해 방지
    }

    [UnityTest] // 늑대의 장애물 경로 예고 사례
    public IEnumerator WolfPreviewEndsAtWall() // 벽에 맞춘 도약 경로 표시 검증
    {
        DisableEnemiesExcept("Wolf 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Wolf 1").GetComponent<Fighter>(); // 시험 늑대
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 벽 뒤의 플레이어
        enemy.transform.position = new Vector3(-1f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(1f, 0f, 0f); // 벽 오른쪽의 플레이어
        trial.wolfSpeed = 0f; // 예고 전 추격 방지
        trial.wolfAttackRange = 3f; // 시험 도약 시작 거리
        trial.wolfWindup = 0.5f; // 경로 확인을 위한 예고 시간
        trial.wolfLeapDistance = 3f; // 벽 너머를 향한 도약 거리
        CreateWall(Vector2.zero, new Vector2(0.2f, 8f), false); // 두 대상 사이의 벽
        yield return null; // 경로 예고 시작 대기
        Transform path = enemy.transform.Find("Wolf Path"); // 실제 도약 경로 표시
        Assert.That(path.gameObject.activeSelf, Is.True); // 공격 경로 표시 확인
        Vector3 end = path.TransformPoint(new Vector3(0.5f, 0f, 0f)); // 화면에 표시된 경로 끝
        Assert.That(end.x, Is.LessThanOrEqualTo(-0.45f)); // 벽 앞의 경로 종료
    }

    [UnityTest] // 원형 장애물 접촉 사례
    public IEnumerator PlayerStopsAtCircleThenMovesAway() // 원형 벽 차단과 후퇴 검증
    {
        DisableEnemiesExcept(""); // 적의 시험 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 실제 플레이어
        GameObject wall = new GameObject("Movement Test Circle"); // 원형 시험 장애물
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = new Vector3(1.2f, 0f, 0f); // 플레이어 오른쪽 원형 벽
        wall.AddComponent<CircleCollider2D>().radius = 0.3f; // 시험 장애물 반지름
        walls.Add(wall); // 시험 후 제거할 벽 등록
        Physics2D.SyncTransforms(); // 장애물 위치의 물리 반영
        Press(Key.D); // 원형 벽을 향한 이동
        yield return new WaitForSeconds(0.2f); // 원형 벽 도달 대기
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(0.551f)); // 원형 벽 통과 방지
        Press(Key.A); // 벽 반대 방향 입력
        yield return new WaitForSeconds(0.1f); // 접촉 후 후퇴 대기
        Assert.That(player.transform.position.x, Is.LessThan(0.3f)); // 원형 벽에서 후퇴 허용
        Press(); // 가상 입력 해제
    }

    [UnityTest] // 궁수의 후퇴 방향 벽 사례
    public IEnumerator SkeletonRetreatStopsAtWall() // 궁수의 거리 유지 후퇴와 벽 충돌
    {
        DisableEnemiesExcept("Skeleton 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Skeleton 1").GetComponent<Fighter>(); // 시험 궁수
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 가까운 플레이어
        enemy.transform.position = new Vector3(1f, 0f, 0f); // 플레이어 오른쪽 궁수
        player.transform.position = Vector3.zero; // 후퇴를 유발할 플레이어 위치
        trial.skeletonKeepDistance = 3f; // 후퇴가 필요한 유지 거리
        CreateWall(new Vector2(1.6f, 0f), new Vector2(0.2f, 8f), false); // 궁수 뒤쪽 벽
        yield return new WaitForSeconds(0.25f); // 후퇴 이동 대기
        Assert.That(enemy.transform.position.x, Is.LessThanOrEqualTo(1.151f)); // 벽 뒤로 후퇴 방지
        Assert.That(enemy.transform.position.x, Is.GreaterThanOrEqualTo(1f)); // 플레이어 반대 방향 후퇴
    }

    private IEnumerator CheckEnemyApproach(string name) // 적 종류별 벽 추격 확인
    {
        DisableEnemiesExcept(name); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find(name).GetComponent<Fighter>(); // 시험할 실제 적
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 추격 대상 플레이어
        enemy.transform.position = new Vector3(-2f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(2f, 0f, 0f); // 벽 오른쪽의 플레이어
        CreateWall(Vector2.zero, new Vector2(0.2f, 8f), false); // 두 대상 사이의 벽
        yield return new WaitForSeconds(0.25f); // 고속 추격 대기
        Assert.That(enemy.transform.position.x, Is.GreaterThan(-1.9f), name); // 벽 앞까지 추격 확인
        Assert.That(enemy.transform.position.x, Is.LessThanOrEqualTo(-0.45f), name); // 적 도형의 벽 통과 방지
    }

    private void DisableEnemiesExcept(string keep) // 시험에 불필요한 적 제거
    {
        string[] names = { "Slime 1", "Goblin 1", "Skeleton 1", "Orc 1", "Wolf 1" }; // 일반 적 다섯 종류
        foreach (string name in names) // 시험 적 목록 순회
        {
            if (name != keep) // 시험 대상 외 적 확인
            {
                GameObject enemy = GameObject.Find(name); // 실제 적 오브젝트
                enemy.GetComponent<Fighter>().TakeDamage(100000); // 기존 사망 처리로 행동 중지
            }
        }
    }

    private void CreateWall(Vector2 position, Vector2 size, bool trigger) // 시험 인스턴스의 장애물 생성
    {
        GameObject wall = new GameObject("Movement Test Wall"); // 시험용 벽 오브젝트
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = position; // 시험 벽 위치
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>(); // 시험 벽 충돌체
        box.size = size; // 시험 벽 크기
        box.isTrigger = trigger; // 트리거 여부
        walls.Add(wall); // 시험 후 제거할 벽 등록
        Physics2D.SyncTransforms(); // 변경 위치의 물리 반영
    }

    private void Press(params Key[] keys) // 가상 키보드 상태 입력
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 선택한 키의 상태 반영
    }
}
