using System; // 상호작용 알림
using System.Collections.Generic; // 활성 대상 목록
using UnityEngine; // 유니티 오브젝트

namespace Phi // 상호작용 대상 영역
{
    public enum InteractionKind // 시험 상호작용 대상 유형
    {
        Npc, // 시험 NPC 대상
        Door, // 시험 문 대상
        Chest, // 시험 상자 대상
        Loot // 시험 전리품 대상
    }

    public sealed class Interactable : MonoBehaviour // 재사용 가능한 상호작용 대상
    {
        public const string SlimeGelId = "test.slime_gel"; // 임시 슬라임 점액 식별자
        private static readonly List<Interactable> active = new List<Interactable>(); // 활성화 순서의 대상 목록
        [SerializeField] private InteractionKind kind; // 시험 대상 유형
        [SerializeField] private string displayName = "TARGET"; // 시험 표시 이름
        [SerializeField] private string itemId = ""; // 표시명과 구분된 임시 아이템 식별자
        private bool consumed; // 동일 전리품의 중복 수집 방지
        public event Action<Interactable> Interacted; // 다른 기능에서 연결할 대상 작동 알림

        public InteractionKind Kind // 상호작용 대상 유형
        {
            get // 대상 유형 읽기
            {
                return kind; // 현재 시험 대상 유형
            }
        }
        public string DisplayName // 상호작용 표시 이름
        {
            get // 표시 이름 읽기
            {
                return displayName; // 현재 표시 이름
            }
        }
        public string ItemId // 임시 아이템 식별자
        {
            get // 아이템 식별자 읽기
            {
                return itemId; // 현재 아이템 식별자
            }
        }
        public bool IsSelected // 현재 대상 선택 여부
        {
            get; // 대상 선택 상태 읽기
            private set; // 대상 선택 상태 내부 설정
        }
        public bool IsAvailable // 선택과 작동 가능 여부
        {
            get // 현재 대상 사용 가능 상태 읽기
            {
                return isActiveAndEnabled && !consumed; // 활성 상태와 중복 수집 여부
            }
        }
        internal static IReadOnlyList<Interactable> Active // 활성 대상의 등록 순서
        {
            get // 활성 대상 목록 읽기
            {
                return active; // 내부 대상 순서 유지
            }
        }

        public void Configure(InteractionKind targetKind, string name) // 시험 대상의 유형과 이름 설정
        {
            if (consumed) // 이미 수집된 전리품 여부
            {
                return; // 수집된 인스턴스 재사용 방지
            }
            kind = targetKind; // 시험 대상 유형 저장
            displayName = string.IsNullOrEmpty(name) ? "TARGET" : name; // 빈 이름의 안전한 표시
            itemId = kind == InteractionKind.Loot ? SlimeGelId : ""; // 시험 전리품의 별도 식별자
        }

        internal void SetSelected(bool value) // 대상 선택 표시 갱신
        {
            IsSelected = value && IsAvailable; // 비활성 대상 선택 방지
        }

        public bool TryInteract() // 대상 작동과 중복 수집 방지
        {
            if (!IsAvailable) // 비활성 또는 수집 완료 여부
            {
                return false; // 동일 대상 재작동 차단
            }
            if (kind == InteractionKind.Loot) // 일회성 전리품 여부
            {
                consumed = true; // 알림 이전 수집 완료 표시
                SetSelected(false); // 즉시 선택 해제
                gameObject.SetActive(false); // 지연 제거 전 재선택 차단
            }
            Interacted?.Invoke(this); // 공통 대상 작동 알림
            if (kind == InteractionKind.Loot) // 수집된 시험 전리품 여부
            {
                Destroy(gameObject); // 이번 전투의 전리품 표시 제거
            }
            return true; // 하나의 대상 작동 완료
        }

        private void OnEnable() // 활성 대상 등록
        {
            if (!active.Contains(this)) // 중복 등록 여부
            {
                active.Add(this); // 활성화 순서대로 등록
            }
        }

        private void OnDisable() // 비활성 대상 등록 해제
        {
            IsSelected = false; // 대상 표시 초기화
            active.Remove(this); // 비활성 대상 제거
        }
    }
}
