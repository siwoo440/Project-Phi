using System; // 자료형 변환
using System.Collections.Generic; // 새 설정의 기본값 목록
using System.IO; // 시험 사본 파일 읽기와 쓰기
using System.Reflection; // 기존 설정 필드 확인
using System.Text.RegularExpressions; // 이전 형식의 시험 사본 구성
using NUnit.Framework; // 테스트 도구
using Phi; // 프로젝트 설정 자료형
using UnityEditor; // 에셋 데이터베이스

public class DodgeSettingsMigrationTests // 기존 설정 에셋의 회피 필드 이관 검증
{
    [Test] // 회피 설정이 없는 이전 에셋 사례
    public void LegacyAssetReceivesDodgeDefaultsAndPreservesExistingSettings() // 새 기본값과 원본 보존 검증
    {
        const string sourcePath = "Assets/Phi/Data/PrototypeSettings.asset"; // 원본 사용자 설정 경로
        const string scenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 원본 전투 씬 경로
        PrototypeSettings source = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(sourcePath); // 사용자 설정 에셋
        Assert.That(source, Is.Not.Null); // 원본 에셋 존재 확인
        string originalText = File.ReadAllText(sourcePath); // 원본 설정 파일 내용 보관
        string originalGuid = AssetDatabase.AssetPathToGUID(sourcePath); // 원본 설정 식별자 보관
        string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, false); // 원본 씬 참조 목록 보관
        string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PhiTests/TemporaryDodgeMigration.asset"); // 독립 시험 사본 경로
        Dictionary<string, float> defaults = new Dictionary<string, float> // 회피 기획 수치와 임시 설정
        {
            { "playerStamina", 100f }, // 기본 최대 스태미나
            { "staminaRegen", 15f }, // 초당 스태미나 재생
            { "staminaRegenDelay", 1f }, // 재생 대기시간
            { "dodgeCost", 25f }, // 회피 비용
            { "dodgeCooldown", 1.2f }, // 회피 재사용 대기시간
            { "dodgeDuration", 0.25f }, // 회피 이동 지속시간
            { "dodgeDistance", 2f }, // 임시 회피 거리
            { "dodgeInvulnerability", 0.25f } // 임시 회피 무적 시간
        };
        try // 시험 사본 정리 범위
        {
            Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True); // 사용자 에셋의 독립 사본 생성
            string content = File.ReadAllText(copyPath); // 사본의 직렬화 내용
            foreach (string name in defaults.Keys) // 새 회피 필드 순회
            {
                content = Regex.Replace(content, @"(?m)^  " + Regex.Escape(name) + @":[^\r\n]*(\r?\n|$)", ""); // 사본의 새 필드 제거
            }
            File.WriteAllText(copyPath, content); // 사본에만 이전 형식 적용
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); // 이전 설정 사본 다시 불러오기
            PrototypeSettings copy = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(copyPath); // 이전 형식의 Unity 로드 결과
            Assert.That(copy, Is.Not.Null); // 시험 사본 로드 확인
            foreach (KeyValuePair<string, float> entry in defaults) // 새 회피 필드 순회
            {
                FieldInfo field = typeof(PrototypeSettings).GetField(entry.Key); // 지정된 설정 필드
                Assert.That(field, Is.Not.Null, entry.Key); // 새 설정 필드 존재 확인
                Assert.That(Convert.ToSingle(field.GetValue(copy)), Is.EqualTo(entry.Value).Within(0.0001f), entry.Key); // 누락된 필드의 안전한 기본값
            }
            foreach (FieldInfo field in typeof(PrototypeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) // 모든 공개 설정 필드 순회
            {
                if (!defaults.ContainsKey(field.Name)) // 새 회피 설정 외의 필드
                {
                    Assert.That(field.GetValue(copy), Is.EqualTo(field.GetValue(source)), field.Name + " 기존 값 보존"); // 기존 전투 설정 유지
                }
            }
            Assert.That(File.ReadAllText(sourcePath), Is.EqualTo(originalText)); // 원본 사용자 설정 파일 불변
            Assert.That(AssetDatabase.AssetPathToGUID(sourcePath), Is.EqualTo(originalGuid)); // 원본 사용자 설정 식별자 불변
            CollectionAssert.AreEquivalent(sceneDependencies, AssetDatabase.GetDependencies(scenePath, false)); // 기존 씬 참조 보존
        }
        finally // 시험 사본 정리
        {
            AssetDatabase.DeleteAsset(copyPath); // 생성한 시험 사본만 제거
        }
    }
}
