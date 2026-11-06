using System.Collections; // 프레임 단위 시험
using NUnit.Framework; // 시험 도구
using Phi; // 전투 상태
using UnityEngine; // 유니티 자료형
using UnityEngine.SceneManagement; // 전투 씬 로드
using UnityEngine.TestTools; // 플레이 모드 시험
using UnityEngine.UI; // 무기 아이콘 표시

public class DocumentArtPlayModeTests // 기획서 이미지의 게임 연결 검증
{
    private SceneMapTestFixture map; // 독립 시험 맵 설정
    private SwordSceneSettingsFixture settings; // 독립 시험 전투 설정

    [UnitySetUp] // 각 시험의 전투 준비
    public IEnumerator SetUp() // 이미지 시험 전투 생성
    {
        map = new SceneMapTestFixture(); // 사용자 맵의 독립 시험 배치
        settings = new SwordSceneSettingsFixture(3); // 슬라임과 검 시험 설정
        SceneManager.LoadScene("CombatPrototype"); // 실제 전투 씬 로드
        yield return null; // 전투와 화면 생성 대기
        yield return null; // 화면 초기화 대기
    }

    [UnityTearDown] // 각 시험의 자원 정리
    public IEnumerator TearDown() // 시험 설정 복원
    {
        settings.Dispose(); // 복제 설정과 이벤트 정리
        map.Dispose(); // 시험 맵과 이벤트 정리
        yield return null; // 프레임 정리 대기
    }

    [UnityTest] // 기획서 무기 이미지 연결 사례
    public IEnumerator WeaponButtonsShowMatchingDocumentIcons() // 세 무기 버튼의 그림 확인
    {
        string[] weapons = { "Sword", "Bow", "Staff" }; // 시험 무기 이름
        string[] files = { "sword", "bow", "staff" }; // 기획서 그림 파일 이름
        for (int index = 0; index < weapons.Length; index++) // 세 무기 순회
        {
            Sprite expected = Resources.Load<Sprite>("PhiArt/" + files[index]); // 기획서에서 가져온 스프라이트
            Assert.That(expected, Is.Not.Null, weapons[index]); // 그림 에셋 로드 확인
            GameObject button = GameObject.Find("Equip " + weapons[index]); // 실제 무기 선택 버튼
            Assert.That(button, Is.Not.Null, weapons[index]); // 기존 버튼 유지 확인
            Transform icon = button.transform.Find("Weapon Icon"); // 버튼 내부 그림
            Assert.That(icon, Is.Not.Null, weapons[index]); // 해당 버튼의 그림 확인
            Image image = icon.GetComponent<Image>(); // 무기 아이콘 표시기
            Assert.That(image.sprite, Is.SameAs(expected), weapons[index]); // 각 무기와 원본 그림 연결
            Assert.That(image.raycastTarget, Is.False, weapons[index]); // 아이콘의 버튼 입력 간섭 방지
            Assert.That(button.GetComponent<Button>().interactable, Is.True); // 버튼 선택 가능 상태
        }
        yield return null; // 이미지 적용 프레임 확인
    }

    [UnityTest] // 기획서 점액 그림 연결 사례
    public IEnumerator SlimeLootShowsDocumentImage() // 실제 슬라임 전리품 그림 확인
    {
        Sprite expected = Resources.Load<Sprite>("PhiArt/slime_gel"); // 기획서 점액 그림
        Assert.That(expected, Is.Not.Null); // 점액 그림 에셋 로드 확인
        Fighter slime = GameObject.Find("Slime 1").GetComponent<Fighter>(); // 실제 슬라임 전투 상태
        slime.TakeDamage(10000); // 기존 사망 전리품 생성
        yield return null; // 전리품 생성 대기
        GameObject loot = GameObject.Find("Slime Loot"); // 실제 생성 전리품
        Assert.That(loot, Is.Not.Null); // 기존 전리품 생성 확인
        Assert.That(loot.GetComponent<SpriteRenderer>().sprite, Is.SameAs(expected)); // 기획서 점액 그림 연결
        Assert.That(loot.GetComponent<Interactable>(), Is.Not.Null); // 기존 상호작용 유지
    }
}
