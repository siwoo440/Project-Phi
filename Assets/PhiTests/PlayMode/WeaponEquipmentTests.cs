using System.Collections; // 프레임 단위 시험
using System.Reflection; // 전투 상태 시험 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 형식
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 마우스 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 무기 버튼과 선택 표시
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class WeaponEquipmentTests // 전투 중 시험 무기 장착 검증
{
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings asset; // 씬에 연결된 원본 설정 에셋
    private PrototypeSettings trial; // 원본을 보존할 독립 시험 설정
    private PrototypeBootstrap bootstrap; // 실제 전투 구성 요소
    private SceneMapTestFixture mapFixture; // 씬 맵 시험 격리
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 시험 전투와 입력 준비
    {
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        mapFixture = new SceneMapTestFixture(); // 씬 맵의 독립 시험 배치
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 씬 전투 구성 요소
            FieldInfo settingsField = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬의 설정 참조 필드
            asset = (PrototypeSettings)settingsField.GetValue(bootstrap); // 원본 설정 에셋 보관
            trial = Object.Instantiate(asset); // 원본을 보존할 시험 설정 복제
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.playerHealth = 100; // 시험 플레이어 체력
            trial.playerAttack = 10; // 시험 플레이어 기본 공격력
            trial.playerDefense = 5; // 시험 플레이어 방어력
            trial.playerSpeed = 4f; // 시험 플레이어 이동속도
            trial.swordAttack = 12; // 시험 검 공격력
            trial.swordInterval = 0.55f; // 시험 검 공격 간격
            trial.swordRange = 1.7f; // 시험 검 사거리
            trial.swordAngle = 90f; // 시험 검 부채꼴 각도
            trial.swordTargets = 3; // 시험 검 최대 대상 수
            trial.bowAttack = 10; // 시험 활 공격력
            trial.bowInterval = 0.70f; // 시험 활 공격 간격
            trial.bowRange = 7f; // 시험 활 사거리
            trial.bowProjectileSpeed = 14f; // 시험 화살 이동속도
            trial.staffAttack = 8; // 시험 지팡이 공격력
            trial.staffInterval = 0.90f; // 시험 지팡이 공격 간격
            trial.staffRange = 5.5f; // 시험 지팡이 사거리
            trial.staffProjectileSpeed = 9f; // 시험 마법탄 이동속도
            trial.staffExplosionRadius = 1.1f; // 시험 폭발 반경
            trial.staffExplosionTargets = 4; // 시험 폭발 최대 대상 수
            trial.slimeCount = 3; // 시험 슬라임 수
            trial.slimeHealth = 50; // 시험 슬라임 체력
            trial.slimeAttack = 8; // 시험 슬라임 공격력
            trial.slimeDefense = 2; // 시험 슬라임 방어력
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            trial.slimeAttackRange = 0.9f; // 시험 슬라임 공격 거리
            trial.slimeAttackInterval = 1.2f; // 시험 슬라임 공격 간격
            trial.slimeWindup = 0.4f; // 시험 슬라임 공격 예고
            trial.slimeDashDistance = 0.8f; // 시험 슬라임 돌진 거리
            trial.slimeDashSpeed = 5f; // 시험 슬라임 돌진 속도
            trial.slimeHitDistance = 0.65f; // 시험 슬라임 적중 거리
            settingsField.SetValue(bootstrap, trial); // 시작 전 시험 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 씬 로드 콜백 해제
    }

    [UnityTearDown] // 각 시험의 정리
    public IEnumerator TearDown() // 시험 자원과 전역 상태 복원
    {
        if (mouse != null && mouse.added) // 가상 장치 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        if (mapFixture != null) // 시험 맵 격리 존재 여부
        {
            mapFixture.Dispose(); // 시험 맵 이벤트와 그림 정리
        }
        if (trial != null) // 임시 설정 존재 여부
        {
            Object.Destroy(trial); // 임시 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 세 무기 선택과 공격 사례
    public IEnumerator ButtonsSelectAllWeaponsWithoutChangingSettingsAsset() // 선택 표시와 무기별 피해 검증
    {
        WeaponType originalAssetWeapon = asset.startingWeapon; // 사용자 설정 에셋의 원래 시작 무기
        Fighter swordTarget = PlaceSlime(1, new Vector2(1.2f, 0f)); // 검 사거리의 첫 적
        Fighter bowTarget = PlaceSlime(2, new Vector2(3f, 0f)); // 활 사거리의 두 번째 적
        Fighter staffTarget = PlaceSlime(3, new Vector2(5f, 0f)); // 지팡이 사거리의 세 번째 적
        AssertSelected("Equip Sword", "SWORD *"); // 시작 검의 선택 표시
        PressTowards(new Vector2(5f, 0f)); // 검 방향 일반 공격
        yield return null; // 검 타격 처리 대기
        ReleaseAt(new Vector2(5f, 0f)); // 검 공격 입력 해제
        yield return null; // 버튼 해제 처리 대기
        Assert.That(swordTarget.Health, Is.EqualTo(29)); // 검의 21 피해
        swordTarget.transform.position = new Vector3(-4f, 0f, 0f); // 이후 원거리 공격의 간섭 제거
        yield return ClickButton("Equip Bow"); // 실제 활 버튼 클릭
        AssertSelected("Equip Bow", "BOW *"); // 활 선택 표시
        Assert.That(trial.startingWeapon, Is.EqualTo(WeaponType.Sword)); // 시험 시작 설정 불변
        Assert.That(asset.startingWeapon, Is.EqualTo(originalAssetWeapon)); // 원본 설정 에셋 불변
        yield return WaitForAttackReady(); // 기존 공격 대기시간 경과
        PressTowards(new Vector2(5f, 0f)); // 활 방향 일반 공격
        yield return null; // 활 발사 처리 대기
        ReleaseAt(new Vector2(5f, 0f)); // 활 공격 입력 해제
        yield return new WaitForSeconds(0.4f); // 화살 적중 대기
        Assert.That(bowTarget.Health, Is.EqualTo(31)); // 활의 19 피해
        bowTarget.transform.position = new Vector3(-4f, 1f, 0f); // 이후 마법탄 공격의 간섭 제거
        yield return ClickButton("Equip Staff"); // 실제 지팡이 버튼 클릭
        AssertSelected("Equip Staff", "STAFF *"); // 지팡이 선택 표시
        yield return WaitForAttackReady(); // 이전 활 공격의 대기시간 경과
        PressTowards(new Vector2(7f, 0f)); // 지팡이 방향 일반 공격
        yield return null; // 마법탄 발사 처리 대기
        ReleaseAt(new Vector2(7f, 0f)); // 지팡이 공격 입력 해제
        yield return new WaitForSeconds(0.7f); // 마법탄 적중 대기
        Assert.That(staffTarget.Health, Is.EqualTo(33)); // 지팡이의 17 피해
        Assert.That(asset.startingWeapon, Is.EqualTo(originalAssetWeapon)); // 세 무기 사용 후 원본 에셋 불변
    }

    [UnityTest] // UI 입력 차단 사례
    public IEnumerator ButtonClickAndDragNeverStartCombatAttack() // 버튼 클릭과 드래그의 공격 차단
    {
        Fighter target = PlaceSlime(1, new Vector2(1.2f, 0f)); // 전투 입력 시험 대상
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        yield return ClickButton("Equip Bow"); // 실제 활 버튼 클릭
        AssertSelected("Equip Bow", "BOW *"); // 활 장착 확인
        Assert.That(target.Health, Is.EqualTo(50)); // 버튼 클릭의 검 피해 차단
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 버튼 클릭의 화살 발사 차단
        Vector2 ui = ButtonPosition("Equip Staff"); // 지팡이 버튼 위치
        Vector2 world = WorldPosition(new Vector2(3f, 0f)); // 전투 화면 위치
        InputSystem.QueueStateEvent(mouse, new MouseState { position = ui }); // 버튼 위 포인터 이동
        yield return null; // 포인터 위치 반영
        InputSystem.QueueStateEvent(mouse, new MouseState { position = ui }.WithButton(MouseButton.Left)); // 버튼 위 누름 시작
        yield return null; // UI 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }.WithButton(MouseButton.Left)); // 누른 채 전투 화면으로 드래그
        yield return new WaitForSeconds(0.8f); // 공격 간격 이상 드래그 유지
        Assert.That(target.Health, Is.EqualTo(50)); // UI에서 시작한 드래그 피해 차단
        Assert.That(GameObject.Find("Arrow"), Is.Null); // UI 드래그의 화살 발사 차단
        InputSystem.QueueStateEvent(mouse, new MouseState { position = world }); // UI 밖에서 버튼 놓기
        yield return null; // 버튼 해제 처리 대기
        AssertSelected("Equip Bow", "BOW *"); // 버튼 밖 놓기 시 기존 장착 유지
        PressTowards(new Vector2(3f, 0f)); // 전투 화면의 새 공격
        yield return new WaitForSeconds(0.3f); // 화살 적중 대기
        Assert.That(target.Health, Is.EqualTo(31)); // 새 전투 입력의 활 피해
    }

    [UnityTest] // 무기 교체 중 공격 간격 사례
    public IEnumerator SwitchingWeaponDoesNotShortenExistingCooldown() // 공격 대기 시각 보존 검증
    {
        trial.swordInterval = 2f; // 전환 중 대기시간을 확인할 시험 간격
        Fighter swordTarget = PlaceSlime(1, new Vector2(1.2f, 0f)); // 첫 검 공격 대상
        Fighter bowTarget = PlaceSlime(2, new Vector2(3f, 0f)); // 이후 활 공격 대상
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        PressTowards(new Vector2(4f, 0f)); // 검 일반 공격
        yield return null; // 검 공격 처리 대기
        ReleaseAt(new Vector2(4f, 0f)); // 검 공격 입력 해제
        yield return null; // 버튼 해제 처리 대기
        Assert.That(swordTarget.Health, Is.EqualTo(29)); // 첫 검 공격 피해
        swordTarget.transform.position = new Vector3(-4f, 0f, 0f); // 활 공격의 간섭 제거
        float deadline = ReadNextAttack(); // 기존 검 공격의 대기 종료 시각
        yield return ClickButton("Equip Bow"); // 대기 중 실제 활 버튼 클릭
        Assert.That(ReadNextAttack(), Is.EqualTo(deadline).Within(0.0001f)); // 절대 공격 가능 시각 유지
        Assert.That(Time.time, Is.LessThan(deadline)); // 전환 후 아직 대기 중인 시험 조건
        PressTowards(new Vector2(4f, 0f)); // 대기 중 활 공격 입력
        yield return null; // 입력 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 대기 중 화살 발사 방지
        Assert.That(bowTarget.Health, Is.EqualTo(50)); // 대기 중 활 피해 방지
        ReleaseAt(new Vector2(4f, 0f)); // 공격 입력 해제
        yield return null; // 버튼 해제 처리 대기
        yield return WaitForAttackReady(); // 원래 대기 시각까지 대기
        PressTowards(new Vector2(4f, 0f)); // 대기 종료 후 활 공격
        yield return new WaitForSeconds(0.4f); // 화살 적중 대기
        Assert.That(bowTarget.Health, Is.EqualTo(31)); // 대기 종료 후 활 피해
    }

    [UnityTest] // 발사 후 교체 사례
    public IEnumerator SwitchingPreservesLaunchedProjectileData() // 기존 화살의 공격력과 이동 상태 보존
    {
        trial.bowProjectileSpeed = 1f; // 전환 전에 살아 있는 느린 시험 화살
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 발사 방향의 적
        PlaceSlime(2, new Vector2(0f, 3f)); // 전환 시 마우스 방향의 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        yield return ClickButton("Equip Bow"); // 실제 활 버튼 클릭
        PressTowards(new Vector2(4f, 0f)); // 오른쪽 방향 화살 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Arrow"), Is.Not.Null); // 발사된 화살 존재
        ReleaseAt(new Vector2(4f, 0f)); // 공격 입력 해제
        yield return null; // 버튼 해제 처리 대기
        yield return ClickButton("Equip Staff"); // 비행 중 실제 지팡이 버튼 클릭
        trial.playerAttack = 0; // 발사 후 설정 공격력 변경
        trial.bowAttack = 0; // 발사 후 활 공격력 변경
        trial.bowProjectileSpeed = 0.01f; // 발사 후 설정 속도 변경
        trial.bowRange = 0.1f; // 발사 후 설정 사거리 변경
        yield return new WaitForSeconds(2.1f); // 기존 속도의 화살 적중 대기
        Assert.That(target.Health, Is.EqualTo(31)); // 발사 순간 공격력과 속도 유지
        Assert.That(GameObject.Find("Arrow"), Is.Null); // 최초 적중 후 화살 제거
        Assert.That(trial.startingWeapon, Is.EqualTo(WeaponType.Sword)); // 전환 후 시작 설정 불변
    }

    [UnityTest] // 마법탄 발사 후 교체 사례
    public IEnumerator SwitchingPreservesLaunchedStaffExplosionData() // 발사된 마법탄의 폭발 설정 보존
    {
        trial.staffProjectileSpeed = 1f; // 전환 전에 살아 있는 느린 시험 마법탄
        Fighter direct = PlaceSlime(1, new Vector2(2f, 0f)); // 마법탄과 직접 충돌할 적
        Fighter nearby = PlaceSlime(2, new Vector2(2f, 0.7f)); // 폭발 반경 안의 주변 적
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        yield return ClickButton("Equip Staff"); // 실제 지팡이 버튼 클릭
        PressTowards(new Vector2(4f, 0f)); // 오른쪽 마법탄 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Not.Null); // 발사된 마법탄 존재
        ReleaseAt(new Vector2(4f, 0f)); // 공격 입력 해제
        yield return null; // 버튼 해제 처리 대기
        yield return ClickButton("Equip Sword"); // 비행 중 실제 검 버튼 클릭
        trial.playerAttack = 0; // 발사 후 기본 공격력 변경
        trial.staffAttack = 0; // 발사 후 지팡이 공격력 변경
        trial.staffProjectileSpeed = 0.01f; // 발사 후 설정 속도 변경
        trial.staffRange = 0.1f; // 발사 후 설정 사거리 변경
        trial.staffExplosionRadius = 0f; // 발사 후 설정 폭발 반경 변경
        trial.staffExplosionTargets = 0; // 발사 후 설정 대상 상한 변경
        yield return new WaitForSeconds(2.1f); // 발사 당시 속도의 마법탄 적중 대기
        Assert.That(direct.Health, Is.EqualTo(33)); // 발사 순간의 공격력 유지
        Assert.That(nearby.Health, Is.EqualTo(33)); // 발사 순간의 폭발 반경과 상한 유지
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // 적중한 마법탄 제거
        Assert.That(trial.startingWeapon, Is.EqualTo(WeaponType.Sword)); // 전환 후 시작 설정 불변
    }

    [UnityTest] // 빈 패널 영역 사례
    public IEnumerator BlankWeaponPanelAreaDoesNotBlockWorldAttack() // 버튼 사이 빈 공간의 전투 입력 검증
    {
        GameObject panel = GameObject.Find("Weapon Panel"); // 시험 무기 UI 패널
        Assert.That(panel, Is.Not.Null, "무기 버튼 패널이 필요합니다."); // 버튼 패널 존재 확인
        RectTransform rect = panel.GetComponent<RectTransform>(); // 패널의 화면 배치
        Image image = panel.GetComponent<Image>(); // 선택적인 패널 배경
        Assert.That(image == null || !image.raycastTarget, Is.True); // 빈 배경의 입력 차단 방지
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(0f, rect.rect.yMax - 2f, 0f))); // 버튼 위쪽 빈 여백 좌표
        Vector3 aimWorld = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f)); // 여백이 가리키는 월드 좌표
        Vector2 direction = ((Vector2)(aimWorld - GameObject.Find("Player").transform.position)).normalized; // 빈 여백의 조준 방향
        Fighter target = PlaceSlime(1, direction * 1.2f); // 빈 여백 방향의 검 대상
        PlaceSlime(2, new Vector2(4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 패널 빈 여백 클릭
        yield return null; // 전투 입력 처리 대기
        Assert.That(target.Health, Is.EqualTo(29)); // 빈 배경이 전투 입력을 막지 않음
    }

    [UnityTest] // 씬 맵 및 사망 재시작 사례
    public IEnumerator DeathBlocksSwitchAndRestartRestoresConfiguredWeapon() // 씬 맵 보존과 재시작 상태 검증
    {
        Transform floor = GameObject.Find("Map Floor").transform; // 씬의 맵 바닥
        Transform boundary = floor.Find("Boundary Right"); // 씬의 오른쪽 경계
        int floorId = floor.gameObject.GetInstanceID(); // 기존 바닥 식별자
        int boundaryId = boundary.gameObject.GetInstanceID(); // 기존 경계 식별자
        Vector3 floorPosition = floor.position; // 기존 바닥 위치
        Vector3 floorScale = floor.localScale; // 기존 바닥 크기
        WeaponType originalAssetWeapon = asset.startingWeapon; // 설정 에셋의 원래 시작 무기
        Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 플레이어 전투 상태
        PlaceSlime(1, new Vector2(0.75f, 0f)); // 사망 처리할 슬라임 배치
        PlaceSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PlaceSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        yield return ClickButton("Equip Staff"); // 실제 지팡이 버튼 클릭
        AssertSelected("Equip Staff", "STAFF *"); // 사망 전 지팡이 장착 확인
        Assert.That(floor.gameObject.GetInstanceID(), Is.EqualTo(floorId)); // 바닥 오브젝트 보존
        Assert.That(boundary.gameObject.GetInstanceID(), Is.EqualTo(boundaryId)); // 경계 오브젝트 보존
        Assert.That(floor.position, Is.EqualTo(floorPosition)); // 바닥 위치 보존
        Assert.That(floor.localScale, Is.EqualTo(floorScale)); // 바닥 크기 보존
        player.TakeDamage(99); // 한 번의 적 공격으로 사망할 체력
        yield return new WaitForSeconds(0.9f); // 슬라임 예고와 공격 대기
        Assert.That(player.Dead, Is.True); // 실제 슬라임 공격의 사망 처리
        yield return ClickButton("Equip Bow"); // 사망 중 실제 활 버튼 클릭
        AssertSelected("Equip Staff", "STAFF *"); // 사망 중 장착 변경 방지
        yield return ClickButton("Restart"); // 실제 재시작 버튼 클릭
        yield return null; // 새 전투 초기화 대기
        yield return null; // 새 전투 표시 대기
        Assert.That(asset.startingWeapon, Is.EqualTo(originalAssetWeapon)); // 원본 시작 무기 불변
        AssertSelected(EquipmentName(originalAssetWeapon), EquipmentLabel(originalAssetWeapon)); // 에셋 시작 무기 복원
    }

    private IEnumerator ClickButton(string name) // 실제 UI 포인터 클릭
    {
        Vector2 position = ButtonPosition(name); // 버튼의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 위 포인터 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // 버튼 누름
        yield return null; // 버튼 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 놓기
        yield return null; // 버튼 클릭 처리 대기
    }

    private static Vector2 ButtonPosition(string name) // UI 버튼 중앙 좌표
    {
        GameObject target = GameObject.Find(name); // 이름에 맞는 버튼 오브젝트
        Assert.That(target, Is.Not.Null, name + " 버튼이 필요합니다."); // 버튼 존재 확인
        RectTransform rect = target.GetComponent<RectTransform>(); // 버튼의 화면 배치
        Assert.That(target.GetComponent<Button>(), Is.Not.Null); // 실제 버튼 구성 요소 확인
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // 버튼 중앙 화면 좌표
    }

    private static void AssertSelected(string name, string label) // 선택된 무기 표시 확인
    {
        GameObject target = GameObject.Find(name); // 선택된 무기 버튼
        Assert.That(target, Is.Not.Null, name + " 버튼이 필요합니다."); // 버튼 존재 확인
        Text text = target.GetComponentInChildren<Text>(); // 버튼의 선택 표시 글자
        Assert.That(text, Is.Not.Null); // 선택 표시 존재 확인
        Assert.That(text.text, Is.EqualTo(label)); // 선택된 무기 글자 확인
        string[] names = { "Equip Sword", "Equip Bow", "Equip Staff" }; // 모든 무기 버튼 이름
        foreach (string other in names) // 나머지 무기 선택 표시 검사
        {
            if (other != name) // 선택되지 않은 무기 확인
            {
                GameObject button = GameObject.Find(other); // 다른 무기 버튼
                Assert.That(button, Is.Not.Null, other + " 버튼이 필요합니다."); // 다른 버튼 존재 확인
                Assert.That(button.GetComponentInChildren<Text>().text.EndsWith(" *"), Is.False); // 다른 무기의 중복 선택 표시 방지
            }
        }
    }

    private static string EquipmentName(WeaponType weapon) // 무기별 버튼 이름
    {
        return weapon == WeaponType.Bow ? "Equip Bow" : weapon == WeaponType.Staff ? "Equip Staff" : "Equip Sword"; // 시작 무기 버튼 이름
    }

    private static string EquipmentLabel(WeaponType weapon) // 무기별 선택 표시
    {
        return weapon == WeaponType.Bow ? "BOW *" : weapon == WeaponType.Staff ? "STAFF *" : "SWORD *"; // 시작 무기 선택 글자
    }

    private IEnumerator WaitForAttackReady() // 현재 공격 대기시간 경과
    {
        float delay = Mathf.Max(0f, ReadNextAttack() - Time.time + 0.05f); // 공격 가능 시각까지의 여유 시간
        yield return new WaitForSeconds(delay); // 기존 대기 시각까지 대기
    }

    private float ReadNextAttack() // 공격 가능 시각 확인
    {
        return (float)typeof(PrototypeBootstrap).GetField("nextAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap); // 실제 전투의 절대 대기 시각
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적 확인
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적의 체력 상태
    }

    private static Vector2 WorldPosition(Vector2 world) // 월드 목표의 화면 좌표
    {
        return Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 목표의 화면 좌표
    }

    private void PressTowards(Vector2 world) // 지정 방향의 마우스 누름
    {
        Vector2 position = WorldPosition(world); // 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // 좌클릭 입력
    }

    private void ReleaseAt(Vector2 world) // 지정 방향의 마우스 놓기
    {
        Vector2 position = WorldPosition(world); // 목표의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }); // 버튼 해제 입력
    }
}
