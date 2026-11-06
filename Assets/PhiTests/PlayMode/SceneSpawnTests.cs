using System.Collections; // 프레임 단위 시험
using System.Reflection; // 전투 설정 참조
using NUnit.Framework; // 시험 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백
using UnityEngine.SceneManagement; // 시험 씬 로드
using UnityEngine.TestTools; // 플레이 모드 시험
using Object = UnityEngine.Object; // 유니티 객체 구분

public class SceneSpawnTests // 24일차 씬 지점과 밀집 시험
{
    private SceneMapTestFixture map; // 독립 씬 맵
    private PrototypeSettings trial; // 독립 전투 설정
    private UnityAction<Scene, LoadSceneMode> loaded; // 씬 설정 콜백

    [UnitySetUp] // 각 시험의 맵 준비
    public IEnumerator SetUp() // 기존 시험 맵 격리
    {
        map = new SceneMapTestFixture(); // 기존 맵 인스턴스 설정
        yield return null; // 준비 프레임
    }

    [UnityTearDown] // 각 시험의 설정 정리
    public IEnumerator TearDown() // 설정과 콜백 복원
    {
        if (loaded != null) // 남은 콜백 여부
        {
            SceneManager.sceneLoaded -= loaded; // 콜백 해제
        }
        if (trial != null) // 복제 설정 존재 여부
        {
            Object.Destroy(trial); // 독립 설정 제거
        }
        map.Dispose(); // 맵 설정 해제
        yield return null; // 자원 제거 대기
    }

    [UnityTest] // 씬에서 이동한 시작 지점 사례
    public IEnumerator InitialEnemyUsesAuthoredPoint() // 시작 위치와 씬 지점 일치
    {
        yield return LoadBattle(1, 20, true, false); // 이동한 시작 지점으로 전투 시작
        GameObject slime = GameObject.Find("Slime 1"); // 실제 시작 슬라임
        Assert.That(slime, Is.Not.Null); // 시작 적 생성 확인
        Assert.That(Vector2.Distance(slime.transform.position, new Vector2(6f, 0f)), Is.LessThan(0.01f)); // 씬 지점의 실제 좌표 사용
    }

    [UnityTest] // 추가 구역의 일회성 등장 사례
    public IEnumerator EncounterActivatesOnceAndSpawnsFiveSpecies() // 진입과 재진입 검증
    {
        yield return LoadBattle(1, 20, false, false); // 시작 적 한 마리의 전투
        Transform floor = GameObject.Find("Map Floor").transform; // 실제 씬 바닥
        Transform group = floor.Find("Spawn Areas/Encounter"); // 저장된 구역 지점
        group.gameObject.SetActive(true); // 이 시험의 구역 활성화
        Assert.That(GameObject.Find("Goblin 1"), Is.Null); // 진입 전 추가 적 부재
        GameObject player = GameObject.Find("Player"); // 실제 플레이어
        player.transform.position = floor.Find("Encounter Trigger").position; // 저장된 구역으로 진입
        yield return null; // 구역 등장 처리
        yield return null; // 전투 대상 초기화
        string[] names = // 구역에서 등장할 다섯 적
        {
            "Slime 2", // 추가 슬라임
            "Goblin 1", // 추가 고블린
            "Skeleton 1", // 추가 궁수
            "Orc 1", // 추가 오크
            "Wolf 1" // 추가 늑대
        }; // 검증할 적 이름 목록
        foreach (string name in names) // 다섯 종류 순회
        {
            Assert.That(GameObject.Find(name), Is.Not.Null, name); // 종류별 추가 등장 확인
        }
        GameObject.Find("Goblin 1").GetComponent<Fighter>().TakeDamage(10000); // 구역 적 제거
        player.transform.position = Vector3.zero; // 구역 이탈
        yield return null; // 이탈 처리
        player.transform.position = floor.Find("Encounter Trigger").position; // 같은 구역 재진입
        yield return null; // 재진입 처리
        Assert.That(GameObject.Find("Goblin 2"), Is.Null); // 소진된 요청 재생성 방지
        Assert.That(GameObject.Find("Slime 3"), Is.Null); // 중복 구역 등장 방지
    }

    [UnityTest] // 동시 생존 상한과 대기 사례
    public IEnumerator LimitQueuesRemainingEnemiesUntilOneDies() // 밀집 상한과 대기 요청 검증
    {
        yield return LoadBattle(3, 2, false, false); // 세 요청과 두 마리 상한
        Assert.That(GameObject.Find("Slime 1"), Is.Not.Null); // 첫 적 생성
        Assert.That(GameObject.Find("Slime 2"), Is.Not.Null); // 두 번째 적 생성
        Assert.That(GameObject.Find("Slime 3"), Is.Null); // 상한을 넘는 생성 대기
        GameObject.Find("Slime 1").GetComponent<Fighter>().TakeDamage(10000); // 생존 적 한 마리 제거
        yield return null; // 대기 요청 처리
        Assert.That(GameObject.Find("Slime 3"), Is.Not.Null); // 남은 요청의 실제 생성
        GameObject.Find("Slime 2").GetComponent<Fighter>().TakeDamage(10000); // 추가 적 제거
        yield return null; // 소진된 요청 확인
        Assert.That(GameObject.Find("Slime 4"), Is.Null); // 소진된 요청의 부활 방지
    }

