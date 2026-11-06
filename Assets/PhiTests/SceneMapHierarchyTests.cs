using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEditor; // 씬 참조 확인 도구
using UnityEditor.SceneManagement; // 프리뷰 씬 관리
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 씬 자료형

public class SceneMapHierarchyTests // 저장된 맵 구성 검증
{
    private const string ScenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 전투 씬 경로
    private Scene preview; // 사용자 씬과 독립된 프리뷰 씬

    [SetUp] // 각 시험 전 준비
    public void OpenPreview() // 저장된 씬 열기
    {
        preview = EditorSceneManager.OpenPreviewScene(ScenePath); // 프리뷰 씬 열기
        Assert.That(preview.IsValid(), Is.True); // 씬 열기 성공 확인
        Assert.That(preview.isLoaded, Is.True); // 씬 로드 확인
    }

    [TearDown] // 각 시험 후 정리
    public void ClosePreview() // 사용자 씬을 보존하며 프리뷰 닫기
    {
        if (preview.IsValid() && preview.isLoaded) // 프리뷰 씬 열림 확인
        {
            EditorSceneManager.ClosePreviewScene(preview); // 프리뷰 씬 닫기
        }
    }

    [Test] // 저장된 맵과 경계 사례
    public void MapAndBoundariesExistBeforePlay() // 정적 맵 하이어라키 검증
    {
        Assert.That(EditorApplication.isPlaying, Is.False); // 플레이 실행 전 확인
        GameObject map = FindRoot("Map"); // 맵 루트 확인
        Assert.That(map, Is.Not.Null); // 정적 맵 루트 존재
        Transform floor = map.transform.Find("Map Floor"); // 맵 바닥 확인
        Assert.That(floor, Is.Not.Null); // 바닥 자식 존재
        SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>(); // 바닥 렌더러 확인
        AssertRenderer(floorRenderer); // 바닥 스프라이트 확인
        Bounds area = floorRenderer.bounds; // 저장된 바닥의 월드 영역
        AssertAxes(floor); // 사용 가능한 바닥 축 확인
        string[] names = { "Boundary Top", "Boundary Bottom", "Boundary Left", "Boundary Right" }; // 필수 경계 이름
        SpriteRenderer[] edges = new SpriteRenderer[names.Length]; // 실제 경계 렌더러 목록
        for (int index = 0; index < names.Length; index++) // 네 방향 경계 검사
        {
            Transform boundary = floor.Find(names[index]); // 바닥의 경계 자식 확인
            Assert.That(boundary, Is.Not.Null, names[index]); // 저장된 경계 존재
            edges[index] = boundary.GetComponent<SpriteRenderer>(); // 실제 경계 렌더러 보관
            AssertRenderer(edges[index]); // 경계 스프라이트 확인
            AssertAxes(boundary); // 사용 가능한 경계 축 확인
        }
        float left = edges[2].bounds.max.x; // 왼쪽 경계의 안쪽 면
        float right = edges[3].bounds.min.x; // 오른쪽 경계의 안쪽 면
        float bottom = edges[1].bounds.max.y; // 아래쪽 경계의 안쪽 면
        float top = edges[0].bounds.min.y; // 위쪽 경계의 안쪽 면
        float minimumSize = PrototypeSettings.SlimeRadius * 2f; // 가장 큰 적 도형의 최소 공간
        Assert.That(right - left, Is.GreaterThanOrEqualTo(minimumSize)); // 도형을 담는 가로 공간
        Assert.That(top - bottom, Is.GreaterThanOrEqualTo(minimumSize)); // 도형을 담는 세로 공간
        Assert.That(left, Is.GreaterThanOrEqualTo(area.min.x - 0.0001f)); // 바닥 내부의 왼쪽 면
        Assert.That(right, Is.LessThanOrEqualTo(area.max.x + 0.0001f)); // 바닥 내부의 오른쪽 면
        Assert.That(bottom, Is.GreaterThanOrEqualTo(area.min.y - 0.0001f)); // 바닥 내부의 아래쪽 면
        Assert.That(top, Is.LessThanOrEqualTo(area.max.y + 0.0001f)); // 바닥 내부의 위쪽 면
        Assert.That(edges[0].bounds.min.x, Is.LessThanOrEqualTo(left)); // 위쪽 벽의 왼쪽 연결
        Assert.That(edges[0].bounds.max.x, Is.GreaterThanOrEqualTo(right)); // 위쪽 벽의 오른쪽 연결
        Assert.That(edges[1].bounds.min.x, Is.LessThanOrEqualTo(left)); // 아래쪽 벽의 왼쪽 연결
        Assert.That(edges[1].bounds.max.x, Is.GreaterThanOrEqualTo(right)); // 아래쪽 벽의 오른쪽 연결
        Assert.That(edges[2].bounds.min.y, Is.LessThanOrEqualTo(bottom)); // 왼쪽 벽의 아래쪽 연결
        Assert.That(edges[2].bounds.max.y, Is.GreaterThanOrEqualTo(top)); // 왼쪽 벽의 위쪽 연결
        Assert.That(edges[3].bounds.min.y, Is.LessThanOrEqualTo(bottom)); // 오른쪽 벽의 아래쪽 연결
        Assert.That(edges[3].bounds.max.y, Is.GreaterThanOrEqualTo(top)); // 오른쪽 벽의 위쪽 연결
    }

