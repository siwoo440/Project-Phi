using System; // 설정 자료형 변환
using System.IO; // 시험 사본 파일 접근
using System.Reflection; // 기존 설정 필드 확인
using System.Text.RegularExpressions; // 이전 설정 형식 구성
using NUnit.Framework; // 시험 도구
using Phi; // 전투 설정
using UnityEditor; // 에셋 데이터베이스

public class EnemyRosterSettingsMigrationTests // 기존 설정의 새 적 이관 검증
{
    [Test] // 새 적 필드가 없는 이전 설정 사례
    public void LegacySettingsPreserveExistingValuesAndDisableNewEnemies() // 기본값과 원본 보존 검증
    {
        const string sourcePath = "Assets/Phi/Data/PrototypeSettings.asset"; // 사용자 설정 에셋
        const string scenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 저장된 전투 씬
        PrototypeSettings source = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(sourcePath); // 기존 사용자 설정
        Assert.That(source, Is.Not.Null); // 원본 설정 존재 확인
        string originalText = File.ReadAllText(sourcePath); // 원본 파일 내용 보관
        string originalGuid = AssetDatabase.AssetPathToGUID(sourcePath); // 원본 설정 식별자 보관
        string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, false); // 씬 참조 목록 보관
        string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PhiTests/TemporaryRosterMigration.asset"); // 독립 시험 사본 경로
        try // 시험 사본 정리 보장
        {
            Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True); // 사용자 설정 복제
            string content = File.ReadAllText(copyPath); // 시험 사본 직렬화 내용
            content = Regex.Replace(content, @"(?m)^  (skeleton|orc|wolf)[^:]*:[^\r\n]*(\r?\n|$)", ""); // 세 적의 새 필드만 제거
            File.WriteAllText(copyPath, content); // 사본에 이전 형식 적용
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); // 이전 설정 다시 불러오기
            PrototypeSettings copy = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(copyPath); // 이관된 설정 사본
            Assert.That(copy, Is.Not.Null); // 사본 로드 확인
            Assert.That(Read(copy, "skeletonCount"), Is.EqualTo(0f)); // 기존 전투의 궁수 자동 추가 방지
            Assert.That(Read(copy, "orcCount"), Is.EqualTo(0f)); // 기존 전투의 오크 자동 추가 방지
            Assert.That(Read(copy, "wolfCount"), Is.EqualTo(0f)); // 기존 전투의 늑대 자동 추가 방지
            Assert.That(Read(copy, "skeletonHealth"), Is.EqualTo(35f)); // 기획 궁수 체력
            Assert.That(Read(copy, "skeletonAttack"), Is.EqualTo(9f)); // 기획 궁수 공격력
            Assert.That(Read(copy, "skeletonDefense"), Is.EqualTo(1f)); // 기획 궁수 방어력
            Assert.That(Read(copy, "orcHealth"), Is.EqualTo(100f)); // 기획 오크 체력
            Assert.That(Read(copy, "orcAttack"), Is.EqualTo(15f)); // 기획 오크 공격력
            Assert.That(Read(copy, "orcDefense"), Is.EqualTo(5f)); // 기획 오크 방어력
            Assert.That(Read(copy, "wolfHealth"), Is.EqualTo(45f)); // 기획 늑대 체력
            Assert.That(Read(copy, "wolfAttack"), Is.EqualTo(9f)); // 기획 늑대 공격력
            Assert.That(Read(copy, "wolfDefense"), Is.EqualTo(1f)); // 기획 늑대 방어력
            foreach (FieldInfo field in typeof(PrototypeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) // 모든 공개 설정 확인
            {
                if (!field.Name.StartsWith("skeleton", StringComparison.Ordinal) && !field.Name.StartsWith("orc", StringComparison.Ordinal) && !field.Name.StartsWith("wolf", StringComparison.Ordinal)) // 기존 전투 필드 여부
                {
                    Assert.That(field.GetValue(copy), Is.EqualTo(field.GetValue(source)), field.Name); // 기존 수치와 선택 보존
                }
            }
            Assert.That(File.ReadAllText(sourcePath), Is.EqualTo(originalText)); // 원본 설정 파일 불변
            Assert.That(AssetDatabase.AssetPathToGUID(sourcePath), Is.EqualTo(originalGuid)); // 원본 설정 식별자 불변
            CollectionAssert.AreEquivalent(sceneDependencies, AssetDatabase.GetDependencies(scenePath, false)); // 씬 참조 불변
        }
        finally // 성공과 실패 시 사본 정리
        {
            AssetDatabase.DeleteAsset(copyPath); // 생성한 시험 사본만 제거
        }
    }

    private static float Read(PrototypeSettings settings, string name) // 설정의 신규 수치 조회
    {
        FieldInfo field = typeof(PrototypeSettings).GetField(name); // 신규 설정 필드
        Assert.That(field, Is.Not.Null, name); // 필드 존재 확인
        return Convert.ToSingle(field.GetValue(settings)); // 시험 수치 변환
    }
}
