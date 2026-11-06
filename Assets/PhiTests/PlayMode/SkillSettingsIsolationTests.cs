using System.Collections; // 프레임 단위 시험
using System.Collections.Generic; // 시험 설정 복제본 목록
using System.Reflection; // 씬 설정 참조 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 전투 설정과 스킬 자료형
using UnityEngine; // 유니티 자료형
using UnityEngine.Events; // 씬 로드 콜백 자료형
using UnityEngine.SceneManagement; // 시험 씬 전환
using UnityEngine.TestTools; // 플레이 모드 시험

public class SkillSettingsIsolationTests // 다른 전투 시험의 스킬 설정 격리 검증
{
    [UnityTest] // 잘못된 사용자 스킬 조합 사례
    public IEnumerator InvalidSourceIsNotChangedAndRestartFixtureUsesValidCopy() // 원본 보존과 재시작 설정 격리 검증
    {
        SceneMapTestFixture map = new SceneMapTestFixture(); // 씬 맵의 독립 시험 배치
        List<PrototypeSettings> sources = new List<PrototypeSettings>(); // 임시 원본 설정 목록
        PrototypeSettings source = null; // 잘못된 값을 모사하는 임시 원본
        PrototypeSettings asset = null; // 씬에 연결된 원본 설정 에셋
        SkillId originalQ = SkillId.None; // 원본 에셋의 Q 슬롯
        SkillId originalE = SkillId.None; // 원본 에셋의 E 슬롯
        SkillId originalR = SkillId.None; // 원본 에셋의 R 슬롯
        UnityAction<Scene, LoadSceneMode> inject = (scene, mode) => // 새 씬에 임시 원본 설정 연결
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 새 씬 루트 검사
            {
                PrototypeBootstrap bootstrap = root.GetComponentInChildren<PrototypeBootstrap>(true); // 새 씬의 전투 구성 요소
                if (bootstrap != null) // 전투 구성 요소 존재 여부
                {
                    FieldInfo field = typeof(PrototypeBootstrap).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic); // 씬 설정 필드
                    asset = (PrototypeSettings)field.GetValue(bootstrap); // 원본 설정 에셋 보관
                    originalQ = asset.startingSkillQ; // 원본 Q 슬롯 보관
                    originalE = asset.startingSkillE; // 원본 E 슬롯 보관
                    originalR = asset.startingSkillR; // 원본 R 슬롯 보관
                    source = Object.Instantiate(asset); // 원본을 변경하지 않는 설정 복제
                    source.startingSkillQ = SkillId.None; // 빈 Q 슬롯을 가진 시험 원본
                    source.startingSkillE = SkillId.IceShard; // 중복된 E 슬롯을 가진 시험 원본
                    source.startingSkillR = SkillId.IceShard; // 중복된 R 슬롯을 가진 시험 원본
                    sources.Add(source); // 시험 후 정리할 임시 원본 등록
                    field.SetValue(bootstrap, source); // 새 씬에 시험 원본 설정 연결
                    break; // 전투 구성 요소 검색 종료
                }
            }
        };
        SceneManager.sceneLoaded += inject; // 임시 원본 설정 이벤트 등록
        SkillSettingsTestFixture fixture = new SkillSettingsTestFixture(); // 그 뒤에 적용되는 스킬 격리
        try // 시험 이벤트와 자원 정리 보장
        {
            for (int index = 0; index < 2; index++) // 최초 진입과 재시작 검사
            {
                SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
                yield return null; // 씬 로드와 전투 시작 대기
                yield return null; // 실행 스킬 상태 초기화 대기
                Assert.That(source, Is.Not.Null); // 시험 원본 설정 존재 확인
                Assert.That(source.startingSkillQ, Is.EqualTo(SkillId.None)); // 시험 원본의 빈 Q 유지
                Assert.That(source.startingSkillE, Is.EqualTo(SkillId.IceShard)); // 시험 원본의 E 유지
                Assert.That(source.startingSkillR, Is.EqualTo(SkillId.IceShard)); // 시험 원본의 중복 R 유지
                Assert.That(asset.startingSkillQ, Is.EqualTo(originalQ)); // 사용자 에셋 Q 보존
                Assert.That(asset.startingSkillE, Is.EqualTo(originalE)); // 사용자 에셋 E 보존
                Assert.That(asset.startingSkillR, Is.EqualTo(originalR)); // 사용자 에셋 R 보존
                PrototypeBootstrap bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>(); // 실제 전투 구성 요소
                Assert.That(bootstrap.EquippedSkills.Q, Is.EqualTo(SkillId.Fireball)); // 독립 실행 Q 슬롯
                Assert.That(bootstrap.EquippedSkills.E, Is.EqualTo(SkillId.WhirlwindSlash)); // 독립 실행 E 슬롯
                Assert.That(bootstrap.EquippedSkills.R, Is.EqualTo(SkillId.HealingLight)); // 독립 실행 R 슬롯
                Assert.That(bootstrap.EquippedSkills.QError, Is.EqualTo(SkillSlotError.None)); // 시험 스킬 오류 방지
            }
        }
        finally // 시험 임시 상태 정리
        {
            fixture.Dispose(); // 복제 설정과 격리 이벤트 정리
            SceneManager.sceneLoaded -= inject; // 임시 원본 설정 이벤트 해제
            map.Dispose(); // 시험 맵 설정 이벤트와 그림 정리
            foreach (PrototypeSettings item in sources) // 시험 원본 복제본 순회
            {
                if (item != null) // 시험 원본 존재 여부
                {
                    Object.Destroy(item); // 시험 원본 복제본 제거
                }
            }
        }
    }
}