    [UnityTest] // 장애물로 막힌 스폰 지점 사례
    public IEnumerator BlockedPointsWaitUntilAStoredPointBecomesFree() // 장애물과 대기 요청 검증
    {
        yield return LoadBattle(1, 20, false, true); // 모든 슬라임 지점을 벽 안에 배치
        Assert.That(GameObject.Find("Slime 1"), Is.Null); // 벽 안 생성 방지
        Transform point = GameObject.Find("Map Floor").transform.Find("Spawn Areas/Initial/Slime Spawn 1"); // 저장된 첫 슬라임 지점
        point.position = new Vector3(6f, 0f, 0f); // 지점을 벽 밖으로 이동
        yield return null; // 대기 요청 재검사
        GameObject slime = GameObject.Find("Slime 1"); // 실제 생성된 슬라임
        Assert.That(slime, Is.Not.Null); // 안전해진 지점의 등장
        Assert.That(Vector2.Distance(slime.transform.position, point.position), Is.LessThan(0.01f)); // 이동한 씬 지점 사용
    }

    [UnityTest] // 플레이어 사망 중 대기 요청 사례
    public IEnumerator PlayerDeathStopsPendingSpawns() // 사망 후 추가 등장 방지
    {
        yield return LoadBattle(1, 0, false, false); // 상한 때문에 대기 중인 시작 적
        Assert.That(GameObject.Find("Slime 1"), Is.Null); // 상한에 따른 등장 대기
        GameObject.Find("Player").GetComponent<Fighter>().TakeDamage(10000); // 플레이어 사망
        PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
        typeof(PrototypeBootstrap).GetField("maxLivingEnemies", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 20); // 사망 후 상한 해제
        yield return null; // 사망 상태 프레임 처리
        Assert.That(GameObject.Find("Slime 1"), Is.Null); // 전투 종료 후 대기 적 생성 방지
    }

    [UnityTest] // 플레이어가 스폰 지점을 막은 사례
    public IEnumerator PlayerOccupyingAllPointsDelaysSpawn() // 플레이어 중첩 생성 방지
    {
        yield return LoadBattle(1, 20, false, false, true); // 모든 후보 지점을 맵 중심으로 이동
        Assert.That(GameObject.Find("Slime 1"), Is.Null); // 플레이어와 겹친 등장 방지
        GameObject.Find("Player").transform.position = new Vector3(2f, 0f, 0f); // 플레이어의 지점 이탈
        yield return null; // 안전해진 지점의 요청 처리
        GameObject slime = GameObject.Find("Slime 1"); // 대기 후 생성된 적
        Assert.That(slime, Is.Not.Null); // 플레이어 이탈 후 등장
        Assert.That(Vector2.Distance(slime.transform.position, Vector2.zero), Is.LessThan(0.01f)); // 씬에 지정된 중심 지점 사용
    }

