using System.Collections.Generic; // 대상 목록 자료형
using UnityEngine; // 유니티 자료형
using UnityEngine.EventSystems; // UI 포인터 판정
using UnityEngine.InputSystem; // 입력 장치
using UnityEngine.InputSystem.UI; // 새 입력 시스템 UI
using UnityEngine.SceneManagement; // 장면 재시작
using UnityEngine.UI; // 화면 표시

namespace Phi // 프로토타입 영역
{
    public sealed class PrototypeBootstrap : MonoBehaviour // 시험 전투 생성
    {
        [SerializeField] private PrototypeSettings settings; // 조정 가능한 전투 설정
        [SerializeField] private SpriteRenderer mapFloor; // 씬에 배치한 맵 바닥
        private MapRegion mapRegion; // 씬 맵의 실제 경계
        private PrototypeSettings runtimeSettings; // 현재 전투 설정
        private WeaponType equippedWeapon; // 현재 전투에서 장착한 무기
        public SkillLoadout EquippedSkills { get; private set; } // 설정과 분리된 전투 스킬 슬롯
        public SkillRuntime SkillState // 설정과 분리된 마나와 대기시간
        {
            get; // 전투 스킬 상태 읽기
            private set; // 전투 스킬 상태 내부 설정
        }
        public DodgeRuntime DodgeState // 설정과 분리된 회피 상태
        {
            get; // 전투 회피 상태 읽기
            private set; // 전투 회피 상태 내부 설정
        }
        public Interactable SelectedInteraction // 현재 선택된 상호작용 대상
        {
            get; // 선택 대상 읽기
            private set; // 선택 대상 내부 설정
        }
        public int CollectedLootCount // 이번 전투의 임시 수집 횟수
        {
            get; // 임시 수집 횟수 읽기
            private set; // 임시 수집 횟수 내부 설정
        }
        private Fighter player; // 플레이어 전투 상태
        private readonly List<Slime> slimes = new List<Slime>(); // 슬라임 목록
        private readonly List<Goblin> goblins = new List<Goblin>(); // 고블린 행동 목록
        private readonly List<Fighter> enemies = new List<Fighter>(); // 생성 순서의 공통 적 목록
        private readonly List<BowArrow> arrows = new List<BowArrow>(); // 발사된 화살 상태 목록
        private readonly List<RaycastHit2D> arrowHits = new List<RaycastHit2D>(); // 화살 경로 충돌 결과
        private readonly List<Collider2D> arrowOverlaps = new List<Collider2D>(); // 화살 출발점 충돌 결과
        private readonly List<RaycastHit2D> dodgeHits = new List<RaycastHit2D>(); // 회피 경로의 장애물 충돌 결과
        private readonly List<Collider2D> dodgeOverlaps = new List<Collider2D>(); // 회피 시작점의 장애물 충돌 결과
        private readonly List<RaycastHit2D> interactionHits = new List<RaycastHit2D>(); // 상호작용 경로의 장애물 충돌 결과
        private readonly Dictionary<Collider2D, Vector2> dodgeSafeContacts = new Dictionary<Collider2D, Vector2>(); // 실제 침투 없는 검증된 접촉 법선
        private readonly Dictionary<Collider2D, float> dodgeContactGaps = new Dictionary<Collider2D, float>(); // 검증된 접촉의 보정된 실제 간격
        private readonly List<GameObject> explosionEffects = new List<GameObject>(); // 일시적인 폭발 표시 목록
        private Camera gameCamera; // 추적 카메라
        private GameObject aimMarker; // 조준 표시
        private GameObject interactionMarker; // 선택 대상의 월드 표시
        private Text healthText; // 체력 글자
        private Text skillStatusText; // 마나와 스킬 상태 글자
        private Text interactionStatusText; // 상호작용 대상과 시험 수집 표시
        private readonly Dictionary<WeaponType, Text> equipmentLabels = new Dictionary<WeaponType, Text>(); // 무기별 선택 표시
        private readonly List<Button> equipmentButtons = new List<Button>(); // 무기 장착 버튼 목록
        private GameObject gameOverPanel; // 사망 화면
        private float nextAttack; // 다음 공격 가능 시각
        private bool blockedPress; // UI에서 시작한 클릭 차단
        private bool attackPressed; // 이번 입력 처리의 클릭 시작
        private bool pressActive; // 유효한 클릭 유지
        private InputAction attackAction; // 클릭 시작 이벤트
        private int obstacleLayer; // 장애물 충돌 계층 번호
        private int playerLayer; // 플레이어 충돌 계층 번호
        private int enemyLayer; // 적 충돌 계층 번호
        private bool combatLayersReady; // 전투 계층 준비 여부
        private ContactFilter2D swordOcclusionFilter; // 검 장애물 충돌 필터
        private ContactFilter2D arrowCollisionFilter; // 화살 적과 장애물 충돌 필터
        private readonly RaycastHit2D[] swordOcclusionHits = new RaycastHit2D[1]; // 검 장애물 판정 결과
        private Sprite square; // 임시 사각형 그림
        private Texture2D explosionTexture; // 임시 원형 폭발 테두리 질감
        private Sprite explosionSprite; // 임시 원형 폭발 테두리 그림
        private Vector2 aim = Vector2.right; // 마지막 조준 방향
        private Vector2 dodgeDirection; // 회피 시작 시 고정된 방향
        private float dodgeSpeed; // 회피 시작 시 고정된 이동속도
        private bool dodgeBlocked; // 이번 회피의 벽 도달 여부
        private BoxCollider2D dodgeProbe; // 실제 겹침 깊이의 임시 검사 도형

        private void Start() // 전투 시작
        {
            runtimeSettings = settings != null ? settings : ScriptableObject.CreateInstance<PrototypeSettings>(); // 설정이 없을 때 기본값 사용
            equippedWeapon = runtimeSettings.startingWeapon; // 설정값에서 전투 시작 무기 복사
            EquippedSkills = SkillLoadout.Create(runtimeSettings.startingSkillQ, runtimeSettings.startingSkillE, runtimeSettings.startingSkillR); // 전투 시작 스킬 복사와 슬롯 검증
            SkillState = new SkillRuntime(runtimeSettings); // 시작 마나와 대기시간 초기화
            DodgeState = new DodgeRuntime(runtimeSettings); // 시작 스태미나와 회피 상태 초기화
            List<string> skillErrors = new List<string>(); // 슬롯별 오류 목록
            AddSkillError(skillErrors, "Q", EquippedSkills.QError); // Q 슬롯 오류 수집
            AddSkillError(skillErrors, "E", EquippedSkills.EError); // E 슬롯 오류 수집
            AddSkillError(skillErrors, "R", EquippedSkills.RError); // R 슬롯 오류 수집
            if (skillErrors.Count > 0) // 비활성화된 슬롯 존재 여부
            {
                Debug.LogError("스킬 장착 오류: " + string.Join(", ", skillErrors)); // 슬롯별 오류 보고
            }
            if (!InitializeMap()) // 씬 맵과 필수 경계 확인
            {
                enabled = false; // 맵 오류 시 전투 갱신 중지
                return; // 맵을 생성하지 않고 초기화 중단
            }
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f); // 임시 전투 도형 생성
            InitializeCombatLayers(); // 전투 계층과 벽 판정 준비
            CreateCamera(); // 카메라 준비
            CreateWorld(); // 플레이어와 적 준비
            CreateInterface(); // 체력과 재시작 화면 준비
            attackAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton"); // 클릭 이벤트 구성
            attackAction.performed += OnAttackPressed; // 클릭 시작 콜백 연결
            attackAction.Enable(); // 클릭 이벤트 활성화
        }

        private bool InitializeMap() // 씬에 배치된 맵 검사
        {
            if (mapFloor == null || mapFloor.sprite == null) // 바닥 참조와 스프라이트 확인
            {
                Debug.LogError("씬에 연결된 Map Floor와 네 경계가 필요합니다."); // 필수 맵 누락 오류
                return false; // 전투 초기화 중단
            }
            Transform topObject = mapFloor.transform.Find("Boundary Top"); // 위쪽 경계 오브젝트
            Transform bottomObject = mapFloor.transform.Find("Boundary Bottom"); // 아래쪽 경계 오브젝트
            Transform leftObject = mapFloor.transform.Find("Boundary Left"); // 왼쪽 경계 오브젝트
            Transform rightObject = mapFloor.transform.Find("Boundary Right"); // 오른쪽 경계 오브젝트
            SpriteRenderer top = topObject != null ? topObject.GetComponent<SpriteRenderer>() : null; // 위쪽 경계 렌더러
            SpriteRenderer bottom = bottomObject != null ? bottomObject.GetComponent<SpriteRenderer>() : null; // 아래쪽 경계 렌더러
            SpriteRenderer left = leftObject != null ? leftObject.GetComponent<SpriteRenderer>() : null; // 왼쪽 경계 렌더러
            SpriteRenderer right = rightObject != null ? rightObject.GetComponent<SpriteRenderer>() : null; // 오른쪽 경계 렌더러
            if (top == null || bottom == null || left == null || right == null || top.sprite == null || bottom.sprite == null || left.sprite == null || right.sprite == null) // 필수 경계와 스프라이트 확인
            {
                Debug.LogError("씬에 연결된 Map Floor와 네 경계가 필요합니다."); // 필수 경계 누락 오류
                return false; // 전투 초기화 중단
            }
            mapRegion = new MapRegion(mapFloor, top, bottom, left, right); // 실제 씬 맵 참조 보관
            if (!mapRegion.IsValid()) // 맵 크기와 축 정렬 확인
            {
                Debug.LogError("씬 맵의 경계 배치 또는 크기가 올바르지 않습니다."); // 잘못된 맵 구성 오류
                mapRegion = null; // 잘못된 맵 참조 제거
                return false; // 전투 초기화 중단
            }
            return true; // 필수 씬 맵 확인 완료
        }
        private void InitializeCombatLayers() // 전투 계층과 검 장애물 필터 설정
        {
            obstacleLayer = LayerMask.NameToLayer("PhiObstacle"); // 벽 충돌 계층 번호
            playerLayer = LayerMask.NameToLayer("PhiPlayer"); // 플레이어 충돌 계층 번호
            enemyLayer = LayerMask.NameToLayer("PhiEnemy"); // 적 충돌 계층 번호
            combatLayersReady = obstacleLayer >= 0 && playerLayer >= 0 && enemyLayer >= 0; // 필수 계층 존재 여부
            if (!combatLayersReady) // 전투 계층 누락 여부
            {
                Debug.LogError("전투 충돌 계층 PhiObstacle, PhiPlayer, PhiEnemy가 필요합니다."); // 설정 오류 표시
                return; // 잘못된 벽 판정 방지
            }
            swordOcclusionFilter = new ContactFilter2D(); // 검 장애물 검사 필터
            swordOcclusionFilter.SetLayerMask(1 << obstacleLayer); // 장애물 계층만 선택
            swordOcclusionFilter.useTriggers = false; // 트리거 충돌체 제외
            arrowCollisionFilter = new ContactFilter2D(); // 화살 충돌 검사 필터
            arrowCollisionFilter.SetLayerMask((1 << obstacleLayer) | (1 << enemyLayer)); // 벽과 적의 충돌 계층 선택
            arrowCollisionFilter.useTriggers = false; // 화살 트리거 충돌 제외
        }

