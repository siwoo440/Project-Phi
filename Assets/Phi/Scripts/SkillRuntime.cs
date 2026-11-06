using UnityEngine; // 마나와 대기시간 계산

namespace Phi // 공용 스킬 실행 상태 영역
{
    public sealed class SkillRuntime // 전투별 마나와 슬롯 대기시간
    {
        public float Mana // 현재 마나
        {
            get; // 현재 마나 읽기
            private set; // 현재 마나 내부 설정
        }
        public float MaximumMana // 전투 시작 시 최대 마나
        {
            get; // 최대 마나 읽기
        }
        private readonly float regeneration; // 전투 시작 시 초당 회복량
        private readonly float[] cooldowns = new float[3]; // 슬롯별 남은 대기시간
        private readonly float fireballCost; // 화염탄 마나 비용
        private readonly float fireballCooldown; // 화염탄 대기시간
        private readonly float whirlwindCost; // 회오리 베기 마나 비용
        private readonly float whirlwindCooldown; // 회오리 베기 대기시간
        private readonly float healingCost; // 치유의 빛 마나 비용
        private readonly float healingCooldown; // 치유의 빛 대기시간

        public SkillRuntime(PrototypeSettings settings) // 설정과 분리된 전투 상태 생성
        {
            MaximumMana = Mathf.Max(0f, settings.playerMana); // 음수 최대 마나 방지
            Mana = MaximumMana; // 전투 시작 마나
            regeneration = Mathf.Max(0f, settings.manaRegen); // 음수 회복량 방지
            fireballCost = Mathf.Max(0f, settings.fireballCost); // 화염탄 비용 복사
            fireballCooldown = Mathf.Max(0f, settings.fireballCooldown); // 화염탄 대기시간 복사
            whirlwindCost = Mathf.Max(0f, settings.whirlwindCost); // 회오리 베기 비용 복사
            whirlwindCooldown = Mathf.Max(0f, settings.whirlwindCooldown); // 회오리 베기 대기시간 복사
            healingCost = Mathf.Max(0f, settings.healingCost); // 치유의 빛 비용 복사
            healingCooldown = Mathf.Max(0f, settings.healingCooldown); // 치유의 빛 대기시간 복사
        }

        public void Tick(float elapsed) // 게임 시간에 따른 자원 회복
        {
            float duration = Mathf.Max(0f, elapsed); // 음수 경과 시간 방지
            Mana = Mathf.Min(MaximumMana, Mana + regeneration * duration); // 최대치 이내 마나 회복
            for (int index = 0; index < cooldowns.Length; index++) // 각 슬롯의 대기시간 검사
            {
                cooldowns[index] = Mathf.Max(0f, cooldowns[index] - duration); // 음수 없는 남은 시간
            }
        }

        public float RemainingCooldown(int slot) // 지정 슬롯의 남은 대기시간
        {
            return slot >= 0 && slot < cooldowns.Length ? cooldowns[slot] : 0f; // 범위 밖 슬롯의 안전한 기본값
        }

        public bool TryUse(int slot, SkillId id, bool allowed = true) // 스킬 비용과 대기시간 적용
        {
            if (!allowed || slot < 0 || slot >= cooldowns.Length || cooldowns[slot] > 0f) // 시전 조건과 슬롯 검사
            {
                return false; // 실패 시 자원 보존
            }
            float cost; // 선택 스킬의 비용
            float cooldown; // 선택 스킬의 대기시간
            switch (id) // 구현된 스킬의 비용 선택
            {
                case SkillId.Fireball: // 화염탄 선택
                    cost = fireballCost; // 화염탄 비용
                    cooldown = fireballCooldown; // 화염탄 대기시간
                    break; // 비용 선택 완료
                case SkillId.WhirlwindSlash: // 회오리 베기 선택
                    cost = whirlwindCost; // 회오리 베기 비용
                    cooldown = whirlwindCooldown; // 회오리 베기 대기시간
                    break; // 비용 선택 완료
                case SkillId.HealingLight: // 치유의 빛 선택
                    cost = healingCost; // 치유의 빛 비용
                    cooldown = healingCooldown; // 치유의 빛 대기시간
                    break; // 비용 선택 완료
                default: // 미구현 또는 빈 스킬
                    return false; // 비용과 대기시간 보존
            }
            if (Mana < cost) // 마나 부족 여부
            {
                return false; // 마나 부족 시 시전 차단
            }
            Mana -= cost; // 시전 순간 마나 차감
            cooldowns[slot] = cooldown; // 해당 슬롯의 대기시간 시작
            return true; // 시전 성공
        }
    }
}
