using System; // 자료형과 열거형 변환
using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 대상 목록
using System.Linq; // 활성 전리품 선택
using System.Reflection; // 미구현 상태와 설정 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.EventSystems; // 선택된 UI 확인
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 가상 입력 상태
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 상호작용 상태와 입력 필드
using Object = UnityEngine.Object; // 유니티 오브젝트 구분

public class InteractionPlayModeTests // 상호작용과 임시 전리품 검증
{
    private Keyboard keyboard; // 가상 키보드
    private Mouse mouse; // 가상 마우스
    private PrototypeSettings trial; // 독립 시험 설정
    private PrototypeBootstrap bootstrap; // 전투 구성 요소
    private SceneMapTestFixture mapFixture; // 시험용 씬 맵 인스턴스
    private UnityAction<Scene, LoadSceneMode> sceneCallback; // 독립 설정 연결 콜백
    private readonly List<GameObject> objects = new List<GameObject>(); // 시험 대상 오브젝트 목록
    private bool previousRunInBackground; // 기존 실행 설정
    private InputSettings.BackgroundBehavior previousBackgroundBehavior; // 기존 포커스 설정
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior; // 기존 입력 경로

    [UnitySetUp] // 각 시험 준비
    public IEnumerator SetUp() // 독립 전투와 가상 입력 준비
    {
        previousRunInBackground = Application.runInBackground; // 기존 실행 설정 보관
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 입력 경로 보관
        Application.runInBackground = true; // 백그라운드 시험 실행
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 가상 입력 포커스 제한 해제
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면 입력 전달
        keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 생성
        mouse = InputSystem.AddDevice<Mouse>(); // 가상 마우스 생성
        mapFixture = new SceneMapTestFixture(); // 기존 씬 맵의 독립 시험 배치
        yield return LoadBattle(); // 시험 설정으로 전투 시작
        PlaceSlime(1, new Vector2(-4f, 3f)); // 첫 적의 전투 간섭 제거
        PlaceSlime(2, new Vector2(-4f, 4f)); // 두 번째 적의 전투 간섭 제거
        PlaceSlime(3, new Vector2(-4f, -3f)); // 세 번째 적의 전투 간섭 제거
    }

    [UnityTearDown] // 각 시험 정리
    public IEnumerator TearDown() // 시험 자원과 전역 입력 복원
    {
        if (sceneCallback != null) // 씬 로드 콜백 존재 여부
        {
            SceneManager.sceneLoaded -= sceneCallback; // 남은 콜백 해제
        }
        if (keyboard != null && keyboard.added) // 가상 키보드 존재 여부
        {
            InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
        }
        if (mouse != null && mouse.added) // 가상 마우스 존재 여부
        {
            InputSystem.RemoveDevice(mouse); // 가상 마우스 제거
        }
        foreach (GameObject item in objects) // 시험 대상 목록 순회
        {
            if (item != null) // 시험 대상 존재 여부
            {
                Object.Destroy(item); // 시험 대상 제거
            }
        }
        if (mapFixture != null) // 시험 맵 격리 존재 여부
        {
            mapFixture.Dispose(); // 시험 맵 이벤트와 그림 정리
        }
        if (trial != null) // 독립 설정 존재 여부
        {
            Object.Destroy(trial); // 시험 설정 제거
        }
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior; // 기존 입력 경로 복원
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior; // 기존 포커스 설정 복원
        Application.runInBackground = previousRunInBackground; // 기존 실행 설정 복원
        yield return null; // 시험 자원 정리 대기
    }

    [UnityTest] // 슬라임 전리품 생성과 중복 사례
    public IEnumerator SlimeDeathCreatesExactlyOneLoot() // 사망과 중복 피해의 전리품 검증
    {
        Fighter slime = PlaceSlime(1, new Vector2(1f, 0f)); // 플레이어 가까이의 슬라임
        slime.TakeDamage(50); // 슬라임 사망 처리
        yield return null; // 전리품 생성 대기
        yield return null; // 사망 처리 완료 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 슬라임 한 마리의 임시 전리품 한 개
        slime.TakeDamage(50); // 이미 사망한 슬라임의 추가 피해
        yield return null; // 중복 사망 처리 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 같은 적의 중복 전리품 방지
    }

