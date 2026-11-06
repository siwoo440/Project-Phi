using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEditor; // 씬 참조 확인 도구
using UnityEditor.SceneManagement; // 프리뷰 씬 관리
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 씬 자료형

public class SceneMapHierarchyTests // 저장된 맵 구성 검증
{
    private const string ScenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 전투 씬 경로
    private const string SpritePath = "Assets/Phi/Art/MapSquare.png"; // 맵 스프라이트 경로
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
        Assert.That(area.center.x, Is.EqualTo(0f).Within(0.001f)); // 기본 가로 중심
        Assert.That(area.center.y, Is.EqualTo(0f).Within(0.001f)); // 기본 세로 중심
        Assert.That(area.extents.x, Is.EqualTo(10f).Within(0.001f)); // 기본 가로 반폭
        Assert.That(area.extents.y, Is.EqualTo(6f).Within(0.001f)); // 기본 세로 반높이
        string[] names = { "Boundary Top", "Boundary Bottom", "Boundary Left", "Boundary Right" }; // 필수 경계 이름
        foreach (string boundaryName in names) // 네 방향 경계 검사
        {
            Transform boundary = floor.Find(boundaryName); // 바닥의 경계 자식 확인
            Assert.That(boundary, Is.Not.Null, boundaryName); // 저장된 경계 존재
            AssertRenderer(boundary.GetComponent<SpriteRenderer>()); // 경계 스프라이트 확인
        }
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

    private static void AssertRenderer(SpriteRenderer renderer) // 맵 그림 참조 확인
    {
        Assert.That(renderer, Is.Not.Null); // 스프라이트 렌더러 존재
        Assert.That(renderer.enabled, Is.True); // 스프라이트 표시 활성화
        Assert.That(renderer.sprite, Is.Not.Null); // 실제 스프라이트 에셋 연결
        Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Is.EqualTo(SpritePath)); // 맵 에셋 경로 일치
    }
}
