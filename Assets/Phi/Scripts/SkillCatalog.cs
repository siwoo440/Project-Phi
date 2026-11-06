using System; // 읽기 전용 목록 생성
using System.Collections.Generic; // 읽기 전용 스킬 목록
using UnityEngine; // 인스펙터 스킬 이름

namespace Phi // 공용 스킬 정의 영역
{
    public enum SkillId // 공용 스킬의 안정적인 식별자
    {
        [InspectorName("비어 있음")] None = 0, // 빈 시험 슬롯
        [InspectorName("화염탄")] Fireball = 1, // 방향형 화염탄
        [InspectorName("얼음 파편")] IceShard = 2, // 방향형 얼음 파편
        [InspectorName("방패 치기")] ShieldBash = 3, // 방향형 방패 치기
        [InspectorName("관통 사격")] PiercingShot = 4, // 방향형 관통 사격
        [InspectorName("순간 이동")] Teleport = 5, // 방향형 순간 이동
        [InspectorName("번개 사슬")] ChainLightning = 6, // 지정형 번개 사슬
        [InspectorName("속박 덩굴")] EntanglingVines = 7, // 지정형 속박 덩굴
        [InspectorName("마력 폭발")] ArcaneBlast = 8, // 지정형 마력 폭발
        [InspectorName("회오리 베기")] WhirlwindSlash = 9, // 자기 중심형 회오리 베기
        [InspectorName("치유의 빛")] HealingLight = 10, // 자기 중심형 치유의 빛
        [InspectorName("수호막")] Shield = 11, // 자기 중심형 수호막
        [InspectorName("질주")] Sprint = 12 // 자기 중심형 질주
    }

    public enum SkillAimType // 스킬의 입력 판정 유형
    {
        Directional, // 방향형 스킬
        Targeted, // 지정형 스킬
        SelfCentered // 자기 중심형 스킬
    }

    public sealed class SkillDefinition // 변경할 수 없는 공용 스킬 후보
    {
        public SkillId Id { get; } // 스킬 식별자
        public string Name { get; } // 기획의 한글 이름
        public string EnglishName { get; } // HUD의 임시 영문 이름
        public SkillAimType AimType { get; } // 기획의 입력 판정 유형

        internal SkillDefinition(SkillId id, string name, string englishName, SkillAimType aimType) // 공용 후보 정의 생성
        {
            Id = id; // 스킬 식별자 보관
            Name = name; // 기획 이름 보관
            EnglishName = englishName; // HUD 표시 이름 보관
            AimType = aimType; // 입력 판정 유형 보관
        }
    }

    public static class SkillCatalog // 해금과 무관한 공용 시험 후보 목록
    {
        public static IReadOnlyList<SkillDefinition> All { get; } = Array.AsReadOnly(new[] // 변경할 수 없는 열두 스킬 후보
        {
            new SkillDefinition(SkillId.Fireball, "화염탄", "FIREBALL", SkillAimType.Directional), // 방향형 화염탄
            new SkillDefinition(SkillId.IceShard, "얼음 파편", "ICE SHARD", SkillAimType.Directional), // 방향형 얼음 파편
            new SkillDefinition(SkillId.ShieldBash, "방패 치기", "SHIELD BASH", SkillAimType.Directional), // 방향형 방패 치기
            new SkillDefinition(SkillId.PiercingShot, "관통 사격", "PIERCING SHOT", SkillAimType.Directional), // 방향형 관통 사격
            new SkillDefinition(SkillId.Teleport, "순간 이동", "TELEPORT", SkillAimType.Directional), // 방향형 순간 이동
            new SkillDefinition(SkillId.ChainLightning, "번개 사슬", "CHAIN LIGHTNING", SkillAimType.Targeted), // 지정형 번개 사슬
            new SkillDefinition(SkillId.EntanglingVines, "속박 덩굴", "ENTANGLING VINES", SkillAimType.Targeted), // 지정형 속박 덩굴
            new SkillDefinition(SkillId.ArcaneBlast, "마력 폭발", "ARCANE BLAST", SkillAimType.Targeted), // 지정형 마력 폭발
            new SkillDefinition(SkillId.WhirlwindSlash, "회오리 베기", "WHIRLWIND SLASH", SkillAimType.SelfCentered), // 자기 중심형 회오리 베기
            new SkillDefinition(SkillId.HealingLight, "치유의 빛", "HEALING LIGHT", SkillAimType.SelfCentered), // 자기 중심형 치유의 빛
            new SkillDefinition(SkillId.Shield, "수호막", "SHIELD", SkillAimType.SelfCentered), // 자기 중심형 수호막
            new SkillDefinition(SkillId.Sprint, "질주", "SPRINT", SkillAimType.SelfCentered) // 자기 중심형 질주
        });

        public static bool TryGet(SkillId id, out SkillDefinition definition) // 후보 목록에서 스킬 찾기
        {
            foreach (SkillDefinition candidate in All) // 공용 후보 순회
            {
                if (candidate.Id == id) // 식별자 일치 여부
                {
                    definition = candidate; // 찾은 스킬 정의
                    return true; // 유효한 후보 확인
                }
            }
            definition = null; // 일치하는 후보 없음
            return false; // 빈 값 또는 목록 밖 식별자
        }
    }
}
