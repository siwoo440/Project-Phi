using System.Collections.Generic; // 빌드 씬 목록 자료형
using System.IO; // 파일 존재 검사
using UnityEditor; // 유니티 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 씬 오브젝트 이동

namespace Phi.Editor // 에디터 도구 영역
{
    public static class PrototypeSceneBuilder // 시험 씬 생성 도구
    {
        private const string ScenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 시험 씬 저장 위치
        private const string SettingsPath = "Assets/Phi/Data/PrototypeSettings.asset"; // 설정 에셋 저장 위치

        [MenuItem("Phi/Create or Open Combat Prototype")] // 에디터 실행 메뉴
        public static void CreateScene() // 시험 씬 생성과 열기
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) // 미저장 씬 저장 여부 확인
            {
                return; // 취소 시 기존 씬 유지
            }
            PrototypeSettings settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(SettingsPath); // 기존 설정 에셋 확인
            if (settings == null) // 설정 에셋이 없는 경우
            {
                settings = ScriptableObject.CreateInstance<PrototypeSettings>(); // 기본 설정 생성
                AssetDatabase.CreateAsset(settings, SettingsPath); // 설정 에셋 저장
                AssetDatabase.SaveAssets(); // 에셋 저장 완료
            }
            if (!File.Exists(ScenePath)) // 기존 시험 씬 보존
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 기존 씬 확인 후 빈 씬 생성
                settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(SettingsPath); // 씬 변경 후 설정 에셋 재조회
                if (settings == null) // 설정 에셋 로드 실패 확인
                {
                    Debug.LogError("전투 설정 에셋을 불러오지 못했습니다: " + SettingsPath); // 설정 로드 오류 표시
                    return; // 설정 없는 씬 생성 중단
                }
                GameObject root = new GameObject("Combat Prototype"); // 시험 전투 루트
                PrototypeBootstrap bootstrap = root.AddComponent<PrototypeBootstrap>(); // 전투 생성기 부착
                SerializedObject serialized = new SerializedObject(bootstrap); // 설정 참조 편집기
                serialized.FindProperty("settings").objectReferenceValue = settings; // 설정 에셋 연결
                serialized.ApplyModifiedPropertiesWithoutUndo(); // 설정 참조 적용
                EditorSceneManager.SaveScene(scene, ScenePath); // 시험 씬 저장
            }
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); // 현재 빌드 씬 목록
            bool found = false; // 시험 씬 등록 여부
            foreach (EditorBuildSettingsScene scene in scenes) // 기존 빌드 씬 검사
            {
                if (scene.path == ScenePath) // 시험 씬 경로 일치
                {
                    scene.enabled = true; // 시험 씬 활성화
                    found = true; // 기존 등록 확인
                }
            }
            if (!found) // 시험 씬이 등록되지 않은 경우
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); // 시험 씬 추가
            }
            EditorBuildSettings.scenes = scenes.ToArray(); // 기존 씬을 보존하며 목록 갱신
            AssetDatabase.Refresh(); // 새 에셋 반영
            Scene openedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // 시험 씬 열기
            settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(SettingsPath); // 씬 변경 후 설정 에셋 재조회
            if (settings == null) // 설정 에셋 로드 실패 확인
            {
                Debug.LogError("전투 설정 에셋을 불러오지 못했습니다: " + SettingsPath); // 설정 로드 오류 표시
                return; // 설정 복구 중단
            }
            bool repaired = false; // 누락된 설정 연결 복구 여부
            foreach (GameObject root in openedScene.GetRootGameObjects()) // 씬 루트 검사
            {
                foreach (PrototypeBootstrap bootstrap in root.GetComponentsInChildren<PrototypeBootstrap>(true)) // 전투 생성기 검사
                {
                    if (EnsureSettingsReference(bootstrap, settings)) // 누락된 설정 참조 복구
                    {
                        repaired = true; // 복구 상태 기록
                    }
                }
            }
            if (repaired) // 설정 참조 변경 여부
            {
                EditorSceneManager.MarkSceneDirty(openedScene); // 씬 변경 상태 기록
                EditorSceneManager.SaveScene(openedScene); // 복구된 씬 저장
            }
        }

        public static bool EnsureSettingsReference(PrototypeBootstrap bootstrap, PrototypeSettings settings) // 누락된 설정 참조 복구
        {
            if (bootstrap == null || settings == null) // 복구 대상과 기본 설정 확인
            {
                return false; // 복구 불가 상태
            }
            SerializedObject serialized = new SerializedObject(bootstrap); // 설정 참조 편집기
            SerializedProperty property = serialized.FindProperty("settings"); // 설정 에셋 참조
            if (property.objectReferenceValue != null) // 기존 설정 참조 확인
            {
                return false; // 사용자 설정 보존
            }
            property.objectReferenceValue = settings; // 기본 설정 에셋 연결
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 누락된 참조 적용
            return true; // 참조 복구 완료
        }
    }
}


