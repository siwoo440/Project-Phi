using UnityEngine; // 스태미나와 회피 시간 계산

namespace Phi // 회피 실행 상태 영역
{
    public sealed class DodgeRuntime // 전투별 스태미나와 회피 상태
    {
        public float Stamina // 현재 스태미나
        {
            get; // 현재 스태미나 읽기
            private set; // 현재 스태미나 내부 설정
        }
        public float MaximumStamina // 전투 시작 최대 스태미나
        {
            get; // 최대 스태미나 읽기
        }
        public float RemainingCooldown // 남은 회피 재사용 시간
        {
            get; // 재사용 시간 읽기
            private set; // 재사용 시간 내부 설정
        }
        public bool IsDodging // 회피 이동 중 여부
        {
            get // 회피 이동 상태 읽기
            {
                return remainingDodge > 0f; // 남은 이동 시간 검사
            }
        }
        public bool IsInvulnerable // 회피 무적 중 여부
        {
            get // 회피 무적 상태 읽기
            {
                return remainingInvulnerability > 0f; // 남은 무적 시간 검사
            }
        }
        private readonly float regeneration; // 초당 스태미나 회복량
        private readonly float regenerationDelay; // 회피 종료 후 회복 대기시간
        private readonly float cost; // 회피 비용
        private readonly float cooldown; // 회피 재사용 시간
        private readonly float duration; // 회피 이동 시간
        private readonly float invulnerability; // 회피 무적 시간
        private float remainingDodge; // 남은 회피 이동 시간
        private float remainingInvulnerability; // 남은 회피 무적 시간
        private float remainingRegenerationDelay; // 남은 회복 대기시간

        public DodgeRuntime(PrototypeSettings settings) // 설정과 분리된 회피 상태 생성
        {
            MaximumStamina = Mathf.Max(0f, settings.playerStamina); // 음수 최대 스태미나 방지
            Stamina = MaximumStamina; // 전투 시작 스태미나
            regeneration = Mathf.Max(0f, settings.staminaRegen); // 음수 회복량 방지
            regenerationDelay = Mathf.Max(0f, settings.staminaRegenDelay); // 음수 회복 대기 방지
            cost = Mathf.Max(0f, settings.dodgeCost); // 회피 비용 복사
            cooldown = Mathf.Max(0f, settings.dodgeCooldown); // 회피 대기시간 복사
            duration = Mathf.Max(0f, settings.dodgeDuration); // 회피 시간 복사
            invulnerability = Mathf.Max(0f, settings.dodgeInvulnerability); // 무적 시간 복사
        }

        public bool TryDodge() // 회피 시작과 비용 적용
        {
            if (IsDodging || RemainingCooldown > 0f || Stamina < cost || duration <= 0f) // 회피 가능 상태 확인
            {
                return false; // 실패 시 자원 보존
            }
            Stamina -= cost; // 시작 순간 스태미나 차감
            RemainingCooldown = cooldown; // 시작 순간 재사용 시간 적용
            remainingDodge = duration; // 회피 이동 시작
            remainingInvulnerability = invulnerability; // 회피 무적 시작
            remainingRegenerationDelay = duration + regenerationDelay; // 이동 종료 후 회복 대기
            return true; // 회피 시작 성공
        }

        public float Tick(float elapsed) // 경과 시간 처리와 회피 이동 시간 반환
        {
            float seconds = Mathf.Max(0f, elapsed); // 음수 경과 시간 방지
            float movement = Mathf.Min(seconds, remainingDodge); // 이번 프레임의 회피 이동 시간
            float regenerationTime = Mathf.Max(0f, seconds - remainingRegenerationDelay); // 대기 종료 이후 경과 시간
            remainingDodge = Mathf.Max(0f, remainingDodge - seconds); // 남은 회피 이동 시간
            remainingInvulnerability = Mathf.Max(0f, remainingInvulnerability - seconds); // 남은 무적 시간
            RemainingCooldown = Mathf.Max(0f, RemainingCooldown - seconds); // 남은 재사용 시간
            remainingRegenerationDelay = Mathf.Max(0f, remainingRegenerationDelay - seconds); // 남은 회복 대기시간
            Stamina = Mathf.Min(MaximumStamina, Stamina + regeneration * regenerationTime); // 대기 이후 최대치 이내 회복
            return movement; // 실제 회피 이동 시간 반환
        }
    }
}
