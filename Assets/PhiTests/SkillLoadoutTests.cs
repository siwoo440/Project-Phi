using System; // 자료형과 열거형 변환
using System.Collections; // 공용 목록 순회
using System.Collections.Generic; // 스킬 정의 목록
using System.Linq; // 목록 변환과 비교
using System.Reflection; // 미구현 API 시험 접근
using NUnit.Framework; // 테스트 도구
using Phi; // 프로젝트 설정 자료형

public class SkillLoadoutTests // 공용 스킬과 슬롯 검증
{
    [Test] // 공용 후보 목록 사례
    public void CatalogContainsExactlyTwelveNamedAndClassifiedSkills() // 후보 이름과 판정 유형 검증
    {
        Type catalogType = RequireType("Phi.SkillCatalog"); // 공용 스킬 목록 자료형
        PropertyInfo allProperty = catalogType.GetProperty("All", BindingFlags.Public | BindingFlags.Static); // 공용 후보 목록 속성
        Assert.That(allProperty, Is.Not.Null, "공용 스킬 후보 목록이 필요합니다."); // 후보 목록 존재 확인
        IEnumerable values = (IEnumerable)allProperty.GetValue(null); // 공용 스킬 후보
        List<object> entries = values.Cast<object>().ToList(); // 후보 목록의 독립 복사
        string[] ids = { "Fireball", "IceShard", "ShieldBash", "PiercingShot", "Teleport", "ChainLightning", "EntanglingVines", "ArcaneBlast", "WhirlwindSlash", "HealingLight", "Shield", "Sprint" }; // 기획의 열두 스킬 식별자
        string[] names = { "화염탄", "얼음 파편", "방패 치기", "관통 사격", "순간 이동", "번개 사슬", "속박 덩굴", "마력 폭발", "회오리 베기", "치유의 빛", "수호막", "질주" }; // 스킬별 기획 이름
        string[] aims = { "Directional", "Directional", "Directional", "Directional", "Directional", "Targeted", "Targeted", "Targeted", "SelfCentered", "SelfCentered", "SelfCentered", "SelfCentered" }; // 스킬별 조준 유형
        Assert.That(entries.Count, Is.EqualTo(12)); // 정확한 공용 후보 수
        for (int index = 0; index < ids.Length; index++) // 기획의 각 스킬 확인
        {
            object entry = entries.Single(item => Read(item, "Id").ToString() == ids[index]); // 식별자에 맞는 단일 정의
            Assert.That(Read(entry, "Name"), Is.EqualTo(names[index])); // 기획의 한글 이름
            Assert.That(Read(entry, "AimType").ToString(), Is.EqualTo(aims[index])); // 기획의 조준 유형
        }
    }

    [Test] // 유효한 사용자 지정 조합 사례
    public void DistinctCandidatesRemainInTheirAssignedSlots() // 유효한 Q/E/R 복사 검증
    {
        object loadout = Create("IceShard", "Shield", "Teleport"); // 서로 다른 사용자 지정 조합
        AssertSlot(loadout, "Q", "IceShard", "None"); // Q의 지정값과 정상 상태
        AssertSlot(loadout, "E", "Shield", "None"); // E의 지정값과 정상 상태
        AssertSlot(loadout, "R", "Teleport", "None"); // R의 지정값과 정상 상태
    }

    [Test] // 빈 슬롯과 중복 사례
    public void EmptyAndDuplicateSlotsAreDisabledIndividually() // 빈 슬롯과 중복의 구분 검증
    {
        object loadout = Create("None", "IceShard", "IceShard"); // 빈 Q와 중복 R을 포함한 조합
        AssertSlot(loadout, "Q", "None", "Empty"); // 빈 Q의 개별 비활성화
        AssertSlot(loadout, "E", "IceShard", "None"); // 유효한 앞선 E 유지
        AssertSlot(loadout, "R", "None", "Duplicate"); // 뒤쪽 중복 R만 비활성화
    }

    [Test] // 목록 밖 식별자 사례
    public void UnknownValueIsDisabledWithoutDiscardingOtherSlots() // 유효하지 않은 값의 개별 비활성화
    {
        Type idType = RequireType("Phi.SkillId"); // 스킬 식별자 자료형
        object invalid = Enum.ToObject(idType, 999); // 목록 밖의 잘못된 직렬화 값
        object loadout = CreateValues(Enum.Parse(idType, "Fireball"), invalid, Enum.Parse(idType, "HealingLight")); // 잘못된 E를 포함한 조합
        AssertSlot(loadout, "Q", "Fireball", "None"); // 정상 Q 보존
        AssertSlot(loadout, "E", "None", "Unknown"); // 목록 밖 E의 비활성화
        AssertSlot(loadout, "R", "HealingLight", "None"); // 정상 R 보존
    }

    [Test] // 반복되는 세 슬롯 사례
    public void DuplicatePriorityIsQThenEThenR() // 앞선 슬롯 우선 정책 검증
    {
        object loadout = Create("Fireball", "Fireball", "Fireball"); // 세 슬롯의 같은 스킬
        AssertSlot(loadout, "Q", "Fireball", "None"); // 가장 앞선 Q 유지
        AssertSlot(loadout, "E", "None", "Duplicate"); // 중복 E 제외
        AssertSlot(loadout, "R", "None", "Duplicate"); // 중복 R 제외
    }

    private static object Create(string q, string e, string r) // 이름으로 시험 슬롯 생성
    {
        Type idType = RequireType("Phi.SkillId"); // 스킬 식별자 자료형
        return CreateValues(Enum.Parse(idType, q), Enum.Parse(idType, e), Enum.Parse(idType, r)); // 지정된 세 슬롯 생성
    }

    private static object CreateValues(object q, object e, object r) // 식별자로 시험 슬롯 생성
    {
        Type type = RequireType("Phi.SkillLoadout"); // 실행 장착 상태 자료형
        MethodInfo create = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static); // 장착 상태 생성 메서드
        Assert.That(create, Is.Not.Null, "슬롯 검증과 복사 기능이 필요합니다."); // 생성 메서드 존재 확인
        return create.Invoke(null, new[] { q, e, r }); // 유효성 검사된 독립 장착 상태
    }

    private static void AssertSlot(object loadout, string slot, string id, string error) // 슬롯 값과 오류 종류 검증
    {
        Assert.That(Read(loadout, slot).ToString(), Is.EqualTo(id)); // 유효하거나 비활성화된 슬롯 값
        Assert.That(Read(loadout, slot + "Error").ToString(), Is.EqualTo(error)); // 슬롯별 오류 분류
    }

    private static object Read(object value, string name) // 공개 읽기 전용 속성 확인
    {
        PropertyInfo property = value.GetType().GetProperty(name); // 이름에 맞는 공개 속성
        Assert.That(property, Is.Not.Null, name + " 속성이 필요합니다."); // 속성 존재 확인
        return property.GetValue(value); // 공개 속성 값
    }

    private static Type RequireType(string name) // 프로젝트 전투 자료형 확인
    {
        Type type = typeof(PrototypeSettings).Assembly.GetType(name); // 전투 어셈블리의 자료형 검색
        Assert.That(type, Is.Not.Null, name + " 자료형이 필요합니다."); // 자료형 존재 확인
        return type; // 확인된 자료형
    }
}