        private void Update() // 매 프레임 입력과 전투 처리
        {
            if (player == null || player.Dead) // 플레이어 사망 여부
            {
                SetInteractionSelection(null); // 사망 중 대상 선택 해제
                UpdateInteractionStatus(); // 남은 시험 수집 횟수 표시
                SetEquipmentButtonsInteractable(false); // 사망 중 무기 전환 차단 표시
                ClearArrows(); // 전투 종료 시 기존 화살 제거
                return; // 사망 중 전투 입력 중지
            }
            FollowCamera(); // 회피 방향 선택 전 카메라 위치 반영
            UpdateAim(); // 현재 프레임의 마우스 조준
            bool wasDodging = DodgeState.IsDodging; // 프레임 시작의 회피 상태
            float dodgeTime = DodgeState.Tick(Time.deltaTime); // 이전 회피에만 경과 시간 적용
            if (dodgeTime > 0f) // 이번 프레임의 회피 이동 여부
            {
                MoveDodge(dodgeTime); // 장애물과 씬 경계 안의 회피 이동
            }
            FollowCamera(); // 이전 회피 이동 후 카메라 위치 반영
            UpdateAim(); // 이동 위치 기준의 현재 커서 조준
            bool startedDodge = ReadDodgeInput(); // 현재 입력 시점의 새 회피 시작
            bool suppressAttack = wasDodging || startedDodge; // 이동 종료 프레임까지 새 공격 차단
            player.SetInvulnerable(DodgeState.IsInvulnerable); // 새 회피 시작과 기존 회피 종료 반영
            if (!suppressAttack) // 회피 중이 아닌 일반 이동 여부
            {
                ReadMovement(); // 이동 입력
            }
            FollowCamera(); // 이동 직후 카메라 위치 반영
            SkillState.Tick(Time.deltaTime); // 마나 회복과 대기시간 갱신
            ReadAimAndAttack(suppressAttack); // 조준과 일반 공격 입력
            if (!suppressAttack) // 회피 중 스킬 입력 차단
            {
                ReadSkillInput(); // 현재 조준 방향의 스킬 입력
            }
            UpdateArrows(); // 발사된 화살 이동과 충돌
            UpdateExplosionEffects(); // 종료된 폭발 표시 정리
            UpdateSlimes(); // 슬라임 행동
            if (!player.Dead) // 슬라임 공격 이후 생존 여부
            {
                UpdateGoblins(); // 고블린 근접 행동
            }
            if (player.Dead) // 이번 프레임의 플레이어 사망 여부
            {
                ClearArrows(); // 사망 후 화살 피해 방지
            }
            UpdateInteraction(!suppressAttack && !player.Dead); // 대상 선택과 회피 중 입력 차단
            UpdateHealth(); // 체력 표시
            UpdateSkillStatus(); // 마나와 슬롯 상태 표시
        }

        private void LateUpdate() // 이동 후 카메라 추적
        {
            FollowCamera(); // 최종 카메라 위치 반영
        }

        private void FollowCamera() // 플레이어 위치에 카메라 정렬
        {
            if (player != null && gameCamera != null) // 추적 대상 확인
            {
                Bounds floorBounds = mapRegion.FloorBounds; // 씬 바닥의 월드 영역
                float aspect = Mathf.Max(0.01f, gameCamera.aspect); // 유효한 화면 가로세로 비율
                gameCamera.orthographicSize = Mathf.Min(6f, floorBounds.extents.y, floorBounds.extents.x / aspect); // 바닥 안에 들어가는 세로 시야
                float halfViewWidth = gameCamera.orthographicSize * aspect; // 카메라 가로 시야 반경
                float halfViewHeight = gameCamera.orthographicSize; // 카메라 세로 시야 반경
                Vector3 position = player.transform.position; // 플레이어 위치
                float cameraX = Mathf.Clamp(position.x, floorBounds.min.x + halfViewWidth, floorBounds.max.x - halfViewWidth); // 바닥 내부의 카메라 가로 위치
                float cameraY = Mathf.Clamp(position.y, floorBounds.min.y + halfViewHeight, floorBounds.max.y - halfViewHeight); // 바닥 내부의 카메라 세로 위치
                gameCamera.transform.position = new Vector3(cameraX, cameraY, -10f); // 씬 맵 내부 카메라 추적
            }
        }

        private void CreateCamera() // 카메라 생성
        {
            gameCamera = Camera.main; // 기존 메인 카메라 확인
            if (gameCamera == null) // 카메라가 없는 경우
            {
                GameObject cameraObject = new GameObject("Main Camera"); // 카메라 오브젝트
                cameraObject.tag = "MainCamera"; // 메인 카메라 태그
                gameCamera = cameraObject.AddComponent<Camera>(); // 카메라 부착
            }
            gameCamera.orthographic = true; // 2D 직교 투영
            gameCamera.orthographicSize = 6f; // 시야 크기
            gameCamera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경
            gameCamera.backgroundColor = new Color(0.12f, 0.18f, 0.17f); // 배경색
            gameCamera.transform.position = new Vector3(0f, 0f, -10f); // 초기 카메라 위치
        }

        private GameObject Shape(string objectName, Vector2 position, Vector2 scale, Color color, int order) // 임시 도형 생성
        {
            GameObject shape = new GameObject(objectName); // 도형 오브젝트
            shape.transform.position = position; // 도형 위치
            shape.transform.localScale = new Vector3(scale.x, scale.y, 1f); // 도형 크기
            SpriteRenderer renderer = shape.AddComponent<SpriteRenderer>(); // 그림 표시기
            renderer.sprite = square; // 사각형 그림
            renderer.color = color; // 도형 색상
            renderer.sortingOrder = order; // 겹침 순서
            return shape; // 생성 도형
        }

        private void CreateWorld() // 전투 대상 생성
        {
            Vector2 mapCenter = mapRegion.FloorBounds.center; // 씬 맵의 중심 위치
            GameObject playerObject = Shape("Player", mapRegion.ClampPosition(mapCenter, PrototypeSettings.PlayerRadius), new Vector2(0.7f, 0.7f), new Color(0.3f, 0.65f, 1f), 2); // 맵 중심의 플레이어 임시 도형
            if (combatLayersReady) // 전투 계층 준비 여부
            {
                playerObject.layer = playerLayer; // 플레이어 충돌 계층 지정
            }
            player = playerObject.AddComponent<Fighter>(); // 플레이어 체력 상태
            player.Initialize(runtimeSettings.playerHealth, runtimeSettings.playerDefense); // 플레이어 능력치 설정
            aimMarker = Shape("Aim", Vector2.zero, new Vector2(0.8f, 0.12f), Color.white, 3); // 조준 방향 표시
            interactionMarker = Shape("Interaction Marker", Vector2.zero, new Vector2(0.2f, 0.2f), new Color(1f, 0.9f, 0.2f), 5); // 선택 대상의 임시 표시
            interactionMarker.SetActive(false); // 선택 대상이 없을 때 표시 숨김
            for (int index = 0; index < Mathf.Max(0, runtimeSettings.slimeCount); index++) // 시험 슬라임 생성 반복
            {
                float angle = index * Mathf.PI * 2f / Mathf.Max(1, runtimeSettings.slimeCount); // 배치 각도
                Vector2 position = mapRegion.ClampPosition(mapCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f, PrototypeSettings.SlimeRadius); // 경계 안의 배치 위치
                GameObject slimeObject = Shape("Slime " + (index + 1), position, new Vector2(0.65f, 0.65f), new Color(0.35f, 0.9f, 0.35f), 1); // 슬라임 도형
                if (combatLayersReady) // 전투 계층 준비 여부
                {
                    slimeObject.layer = enemyLayer; // 적 충돌 계층 지정
                }
                BoxCollider2D slimeCollider = slimeObject.AddComponent<BoxCollider2D>(); // 화살 적중용 슬라임 충돌체
                slimeCollider.size = Vector2.one; // 슬라임 도형과 같은 충돌 크기
                Fighter fighter = slimeObject.AddComponent<Fighter>(); // 슬라임 체력 상태
                fighter.Initialize(runtimeSettings.slimeHealth, runtimeSettings.slimeDefense); // 슬라임 능력치 설정
                fighter.Died += SpawnSlimeLoot; // 슬라임 사망 전이의 시험 전리품 연결
                slimes.Add(new Slime(fighter)); // 슬라임 행동 상태 추가
                enemies.Add(fighter); // 공통 공격 대상에 슬라임 등록
            }
            for (int index = 0; index < Mathf.Max(0, runtimeSettings.goblinCount); index++) // 시험 고블린 생성 반복
            {
                float angle = -Mathf.PI * 0.5f + index * Mathf.PI * 2f / Mathf.Max(1, runtimeSettings.goblinCount); // 근접 적 배치 각도
                Vector2 position = mapRegion.ClampPosition(mapCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f, PrototypeSettings.SlimeRadius); // 씬 경계 안의 고블린 위치
                GameObject goblinObject = Shape("Goblin " + (index + 1), position, new Vector2(0.7f, 0.7f), new Color(0.85f, 0.45f, 0.25f), 1); // 고블린 임시 도형
                if (combatLayersReady) // 전투 계층 준비 여부
                {
                    goblinObject.layer = enemyLayer; // 적 충돌 계층 지정
                }
                BoxCollider2D goblinCollider = goblinObject.AddComponent<BoxCollider2D>(); // 투사체 적중용 충돌체
                goblinCollider.size = Vector2.one; // 고블린 도형 크기와 일치
                Fighter goblinFighter = goblinObject.AddComponent<Fighter>(); // 고블린 전투 상태
                goblinFighter.Initialize(runtimeSettings.goblinHealth, runtimeSettings.goblinDefense); // 고블린 기획 능력치 적용
                GameObject weapon = Shape("Goblin Weapon", position, new Vector2(0.18f, 0.8f), new Color(0.95f, 0.9f, 0.6f), 2); // 근접 베기 예고용 무기
                weapon.transform.SetParent(goblinObject.transform, false); // 고블린에 무기 표시 연결
                weapon.transform.localPosition = new Vector3(0.55f, 0.05f, 0f); // 본체 옆 무기 위치
                goblins.Add(new Goblin(goblinFighter, weapon.transform)); // 독립 고블린 행동 등록
                enemies.Add(goblinFighter); // 공통 공격 대상에 고블린 등록
            }
        }

