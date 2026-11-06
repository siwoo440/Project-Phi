using System.Collections.Generic; // 사용한 스킬 식별자 집합

namespace Phi // 공용 스킬 장착 영역
{
    public enum SkillSlotError // 시험 슬롯 검증 결과
    {
        None, // 유효한 슬롯
        Empty, // 빈 슬롯
        Unknown, // 목록 밖 식별자
        Duplicate // 앞 슬롯과 중복된 스킬
    }

    public sealed class SkillLoadout // 전투 시작 시 고정되는 슬롯 상태
    {
        public SkillId Q { get; } // Q 슬롯의 유효한 스킬
        public SkillId E { get; } // E 슬롯의 유효한 스킬
        public SkillId R { get; } // R 슬롯의 유효한 스킬
        public SkillSlotError QError { get; } // Q 슬롯 오류
        public SkillSlotError EError { get; } // E 슬롯 오류
        public SkillSlotError RError { get; } // R 슬롯 오류

        private SkillLoadout(SkillId q, SkillId e, SkillId r, SkillSlotError qError, SkillSlotError eError, SkillSlotError rError) // 독립된 장착 상태 생성
        {
            Q = q; // Q 슬롯 복사
            E = e; // E 슬롯 복사
            R = r; // R 슬롯 복사
            QError = qError; // Q 검증 결과 복사
            EError = eError; // E 검증 결과 복사
            RError = rError; // R 검증 결과 복사
        }

        public static SkillLoadout Create(SkillId q, SkillId e, SkillId r) // 슬롯 순서대로 유효성 검사
        {
            HashSet<SkillId> used = new HashSet<SkillId>(); // 유효한 앞 슬롯의 스킬 집합
            SkillId qValue = Validate(q, used, out SkillSlotError qError); // Q 슬롯 검사
            SkillId eValue = Validate(e, used, out SkillSlotError eError); // E 슬롯 검사
            SkillId rValue = Validate(r, used, out SkillSlotError rError); // R 슬롯 검사
            return new SkillLoadout(qValue, eValue, rValue, qError, eError, rError); // 설정과 분리된 결과
        }

        private static SkillId Validate(SkillId id, HashSet<SkillId> used, out SkillSlotError error) // 단일 슬롯 검사
        {
            if (id == SkillId.None) // 빈 시험 슬롯 확인
            {
                error = SkillSlotError.Empty; // 빈 슬롯 오류
                return SkillId.None; // 해당 슬롯 비활성화
            }
            if (!SkillCatalog.TryGet(id, out _)) // 정의되지 않은 식별자 확인
            {
                error = SkillSlotError.Unknown; // 목록 밖 오류
                return SkillId.None; // 해당 슬롯 비활성화
            }
            if (!used.Add(id)) // 앞 슬롯과의 중복 확인
            {
                error = SkillSlotError.Duplicate; // 중복 슬롯 오류
                return SkillId.None; // 해당 슬롯 비활성화
            }
            error = SkillSlotError.None; // 유효한 슬롯 결과
            return id; // 유효한 스킬 유지
        }
    }
}
