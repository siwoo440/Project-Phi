using System; // 이동 함수 자료형
using System.Collections.Generic; // 경로와 장애물 목록
using UnityEngine; // 유니티 물리와 위치

namespace Phi // 적 경로 처리 영역
{
    internal sealed class EnemyNavigator // 씬 장애물 기반 적 경로 계산
    {
        private const float Clearance = 0.06f; // 장애물 모서리 여유 거리
        private const float ReplanInterval = 0.25f; // 도달 불가 경로 재검사 간격
        private const int MaximumObstacles = 32; // 경로 계산 장애물 시험 상한
        private readonly MapRegion region; // 실제 씬 맵 경계
        private readonly ContactFilter2D obstacleFilter; // 단단한 장애물 필터
        private readonly Func<Vector2, Vector2, float, bool, Vector2> move; // 기존 연속 충돌 이동
        private readonly List<Collider2D> obstacles = new List<Collider2D>(); // 실제 씬 장애물 목록
        private readonly List<Collider2D> goalOverlaps = new List<Collider2D>(); // 목표 위치의 장애물 검사 결과
        private readonly List<Vector2> nodes = new List<Vector2>(); // 경로 후보 좌표
        private readonly List<Vector2> path = new List<Vector2>(); // 현재 경로의 남은 지점
        private Vector2 plannedTarget; // 마지막 경로의 목표
        private float nextReplan; // 다음 경로 재검사 시각

        public EnemyNavigator(MapRegion region, ContactFilter2D obstacleFilter, Func<Vector2, Vector2, float, bool, Vector2> move) // 개별 적의 경로 상태 준비
        {
            this.region = region; // 씬 맵 경계 보관
            this.obstacleFilter = obstacleFilter; // 장애물 필터 보관
            this.move = move; // 기존 이동 검사 보관
        }

        public Vector2 Move(Vector2 start, Vector2 target, float distance, float radius) // 목표를 향한 안전한 접근
        {
            if (distance <= 0f) // 이동량 부재 여부
            {
                return start; // 정지 상태 유지
            }
            Vector2 goal = region.ClampPosition(target, radius); // 적 도형을 수용하는 맵 내부 목표
            if (CanTravel(start, goal, radius)) // 목표까지 직선 경로 확인
            {
                path.Clear(); // 더 이상 필요 없는 우회 경로 제거
                return Step(start, goal, distance, radius); // 기존 충돌 검사로 직접 접근
            }
            while (path.Count > 0 && Vector2.Distance(start, path[0]) < 0.01f) // 도달한 우회 지점 확인
            {
                path.RemoveAt(0); // 지난 지점 제거
            }
            bool changed = (goal - plannedTarget).sqrMagnitude > 0.25f; // 목표 이동 여부
            bool blocked = path.Count > 0 && !CanTravel(start, path[0], radius); // 현재 지점의 새 장애물 여부
            if (changed || blocked || Time.time >= nextReplan) // 재탐색 조건 확인
            {
                plannedTarget = goal; // 재탐색 목표 보관
                nextReplan = Time.time + ReplanInterval; // 다음 재검사 시각
                FindPath(start, goal, radius); // 현재 씬의 경로 계산
            }
            if (path.Count == 0) // 안전한 우회 경로 부재
            {
                return start; // 경로가 생길 때까지 제자리 유지
            }
            return Step(start, path[0], distance, radius); // 다음 우회 지점으로 이동
        }

        private Vector2 Step(Vector2 start, Vector2 destination, float distance, float radius) // 다음 경로 지점까지 이동
        {
            Vector2 offset = destination - start; // 다음 지점 방향
            float length = Mathf.Min(distance, offset.magnitude); // 지점을 넘지 않는 이동량
            return move(start, offset.normalized * length, radius, true); // 기존 장애물 이동 검사 적용
        }

        private bool CanTravel(Vector2 start, Vector2 destination, float radius) // 도형 전체의 직선 통과 확인
        {
            Vector2 delta = destination - start; // 검사할 전체 경로
            Vector2 end = move(start, delta, radius, false); // 벽에서 멈추는 연속 충돌 검사
            return (end - destination).sqrMagnitude <= 0.000001f; // 목표까지 막힘 없는지 확인
        }

