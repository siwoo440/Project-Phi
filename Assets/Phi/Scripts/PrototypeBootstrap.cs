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
        private PrototypeSettings runtimeSettings; // 현재 전투 설정
        private Fighter player; // 플레이어 전투 상태
        private readonly List<Slime> slimes = new List<Slime>(); // 슬라임 목록
        private Camera gameCamera; // 추적 카메라
        private GameObject aimMarker; // 조준 표시
        private Text healthText; // 체력 글자
        private GameObject gameOverPanel; // 사망 화면
        private float nextAttack; // 다음 공격 가능 시각
        private bool blockedPress; // UI에서 시작한 클릭 차단
        private bool attackPressed; // 이번 입력 처리의 클릭 시작
        private bool pressActive; // 유효한 클릭 유지
        private InputAction attackAction; // 클릭 시작 이벤트
        private Sprite square; // 임시 사각형 그림
        private Vector2 aim = Vector2.right; // 마지막 조준 방향

        private void Start() // 전투 시작
        {
            runtimeSettings = settings != null ? settings : ScriptableObject.CreateInstance<PrototypeSettings>(); // 설정이 없을 때 기본값 사용
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f); // 임시 도형 생성
            CreateCamera(); // 카메라 준비
            CreateWorld(); // 플레이어와 적 준비
            CreateInterface(); // 체력과 재시작 화면 준비
            attackAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton"); // 클릭 이벤트 구성
            attackAction.performed += OnAttackPressed; // 클릭 시작 콜백 연결
            attackAction.Enable(); // 클릭 이벤트 활성화
        }

        private void Update() // 매 프레임 입력과 전투 처리
        {
            if (player == null || player.Dead) // 플레이어 사망 여부
            {
                return; // 사망 중 전투 입력 중지
            }
            ReadMovement(); // 이동 입력
            FollowCamera(); // 이동 직후 카메라 위치 반영
            ReadAimAndAttack(); // 조준과 공격 입력
            UpdateSlimes(); // 슬라임 행동
            UpdateHealth(); // 체력 표시
        }

        private void LateUpdate() // 이동 후 카메라 추적
        {
            FollowCamera(); // 최종 카메라 위치 반영
        }

        private void FollowCamera() // 플레이어 위치에 카메라 정렬
        {
            if (player != null && gameCamera != null) // 추적 대상 확인
            {
                float width = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfWidth); // 시험 공간 가로 반경
                float height = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfHeight); // 시험 공간 세로 반경
                float aspect = Mathf.Max(0.01f, gameCamera.aspect); // 유효한 화면 가로세로 비율
                gameCamera.orthographicSize = Mathf.Min(6f, height, width / aspect); // 맵을 벗어나지 않는 세로 시야
                float halfViewWidth = gameCamera.orthographicSize * aspect; // 카메라 가로 시야 반경
                float limitX = Mathf.Max(0f, width - halfViewWidth); // 카메라 가로 중심 한계
                float limitY = Mathf.Max(0f, height - gameCamera.orthographicSize); // 카메라 세로 중심 한계
                Vector3 position = player.transform.position; // 플레이어 위치
                gameCamera.transform.position = new Vector3(Mathf.Clamp(position.x, -limitX, limitX), Mathf.Clamp(position.y, -limitY, limitY), -10f); // 맵 내부 카메라 추적
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
            CreateBoundary(); // 시험 공간 경계 표시
            GameObject playerObject = Shape("Player", Vector2.zero, new Vector2(0.7f, 0.7f), new Color(0.3f, 0.65f, 1f), 2); // 플레이어 임시 도형
            player = playerObject.AddComponent<Fighter>(); // 플레이어 체력 상태
            player.Initialize(runtimeSettings.playerHealth, runtimeSettings.playerDefense); // 플레이어 능력치 설정
            aimMarker = Shape("Aim", Vector2.zero, new Vector2(0.8f, 0.12f), Color.white, 3); // 조준 방향 표시
            for (int index = 0; index < Mathf.Max(0, runtimeSettings.slimeCount); index++) // 시험 슬라임 생성 반복
            {
                float angle = index * Mathf.PI * 2f / Mathf.Max(1, runtimeSettings.slimeCount); // 배치 각도
                Vector2 position = runtimeSettings.ClampPosition(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f, PrototypeSettings.SlimeRadius); // 경계 안의 배치 위치
                GameObject slimeObject = Shape("Slime " + (index + 1), position, new Vector2(0.65f, 0.65f), new Color(0.35f, 0.9f, 0.35f), 1); // 슬라임 도형
                Fighter fighter = slimeObject.AddComponent<Fighter>(); // 슬라임 체력 상태
                fighter.Initialize(runtimeSettings.slimeHealth, runtimeSettings.slimeDefense); // 슬라임 능력치 설정
                slimes.Add(new Slime(fighter)); // 슬라임 행동 상태 추가
            }
        }

        private void CreateBoundary() // 시험 공간 테두리 생성
        {
            float width = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfWidth); // 가로 경계 반경
            float height = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfHeight); // 세로 경계 반경
            float thickness = PrototypeSettings.BoundaryThickness; // 임시 테두리 두께
            Color color = new Color(0.65f, 0.74f, 0.68f); // 임시 테두리 색상
            Shape("Boundary Top", new Vector2(0f, height - thickness * 0.5f), new Vector2(width * 2f, thickness), color, 0); // 위쪽 경계 표시
            Shape("Boundary Bottom", new Vector2(0f, -height + thickness * 0.5f), new Vector2(width * 2f, thickness), color, 0); // 아래쪽 경계 표시
            Shape("Boundary Left", new Vector2(-width + thickness * 0.5f, 0f), new Vector2(thickness, height * 2f), color, 0); // 왼쪽 경계 표시
            Shape("Boundary Right", new Vector2(width - thickness * 0.5f, 0f), new Vector2(thickness, height * 2f), color, 0); // 오른쪽 경계 표시
        }

        private void ReadMovement() // 키보드 이동
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            if (keyboard == null) // 키보드 부재 확인
            {
                return; // 이동 입력 없음
            }
            Vector2 input = new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f), (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)); // WASD 방향
            Vector2 movement = CombatMath.NormalizeMovement(input) * runtimeSettings.playerSpeed * Time.deltaTime; // 정규화 이동량
            player.transform.position = runtimeSettings.ClampPosition((Vector2)player.transform.position + movement, PrototypeSettings.PlayerRadius); // 경계 안의 플레이어 위치 변경
        }

        private void ReadAimAndAttack() // 마우스 조준과 공격
        {
            Mouse mouse = Mouse.current; // 현재 마우스
            if (mouse == null) // 마우스 부재 확인
            {
                return; // 마우스 입력 없음
            }
            Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, -gameCamera.transform.position.z)); // 커서의 월드 좌표
            Vector2 direction = (Vector2)(mouseWorld - player.transform.position); // 플레이어에서 커서 방향
            if (direction.sqrMagnitude > 0.0001f) // 유효한 조준 방향
            {
                aim = direction.normalized; // 조준 방향 저장
            }
            UpdateAimMarker(); // 경계 안의 조준 표시 갱신
            if ((attackPressed || (pressActive && mouse.leftButton.isPressed)) && !blockedPress && !PointerOverUi(mouse.position.ReadValue()) && Time.time >= nextAttack) // 짧은 클릭과 홀드 공격 검사
            {
                Attack(); // 검 공격
                nextAttack = Time.time + Mathf.Max(0.01f, runtimeSettings.swordInterval); // 다음 공격 가능 시각
            }
            attackPressed = false; // 이번 프레임의 클릭 시작 처리 완료
            if (!mouse.leftButton.isPressed) // 클릭 종료 여부
            {
                blockedPress = false; // 클릭 차단 해제
                pressActive = false; // 클릭 유지 상태 해제
            }
        }

        private void UpdateAimMarker() // 조준 표시의 경계 제한
        {
            Vector2 origin = player.transform.position; // 플레이어 중심 좌표
            float markerWidth = 0.12f; // 조준 표시 두께
            float mapWidth = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfWidth); // 공간 가로 반경
            float mapHeight = Mathf.Max(PrototypeSettings.MinimumMapHalfSize, runtimeSettings.mapHalfHeight); // 공간 세로 반경
            float limitX = Mathf.Max(0f, mapWidth - PrototypeSettings.BoundaryThickness - markerWidth * 0.5f * Mathf.Abs(aim.y)); // 두께를 포함한 가로 한계
            float limitY = Mathf.Max(0f, mapHeight - PrototypeSettings.BoundaryThickness - markerWidth * 0.5f * Mathf.Abs(aim.x)); // 두께를 포함한 세로 한계
            float length = 1.15f; // 기본 조준 표시 길이
            if (aim.x > 0.0001f) // 오른쪽 조준 여부
            {
                length = Mathf.Min(length, (limitX - origin.x) / aim.x); // 오른쪽 경계까지의 길이
            }
            if (aim.x < -0.0001f) // 왼쪽 조준 여부
            {
                length = Mathf.Min(length, (-limitX - origin.x) / aim.x); // 왼쪽 경계까지의 길이
            }
            if (aim.y > 0.0001f) // 위쪽 조준 여부
            {
                length = Mathf.Min(length, (limitY - origin.y) / aim.y); // 위쪽 경계까지의 길이
            }
            if (aim.y < -0.0001f) // 아래쪽 조준 여부
            {
                length = Mathf.Min(length, (-limitY - origin.y) / aim.y); // 아래쪽 경계까지의 길이
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
            blockedPress = PointerOverUi(mouse.position.ReadValue()); // 입력 이벤트 시점의 UI 시작 위치 기록
        }

        private void OnDestroy() // 입력 이벤트 정리
        {
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

        private void Attack() // 검 부채꼴 공격
        {
            List<Fighter> targets = new List<Fighter>(); // 이번 공격의 대상 목록
            foreach (Slime slime in slimes) // 각 슬라임 검사
            {
                if (!slime.Fighter.Dead && CombatMath.InSwordSector(player.transform.position, aim, slime.Fighter.transform.position, runtimeSettings.swordRange, runtimeSettings.swordAngle)) // 생존과 부채꼴 판정
                {
                    targets.Add(slime.Fighter); // 고유 대상 추가
                }
            }
            targets.Sort((left, right) => ((Vector2)(left.transform.position - player.transform.position)).sqrMagnitude.CompareTo(((Vector2)(right.transform.position - player.transform.position)).sqrMagnitude)); // 가까운 대상 우선
            for (int index = 0; index < Mathf.Min(targets.Count, Mathf.Max(0, runtimeSettings.swordTargets)); index++) // 최대 대상 수 제한
            {
                Fighter target = targets[index]; // 현재 대상
                target.TakeDamage(CombatMath.Damage(runtimeSettings.playerAttack + runtimeSettings.swordAttack, 1f, target.Defense, false)); // 공통 계산으로 피해 적용
            }
        }

        private void UpdateSlimes() // 슬라임 행동 갱신
        {
            foreach (Slime slime in slimes) // 각 슬라임 행동
            {
                if (slime.Fighter.Dead) // 사망 슬라임 확인
                {
                    continue; // 사망 개체 행동 제외
                }
                slime.Tick(player, runtimeSettings); // 이동과 공격 처리
                if (player.Dead) // 플레이어 사망 확인
                {
                    gameOverPanel.SetActive(true); // 재시작 화면 표시
                    break; // 추가 공격 중지
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
        public int Health { get; private set; } // 현재 체력
        public int Defense { get; private set; } // 방어력
        public bool Dead { get { return Health <= 0; } } // 사망 여부
        private int maximumHealth; // 최대 체력
        private SpriteRenderer body; // 대상 도형
        private SpriteRenderer healthBar; // 머리 위 체력 막대
        private Color bodyColor; // 기본 도형 색상
        private float flashUntil; // 피격 표시 종료 시각

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
            if (Dead) // 사망 상태 확인
            {
                return; // 사망 대상 중복 피해 방지
            }
            Health = Mathf.Max(0, Health - Mathf.Max(0, damage)); // 체력 감소
            float ratio = (float)Health / maximumHealth; // 남은 체력 비율
            healthBar.transform.localScale = new Vector3(ratio, 0.12f, 1f); // 체력 막대 길이 갱신
            healthBar.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0.38f, 0f); // 막대 왼쪽 기준 정렬
            if (Dead) // 사망 처리
            {
                body.color = new Color(0.25f, 0.25f, 0.25f, 0.45f); // 사망 도형 표시
                healthBar.enabled = false; // 사망 대상 체력 막대 숨김
            }
            else // 생존 대상 피격 처리
            {
                body.color = Color.white; // 피격 순간 색상 변화
                flashUntil = Time.time + 0.15f; // 피격 표시 시간
            }
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

        public void Tick(Fighter player, PrototypeSettings settings) // 슬라임 행동 갱신
        {
            Vector2 offset = player.transform.position - Fighter.transform.position; // 플레이어까지 방향
            if (dashRemaining > 0f) // 돌진 중 확인
            {
                float step = Mathf.Min(dashRemaining, Mathf.Max(0.01f, settings.slimeDashSpeed) * Time.deltaTime); // 돌진 이동량
                Vector2 start = Fighter.transform.position; // 돌진 이동 전 위치
                Vector2 end = settings.ClampPosition(start + dashDirection * step, PrototypeSettings.SlimeRadius); // 경계 안의 돌진 도착 위치
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
                Fighter.transform.position = settings.ClampPosition((Vector2)Fighter.transform.position + movement, PrototypeSettings.SlimeRadius); // 경계 안의 접근 위치
            }
        }
    }
}








