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
    public IEnumerator SlimeApproachAvoidsWall() // 슬라임 벽 이동 검증
    {
        yield return CheckEnemyApproach("Slime 1"); // 실제 슬라임 추격 검사
    }

    [UnityTest] // 씬 벽을 우회하는 슬라임 사례
    public IEnumerator SlimeRoutesAroundSceneWall() // 슬라임의 벽 우회
    {
        yield return CheckEnemyRoutesAroundSceneWall("Slime 1"); // 실제 슬라임 우회 검사
    }

    [UnityTest] // 씬 벽을 우회하는 고블린 사례
    public IEnumerator GoblinRoutesAroundSceneWall() // 고블린의 벽 우회
    {
        yield return CheckEnemyRoutesAroundSceneWall("Goblin 1"); // 실제 고블린 우회 검사
    }

    [UnityTest] // 씬 벽을 우회하는 궁수 사례
    public IEnumerator SkeletonRoutesAroundSceneWall() // 궁수의 벽 우회
    {
        yield return CheckEnemyRoutesAroundSceneWall("Skeleton 1"); // 실제 궁수 우회 검사
    }

    [UnityTest] // 씬 벽을 우회하는 오크 사례
    public IEnumerator OrcRoutesAroundSceneWall() // 오크의 벽 우회
    {
        yield return CheckEnemyRoutesAroundSceneWall("Orc 1"); // 실제 오크 우회 검사
    }

    [UnityTest] // 씬 벽을 우회하는 늑대 사례
    public IEnumerator WolfRoutesAroundSceneWall() // 늑대의 벽 우회
    {
        yield return CheckEnemyRoutesAroundSceneWall("Wolf 1"); // 실제 늑대 우회 검사
    }

    [UnityTest] // 공격 범위 안에서 벽에 가린 슬라임 사례
    public IEnumerator SlimeRoutesWhenAttackRangeIsBlocked() // 가려진 공격 범위의 슬라임 추격
    {
        trial.slimeAttackRange = 6f; // 벽 너머까지 닿는 시험 공격 거리
        yield return CheckEnemyFindsSightAroundSceneWall("Slime 1"); // 벽 뒤 목표의 시야 확보 확인
    }

    [UnityTest] // 공격 범위 안에서 벽에 가린 고블린 사례
    public IEnumerator GoblinRoutesWhenAttackRangeIsBlocked() // 가려진 공격 범위의 고블린 추격
    {
        trial.goblinAttackRange = 6f; // 벽 너머까지 닿는 시험 공격 거리
        yield return CheckEnemyFindsSightAroundSceneWall("Goblin 1"); // 벽 뒤 목표의 시야 확보 확인
    }

    [UnityTest] // 공격 범위 안에서 벽에 가린 궁수 사례
    public IEnumerator SkeletonRoutesWhenAttackRangeIsBlocked() // 가려진 공격 범위의 궁수 접근
    {
        trial.skeletonAttackRange = 6f; // 벽 너머까지 닿는 시험 발사 거리
        yield return CheckEnemyFindsSightAroundSceneWall("Skeleton 1"); // 벽 뒤 목표의 시야 확보 확인
    }

    [UnityTest] // 공격 범위 안에서 벽에 가린 오크 사례
    public IEnumerator OrcRoutesWhenAttackRangeIsBlocked() // 가려진 공격 범위의 오크 추격
    {
        trial.orcAttackRange = 6f; // 벽 너머까지 닿는 시험 공격 거리
        yield return CheckEnemyFindsSightAroundSceneWall("Orc 1"); // 벽 뒤 목표의 시야 확보 확인
    }

    [UnityTest] // 공격 범위 안에서 벽에 가린 늑대 사례
    public IEnumerator WolfRoutesWhenAttackRangeIsBlocked() // 가려진 공격 범위의 늑대 추격
    {
        trial.wolfAttackRange = 6f; // 벽 너머까지 닿는 시험 도약 거리
        yield return CheckEnemyFindsSightAroundSceneWall("Wolf 1"); // 벽 뒤 목표의 시야 확보 확인
    }

    [UnityTest] // 막힌 경로와 장애물 변경 사례
    public IEnumerator EnemyStopsWhenSeparatedAndRoutesAfterWallShrinks() // 도달 불가 정지와 재탐색
    {
        DisableEnemiesExcept("Goblin 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Goblin 1").GetComponent<Fighter>(); // 시험 고블린
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 추격 대상
        enemy.transform.position = new Vector3(-2f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(2f, 0f, 0f); // 벽 오른쪽의 플레이어
        Transform wall = ConfigureSceneWall(new Vector2(0.4f, 12f)); // 맵을 완전히 가르는 씬 벽
        yield return new WaitForSeconds(0.6f); // 도달 불가 처리 대기
        Vector2 stopped = enemy.transform.position; // 접근 불가 상태의 위치
        Assert.That(stopped.x, Is.LessThan(-0.6f)); // 완전히 막힌 벽 통과 방지
        yield return new WaitForSeconds(0.4f); // 정지 상태 유지 대기
        Assert.That(Vector2.Distance(stopped, enemy.transform.position), Is.LessThan(0.05f)); // 도달 불가 시 반복 진동 방지
        wall.localScale = new Vector3(0.02f, 0.25f, 1f); // 씬 벽의 위아래 통로 개방
        Physics2D.SyncTransforms(); // 변경된 벽 위치 반영
        yield return new WaitForSeconds(2f); // 바뀐 경로 탐색과 우회 대기
        Assert.That(enemy.transform.position.x, Is.GreaterThan(0.6f)); // 장애물 변경 후 실제 우회
    }

    [UnityTest] // 연속된 두 장애물 우회 사례
    public IEnumerator EnemyRoutesAroundTwoWallsWithoutCrossingEither() // 여러 씬 장애물의 우회 검증
    {
        DisableEnemiesExcept("Goblin 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Goblin 1").GetComponent<Fighter>(); // 시험 고블린
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 추격 대상
        enemy.transform.position = new Vector3(-3f, 0f, 0f); // 두 벽 왼쪽의 적
        player.transform.position = new Vector3(3f, 0f, 0f); // 두 벽 오른쪽의 플레이어
        ConfigureSceneWall(new Vector2(0.4f, 3f)); // 첫 번째 씬 벽
        CreateWall(new Vector2(1f, 0f), new Vector2(0.4f, 3f), false); // 두 번째 시험 벽
        Collider2D first = GameObject.Find("Map").transform.Find("Map Floor/Internal Wall").GetComponent<Collider2D>(); // 첫 벽 충돌체
        Collider2D second = GameObject.Find("Movement Test Wall").GetComponent<Collider2D>(); // 두 번째 벽 충돌체
        float until = Time.time + 2f; // 두 장애물을 우회할 종료 시각
        while (Time.time < until) // 실제 우회 중 위치 검사
        {
            AssertOutsideWall(enemy, first, "첫 벽"); // 첫 장애물 관통 방지
            AssertOutsideWall(enemy, second, "두 번째 벽"); // 두 번째 장애물 관통 방지
            yield return null; // 다음 이동 프레임 대기
        }
        Assert.That(enemy.transform.position.x, Is.GreaterThan(1.7f)); // 두 장애물의 실제 우회
    }

    [UnityTest] // 플레이어가 벽 가까이 서 있는 사례
    public IEnumerator EnemyApproachesPlayerBesideWall() // 적보다 작은 플레이어의 벽 접촉 거리 검증
    {
        DisableEnemiesExcept("Goblin 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Goblin 1").GetComponent<Fighter>(); // 시험 고블린
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 추격 대상
        enemy.transform.position = new Vector3(3f, 0f, 0f); // 벽 오른쪽의 적
        player.transform.position = new Vector3(0.57f, 0f, 0f); // 플레이어 도형은 안전하지만 적 도형은 겹치는 위치
        Transform wall = ConfigureSceneWall(new Vector2(0.4f, 3f)); // 플레이어 옆의 씬 벽
        yield return new WaitForSeconds(0.5f); // 가능한 접근 위치 탐색 대기
        Assert.That(enemy.transform.position.x, Is.LessThan(1.5f)); // 벽 곁의 플레이어에게 접근
        AssertOutsideWall(enemy, wall.GetComponent<Collider2D>(), "고블린"); // 접근 중 벽 관통 방지
    }

    [UnityTest] // 고블린의 벽 추격 사례
    public IEnumerator GoblinApproachAvoidsWall() // 고블린 벽 이동 검증
    {
        yield return CheckEnemyApproach("Goblin 1"); // 실제 고블린 추격 검사
    }

    [UnityTest] // 궁수의 벽 접근 사례
    public IEnumerator SkeletonApproachAvoidsWall() // 궁수 벽 이동 검증
    {
        yield return CheckEnemyApproach("Skeleton 1"); // 실제 궁수 접근 검사
    }

    [UnityTest] // 오크의 벽 추격 사례
    public IEnumerator OrcApproachAvoidsWall() // 오크 벽 이동 검증
    {
        yield return CheckEnemyApproach("Orc 1"); // 실제 오크 추격 검사
    }

    [UnityTest] // 늑대의 벽 추격 사례
    public IEnumerator WolfApproachAvoidsWall() // 늑대 벽 이동 검증
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

    [UnityTest] // 궁수의 벽 뒤쪽 거리 유지 사례
    public IEnumerator SkeletonRetreatsAroundWallToKeepDistance() // 후퇴 경로의 벽 우회 검증
    {
        DisableEnemiesExcept("Skeleton 1"); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find("Skeleton 1").GetComponent<Fighter>(); // 시험 궁수
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 가까운 플레이어
        enemy.transform.position = new Vector3(1f, 0f, 0f); // 플레이어 오른쪽 궁수
        player.transform.position = Vector3.zero; // 후퇴를 유발할 플레이어 위치
        trial.skeletonKeepDistance = 3f; // 후퇴가 필요한 유지 거리
        CreateWall(new Vector2(1.6f, 0f), new Vector2(0.2f, 4f), false); // 후퇴 방향의 벽
        Collider2D wall = GameObject.Find("Movement Test Wall").GetComponent<Collider2D>(); // 실제 시험 벽
        yield return new WaitForSeconds(0.5f); // 안전한 후퇴 경로 탐색
        Assert.That(Vector2.Distance(enemy.transform.position, player.transform.position), Is.GreaterThan(2.5f)); // 벽을 따라 안전 거리 확보
        AssertOutsideWall(enemy, wall, "궁수"); // 후퇴 중 벽 관통 방지
    }

    private IEnumerator CheckEnemyFindsSightAroundSceneWall(string name) // 벽에 가린 공격 범위의 시야 확보 검사
    {
        DisableEnemiesExcept(name); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find(name).GetComponent<Fighter>(); // 시험할 실제 적
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 공격 대상
        enemy.transform.position = new Vector3(-2f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(2f, 0f, 0f); // 벽 오른쪽의 플레이어
        Transform wall = ConfigureSceneWall(new Vector2(0.4f, 3f)); // 두 대상 사이의 씬 벽
        yield return new WaitForSeconds(2f); // 공격 가능한 위치 탐색 대기
        Physics2D.SyncTransforms(); // 이동한 적 위치의 물리 반영
        ContactFilter2D filter = new ContactFilter2D(); // 실제 장애물 시야 필터
        filter.SetLayerMask(1 << LayerMask.NameToLayer("PhiObstacle")); // 장애물 계층만 선택
        filter.useTriggers = false; // 트리거 장애물 제외
        RaycastHit2D[] hits = new RaycastHit2D[1]; // 벽 가림 검사 결과
        Assert.That(Physics2D.Linecast(enemy.transform.position, player.transform.position, filter, hits), Is.EqualTo(0), name); // 공격 가능한 시야 확보
        AssertOutsideWall(enemy, wall.GetComponent<Collider2D>(), name); // 벽을 통과하지 않은 우회
    }

    private IEnumerator CheckEnemyRoutesAroundSceneWall(string name) // 저장된 씬 벽을 이용한 실제 우회 검사
    {
        DisableEnemiesExcept(name); // 시험 대상 외 적 제거
        Fighter enemy = GameObject.Find(name).GetComponent<Fighter>(); // 시험할 실제 적
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 추격 대상
        enemy.transform.position = new Vector3(-2f, 0f, 0f); // 벽 왼쪽의 적
        player.transform.position = new Vector3(2f, 0f, 0f); // 벽 오른쪽의 플레이어
        Transform wall = ConfigureSceneWall(new Vector2(0.4f, 3f)); // 통로가 있는 씬 벽
        float until = Time.time + 2f; // 벽 끝을 우회할 종료 시각
        while (Time.time < until) // 우회 중 매 프레임 검사
        {
            AssertOutsideWall(enemy, wall.GetComponent<Collider2D>(), name); // 적 도형의 벽 관통 방지
            yield return null; // 다음 이동 프레임 대기
        }
        Assert.That(enemy.transform.position.x, Is.GreaterThan(0.6f), name); // 벽을 돌아 플레이어 쪽으로 이동
        Assert.That(wall.gameObject.activeInHierarchy, Is.True); // 기존 씬 벽 유지
    }

    private Transform ConfigureSceneWall(Vector2 size) // 기존 씬 장애물의 시험 크기 설정
    {
        Transform wall = GameObject.Find("Map").transform.Find("Map Floor/Internal Wall"); // 씬에 저장된 내부 벽
        wall.localPosition = Vector3.zero; // 맵 가운데의 시험 벽
        wall.localScale = new Vector3(size.x / 20f, size.y / 12f, 1f); // 바닥 크기에 맞춘 실제 벽 크기
        wall.gameObject.SetActive(true); // 씬 벽 활성화
        Physics2D.SyncTransforms(); // 시험 장애물 위치 반영
        return wall; // 변경한 씬 벽
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
        AssertOutsideWall(enemy, GameObject.Find("Movement Test Wall").GetComponent<Collider2D>(), name); // 적 도형의 벽 관통 방지
    }

    private static void AssertOutsideWall(Fighter enemy, Collider2D wall, string name) // 적 도형과 벽의 겹침 검증
    {
        Bounds bounds = wall.bounds; // 실제 벽의 월드 경계
        Vector2 position = enemy.transform.position; // 실제 적 중심
        float radius = PrototypeSettings.SlimeRadius - 0.002f; // 허용 오차를 제외한 적 반폭
        bool overlapsX = position.x + radius > bounds.min.x && position.x - radius < bounds.max.x; // 가로축 겹침 여부
        bool overlapsY = position.y + radius > bounds.min.y && position.y - radius < bounds.max.y; // 세로축 겹침 여부
        Assert.That(overlapsX && overlapsY, Is.False, name); // 벽 내부에 도형이 들어가지 않는지 확인
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
