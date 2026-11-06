using System.Collections; // 프레임 단위 시험
using System.Reflection; // 설정 참조 확인
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 구성 요소
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 형식
using UnityEngine.SceneManagement; // 전투 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험

public class SettingsBindingTests // 씬 설정 연결 검증
{
    [UnityTest] // 설정 적용 사례
    public IEnumerator SceneSettingsControlPlayerHealth() // 씬에 연결된 설정의 실제 사용 검증
    {
        FieldInfo settingsField = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 참조 필드
        PrototypeBootstrap bootstrap = null; // 로드된 전투 구성 요소
        PrototypeSettings original = null; // 씬에 저장된 설정 에셋
        PrototypeSettings trial = null; // 원본을 보존할 임시 설정
        UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => // 시작 전 씬 로드 콜백
        {
            bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 로드된 전투 구성 요소 확인
            if (bootstrap != null) // 전투 구성 요소 존재 여부
            {
                original = (PrototypeSettings)settingsField.GetValue(bootstrap); // 씬 설정 참조 보관
                if (original != null) // 연결된 설정 에셋 확인
                {
                    trial = Object.Instantiate(original); // 원본 설정의 임시 복제
                    trial.playerHealth = 137; // 기본값과 다른 시험 체력
                    settingsField.SetValue(bootstrap, trial); // 시작 전에 임시 설정 연결
                }
            }
        };
        SceneManager.sceneLoaded += loaded; // 씬 로드 콜백 등록
        try // 시험 자원 정리 범위
        {
            SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 열기
            yield return null; // 씬 로드와 전투 시작 대기
            yield return null; // 플레이어 생성 대기
            Assert.That(bootstrap, Is.Not.Null, "전투 씬에 전투 구성 요소가 있어야 합니다."); // 씬 오브젝트 확인
            Assert.That(original, Is.Not.Null, "전투 씬에 설정 에셋이 연결되어야 합니다."); // 설정 에셋 연결 검증
            Fighter player = GameObject.Find("Player").GetComponent<Fighter>(); // 생성된 플레이어 전투 상태
            Assert.That(player.Health, Is.EqualTo(137)); // 설정에 따른 시작 체력 검증
        }
        finally // 시험 설정 정리
        {
            SceneManager.sceneLoaded -= loaded; // 씬 로드 콜백 해제
            if (trial != null) // 임시 설정 존재 여부
            {
                Object.Destroy(trial); // 임시 설정 제거
            }
        }
    }
}