        private void FindPath(Vector2 start, Vector2 goal, float radius) // 씬 장애물의 안전한 우회 경로 계산
        {
            path.Clear(); // 이전 우회 경로 제거
            nodes.Clear(); // 이전 후보 좌표 제거
            obstacles.Clear(); // 이전 씬 장애물 목록 제거
            Physics2D.SyncTransforms(); // 이동된 씬 장애물 위치 반영
            Rect area = region.Interior; // 실제 씬 경계 내부
            Physics2D.OverlapBox(area.center, area.size, 0f, obstacleFilter, obstacles); // 비트리거 장애물 수집
            if (obstacles.Count > MaximumObstacles) // 경로 계산 상한 초과 여부
            {
                return; // 계산량이 불확실하면 안전하게 정지
            }
            nodes.Add(start); // 시작 후보
            nodes.Add(goal); // 최종 목표 후보
            goalOverlaps.Clear(); // 이전 목표 위치 검사 결과 제거
            Physics2D.OverlapBox(goal, Vector2.one * (radius * 2f), 0f, obstacleFilter, goalOverlaps); // 적 도형 기준 목표 장애물 검사
            if (goalOverlaps.Count > 0) // 플레이어가 벽 가까이에 있어 적 중심이 갈 수 없는 경우
            {
                AddApproachGoals(goal, radius); // 플레이어 주변의 안전한 접근 후보 추가
            }
            int approachEnd = nodes.Count; // 대체 목표 후보의 마지막 다음 번호
            foreach (Collider2D obstacle in obstacles) // 실제 장애물 순회
            {
                Bounds bounds = obstacle.bounds; // 장애물의 월드 경계
                float margin = radius + Clearance; // 적 반폭과 모서리 여유
                AddCorner(new Vector2(bounds.min.x - margin, bounds.min.y - margin), radius); // 왼쪽 아래 우회 후보
                AddCorner(new Vector2(bounds.min.x - margin, bounds.max.y + margin), radius); // 왼쪽 위 우회 후보
                AddCorner(new Vector2(bounds.max.x + margin, bounds.min.y - margin), radius); // 오른쪽 아래 우회 후보
                AddCorner(new Vector2(bounds.max.x + margin, bounds.max.y + margin), radius); // 오른쪽 위 우회 후보
            }
            int count = nodes.Count; // 경로 후보 수
            float[] costs = new float[count]; // 시작점부터의 최단 거리
            int[] previous = new int[count]; // 최단 경로의 이전 지점
            bool[] visited = new bool[count]; // 검사 완료 후보
            for (int index = 0; index < count; index++) // 최단 거리 초기화
            {
                costs[index] = float.PositiveInfinity; // 미도달 거리
                previous[index] = -1; // 이전 지점 없음
            }
            costs[0] = 0f; // 시작점 거리
            int reached = -1; // 실제 도달 가능한 목표 번호
            for (int pass = 0; pass < count; pass++) // 최단 경로 탐색 반복
            {
                int current = -1; // 이번에 검사할 후보
                for (int index = 0; index < count; index++) // 가장 가까운 미방문 후보 선택
                {
                    if (!visited[index] && !float.IsPositiveInfinity(costs[index]) && (current < 0 || costs[index] < costs[current])) // 유효한 최소 거리 비교
                    {
                        current = index; // 가장 가까운 후보 보관
                    }
                }
                if (current < 0) // 더 이상 갈 수 있는 후보 부재
                {
                    break; // 탐색 종료
                }
                if (current == 1 || (current >= 2 && current < approachEnd)) // 원래 목표 또는 안전한 접근 후보 도달
                {
                    reached = current; // 도달 가능한 목표 보관
                    break; // 탐색 종료
                }
                visited[current] = true; // 현재 후보 검사 완료
                for (int next = 1; next < count; next++) // 다른 후보와의 이동 확인
                {
                    if (visited[next] || next == current) // 이미 검사한 후보 여부
                    {
                        continue; // 불필요한 경로 제외
                    }
                    float cost = costs[current] + Vector2.Distance(nodes[current], nodes[next]); // 새 경로의 총 거리
                    if (cost < costs[next] && CanTravel(nodes[current], nodes[next], radius)) // 더 짧고 안전한 연결 여부
                    {
                        costs[next] = cost; // 최단 거리 갱신
                        previous[next] = current; // 이전 지점 저장
                    }
                }
            }
            if (reached < 0) // 모든 목표에 도달할 수 없는 경우
            {
                return; // 안전한 경로 없이 정지
            }
            for (int node = reached; node != 0; node = previous[node]) // 목표부터 시작 직전까지 역순 순회
            {
                path.Insert(0, nodes[node]); // 실제 이동 순서로 경로 보관
            }
        }

        private void AddApproachGoals(Vector2 goal, float radius) // 벽 곁 플레이어의 대체 접근 위치
        {
            for (int index = 0; index < 8; index++) // 플레이어 주변 여덟 방향 검사
            {
                float angle = index * Mathf.PI * 0.25f; // 접근 방향 각도
                Vector2 candidate = goal + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; // 적 도형 중심의 접근 후보
                if ((region.ClampPosition(candidate, radius) - candidate).sqrMagnitude > 0.000001f) // 맵 경계 밖 여부
                {
                    continue; // 경계 밖 후보 제외
                }
                goalOverlaps.Clear(); // 이전 후보 장애물 결과 제거
                Physics2D.OverlapBox(candidate, Vector2.one * (radius * 2f), 0f, obstacleFilter, goalOverlaps); // 후보의 안전한 공간 검사
                if (goalOverlaps.Count == 0) // 장애물과 겹치지 않는 후보
                {
                    nodes.Add(candidate); // 도달 가능한 접근 후보 등록
                }
            }
        }

        private void AddCorner(Vector2 corner, float radius) // 맵 안에 있는 장애물 모서리 등록
        {
            if ((region.ClampPosition(corner, radius) - corner).sqrMagnitude <= 0.000001f) // 적 도형이 들어가는 맵 내부 여부
            {
                nodes.Add(corner); // 실제 씬 벽의 우회 후보 추가
            }
        }
    }
}
