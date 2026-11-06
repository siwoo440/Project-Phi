using System; // 시험용 정리 인터페이스
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 시험 씬 로드 이벤트

internal sealed class SceneMapTestFixture : IDisposable // 독립 씬 맵 시험 설정
{
    private readonly Sprite square; // 시험 인스턴스 전용 사각형 그림

    public SceneMapTestFixture() // 시험 씬 설정 준비
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f); // 일정한 시험용 그림 생성
        SceneManager.sceneLoaded += ConfigureLoadedScene; // 로드된 씬 인스턴스 설정 연결
    }

    public void Dispose() // 시험 자원 정리
    {
        SceneManager.sceneLoaded -= ConfigureLoadedScene; // 씬 설정 이벤트 해제
        if (square != null) // 시험용 그림 존재 여부
        {
            UnityEngine.Object.Destroy(square); // 시험용 그림 제거
        }
    }

    private void ConfigureLoadedScene(Scene scene, LoadSceneMode mode) // 로드된 씬 인스턴스의 시험 범위 설정
    {
        if (scene.name != "CombatPrototype") // 시험 전투 씬 확인
        {
            return; // 다른 씬 변경 방지
        }
        foreach (GameObject root in scene.GetRootGameObjects()) // 새 씬 루트 순회
        {
            if (root.name == "Map") // 시험할 씬 맵 확인
            {
                root.transform.position = Vector3.zero; // 시험용 맵 중심
                root.transform.rotation = Quaternion.identity; // 시험용 맵 회전
                root.transform.localScale = Vector3.one; // 시험용 맵 루트 크기
                Transform floor = root.transform.Find("Map Floor"); // 기존 바닥 오브젝트
                if (floor == null) // 바닥 누락 확인
                {
                    return; // 누락된 오브젝트 생성 금지
                }
                Transform wall = floor.Find("Internal Wall"); // 기본 씬의 내부 장애물
                if (wall != null) // 저장된 장애물 존재 여부
                {
                    wall.gameObject.SetActive(false); // 독립 전투 시험의 벽 간섭 제외
                }
                floor.localPosition = Vector3.zero; // 시험용 바닥 중심
                floor.localRotation = Quaternion.identity; // 시험용 바닥 회전
                floor.localScale = new Vector3(20f, 12f, 1f); // 시험용 바닥 크기
                SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>(); // 기존 바닥 렌더러
                if (renderer != null) // 바닥 렌더러 존재 여부
                {
                    renderer.sprite = square; // 시험 인스턴스의 바닥 그림
                }
                ConfigureBoundary(floor, "Boundary Top", new Vector3(0f, 0.495f, 0f), new Vector3(1f, 0.01f, 1f)); // 위쪽 경계의 시험 좌표
                ConfigureBoundary(floor, "Boundary Bottom", new Vector3(0f, -0.495f, 0f), new Vector3(1f, 0.01f, 1f)); // 아래쪽 경계의 시험 좌표
                ConfigureBoundary(floor, "Boundary Left", new Vector3(-0.497f, 0f, 0f), new Vector3(0.006f, 1f, 1f)); // 왼쪽 경계의 시험 좌표
                ConfigureBoundary(floor, "Boundary Right", new Vector3(0.497f, 0f, 0f), new Vector3(0.006f, 1f, 1f)); // 오른쪽 경계의 시험 좌표
                return; // 해당 씬 설정 완료
            }
        }
    }

    private void ConfigureBoundary(Transform floor, string name, Vector3 position, Vector3 scale) // 기존 경계의 시험 배치
    {
        Transform boundary = floor.Find(name); // 저장된 경계 오브젝트
        if (boundary == null) // 경계 누락 확인
        {
            return; // 누락된 경계 생성 금지
        }
        boundary.localPosition = position; // 시험 경계 위치
        boundary.localRotation = Quaternion.identity; // 시험 경계 회전
        boundary.localScale = scale; // 시험 경계 크기
        SpriteRenderer renderer = boundary.GetComponent<SpriteRenderer>(); // 기존 경계 렌더러
        if (renderer != null) // 경계 렌더러 존재 여부
        {
            renderer.sprite = square; // 시험 인스턴스의 경계 그림
        }
    }
}
