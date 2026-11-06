using System; // 자료형 변환
using System.IO; // 시험 사본 파일 읽기와 쓰기
using System.Reflection; // 기존 설정 필드 확인
using System.Text.RegularExpressions; // 이전 형식 시험 사본 구성
using NUnit.Framework; // 테스트 도구
using Phi; // 프로젝트 설정 자료형
using UnityEditor; // 에셋 데이터베이스

public class InteractionSettingsMigrationTests // 기존 설정의 상호작용 필드 이관 검증
{
    [Test] // 상호작용 범위가 없는 이전 에셋 사례
    public void LegacyAssetReceivesRangeDefaultAndPreservesExistingSettings() // 새 기본값과 원본 보존 검증
    {
        const string sourcePath = "Assets/Phi/Data/PrototypeSettings.asset"; // 원본 사용자 설정 경로
        const string scenePath = "Assets/Phi/Scenes/CombatPrototype.unity"; // 원본 전투 씬 경로
        PrototypeSettings source = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(sourcePath); // 사용자 설정 에셋
        Assert.That(source, Is.Not.Null); // 원본 에셋 존재 확인
        string originalText = File.ReadAllText(sourcePath); // 원본 설정 파일 내용 보관
        string originalGuid = AssetDatabase.AssetPathToGUID(sourcePath); // 원본 설정 식별자 보관
        string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, false); // 원본 씬 참조 목록 보관
        string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PhiTests/TemporaryInteractionMigration.asset"); // 독립 시험 사본 경로
        try // 시험 사본 정리 범위
        {
            Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True); // 사용자 에셋의 독립 사본 생성
            string content = File.ReadAllText(copyPath); // 사본의 직렬화 내용
            content = Regex.Replace(content, @"(?m)^  interactionRange:[^\r\n]*(\r?\n|$)", ""); // 사본의 새 상호작용 범위 제거
            File.WriteAllText(copyPath, content); // 사본에만 이전 형식 적용
            AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); // 이전 설정 사본 다시 불러오기
            PrototypeSettings copy = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(copyPath); // 이전 형식의 Unity 로드 결과
            Assert.That(copy, Is.Not.Null); // 시험 사본 로드 확인
            FieldInfo range = typeof(PrototypeSettings).GetField("interactionRange"); // 새 상호작용 범위 설정
            Assert.That(range, Is.Not.Null, "상호작용 범위 설정이 필요합니다."); // 새 설정 필드 존재 확인
            Assert.That(Convert.ToSingle(range.GetValue(copy)), Is.EqualTo(1.5f).Within(0.0001f)); // 이전 에셋의 임시 기본 반경
            foreach (FieldInfo field in typeof(PrototypeSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) // 모든 공개 설정 필드 순회
            {
                if (field.Name != "interactionRange") // 새 상호작용 범위 외 필드
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
