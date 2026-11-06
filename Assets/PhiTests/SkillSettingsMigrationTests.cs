using System; // 자료형 검색
using System.Collections.Generic; // 신규 설정 필드 집합
using System.IO; // 시험용 설정 파일 읽기와 쓰기
using System.Reflection; // 설정 필드 비교
using System.Text.RegularExpressions; // 새 필드 없는 이전 형식 구성
using NUnit.Framework; // 테스트 도구
using Phi; // 프로젝트 설정 자료형
using UnityEditor; // 에셋 데이터베이스
using UnityEngine; // 유니티 자료형

public class SkillSettingsMigrationTests // 기존 설정 에셋 보존 검증
{
    [Test] // 이전 설정 에셋 로드 사례
    public void LegacyAssetReceivesRecommendedDefaultsWithoutChangingOriginal() // 기존 에셋의 안전한 기본값 검증
    {
        const string sourcePath = "Assets/Phi/Data/PrototypeSettings.asset"; // 기존 사용자 설정 에셋 경로
        const string scenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 기존 전투 씬 경로
        PrototypeSettings source = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(sourcePath); // 기존 설정 에셋 읽기
        Assert.That(source, Is.Not.Null); // 기존 설정 에셋 존재 확인
        string originalText = File.ReadAllText(sourcePath); // 원본 설정 파일 내용 보관
        string originalGuid = AssetDatabase.AssetPathToGUID(sourcePath); // 원본 에셋 식별자 보관
        string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, false); // 기존 씬 참조 목록 보관
        string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PhiTests/TemporarySkillMigration.asset"); // 충돌 없는 시험 사본 경로
        try // 시험 사본 정리 범위
        {
            Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True); // 사용자 에셋의 시험 사본 생성
            string content = File.ReadAllText(copyPath); // 사본의 직렬화 내용
            string legacy = Regex.Replace(content, @"(?m)^  startingSkill[QER]:[^\r\n]*(\r?\n|$)", ""); // 새 스킬 필드 없는 이전 형식
            File.WriteAllText(copyPath, legacy); // 시험 사본에만 이전 형식 적용
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); // 시험 사본 다시 불러오기
            PrototypeSettings copy = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(copyPath); // 이전 형식의 Unity 로드 결과
            Assert.That(copy, Is.Not.Null); // 시험 사본 로드 확인
            Assert.That(Read(copy, "startingSkillQ").ToString(), Is.EqualTo("Fireball")); // 추천 Q 기본값
            Assert.That(Read(copy, "startingSkillE").ToString(), Is.EqualTo("WhirlwindSlash")); // 추천 E 기본값
            Assert.That(Read(copy, "startingSkillR").ToString(), Is.EqualTo("HealingLight")); // 추천 R 기본값
            foreach (FieldInfo field in typeof(PrototypeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) // 기존 공개 설정값 순회
            {
                if (!field.Name.StartsWith("startingSkill", StringComparison.Ordinal)) // 새 스킬 필드 제외
                {
                    Assert.That(field.GetValue(copy), Is.EqualTo(field.GetValue(source)), field.Name + " 기존 설정값 보존"); // 기존 값의 손실 방지
                }
            }
            Assert.That(File.ReadAllText(sourcePath), Is.EqualTo(originalText)); // 사용자 설정 파일 불변
            Assert.That(AssetDatabase.AssetPathToGUID(sourcePath), Is.EqualTo(originalGuid)); // 사용자 설정 식별자 불변
            CollectionAssert.AreEquivalent(sceneDependencies, AssetDatabase.GetDependencies(scenePath, false)); // 전투 씬의 기존 참조 보존
        }
        finally // 시험 사본 정리
        {
            AssetDatabase.DeleteAsset(copyPath); // 생성한 시험 사본만 제거
        }
    }

    [Test] // 마나와 첫 스킬 설정의 이전 형식 사례
    public void LegacyAssetReceivesSkillNumbersWithoutChangingExistingFields() // 새 수치 기본값과 원본 보존 검증
    {
        const string sourcePath = "Assets/Phi/Data/PrototypeSettings.asset"; // 사용자 설정 경로
        PrototypeSettings source = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(sourcePath); // 사용자 설정 에셋
        Assert.That(source, Is.Not.Null); // 원본 에셋 존재 확인
        string originalText = File.ReadAllText(sourcePath); // 원본 설정 파일 보관
        string originalGuid = AssetDatabase.AssetPathToGUID(sourcePath); // 원본 식별자 보관
        string[] sceneDependencies = AssetDatabase.GetDependencies("Assets/Phi/Scenes/CombatPrototype.unity", false); // 원본 씬 참조 목록 보관
        string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PhiTests/TemporarySkillNumbers.asset"); // 시험 사본 경로
        Dictionary<string, float> defaults = new Dictionary<string, float> // 기획과 임시 선택의 기본 수치
        {
            { "playerMana", 50f }, // 기본 최대 마나
            { "manaRegen", 3f }, // 초당 마나 회복량
            { "fireballCost", 10f }, // 화염탄 비용
            { "fireballCooldown", 5f }, // 화염탄 대기시간
            { "fireballMultiplier", 1.4f }, // 화염탄 피해 배율
            { "fireballRange", 7f }, // 화염탄 임시 사거리
            { "fireballSpeed", 14f }, // 화염탄 임시 이동속도
            { "whirlwindCost", 14f }, // 회오리 비용
            { "whirlwindCooldown", 9f }, // 회오리 대기시간
            { "whirlwindMultiplier", 1.3f }, // 회오리 피해 배율
            { "whirlwindRadius", 2f }, // 회오리 임시 반경
            { "healingCost", 18f }, // 치유 비용
            { "healingCooldown", 20f }, // 치유 대기시간
            { "healingFraction", 0.15f } // 최대 체력 기준 치유 비율
        };
        try // 시험 사본 정리 범위
        {
            Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True); // 원본의 독립 사본 생성
            string content = File.ReadAllText(copyPath); // 복제된 설정 파일
            foreach (string name in defaults.Keys) // 새 수치 필드 순회
            {
                content = Regex.Replace(content, @"(?m)^  " + Regex.Escape(name) + @":[^\r\n]*(\r?\n|$)", ""); // 시험 사본의 새 필드 제거
            }
            File.WriteAllText(copyPath, content); // 시험 사본에만 이전 형식 적용
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); // 이전 형식 다시 불러오기
            PrototypeSettings copy = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(copyPath); // 이전 형식 로드 결과
            Assert.That(copy, Is.Not.Null); // 시험 사본 존재 확인
            foreach (KeyValuePair<string, float> entry in defaults) // 새 필드의 기본값 확인
            {
                Assert.That(Convert.ToSingle(Read(copy, entry.Key)), Is.EqualTo(entry.Value).Within(0.0001f), entry.Key); // 기획 또는 임시 시험 기본값
            }
            foreach (FieldInfo field in typeof(PrototypeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) // 공개 설정 필드 순회
            {
                if (!defaults.ContainsKey(field.Name)) // 이번에 추가한 수치 외 필드
                {
                    Assert.That(field.GetValue(copy), Is.EqualTo(field.GetValue(source)), field.Name + " 기존 값 보존"); // 이전 설정값 유지
                }
            }
            Assert.That(File.ReadAllText(sourcePath), Is.EqualTo(originalText)); // 사용자 설정 파일 불변
            Assert.That(AssetDatabase.AssetPathToGUID(sourcePath), Is.EqualTo(originalGuid)); // 사용자 설정 식별자 불변
            CollectionAssert.AreEquivalent(sceneDependencies, AssetDatabase.GetDependencies("Assets/Phi/Scenes/CombatPrototype.unity", false)); // 씬의 원본 참조 보존
        }
        finally // 시험 사본 정리
        {
            AssetDatabase.DeleteAsset(copyPath); // 생성한 시험 사본만 제거
        }
    }

    private static object Read(PrototypeSettings settings, string name) // 설정의 스킬 필드 읽기
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 이름에 맞는 공개 설정 필드
        Assert.That(field, Is.Not.Null, name + " 설정이 필요합니다."); // 새 필드 존재 확인
        return field.GetValue(settings); // 스킬 설정값
    }
}