        private void ReadMovement() // 키보드 이동
        {
            if (CombatInputBlocked()) // UI의 전투 입력 점유 여부
            {
                return; // 화면 입력 중 플레이어 이동 차단
            }
            Vector2 movement = MovementInput() * runtimeSettings.playerSpeed * Time.deltaTime; // 정규화 이동량
            player.transform.position = mapRegion.ClampPosition((Vector2)player.transform.position + movement, PrototypeSettings.PlayerRadius); // 경계 안의 플레이어 위치 변경
        }

        private static Vector2 MovementInput() // 현재 WASD 이동 방향
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null) // 키보드 부재 확인
            {
                return Vector2.zero; // 이동 입력 없음
            }
            Vector2 input = new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f), (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)); // WASD 방향
            return CombatMath.NormalizeMovement(input); // 대각선 속도 정규화
        }

        private bool ReadDodgeInput() // Shift 회피 입력과 시작 방향
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null || CombatInputBlocked() || !combatLayersReady || !(keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)) // 입력과 충돌 검사 조건
            {
                return false; // 회피 시작 없음
            }
            if (!DodgeState.TryDodge()) // 자원과 대기시간 검사
            {
                return false; // 회피 실패 시 기존 전투 유지
            }
            Vector2 movement = MovementInput(); // 회피 시작 순간의 이동 방향
            dodgeDirection = movement.sqrMagnitude > 0f ? movement : -aim; // 이동 방향 또는 조준 반대 방향 고정
            dodgeSpeed = Mathf.Max(0f, runtimeSettings.dodgeDistance) / Mathf.Max(0.00001f, runtimeSettings.dodgeDuration); // 회피 시작 속도 고정
            dodgeBlocked = false; // 새 회피의 장애물 상태 초기화
            return true; // 회피 시작 성공
        }

        private void MoveDodge(float elapsed) // 장애물과 씬 경계에 막히는 회피 이동
        {
            if (dodgeBlocked) // 이번 회피의 벽 도달 여부
            {
                return; // 남은 회피 시간 동안 제자리 유지
            }
            Vector2 start = player.transform.position; // 회피 이동 시작 중심
            float distance = Mathf.Max(0f, dodgeSpeed * elapsed); // 남은 회피 시간의 이동 거리
            float requested = distance; // 장애물 검사 전 이동 거리
            Rect area = mapRegion.Interior; // 씬 경계 안쪽 영역
            float radius = PrototypeSettings.PlayerRadius; // 플레이어 사각형 반폭
            if (dodgeDirection.x > 0f) // 오른쪽 회피 여부
            {
                distance = Mathf.Min(distance, (area.xMax - radius - start.x) / dodgeDirection.x); // 오른쪽 경계 도달 거리
            }
            if (dodgeDirection.x < 0f) // 왼쪽 회피 여부
            {
                distance = Mathf.Min(distance, (area.xMin + radius - start.x) / dodgeDirection.x); // 왼쪽 경계 도달 거리
            }
            if (dodgeDirection.y > 0f) // 위쪽 회피 여부
            {
                distance = Mathf.Min(distance, (area.yMax - radius - start.y) / dodgeDirection.y); // 위쪽 경계 도달 거리
            }
            if (dodgeDirection.y < 0f) // 아래쪽 회피 여부
            {
                distance = Mathf.Min(distance, (area.yMin + radius - start.y) / dodgeDirection.y); // 아래쪽 경계 도달 거리
            }
            distance = Mathf.Max(0f, distance); // 음수 이동 거리 방지
            Physics2D.SyncTransforms(); // 이동된 장애물의 물리 위치 반영
            Vector2 size = Vector2.one * (radius * 2f); // 플레이어 사각 도형 크기
            dodgeOverlaps.Clear(); // 이전 시작점 충돌 결과 제거
            dodgeSafeContacts.Clear(); // 이전 검사 접촉 법선 제거
            dodgeContactGaps.Clear(); // 이전 검사 접촉 간격 제거
            Physics2D.OverlapBox(start, size, 0f, swordOcclusionFilter, dodgeOverlaps); // 시작점의 장애물 겹침 검사
            if (DodgeStartsInsideObstacle(start, size)) // 실제 장애물 내부의 시작점 여부
            {
                dodgeBlocked = true; // 현재 회피의 이동 중단
                return; // 남은 회피 시간 동안 제자리 유지
            }
            foreach (KeyValuePair<Collider2D, Vector2> contact in dodgeSafeContacts) // 실제 침투 없는 검증된 접촉 순회
            {
                float approach = Vector2.Dot(dodgeDirection, contact.Value); // 현재 접촉면을 향한 이동 비율
                if (approach > 0f) // 접촉면 방향 이동 여부
                {
                    float gap = Mathf.Max(0f, dodgeContactGaps[contact.Key]); // 음수 없는 실제 도형 간격
                    distance = Mathf.Min(distance, gap / approach); // 실제 접촉면까지의 보수적인 이동 거리
                }
            }
            dodgeHits.Clear(); // 이전 경로 충돌 결과 제거
            Physics2D.BoxCast(start, size, 0f, dodgeDirection, swordOcclusionFilter, dodgeHits, distance); // 플레이어 크기의 연속 경로 검사
            foreach (RaycastHit2D hit in dodgeHits) // 충돌한 장애물 순회
            {
                if (hit.distance <= 0.00001f && dodgeSafeContacts.TryGetValue(hit.collider, out Vector2 normal) && Vector2.Dot(dodgeDirection, normal) < -0.0001f) // 검증된 접촉면에서 장애물 반대 방향 여부
                {
                    continue; // 접촉면의 후퇴 허용
                }
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.0001f)); // 가장 가까운 벽 앞 정지
            }
            player.transform.position = mapRegion.ClampPosition(start + dodgeDirection * distance, radius); // 씬 경계 안쪽의 회피 위치
            dodgeBlocked = distance < requested - 0.000001f; // 벽에 닿으면 남은 이동 중단
        }

        private bool DodgeStartsInsideObstacle(Vector2 position, Vector2 size) // 접촉 여유와 실제 침투 구분
        {
            if (dodgeOverlaps.Count == 0) // 근처 장애물 부재 여부
            {
                return false; // 시작점 침투 없음
            }
            if (dodgeProbe == null) // 거리 검사 도형 생성 여부
            {
                GameObject probeObject = new GameObject("Dodge Collision Probe"); // 임시 거리 검사 오브젝트
                probeObject.layer = playerLayer; // 적과 장애물 판정에서 제외할 계층
                dodgeProbe = probeObject.AddComponent<BoxCollider2D>(); // 플레이어 모양의 검사 충돌체
                dodgeProbe.isTrigger = true; // 물리 반응 없는 검사 도형
                dodgeProbe.enabled = false; // 검사 이외의 충돌 방지
            }
            dodgeProbe.transform.position = position; // 플레이어 중심에 검사 도형 배치
            dodgeProbe.size = size; // 플레이어의 월드 크기 사용
            dodgeProbe.enabled = true; // 일시적인 거리 검사 활성화
            try // 검사 도형의 안전한 해제 보장
            {
                Physics2D.SyncTransforms(); // 검사 도형 위치 반영
                foreach (Collider2D obstacle in dodgeOverlaps) // 겹침 후보 순회
                {
                    ColliderDistance2D separation = dodgeProbe.Distance(obstacle); // 두 도형의 실제 최단 거리
                    float offset = Mathf.Max(0f, Physics2D.defaultContactOffset); // 물리 도형의 접촉 여유
                    bool verifiedBox = obstacle is BoxCollider2D box && box.edgeRadius <= 0f; // 검증된 기본 사각 충돌체
                    bool verifiedCircle = obstacle is CircleCollider2D; // 검증된 원형 충돌체
                    float correction = verifiedBox ? offset * 2f : verifiedCircle ? offset : 0f; // 검증된 도형의 접촉 여유 보정
                    if (!separation.isValid || separation.distance + correction < -0.000001f) // 유효하지 않은 검사 또는 보정 후 실제 침투
                    {
                        return true; // 안전한 회피 차단
                    }
                    if (verifiedBox || verifiedCircle) // 보정 규칙을 검증한 도형 여부
                    {
                        dodgeSafeContacts[obstacle] = separation.normal; // 검사 도형에서 벽을 향한 법선 보관
                        dodgeContactGaps[obstacle] = separation.distance + correction; // 접촉 여유를 제외한 실제 간격
                    }
                }
                return false; // 실제 침투 없는 접촉 여유
            }
            finally // 예외나 조기 반환 이후 정리
            {
                dodgeProbe.enabled = false; // 물리 단계 이전 검사 도형 비활성화
            }
        }

        private void UpdateAim() // 현재 마우스 조준 방향 갱신
        {
            Mouse mouse = Mouse.current; // 현재 마우스
            if (mouse == null) // 마우스 부재 확인
            {
                return; // 기존 조준 방향 유지
            }
            Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, -gameCamera.transform.position.z)); // 커서의 월드 좌표
            Vector2 direction = (Vector2)(mouseWorld - player.transform.position); // 플레이어에서 커서 방향
            if (direction.sqrMagnitude > 0.0001f) // 유효한 조준 방향
            {
                aim = direction.normalized; // 조준 방향 저장
            }
            UpdateAimMarker(); // 경계 안의 조준 표시 갱신
        }

        private void ReadAimAndAttack(bool suppressAttack) // 마우스 조준과 공격
        {
            Mouse mouse = Mouse.current; // 현재 마우스
            if (mouse == null) // 마우스 부재 확인
            {
                return; // 마우스 입력 없음
            }
            UpdateAim(); // 이동 이후 조준 표시 갱신
            bool inputBlocked = CombatInputBlocked(); // 화면의 전투 입력 점유 여부
            if (inputBlocked && (attackPressed || mouse.leftButton.isPressed)) // 화면 입력 중 클릭 유지 여부
            {
                blockedPress = true; // 클릭 해제 전까지 공격 차단 유지
            }
            if (!suppressAttack && !inputBlocked && (attackPressed || (pressActive && mouse.leftButton.isPressed)) && !blockedPress && !PointerOverUi(mouse.position.ReadValue()) && Time.time >= nextAttack) // 짧은 클릭과 홀드 공격 검사
            {
                Attack(); // 선택한 무기의 일반 공격
                float interval = equippedWeapon == WeaponType.Staff ? runtimeSettings.staffInterval : equippedWeapon == WeaponType.Bow ? runtimeSettings.bowInterval : runtimeSettings.swordInterval; // 장착 무기의 공격 간격
                nextAttack = Time.time + Mathf.Max(0.01f, interval); // 다음 공격 가능 시각
            }
            attackPressed = false; // 이번 프레임의 클릭 시작 처리 완료
            if (!mouse.leftButton.isPressed) // 클릭 종료 여부
            {
                blockedPress = false; // 클릭 차단 해제
                pressActive = false; // 클릭 유지 상태 해제
            }
        }

        private void ReadSkillInput() // 슬롯 순서의 키보드 시전
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null || CombatInputBlocked()) // 키보드 부재 또는 화면 입력 여부
            {
                return; // 스킬 키 입력 생략
            }
            if (keyboard.qKey.wasPressedThisFrame) // Q 키 시작 여부
            {
                UseSkill(0, EquippedSkills.Q); // Q 슬롯 시전
            }
            if (keyboard.eKey.wasPressedThisFrame) // E 키 시작 여부
            {
                UseSkill(1, EquippedSkills.E); // E 슬롯 시전
            }
            if (keyboard.rKey.wasPressedThisFrame) // R 키 시작 여부
            {
                UseSkill(2, EquippedSkills.R); // R 슬롯 시전
            }
        }

        private static bool CombatInputBlocked() // UI의 전투 입력 차단 여부
        {
            return CombatInputFocus.IsCaptured || TextInputFocused(); // 화면 점유 또는 실제 텍스트 편집
        }

        private static bool TextInputFocused() // 텍스트 입력에 사용 중인 UI 확인
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null; // 현재 선택된 UI
            if (selected == null) // 선택된 UI 부재
            {
                return false; // 스킬 입력 허용
            }
            InputField field = selected.GetComponentInParent<InputField>(); // 기본 텍스트 입력 필드
            if (field != null && field.isFocused) // 기본 입력 필드의 실제 편집 여부
            {
                return true; // 입력 중 스킬 차단
            }
            for (Transform node = selected.transform; node != null; node = node.parent) // 선택된 UI의 부모 검사
            {
                Component tmp = node.GetComponent("TMP_InputField"); // 선택적 텍스트 메시 입력 필드
                if (tmp == null) // 입력 필드 부재 여부
                {
                    continue; // 다음 부모 검사
                }
                System.Reflection.PropertyInfo focus = tmp.GetType().GetProperty("isFocused"); // 입력 패키지의 포커스 속성
                return focus != null && focus.GetValue(tmp) is bool active && active; // 실제 텍스트 편집 여부
            }
            return false; // 텍스트 입력이 아닌 선택 상태
        }

        private void UseSkill(int slot, SkillId skill) // 선택 슬롯의 스킬 시전
        {
            int healAmount = Mathf.FloorToInt(player.MaximumHealth * Mathf.Max(0f, runtimeSettings.healingFraction)); // 최대 체력 기준 회복량
            bool allowed = skill != SkillId.HealingLight || (player.Health < player.MaximumHealth && healAmount > 0); // 치유 가능 체력 확인
            allowed &= combatLayersReady || skill == SkillId.HealingLight; // 피해 스킬의 충돌 계층 확인
            if (!SkillState.TryUse(slot, skill, allowed)) // 자원과 대기시간 확인
            {
                return; // 실패 시 스킬 효과 생략
            }
            if (skill == SkillId.Fireball) // 화염탄 시전 여부
            {
                float size = BowArrow.VisualRadius * 2f; // 화염탄 표시와 판정 크기
                GameObject visual = Shape("Fireball", player.transform.position, new Vector2(size, size), new Color(1f, 0.3f, 0.1f), 4); // 화염탄 임시 도형
                arrows.Add(new BowArrow(visual, aim, runtimeSettings.fireballSpeed, runtimeSettings.fireballRange, CurrentAttack(), obstacleLayer, null, runtimeSettings.fireballMultiplier)); // 발사 순간 방향과 공격력 고정
            }
            else if (skill == SkillId.WhirlwindSlash) // 회오리 베기 시전 여부
            {
                CastWhirlwind(); // 주변 적의 범위 피해
            }
            else if (skill == SkillId.HealingLight) // 치유의 빛 시전 여부
            {
                player.Heal(healAmount); // 최대 체력 이내 회복
                ShowCircleEffect(player.transform.position, 0.6f, "Healing Light", new Color(0.55f, 1f, 0.6f)); // 임시 치유 표시
            }
        }

        private int CurrentAttack() // 장착 무기를 반영한 최종 공격력
        {
            int weaponAttack = equippedWeapon == WeaponType.Staff ? runtimeSettings.staffAttack : equippedWeapon == WeaponType.Bow ? runtimeSettings.bowAttack : runtimeSettings.swordAttack; // 현재 무기의 공격력
            return runtimeSettings.playerAttack + weaponAttack; // 무기 공격력 한 번 합산
        }

        private void CastWhirlwind() // 자기 중심 범위 피해
        {
            Vector2 origin = player.transform.position; // 시전 순간 중심 위치
            float radius = Mathf.Max(0f, runtimeSettings.whirlwindRadius); // 음수 없는 판정 반경
            ShowCircleEffect(origin, radius, "Whirlwind Slash", new Color(0.95f, 0.9f, 0.45f)); // 임시 범위 표시
            Physics2D.SyncTransforms(); // 변경된 적과 장애물 위치 반영
            HashSet<Fighter> targets = new HashSet<Fighter>(); // 공격당 중복 피해 방지
            foreach (Fighter target in enemies) // 생성 순서의 적 순회
            {
                if (target == null || target.Dead || !targets.Add(target)) // 사망 또는 중복 대상 확인
                {
                    continue; // 피해 대상 제외
                }
                Vector2 position = target.transform.position; // 현재 대상 중심
                if ((position - origin).sqrMagnitude > radius * radius || Physics2D.Linecast(origin, position, swordOcclusionFilter, swordOcclusionHits) != 0) // 범위 밖 또는 장애물 뒤 여부
                {
                    continue; // 피해 대상 제외
                }
                target.TakeDamage(CombatMath.Damage(CurrentAttack(), runtimeSettings.whirlwindMultiplier, target.Defense, false)); // 공통 피해식 적용
            }
        }

        private void UpdateAimMarker() // 조준 표시의 경계 제한
        {
            Vector2 origin = player.transform.position; // 플레이어 중심 좌표
            float markerWidth = 0.12f; // 조준 표시 두께
            Rect area = mapRegion.Interior; // 씬 경계의 안쪽 영역
            float paddingX = markerWidth * 0.5f * Mathf.Abs(aim.y); // 조준 표시의 가로 여유
            float paddingY = markerWidth * 0.5f * Mathf.Abs(aim.x); // 조준 표시의 세로 여유
            float left = area.xMin + paddingX; // 표시 두께를 반영한 왼쪽 한계
            float right = area.xMax - paddingX; // 표시 두께를 반영한 오른쪽 한계
            float bottom = area.yMin + paddingY; // 표시 두께를 반영한 아래쪽 한계
            float top = area.yMax - paddingY; // 표시 두께를 반영한 위쪽 한계
            float length = 1.15f; // 기본 조준 표시 길이
            if (aim.x > 0.0001f) // 오른쪽 조준 여부
            {
                length = Mathf.Min(length, (right - origin.x) / aim.x); // 오른쪽 경계까지의 길이
            }
            if (aim.x < -0.0001f) // 왼쪽 조준 여부
            {
                length = Mathf.Min(length, (left - origin.x) / aim.x); // 왼쪽 경계까지의 길이
            }
            if (aim.y > 0.0001f) // 위쪽 조준 여부
            {
                length = Mathf.Min(length, (top - origin.y) / aim.y); // 위쪽 경계까지의 길이
            }
            if (aim.y < -0.0001f) // 아래쪽 조준 여부
            {
                length = Mathf.Min(length, (bottom - origin.y) / aim.y); // 아래쪽 경계까지의 길이
            }
            length = Mathf.Max(0f, length); // 음수 길이 방지
            aimMarker.transform.position = player.transform.position + (Vector3)(aim * (length * 0.5f)); // 플레이어 중심에서 표시 끝까지 위치
            aimMarker.transform.localScale = new Vector3(length, markerWidth, 1f); // 남은 공간에 맞춘 표시 길이
            aimMarker.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg); // 조준 표시 방향
        }
        private void OnAttackPressed(InputAction.CallbackContext context) // 클릭 입력 이벤트 처리
        {
            Mouse mouse = Mouse.current; // 클릭 장치 확인
            if (mouse == null) // 마우스 부재 확인
            {
                return; // 클릭 처리 중지
            }
            attackPressed = true; // 짧은 클릭 보존
            pressActive = true; // 클릭 유지 상태 기록
            blockedPress = CombatInputBlocked() || PointerOverUi(mouse.position.ReadValue()); // 화면 점유와 UI 클릭 차단
        }

        private void OnDestroy() // 입력 이벤트 정리
        {
            ClearArrows(); // 장면 종료 시 화살 상태 정리
            SetInteractionSelection(null); // 장면 종료 시 대상 선택 해제
            foreach (Slime slime in slimes) // 등록된 슬라임 순회
            {
                if (slime.Fighter != null) // 남아 있는 슬라임 여부
                {
                    slime.Fighter.Died -= SpawnSlimeLoot; // 사망 전리품 연결 해제
                }
            }
            if (dodgeProbe != null) // 임시 회피 검사 도형 존재 여부
            {
                Destroy(dodgeProbe.gameObject); // 전투 종료 시 검사 오브젝트 제거
            }
            if (explosionSprite != null) // 폭발 그림 생성 여부
            {
                Destroy(explosionSprite); // 임시 폭발 그림 제거
            }
            if (explosionTexture != null) // 폭발 질감 생성 여부
            {
                Destroy(explosionTexture); // 임시 폭발 질감 제거
            }
            if (attackAction != null) // 입력 이벤트 생성 여부
            {
                attackAction.performed -= OnAttackPressed; // 클릭 콜백 해제
                attackAction.Dispose(); // 입력 이벤트 자원 해제
            }
        }

        private static bool PointerOverUi(Vector2 position) // UI 클릭 여부 검사
        {
            if (EventSystem.current == null) // 이벤트 시스템 부재
            {
                return false; // UI 차단 대상 없음
            }
            PointerEventData pointer = new PointerEventData(EventSystem.current); // 포인터 검사 정보
            pointer.position = position; // 화면 좌표 설정
            List<RaycastResult> results = new List<RaycastResult>(); // UI 검사 결과 목록
            EventSystem.current.RaycastAll(pointer, results); // UI 영역 검사
            return results.Count > 0; // UI 겹침 여부
        }

        private void SpawnSlimeLoot(Fighter source) // 슬라임 사망 전이의 시험 전리품 생성
        {
            GameObject loot = Shape("Slime Loot", source.transform.position, new Vector2(0.3f, 0.3f), new Color(0.8f, 1f, 0.35f), 2); // 임시 바닥 전리품 표시
            Interactable target = loot.AddComponent<Interactable>(); // 공통 상호작용 대상 연결
            target.Configure(InteractionKind.Loot, "SLIME GEL (TEST)"); // 시험 아이템 이름과 식별자 설정
        }

        private void UpdateInteraction(bool allowInput) // 가장 가까운 대상 선택과 짧은 F 입력
        {
            if (player == null || player.Dead || !combatLayersReady) // 사망 또는 벽 판정 불가능 여부
            {
                SetInteractionSelection(null); // 잘못된 선택과 수집 방지
                UpdateInteractionStatus(); // 현재 시험 수집 횟수 표시
                return; // 상호작용 입력 중지
            }
            Physics2D.SyncTransforms(); // 대상과 장애물의 최신 위치 반영
            Vector2 origin = player.transform.position; // 플레이어 중심 위치
            float range = Mathf.Max(0f, runtimeSettings.interactionRange); // 음수 없는 시험 반경
            float nearest = float.PositiveInfinity; // 현재 선택 대상의 거리 제곱
            Interactable selected = null; // 가장 가까운 유효 대상
            foreach (Interactable candidate in Interactable.Active) // 등록 순서의 활성 대상 순회
            {
                if (candidate == null || !candidate.IsAvailable) // 제거되었거나 비활성 대상 여부
                {
                    continue; // 선택 후보 제외
                }
                Vector2 destination = candidate.transform.position; // 상호작용 대상 중심
                float distance = (destination - origin).sqrMagnitude; // 플레이어와 대상의 거리 제곱
                if (distance > range * range || distance >= nearest || InteractionOccluded(origin, destination, candidate)) // 반경·등록 순서·벽 차단 검사
                {
                    continue; // 더 멀거나 접근할 수 없는 대상 제외
                }
                nearest = distance; // 가장 가까운 거리 갱신
                selected = candidate; // 현재 대상 선택
            }
            SetInteractionSelection(selected); // 선택 표시와 월드 마커 갱신
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (allowInput && !CombatInputBlocked() && keyboard != null && keyboard.fKey.wasPressedThisFrame && selected != null) // 짧은 F 입력과 유효 대상 확인
            {
                bool loot = selected.Kind == InteractionKind.Loot; // 시험 전리품 수집 여부
                if (selected.TryInteract() && loot) // 유효한 전리품의 단일 수집
                {
                    CollectedLootCount++; // 이번 전투의 시험 수집 횟수 증가
                    SetInteractionSelection(null); // 수집 직후 이전 선택 해제
                }
            }
            UpdateInteractionStatus(); // 대상과 시험 수집 표시 갱신
        }

        private bool InteractionOccluded(Vector2 origin, Vector2 destination, Interactable candidate) // 비트리거 장애물의 시야 차단
        {
            interactionHits.Clear(); // 이전 장애물 검사 결과 제거
            Physics2D.Linecast(origin, destination, swordOcclusionFilter, interactionHits); // 대상 중심까지의 장애물 검사
            foreach (RaycastHit2D hit in interactionHits) // 겹친 장애물 순회
            {
                if (hit.collider != null && !hit.collider.transform.IsChildOf(candidate.transform)) // 대상 자신의 충돌체가 아닌 장애물 여부
                {
                    return true; // 다른 장애물 뒤의 대상 제외
                }
            }
            return false; // 장애물에 가려지지 않은 대상
        }

        private void SetInteractionSelection(Interactable target) // 단일 대상 선택과 표시 갱신
        {
            if (SelectedInteraction != target) // 선택 대상 변경 여부
            {
                if (SelectedInteraction != null) // 이전 선택 대상 존재 여부
                {
                    SelectedInteraction.SetSelected(false); // 이전 대상 선택 해제
                }
                SelectedInteraction = target; // 새 대상 참조 보관
            }
            if (SelectedInteraction != null) // 새 대상 존재 여부
            {
                SelectedInteraction.SetSelected(true); // 대상 선택 상태 표시
            }
            if (interactionMarker != null) // 월드 선택 표시 존재 여부
            {
                interactionMarker.SetActive(SelectedInteraction != null); // 선택 대상 유무에 따른 표시
                if (SelectedInteraction != null) // 선택된 대상의 위치 확인
                {
                    interactionMarker.transform.position = SelectedInteraction.transform.position + new Vector3(0f, 0.5f, 0f); // 대상 위쪽의 선택 표시
                }
            }
        }

        private void UpdateInteractionStatus() // 대상 안내와 임시 수집 횟수 표시
        {
            if (interactionStatusText == null) // 인터페이스 준비 여부
            {
                return; // 초기화 전 표시 생략
            }
            string selection = "NO TARGET"; // 대상이 없을 때의 안내
            if (SelectedInteraction != null) // 선택된 대상 존재 여부
            {
                bool loot = SelectedInteraction.Kind == InteractionKind.Loot; // 실제 시험 수집 대상 여부
                selection = "F: " + SelectedInteraction.DisplayName + (loot ? "" : " (NOT IMPLEMENTED)"); // 대상과 미구현 안내
            }
            interactionStatusText.text = selection + "\nTEST LOOT " + CollectedLootCount; // 전투 중 시험 수집 카운트
        }

        private void Attack() // 선택한 무기의 일반 공격
        {
            if (!combatLayersReady) // 충돌 계층 누락 여부
            {
                return; // 장애물 검사가 불가능한 공격 차단
            }
            if (equippedWeapon == WeaponType.Bow) // 활 장착 여부
            {
                LaunchArrow(); // 고정 조준 방향의 화살 발사
                return; // 검 공격 생략
            }
            if (equippedWeapon == WeaponType.Staff) // 지팡이 장착 여부
            {
                LaunchStaffBolt(); // 고정 조준 방향의 마법탄 발사
                return; // 검 공격 생략
            }
            Physics2D.SyncTransforms(); // 변경된 장애물 위치의 물리 반영
            List<Fighter> targets = new List<Fighter>(); // 이번 공격의 대상 목록
            foreach (Fighter target in enemies) // 각 적 검사
            {
                if (target != null && !target.Dead && CombatMath.InSwordSector(player.transform.position, aim, target.transform.position, runtimeSettings.swordRange, runtimeSettings.swordAngle) && Physics2D.Linecast(player.transform.position, target.transform.position, swordOcclusionFilter, swordOcclusionHits) == 0) // 생존과 부채꼴 및 벽 차단 판정
                {
                    targets.Add(target); // 고유 대상 추가
                }
            }
            targets.Sort((left, right) => ((Vector2)(left.transform.position - player.transform.position)).sqrMagnitude.CompareTo(((Vector2)(right.transform.position - player.transform.position)).sqrMagnitude)); // 가까운 대상 우선
            for (int index = 0; index < Mathf.Min(targets.Count, Mathf.Max(0, runtimeSettings.swordTargets)); index++) // 최대 대상 수 제한
            {
                Fighter target = targets[index]; // 현재 대상
                target.TakeDamage(CombatMath.Damage(runtimeSettings.playerAttack + runtimeSettings.swordAttack, 1f, target.Defense, false)); // 공통 계산으로 피해 적용
            }
        }

        private void LaunchArrow() // 활의 일반 공격 발사
        {
            float size = BowArrow.VisualRadius * 2f; // 임시 화살 도형 크기
            GameObject visual = Shape("Arrow", player.transform.position, new Vector2(size, size), new Color(1f, 0.85f, 0.2f), 4); // 플레이어 중심의 화살 표시
            int attack = runtimeSettings.playerAttack + runtimeSettings.bowAttack; // 발사 순간의 최종 공격력
            arrows.Add(new BowArrow(visual, aim, runtimeSettings.bowProjectileSpeed, runtimeSettings.bowRange, attack, obstacleLayer)); // 고정 방향 화살 상태 추가
        }

        private void LaunchStaffBolt() // 지팡이의 일반 공격 발사
        {
            float size = BowArrow.VisualRadius * 2f; // 임시 마법탄 도형 크기
            GameObject visual = Shape("Magic Bolt", player.transform.position, new Vector2(size, size), new Color(0.45f, 0.75f, 1f), 4); // 플레이어 중심의 마법탄 표시
            int attack = runtimeSettings.playerAttack + runtimeSettings.staffAttack; // 발사 순간의 최종 공격력
            float radius = Mathf.Max(0f, runtimeSettings.staffExplosionRadius); // 발사 순간의 폭발 반경
            int maximum = Mathf.Max(0, runtimeSettings.staffExplosionTargets); // 발사 순간의 최대 대상 수
            arrows.Add(new BowArrow(visual, aim, runtimeSettings.staffProjectileSpeed, runtimeSettings.staffRange, attack, obstacleLayer, position => ExplodeStaff(position, attack, radius, maximum))); // 적중 또는 사거리 도달 시 폭발하는 마법탄
        }

        private void ExplodeStaff(Vector2 position, int attack, float radius, int maximum) // 지팡이의 단일 폭발 처리
        {
            ShowStaffExplosion(position, radius); // 임시 폭발 범위 표시
            List<Fighter> candidates = new List<Fighter>(); // 벽에 가려지지 않은 폭발 대상 목록
            HashSet<Fighter> unique = new HashSet<Fighter>(); // 동일 전투 대상 중복 검사
            foreach (Fighter target in enemies) // 현재 적 목록 검사
            {
                if (target == null || target.Dead || !unique.Add(target)) // 사망 또는 중복 대상 확인
                {
                    continue; // 유효하지 않은 대상 제외
                }
                Vector2 offset = (Vector2)target.transform.position - position; // 폭발 중심에서 대상 중심까지 방향
                if (offset.sqrMagnitude > radius * radius) // 대상 중심의 폭발 반경 검사
                {
                    continue; // 폭발 범위 밖 대상 제외
                }
                if (Physics2D.Linecast(position, target.transform.position, swordOcclusionFilter, swordOcclusionHits) != 0) // 폭발 중심과 대상 사이의 벽 검사
                {
                    continue; // 벽에 가려진 대상 제외
                }
                candidates.Add(target); // 피해 가능한 고유 대상 추가
            }
            candidates.Sort((left, right) => // 폭발 중심에서 가까운 대상 정렬
            {
                float leftDistance = ((Vector2)left.transform.position - position).sqrMagnitude; // 왼쪽 대상의 거리 제곱
                float rightDistance = ((Vector2)right.transform.position - position).sqrMagnitude; // 오른쪽 대상의 거리 제곱
                int distanceOrder = leftDistance.CompareTo(rightDistance); // 대상 거리 순서 비교
                return distanceOrder != 0 ? distanceOrder : enemies.IndexOf(left).CompareTo(enemies.IndexOf(right)); // 거리 동률의 생성 순서
            });
            for (int index = 0; index < Mathf.Min(maximum, candidates.Count); index++) // 최대 대상 수만큼 피해 적용
            {
                Fighter target = candidates[index]; // 현재 폭발 피해 대상
                target.TakeDamage(CombatMath.Damage(attack, 1f, target.Defense, false)); // 공통 계산의 지팡이 피해 적용
            }
        }

        private void ShowStaffExplosion(Vector2 position, float radius) // 일시적인 원형 폭발 범위 표시
        {
            ShowCircleEffect(position, radius, "Staff Explosion", new Color(0.5f, 0.8f, 1f)); // 지팡이 폭발 임시 표시
        }

        private void ShowCircleEffect(Vector2 position, float radius, string effectName, Color color) // 임시 원형 범위 표시
        {
            if (explosionSprite == null) // 원형 폭발 그림 생성 여부
            {
                const int resolution = 64; // 임시 폭발 그림 해상도
                explosionTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false); // 임시 투명 질감 생성
                explosionTexture.filterMode = FilterMode.Bilinear; // 폭발 테두리 확대 보간
                Color32[] pixels = new Color32[resolution * resolution]; // 폭발 그림의 색상 배열
                for (int y = 0; y < resolution; y++) // 그림의 세로 좌표 순회
                {
                    for (int x = 0; x < resolution; x++) // 그림의 가로 좌표 순회
                    {
                        float dx = x + 0.5f - resolution * 0.5f; // 그림 중심에서 가로 거리
                        float dy = y + 0.5f - resolution * 0.5f; // 그림 중심에서 세로 거리
                        float distance = Mathf.Sqrt(dx * dx + dy * dy); // 그림 중심에서 원형 거리
                        byte alpha = distance >= 28f && distance <= 31f ? (byte)255 : (byte)0; // 임시 원형 테두리 불투명도
                        pixels[y * resolution + x] = new Color32(255, 255, 255, alpha); // 테두리 색상 저장
                    }
                }
                explosionTexture.SetPixels32(pixels); // 폭발 테두리 픽셀 적용
                explosionTexture.Apply(); // 폭발 질감 업로드
                explosionSprite = Sprite.Create(explosionTexture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution); // 단위 크기의 원형 그림
            }
            GameObject effect = Shape(effectName, position, new Vector2(radius * 2f, radius * 2f), color, 4); // 지정 반경과 색상의 임시 표시
            effect.GetComponent<SpriteRenderer>().sprite = explosionSprite; // 원형 테두리 그림 연결
            explosionEffects.Add(effect); // 전투 종료 시 정리할 표시 보관
            Destroy(effect, 0.15f); // 임시 폭발 표시 지속시간
        }

        private void UpdateExplosionEffects() // 종료된 폭발 표시 목록 정리
        {
            for (int index = explosionEffects.Count - 1; index >= 0; index--) // 제거에 안전한 역순 검사
            {
                if (explosionEffects[index] == null) // 표시 시간 종료 여부
                {
                    explosionEffects.RemoveAt(index); // 종료된 표시 상태 제거
                }
            }
        }

        private void UpdateArrows() // 발사된 화살 이동과 충돌 처리
        {
            if (arrows.Count == 0) // 화살 존재 여부
            {
                return; // 불필요한 충돌 검사 생략
            }
            Physics2D.SyncTransforms(); // 변경된 적과 장애물 위치의 물리 반영
            for (int index = arrows.Count - 1; index >= 0; index--) // 제거에 안전한 역순 화살 검사
            {
                if (arrows[index].Tick(mapRegion, arrowCollisionFilter, arrowHits, arrowOverlaps, Time.deltaTime)) // 화살 이동과 종료 판정
                {
                    arrows.RemoveAt(index); // 종료된 화살 상태 제거
                }
            }
        }

        private void ClearArrows() // 전투 종료 시 화살 정리
        {
            foreach (BowArrow arrow in arrows) // 남은 화살 상태 검사
            {
                arrow.Clear(); // 화살 표시 제거
            }
            arrows.Clear(); // 화살 상태 목록 초기화
            foreach (GameObject effect in explosionEffects) // 남은 폭발 표시 검사
            {
                if (effect != null) // 폭발 표시 존재 여부
                {
                    Destroy(effect); // 이전 전투의 폭발 표시 제거
                }
            }
            explosionEffects.Clear(); // 폭발 표시 목록 초기화
        }

        private void UpdateSlimes() // 슬라임 행동 갱신
        {
            foreach (Slime slime in slimes) // 각 슬라임 행동
            {
                if (slime.Fighter.Dead) // 사망 슬라임 확인
                {
                    continue; // 사망 개체 행동 제외
                }
                slime.Tick(player, runtimeSettings, mapRegion); // 이동과 공격 처리
                if (player.Dead) // 플레이어 사망 확인
                {
                    SetEquipmentButtonsInteractable(false); // 사망 중 무기 버튼 비활성화
                    gameOverPanel.SetActive(true); // 재시작 화면 표시
                    break; // 추가 공격 중지
                }
            }
        }

        private void UpdateGoblins() // 고블린 근접 행동 갱신
        {
            if (!combatLayersReady) // 적 공격의 장애물 계층 확인
            {
                return; // 장애물 판정이 없으면 잘못된 공격 방지
            }
            foreach (Goblin goblin in goblins) // 각 고블린 행동
            {
                if (goblin.Fighter == null || goblin.Fighter.Dead) // 사망하거나 제거된 적 확인
                {
                    continue; // 사망 개체 행동 제외
                }
                goblin.Tick(player, runtimeSettings, mapRegion, swordOcclusionFilter, swordOcclusionHits); // 빠른 접근과 근접 베기 처리
                if (player.Dead) // 고블린 공격으로 플레이어 사망 여부
                {
                    SetEquipmentButtonsInteractable(false); // 사망 중 무기 버튼 비활성화
                    gameOverPanel.SetActive(true); // 재시작 화면 표시
                    break; // 사망 후 추가 공격 중지
                }
            }
        }

        private void UpdateHealth() // 플레이어 체력 표시
        {
            healthText.text = "HP " + player.Health + " / " + runtimeSettings.playerHealth; // 현재 체력 갱신
        }

        private void CreateInterface() // 화면 UI 생성
        {
            GameObject eventObject = new GameObject("EventSystem"); // UI 이벤트 시스템
            eventObject.AddComponent<EventSystem>(); // 이벤트 처리기
            eventObject.AddComponent<InputSystemUIInputModule>(); // 새 입력 시스템 UI 모듈
            GameObject canvasObject = new GameObject("Canvas"); // UI 캔버스 오브젝트
            Canvas canvas = canvasObject.AddComponent<Canvas>(); // 캔버스 구성 요소
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 UI
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); // 화면 크기 보정기
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 기준 크기 조정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            canvasObject.AddComponent<GraphicRaycaster>(); // UI 포인터 검사기
            GameObject healthPanel = Panel("Health Panel", canvasObject.transform, new Vector2(10f, -10f), new Vector2(250f, 70f), new Vector2(0f, 1f)); // 체력 배경과 클릭 차단 영역
            healthText = Label("Health", healthPanel.transform, "HP", 28); // 체력 글자
            UpdateHealth(); // 초기 체력 표시
            CreateEquipmentInterface(canvasObject.transform); // 전투 무기 선택 버튼 준비
            CreateSkillInterface(canvasObject.transform); // 현재 장착과 미구현 상태 표시
            CreateInteractionInterface(canvasObject.transform); // 상호작용과 시험 수집 안내 준비
            gameOverPanel = Panel("Game Over", canvasObject.transform, Vector2.zero, new Vector2(440f, 220f), new Vector2(0.5f, 0.5f)); // 사망 배경
            GameObject title = new GameObject("Title", typeof(RectTransform)); // 사망 글자 오브젝트
            title.transform.SetParent(gameOverPanel.transform, false); // 사망 화면에 연결
            RectTransform titleRect = title.GetComponent<RectTransform>(); // 글자 위치
            titleRect.anchorMin = new Vector2(0f, 0.5f); // 글자 하단 기준
            titleRect.anchorMax = Vector2.one; // 글자 상단 기준
            titleRect.offsetMin = Vector2.zero; // 글자 하단 여백
            titleRect.offsetMax = Vector2.zero; // 글자 상단 여백
            Text titleText = title.AddComponent<Text>(); // 사망 글자
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 글꼴
            titleText.text = "GAME OVER"; // 사망 문구
            titleText.fontSize = 36; // 글자 크기
            titleText.alignment = TextAnchor.MiddleCenter; // 가운데 정렬
            titleText.raycastTarget = false; // 글자 포인터 검사 제외
            GameObject buttonObject = Panel("Restart", gameOverPanel.transform, new Vector2(0f, -45f), new Vector2(220f, 60f), new Vector2(0.5f, 0.5f)); // 재시작 버튼 배경
            buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.65f, 1f); // 버튼 색상
            Button button = buttonObject.AddComponent<Button>(); // 버튼 동작
            button.onClick.AddListener(Restart); // 재시작 연결
            Label("Button Label", buttonObject.transform, "RESTART", 26); // 버튼 글자
            gameOverPanel.SetActive(false); // 시작 시 사망 화면 숨김
        }

        private void CreateInteractionInterface(Transform canvas) // 상호작용 안내와 클릭 차단 영역
        {
            GameObject panel = new GameObject("Interaction Panel", typeof(RectTransform)); // 상호작용 안내 영역
            panel.transform.SetParent(canvas, false); // 화면 캔버스 연결
            RectTransform rect = panel.GetComponent<RectTransform>(); // 안내 영역 위치
            rect.anchorMin = new Vector2(1f, 1f); // 화면 오른쪽 위 기준점
            rect.anchorMax = new Vector2(1f, 1f); // 화면 오른쪽 위 기준점
            rect.pivot = new Vector2(1f, 1f); // 안내 영역 오른쪽 위 기준
            rect.anchoredPosition = new Vector2(-10f, -10f); // 화면 가장자리 여백
            rect.sizeDelta = new Vector2(500f, 72f); // 대상과 수집 안내 크기
            interactionStatusText = Label("Interaction Status", panel.transform, "", 20); // 상호작용 안내 글자
            interactionStatusText.raycastTarget = true; // 안내 영역의 전투 클릭 차단
            UpdateInteractionStatus(); // 초기 대상과 수집 횟수 표시
        }

        private static void AddSkillError(List<string> errors, string slot, SkillSlotError error) // 잘못된 슬롯의 오류 수집
        {
            if (error != SkillSlotError.None) // 슬롯 오류 여부
            {
                errors.Add(slot + " " + error.ToString().ToUpperInvariant()); // 슬롯과 오류 종류 기록
            }
        }

        private static string SkillSlotLabel(string slot, SkillId id, SkillSlotError error) // 슬롯별 HUD 문구
        {
            if (error != SkillSlotError.None) // 비활성 슬롯 여부
            {
                return slot + ": " + error.ToString().ToUpperInvariant(); // 오류 종류 표시
            }
            SkillCatalog.TryGet(id, out SkillDefinition definition); // 유효한 스킬 정의 조회
            return slot + ": " + definition.EnglishName; // 임시 영문 스킬 표시
        }

        private void CreateSkillInterface(Transform canvas) // 스킬 상태와 클릭 차단 영역 생성
        {
            GameObject panel = new GameObject("Skill Panel", typeof(RectTransform)); // 스킬 상태 표시 영역
            panel.transform.SetParent(canvas, false); // 화면 캔버스 연결
            RectTransform rect = panel.GetComponent<RectTransform>(); // 표시 영역 위치
            rect.anchorMin = new Vector2(0.5f, 0f); // 화면 아래쪽 기준점
            rect.anchorMax = new Vector2(0.5f, 0f); // 화면 아래쪽 기준점
            rect.pivot = new Vector2(0.5f, 0.5f); // 표시 영역 중심
            rect.anchoredPosition = new Vector2(0f, 132f); // 무기 버튼 위쪽 위치
            rect.sizeDelta = new Vector2(1400f, 64f); // 슬롯과 안내 크기
            skillStatusText = Label("Skill Status", panel.transform, "", 20); // 스킬 상태 글자
            skillStatusText.raycastTarget = true; // 상태 표시 영역의 전투 클릭 차단
            UpdateSkillStatus(); // 시작 마나와 슬롯 표시
        }

        private void UpdateSkillStatus() // 마나와 슬롯별 상태 갱신
        {
            string slots = SkillDisplay("Q", EquippedSkills.Q, EquippedSkills.QError, 0) + " | " + SkillDisplay("E", EquippedSkills.E, EquippedSkills.EError, 1) + " | " + SkillDisplay("R", EquippedSkills.R, EquippedSkills.RError, 2); // 슬롯과 대기시간 표시
            skillStatusText.text = "MP " + Mathf.FloorToInt(SkillState.Mana) + " / " + Mathf.FloorToInt(SkillState.MaximumMana) + "    ST " + Mathf.FloorToInt(DodgeState.Stamina) + " / " + Mathf.FloorToInt(DodgeState.MaximumStamina) + "\n" + slots; // 마나와 스태미나 및 스킬 상태
        }

        private string SkillDisplay(string slot, SkillId skill, SkillSlotError error, int index) // 슬롯별 스킬과 대기시간
        {
            string label = SkillSlotLabel(slot, skill, error); // 스킬 이름 또는 슬롯 오류
            if (error != SkillSlotError.None) // 잘못된 슬롯 여부
            {
                return label; // 오류 표시 유지
            }
            if (skill != SkillId.Fireball && skill != SkillId.WhirlwindSlash && skill != SkillId.HealingLight) // 미구현 스킬 여부
            {
                return label + " (NOT IMPLEMENTED)"; // 후보별 미구현 안내
            }
            float remaining = SkillState.RemainingCooldown(index); // 남은 슬롯 대기시간
            return remaining > 0f ? label + " (" + Mathf.CeilToInt(remaining) + "s)" : label + " (READY)"; // 시전 가능 여부 표시
        }

        private void CreateEquipmentInterface(Transform canvas) // 전투 중 시험 무기 선택 UI 생성
        {
            GameObject panel = new GameObject("Weapon Panel", typeof(RectTransform)); // 입력을 막지 않는 버튼 배치 영역
            panel.transform.SetParent(canvas, false); // 화면 캔버스에 연결
            RectTransform rect = panel.GetComponent<RectTransform>(); // 버튼 영역 위치
            rect.anchorMin = new Vector2(0.5f, 0f); // 화면 아래쪽 기준점
            rect.anchorMax = new Vector2(0.5f, 0f); // 화면 아래쪽 기준점 고정
            rect.pivot = new Vector2(0.5f, 0.5f); // 버튼 영역 중심 기준
            rect.anchoredPosition = new Vector2(0f, 48f); // 화면 아래 여백
            rect.sizeDelta = new Vector2(480f, 72f); // 버튼 영역 크기
            CreateEquipmentButton(panel.transform, "Equip Sword", WeaponType.Sword, -156f); // 검 장착 버튼
            CreateEquipmentButton(panel.transform, "Equip Bow", WeaponType.Bow, 0f); // 활 장착 버튼
            CreateEquipmentButton(panel.transform, "Equip Staff", WeaponType.Staff, 156f); // 지팡이 장착 버튼
            RefreshEquipmentLabels(); // 시작 무기의 선택 표시
        }

        private void CreateEquipmentButton(Transform parent, string name, WeaponType weapon, float x) // 무기 선택 버튼 생성
        {
            GameObject buttonObject = Panel(name, parent, new Vector2(x, 0f), new Vector2(144f, 54f), new Vector2(0.5f, 0.5f)); // 개별 무기 버튼 배경
            buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.42f, 1f); // 무기 버튼 색상
            Button button = buttonObject.AddComponent<Button>(); // 무기 선택 버튼 동작
            button.onClick.AddListener(() => EquipWeapon(weapon)); // 해당 무기 장착 연결
            Text label = Label("Weapon Label", buttonObject.transform, "", 20); // 버튼의 무기 이름과 선택 표시
            equipmentLabels.Add(weapon, label); // 무기별 표시 참조 보관
            equipmentButtons.Add(button); // 사망 시 비활성화할 버튼 보관
        }

        private void EquipWeapon(WeaponType weapon) // 전투 중 무기 장착 상태 변경
        {
            if (player == null || player.Dead) // 전투 가능 여부 확인
            {
                return; // 사망 중 무기 전환 방지
            }
            equippedWeapon = weapon; // 선택한 무기를 실행 상태에 저장
            RefreshEquipmentLabels(); // 현재 선택 표시 갱신
        }

        private void RefreshEquipmentLabels() // 현재 장착 무기 표시 갱신
        {
            foreach (KeyValuePair<WeaponType, Text> entry in equipmentLabels) // 무기별 버튼 표시 순회
            {
                string name = entry.Key == WeaponType.Bow ? "BOW" : entry.Key == WeaponType.Staff ? "STAFF" : "SWORD"; // 무기별 표시 이름
                entry.Value.text = name + (entry.Key == equippedWeapon ? " *" : ""); // 현재 무기에 선택 표식 추가
            }
        }

        private void SetEquipmentButtonsInteractable(bool value) // 무기 선택 가능 상태 변경
        {
            foreach (Button button in equipmentButtons) // 무기 버튼 순회
            {
                if (button != null) // 버튼 존재 여부
                {
                    button.interactable = value; // 무기 선택 가능 상태 적용
                }
            }
        }

        private static GameObject Panel(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor) // UI 배경 생성
        {
            GameObject panel = new GameObject(name, typeof(RectTransform)); // UI 배경 오브젝트
            panel.transform.SetParent(parent, false); // 부모 캔버스 연결
            RectTransform rect = panel.GetComponent<RectTransform>(); // 위치 구성 요소
            rect.anchorMin = anchor; // 기준점 최소값
            rect.anchorMax = anchor; // 기준점 최대값
            rect.pivot = anchor == new Vector2(0f, 1f) ? anchor : new Vector2(0.5f, 0.5f); // 화면 모서리 기준 설정
            rect.anchoredPosition = position; // 기준점 상대 위치
            rect.sizeDelta = size; // 배경 크기
            Image image = panel.AddComponent<Image>(); // 배경 그림
            image.color = new Color(0.05f, 0.06f, 0.08f, 0.9f); // 배경 색상
            return panel; // 생성 배경
        }

        private static Text Label(string name, Transform parent, string value, int size) // UI 글자 생성
        {
            GameObject label = new GameObject(name, typeof(RectTransform)); // 글자 오브젝트
            label.transform.SetParent(parent, false); // 부모 UI 연결
            RectTransform rect = label.GetComponent<RectTransform>(); // 글자 위치 구성 요소
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 왼쪽 아래 여백
            rect.offsetMax = Vector2.zero; // 오른쪽 위 여백
            Text text = label.AddComponent<Text>(); // 글자 표시기
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 기본 글꼴
            text.text = value; // 표시 내용
            text.fontSize = size; // 글자 크기
            text.alignment = TextAnchor.MiddleCenter; // 가운데 정렬
            text.raycastTarget = false; // 글자 포인터 검사 제외
            return text; // 생성 글자
        }

        private static void Restart() // 전투 재시작
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 재로드
        }
    }

    public sealed class Fighter : MonoBehaviour // 전투 대상의 실행 상태
    {
        public event System.Action<Fighter> Died; // 생존에서 사망으로 바뀐 순간 알림
        public int Health { get; private set; } // 현재 체력
        public int MaximumHealth // 최대 체력
        {
            get // 최대 체력 읽기
            {
                return maximumHealth; // 최대 체력 반환
            }
        }
        public int Defense { get; private set; } // 방어력
        public bool Dead { get { return Health <= 0; } } // 사망 여부
        public bool IsInvulnerable // 회피 무적 여부
        {
            get; // 무적 상태 읽기
            private set; // 무적 상태 내부 설정
        }
        private int maximumHealth; // 최대 체력
        private SpriteRenderer body; // 대상 도형
        private SpriteRenderer healthBar; // 머리 위 체력 막대
        private Color bodyColor; // 기본 도형 색상
        private float flashUntil; // 피격 표시 종료 시각
        private GameObject invulnerabilityVisual; // 기존 색상과 독립적인 무적 표시

        public void Initialize(int health, int defense) // 전투 상태 초기화
        {
            maximumHealth = Mathf.Max(1, health); // 최대 체력
            Health = maximumHealth; // 초기 체력
            Defense = defense; // 방어력
            body = GetComponent<SpriteRenderer>(); // 대상 도형 연결
            bodyColor = body.color; // 기본 색상 보관
            GameObject bar = new GameObject("Health Bar"); // 체력 막대 오브젝트
            bar.transform.SetParent(transform, false); // 대상 위치에 막대 연결
            bar.transform.localPosition = new Vector3(0f, 0.38f, 0f); // 본체 상단 안쪽 막대 위치
            bar.transform.localScale = new Vector3(1f, 0.12f, 1f); // 초기 막대 크기
            healthBar = bar.AddComponent<SpriteRenderer>(); // 막대 표시기
            healthBar.sprite = body.sprite; // 임시 사각형 그림 공유
            healthBar.color = new Color(0.95f, 0.25f, 0.25f); // 막대 색상
            healthBar.sortingOrder = body.sortingOrder + 1; // 대상 앞쪽 표시
        }

        private void Update() // 피격 표시 갱신
        {
            if (body != null && !Dead && flashUntil > 0f && Time.time >= flashUntil) // 피격 표시 종료 검사
            {
                body.color = bodyColor; // 기본 색상 복원
                flashUntil = 0f; // 피격 표시 종료
            }
        }

        public void TakeDamage(int damage) // 피해 적용
        {
            if (Dead || IsInvulnerable) // 사망 또는 무적 상태 확인
            {
                return; // 사망 대상 중복 피해와 무적 피해 방지
            }
            Health = Mathf.Max(0, Health - Mathf.Max(0, damage)); // 체력 감소
            RefreshHealthBar(); // 체력 막대 갱신
            if (Dead) // 사망 처리
            {
                body.color = new Color(0.25f, 0.25f, 0.25f, 0.45f); // 사망 도형 표시
                healthBar.enabled = false; // 사망 대상 체력 막대 숨김
                Died?.Invoke(this); // 사망 전이의 단일 알림
            }
            else // 생존 대상 피격 처리
            {
                body.color = Color.white; // 피격 순간 색상 변화
                flashUntil = Time.time + 0.15f; // 피격 표시 시간
            }
        }

        public void SetInvulnerable(bool value) // 무적 상태와 별도 표시 변경
        {
            IsInvulnerable = value && !Dead; // 사망 대상의 무적 방지
            if (IsInvulnerable && invulnerabilityVisual == null) // 첫 무적 표시 생성 여부
            {
                invulnerabilityVisual = new GameObject("Dodge Invulnerability"); // 무적 표시 오브젝트
                invulnerabilityVisual.transform.SetParent(transform, false); // 플레이어 위치에 표시 연결
                invulnerabilityVisual.transform.localScale = new Vector3(1.3f, 1.3f, 1f); // 본체 주변 표시 크기
                SpriteRenderer outline = invulnerabilityVisual.AddComponent<SpriteRenderer>(); // 무적 표시 렌더러
                outline.sprite = body.sprite; // 기존 사각 도형 공유
                outline.color = new Color(0.5f, 0.85f, 1f, 0.45f); // 임시 반투명 무적 표시
                outline.sortingOrder = body.sortingOrder - 1; // 본체 뒤에 무적 표시
            }
            if (invulnerabilityVisual != null) // 생성된 표시 존재 여부
            {
                invulnerabilityVisual.SetActive(IsInvulnerable); // 현재 무적 상태 반영
            }
        }

        public int Heal(int amount) // 살아 있는 대상의 체력 회복
        {
            if (Dead || amount <= 0) // 사망 또는 유효하지 않은 회복량 확인
            {
                return 0; // 사망 부활과 음수 회복 방지
            }
            int restored = Mathf.Min(amount, maximumHealth - Health); // 최대 체력 이내 회복량
            Health += restored; // 현재 체력 회복
            RefreshHealthBar(); // 회복된 체력 막대 갱신
            return restored; // 실제 회복량 반환
        }

        private void RefreshHealthBar() // 현재 체력에 맞춘 막대 표시
        {
            float ratio = (float)Health / maximumHealth; // 남은 체력 비율
            healthBar.transform.localScale = new Vector3(ratio, 0.12f, 1f); // 체력 막대 길이 갱신
            healthBar.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0.38f, 0f); // 막대 왼쪽 기준 정렬
        }
    }

    internal sealed class MapRegion // 씬 맵의 바닥과 경계 정보
    {
        private readonly SpriteRenderer floor; // 씬 바닥 렌더러
        private readonly SpriteRenderer top; // 위쪽 경계 렌더러
        private readonly SpriteRenderer bottom; // 아래쪽 경계 렌더러
        private readonly SpriteRenderer left; // 왼쪽 경계 렌더러
        private readonly SpriteRenderer right; // 오른쪽 경계 렌더러
        public Bounds FloorBounds { get { return floor.bounds; } } // 바닥의 월드 좌표 범위
        public Rect Interior { get { return Rect.MinMaxRect(left.bounds.max.x, bottom.bounds.max.y, right.bounds.min.x, top.bounds.min.y); } } // 사방 벽의 안쪽 면

        public MapRegion(SpriteRenderer floor, SpriteRenderer top, SpriteRenderer bottom, SpriteRenderer left, SpriteRenderer right) // 씬 맵 참조 연결
        {
            this.floor = floor; // 바닥 렌더러 보관
            this.top = top; // 위쪽 경계 렌더러 보관
            this.bottom = bottom; // 아래쪽 경계 렌더러 보관
            this.left = left; // 왼쪽 경계 렌더러 보관
            this.right = right; // 오른쪽 경계 렌더러 보관
        }

        public bool IsValid() // 씬 맵의 축 정렬과 경계 배치 확인
        {
            if (!HasPositiveAlignedAxes(floor.transform) || !HasPositiveAlignedAxes(top.transform) || !HasPositiveAlignedAxes(bottom.transform) || !HasPositiveAlignedAxes(left.transform) || !HasPositiveAlignedAxes(right.transform)) // 바닥과 각 벽의 회전 또는 뒤집힘 확인
            {
                return false; // 축 정렬된 양수 크기 필요
            }
            if (!floor.enabled || !top.enabled || !bottom.enabled || !left.enabled || !right.enabled || !floor.gameObject.activeInHierarchy || !top.gameObject.activeInHierarchy || !bottom.gameObject.activeInHierarchy || !left.gameObject.activeInHierarchy || !right.gameObject.activeInHierarchy) // 비활성 맵 구성 요소 확인
            {
                return false; // 표시되지 않는 맵 구성 차단
            }
            Bounds floorBounds = FloorBounds; // 바닥의 월드 좌표 범위
            Rect area = Interior; // 사방 경계의 안쪽 영역
            float minimumSize = PrototypeSettings.SlimeRadius * 2f; // 가장 큰 적 도형의 최소 공간
            if (area.width < minimumSize || area.height < minimumSize) // 도형을 담을 수 없는 공간 확인
            {
                return false; // 너무 작은 맵 차단
            }
            if (area.xMin < floorBounds.min.x || area.xMax > floorBounds.max.x || area.yMin < floorBounds.min.y || area.yMax > floorBounds.max.y) // 벽이 바닥 밖에 있는지 확인
            {
                return false; // 바닥과 경계 불일치 차단
            }
            if (top.bounds.min.x > area.xMin || top.bounds.max.x < area.xMax || bottom.bounds.min.x > area.xMin || bottom.bounds.max.x < area.xMax || left.bounds.min.y > area.yMin || left.bounds.max.y < area.yMax || right.bounds.min.y > area.yMin || right.bounds.max.y < area.yMax) // 벽이 사방을 덮는지 확인
            {
                return false; // 틈이 있는 경계 차단
            }
            return true; // 사용 가능한 씬 맵
        }

        private static bool HasPositiveAlignedAxes(Transform part) // 구성 요소의 양수 축 정렬 확인
        {
            Vector3 axisX = part.localToWorldMatrix.MultiplyVector(Vector3.right); // 구성 요소의 월드 가로축
            Vector3 axisY = part.localToWorldMatrix.MultiplyVector(Vector3.up); // 구성 요소의 월드 세로축
            return axisX.x > 0f && axisY.y > 0f && Mathf.Abs(axisX.y) <= 0.0001f && Mathf.Abs(axisY.x) <= 0.0001f && Mathf.Abs(axisX.z) <= 0.0001f && Mathf.Abs(axisY.z) <= 0.0001f; // 양수 크기와 축 정렬 여부
        }
        public Vector2 ClampPosition(Vector2 position, float radius) // 도형 전체의 벽 안쪽 위치 계산
        {
            Rect area = Interior; // 실제 벽의 안쪽 영역
            float margin = Mathf.Max(0f, radius); // 대상 도형 반폭
            float minX = Mathf.Min(area.center.x, area.xMin + margin); // 가로 최소 중심 위치
            float maxX = Mathf.Max(area.center.x, area.xMax - margin); // 가로 최대 중심 위치
            float minY = Mathf.Min(area.center.y, area.yMin + margin); // 세로 최소 중심 위치
            float maxY = Mathf.Max(area.center.y, area.yMax - margin); // 세로 최대 중심 위치
            return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY)); // 씬 벽 기준 위치 제한
        }
    }
    internal sealed class Slime // 슬라임 행동 상태
    {
        public Fighter Fighter { get; private set; } // 슬라임 전투 상태
        private float nextAttack; // 다음 공격 가능 시각
        private float windupUntil; // 공격 예고 종료 시각
        private float dashRemaining; // 남은 돌진 거리
        private Vector2 dashDirection; // 고정 돌진 방향
        private bool hitDuringDash; // 돌진 중 중복 피해 방지

        public Slime(Fighter fighter) // 슬라임 상태 생성
        {
            Fighter = fighter; // 전투 상태 저장
        }

        public void Tick(Fighter player, PrototypeSettings settings, MapRegion mapRegion) // 슬라임 행동 갱신
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지 방향
            if (dashRemaining > 0f) // 돌진 중 확인
            {
                float step = Mathf.Min(dashRemaining, Mathf.Max(0.01f, settings.slimeDashSpeed) * Time.deltaTime); // 돌진 이동량
                Vector2 start = Fighter.transform.position; // 돌진 이동 전 위치
                Vector2 end = mapRegion.ClampPosition(start + dashDirection * step, PrototypeSettings.SlimeRadius); // 경계 안의 돌진 도착 위치
                Vector2 movement = end - start; // 실제 돌진 이동 벡터
                Fighter.transform.position = end; // 경계 안의 돌진 이동
                dashRemaining -= step; // 남은 돌진 거리 갱신
                float fraction = movement.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)player.transform.position - start, movement) / movement.sqrMagnitude) : 0f; // 이동 경로상 최근접 위치 비율
                Vector2 closest = start + movement * fraction; // 플레이어와 가장 가까운 경로 지점
                if (!hitDuringDash && Vector2.Distance(closest, player.transform.position) <= settings.slimeHitDistance) // 돌진 이동 경로 적중 검사
                {
                    player.TakeDamage(CombatMath.Damage(settings.slimeAttack, 1f, player.Defense, false)); // 플레이어 피해 적용
                    hitDuringDash = true; // 중복 피해 방지
                }
                return; // 돌진 중 다른 행동 중지
            }
            if (windupUntil > 0f) // 예고 중 확인
            {
                if (Time.time >= windupUntil) // 예고 종료 확인
                {
                    windupUntil = 0f; // 예고 종료
                    Fighter.transform.localScale = new Vector3(0.65f, 0.65f, 1f); // 원래 크기 복원
                    dashRemaining = Mathf.Max(0f, settings.slimeDashDistance); // 돌진 거리
                    hitDuringDash = false; // 돌진 적중 상태 초기화
                }
                return; // 예고 중 이동 중지
            }
            if (offset.magnitude <= settings.slimeAttackRange && Time.time >= nextAttack) // 공격 거리와 간격 검사
            {
                dashDirection = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.right; // 돌진 방향 고정
                windupUntil = Time.time + Mathf.Max(0.01f, settings.slimeWindup); // 예고 종료 시각
                nextAttack = Time.time + Mathf.Max(settings.slimeAttackInterval, settings.slimeWindup); // 다음 공격 시각
                Fighter.transform.localScale = new Vector3(0.85f, 0.85f, 1f); // 몸을 부풀리는 예고
                return; // 이번 프레임 이동 중지
            }
            if (offset.magnitude > settings.slimeAttackRange * 0.8f) // 적정 거리 밖 확인
            {
                Vector2 movement = offset.normalized * Mathf.Min(settings.slimeSpeed * Time.deltaTime, offset.magnitude); // 플레이어 방향 접근 이동량
                Fighter.transform.position = mapRegion.ClampPosition((Vector2)Fighter.transform.position + movement, PrototypeSettings.SlimeRadius); // 경계 안의 접근 위치
            }
        }
    }
}











