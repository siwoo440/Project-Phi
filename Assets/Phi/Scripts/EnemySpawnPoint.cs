using UnityEngine; // 유니티 구성 요소

namespace Phi // 프로토타입 영역
{
    public enum EnemySpecies // 스폰 지점의 적 종류
    {
        Slime, // 슬라임
        Goblin, // 고블린
        Skeleton, // 스켈레톤
        Orc, // 오크
        Wolf // 늑대
    }

    public sealed class EnemySpawnPoint : MonoBehaviour // 씬에 저장한 적 스폰 위치
    {
        [SerializeField] private EnemySpecies species; // 이 위치에서 등장할 적 종류

        public EnemySpecies Species // 적 종류 읽기
        {
            get // 설정된 종류 읽기
            {
                return species; // 설정된 종류 반환
            }
        }
    }
}
