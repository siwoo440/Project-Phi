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

public class StaffProjectileTests // 지팡이 폭발 공격 검증
{
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings trial; // 독립 전투 설정
    private SceneMapTestFixture mapFixture; // 씬 맵 시험 격리
    private FieldInfo weaponField; // 시작 무기 설정 필드
    private FieldInfo speedField; // 마법탄 속도 설정 필드
    private readonly List<GameObject> obstacles = new List<GameObject>(); // 시험 장애물 목록
    private bool previousRunInBackground; // 기존 백그라운드 실행 설정
    private bool previousQueriesHitTriggers; // 기존 트리거 검사 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 처리 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 에디터 입력 경로

    [UnitySetUp] // 각 시험의 준비
    public IEnumerator SetUp() // 지팡이 전투와 시험 맵 준비
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
        speedField = typeof(PrototypeSettings).GetField("staffProjectileSpeed"); // 마법탄 속도 설정 확인
        bool supportsStaff = weaponField != null && Enum.IsDefined(weaponField.FieldType, "Staff"); // 지팡이 선택 지원 여부
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 전투 시작 전 시험 설정
        {
            trial = ScriptableObject.CreateInstance<PrototypeSettings>(); // 원본을 보존할 시험 설정
            trial.slimeCount = 6; // 폭발 대상 상한을 넘는 시험 적 수
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            if (supportsStaff) // 지팡이 선택 지원 여부
            {
                weaponField.SetValue(trial, Enum.Parse(weaponField.FieldType, "Staff")); // 시작 무기를 지팡이로 지정
            }
            PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 씬 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, trial); // 시작 전 시험 설정 연결
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 시험 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= loaded; // 씬 로드 콜백 해제
        Assert.That(supportsStaff, Is.True, "시작 무기에서 지팡이를 선택할 수 있어야 합니다."); // 지팡이 선택 지원 확인
        Assert.That(speedField, Is.Not.Null, "마법탄 속도 설정이 필요합니다."); // 조정 가능한 마법탄 속도 확인
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

    [UnityTest] // 폭발 대상과 벽 가림 사례
    public IEnumerator ImpactExplosionFiltersWallsAndDuplicateCollidersBeforeFourTargetLimit() // 폭발 피해와 대상 상한 검증
    {
        Fighter direct = PlaceSlime(1, new Vector2(2f, 0f)); // 직접 충돌한 적
        Fighter hidden = PlaceSlime(2, new Vector2(2f, 0.45f)); // 가까우나 벽에 가려진 적
        Fighter first = PlaceSlime(3, new Vector2(1.5f, -0.6f)); // 첫 번째 가려지지 않은 적
        Fighter second = PlaceSlime(4, new Vector2(1.9f, -0.7f)); // 두 번째 가려지지 않은 적
        Fighter third = PlaceSlime(5, new Vector2(2.2f, -0.6f)); // 세 번째 가려지지 않은 적
        Fighter fourth = PlaceSlime(6, new Vector2(2.3f, -0.6f)); // 상한 밖의 네 번째 주변 적
        GameObject duplicate = new GameObject("Duplicate Enemy Collider"); // 같은 적의 중복 충돌체 오브젝트
        duplicate.layer = 10; // 적 충돌 계층
        duplicate.transform.SetParent(direct.transform, false); // 직접 충돌 적의 자식 설정
        duplicate.AddComponent<BoxCollider2D>(); // 같은 적의 추가 충돌체
        CreateObstacle(new Vector2(1.8f, 0.25f), new Vector2(0.15f, 0.14f), 8, false); // 폭발 중심과 숨은 적 사이의 벽
        PressTowards(new Vector2(4f, 0f)); // 첫 적 방향 마법탄 발사
        yield return new WaitForSeconds(0.4f); // 적중과 폭발 처리 대기
        Assert.That(direct.Health, Is.EqualTo(33)); // 직접 적중과 폭발의 합산 중복 방지
        Assert.That(hidden.Health, Is.EqualTo(50)); // 폭발 중심에서 벽에 가려진 적 제외
        Assert.That(first.Health, Is.EqualTo(33)); // 첫 번째 유효 대상 17 피해
        Assert.That(second.Health, Is.EqualTo(33)); // 두 번째 유효 대상 17 피해
        Assert.That(third.Health, Is.EqualTo(33)); // 중복 충돌체와 가려진 적의 상한 미소모
        Assert.That(fourth.Health, Is.EqualTo(50)); // 거리순 최대 네 대상 제한
    }

