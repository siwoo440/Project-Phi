using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 장애물 목록
using System.Reflection; // 전투 입력 시험 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 형식
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험

public class SwordOcclusionTests // 검과 장애물 검증
{
    private const int ObstacleLayer = 8; // 시험 장애물 계층
    private const int PlayerLayer = 9; // 시험 플레이어 계층
    private const int EnemyLayer = 10; // 시험 적 계층
    private readonly List<GameObject> obstacles = new List<GameObject>(); // 시험 장애물 목록
    private PrototypeSettings trial; // 원본을 보존할 임시 설정
    private PrototypeBootstrap bootstrap; // 실제 전투 구성 요소
    private bool previousQueriesHitTriggers; // 기존 트리거 검사 설정

    [UnitySetUp] // 각 시험 전 준비
    public IEnumerator SetUp() // 독립 전투 씬 준비
    {
        previousQueriesHitTriggers = Physics2D.queriesHitTriggers; // 기존 트리거 설정 보관
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 전투 시작 전 설정 교체
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 전투 구성 요소 확인
            trial = ScriptableObject.CreateInstance<PrototypeSettings>(); // 임시 전투 설정 생성
            trial.slimeCount = 4; // 최대 타격 수를 넘는 시험 적 수
            trial.slimeSpeed = 0f; // 시험 중 적 위치 유지
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, trial); // 전투 시작 설정 교체
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
        yield return null; // 씬 로드 대기
        yield return null; // 전투 대상 생성 대기
        SceneManager.sceneLoaded -= loaded; // 씬 로드 콜백 해제
        typeof(PrototypeBootstrap).GetField("aim", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bootstrap, Vector2.right); // 오른쪽 검 조준
    }

    [UnityTearDown] // 각 시험 후 정리
    public IEnumerator TearDown() // 시험 자원과 전역 설정 복원
    {
        Physics2D.queriesHitTriggers = previousQueriesHitTriggers; // 기존 트리거 설정 복원
        foreach (GameObject obstacle in obstacles) // 시험 장애물 검사
        {
            if (obstacle != null) // 장애물 존재 여부
            {
                Object.Destroy(obstacle); // 시험 장애물 제거
            }
        }
        obstacles.Clear(); // 시험 장애물 목록 초기화
        if (trial != null) // 임시 설정 존재 여부
        {
            Object.Destroy(trial); // 임시 설정 제거
        }
        yield return null; // 제거 처리 대기
    }

    [UnityTest] // 전투 계층 설정 사례
    public IEnumerator ActorsUseDedicatedCollisionLayers() // 지정된 플레이어와 적 계층 검증
    {
        Assert.That(LayerMask.NameToLayer("PhiObstacle"), Is.EqualTo(ObstacleLayer)); // 장애물 계층 이름과 위치
        Assert.That(LayerMask.NameToLayer("PhiPlayer"), Is.EqualTo(PlayerLayer)); // 플레이어 계층 이름과 위치
        Assert.That(LayerMask.NameToLayer("PhiEnemy"), Is.EqualTo(EnemyLayer)); // 적 계층 이름과 위치
        Assert.That(GameObject.Find("Player").layer, Is.EqualTo(PlayerLayer)); // 실제 플레이어 계층
        Assert.That(GameObject.Find("Slime 1").layer, Is.EqualTo(EnemyLayer)); // 실제 적 계층
        yield return null; // 프레임 단위 시험 완료
    }

    [UnityTest] // 벽 앞과 뒤 적 사례
    public IEnumerator OccludedEnemyDoesNotConsumeTargetLimit() // 가려진 적 제외와 최대 적중 수 검증
    {
        Fighter hidden = PositionSlime(1, new Vector2(0.8f, 0f)); // 벽 뒤에 있는 가까운 적
        Fighter front = PositionSlime(2, new Vector2(0.4f, 0f)); // 벽 앞에 있는 적
        Fighter upper = PositionSlime(3, new Vector2(1.1f, 0.6f)); // 벽을 벗어난 위쪽 적
        Fighter lower = PositionSlime(4, new Vector2(1.2f, -0.6f)); // 벽을 벗어난 아래쪽 적
        CreateObstacle(new Vector2(0.65f, 0f), ObstacleLayer, false); // 검 방향의 실제 벽
        Strike(); // 실제 검 타격
        Assert.That(hidden.Health, Is.EqualTo(50)); // 벽 뒤 적의 피해 차단
        Assert.That(front.Health, Is.EqualTo(29)); // 벽 앞 적의 피해 21
        Assert.That(upper.Health, Is.EqualTo(29)); // 가려지지 않은 위쪽 적의 피해
        Assert.That(lower.Health, Is.EqualTo(29)); // 가려진 적이 대상 수를 소비하지 않음
        yield return null; // 프레임 단위 시험 완료
    }

    [UnityTest] // 다른 계층과 트리거 사례
    public IEnumerator NonObstacleAndTriggerCollidersDoNotBlockSword() // 무관한 충돌체와 트리거 제외 검증
    {
        Fighter target = PositionSlime(1, new Vector2(1.4f, 0f)); // 검 범위 안의 적
        PositionSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PositionSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        PositionSlime(4, new Vector2(-4f, -1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(0.5f, 0f), 0, false); // 장애물 계층이 아닌 일반 충돌체
        CreateObstacle(new Vector2(0.9f, 0f), ObstacleLayer, true); // 장애물 계층의 트리거
        Physics2D.queriesHitTriggers = true; // 전역 트리거 검사가 켜진 조건
        Strike(); // 실제 검 타격
        Assert.That(target.Health, Is.EqualTo(29)); // 무관한 충돌체와 트리거 무시
        yield return null; // 프레임 단위 시험 완료
    }

    [UnityTest] // 트리거 뒤 실제 벽 사례
    public IEnumerator TriggerBeforeWallDoesNotHideBlockingWall() // 앞쪽 트리거와 뒤쪽 벽 구분 검증
    {
        Fighter target = PositionSlime(1, new Vector2(1.4f, 0f)); // 벽 뒤의 적
        PositionSlime(2, new Vector2(-4f, 0f)); // 다른 적의 간섭 제거
        PositionSlime(3, new Vector2(-4f, 1f)); // 다른 적의 간섭 제거
        PositionSlime(4, new Vector2(-4f, -1f)); // 다른 적의 간섭 제거
        CreateObstacle(new Vector2(0.5f, 0f), ObstacleLayer, true); // 벽 앞의 트리거
        CreateObstacle(new Vector2(0.9f, 0f), ObstacleLayer, false); // 트리거 뒤의 실제 벽
        Physics2D.queriesHitTriggers = true; // 전역 트리거 검사가 켜진 조건
        Strike(); // 실제 검 타격
        Assert.That(target.Health, Is.EqualTo(50)); // 트리거 뒤 실제 벽의 피해 차단
        yield return null; // 프레임 단위 시험 완료
    }

    [UnityTest] // 기본 적중 상한 사례
    public IEnumerator UnblockedSwordHitsOnlyThreeEnemies() // 벽 없는 기존 피해와 대상 상한 검증
    {
        Fighter first = PositionSlime(1, new Vector2(0.6f, 0f)); // 첫 번째 적
        Fighter second = PositionSlime(2, new Vector2(0.9f, 0f)); // 두 번째 적
        Fighter third = PositionSlime(3, new Vector2(1.2f, 0f)); // 세 번째 적
        Fighter fourth = PositionSlime(4, new Vector2(1.5f, 0f)); // 네 번째 적
        Strike(); // 실제 검 타격
        Assert.That(first.Health, Is.EqualTo(29)); // 첫 대상 1회 피해
        Assert.That(second.Health, Is.EqualTo(29)); // 두 번째 대상 1회 피해
        Assert.That(third.Health, Is.EqualTo(29)); // 세 번째 대상 1회 피해
        Assert.That(fourth.Health, Is.EqualTo(50)); // 네 번째 대상 제외
        yield return null; // 프레임 단위 시험 완료
    }

    private Fighter PositionSlime(int index, Vector2 position) // 시험 적 위치 배치
    {
        GameObject slime = GameObject.Find("Slime " + index); // 번호에 맞는 적 확인
        slime.transform.position = position; // 시험 위치 적용
        return slime.GetComponent<Fighter>(); // 적의 체력 상태
    }

    private void CreateObstacle(Vector2 position, int layer, bool trigger) // 시험용 충돌체 생성
    {
        GameObject obstacle = new GameObject("Test Obstacle"); // 시험 장애물 오브젝트
        obstacle.layer = layer; // 지정된 충돌 계층
        obstacle.transform.position = position; // 장애물 위치
        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>(); // 장애물 충돌체
        collider.size = new Vector2(0.15f, 0.18f); // 좁은 장애물 크기
        collider.isTrigger = trigger; // 트리거 여부
        obstacles.Add(obstacle); // 시험 후 정리 대상 등록
    }

    private void Strike() // 실제 검 공격 실행
    {
        Physics2D.SyncTransforms(); // 바뀐 장애물 위치의 물리 반영
        typeof(PrototypeBootstrap).GetMethod("Attack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bootstrap, null); // 실제 검 타격 호출
    }
}