    [UnityTest] // 같은 프레임 다중 사망 사례
    public IEnumerator MultipleSlimeDeathsAndPlayerDeathCreateOneEachWithoutPickup() // 다중 사망과 사망 중 수집 차단
    {
        Fighter first = PlaceSlime(1, new Vector2(0.6f, 0f)); // 첫 번째 시험 슬라임
        Fighter second = PlaceSlime(2, new Vector2(1f, 0f)); // 두 번째 시험 슬라임
        first.TakeDamage(50); // 첫 슬라임 사망
        second.TakeDamage(50); // 같은 프레임의 두 번째 사망
        first.TakeDamage(50); // 첫 사망 상태의 중복 피해
        second.TakeDamage(50); // 두 번째 사망 상태의 중복 피해
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 같은 프레임의 플레이어 사망
        Press(Key.F); // 사망 중 수집 시도
        yield return null; // 사망과 입력 처리 대기
        Assert.That(Loot().Length, Is.EqualTo(2)); // 각 슬라임의 독립 전리품 한 개
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 플레이어 사망 중 수집 차단
    }

    [UnityTest] // 검 전투의 전리품 사례
    public IEnumerator SwordKillCreatesLoot() // 실제 검 공격과 전리품 생성 연결
    {
        Fighter slime = PlaceSlime(1, new Vector2(1f, 0f)); // 검 사거리 안의 슬라임
        slime.TakeDamage(40); // 한 번의 검 공격으로 처치 가능한 체력
        yield return AttackAt(new Vector2(3f, 0f)); // 오른쪽을 향한 실제 검 공격
        yield return null; // 사망 전리품 처리 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 검 처치의 단일 전리품
    }

    [UnityTest] // 활 전투의 전리품 사례
    public IEnumerator BowKillCreatesLoot() // 실제 활 투사체와 전리품 생성 연결
    {
        Fighter slime = PlaceSlime(1, new Vector2(2f, 0f)); // 활 투사체 대상
        slime.TakeDamage(40); // 한 번의 화살로 처치 가능한 체력
        yield return ClickButton("Equip Bow"); // 실제 무기 UI의 활 선택
        yield return AttackAt(new Vector2(4f, 0f)); // 오른쪽 활 공격
        yield return new WaitForSeconds(0.4f); // 화살 적중 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 활 처치의 단일 전리품
    }

    [UnityTest] // 지팡이 전투의 전리품 사례
    public IEnumerator StaffKillCreatesLoot() // 실제 지팡이 폭발과 전리품 생성 연결
    {
        Fighter slime = PlaceSlime(1, new Vector2(2f, 0f)); // 마법탄과 폭발 대상
        slime.TakeDamage(40); // 한 번의 지팡이 공격으로 처치 가능한 체력
        yield return ClickButton("Equip Staff"); // 실제 무기 UI의 지팡이 선택
        yield return AttackAt(new Vector2(4f, 0f)); // 오른쪽 지팡이 공격
        yield return new WaitForSeconds(0.5f); // 투사체와 폭발 처리 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 지팡이 처치의 단일 전리품
    }