    [Test] // 저장된 내부 장애물 사례
    public void InternalWallExistsInHierarchyBeforePlay() // 씬에 저장된 내부 벽 검증
    {
        GameObject map = FindRoot("Map"); // 저장된 맵 루트
        Assert.That(map, Is.Not.Null); // 맵 루트 존재 확인
        Transform wall = map.transform.Find("Map Floor/Internal Wall"); // 바닥 하이어라키의 내부 벽
        Assert.That(wall, Is.Not.Null); // 플레이 전 벽 오브젝트 존재 확인
        Assert.That(wall.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("PhiObstacle"))); // 장애물 충돌 계층
        AssertRenderer(wall.GetComponent<SpriteRenderer>()); // 눈에 보이는 벽 그림
        BoxCollider2D collider = wall.GetComponent<BoxCollider2D>(); // 저장된 벽 충돌체
        Assert.That(collider, Is.Not.Null); // 내부 벽 충돌체 존재
        Assert.That(collider.enabled, Is.True); // 벽 충돌체 활성화
        Assert.That(collider.isTrigger, Is.False); // 이동과 공격을 막는 벽 설정
    }

    [Test] // 저장된 바닥 참조 사례
    public void BootstrapReferencesSceneFloor() // 전투 구성 요소의 맵 연결 검증
    {
        GameObject map = FindRoot("Map"); // 맵 루트 확인
        Assert.That(map, Is.Not.Null); // 맵 루트 존재
        Transform floor = map.transform.Find("Map Floor"); // 바닥 자식 확인
        Assert.That(floor, Is.Not.Null); // 바닥 자식 존재
        SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>(); // 바닥 렌더러
        AssertRenderer(renderer); // 바닥 스프라이트 확인
        GameObject root = FindRoot("Combat Prototype"); // 전투 루트 확인
        Assert.That(root, Is.Not.Null); // 전투 루트 존재
        PrototypeBootstrap bootstrap = root.GetComponent<PrototypeBootstrap>(); // 전투 구성 요소
        Assert.That(bootstrap, Is.Not.Null); // 전투 구성 요소 존재
        SerializedObject serialized = new SerializedObject(bootstrap); // 저장된 전투 참조 확인
        SerializedProperty property = serialized.FindProperty("mapFloor"); // 바닥 참조 속성
        Assert.That(property, Is.Not.Null); // 바닥 참조 속성 존재
        Assert.That(property.objectReferenceValue, Is.SameAs(renderer)); // 같은 씬 바닥 참조 확인
        SerializedProperty settings = serialized.FindProperty("settings"); // 전투 설정 속성
        Assert.That(settings, Is.Not.Null); // 전투 설정 속성 존재
        Assert.That(settings.objectReferenceValue, Is.Not.Null); // 기존 설정 연결 보존 확인
    }

    private GameObject FindRoot(string name) // 프리뷰 씬의 루트 검색
    {
        foreach (GameObject candidate in preview.GetRootGameObjects()) // 씬 루트 순회
        {
            if (candidate.name == name) // 루트 이름 일치 확인
            {
                return candidate; // 해당 루트 반환
            }
        }
        return null; // 일치하는 루트 없음
    }

    private static void AssertAxes(Transform part) // 맵의 양수 축 정렬 확인
    {
        Vector3 axisX = part.localToWorldMatrix.MultiplyVector(Vector3.right); // 월드 가로축
        Vector3 axisY = part.localToWorldMatrix.MultiplyVector(Vector3.up); // 월드 세로축
        Assert.That(axisX.x, Is.GreaterThan(0f)); // 양수 가로 크기
        Assert.That(axisY.y, Is.GreaterThan(0f)); // 양수 세로 크기
        Assert.That(Mathf.Abs(axisX.y), Is.LessThanOrEqualTo(0.0001f)); // 가로축의 평면 정렬
        Assert.That(Mathf.Abs(axisY.x), Is.LessThanOrEqualTo(0.0001f)); // 세로축의 평면 정렬
        Assert.That(Mathf.Abs(axisX.z), Is.LessThanOrEqualTo(0.0001f)); // 가로축의 깊이 정렬
        Assert.That(Mathf.Abs(axisY.z), Is.LessThanOrEqualTo(0.0001f)); // 세로축의 깊이 정렬
    }
    private static void AssertRenderer(SpriteRenderer renderer) // 맵 그림 참조 확인
    {
        Assert.That(renderer, Is.Not.Null); // 스프라이트 렌더러 존재
        Assert.That(renderer.enabled, Is.True); // 스프라이트 표시 활성화
        Assert.That(renderer.gameObject.activeInHierarchy, Is.True); // 씬에서 활성화된 맵 오브젝트
        Assert.That(renderer.sprite, Is.Not.Null); // 실제 스프라이트 에셋 연결
    }
}