    [UnityTest] // 최대 사거리와 폭발 반경 사례
    public IEnumerator RangeEndExplosionIncludesRadiusBoundaryAndExcludesOutside() // 사거리 종료 폭발과 중심 거리 검증
    {
        Fighter boundary = PlaceSlime(1, new Vector2(5.5f, 1.1f)); // 폭발 반경과 정확히 만나는 적 중심
        Fighter outside = PlaceSlime(2, new Vector2(5.5f, 1.12f)); // 폭발 반경 바깥의 적 중심
        MoveOtherSlimesAway(3); // 나머지 적의 간섭 제거
        PressTowards(new Vector2(8f, 0f)); // 오른쪽으로 마법탄 발사
        yield return new WaitForSeconds(0.8f); // 최대 사거리 도달과 폭발 대기
        Assert.That(boundary.Health, Is.EqualTo(33)); // 반경 경계의 적에게 17 피해
        Assert.That(outside.Health, Is.EqualTo(50)); // 반경 밖 적의 피해 방지
    }

    [UnityTest] // 적중 순간 마법탄 중심 기준 사례
    public IEnumerator ImpactExplosionUsesProjectileCenterForRadius() // 접촉 표면과 마법탄 중심의 반경 차이 검증
    {
        Fighter direct = PlaceSlime(1, new Vector2(2f, 0f)); // 마법탄이 직접 충돌하는 적
        Fighter inside = PlaceSlime(2, new Vector2(1.075f, 0.94f)); // 마법탄 중심 기준 반경 안의 적
        Fighter outside = PlaceSlime(3, new Vector2(1.075f, 0.97f)); // 마법탄 중심 기준 반경 밖의 적
        MoveOtherSlimesAway(4); // 나머지 적의 간섭 제거
        PressTowards(new Vector2(4f, 0f)); // 직접 충돌 대상 방향으로 발사
        yield return new WaitForSeconds(0.4f); // 적중과 폭발 대기
        Assert.That(direct.Health, Is.EqualTo(33)); // 직접 충돌 대상의 17 피해
        Assert.That(inside.Health, Is.EqualTo(33)); // 접촉 표면이 아닌 마법탄 중심의 반경 안 대상
        Assert.That(outside.Health, Is.EqualTo(50)); // 마법탄 중심 반경 밖 대상 제외
    }

    [UnityTest] // 폭발 대상 거리 동률 사례
    public IEnumerator EqualDistanceTargetsUseSpawnOrderAtLimit() // 동일 거리의 생성 순서 우선 검증
    {
        Fighter direct = PlaceSlime(1, new Vector2(2f, 0f)); // 직접 충돌하는 적
        Fighter earlier = PlaceSlime(2, new Vector2(1.625f, 0.9f)); // 동일 거리에서 먼저 생성된 적
        Fighter later = PlaceSlime(3, new Vector2(1.625f, -0.9f)); // 동일 거리에서 나중에 생성된 적
        Fighter closerFirst = PlaceSlime(4, new Vector2(1.625f, 0.8f)); // 더 가까운 첫 적
        Fighter closerSecond = PlaceSlime(5, new Vector2(1.625f, -0.8f)); // 더 가까운 두 번째 적
        Fighter farthest = PlaceSlime(6, new Vector2(2.5f, 0.5f)); // 반경 안에서 가장 먼 적
        PressTowards(new Vector2(4f, 0f)); // 직접 충돌 대상 방향으로 발사
        yield return new WaitForSeconds(0.4f); // 적중과 폭발 대기
        Assert.That(direct.Health, Is.EqualTo(33)); // 직접 대상의 단일 피해
        Assert.That(closerFirst.Health, Is.EqualTo(33)); // 가까운 첫 적의 피해
        Assert.That(closerSecond.Health, Is.EqualTo(33)); // 가까운 두 번째 적의 피해
        Assert.That(earlier.Health, Is.EqualTo(33)); // 동률에서 먼저 생성된 적 우선
        Assert.That(later.Health, Is.EqualTo(50)); // 동률에서 나중에 생성된 적 제외
        Assert.That(farthest.Health, Is.EqualTo(50)); // 상한 밖의 먼 대상 제외
    }

    [UnityTest] // 적중과 최대 사거리 동시 사례
    public IEnumerator EnemyImpactAtMaximumRangeExplodesOnlyOnce() // 적중과 사거리 중복 폭발 방지
    {
        Fighter target = PlaceSlime(1, new Vector2(5.875f, 0f)); // 마법탄 반경을 포함해 사거리 끝에 닿는 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        PressTowards(new Vector2(8f, 0f)); // 사거리 끝의 적을 향한 발사
        yield return new WaitForSeconds(0.8f); // 적중과 사거리 도달 대기
        Assert.That(target.Health, Is.EqualTo(33)); // 동시 도달에도 17 피해 한 번
    }

