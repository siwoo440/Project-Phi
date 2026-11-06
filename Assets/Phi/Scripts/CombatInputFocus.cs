using System.Collections.Generic; // 화면 포커스 집합
using UnityEngine; // 유니티 구성 요소

namespace Phi // 전투 입력 제어 영역
{
    public sealed class CombatInputFocus : MonoBehaviour // 활성 화면의 전투 입력 점유
    {
        private static readonly HashSet<CombatInputFocus> active = new HashSet<CombatInputFocus>(); // 현재 화면 포커스 집합

        public static bool IsCaptured // 전투 입력 점유 상태
        {
            get // 활성 화면 확인
            {
                return active.Count > 0; // 화면의 전투 입력 점유 여부
            }
        }

        private void OnEnable() // 화면 활성화
        {
            active.Add(this); // 전투 입력 점유 시작
        }

        private void OnDisable() // 화면 비활성화 또는 파괴
        {
            active.Remove(this); // 해당 화면의 점유 해제
        }
    }
}
