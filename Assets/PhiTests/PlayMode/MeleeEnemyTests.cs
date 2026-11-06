using System; // 동적 설정 자료형
using System.Collections; // 프레임 단위 시험
using System.Reflection; // 기존 설정 참조
using NUnit.Framework; // 시험 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using Object = UnityEngine.Object; // 유니티 객체 구분

public class MeleeEnemyTests // 근접형과 돌진형 적의 전투 검증
{
    private Mouse mouse; // 시험 마우스
    private Keyboard keyboard; // 시험 키보드
    private SceneMapTestFixture map; // 독립 씬 맵
    private PrototypeSettings trial; // 사용자 설정의 시험 복제본
    private UnityAction<Scene, LoadSceneMode> loaded; // 시험 설정 연결
    private bool oldBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior oldBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior; // 기존 입력 경로

    [UnitySetUp] // 각 시험의 전투 준비
    public IEnumerator SetUp() // 실제 전투 씬과 독립 설정
    {
        oldBackground = Application.runInBackground; // 기존 실행 설정 저장
        oldBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 저장
        oldEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 저장
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 유지
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 시험 입력 경로
        mouse = InputSystem.AddDevice<Mouse>(); // 시험 마우스
        keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
        map = new SceneMapTestFixture(); // 씬 맵 인스턴스 격리
        loaded = (scene, mode) => // 씬 시작 전 시험 설정
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 참조
            trial = Object.Instantiate((PrototypeSettings)field.GetValue(battle)); // 사용자 설정의 독립 복제
            trial.playerHealth = 100; // 시험 플레이어 체력
            trial.playerAttack = 10; // 시험 기본 공격력
            trial.playerDefense = 5; // 시험 플레이어 방어력
            trial.playerMana = 50; // 시험 최대 마나
            trial.fireballCost = 10; // 시험 화염탄 비용
            trial.whirlwindCost = 14; // 시험 회오리 비용
            trial.playerStamina = 100; // 시험 최대 스태미나
            trial.dodgeDuration = 1f; // 시험 무적 유지 시간
            trial.dodgeInvulnerability = 1f; // 시험 회피 무적 시간
            trial.dodgeDistance = 0f; // 시험 중 플레이어 위치 유지
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.swordAttack = 12; // 시험 검 공격력
            trial.swordRange = 1.7f; // 시험 검 사거리
            trial.swordAngle = 90f; // 시험 검 판정 각도
            trial.swordTargets = 3; // 시험 검 최대 대상
            trial.slimeCount = 1; // 비교할 슬라임 수
            trial.slimeHealth = 50; // 시험 슬라임 체력
            trial.slimeDefense = 2; // 시험 슬라임 방어력
            trial.slimeSpeed = 1.5f; // 시험 슬라임 속도
            trial.startingSkillQ = SkillId.Fireball; // 시험 화염탄 슬롯
            trial.startingSkillE = SkillId.WhirlwindSlash; // 시험 회오리 슬롯
            trial.startingSkillR = SkillId.HealingLight; // 시험 치유 슬롯
            SetNumber("goblinCount", 1f); // 시험 근접 고블린 수
            SetNumber("goblinHealth", 40f); // 시험 고블린 체력
            SetNumber("goblinAttack", 10f); // 시험 고블린 공격력
            SetNumber("goblinDefense", 1f); // 시험 고블린 방어력
            SetNumber("goblinSpeed", 2.5f); // 시험 고블린 접근 속도
            SetNumber("goblinAttackRange", 1f); // 시험 근접 공격 거리
            SetNumber("goblinWindup", 0.35f); // 시험 근접 공격 예고
            SetNumber("goblinAttackInterval", 1f); // 시험 공격 간격
            SetNumber("goblinAttackAngle", 90f); // 시험 근접 공격 각도
            field.SetValue(battle, trial); // 전투 시작 전 복제 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
        yield return null; // 전투 생성 대기
        yield return null; // 입력과 행동 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 콜백 중복 해제
        loaded = null; // 정리 상태 표시
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 시험 자원과 입력 설정 복원
    {
        if (loaded != null) // 남은 씬 콜백 여부
        {
            SceneManager.sceneLoaded -= loaded; // 씬 콜백 해제
        }
        if (mouse != null && mouse.added) // 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 시험 마우스 제거
        }
        if (keyboard != null && keyboard.added) // 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
        }
        if (trial != null) // 독립 설정 존재 여부
        {
            Object.Destroy(trial); // 시험 설정 제거
        }
        map.Dispose(); // 시험 맵 이벤트 정리
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = oldBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = oldBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 제거 대기
    }

    [UnityTest] // 두 유형의 능력치와 접근 사례
    public IEnumerator GoblinUsesPlannedStatsAndApproachesFasterThanSlime() // 기존 돌진형과 빠른 근접형 구분
    {
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Fighter slime = Enemy("Slime 1"); // 돌진 슬라임
        Assert.That(goblin.Health, Is.EqualTo(40)); // 기획 고블린 체력
        Assert.That(goblin.Defense, Is.EqualTo(1)); // 기획 고블린 방어력
        goblin.transform.position = new Vector3(3f, 0f, 0f); // 오른쪽 같은 거리
        slime.transform.position = new Vector3(-3f, 0f, 0f); // 왼쪽 같은 거리
        yield return new WaitForSeconds(0.25f); // 두 적의 접근 대기
        Assert.That(Mathf.Abs(goblin.transform.position.x), Is.LessThan(Mathf.Abs(slime.transform.position.x) - 0.1f)); // 고블린의 빠른 접근
    }

    [UnityTest] // 근접 베기 예고와 피해 사례
    public IEnumerator GoblinWarnsBeforeSingleMeleeHit() // 예고와 근접 피해 검증
    {
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Enemy("Slime 1").transform.position = new Vector3(-5f, 0f, 0f); // 슬라임 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 시험 플레이어
        goblin.transform.position = new Vector3(0.8f, 0f, 0f); // 근접 사거리 안에 배치
        yield return null; // 공격 예고 시작
        Transform weapon = goblin.transform.Find("Goblin Weapon"); // 공격 예고 도형
        Assert.That(weapon, Is.Not.Null); // 고블린 무기 표시
        Assert.That(Quaternion.Angle(weapon.localRotation, Quaternion.identity), Is.GreaterThan(45f)); // 들어 올린 무기 예고
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 중 피해 없음
        yield return new WaitForSeconds(0.2f); // 예고가 끝나기 전 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 선행 예고 유지
        yield return new WaitForSeconds(0.23f); // 베기 처리 대기
        Assert.That(player.Health, Is.EqualTo(93)); // 공통 피해식의 단일 피해 7
        Assert.That(Quaternion.Angle(weapon.localRotation, Quaternion.identity), Is.LessThan(0.1f)); // 베기 후 무기 자세 복원
        yield return new WaitForSeconds(0.2f); // 같은 공격의 추가 프레임
        Assert.That(player.Health, Is.EqualTo(93)); // 공격당 중복 피해 방지
    }

    [UnityTest] // 실제 검의 두 적 공격 사례
    public IEnumerator SwordDamagesGoblinAndSlimeOnceEach() // 공통 근접 판정 검증
    {
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Fighter slime = Enemy("Slime 1"); // 돌진 슬라임
        goblin.transform.position = new Vector3(0.9f, 0.2f, 0f); // 검 범위의 고블린
        slime.transform.position = new Vector3(1.2f, -0.2f, 0f); // 검 범위의 슬라임
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(2f, 0f, 0f)); // 오른쪽 조준 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 실제 검 공격
        yield return null; // 피해 처리 대기
        Assert.That(goblin.Health, Is.EqualTo(19)); // 고블린에게 검 피해 21
        Assert.That(slime.Health, Is.EqualTo(29)); // 슬라임에게 검 피해 21
    }

    [UnityTest] // 활의 고블린 우선 적중 사례
    public IEnumerator BowHitsGoblinBeforeSlime() // 공통 적 계층의 최초 충돌 검증
    {
        trial.bowAttack = 10; // 시험 활 공격력
        trial.bowRange = 7f; // 시험 활 사거리
        trial.bowProjectileSpeed = 14f; // 시험 화살 속도
        SetNumber("goblinSpeed", 0f); // 시험 중 고블린 이동 차단
        trial.slimeSpeed = 0f; // 시험 중 슬라임 이동 차단
        Fighter goblin = Enemy("Goblin 1"); // 가까운 고블린
        Fighter slime = Enemy("Slime 1"); // 고블린 뒤 슬라임
        goblin.transform.position = new Vector3(2f, 0f, 0f); // 첫 번째 충돌 위치
        slime.transform.position = new Vector3(4f, 0f, 0f); // 뒤쪽 충돌 위치
        SetWeapon(WeaponType.Bow); // 실제 전투의 활 장착
        yield return AttackRight(); // 오른쪽으로 화살 발사
        yield return new WaitForSeconds(0.4f); // 화살 충돌 대기
        Assert.That(goblin.Health, Is.EqualTo(21)); // 고블린에게 활 피해 19
        Assert.That(slime.Health, Is.EqualTo(50)); // 최초 대상 뒤의 피해 방지
    }

    [UnityTest] // 지팡이 폭발의 두 적 사례
    public IEnumerator StaffExplosionDamagesBothEnemyTypes() // 공통 폭발 대상 검증
    {
        trial.staffAttack = 8; // 시험 지팡이 공격력
        trial.staffRange = 5.5f; // 시험 마법탄 사거리
        trial.staffProjectileSpeed = 9f; // 시험 마법탄 속도
        trial.staffExplosionRadius = 1.1f; // 시험 폭발 반경
        trial.staffExplosionTargets = 4; // 시험 폭발 최대 대상
        SetNumber("goblinSpeed", 0f); // 시험 중 고블린 이동 차단
        trial.slimeSpeed = 0f; // 시험 중 슬라임 이동 차단
        Fighter goblin = Enemy("Goblin 1"); // 폭발의 첫 적
        Fighter slime = Enemy("Slime 1"); // 폭발 범위의 다른 적
        goblin.transform.position = new Vector3(2f, 0f, 0f); // 마법탄 경로의 고블린
        slime.transform.position = new Vector3(2f, 0.6f, 0f); // 폭발 범위의 슬라임
        SetWeapon(WeaponType.Staff); // 실제 전투의 지팡이 장착
        yield return AttackRight(); // 오른쪽 마법탄 발사
        yield return new WaitForSeconds(0.4f); // 충돌과 폭발 대기
        Assert.That(goblin.Health, Is.EqualTo(23)); // 고블린에게 폭발 피해 17
        Assert.That(slime.Health, Is.EqualTo(33)); // 슬라임에게 폭발 피해 17
    }

    [UnityTest] // 회오리의 두 적 사례
    public IEnumerator WhirlwindDamagesBothEnemyTypes() // 공통 스킬 대상 검증
    {
        trial.whirlwindRadius = 2f; // 시험 회오리 반경
        trial.whirlwindMultiplier = 1.3f; // 시험 회오리 공격 배율
        SetNumber("goblinSpeed", 0f); // 시험 중 고블린 이동 차단
        trial.slimeSpeed = 0f; // 시험 중 슬라임 이동 차단
        Fighter goblin = Enemy("Goblin 1"); // 회오리 범위의 고블린
        Fighter slime = Enemy("Slime 1"); // 회오리 범위의 슬라임
        goblin.transform.position = new Vector3(1.3f, 0f, 0f); // 오른쪽 근접 적
        slime.transform.position = new Vector3(-1.4f, 0f, 0f); // 왼쪽 돌진 적
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 회오리 입력
        yield return null; // 범위 피해 처리 대기
        Assert.That(goblin.Health, Is.EqualTo(12)); // 고블린에게 회오리 피해 28
        Assert.That(slime.Health, Is.EqualTo(23)); // 슬라임에게 회오리 피해 27
    }

    [UnityTest] // 화염탄의 고블린 적중 사례
    public IEnumerator FireballDamagesGoblin() // 공용 스킬 투사체 검증
    {
        trial.fireballRange = 7f; // 시험 화염탄 사거리
        trial.fireballSpeed = 14f; // 시험 화염탄 속도
        trial.fireballMultiplier = 1.4f; // 시험 화염탄 피해 배율
        SetNumber("goblinSpeed", 0f); // 시험 중 고블린 이동 차단
        trial.slimeSpeed = 0f; // 시험 중 슬라임 이동 차단
        Fighter goblin = Enemy("Goblin 1"); // 화염탄 대상
        goblin.transform.position = new Vector3(2f, 0f, 0f); // 오른쪽 적 위치
        Enemy("Slime 1").transform.position = new Vector3(-4f, 0f, 0f); // 슬라임 간섭 제거
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 오른쪽 조준 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 조준 위치 변경
        yield return null; // 조준 반영 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); // 실제 화염탄 입력
        yield return new WaitForSeconds(0.4f); // 투사체 충돌 대기
        Assert.That(goblin.Health, Is.EqualTo(10)); // 고블린에게 화염탄 피해 30
    }

    [UnityTest] // 근접 베기 회피 사례
    public IEnumerator GoblinSlashMissesPlayerWhoLeavesWindupDirection() // 고정 방향과 적중 시점 거리 검증
    {
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Enemy("Slime 1").transform.position = new Vector3(-5f, 0f, 0f); // 슬라임 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 시험 플레이어
        goblin.transform.position = new Vector3(0.8f, 0f, 0f); // 공격 시작 거리
        yield return null; // 고블린 예고 시작
        player.transform.position = new Vector3(2f, 0f, 0f); // 예고 방향 반대로 이탈
        yield return new WaitForSeconds(0.4f); // 베기 처리 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 예고 방향 밖 피해 방지
    }

    [UnityTest] // 근접 베기의 벽 가림 사례
    public IEnumerator GoblinSlashCannotHitThroughObstacle() // 고블린과 플레이어 사이 장애물
    {
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Enemy("Slime 1").transform.position = new Vector3(-5f, 0f, 0f); // 슬라임 간섭 제거
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 시험 플레이어
        goblin.transform.position = new Vector3(0.8f, 0f, 0f); // 벽 뒤의 공격 거리
        GameObject wall = new GameObject("Goblin Test Wall"); // 시험 전용 장애물
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = new Vector3(0.4f, 0f, 0f); // 플레이어와 적 사이
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2f); // 얇은 비트리거 벽
        yield return new WaitForSeconds(0.45f); // 예고와 베기 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 장애물 뒤 피해 차단
        Object.Destroy(wall); // 시험 장애물 제거
    }

    [UnityTest] // 플레이어 무적과 적 사망 사례
    public IEnumerator GoblinRespectsInvulnerabilityAndDoesNotDropSlimeLoot() // 공통 피해 차단과 보상 분리
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 시험 플레이어
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Fighter slime = Enemy("Slime 1"); // 돌진 슬라임
        slime.transform.position = new Vector3(-5f, 0f, 0f); // 슬라임 공격 간섭 제거
        goblin.transform.position = new Vector3(0.8f, 0f, 0f); // 고블린 공격 거리
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift)); // 실제 회피 무적 시작
        yield return null; // 회피 입력 처리 대기
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 회피 키 해제
        yield return new WaitForSeconds(0.45f); // 고블린 예고와 베기 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 무적 중 고블린 피해 차단
        goblin.TakeDamage(40); // 고블린 사망 처리
        goblin.TakeDamage(40); // 동일 적에 중복 피해
        Assert.That(GameObject.Find("Slime Loot"), Is.Null); // 고블린에 슬라임 전리품 미지급
        slime.TakeDamage(50); // 슬라임 사망 처리
        Assert.That(GameObject.Find("Slime Loot"), Is.Not.Null); // 기존 슬라임 시험 전리품 유지
    }

    [UnityTest] // 고블린 공격에 의한 플레이어 사망 사례
    public IEnumerator GoblinCanDefeatPlayerAndDisplayGameOver() // 근접 피해와 사망 화면 연결
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 시험 플레이어
        Fighter goblin = Enemy("Goblin 1"); // 근접 고블린
        Enemy("Slime 1").transform.position = new Vector3(-5f, 0f, 0f); // 슬라임 간섭 제거
        goblin.transform.position = new Vector3(0.8f, 0f, 0f); // 근접 공격 거리
        player.TakeDamage(94); // 한 번의 고블린 공격으로 사망할 체력
        yield return new WaitForSeconds(0.45f); // 고블린 예고와 베기 대기
        Assert.That(player.Dead, Is.True); // 고블린 피해로 플레이어 사망
        Assert.That(GameObject.Find("Game Over"), Is.Not.Null); // 사망 화면 활성화
    }

    [UnityTest] // 전투 재시작의 고블린 초기화 사례
    public IEnumerator RestartCreatesFreshGoblinAndResetsHealth() // 재시작 시 고블린 상태 복원
    {
        Fighter oldGoblin = Enemy("Goblin 1"); // 이전 전투의 고블린
        oldGoblin.TakeDamage(20); // 이전 전투의 고블린 피해
        PrototypeSettings savedTrial = trial; // 재시작에 적용할 독립 설정
        UnityAction<Scene, LoadSceneMode> reset = (scene, mode) => // 새 씬의 시험 설정 연결
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 씬의 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, savedTrial); // 사용자 에셋과 분리된 시험 설정
        };
        SceneManager.sceneLoaded += reset; // 재시작 설정 콜백 등록
        try // 재시작 후 콜백 해제 보장
        {
            SceneManager.LoadScene("CombatPrototype"); // 실제 전투 재시작
            yield return null; // 새 전투 생성 대기
            yield return null; // 초기화 완료 대기
            Fighter newGoblin = Enemy("Goblin 1"); // 새 전투의 고블린
            Assert.That(newGoblin, Is.Not.SameAs(oldGoblin)); // 이전 고블린 객체 제거
            Assert.That(newGoblin.Health, Is.EqualTo(40)); // 새 고블린 체력 초기화
        }
        finally // 성공과 실패 시 콜백 해제
        {
            SceneManager.sceneLoaded -= reset; // 시험 콜백 제거
        }
    }

    private void SetWeapon(WeaponType weapon) // 시험 전투의 무기 선택
    {
        PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
        typeof(PrototypeBootstrap).GetField("equippedWeapon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, weapon); // 시험 무기의 실행 상태 적용
    }

    private IEnumerator AttackRight() // 오른쪽 방향의 실제 마우스 공격
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 오른쪽 조준 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 포인터 조준
        yield return null; // 조준 반영 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 일반 공격 누름
        yield return null; // 공격 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 마우스 놓기
    }

    private static Fighter Enemy(string name) // 이름으로 실제 적 상태 조회
    {
        GameObject target = GameObject.Find(name); // 전투 씬의 적 오브젝트
        Assert.That(target, Is.Not.Null, name + " 생성 필요"); // 적 생성 확인
        return target.GetComponent<Fighter>(); // 적 전투 상태
    }

    private void SetNumber(string name, float value) // 선택적 신규 설정의 시험 값
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 설정 필드 검색
        if (field != null) // 구현 전후 필드 존재 확인
        {
            field.SetValue(trial, Convert.ChangeType(value, field.FieldType)); // 독립 설정의 수치 적용
        }
    }
}