    [UnityTest] // 벽 충돌 사례
    public IEnumerator ObstacleStopsBoltWithoutExplosion() // 벽 충돌의 폭발 방지
    {
        Fighter hidden = PlaceSlime(1, new Vector2(2.5f, 0f)); // 벽 뒤의 적
        Fighter nearby = PlaceSlime(2, new Vector2(1.4f, 0.6f)); // 벽 앞 폭발 범위의 적
        MoveOtherSlimesAway(3); // 나머지 적의 간섭 제거
        CreateObstacle(new Vector2(1.5f, 0f), new Vector2(0.1f, 0.4f), 8, false); // 마법탄 앞의 장애물
        PressTowards(new Vector2(4f, 0f)); // 벽 방향으로 마법탄 발사
        yield return new WaitForSeconds(0.3f); // 벽까지 이동 대기
        Assert.That(hidden.Health, Is.EqualTo(50)); // 벽 뒤 적의 피해 방지
        Assert.That(nearby.Health, Is.EqualTo(50)); // 벽 충돌 시 주변 폭발 방지
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // 벽에서 마법탄 제거
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 벽에서 폭발 효과 없음
    }

    [UnityTest] // 벽과 적의 충돌 동률 사례
    public IEnumerator WallWinsWhenImpactDistanceMatchesEnemy() // 동시 충돌의 벽 우선 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 벽과 같은 거리에 닿는 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1.725f, 0f), new Vector2(0.1f, 1f), 8, false); // 적의 왼쪽 면과 맞춘 벽
        PressTowards(new Vector2(3f, 0f)); // 적과 벽 방향으로 발사
        yield return new WaitForSeconds(0.3f); // 충돌 거리 도달 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 동률에서 벽 우선과 폭발 차단
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 벽 충돌의 폭발 방지
    }

    [UnityTest] // 씬 경계 충돌 사례
    public IEnumerator SceneBoundaryStopsBoltWithoutExplosion() // 기존 씬 경계의 폭발 방지
    {
        GameObject.Find("Player").transform.position = new Vector3(9.2f, 0f, 0f); // 오른쪽 경계 근처의 플레이어
        Fighter nearby = PlaceSlime(1, new Vector2(9f, 0.6f)); // 경계 주변의 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        PressTowards(new Vector2(12f, 0f)); // 씬 오른쪽 경계 방향 발사
        yield return new WaitForSeconds(0.3f); // 씬 경계 도달 대기
        Assert.That(nearby.Health, Is.EqualTo(50)); // 씬 경계에서의 폭발 피해 방지
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // 씬 경계의 마법탄 소멸
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 씬 경계의 폭발 효과 없음
    }

    [UnityTest] // 고속 이동과 발사 방향 고정 사례
    public IEnumerator HighSpeedBoltKeepsLaunchDirectionAndCannotSkipEnemy() // 연속 충돌과 방향 고정 검증
    {
        speedField.SetValue(trial, 1000f); // 한 프레임에 긴 거리를 이동할 시험 속도
        Fighter right = PlaceSlime(1, new Vector2(3f, 0f)); // 최초 조준 방향의 적
        Fighter up = PlaceSlime(2, new Vector2(0f, 3f)); // 변경된 조준 방향의 적
        MoveOtherSlimesAway(3); // 나머지 적의 간섭 제거
        PressTowards(new Vector2(4f, 0f)); // 오른쪽으로 마법탄 발사
        yield return null; // 첫 발사 처리 대기
        ReleaseAt(new Vector2(0f, 4f)); // 마우스를 위로 옮기고 버튼 해제
        yield return new WaitForSeconds(0.15f); // 고속 충돌 처리 대기
        Assert.That(right.Health, Is.EqualTo(33)); // 최초 방향의 적에게 17 피해
        Assert.That(up.Health, Is.EqualTo(50)); // 발사 후 마우스 방향 무시
    }

    [UnityTest] // 트리거 및 무관 계층 사례
    public IEnumerator TriggersAndOtherLayersDoNotBlockBolt() // 마법탄의 무관 충돌체 무시 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 뒤쪽의 생존 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 1f), 0, false); // 무관 계층의 충돌체
        CreateObstacle(new Vector2(2f, 0f), new Vector2(0.1f, 1f), 8, true); // 장애물 계층의 트리거
        Physics2D.queriesHitTriggers = true; // 전역 트리거 검사가 켜진 조건
        PressTowards(new Vector2(4f, 0f)); // 적 방향 마법탄 발사
        yield return new WaitForSeconds(0.4f); // 적까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(33)); // 트리거와 무관 계층 통과 후 17 피해
    }

    [UnityTest] // 트리거 뒤의 실제 벽 사례
    public IEnumerator TriggerBeforeWallDoesNotHideTheWall() // 트리거와 뒤쪽 벽 구분 검증
    {
        Fighter target = PlaceSlime(1, new Vector2(3f, 0f)); // 벽 뒤의 생존 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(1f, 0f), new Vector2(0.1f, 1f), 8, true); // 앞쪽의 트리거
        CreateObstacle(new Vector2(2f, 0f), new Vector2(0.1f, 1f), 8, false); // 트리거 뒤의 실제 벽
        Physics2D.queriesHitTriggers = true; // 전역 트리거 검사가 켜진 조건
        PressTowards(new Vector2(4f, 0f)); // 적과 벽 방향 발사
        yield return new WaitForSeconds(0.4f); // 벽까지 이동 대기
        Assert.That(target.Health, Is.EqualTo(50)); // 트리거 뒤 실제 벽의 피해 차단
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 벽 충돌의 폭발 방지
    }

    [UnityTest] // 범위 시각 효과 사례
    public IEnumerator ExplosionEffectAppearsThenExpires() // 폭발 범위 시각 표시의 수명 검증
    {
        speedField.SetValue(trial, 1000f); // 빠른 충돌을 위한 시험 속도
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 직접 적중할 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        PressTowards(new Vector2(3f, 0f)); // 적 방향 발사
        float deadline = Time.realtimeSinceStartup + 2f; // 폭발 표시 대기 상한
        GameObject effect = GameObject.Find("Staff Explosion"); // 현재 폭발 표시
        while (effect == null && Time.realtimeSinceStartup < deadline) // 폭발 표시가 나타날 때까지 대기
        {
            yield return null; // 다음 프레임 확인
            effect = GameObject.Find("Staff Explosion"); // 현재 폭발 표시 재확인
        }
        bool boltPresent = GameObject.Find("Magic Bolt") != null; // 마법탄 잔존 여부
        string diagnostics = $"health={target.Health}, bolt={boltPresent}, time={Time.time}, dt={Time.deltaTime}"; // 폭발 누락 원인 진단 정보
        Assert.That(effect, Is.Not.Null, diagnostics); // 폭발 범위 표시 생성
        Assert.That(target.Health, Is.EqualTo(33)); // 실제 폭발 피해 확인
        yield return new WaitForSeconds(0.3f); // 임시 표시 시간 경과
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 폭발 범위 표시 종료
    }

    [UnityTest] // 홀드 공격 간격 사례
    public IEnumerator HoldingAttackUsesStaffInterval() // 지팡이 연속 발사 간격 검증
    {
        speedField.SetValue(trial, 1f); // 동시에 남는 느린 시험 마법탄
        MoveOtherSlimesAway(1); // 모든 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 오른쪽 공격 유지
        yield return new WaitForSeconds(0.65f); // 두 번째 발사 전 대기
        Assert.That(CountNamed("Magic Bolt"), Is.EqualTo(1)); // 0.90초 이전의 단일 발사
        yield return new WaitForSeconds(0.35f); // 두 번째 발사 가능 시각 대기
        Assert.That(CountNamed("Magic Bolt"), Is.EqualTo(2)); // 공격 간격 후 반복 발사
    }

    [UnityTest] // UI 입력 분리 사례
    public IEnumerator UiClickDoesNotFireStaff() // UI 클릭의 지팡이 공격 차단
    {
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 이후 공격할 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        RectTransform panel = GameObject.Find("Health Panel").GetComponent<RectTransform>(); // 체력 UI 영역
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center)); // UI 중앙 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left)); // UI 영역의 클릭
        yield return new WaitForSeconds(0.1f); // UI 클릭 처리 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // UI 클릭의 마법탄 차단
        Assert.That(target.Health, Is.EqualTo(50)); // UI 클릭의 피해 차단
        ReleaseAt(new Vector2(3f, 0f)); // 버튼 해제와 전투 화면 이동
        yield return null; // 버튼 해제 처리 대기
        PressTowards(new Vector2(3f, 0f)); // 전투 화면에서 새 공격
        yield return new WaitForSeconds(0.4f); // 마법탄 적중 대기
        Assert.That(target.Health, Is.EqualTo(33)); // UI 밖 공격의 정상 피해
    }

    [UnityTest] // 플레이어 사망 사례
    public IEnumerator PlayerDeathRemovesExistingBolt() // 사망 후 남은 마법탄 정리
    {
        speedField.SetValue(trial, 1f); // 사망 전 유지되는 느린 마법탄
        MoveOtherSlimesAway(1); // 모든 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 시험 마법탄 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Not.Null); // 발사된 마법탄 존재
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 플레이어 사망 처리
        yield return null; // 사망 후 마법탄 정리 대기
        yield return null; // 표시 오브젝트 제거 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // 사망 후 이전 마법탄 제거
    }

    [UnityTest] // 폭발 표시 중 플레이어 사망 사례
    public IEnumerator PlayerDeathRemovesExplosionEffect() // 사망 후 남은 폭발 표시 정리
    {
        speedField.SetValue(trial, 1000f); // 빠른 충돌을 위한 시험 속도
        Fighter target = PlaceSlime(1, new Vector2(2f, 0f)); // 직접 적중할 적
        MoveOtherSlimesAway(2); // 다른 적의 간섭 제거
        PressTowards(new Vector2(3f, 0f)); // 적 방향 발사
        float deadline = Time.realtimeSinceStartup + 2f; // 폭발 표시 대기 상한
        GameObject effect = GameObject.Find("Staff Explosion"); // 현재 폭발 표시
        while (effect == null && Time.realtimeSinceStartup < deadline) // 폭발 표시가 나타날 때까지 대기
        {
            yield return null; // 다음 프레임 확인
            effect = GameObject.Find("Staff Explosion"); // 현재 폭발 표시 재확인
        }
        bool boltPresent = GameObject.Find("Magic Bolt") != null; // 마법탄 잔존 여부
        string diagnostics = $"health={target.Health}, bolt={boltPresent}, time={Time.time}, dt={Time.deltaTime}"; // 폭발 누락 원인 진단 정보
        Assert.That(effect, Is.Not.Null, diagnostics); // 기존 폭발 표시 확인
        Assert.That(target.Health, Is.EqualTo(33)); // 실제 폭발 피해 확인
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 플레이어 사망 처리
        yield return null; // 사망 후 표시 정리 대기
        yield return null; // 표시 오브젝트 제거 대기
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 사망 후 폭발 표시 제거
    }

    [UnityTest] // 재시작 상태 사례
    public IEnumerator SceneRestartRemovesExistingBolt() // 재시작 시 마법탄 초기화
    {
        speedField.SetValue(trial, 1f); // 씬 전환 전 유지되는 느린 마법탄
        MoveOtherSlimesAway(1); // 모든 적의 간섭 제거
        PressTowards(new Vector2(5f, 0f)); // 시험 마법탄 발사
        yield return null; // 발사 처리 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Not.Null); // 발사된 마법탄 존재
        ReleaseAt(new Vector2(5f, 0f)); // 재시작 전 버튼 해제
        yield return null; // 버튼 해제 처리 대기
        SceneManager.LoadScene("CombatPrototype"); // 전투 씬 재시작
        yield return null; // 씬 전환 대기
        yield return null; // 새 전투 초기화 대기
        Assert.That(GameObject.Find("Magic Bolt"), Is.Null); // 이전 전투 마법탄 제거
        Assert.That(GameObject.Find("Staff Explosion"), Is.Null); // 이전 전투 폭발 효과 제거
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적 확인
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적의 체력 상태
    }

    private static void MoveOtherSlimesAway(int firstIndex) // 시험과 무관한 적 이동
    {
        for (int index = firstIndex; index <= 6; index++) // 나머지 적 순회
        {
            PlaceSlime(index, new Vector2(-4f, index * 0.5f)); // 반대 방향의 안전한 시험 위치
        }
    }

    private void CreateObstacle(Vector2 position, Vector2 size, int layer, bool trigger) // 시험 장애물 생성
    {
        GameObject obstacle = new GameObject("Test Obstacle"); // 시험 장애물 오브젝트
        obstacle.layer = layer; // 지정된 충돌 계층
        obstacle.transform.position = position; // 장애물 위치
        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>(); // 장애물 충돌체
        collider.size = size; // 시험 장애물 크기
        collider.isTrigger = trigger; // 트리거 여부
        obstacles.Add(obstacle); // 시험 후 정리 대상 등록
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

    private static int CountNamed(string name) // 현재 표시 오브젝트 수 확인
    {
        int count = 0; // 일치하는 오브젝트 수
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) // 활성 씬 오브젝트 검사
        {
            if (item.name == name) // 시험 오브젝트 이름 확인
            {
                count++; // 일치하는 오브젝트 수 증가
            }
        }
        return count; // 확인된 오브젝트 수
    }
}