    [UnityTest] // 같은 종류의 서로 다른 구역 지점 사례
    public IEnumerator EncounterKeepsEachBlockedPointPending() // 각 지점의 일회성 요청 보존
    {
        yield return LoadBattle(1, 20, false, false); // 시작 적 한 마리의 전투
        Transform floor = GameObject.Find("Map Floor").transform; // 현재 씬 바닥
        Transform group = floor.Find("Spawn Areas/Encounter"); // 구역 스폰 지점
        EnemySpawnPoint blocked = group.Find("Wolf").GetComponent<EnemySpawnPoint>(); // 종류를 변경할 구역 지점
        typeof(EnemySpawnPoint).GetField("species", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(blocked, EnemySpecies.Slime); // 두 번째 슬라임 지점
        blocked.transform.position = floor.Find("Internal Wall").position; // 두 번째 지점을 벽 안에 배치
        floor.Find("Internal Wall").gameObject.SetActive(true); // 실제 장애물 활성화
        group.gameObject.SetActive(true); // 구역 요청 활성화
        GameObject.Find("Player").transform.position = floor.Find("Encounter Trigger").position; // 구역 진입
        yield return null; // 첫 슬라임 지점의 등장 처리
        GameObject first = GameObject.Find("Slime 2"); // 안전한 첫 구역 슬라임
        Assert.That(first, Is.Not.Null); // 첫 지점의 적 생성
        first.transform.position = new Vector3(-6f, 0f, 0f); // 첫 지점을 다시 비움
        yield return null; // 막힌 지점의 대기 처리
        Assert.That(GameObject.Find("Slime 3"), Is.Null); // 이미 사용한 지점의 재사용 방지
        blocked.transform.position = new Vector3(8f, 0f, 0f); // 막힌 지점을 안전한 위치로 이동
        yield return null; // 남은 지점의 요청 처리
        GameObject second = GameObject.Find("Slime 3"); // 두 번째 구역 슬라임
        Assert.That(second, Is.Not.Null); // 안전해진 지점의 적 생성
        Assert.That(Vector2.Distance(second.transform.position, blocked.transform.position), Is.LessThan(0.01f)); // 대기하던 지점 사용
    }

    [UnityTest] // 씬 재시작의 구역 초기화 사례
    public IEnumerator RestartAllowsEncounterOnceAgain() // 재시작 후 구역 등장 검증
    {
        yield return LoadBattle(1, 20, false, false); // 시작 적 한 마리의 전투
        Transform floor = GameObject.Find("Map Floor").transform; // 현재 씬 바닥
        floor.Find("Spawn Areas/Encounter").gameObject.SetActive(true); // 시험용 추가 구역 활성화
        GameObject.Find("Player").transform.position = floor.Find("Encounter Trigger").position; // 구역 진입
        yield return null; // 첫 구역 등장 처리
        Assert.That(GameObject.Find("Goblin 1"), Is.Not.Null); // 첫 전투의 구역 적 생성
        loaded = (scene, mode) => // 새 씬의 시험 설정
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 새 전투 구성 요소
            typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, trial); // 같은 독립 설정 연결
        }; // 재시작 설정 콜백 끝
        SceneManager.sceneLoaded += loaded; // 새 씬 설정 콜백 연결
        SceneManager.LoadScene("CombatPrototype"); // 재시작과 같은 씬 재로드
        yield return null; // 새 전투 초기화
        SceneManager.sceneLoaded -= loaded; // 재시작 설정 콜백 해제
        loaded = null; // 중복 해제 방지
        floor = GameObject.Find("Map Floor").transform; // 재시작된 씬 바닥
        floor.Find("Spawn Areas/Encounter").gameObject.SetActive(true); // 새 전투의 구역 활성화
        GameObject.Find("Player").transform.position = floor.Find("Encounter Trigger").position; // 새 구역 진입
        yield return null; // 재시작 후 구역 등장 처리
        Assert.That(GameObject.Find("Goblin 1"), Is.Not.Null); // 새 구역 적 생성
    }

    private IEnumerator LoadBattle(int count, int limit, bool movePoint, bool blockPoints, bool blockWithPlayer = false) // 시험 설정으로 전투 씬 로드
    {
        loaded = (scene, mode) => // 시작 전 독립 설정
        {
            PrototypeBootstrap battle = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
            FieldInfo settings = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 필드
            trial = Object.Instantiate((PrototypeSettings)settings.GetValue(battle)); // 사용자 설정 복제
            trial.slimeCount = count; // 시험 시작 슬라임 수
            trial.goblinCount = 0; // 시작 고블린 제외
            trial.skeletonCount = 0; // 시작 궁수 제외
            trial.orcCount = 0; // 시작 오크 제외
            trial.wolfCount = 0; // 시작 늑대 제외
            trial.slimeSpeed = 0f; // 시작 슬라임 이동 방지
            trial.startingSkillQ = SkillId.Fireball; // 유효한 시험 Q 스킬
            trial.startingSkillE = SkillId.WhirlwindSlash; // 유효한 시험 E 스킬
            trial.startingSkillR = SkillId.HealingLight; // 유효한 시험 R 스킬
            settings.SetValue(battle, trial); // 시험 설정 연결
            typeof(PrototypeBootstrap).GetField("maxLivingEnemies", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, limit); // 시험 생존 상한
            Transform floor = GameObject.Find("Map Floor").transform; // 시험 씬 바닥
            Transform group = floor.Find("Spawn Areas/Initial"); // 시작 스폰 지점
            if (movePoint) // 지점 이동 시험 여부
            {
                group.Find("Slime Spawn 1").position = new Vector3(6f, 0f, 0f); // 저장 지점의 시험 위치
            }
            if (blockPoints) // 장애물 시험 여부
            {
                Transform wall = floor.Find("Internal Wall"); // 저장된 내부 장애물
                wall.gameObject.SetActive(true); // 시험용 벽 활성화
                foreach (EnemySpawnPoint point in group.GetComponentsInChildren<EnemySpawnPoint>()) // 시작 지점 순회
                {
                    if (point.Species == EnemySpecies.Slime) // 슬라임 지점 확인
                    {
                        point.transform.position = wall.position; // 모든 후보를 실제 벽 안에 배치
                    }
                }
            }
            if (blockWithPlayer) // 플레이어 중첩 시험 여부
            {
                foreach (EnemySpawnPoint point in group.GetComponentsInChildren<EnemySpawnPoint>()) // 시작 지점 순회
                {
                    if (point.Species == EnemySpecies.Slime) // 슬라임 지점 확인
                    {
                        point.transform.position = floor.position; // 모든 후보를 플레이어 시작점에 배치
                    }
                }
            }
        }; // 시험 설정 콜백 끝
        SceneManager.sceneLoaded += loaded; // 씬 로드 설정 연결
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
        yield return null; // 전투 생성 대기
        yield return null; // 초기 전투 처리 대기
        SceneManager.sceneLoaded -= loaded; // 설정 콜백 해제
        loaded = null; // 중복 해제 방지
    }
}