    [UnityTest] // 스킬 처치의 전리품 사례
    public IEnumerator FireballKillCreatesLoot() // 실제 스킬 피해와 전리품 생성 연결
    {
        Fighter slime = PlaceSlime(1, new Vector2(2f, 0f)); // 화염탄 대상
        slime.TakeDamage(40); // 한 번의 스킬로 처치 가능한 체력
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(4f, 0f, 0f)); // 대상 방향의 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 화염탄 조준
        yield return null; // 조준 입력 반영 대기
        Press(Key.Q); // 실제 화염탄 발동
        yield return new WaitForSeconds(0.4f); // 화염탄 적중 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 스킬 처치의 단일 전리품
    }

    [UnityTest] // 가장 가까운 전리품과 단일 F 입력 사례
    public IEnumerator FCollectsOnlyNearestLootAndHoldingDoesNotRepeat() // 선택·단일 수집·중복 방지 검증
    {
        PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 가까운 적의 임시 전리품
        PlaceSlime(2, new Vector2(1.2f, 0f)).TakeDamage(50); // 먼 적의 임시 전리품
        yield return null; // 두 전리품 생성 대기
        yield return null; // 가장 가까운 대상 선택 대기
        GameObject[] loot = Loot(); // 생성된 전리품 목록
        Assert.That(loot.Length, Is.EqualTo(2)); // 두 슬라임의 독립 전리품
        GameObject nearest = loot.OrderBy(item => item.transform.position.sqrMagnitude).First(); // 플레이어에게 가까운 전리품
        Assert.That(SelectedObject(), Is.SameAs(nearest)); // F 입력 전 가까운 대상 선택
        Press(Key.F); // 전리품 수집 키 누름 유지
        yield return null; // 단일 대상 수집 처리
        yield return new WaitForSeconds(0.7f); // 긴 누름의 반복 수집 확인
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 한 번 누름의 단일 수집
        Assert.That(Loot().Length, Is.EqualTo(1)); // 나머지 한 개의 보존
        Press(); // F 키 해제
        yield return null; // 키 해제 처리
        Press(Key.F); // 두 번째 F 누름
        yield return null; // 두 번째 전리품 수집 처리
        Assert.That(CollectedCount(), Is.EqualTo(2)); // 별도 입력의 나머지 수집
        Assert.That(Loot().Length, Is.EqualTo(0)); // 수집된 전리품의 비활성화
    }

    [UnityTest] // 수집 알림 중 재진입 사례
    public IEnumerator LootCannotBeCollectedAgainDuringItsOwnNotification() // 지연 삭제 전 중복 지급 방지 검증
    {
        Interactable target = (Interactable)CreateTarget("Loot", "TEST LOOT", new Vector2(0.5f, 0f)); // 독립 전리품 인스턴스
        int notifications = 0; // 실제 수집 알림 횟수
        target.Interacted += current => // 수집 알림의 재진입 시도
        {
            notifications++; // 수집 알림 횟수 증가
            Assert.That(current.IsAvailable, Is.False); // 알림 전에 수집 완료 상태 표시
            Assert.That(current.TryInteract(), Is.False); // 알림 중 같은 전리품 재수집 차단
        };
        Assert.That(target.ItemId, Is.EqualTo("test.slime_gel")); // 표시 이름과 분리된 시험 식별자
        Assert.That(target.TryInteract(), Is.True); // 최초 수집 성공
        Assert.That(notifications, Is.EqualTo(1)); // 하나의 수집 알림만 전달
        Assert.That(target.gameObject.activeSelf, Is.False); // 지연 삭제 전 즉시 비활성화
        yield return null; // 전리품 제거 대기
    }

    [UnityTest] // 후보 거리와 표시 사례
    public IEnumerator NearestEligibleTargetIsMarkedAndChangesWithDistance() // F 입력 전 선택과 화면 표시 검증
    {
        Component npc = CreateTarget("Npc", "TEST NPC", new Vector2(0.5f, 0f)); // 가까운 NPC 시험 대상
        Component chest = CreateTarget("Chest", "TEST CHEST", new Vector2(1f, 0f)); // 더 먼 상자 시험 대상
        yield return null; // 선택 갱신 대기
        Assert.That(SelectedObject(), Is.SameAs(npc.gameObject)); // 가까운 대상 선택
        Assert.That(Selected(npc), Is.True); // 가까운 대상의 선택 표시
        Assert.That(Selected(chest), Is.False); // 다른 대상의 비선택 표시
        GameObject status = GameObject.Find("Interaction Status"); // 상호작용 상태 안내
        Assert.That(status, Is.Not.Null); // 안내 오브젝트 존재 확인
        StringAssert.Contains("TEST NPC", status.GetComponent<Text>().text); // 실제 선택한 대상 이름 표시
        Assert.That(GameObject.Find("Interaction Marker"), Is.Not.Null); // 선택 대상의 월드 표시
        npc.transform.position = new Vector2(2f, 0f); // NPC를 상호작용 범위 밖으로 이동
        yield return null; // 선택 변경 대기
        Assert.That(SelectedObject(), Is.SameAs(chest.gameObject)); // 남은 가까운 대상 선택
        Assert.That(Selected(npc), Is.False); // 기존 대상의 선택 표시 해제
        Assert.That(Selected(chest), Is.True); // 새 대상의 선택 표시 적용
    }

    [UnityTest] // 같은 거리의 후보 사례
    public IEnumerator EqualDistanceUsesActivationOrderAndUpdatesAfterDisable() // 동률과 비활성 후보 검증
    {
        Component first = CreateTarget("Npc", "FIRST", new Vector2(1f, 0f)); // 먼저 활성화된 대상
        Component second = CreateTarget("Door", "SECOND", new Vector2(0f, 1f)); // 같은 거리에 나중 활성화된 대상
        yield return null; // 동률 대상 선택 대기
        Assert.That(SelectedObject(), Is.SameAs(first.gameObject)); // 활성화 등록 순서 우선
        first.gameObject.SetActive(false); // 선택된 대상을 비활성화
        yield return null; // 활성 후보 재선택 대기
        Assert.That(SelectedObject(), Is.SameAs(second.gameObject)); // 남은 활성 대상 선택
        first.gameObject.SetActive(true); // 첫 대상을 나중에 다시 활성화
        yield return null; // 동률 등록 순서 갱신 대기
        Assert.That(SelectedObject(), Is.SameAs(second.gameObject)); // 새 활성화 순서 유지
    }

    [UnityTest] // 사거리 밖 전리품 사례
    public IEnumerator OutOfRangeLootCannotBeSelectedOrCollected() // 거리 밖 F 입력 차단
    {
        PlaceSlime(1, new Vector2(2f, 0f)).TakeDamage(50); // 반경 1.5 밖의 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 후보 검색 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 멀리 있는 전리품 존재
        Assert.That(SelectedObject(), Is.Null); // 범위 밖 대상 미선택
        Press(Key.F); // 범위 밖 전리품 수집 시도
        yield return null; // 입력 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 범위 밖 수집 차단
        Assert.That(Loot().Length, Is.EqualTo(1)); // 범위 밖 전리품 보존
    }

    [UnityTest] // 사거리 경계 사례
    public IEnumerator InteractionRangeIncludesExactBoundaryButExcludesBeyondIt() // 중심 거리의 반경 경계 검증
    {
        Component target = CreateTarget("Chest", "RANGE TEST", new Vector2(1.5f, 0f)); // 정확한 사거리 경계 대상
        yield return null; // 경계 대상 검색 대기
        Assert.That(SelectedObject(), Is.SameAs(target.gameObject)); // 정확한 사거리의 대상 선택
        target.transform.position = new Vector2(1.501f, 0f); // 사거리보다 조금 먼 위치
        yield return null; // 거리 변경 반영 대기
        Assert.That(SelectedObject(), Is.Null); // 반경 밖의 대상 미선택
    }

    [UnityTest] // 장애물 뒤 후보 사례
    public IEnumerator ObstacleBlocksLootAndAccessibleTargetIsSelected() // 비트리거 벽과 후보 선택 검증
    {
        PlaceSlime(1, new Vector2(1.2f, 0f)).TakeDamage(50); // 벽 뒤의 전리품
        CreateWall(new Vector2(0.6f, 0f)); // 플레이어와 전리품 사이 벽
        Component npc = CreateTarget("Npc", "VISIBLE NPC", new Vector2(0f, 1f)); // 벽에 가리지 않은 후보
        yield return null; // 전리품 생성 대기
        yield return null; // 후보 검색 대기
        Assert.That(SelectedObject(), Is.SameAs(npc.gameObject)); // 장애물에 가리지 않은 대상 선택
        Press(Key.F); // NPC 상호작용 시도
        yield return null; // 입력 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 다른 종류 대상에서 전리품 수집 차단
        Assert.That(Loot().Length, Is.EqualTo(1)); // 벽 뒤 전리품 보존
    }

    [UnityTest] // 트리거 장애물 사례
    public IEnumerator TriggerObstacleDoesNotBlockLootSelection() // 비차단 충돌체와 수집 검증
    {
        PlaceSlime(1, new Vector2(1.2f, 0f)).TakeDamage(50); // 트리거 뒤의 전리품
        CreateWall(new Vector2(0.6f, 0f)); // 전리품 앞의 시험 충돌체
        GameObject.Find("Interaction Test Wall").GetComponent<BoxCollider2D>().isTrigger = true; // 비차단 트리거 설정
        Physics2D.SyncTransforms(); // 트리거 상태 반영
        yield return null; // 전리품 생성 대기
        yield return null; // 후보 검색 대기
        Assert.That(SelectedObject(), Is.Not.Null); // 트리거 뒤 전리품 선택
        Press(Key.F); // 전리품 수집 입력
        yield return null; // 수집 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 트리거를 통과한 수집
    }

    [UnityTest] // 상호작용 대상 자신의 충돌체 사례
    public IEnumerator TargetChildColliderDoesNotOccludeItsOwnInteraction() // 대상 자식 장애물 충돌체의 자기 차단 방지
    {
        Component chest = CreateTarget("Chest", "TEST CHEST", new Vector2(1f, 0f)); // 범위 안의 시험 상자
        GameObject child = new GameObject("Chest Child Collider"); // 시험 대상의 자식 충돌체
        child.transform.SetParent(chest.transform, false); // 상자 아래의 충돌체 배치
        child.layer = LayerMask.NameToLayer("PhiObstacle"); // 장애물 판정 계층
        child.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f); // 상자를 둘러싼 충돌체
        Physics2D.SyncTransforms(); // 충돌 검사에 위치 반영
        yield return null; // 후보 선택 대기
        Assert.That(SelectedObject(), Is.SameAs(chest.gameObject)); // 대상 자신의 충돌체 무시
    }

    [UnityTest] // 아직 구현되지 않은 NPC 사례
    public IEnumerator NpcInteractionDoesNotCollectLootOrChangeTarget() // 미구현 대상의 안전한 안내 검증
    {
        Component npc = CreateTarget("Npc", "TEST NPC", new Vector2(0.5f, 0f)); // 전투 중 시험 NPC
        PlaceSlime(1, new Vector2(1.2f, 0f)).TakeDamage(50); // 뒤쪽의 수집 가능 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 후보 선택 대기
        Press(Key.F); // 가까운 NPC와 상호작용
        yield return null; // 상호작용 처리 대기
        Assert.That(SelectedObject(), Is.SameAs(npc.gameObject)); // 미구현 NPC 대상 유지
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 뒤쪽 전리품의 동시 수집 방지
        Assert.That(Loot().Length, Is.EqualTo(1)); // 실제 전리품 보존
        StringAssert.Contains("NOT IMPLEMENTED", GameObject.Find("Interaction Status").GetComponent<Text>().text); // 미구현 안내 표시
    }

    [UnityTest] // 플레이어 사망 사례
    public IEnumerator DeadPlayerCannotCollectLoot() // 사망 중 상호작용 입력 차단
    {
        PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 범위 안의 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 대상 선택 대기
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(100); // 플레이어 사망 처리
        Press(Key.F); // 사망 중 수집 시도
        yield return null; // 사망과 입력 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 사망 중 수집 차단
        Assert.That(Loot().Length, Is.EqualTo(1)); // 수집하지 않은 전리품 유지
    }

    [UnityTest] // 회피와 동시 상호작용 사례
    public IEnumerator DodgeBlocksFUntilMovementEnds() // 회피 우선순위와 종료 후 수집 검증
    {
        PlaceSlime(1, new Vector2(0f, 0.7f)).TakeDamage(50); // 회피 도착점과 가까운 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 대상 선택 대기
        Press(Key.W, Key.LeftShift, Key.F); // 위쪽 회피와 수집 동시 입력
        yield return null; // 회피 우선순위 처리
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 회피 시작 시 수집 차단
        Press(); // 회피와 수집 키 해제
        yield return new WaitForSeconds(0.35f); // 회피 이동 종료 대기
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 짧은 F 입력의 지연 수집 방지
        Press(Key.F); // 회피 종료 후 새 수집 입력
        yield return null; // 전리품 수집 처리
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 회피 종료 후 수집 허용
    }

    [UnityTest] // 텍스트 편집 포커스 사례
    public IEnumerator TextEditingBlocksFButButtonSelectionAllowsIt() // F 입력과 UI 텍스트 편집 분리
    {
        PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 범위 안의 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 대상 선택 대기
        GameObject fieldObject = new GameObject("Interaction Test Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)); // 시험용 입력 필드
        fieldObject.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform, false); // 기존 캔버스의 시험 필드
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); // 입력 문자열 표시
        textObject.transform.SetParent(fieldObject.transform, false); // 필드 자식 글자
        Text text = textObject.GetComponent<Text>(); // 글자 표시 구성 요소
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 시험용 기본 글꼴
        InputField input = fieldObject.GetComponent<InputField>(); // 시험 입력 필드
        input.textComponent = text; // 입력과 표시 글자 연결
        objects.Add(fieldObject); // 시험 후 입력 필드 제거
        EventSystem.current.SetSelectedGameObject(fieldObject); // 시험 입력 필드 선택
        input.ActivateInputField(); // 실제 텍스트 편집 시작
        yield return null; // 입력 필드 포커스 반영
        yield return null; // 입력 필드 활성화 대기
        Assert.That(input.isFocused, Is.True); // 실제 편집 상태 확인
        Press(Key.F); // 텍스트 편집 중 F 입력
        yield return null; // 입력 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 편집 중 수집 방지
        Press(); // F 키 해제
        input.DeactivateInputField(); // 텍스트 편집 종료
        yield return null; // 포커스와 키 해제 반영
        EventSystem.current.SetSelectedGameObject(GameObject.Find("Equip Bow")); // 일반 버튼 선택
        Press(Key.F); // 버튼 선택 상태의 F 입력
        yield return null; // 입력 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 일반 버튼 선택 중 수집 허용
    }

    [UnityTest] // 전투 재시작 사례
    public IEnumerator RestartClearsLootAndTemporaryCollectionCount() // 출정마다 독립적인 전리품 상태
    {
        PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 범위 안의 시험 전리품
        yield return null; // 전리품 생성 대기
        yield return null; // 대상 선택 대기
        Press(Key.F); // 전리품 수집
        yield return null; // 수집 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 이전 전투의 임시 수집 기록
        PlaceSlime(2, new Vector2(1.2f, 0f)).TakeDamage(50); // 수집하지 않은 두 번째 전리품
        yield return null; // 두 번째 전리품 생성 대기
        Assert.That(Loot().Length, Is.EqualTo(1)); // 재시작 전 남은 전리품
        Press(); // 수집 키 해제
        PrototypeSettings previous = trial; // 이전 전투의 독립 설정
        yield return LoadBattle(); // 새 전투 씬 로드
        Object.Destroy(previous); // 이전 독립 설정 제거
        Assert.That(CollectedCount(), Is.EqualTo(0)); // 새 전투의 임시 수집 수 초기화
        Assert.That(Loot().Length, Is.EqualTo(0)); // 이전 전투 전리품 정리
    }

    [UnityTest] // 반복 재시작 후 등록 목록 사례
    public IEnumerator RepeatedRestartsDoNotRetainPreviousLootCandidates() // 정적 후보 목록의 씬 전환 정리 검증
    {
        for (int index = 0; index < 3; index++) // 세 번의 전투 반복
        {
            PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 이전 전투의 전리품 생성
            yield return null; // 전리품 생성 대기
            yield return null; // 후보 선택 대기
            Assert.That(SelectedObject(), Is.Not.Null); // 이전 전투의 후보 선택 확인
            PrototypeSettings previous = trial; // 이전 전투 설정
            yield return LoadBattle(); // 독립 새 전투 시작
            Object.Destroy(previous); // 이전 설정 제거
            Assert.That(Loot().Length, Is.EqualTo(0)); // 이전 씬의 전리품 제거
            Assert.That(SelectedObject(), Is.Null); // 이전 등록 목록의 후보 미선택
            Assert.That(CollectedCount(), Is.EqualTo(0)); // 새 전투의 카운트 초기화
        }
        PlaceSlime(1, new Vector2(0.7f, 0f)).TakeDamage(50); // 최종 전투의 새 전리품
        yield return null; // 새 전리품 생성 대기
        yield return null; // 새 후보 선택 대기
        Press(Key.F); // 최종 전투의 수집 입력
        yield return null; // 수집 처리 대기
        Assert.That(CollectedCount(), Is.EqualTo(1)); // 반복 재시작 뒤 정상 수집
    }

    private IEnumerator LoadBattle() // 독립 시험 설정으로 전투 씬 로드
    {
        sceneCallback = (scene, mode) => // 전투 초기화 전 시험 설정
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 전투 구성 요소
            FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 참조 필드
            PrototypeSettings asset = (PrototypeSettings)field.GetValue(bootstrap); // 원본 사용자 설정
            trial = Object.Instantiate(asset); // 원본을 보존하는 독립 복제 설정
            trial.playerHealth = 100; // 시험 플레이어 체력
            trial.playerAttack = 10; // 시험 기본 공격력
            trial.playerDefense = 5; // 시험 기본 방어력
            trial.playerSpeed = 4f; // 시험 일반 이동속도
            trial.startingWeapon = WeaponType.Sword; // 시험 시작 무기
            trial.swordAttack = 12; // 시험 검 공격력
            trial.swordRange = 1.7f; // 시험 검 사거리
            trial.swordAngle = 90f; // 시험 검 각도
            trial.swordTargets = 3; // 시험 검 최대 대상 수
            trial.bowAttack = 10; // 시험 활 공격력
            trial.bowRange = 7f; // 시험 활 사거리
            trial.bowProjectileSpeed = 14f; // 시험 화살 이동속도
            trial.staffAttack = 8; // 시험 지팡이 공격력
            trial.staffRange = 5.5f; // 시험 지팡이 사거리
            trial.staffProjectileSpeed = 9f; // 시험 마법탄 이동속도
            trial.staffExplosionRadius = 1.1f; // 시험 마법탄 폭발 반경
            trial.staffExplosionTargets = 4; // 시험 마법탄 최대 폭발 대상
            trial.fireballSpeed = 14f; // 시험 화염탄 이동속도
            trial.fireballRange = 7f; // 시험 화염탄 사거리
            trial.fireballMultiplier = 1.4f; // 시험 화염탄 피해 배율
            trial.fireballCost = 10; // 시험 화염탄 마나 비용
            trial.playerMana = 50; // 시험 최대 마나
            trial.slimeCount = 3; // 시험 슬라임 수
            trial.slimeHealth = 50; // 시험 슬라임 체력
            trial.slimeDefense = 2; // 시험 슬라임 방어력
            trial.slimeSpeed = 0f; // 시험 중 적 이동 방지
            trial.slimeAttack = 8; // 시험 슬라임 공격력
            trial.slimeAttackRange = 0.9f; // 시험 슬라임 공격 거리
            trial.slimeAttackInterval = 1.2f; // 시험 슬라임 공격 간격
            trial.slimeWindup = 0.4f; // 시험 슬라임 공격 예고 시간
            trial.slimeDashDistance = 0.8f; // 시험 슬라임 돌진 거리
            trial.slimeDashSpeed = 5f; // 시험 슬라임 돌진 속도
            trial.slimeHitDistance = 0.65f; // 시험 슬라임 적중 거리
            trial.playerStamina = 100; // 시험 최대 스태미나
            trial.dodgeCost = 25; // 시험 회피 비용
            trial.dodgeCooldown = 1.2f; // 시험 회피 대기시간
            trial.dodgeDuration = 0.25f; // 시험 회피 이동 시간
            trial.dodgeDistance = 2f; // 시험 회피 이동 거리
            trial.dodgeInvulnerability = 0.25f; // 시험 회피 무적 시간
            SkillSettingsTestFixture.UseRecommended(trial); // 유효한 시작 스킬 조합
            FieldInfo range = typeof(PrototypeSettings).GetField("interactionRange"); // 상호작용 거리 설정
            if (range != null) // 구현 이후 시험 거리 지정
            {
                range.SetValue(trial, Convert.ChangeType(1.5f, range.FieldType)); // 기획에 없는 임시 시험 반경
            }
            field.SetValue(bootstrap, trial); // 전투 시작 전 독립 설정 연결
        };
        SceneManager.sceneLoaded += sceneCallback; // 시험 설정 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 초기화 대기
        SceneManager.sceneLoaded -= sceneCallback; // 시험 설정 콜백 해제
        sceneCallback = null; // 중복 해제 방지
    }

    private static Fighter PlaceSlime(int index, Vector2 position) // 시험 슬라임 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 슬라임
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적 체력 상태
    }

    private Component CreateTarget(string kind, string displayName, Vector2 position) // 시험용 상호작용 대상 생성
    {
        Type componentType = typeof(PrototypeSettings).Assembly.GetType("Phi.Interactable"); // 공용 상호작용 컴포넌트
        Type kindType = typeof(PrototypeSettings).Assembly.GetType("Phi.InteractionKind"); // 대상 종류 열거형
        Assert.That(componentType, Is.Not.Null, "공용 상호작용 대상이 필요합니다."); // 대상 자료형 확인
        Assert.That(kindType, Is.Not.Null, "상호작용 대상 종류가 필요합니다."); // 대상 종류 확인
        GameObject target = new GameObject("Interaction Test " + displayName); // 독립 시험 대상 오브젝트
        target.transform.position = position; // 시험 대상 위치
        objects.Add(target); // 시험 후 정리 대상 등록
        Component component = target.AddComponent(componentType); // 실제 상호작용 구성 요소 추가
        MethodInfo configure = componentType.GetMethod("Configure"); // 대상 초기화 메서드
        Assert.That(configure, Is.Not.Null); // 초기화 기능 확인
        configure.Invoke(component, new object[] { Enum.Parse(kindType, kind), displayName }); // 시험 대상 종류와 이름 설정
        return component; // 설정된 시험 대상
    }

    private static bool Selected(Component target) // 시험 대상의 선택 표시 상태
    {
        PropertyInfo property = target.GetType().GetProperty("IsSelected"); // 대상 선택 상태 속성
        Assert.That(property, Is.Not.Null); // 선택 상태 속성 확인
        return (bool)property.GetValue(target); // 현재 선택 여부
    }

    private void CreateWall(Vector2 position) // 전리품 앞의 시험 장애물 생성
    {
        GameObject wall = new GameObject("Interaction Test Wall"); // 독립 시험 장애물
        wall.layer = LayerMask.NameToLayer("PhiObstacle"); // 실제 장애물 계층
        wall.transform.position = position; // 시험 벽 위치
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>(); // 시험 벽 충돌체
        collider.size = new Vector2(0.1f, 1f); // 대상 앞의 얇은 벽
        objects.Add(wall); // 시험 후 정리 대상 등록
        Physics2D.SyncTransforms(); // 충돌 검사에 위치 반영
    }

    private static GameObject[] Loot() // 씬의 활성 임시 전리품 목록
    {
        return Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(item => item.name == "Slime Loot" && item.gameObject.activeInHierarchy).Select(item => item.gameObject).ToArray(); // 활성 전리품 오브젝트
    }

    private void Press(params Key[] keys) // 지정 키의 동시 누름
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 가상 키 입력
    }

    private IEnumerator AttackAt(Vector2 world) // 지정한 월드 좌표의 실제 공격
    {
        Vector2 screen = Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f)); // 목표 방향 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 공격 방향의 마우스 이동
        yield return null; // 조준 입력 반영 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 좌클릭 공격 입력
        yield return null; // 공격 입력 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 공격 버튼 해제
        yield return null; // 버튼 해제 처리 대기
    }

    private IEnumerator ClickButton(string name) // 실제 무기 버튼 클릭
    {
        GameObject button = GameObject.Find(name); // 지정한 무기 버튼
        Assert.That(button, Is.Not.Null); // 버튼 존재 확인
        RectTransform rect = button.GetComponent<RectTransform>(); // 버튼 화면 배치
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); // 버튼 중앙 화면 좌표
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 버튼 위 포인터 이동
        yield return null; // 포인터 이동 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); // 버튼 누름
        yield return null; // 버튼 누름 처리 대기
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); // 버튼 해제
        yield return null; // 버튼 클릭 처리 대기
    }

    private int CollectedCount() // 전투 한정 임시 수집 개수
    {
        PropertyInfo property = typeof(PrototypeBootstrap).GetProperty("CollectedLootCount"); // 임시 수집 수 속성
        Assert.That(property, Is.Not.Null, "전리품 수집 상태가 필요합니다."); // 수집 수 공개 속성 확인
        return Convert.ToInt32(property.GetValue(bootstrap)); // 현재 수집 수
    }

    private GameObject SelectedObject() // 선택된 상호작용 대상
    {
        PropertyInfo property = typeof(PrototypeBootstrap).GetProperty("SelectedInteraction"); // 선택 대상 속성
        Assert.That(property, Is.Not.Null, "상호작용 선택 상태가 필요합니다."); // 선택 대상 공개 속성 확인
        Component selected = property.GetValue(bootstrap) as Component; // 선택된 상호작용 컴포넌트
        return selected != null ? selected.gameObject : null; // 선택 오브젝트 또는 없음
    }
}
