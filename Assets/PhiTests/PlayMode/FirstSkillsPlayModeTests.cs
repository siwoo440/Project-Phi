using System; // 열거형과 자료형 변환
using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 오브젝트 목록
using System.Reflection; // 실행 상태와 설정 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.EventSystems; // 선택된 UI 확인
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 스킬 상태 표시
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class FirstSkillsPlayModeTests // 첫 세 스킬의 실제 입력과 전투 검증
{
    private Mouse mouse; // 가상 마우스
    private Keyboard keyboard; // 가상 키보드
    private PrototypeSettings trial; // 독립 시험 설정
    private PrototypeBootstrap bootstrap; // 현재 전투 구성 요소
    private SceneMapTestFixture mapFixture; // 씬 맵 시험 격리
    private UnityAction<Scene, LoadSceneMode> sceneCallback; // 시험 설정 연결 콜백
    private readonly List<GameObject> obstacles = new List<GameObject>(); // 임시 장애물 목록
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 입력 경로
    private string q = "Fireball"; // 시험 Q 스킬
    private string e = "WhirlwindSlash"; // 시험 E 스킬
    private string r = "HealingLight"; // 시험 R 스킬

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 가상 입력과 독립 전투 준비
    {
        q = "Fireball"; // 시험마다 기본 Q 복원
        e = "WhirlwindSlash"; // 시험마다 기본 E 복원
        r = "HealingLight"; // 시험마다 기본 R 복원
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        mapFixture = new SceneMapTestFixture(); // 저장된 맵 인스턴스의 시험 배치
        yield return LoadBattle(); // 독립 설정으로 전투 시작
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 시험 자원과 전역 입력 복원
    {
        if (sceneCallback != null) // 씬 로드 콜백 존재 여부
        {
            SceneManager.sceneLoaded -= sceneCallback; // 시험 콜백 해제
        }
        if (mouse != null && mouse.added) // 가상 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        if (keyboard != null && keyboard.added) // 가상 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
        }
        foreach (GameObject obstacle in obstacles) // 시험 장애물 순회
        {
            if (obstacle != null) // 장애물 존재 여부
            {
                Object.Destroy(obstacle); // 시험 장애물 제거
            }
        }
        if (mapFixture != null) // 맵 시험 격리 존재 여부
        {
            mapFixture.Dispose(); // 맵 이벤트와 그림 정리
        }
        if (trial != null) // 임시 설정 존재 여부
        {
            Object.Destroy(trial); // 독립 시험 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 화염탄의 최초 적 사례
    public IEnumerator FireballHitsFirstEnemyForTwentyNine() // 실제 Q 입력과 비관통 피해 검증
    {
        Fighter first = PlaceSlime(1, new Vector2(2f, 0f)); // 가까운 첫 적
        Fighter second = PlaceSlime(2, new Vector2(4f, 0f)); // 첫 적 뒤의 두 번째 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        yield return AimAt(new Vector2(5f, 0f)); // 오른쪽 조준
        Press(Key.Q); // Q 스킬 입력
        yield return new WaitForSeconds(0.4f); // 투사체 충돌 대기
        Assert.That(first.Health, Is.EqualTo(21)); // 검 공격력 기준 29 피해
        Assert.That(second.Health, Is.EqualTo(50)); // 최초 적 뒤의 피해 방지
    }

    [UnityTest] // 화염탄 장애물 사례
    public IEnumerator FireballCannotPassNonTriggerWall() // 씬 장애물에 막히는 투사체 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 장애물 뒤의 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateWall(new Vector2(1.5f, 0f)); // 적 앞의 비트리거 장애물
        yield return AimAt(new Vector2(4f, 0f)); // 벽 방향 조준
        Press(Key.Q); // 화염탄 입력
        yield return new WaitForSeconds(0.4f); // 벽 충돌 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 벽 뒤 적의 피해 차단
    }

    [UnityTest] // 회오리의 주변 대상 사례
    public IEnumerator WhirlwindHitsEachNearbyEnemyOnce() // 실제 E 입력과 범위 피해 검증
    {
        Fighter first = PlaceSlime(1, new Vector2(1.5f, 0f)); // 범위 안의 첫 적
        Fighter second = PlaceSlime(2, new Vector2(0f, 1.5f)); // 범위 안의 두 번째 적
        Fighter outside = PlaceSlime(3, new Vector2(-3f, 0f)); // 범위 밖의 적
        Press(Key.E); // E 스킬 입력
        yield return null; // 공격 처리 대기
        yield return null; // 추가 프레임의 중복 적중 확인
        Assert.That(first.Health, Is.EqualTo(23)); // 첫 적의 27 피해
        Assert.That(second.Health, Is.EqualTo(23)); // 두 번째 적의 27 피해
        Assert.That(outside.Health, Is.EqualTo(50)); // 범위 밖 피해 방지
    }

    [UnityTest] // 회오리 벽 가림 사례
    public IEnumerator WhirlwindDoesNotHitThroughWall() // 범위 안 장애물 뒤 대상 보호
    {
        Fighter target = PlaceSlime(1, new Vector2(1.5f, 0f)); // 장애물 뒤의 범위 안 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        CreateWall(new Vector2(0.75f, 0f)); // 플레이어와 적 사이의 벽
        Press(Key.E); // 회오리 입력
        yield return null; // 피해 판정 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 벽 뒤 적의 피해 차단
    }

    [UnityTest] // 치유량과 최대 체력 사례
    public IEnumerator HealingRestoresFifteenAndNeverExceedsMaximum() // 실제 R 입력의 회복량 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        player.TakeDamage(10); // 최대 체력보다 10 낮은 상태
        Press(Key.R); // 치유 스킬 입력
        yield return null; // 치유 처리 대기
        Assert.That(player.Health, Is.EqualTo(100)); // 15 회복의 최대 체력 제한
        Assert.That(Cooldown(2), Is.GreaterThan(19f)); // 치유 성공 시 20초 대기
        Assert.That(Mana(), Is.LessThan(33f)); // 치유 성공 시 18 마나 소모
    }

    [UnityTest] // 정확한 치유량 사례
    public IEnumerator HealingRestoresFifteenPercentOfMaximumHealth() // 기획 치유 수치 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        player.TakeDamage(20); // 현재 체력 80
        Press(Key.R); // 치유 스킬 입력
        yield return null; // 치유 처리 대기
        Assert.That(player.Health, Is.EqualTo(95)); // 최대 체력 100의 15% 회복
    }

    [UnityTest] // 만피 치유 실패 사례
    public IEnumerator HealingAtFullHealthPreservesManaAndCooldown() // 회복할 체력이 없을 때의 자원 보존
    {
        Press(Key.R); // 만피 상태의 치유 입력
        yield return null; // 입력 처리 대기
        Assert.That(Mana(), Is.EqualTo(50f).Within(0.01f)); // 만피 시 마나 보존
        Assert.That(Cooldown(2), Is.EqualTo(0f)); // 만피 시 대기시간 미시작
    }

    [UnityTest] // 동시 입력과 독립 대기시간 사례
    public IEnumerator SimultaneousSkillsSpendManaAndStartIndependentCooldowns() // Q와 E의 실제 동시 발동
    {
        PlaceSlime(1, new Vector2(-4f, 0f)); // 적의 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 1f)); // 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -1f)); // 적의 간섭 제거
        yield return AimAt(new Vector2(4f, 0f)); // 빈 공간 조준
        Press(Key.Q, Key.E); // 한 프레임의 두 스킬 입력
        yield return null; // 스킬 발동 처리 대기
        Assert.That(Mana(), Is.InRange(26f, 27f)); // 10과 14 마나 차감
        Assert.That(Cooldown(0), Is.GreaterThan(4f)); // Q 독립 대기시간 시작
        Assert.That(Cooldown(1), Is.GreaterThan(8f)); // E 독립 대기시간 시작
        Assert.That(Cooldown(2), Is.EqualTo(0f)); // 사용하지 않은 R의 대기시간 유지
        Text status = GameObject.Find("Skill Status").GetComponent<Text>(); // 화면 스킬 상태
        StringAssert.Contains("MP", status.text); // 마나 표시 확인
    }

    [UnityTest] // 사망 중 스킬 차단 사례
    public IEnumerator DeadPlayerCannotUseSkills() // 사망 후 스킬 입력과 자원 보존
    {
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 플레이어 사망 처리
        Press(Key.Q, Key.E, Key.R); // 사망 중 세 스킬 입력
        yield return null; // 사망과 입력 처리 대기
        Assert.That(Mana(), Is.EqualTo(50f).Within(0.01f)); // 사망 중 마나 보존
        Assert.That(Cooldown(0), Is.EqualTo(0f)); // 사망 중 Q 대기시간 미시작
        Assert.That(Cooldown(1), Is.EqualTo(0f)); // 사망 중 E 대기시간 미시작
        Assert.That(Cooldown(2), Is.EqualTo(0f)); // 사망 중 R 대기시간 미시작
    }

    [UnityTest] // 사용자 재배치 사례
    public IEnumerator RemappedFireballRespondsToRInsteadOfQ() // 스킬 이름이 아닌 슬롯 키 사용 검증
    {
        PrototypeSettings previous = trial; // 이전 시험 설정
        q = "IceShard"; // 미구현 Q 스킬
        e = "Shield"; // 미구현 E 스킬
        r = "Fireball"; // 화염탄의 R 재배치
        yield return LoadBattle(); // 변경된 시작 조합으로 전투 재시작
        Object.Destroy(previous); // 이전 독립 시험 설정 정리
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 화염탄 대상
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        yield return AimAt(new Vector2(4f, 0f)); // 대상 방향 조준
        Press(Key.Q); // 미구현 Q 입력
        yield return null; // 입력 처리 대기
        Assert.That(Mana(), Is.EqualTo(50f).Within(0.01f)); // 미구현 스킬의 마나 보존
        Press(); // 키 놓기 입력
        yield return null; // 키 해제 처리 대기
        Press(Key.R); // 화염탄을 배치한 R 입력
        yield return new WaitForSeconds(0.4f); // 투사체 적중 대기
        Assert.That(target.Health, Is.EqualTo(21)); // R 슬롯의 화염탄 피해
        Assert.That(Cooldown(2), Is.GreaterThan(4f)); // R 슬롯에 화염탄 대기시간 적용
    }

    [UnityTest] // 전투 재시작 사례
    public IEnumerator RestartResetsManaCooldownAndHealth() // 새 전투의 독립 상태 검증
    {
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 현재 플레이어 상태
        player.TakeDamage(20); // 시험 피해 적용
        SetNumber("fireballSpeed", 1f); // 재시작 전까지 유지되는 시험 투사체
        yield return AimAt(new Vector2(5f, 0f)); // 빈 공간을 향한 투사체 조준
        Press(Key.Q, Key.E); // 투사체 생성과 마나 및 대기시간 소모
        yield return null; // 스킬 사용 처리 대기
        Assert.That(GameObject.Find("Fireball"), Is.Not.Null); // 이전 전투의 투사체 존재
        Assert.That(Mana(), Is.LessThan(27f)); // 재시작 전 마나 소모 확인
        Assert.That(Cooldown(1), Is.GreaterThan(8f)); // 재시작 전 대기시간 확인
        Press(); // 재시작 전 키 해제
        yield return null; // 해제 처리 대기
        PrototypeSettings previous = trial; // 이전 시험 설정
        yield return LoadBattle(); // 같은 조건의 새 전투 시작
        Object.Destroy(previous); // 이전 독립 설정 정리
        Assert.That(Mana(), Is.EqualTo(50f).Within(0.01f)); // 마나 초기화
        Assert.That(Cooldown(1), Is.EqualTo(0f)); // 이전 대기시간 초기화
        Assert.That(GameObject.Find("Player").GetComponent<Fighter>().Health, Is.EqualTo(100)); // 플레이어 체력 초기화
        Assert.That(GameObject.Find("Fireball"), Is.Null); // 이전 전투 투사체 제거
    }

    [UnityTest] // 화염탄 발사 후 무기 변경 사례
    public IEnumerator FireballKeepsAttackPowerFromCastAfterWeaponChange() // 투사체 피해의 발사 시점 보존
    {
        SetNumber("fireballSpeed", 3f); // 무기 변경을 기다릴 느린 투사체
        Fighter target = PlaceSlime(1, new Vector2(6f, 0f)); // 이동 시간이 남는 원거리 적
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        yield return AimAt(new Vector2(7f, 0f)); // 원거리 적 방향 조준
        Press(Key.Q); // 검을 든 상태의 화염탄 발사
        yield return null; // 발사 처리 대기
        Press(); // 스킬 키 해제
        yield return ClickButton("Equip Bow"); // 비행 중 활로 무기 교체
        Assert.That(target.Health, Is.EqualTo(50)); // 무기 교체가 적중보다 먼저 발생
        StringAssert.Contains("*", GameObject.Find("Equip Bow").GetComponentInChildren<Text>().text); // 실제 활 장착 확인
        yield return new WaitForSeconds(2.2f); // 느린 투사체의 적중 대기
        Assert.That(target.Health, Is.EqualTo(21)); // 발사 당시 검 공격력 기준 29 피해
    }

    [UnityTest] // 텍스트 편집과 일반 선택 사례
    public IEnumerator TextEditingBlocksSkillsButInactiveSelectionDoesNot() // 실제 편집 포커스와 선택 상태 구분
    {
        GameObject fieldObject = new GameObject("Skill Test Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 시험용 입력 필드
        fieldObject.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform, false); // 기존 캔버스에 입력 필드 배치
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
        Press(Key.E); // 텍스트 편집 중 스킬 키
        yield return null; // 입력 처리 대기
        Assert.That(Mana(), Is.EqualTo(50f).Within(0.01f)); // 편집 중 마나 보존
        Assert.That(Cooldown(1), Is.EqualTo(0f)); // 편집 중 스킬 미발동
        Press(); // 키 해제
        input.DeactivateInputField(); // 텍스트 편집 종료
        yield return null; // 편집 종료와 키 해제 반영
        Assert.That(input.isFocused, Is.False); // 실제 편집 포커스 해제 확인
        EventSystem.current.SetSelectedGameObject(GameObject.Find("Equip Bow")); // 일반 버튼 선택
        Press(Key.E); // 일반 버튼 선택 중 스킬 키
        yield return null; // 입력 처리 대기
        Assert.That(Mana(), Is.LessThan(37f)); // 일반 버튼 선택 중 스킬 허용
        Assert.That(Cooldown(1), Is.GreaterThan(8f)); // 스킬 대기시간 시작
    }

    private IEnumerator LoadBattle() // 독립 설정을 적용한 전투 씬 로드
    {
        sceneCallback = (scene, mode) => // 씬 초기화 전 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 원본 설정 연결
            PrototypeSettings asset = (PrototypeSettings)field.GetValue(bootstrap); // 사용자 설정 에셋
            trial = Object.Instantiate(asset); // 원본을 보존할 독립 설정
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.playerHealth = 100; // 시험 최대 체력
            trial.playerAttack = 10; // 시험 기본 공격력
            trial.swordAttack = 12; // 시험 검 공격력
            trial.slimeCount = 3; // 시험 적 수
            trial.slimeHealth = 50; // 시험 적 체력
            trial.slimeDefense = 2; // 시험 적 방어력
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            SetNumber("playerMana", 50f); // 시험 최대 마나
            SetNumber("manaRegen", 3f); // 시험 마나 재생 속도
            SetNumber("fireballCost", 10f); // 화염탄 시험 비용
            SetNumber("fireballCooldown", 5f); // 화염탄 시험 대기시간
            SetNumber("fireballMultiplier", 1.4f); // 화염탄 시험 피해 배율
            SetNumber("fireballRange", 7f); // 화염탄 시험 사거리
            SetNumber("fireballSpeed", 14f); // 화염탄 시험 이동속도
            SetNumber("whirlwindCost", 14f); // 회오리 시험 비용
            SetNumber("whirlwindCooldown", 9f); // 회오리 시험 대기시간
            SetNumber("whirlwindMultiplier", 1.3f); // 회오리 시험 피해 배율
            SetNumber("whirlwindRadius", 2f); // 회오리 시험 반경
            SetNumber("healingCost", 18f); // 치유 시험 비용
            SetNumber("healingCooldown", 20f); // 치유 시험 대기시간
            SetNumber("healingFraction", 0.15f); // 치유 시험 비율
            SetSkill("startingSkillQ", q); // 시험 Q 스킬
            SetSkill("startingSkillE", e); // 시험 E 스킬
            SetSkill("startingSkillR", r); // 시험 R 스킬
            field.SetValue(bootstrap, trial); // 시작 전 독립 설정 연결
        };
        SceneManager.sceneLoaded += sceneCallback; // 시험 설정 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= sceneCallback; // 시험 설정 콜백 해제
        sceneCallback = null; // 중복 해제 방지
    }

    private void SetSkill(string name, string value) // 시험 설정의 시작 스킬 변경
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 슬롯 설정 필드
        field.SetValue(trial, Enum.Parse(field.FieldType, value)); // 시험 열거형 값 적용
    }

    private void SetNumber(string name, float value) // 시험 설정의 수치 변경
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 수치 설정 필드
        if (field != null) // 구현 전 시험과 이후 설정 구분
        {
            field.SetValue(trial, Convert.ChangeType(value, field.FieldType)); // 독립 설정의 시험 수치 적용
        }
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적 체력 상태
    }

    private void CreateWall(Vector2 position) // 전투 판정용 장애물 생성
    {
        GameObject wall = new GameObject("Skill Test Wall"); // 시험 장애물 오브젝트
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = position; // 장애물 위치
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>(); // 장애물 충돌체
        collider.size = new Vector2(0.1f, 1f); // 시험 장애물 크기
        obstacles.Add(wall); // 시험 후 정리 대상 등록
        Physics2D.SyncTransforms(); // 물리 검사에 위치 반영
    }

    private IEnumerator AimAt(Vector2 world) // 화면 포인터로 시험 위치 조준
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 월드 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 가상 마우스 이동
        yield return null; // 조준 입력 반영 대기
    }

    private IEnumerator ClickButton(string name) // 실제 무기 버튼 클릭
    {
        GameObject button = GameObject.Find(name); // 지정 무기 버튼
        Assert.That(button, Is.Not.Null); // 버튼 존재 확인
        RectTransform rect = button.GetComponent<RectTransform>(); // 버튼 배치
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // 버튼 중앙 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 버튼 위 포인터 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 버튼 누름
        yield return null; // 버튼 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 버튼 해제
        yield return null; // 버튼 클릭 처리 대기
    }

    private void Press(params Key[] keys) // 가상 키의 동시 누름
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 지정 키 입력
    }

    private object SkillState() // 현재 스킬 실행 상태
    {
        PropertyInfo property = typeof(PrototypeBootstrap).GetProperty("SkillState"); // 공개 읽기 속성
        Assert.That(property, Is.Not.Null, "전투 스킬 상태가 필요합니다."); // 실행 상태 속성 확인
        object value = property.GetValue(bootstrap); // 현재 스킬 상태
        Assert.That(value, Is.Not.Null); // 초기화된 상태 확인
        return value; // 확인된 스킬 상태
    }

    private float Mana() // 현재 마나 읽기
    {
        object state = SkillState(); // 전투 스킬 상태
        return Convert.ToSingle(state.GetType().GetProperty("Mana").GetValue(state)); // 현재 마나
    }

    private float Cooldown(int slot) // 슬롯별 남은 대기시간 읽기
    {
        object state = SkillState(); // 전투 스킬 상태
        return Convert.ToSingle(state.GetType().GetMethod("RemainingCooldown").Invoke(state, new object[] { slot })); // 남은 시간
    }
}
