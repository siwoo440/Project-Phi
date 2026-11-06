using System.Collections.Generic; // 충돌 검사 결과 목록
using UnityEngine; // 유니티 자료형

namespace Phi // 전투 투사체 영역
{
    internal sealed class BowArrow // 한 발의 화살 실행 상태
    {
        public const float VisualRadius = 0.05f; // 임시 화살 도형 반폭
        private const float TieTolerance = 0.00001f; // 동시 충돌 거리 오차
        private readonly GameObject visual; // 화살 표시 오브젝트
        private readonly Vector2 direction; // 발사 순간의 고정 방향
        private readonly float speed; // 발사 순간의 화살 속도
        private readonly int attack; // 발사 순간의 공격력
        private readonly int obstacleLayer; // 장애물 충돌 계층
        private float remainingDistance; // 남은 화살 이동 거리

        public BowArrow(GameObject visual, Vector2 direction, float speed, float range, int attack, int obstacleLayer) // 화살 상태 생성
        {
            this.visual = visual; // 화면 표시 보관
            this.direction = direction.normalized; // 발사 방향 고정
            this.speed = Mathf.Max(0.01f, speed); // 최소 이동속도
            remainingDistance = Mathf.Max(0f, range); // 음수 사거리 방지
            this.attack = attack; // 발사 시점 공격력 보관
            this.obstacleLayer = obstacleLayer; // 장애물 계층 보관
        }

        public bool Tick(MapRegion map, ContactFilter2D filter, List<RaycastHit2D> hits, List<Collider2D> overlaps, float elapsed) // 한 프레임의 화살 이동과 충돌
        {
            if (visual == null) // 표시 오브젝트 존재 여부
            {
                return true; // 제거된 화살 정리
            }
            Vector2 start = visual.transform.position; // 이동 전 화살 중심
            Rect area = map.Interior; // 씬 경계의 안쪽 영역
            float boundaryDistance = DistanceToBoundary(start, area); // 화살 도형이 경계에 닿는 거리
            float requested = Mathf.Min(remainingDistance, speed * Mathf.Max(0f, elapsed)); // 사거리 이내 이동 거리
            float travel = Mathf.Min(requested, boundaryDistance); // 씬 경계 이내 이동 거리
            Vector2 end = start + direction * travel; // 검사할 이동 끝점
            float nearestDistance = float.PositiveInfinity; // 현재까지 가장 가까운 충돌 거리
            Fighter target = null; // 가장 가까운 생존 적
            bool hitWall = false; // 가장 가까운 충돌의 벽 여부
            overlaps.Clear(); // 이전 겹침 결과 제거
            Physics2D.OverlapCircle(start, VisualRadius, filter, overlaps); // 화살 도형에 이미 겹친 충돌체 검사
            foreach (Collider2D overlap in overlaps) // 출발점 충돌체 검사
            {
                ConsiderHit(overlap, 0f, ref nearestDistance, ref target, ref hitWall); // 충돌 거리와 벽 우선순위 비교
            }
            hits.Clear(); // 이전 선분 결과 제거
            Physics2D.CircleCast(start, VisualRadius, direction, filter, hits, travel); // 화살 도형의 연속 이동 충돌 검사
            foreach (RaycastHit2D hit in hits) // 경로의 충돌체 검사
            {
                ConsiderHit(hit.collider, hit.distance, ref nearestDistance, ref target, ref hitWall); // 최초 충돌과 벽 우선순위 비교
            }
            bool reachesBoundary = boundaryDistance <= requested + TieTolerance; // 씬 벽 도달 여부
            if (reachesBoundary && boundaryDistance <= nearestDistance + TieTolerance) // 실제 씬 경계의 우선 충돌
            {
                hitWall = true; // 맵 경계의 벽 판정
                nearestDistance = boundaryDistance; // 맵 경계까지의 거리
                target = null; // 벽 뒤의 적 제외
            }
            if (!float.IsPositiveInfinity(nearestDistance)) // 유효한 벽 또는 생존 적 충돌
            {
                if (!hitWall && target != null) // 최초 충돌이 생존 적인 경우
                {
                    target.TakeDamage(CombatMath.Damage(attack, 1f, target.Defense, false)); // 공통 계산의 활 피해 적용
                }
                Object.Destroy(visual); // 충돌 후 화살 표시 제거
                return true; // 화살 종료
            }
            visual.transform.position = end; // 충돌 없는 화살 이동
            remainingDistance = Mathf.Max(0f, remainingDistance - travel); // 남은 사거리 갱신
            if (remainingDistance <= 0f) // 최대 사거리 도달 여부
            {
                Object.Destroy(visual); // 사거리 도달 후 표시 제거
                return true; // 화살 종료
            }
            return false; // 다음 프레임의 이동 유지
        }

        public void Clear() // 전투 종료 시 화살 정리
        {
            if (visual != null) // 화살 표시 존재 여부
            {
                Object.Destroy(visual); // 이전 전투의 화살 표시 제거
            }
        }

        private float DistanceToBoundary(Vector2 start, Rect area) // 화살 도형이 씬 경계에 닿는 거리
        {
            float distance = float.PositiveInfinity; // 초기 경계 거리
            if (direction.x > 0.000001f) // 오른쪽 이동 여부
            {
                distance = Mathf.Min(distance, (area.xMax - VisualRadius - start.x) / direction.x); // 오른쪽 경계 거리
            }
            if (direction.x < -0.000001f) // 왼쪽 이동 여부
            {
                distance = Mathf.Min(distance, (area.xMin + VisualRadius - start.x) / direction.x); // 왼쪽 경계 거리
            }
            if (direction.y > 0.000001f) // 위쪽 이동 여부
            {
                distance = Mathf.Min(distance, (area.yMax - VisualRadius - start.y) / direction.y); // 위쪽 경계 거리
            }
            if (direction.y < -0.000001f) // 아래쪽 이동 여부
            {
                distance = Mathf.Min(distance, (area.yMin + VisualRadius - start.y) / direction.y); // 아래쪽 경계 거리
            }
            return Mathf.Max(0f, distance); // 음수 경계 거리 방지
        }

        private void ConsiderHit(Collider2D collider, float distance, ref float nearestDistance, ref Fighter target, ref bool hitWall) // 충돌 거리와 우선순위 비교
        {
            if (collider == null) // 충돌체 존재 여부
            {
                return; // 유효하지 않은 충돌 제외
            }
            bool wall = collider.gameObject.layer == obstacleLayer; // 장애물 충돌 여부
            Fighter candidate = wall ? null : collider.GetComponentInParent<Fighter>(); // 충돌한 적의 전투 상태
            if (!wall && (candidate == null || candidate.Dead)) // 무관하거나 사망한 적 확인
            {
                return; // 유효하지 않은 적 충돌 제외
            }
            if (distance < nearestDistance - TieTolerance || (wall && Mathf.Abs(distance - nearestDistance) <= TieTolerance)) // 더 가까운 충돌 또는 동률 벽 우선
            {
                nearestDistance = distance; // 최초 충돌 거리 갱신
                target = candidate; // 최초 생존 적 보관
                hitWall = wall; // 벽 충돌 상태 보관
            }
        }
    }
}
