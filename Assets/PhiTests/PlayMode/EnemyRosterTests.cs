using System; // 동적 설정 자료형
using System.Collections; // 프레임 단위 시험
using System.Reflection; // 씬 설정 참조
using NUnit.Framework; // 시험 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using Object = UnityEngine.Object; // 유니티 객체 구분

public class EnemyRosterTests // 22일차 일반 적 5종 시험
{
    private Mouse mouse; // 시험 마우스
    private Keyboard keyboard; // 시험 키보드
    private SceneMapTestFixture map; // 독립 씬 맵
    private PrototypeSettings trial; // 독립 전투 설정
    private UnityAction<Scene, LoadSceneMode> loaded; // 시험 설정 콜백
    private bool oldBackground; // 기존 백그라운드 설정
    private InputSettings.BackgroundBehavior oldBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior; // 기존 입력 경로

    [UnitySetUp] // 각 시험의 전투 준비
    public IEnumerator SetUp() // 시험 설정과 실제 씬 로드
    {
        oldBackground = Application.runInBackground; // 기존 실행 설정 보관
        oldBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        oldEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 시험 중 백그라운드 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 유지
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 입력 경로
        mouse = InputSystem.AddDevice<Mouse>(); // 독립 시험 마우스
        keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 시험 키보드
        map = new SceneMapTestFixture(); // 기존 맵 인스턴스 격리
        loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 참조
            trial = Object.Instantiate((PrototypeSettings)field.GetValue(battle)); // 사용자 설정 복제
            trial.playerHealth = 100; // 시험 플레이어 체력
            trial.playerDefense = 5; // 시험 플레이어 방어력
            trial.playerAttack = 10; // 시험 플레이어 공격력
            trial.playerMana = 50; // 시험 최대 마나
            trial.playerStamina = 100; // 시험 최대 스태미나
            trial.dodgeCost = 25; // 시험 회피 비용
            trial.dodgeDuration = 1f; // 시험 무적 유지 시간
            trial.dodgeInvulnerability = 1f; // 시험 회피 무적 시간
            trial.dodgeDistance = 0f; // 시험 중 플레이어 위치 유지
            trial.startingWeapon = WeaponType.Sword; // 시험 검 장착
            trial.swordAttack = 12; // 시험 검 공격력
            trial.whirlwindRadius = 2f; // 시험 회오리 반경
            trial.whirlwindCost = 14; // 시험 회오리 마나 비용
            trial.whirlwindMultiplier = 1.3f; // 시험 회오리 배율
            trial.startingSkillQ = SkillId.Fireball; // 시험 Q 슬롯
            trial.startingSkillE = SkillId.WhirlwindSlash; // 시험 E 슬롯
            trial.startingSkillR = SkillId.HealingLight; // 시험 R 슬롯
            trial.slimeCount = 1; // 시험 슬라임 수
            trial.slimeHealth = 50; // 시험 슬라임 체력
            trial.slimeDefense = 2; // 시험 슬라임 방어력
            trial.slimeSpeed = 0f; // 시험 슬라임 접근 중지
            trial.goblinCount = 1; // 시험 고블린 수
            trial.goblinHealth = 40; // 시험 고블린 체력
            trial.goblinDefense = 1; // 시험 고블린 방어력
            trial.goblinSpeed = 0f; // 시험 고블린 접근 중지
            SetNumber("skeletonCount", 1f); // 시험 궁수 수
            SetNumber("skeletonHealth", 35f); // 기획 궁수 체력
            SetNumber("skeletonAttack", 9f); // 기획 궁수 공격력
            SetNumber("skeletonDefense", 1f); // 기획 궁수 방어력
            SetNumber("skeletonSpeed", 0f); // 시험 궁수 이동 중지
            SetNumber("skeletonAttackRange", 0f); // 시작 전 궁수 공격 중지
            SetNumber("skeletonWindup", 0.25f); // 시험 궁수 예고 시간
            SetNumber("skeletonAttackInterval", 2f); // 시험 궁수 공격 간격
            SetNumber("skeletonProjectileSpeed", 14f); // 시험 적 화살 속도
            SetNumber("skeletonProjectileRange", 6f); // 시험 적 화살 사거리
            SetNumber("orcCount", 1f); // 시험 오크 수
            SetNumber("orcHealth", 100f); // 기획 오크 체력
            SetNumber("orcAttack", 15f); // 기획 오크 공격력
            SetNumber("orcDefense", 5f); // 기획 오크 방어력
            SetNumber("orcSpeed", 0f); // 시험 오크 이동 중지
            SetNumber("orcAttackRange", 1.4f); // 시험 강타 거리
            SetNumber("orcWindup", 0.35f); // 시험 강타 예고
            SetNumber("orcAttackInterval", 2f); // 시험 강타 간격
            SetNumber("orcAttackAngle", 120f); // 시험 강타 각도
            SetNumber("wolfCount", 1f); // 시험 늑대 수
            SetNumber("wolfHealth", 45f); // 기획 늑대 체력
            SetNumber("wolfAttack", 9f); // 기획 늑대 공격력
            SetNumber("wolfDefense", 1f); // 기획 늑대 방어력
            SetNumber("wolfSpeed", 0f); // 시험 늑대 접근 중지
            SetNumber("wolfAttackRange", 2.5f); // 시험 도약 시작 거리
            SetNumber("wolfWindup", 0.2f); // 시험 도약 예고
            SetNumber("wolfAttackInterval", 2f); // 시험 도약 간격
            SetNumber("wolfLeapDistance", 2.5f); // 시험 도약 거리
            SetNumber("wolfLeapSpeed", 10f); // 시험 도약 속도
            SetNumber("wolfHitDistance", 0.65f); // 시험 도약 적중 거리
            field.SetValue(battle, trial); // 전투 시작 전 복제 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
        yield return null; // 전투 생성 대기
        yield return null; // 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 시험 콜백 해제
        loaded = null; // 중복 해제 방지
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 장치와 설정 복원
    {
        if (loaded != null) // 남은 콜백 여부
        {
            SceneManager.sceneLoaded -= loaded; // 콜백 해제
        }
        if (mouse != null && mouse.added) // 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 시험 마우스 제거
        }
        if (keyboard != null && keyboard.added) // 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
        }
        if (trial != null) // 시험 설정 존재 여부
        {
            Object.Destroy(trial); // 복제 설정 제거
        }
        map.Dispose(); // 시험 맵 이벤트 정리
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = oldBehavior; // 기존 포커스 복원
        Application.runInBackground = oldBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 제거 대기
    }

    [UnityTest] // 기획 일반 적 5종의 생성 사례
    public IEnumerator AllFiveEnemiesUsePlannedHealthAndDefense() // 다섯 적의 공통 전투 상태 확인
    {
        AssertEnemy("Slime 1", 50, 2); // 돌진형 슬라임
        AssertEnemy("Goblin 1", 40, 1); // 빠른 근접 고블린
        AssertEnemy("Skeleton 1", 35, 1); // 원거리 스켈레톤
        AssertEnemy("Orc 1", 100, 5); // 중장갑 오크
        AssertEnemy("Wolf 1", 45, 1); // 도약형 늑대
        yield return null; // 시험 프레임 완료
    }

    [UnityTest] // 궁수의 예고와 원거리 피해 사례
    public IEnumerator ArcherWarnsThenFiresAndDamagesPlayerOnce() // 실제 적 화살 피해 검증
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Skeleton 1").transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.15f); // 예고 중 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 중 피해 방지
        yield return new WaitForSeconds(0.45f); // 화살 발사와 적중 대기
        Assert.That(player.Health, Is.EqualTo(94)); // 공통 피해식의 궁수 피해 6
        yield return new WaitForSeconds(0.25f); // 같은 화살의 추가 프레임
        Assert.That(player.Health, Is.EqualTo(94)); // 단일 화살 중복 피해 방지
    }

    [UnityTest] // 오크의 예고 후 강타 사례
    public IEnumerator OrcWarnsThenHitsWithHeavyMelee() // 부채꼴 강타 피해 검증
    {
        HideOtherEnemies("Orc 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Orc 1").transform.position = new Vector3(0.9f, 0f, 0f); // 강타 범위의 오크
        yield return new WaitForSeconds(0.2f); // 예고 중 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 중 피해 방지
        yield return new WaitForSeconds(0.25f); // 강타 시점 대기
        Assert.That(player.Health, Is.EqualTo(88)); // 공통 피해식의 오크 피해 12
    }

    [UnityTest] // 늑대의 예고 후 도약 사례
    public IEnumerator WolfWarnsAndLeapsWithOneHit() // 도약 이동과 단일 피해 검증
    {
        HideOtherEnemies("Wolf 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter wolf = Enemy("Wolf 1"); // 도약형 늑대
        wolf.transform.position = new Vector3(2f, 0f, 0f); // 도약 시작 거리
        yield return new WaitForSeconds(0.1f); // 예고 중 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 중 피해 방지
        Assert.That(wolf.transform.localScale.y, Is.LessThan(0.65f)); // 도약 전 몸을 낮추는 예고
        yield return new WaitForSeconds(0.45f); // 도약과 적중 대기
        Assert.That(player.Health, Is.EqualTo(94)); // 공통 피해식의 늑대 피해 6
        Assert.That(wolf.transform.position.x, Is.LessThan(0.8f)); // 고정 경로의 실제 도약
        Assert.That(wolf.transform.localScale.y, Is.EqualTo(0.7f).Within(0.01f)); // 도약 시작 후 기본 자세 복원
        yield return new WaitForSeconds(0.2f); // 도약 종료 후 추가 프레임
        Assert.That(player.Health, Is.EqualTo(94)); // 같은 도약의 중복 피해 방지
    }

    [UnityTest] // 궁수 화살의 벽 가림 사례
    public IEnumerator ArcherArrowCannotPassNonTriggerWall() // 벽의 적 화살 차단
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Skeleton 1").transform.position = new Vector3(3f, 0f, 0f); // 벽 뒤의 궁수
        GameObject wall = new GameObject("Archer Test Wall"); // 시험 전용 장애물
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = new Vector3(1.5f, 0f, 0f); // 궁수와 플레이어 사이
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2f); // 얇은 비트리거 벽
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.65f); // 예고와 화살 충돌 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 벽 뒤 플레이어 피해 차단
        Assert.That(GameObject.Find("Enemy Arrow"), Is.Null); // 벽에 닿은 적 화살 제거
        Object.Destroy(wall); // 시험 장애물 제거
    }

    [UnityTest] // 궁수의 거리 유지 사례
    public IEnumerator ArcherRetreatsWhenPlayerIsTooClose() // 근접 시 거리 유지 이동
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter skeleton = Enemy("Skeleton 1"); // 시험 궁수
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        skeleton.transform.position = new Vector3(1f, 0f, 0f); // 유지 거리 안의 궁수
        SetNumber("skeletonSpeed", 2f); // 시험 궁수 이동속도
        SetNumber("skeletonKeepDistance", 3f); // 시험 유지 거리
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.25f); // 후퇴 이동 대기
        Assert.That(skeleton.transform.position.x, Is.GreaterThan(1.3f)); // 가까운 플레이어에게서 후퇴
        Assert.That(player.Health, Is.EqualTo(100)); // 후퇴 중 발사 방지
    }

    [UnityTest] // 적 화살에 대한 회피 무적 사례
    public IEnumerator DodgeInvulnerabilityBlocksArcherArrow() // 실제 회피 무적과 적 화살 피해
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Skeleton 1").transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift)); // 실제 회피 입력
        yield return null; // 무적 시작 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 회피 키 해제
        yield return new WaitForSeconds(0.6f); // 궁수 화살 적중 시점 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 무적 중 화살 피해 차단
    }

    [UnityTest] // 궁수의 고정 발사 방향 사례
    public IEnumerator ArcherKeepsWindupDirectionWhenPlayerMoves() // 예고 후 조준 방향 고정
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Skeleton 1").transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.1f); // 화살 방향 예고 대기
        player.transform.position = new Vector3(0f, 2f, 0f); // 예고된 경로에서 이탈
        yield return new WaitForSeconds(0.55f); // 발사와 통과 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 이전 방향 화살의 이탈 대상 피해 방지
    }

    [UnityTest] // 궁수 사망 후 비행 중 화살 사례
    public IEnumerator FiredArrowContinuesAfterArcherDies() // 발사된 적 화살의 독립 상태
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter skeleton = Enemy("Skeleton 1"); // 시험 궁수
        skeleton.transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        SetNumber("skeletonProjectileSpeed", 2f); // 화살의 느린 시험 속도
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.35f); // 화살 발사 대기
        Assert.That(GameObject.Find("Enemy Arrow"), Is.Not.Null); // 실제 비행 중 화살
        skeleton.TakeDamage(35); // 발사한 궁수 사망
        yield return new WaitForSeconds(1.5f); // 느린 화살의 적중 대기
        Assert.That(player.Health, Is.EqualTo(94)); // 발사 후 궁수 사망과 독립적인 피해
    }

    [UnityTest] // 적 화살의 플레이어 사망 사례
    public IEnumerator ArcherArrowCanEndCombatAndDisplayGameOver() // 원거리 피해와 사망 화면 연결
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Skeleton 1").transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        player.TakeDamage(94); // 적 화살 한 번에 사망할 체력
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        float deadline = Time.time + 2f; // 화살 적중의 최대 시험 대기 시간
        while (!player.Dead && Time.time < deadline) // 실제 적중 또는 시간 초과까지 대기
        {
            yield return null; // 다음 전투 프레임 대기
        }
        Assert.That(player.Dead, Is.True); // 적 화살 피해로 플레이어 사망
        Assert.That(GameObject.Find("Game Over"), Is.Not.Null); // 사망 화면 활성화
    }

    [UnityTest] // 재시작의 적 화살과 적 상태 사례
    public IEnumerator RestartClearsEnemyArrowAndRecreatesEnemyRoster() // 이전 화살 제거와 적 체력 초기화
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        Fighter oldSkeleton = Enemy("Skeleton 1"); // 이전 전투의 궁수
        oldSkeleton.transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 궁수 위치
        SetNumber("skeletonProjectileSpeed", 2f); // 재시작 전에 남을 느린 화살
        SetNumber("skeletonAttackRange", 6f); // 궁수 공격 허용
        yield return new WaitForSeconds(0.35f); // 적 화살 발사 대기
        Assert.That(GameObject.Find("Enemy Arrow"), Is.Not.Null); // 이전 전투의 비행 중 화살
        PrototypeSettings savedTrial = trial; // 재시작할 독립 설정
        UnityAction<Scene, LoadSceneMode> reset = (scene, mode) => // 새 씬의 시험 설정 연결
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, savedTrial); // 복제 설정 연결
        };
        SceneManager.sceneLoaded += reset; // 재시작 설정 콜백 등록
        try // 성공과 실패 시 콜백 해제 보장
        {
            SceneManager.LoadScene("CombatPrototype"); // 실제 전투 재시작
            yield return null; // 씬 생성 대기
            yield return null; // 초기화 완료 대기
            Assert.That(GameObject.Find("Enemy Arrow"), Is.Null); // 이전 적 화살 제거
            Fighter newSkeleton = Enemy("Skeleton 1"); // 새 전투의 궁수
            Assert.That(newSkeleton, Is.Not.SameAs(oldSkeleton)); // 이전 적 객체 제거
            Assert.That(newSkeleton.Health, Is.EqualTo(35)); // 궁수 체력 초기화
            Assert.That(Enemy("Orc 1").Health, Is.EqualTo(100)); // 오크 체력 초기화
            Assert.That(Enemy("Wolf 1").Health, Is.EqualTo(45)); // 늑대 체력 초기화
        }
        finally // 시험 콜백 정리
        {
            SceneManager.sceneLoaded -= reset; // 재시작 설정 콜백 해제
        }
    }

    [UnityTest] // 오크의 벽 가림과 회피 사례
    public IEnumerator OrcHeavyHitCannotPassWall() // 강타의 장애물 차단
    {
        HideOtherEnemies("Orc 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Orc 1").transform.position = new Vector3(0.9f, 0f, 0f); // 강타 거리의 오크
        GameObject wall = new GameObject("Orc Test Wall"); // 시험 전용 장애물
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = new Vector3(0.45f, 0f, 0f); // 적과 플레이어 사이
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2f); // 얇은 비트리거 벽
        yield return new WaitForSeconds(0.45f); // 예고와 강타 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 벽 뒤 플레이어 피해 차단
        Object.Destroy(wall); // 시험 장애물 제거
    }

    [UnityTest] // 오크 강타의 방향 이탈 사례
    public IEnumerator OrcHeavyHitKeepsWarnedDirection() // 예고 중 반대 방향으로 이탈
    {
        HideOtherEnemies("Orc 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Enemy("Orc 1").transform.position = new Vector3(0.9f, 0f, 0f); // 강타 거리의 오크
        yield return new WaitForSeconds(0.1f); // 강타 방향 예고 대기
        player.transform.position = new Vector3(2f, 0f, 0f); // 오크의 예고 방향 반대편
        yield return new WaitForSeconds(0.35f); // 강타 시점 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 범위 밖 피해 방지
    }

    [UnityTest] // 늑대 도약 방향과 이탈 사례
    public IEnumerator WolfLeapKeepsWarnedDirectionWhenPlayerMoves() // 예고된 경로에서 벗어난 플레이어
    {
        HideOtherEnemies("Wolf 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter wolf = Enemy("Wolf 1"); // 시험 늑대
        wolf.transform.position = new Vector3(2f, 0f, 0f); // 도약 시작 거리
        yield return new WaitForSeconds(0.1f); // 도약 경로 예고 대기
        Transform path = wolf.transform.Find("Wolf Path"); // 실제 경로 예고 표시
        Assert.That(path, Is.Not.Null); // 경로 표시 존재
        Assert.That(path.gameObject.activeSelf, Is.True); // 예고 중 경로 표시
        player.transform.position = new Vector3(0f, 2f, 0f); // 예고된 경로에서 이탈
        yield return new WaitForSeconds(0.45f); // 고정 방향 도약 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 경로 밖 대상의 피해 방지
        Assert.That(wolf.transform.position.y, Is.EqualTo(0f).Within(0.05f)); // 예고한 직선 방향 유지
    }

    [UnityTest] // 늑대의 대각선 도약 경로 사례
    public IEnumerator WolfCrouchPreservesDiagonalPathDirection() // 낮아진 본체와 대각선 예고 일치
    {
        HideOtherEnemies("Wolf 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter wolf = Enemy("Wolf 1"); // 시험 늑대
        wolf.transform.position = new Vector3(2f, 2f, 0f); // 대각선 도약 시작 위치
        SetNumber("wolfAttackRange", 4f); // 대각선 시험의 도약 시작 거리
        yield return new WaitForSeconds(0.1f); // 몸 낮추기와 경로 예고 대기
        Transform path = wolf.transform.Find("Wolf Path"); // 늑대의 경로 표시
        Assert.That(path.gameObject.activeSelf, Is.True); // 경로 예고 활성화
        Vector2 from = path.TransformPoint(new Vector3(-0.5f, 0f, 0f)); // 표시 시작점의 월드 위치
        Vector2 to = path.TransformPoint(new Vector3(0.5f, 0f, 0f)); // 표시 끝점의 월드 위치
        Vector2 direction = (Vector2)(player.transform.position - wolf.transform.position); // 플레이어 방향
        Assert.That(Vector2.Angle(to - from, direction), Is.LessThan(1f)); // 낮아진 본체에서도 실제 도약 방향 유지
    }

    [UnityTest] // 늑대의 예고 중 사망 사례
    public IEnumerator WolfDeathDuringWindupCancelsLeap() // 사망 시 도약과 표시 중단
    {
        HideOtherEnemies("Wolf 1"); // 시험 대상 외 적 격리
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        Fighter wolf = Enemy("Wolf 1"); // 시험 늑대
        wolf.transform.position = new Vector3(2f, 0f, 0f); // 도약 시작 위치
        yield return new WaitForSeconds(0.1f); // 도약 예고 대기
        Transform path = wolf.transform.Find("Wolf Path"); // 실제 경로 예고 표시
        Assert.That(path.gameObject.activeSelf, Is.True); // 예고 표시 확인
        wolf.TakeDamage(45); // 예고 중 늑대 사망
        Assert.That(path.gameObject.activeSelf, Is.False); // 사망 직후 경로 표시 제거
        yield return new WaitForSeconds(0.4f); // 기존 도약 시점 경과
        Assert.That(player.Health, Is.EqualTo(100)); // 사망한 늑대의 도약 피해 방지
        Assert.That(wolf.transform.position.x, Is.EqualTo(2f).Within(0.01f)); // 사망 후 도약 이동 방지
    }

    [UnityTest] // 공통 회오리의 다섯 적 사례
    public IEnumerator WhirlwindDamagesAllFiveEnemyTypes() // 공통 적 목록과 방어력 계산 검증
    {
        Fighter slime = Enemy("Slime 1"); // 돌진형 슬라임
        Fighter goblin = Enemy("Goblin 1"); // 빠른 근접 고블린
        Fighter skeleton = Enemy("Skeleton 1"); // 원거리 궁수
        Fighter orc = Enemy("Orc 1"); // 중장갑 전사
        Fighter wolf = Enemy("Wolf 1"); // 도약형 늑대
        slime.transform.position = new Vector3(1f, 0f, 0f); // 반경 안의 슬라임
        goblin.transform.position = new Vector3(-1f, 0f, 0f); // 반경 안의 고블린
        skeleton.transform.position = new Vector3(0f, 1f, 0f); // 반경 안의 궁수
        orc.transform.position = new Vector3(0f, -1f, 0f); // 반경 안의 오크
        wolf.transform.position = new Vector3(1f, 1f, 0f); // 반경 안의 늑대
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 회오리 입력
        yield return null; // 범위 피해 처리 대기
        Assert.That(slime.Health, Is.EqualTo(23)); // 슬라임 피해 27
        Assert.That(goblin.Health, Is.EqualTo(12)); // 고블린 피해 28
        Assert.That(skeleton.Health, Is.EqualTo(7)); // 궁수 피해 28
        Assert.That(orc.Health, Is.EqualTo(74)); // 오크 방어력 반영 피해 26
        Assert.That(wolf.Health, Is.EqualTo(17)); // 늑대 피해 28
    }

    [UnityTest] // 검의 오크 타격 사례
    public IEnumerator SwordDamagesArmoredOrc() // 오크 방어력과 검 피해 검증
    {
        HideOtherEnemies("Orc 1"); // 시험 대상 외 적 격리
        Fighter orc = Enemy("Orc 1"); // 시험 오크
        orc.transform.position = new Vector3(1f, 0f, 0f); // 검 사거리 안의 오크
        yield return AttackRight(WeaponType.Sword); // 오른쪽 검 공격
        Assert.That(orc.Health, Is.EqualTo(81)); // 검 공격력 22와 오크 방어력 5의 피해 19
    }

    [UnityTest] // 활의 궁수 적중 사례
    public IEnumerator BowDamagesSkeletonArcher() // 궁수 적 계층과 화살 피해 검증
    {
        HideOtherEnemies("Skeleton 1"); // 시험 대상 외 적 격리
        trial.bowAttack = 10; // 시험 활 공격력
        trial.bowProjectileSpeed = 14f; // 시험 화살 속도
        trial.bowRange = 7f; // 시험 활 사거리
        Fighter skeleton = Enemy("Skeleton 1"); // 시험 궁수
        skeleton.transform.position = new Vector3(2f, 0f, 0f); // 오른쪽 화살 경로
        yield return AttackRight(WeaponType.Bow); // 오른쪽 화살 발사
        yield return new WaitForSeconds(0.3f); // 적 화살 충돌 대기
        Assert.That(skeleton.Health, Is.EqualTo(16)); // 활 공격력 20과 방어력 1의 피해 19
    }

    [UnityTest] // 지팡이 폭발의 새 적 세 종류 사례
    public IEnumerator StaffExplosionDamagesNewEnemyTypes() // 공통 폭발 대상과 방어력 검증
    {
        Enemy("Slime 1").TakeDamage(10000); // 기존 슬라임 간섭 제거
        Enemy("Goblin 1").TakeDamage(10000); // 기존 고블린 간섭 제거
        trial.staffAttack = 8; // 시험 지팡이 공격력
        trial.staffRange = 5.5f; // 시험 마법탄 사거리
        trial.staffProjectileSpeed = 9f; // 시험 마법탄 속도
        trial.staffExplosionRadius = 1.1f; // 시험 폭발 반경
        trial.staffExplosionTargets = 4; // 시험 폭발 최대 대상
        Fighter skeleton = Enemy("Skeleton 1"); // 마법탄 경로의 궁수
        Fighter orc = Enemy("Orc 1"); // 폭발 범위의 오크
        Fighter wolf = Enemy("Wolf 1"); // 폭발 범위의 늑대
        skeleton.transform.position = new Vector3(2f, 0f, 0f); // 직접 충돌 위치
        orc.transform.position = new Vector3(2f, 0.6f, 0f); // 폭발 위쪽 위치
        wolf.transform.position = new Vector3(2f, -0.6f, 0f); // 폭발 아래쪽 위치
        yield return AttackRight(WeaponType.Staff); // 오른쪽 마법탄 발사
        yield return new WaitForSeconds(0.4f); // 폭발 처리 대기
        Assert.That(skeleton.Health, Is.EqualTo(18)); // 궁수에게 폭발 피해 17
        Assert.That(orc.Health, Is.EqualTo(85)); // 오크에게 폭발 피해 15
        Assert.That(wolf.Health, Is.EqualTo(28)); // 늑대에게 폭발 피해 17
    }

    [UnityTest] // 적 종류별 시험 전리품 분리 사례
    public IEnumerator OnlySlimeProducesExistingTestLoot() // 다른 적에 슬라임 점액 미지급
    {
        Enemy("Goblin 1").TakeDamage(10000); // 고블린 사망
        Enemy("Skeleton 1").TakeDamage(10000); // 궁수 사망
        Enemy("Orc 1").TakeDamage(10000); // 오크 사망
        Enemy("Wolf 1").TakeDamage(10000); // 늑대 사망
        Assert.That(GameObject.Find("Slime Loot"), Is.Null); // 다른 적의 슬라임 전리품 미지급
        Enemy("Slime 1").TakeDamage(10000); // 슬라임 사망
        Assert.That(GameObject.Find("Slime Loot"), Is.Not.Null); // 기존 슬라임 시험 전리품 보존
        yield return null; // 시험 프레임 완료
    }

    private void SetNumber(string name, float value) // 선택적 새 설정의 시험 값
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 신규 설정 필드 검색
        if (field != null) // 구현 전후 필드 확인
        {
            field.SetValue(trial, Convert.ChangeType(value, field.FieldType)); // 독립 설정의 수치 적용
        }
    }

    private IEnumerator AttackRight(WeaponType weapon) // 실제 전투의 오른쪽 무기 공격
    {
        PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 현재 전투 구성 요소
        typeof(PrototypeBootstrap).GetField("equippedWeapon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, weapon); // 시험 무기 장착
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 오른쪽 조준 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 마우스 조준
        yield return null; // 조준 반영 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 실제 공격 누름
        yield return null; // 공격 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 마우스 놓기
    }

    private static Fighter Enemy(string name) // 이름으로 적의 전투 상태 확인
    {
        GameObject target = GameObject.Find(name); // 실제 씬의 적
        Assert.That(target, Is.Not.Null, name + " 생성 필요"); // 적 생성 확인
        return target.GetComponent<Fighter>(); // 적 전투 상태
    }

    private static void AssertEnemy(string name, int health, int defense) // 적의 기획 능력치 확인
    {
        Fighter fighter = Enemy(name); // 실제 전투 대상
        Assert.That(fighter.Health, Is.EqualTo(health), name); // 기획 체력
        Assert.That(fighter.Defense, Is.EqualTo(defense), name); // 기획 방어력
        Assert.That(fighter.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("PhiEnemy")), name); // 공통 적 충돌 계층
    }

    private static void HideOtherEnemies(string selected) // 시험에 필요 없는 적 격리
    {
        foreach (string name in new[] { "Slime 1", "Goblin 1", "Skeleton 1", "Orc 1", "Wolf 1" }) // 다섯 적 순회
        {
            if (name != selected) // 시험 대상이 아닌 적 여부
            {
                Enemy(name).TakeDamage(10000); // 다른 적의 공격과 이동 중지
            }
        }
    }
}
