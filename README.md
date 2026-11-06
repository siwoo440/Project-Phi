---
# 프로젝트 π

Windows PC를 대상으로 하는 2D 탑뷰 싱글 플레이 생존형 액션 RPG입니다. 첫 개발 목표는 플레이어와 슬라임이 싸우는 최소 전투 프로토타입입니다.

---
## 개발 환경

- Unity 에디터: 6000.3.21f1 (`ProjectSettings/ProjectVersion.txt` 기준)
- 입력 패키지: Input System 1.20.0
- 렌더링: Universal Render Pipeline 2D
- 현재 작업 폴더: `D:\Project-Phi`. 점검 시 이 폴더는 Git 저장소가 아니었습니다.

---
## 실행과 조작

1. Unity Hub에서 이 폴더를 기존 프로젝트로 추가하고 위 버전으로 엽니다.
2. 컴파일이 끝나면 메뉴 `Phi > Create or Open Combat Prototype`을 선택합니다. 제공된 `Assets/Phi/Scenes/CombatPrototype.unity`를 열고 빌드 씬 목록에 등록합니다. 설정 에셋은 `Assets/Phi/Data/PrototypeSettings.asset`입니다.
3. 에디터의 Play 버튼을 누릅니다. 사망 화면의 `RESTART` 버튼으로 씬을 다시 불러옵니다.

Unity 프로젝트 복사본에서 편집 모드와 플레이 모드 자동 테스트를 실행했습니다. 실제 마우스를 이용한 수동 플레이는 아직 검증하지 않았습니다. 메뉴는 기존 빌드 씬 순서를 보존합니다. 기존 `SampleScene`이 첫 빌드 씬이므로 독립 빌드에서 전투 씬을 시작하려면 메뉴로 등록한 뒤 빌드 씬 순서를 확인하고 조정해야 합니다.

| 조작 | 동작 |
|---|---|
| WASD | 이동. 대각선 속도 정규화 |
| 마우스 | 이동 방향과 독립적으로 조준 |
| 좌클릭 | 검 공격. 누르고 있으면 공격 간격에 따라 반복 |

전투 설정은 생성된 `PrototypeSettings` 에셋에서 조정할 수 있습니다. 검증 여부는 [진행 현황](docs/진행현황.md)을 확인하세요.

---
## 문서

- [개발 계획](docs/개발계획.md): 단계별 범위와 완료 기준
- [설계 결정](docs/설계결정.md): 채택한 기준, 문서 불일치와 미결정 사항
- [진행 현황](docs/진행현황.md): 구현과 검증 상태
