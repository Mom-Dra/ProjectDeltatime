# 프로젝트 기획서

## 1. 문서 정보

| 항목 | 내용 |
|---|---|
| 프로젝트명 | Deltatime |
| 문서 작성일 | 2026-07-30 (KST) |
| 마지막 분석일 | 2026-08-04 (KST) |
| 문서 버전 | 1.3.4 |
| 현재 구현 상태 | 핵심 전투 루프가 부분 구현된 3D 프로토타입. 물리 표면 조준점과 총구 기준 수평 발사, 결정적 수평·수직 탄도 산포와 빈 탄약 발사 시도 시간 활동이 있는 권총·자동소총·샷건, 빈손 플레이어 주먹, 현재 장비에 따른 공통 적 전투 AI, 적 무기 드롭·재무장, `DEADLINE`, 공중 무기 가로채기, 2개 조명 프로필 스테이지를 포함 |

### 1.1 분석 기준과 범위

- 이 문서의 경로는 저장소 루트 `C:\Users\HuiYong\UnityProjects\ProjectDeltatime`를 기준으로 적는다.
- 실제 Unity 프로젝트 루트는 저장소 안의 `ProjectDeltatime/`이다. 따라서 Unity의 `Assets`, `Packages`, `ProjectSettings`는 각각 `ProjectDeltatime/Assets`, `ProjectDeltatime/Packages`, `ProjectDeltatime/ProjectSettings`에 있다.
- 확정된 내용은 현재 파일, 직렬화된 씬/프리팹/데이터, 프로젝트 설정, Git 상태에서 직접 확인한 사실만 사용했다.
- 의도나 장르처럼 파일만으로 확정할 수 없는 내용에는 **추정**을 표시했다.
- 현재 브랜치는 `feature/Shotgun`이다. 2026-08-02 자동 연사·샷건·빈손 플레이어 주먹 공격과 무기별 결정적 수평·수직 탄도 산포 구현은 코드, 씬, 프리팹, ScriptableObject와 문서를 함께 갱신한 작업 트리를 기준으로 기록한다.
- 기존 `README`, 기획 문서, `AGENTS.md`는 분석 시작 시 없었다. `Assets/_Project/Tests` 폴더는 비어 있고 `.asmdef` 및 Unity Test Framework 테스트 어셈블리는 없다.
- 비생성 스크립트에서 `TODO`, `FIXME`, `HACK` 표식과 설명 주석은 확인되지 않았다.

### 1.2 테스트 근거의 한계

- `ProjectDeltatime/EnemyMovementSmoke.log`에는 2026-07-31에 `Prototype play-mode smoke test passed.`가 기록되어 있다.
- 커스텀 스모크 테스트는 `Stage2`를 열고 초기 플레이어/적/카메라/월드 시간, NavMeshData, 적 이동 누적 거리와 경로 획득, 근접형 추격 상태, 투척 무기 6 거리, 두 적 유형의 기절·무장 해제, 두 번의 `DEADLINE` 시네마틱 리플레이 시간축·카메라 고정·해제 후 복귀, 적 전멸 후 리플레이와 시야 조명 프록시를 검사한다.
- 현재 테스트 코드는 키보드 `Q` 입력 자체와 플레이어의 공중 무기 가로채기를 직접 검증하는 어설션이 없다. Deadline 리플레이 스모크는 시간축 회귀를 분리하기 위해 `DeadlineController`의 비공개 발동·해제 경로를 호출한다.
- 이번 적 무기 드롭·재무장·주먹 공격 및 플레이어 근접 전투 코드는 위 스모크 테스트보다 최신이다. Unity 6000.1.13f1 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`의 씬·에셋 정적 검증은 통과했지만, 사용자 요청에 따라 플레이 테스트와 스모크 테스트는 **미실행**했으므로 실제 전투 동작은 최신 통합 결과로 확인하지 않았다.
- 2026-08-01 `DEADLINE` 실제 이동 판정 수정은 Unity 6000.1.13f1 스크립트 컴파일, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`의 Stage1/Stage2 재생성과 `ValidateSavedPrototypeRoom`의 두 저장 씬 정적 검증을 통과했다. 두 씬의 `PlayerMovement.minimumPhysicalDisplacement: 0.001`과 `DeadlineController.movement` 참조를 확인했지만, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**했으므로 벽 접촉 중 발동 억제의 런타임 결과는 **확인 불가**다.
- 2026-08-01 리플레이 ViewCone 및 전체 시야 토글 변경은 Unity 6000.1.13f1 배치 스크립트 컴파일에서 `Tundra build success`와 종료 코드 0을 확인했다. 입력 에셋·생성 래퍼·Stage1/Stage2 직렬화는 정적으로 확인했지만, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**했으므로 메시 경계, 조명 전환, 시야 밖 적 표시의 실제 시각 품질은 **확인 불가**다.
- 2026-08-02 ViewCone 리플레이 실시간 재계산 전환은 Unity 6000.1.13f1 배치 스크립트 컴파일에서 `Tundra build success`와 종료 코드 0을 확인했다. 정점 샘플·풀링 참조 제거와 재생용 Raycast API 연결은 정적으로 확인했지만, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**했으므로 실제 시야 경계와 프레임 비용은 **확인 불가**다.
- 2026-08-02 `DEADLINE` 회전 중 최저 시간 배율 변경은 Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`의 Stage1/Stage2 정적 검증을 종료 코드 0으로 완료했다. 두 씬의 `minimumTimeScale: 0.02`, `DeadlineController`·`WorldTimeController` 참조와 캐치의 `RequestHardFreeze` 경로를 정적으로 확인했지만, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**했으므로 회전 중 위험 진행 체감과 동시 해방 결과는 **확인 불가**다.
- 2026-08-02 `DEADLINE` 씬당 충전 횟수 제한은 Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`의 Stage1/Stage2 정적 검증을 종료 코드 0으로 완료했다. 두 씬의 `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`와 필수 참조를 확인했지만, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**했으므로 충전 차감·소진·씬 재시작 회복의 런타임 결과는 **확인 불가**다.
- 2026-08-02 Q 키 기반 `DEADLINE` 발동 전환은 Unity 6000.1.13f1 배치 모드의 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`으로 Stage1/Stage2 정적 검증을 종료 코드 0으로 완료했다. `Deadline` 입력의 Q 바인딩, 기존 탄환·실제 이동 트리거 필드 제거, `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`를 확인했으며, 사용자 요청에 따라 플레이 모드와 커스텀 스모크 테스트는 **미실행**이므로 실제 사용 감각은 **확인 불가**다.
- 2026-08-02 Deadline 전용 시네마틱 리플레이 시간축은 Unity 6000.1.13f1 배치 스크립트 컴파일의 `Tundra build success`, `BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`의 Stage1/Stage2 정적 검증, `PrototypePlayModeSmokeTest`를 모두 통과했다. 스모크는 약 1초의 0.02배 Deadline을 최대 2초, 짧은 Deadline을 최소 0.8초, 해제 후 0.75 월드 초를 1.5초로 재생하는지와 카메라 고정·복귀를 확인한다. 실제 Q 조작 감각, 조준·행동 준비·이동 해제의 시각 연출과 R 재시작은 **확인 불가**다.
- 근접 무기 드롭·재획득, 시작 유형과 다른 무기 사용, 주먹 세 번 피격, 근거리 주먹 우선, 원거리 무기 탐색, 픽업 경쟁, 플레이어 근접 공격과 `DEADLINE` 해제 판정은 구현 코드와 직렬화 연결만 확인했으며 런타임 결과는 **확인 불가**다.
- 정식 Unity Test Framework 어셈블리는 없으며 `DEADLINE` 발동/행동 준비와 실제 입력 기반 공중 가로채기는 여전히 직접 검증하지 않는다.
- 2026-08-02 자동소총 홀드 연사·샷건·빈손 플레이어 주먹 공격은 Unity 6000.1.13f1 배치 컴파일의 `Tundra build success`, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`의 Stage1/Stage2 정적 검증을 종료 코드 0으로 완료했다. 권총/자동소총/샷건 발사 모드와 산탄 수치, Stage1/Stage2의 무기별 픽업 프리팹 및 샷건 정의 GUID, 기존 LMB 바인딩과 생성 래퍼의 일치를 정적으로 확인했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했으므로, 실제 자동 연사·산탄 명중·주먹 적중·`DEADLINE` 준비/해제 연계는 **확인 불가**다.
- 2026-08-02 무기별 결정적 좌우 탄도 산포는 `WeaponDefinition`의 `spreadJitterAngle`/`spreadSeed`와 공용 `WeaponController`의 발사 순번·펠릿 인덱스 기반 상태 없는 해시 계산을 정적으로 확인했다. 권총/자동소총은 각각 최대 ±1.5도(시드 101/211), 샷건은 기존 18도 대칭 팬의 각 펠릿에 최대 ±1도(시드 307)를 더한다. Unity 6000.1.13f1 배치 컴파일은 `Tundra build success`로 통과했고 `BuildAndValidateFromCommandLine`은 Stage1/Stage2 재생성과 저장 씬 검증을 종료 코드 0으로 완료했다. 샷건 에셋 GUID와 Stage1/Stage2의 픽업 프리팹 참조, 기존 LMB/`DEADLINE` 입력 분기 불변도 정적으로 확인했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했으므로 실제 탄도 체감·명중·적 AI 점사·`DEADLINE` 준비 발사 결과는 **확인 불가**다.
- 2026-08-03 총구 기준 마우스 조준 보정은 `PlayerAim`의 가장 가까운 비트리거 물리 표면 Raycast(플레이어 자신 제외)와 `PlayerCombat`의 총구→조준점 수평 방향 계산으로 구현했다. 적·벽·바닥·엄폐물은 같은 거리 우선 규칙을 사용하며, 벽 뒤 적은 조준하지 않고 콜라이더가 없을 때만 기존 `y=0` 지면 투영을 fallback으로 사용한다. Unity 6000.1.13f1 배치 컴파일의 `Tundra build success`, `BuildAndValidateFromCommandLine`의 생성 씬 검증, `PrototypePlayModeSmokeTest.RunFromCommandLine`은 모두 통과했다. 생성기는 기존 저장 씬의 비관련 레이아웃·머티리얼까지 재작성하므로 그 산출물은 보존하지 않았으며, 기존 씬은 새 직렬화 필드가 없어도 코드 기본값 `~0`으로 동작한다. 스모크가 직접 클릭별 탄도를 대조하지 않으므로 실제 마우스 입력에 따른 바닥·벽·적·자기 자신 클릭 결과와 `DEADLINE` 준비 발사는 **확인 불가**다.
- 2026-08-03 수평·수직 결정적 탄도 산포는 Unity 6000.1.13f1 배치 컴파일의 `Tundra build success`와 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`의 Stage1/Stage2 재생성·저장 씬 검증을 종료 코드 0으로 완료했다. 기존 권총·자동소총·샷건의 산포각과 시드, 픽업 GUID, LMB 바인딩과 `DEADLINE` Down 기반 준비 분기는 그대로이며, 공용 발사 경로가 수평·수직에 서로 다른 해시 채널 상수를 사용하고 Unity 전역 `Random`을 사용하지 않는 것을 정적으로 확인했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했으므로 실제 상하 탄도 체감·명중 분포·적 자동소총 점사와 `DEADLINE` 준비 발사 결과는 **확인 불가**다.

- 2026-08-04 빈 탄약 총기 발사 시도 시간 활동은 `WeaponController.TryFire`의 기존 성공 bool과 별도 `fireAttempted` 결과를 통해 구현했다. 일반 플레이어 발사에서만 구성·참조가 유효하고 사용 간격이 지난 빈 탄약 시도에도 기존 `fireActivity: 0.9`, `fireActivityDuration: 0.16` 펄스를 적용하며, 투사체·탄약·발사 순번은 변경하지 않는다. 빈 자동소총 홀드는 무기 사용 간격마다 시도하도록 다음 사용 시각을 전진시킨다. `DEADLINE`은 기존 준비 발사 경로를 유지해 빈 탄약이면 행동을 준비하거나 슬롯을 소비하지 않는다. Unity 6000.1.13f1 배치 컴파일은 `Tundra build success`와 종료 코드 0으로 완료했지만, 정식 테스트와 실제 LMB 빈 탄약 입력을 대조하는 스모크가 없어 플레이 모드 결과는 **확인 불가**다.

## 2. 프로젝트 개요

### 2.1 게임 장르

- **추정:** 3D 탑다운/쿼터뷰 액션 슈터 프로토타입.
- 근거: 원근 카메라가 플레이어를 위쪽에서 추적하고, `WASD` 이동·마우스 조준·발사·대시·무기 투척으로 이동 연사형 2명과 지속 추격 근접형 1명을 상대한다.
- 근거 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`

### 2.2 핵심 콘셉트

현재 코드에서 확인되는 핵심 콘셉트는 다음과 같다.

- 플레이어가 이동하거나 조준 방향을 돌리거나 사격·투척·대시할 때 월드 시간이 빨라진다.
- 플레이어는 실제 시간 기준으로 조작되며, 적·투사체·투척 무기 등 월드 객체는 별도의 `WorldDeltaTime`으로 진행된다.
- 적은 베이크된 NavMesh 경로와 충돌 안전 캡슐 이동으로 엄폐물을 우회하며, 시작 유형과 무관하게 현재 장비에 따라 총기 거리 유지·근접 추격·빈손 주먹 및 무기 탐색을 선택한다.
- 플레이어의 시야 부채꼴 또는 주변 원형 반경 4 안에 있고 장애물에 가리지 않은 적만 렌더링된다. 두 스테이지 모두 같은 반경을 밝히는 원형 Point Light를 사용하며, 어두운 Stage2에서는 부채꼴 손전등과 함께 가시성을 보조한다.
- `Q` 키를 누르면 탄환·이동 상태와 무관하게 `DEADLINE` 하드 프리즈가 발동하며 씬당 최대 2회 사용한다. 마우스를 멈추면 월드는 완전히 정지하고, 정지 중 마우스 회전은 최저 월드 배율로만 진행된다. 최대 2개의 사격·근접 공격·투척 행동을 준비한 뒤 이동으로 동시에 해제한다.
- 무기는 종류에 따라 발사하거나 근접 공격에 사용하며, 던져 모든 적을 기절·무장 해제하거나, 플레이어와 적이 바닥 무기를 확보하고, 적에게서 날아온 무기를 플레이어가 공중에서 가로챌 수 있다.
- 모든 적을 제거하면 실시간 시뮬레이션을 멈추고 기록된 시각 상태를 하이브리드 시간축으로 반복 재생한다. 일반 구간은 1.00배 월드 시간, `DEADLINE`은 현실 시간 기반 시네마틱 구간과 해제 후 0.50배 후속 구간으로 표시한다.

근거 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`

### 2.3 플레이어가 경험해야 하는 핵심 재미

- **추정:** 움직임과 조준 자체가 적과 투사체의 시간 진행량을 결정하는 데서 생기는 판단 재미.
- **추정:** 총알이 닿기 직전 멈춰 시간을 고정하고 여러 원인을 배치한 다음 한 번에 해제하는 전술적 연출.
- **추정:** 제한 탄약, 무기 투척, 적 무장 해제, 바닥 교환, 공중 가로채기를 연결하는 즉흥적 무기 순환.
- **추정:** 제한된 시야와 엄폐물 속에서 적 위치와 사격 경고선을 읽는 긴장감.
- **추정:** 스테이지 종료 후 제한 시야 리플레이와 필요할 때 전환하는 밝은 전체 시야로 플레이 결과를 다시 보는 연출적 보상.

### 2.4 예상 플레이 흐름

현재 구현 기준의 실제 흐름은 다음과 같다.

1. 빌드 인덱스 0의 `Stage1` 또는 에디터에서 연 씬을 시작한다.
2. 플레이어는 권총 8발을 장비한 상태로 시작한다.
3. 이동·조준으로 월드 시간 속도를 조절하며 세 명의 적과 교전한다.
4. 사격, 대시, `DEADLINE`, 무기 투척·회수·교환·가로채기를 사용한다.
5. 적 세 명이 모두 사망하면 스테이지 상태가 `Replaying`으로 바뀌고 리플레이가 반복된다.
6. 어느 시점이든 `R`을 누르면 현재 씬을 다시 불러온다.
7. `Stage1`에서 `Stage2`로 자동 전환하거나 리플레이에서 빠져나가는 흐름은 **미구현**이다.

### 2.5 현재 확인된 프로젝트 방향

- 3D 물리 기반 전투 프로토타입으로 전환된 상태다. 씬 검증 코드도 `Rigidbody2D`가 없어야 하고 원근 카메라여야 한다고 검사한다.
- Git 이력에는 `3D 프로토타입 제작`, `KillCam 구현`, `암흑시야와 Light 구현`이 기록되어 있다.
- 현재 미커밋 변경에는 자동 발사/산탄 무기 정의, 무기별 결정적 수평·수직 탄도 산포, 빈손 플레이어 주먹 공격, 무기별 시작 픽업 프리팹과 재생성된 Stage1/Stage2가 포함된다.
- `Stage1`과 `Stage2`의 게임 오브젝트 구성은 동일하고 조명 프로필만 다르다. 두 씬을 별도 콘텐츠 단계로 사용할지, 밝기 비교용 변형으로 사용할지는 **확인 불가**다.

## 3. 현재 구현 현황

| 기능 | 상태 | 설명 | 근거 파일 | 비고 |
|---|---|---|---|---|
| 3D 플레이어 이동 | 구현 완료 | `WASD` 입력을 동적 Rigidbody의 평면 속도로 변환하고 마지막 물리 스텝의 입력 방향 실제 변위를 공개하며 충돌과 하드 프리즈를 반영 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs` | 이동 속도 6, 실제 이동 최소 변위 0.001m, 벽 접촉 시 위치 강제 이동 없음 |
| 마우스 조준 | 구현 완료 | 화면 포인터 광선에서 플레이어 자신을 제외한 가장 가까운 비트리거 콜라이더의 `RaycastHit.point`를 조준점으로 사용하고, 충돌 대상이 없을 때만 `y=0` 평면으로 fallback하여 플레이어 회전과 조준선을 갱신 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs` | 적·벽·바닥·엄폐물은 거리 우선이며 실제 마우스 입력 검증은 확인 불가 |
| 대시 | 구현 완료 | 이동 방향으로 최대 3.5 거리, 0.03 스킨의 축소 캡슐 캐스트, 대시 중 무적, 0.8초 쿨다운 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs` | 벽 0.01 겹침 시작 회귀 검사 포함 스모크 통과 |
| 행동량 기반 월드 시간 | 구현 완료 | 이동·조준 회전·행동 펄스를 합산해 월드 배율을 0.02~1.0으로 보간하며, 데드라인 전용 하드 프리즈 토큰은 조준 회전 중에만 최저 배율을 허용 | `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs` | 전역 `Time.timeScale`은 변경하지 않음 |
| `DEADLINE` | 부분 구현 | `Q` 키 Down 프레임에 탄환·이동 상태와 무관하게 하드 프리즈하고, 마우스 정지 시 0배·회전 시 최저 배율로 전환한다. 씬당 최대 2회 발동하며 사격·근접 공격·투척 중 최대 2개 행동을 준비해 이동 입력으로 해제한다 | `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs` | 성공 발동에서만 충전 차감, 씬 재로드 시 회복, 리플레이 중 회복 없음. 씬 연결·컴파일·정적 검증은 확인, 최신 플레이 테스트와 전용 테스트 없음 |
| 총기 사격 | 구현 완료 | 권총·샷건은 LMB Down 1회에 1회 발사하고 자동소총만 LMB 홀드 중 발사 간격마다 연사한다. 플레이어 총기와 투척은 총구에서 조준점의 `x/z`로 수평 발사하며, 성공한 매 발사는 발사 순번·펠릿 인덱스·무기 시드로 결정한 독립 수평·수직 탄도 산포를 적용한다. 일반 플레이어 발사에서는 유효한 빈 탄약 시도도 같은 시간 활동 펄스를 발생시키지만 투사체·탄약·발사 순번은 변경하지 않는다 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset` | 빈 자동소총 홀드도 무기 사용 간격마다만 시간 활동을 발생. `DEADLINE` 준비 발사·적 자동소총의 기존 4발 점사와 실제 조작 체감은 확인 불가 |
| 근접 무기 공격 | 구현 완료 | 전방 반각 35도·거리 1.45 안에서 시야가 확보된 가장 가까운 적대 대상 하나에 피해 3을 적용 | `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset` | 플레이어는 실제 시간 쿨다운, 적은 월드 시간 상태 머신 사용. 플레이 검증 미실행 |
| 빈손 플레이어 주먹 | 구현 완료 | 빈손일 때 LMB Down으로 기존 `MeleeAttackResolver`에 거리 1.2, 반각 35도, 피해 1, 사용 간격 0.6초의 근접 공격을 요청한다. `DEADLINE`에서는 기존 행동 준비·이동 해제 경로를 재사용한다 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs` | 현재 적 체력 모델상 유효 피격 1회 처치. 실제 적중과 `DEADLINE` 연계는 플레이 테스트 **미실행**으로 확인 불가 |
| 투사체 충돌·피해 | 구현 완료 | SphereCast로 충돌을 찾고 적대 팩션 `IDamageable`에 피해 전달 | `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs` | 총기 피해 3은 플레이어 최대 체력과 같음 |
| 무기 투척 | 구현 완료 | 장비 무기를 던지고 적 명중 시 기절, 최대 6 거리 후 바닥 픽업으로 변환 | `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs` | 기존 스모크 테스트 범위에 포함 |
| 적 기절·무장 해제·재무장 | 구현 완료 | 모든 적이 기절 시 현재 장비와 남은 탄약을 드롭하고, 2 월드초 후 빈손 판단을 재개해 주먹 공격 또는 예약한 바닥 무기 획득을 시도 | `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs` | 재무장 후 다시 기절/사망하면 새 현재 장비를 다시 드롭. 플레이 검증 미실행 |
| 바닥 무기 획득·교환·예약 | 구현 완료 | 플레이어는 `E`로 근처 픽업을 획득/교환하며 적 예약을 무시한다. 빈손 적은 NavMesh 완전 경로가 있는 픽업을 예약해 획득 | `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs` | 여러 적의 동일 픽업 추적을 예약으로 방지. 런타임 경쟁 결과 확인 불가 |
| 적 무기 공중 드롭 | 구현 완료 | 이동 방향 또는 전방으로 현재 총기/근접 무기를 포물선 드롭하고 종류별 큐브 스케일과 착지 예측선을 표시 | `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs` | 모든 적에 드롭 컴포넌트가 직렬화됨. 최신 스모크 미실행 |
| 공중 무기 가로채기 | 부분 구현 | `E` 입력과 0.18초 버퍼로 반경 1.15 내 공중 무기를 장비하고 0.2초 하드 프리즈 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs` | 최신 플레이 테스트 없음 |
| 적 이동·경로 탐색 | 구현 완료 | 외부 `StageNavigation.asset`의 NavMesh 경로를 사용하고 Kinematic Rigidbody 캡슐을 `WorldDeltaTime`만큼 이동. 벽 충돌과 적 간 분리를 적용 | `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scenes/StageNavigation.asset` | 런타임 동적 NavMesh 재베이크는 없음 |
| 장비 기반 공통 적 전투 AI | 구현 완료 | `EnemyCombatant`가 현재 장비에 따라 총기 거리 유지·후퇴 사격, 근접 무기 선딜 추격, 빈손 주먹/무기 탐색을 전환하며 `EnemyShooter`/`EnemyChaser`는 시작 유형 래퍼로 유지 | `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs` | 시작 이동 속도는 유지하고 공격 방식은 현재 장비가 결정. 플레이 검증 미실행 |
| 플레이어/적 체력 | 부분 구현 | 플레이어는 최대 체력 3과 현재 체력, 변경 이벤트를 가지며 주먹 피해 1은 세 번 누적되어 사망한다. 적은 기존처럼 유효 피해 한 번에 사망 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs` | 총기·근접 무기 피해 3은 플레이어 즉사 유지. 세 번 주먹 피격은 런타임 확인 불가 |
| 시야 부채꼴·암흑 시야 | 구현 완료 | 장애물 Raycast로 메시를 갱신하고, 부채꼴 또는 지면 반경 4 원형 시야 안에서 가리지 않은 적의 몸체·장착 무기를 렌더링. 런타임 손전등과 밝기 4의 원형 Point Light를 생성하며 원형광은 Soft Shadow로 벽·엄폐물에 차단되도록 구성 | `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, 두 스테이지 씬 | 적 AI의 감지 여부와 플레이어 가시성은 별도. 실제 원형 경계·벽 차폐는 확인 불가 |
| 탑다운 카메라 | 구현 완료 | 원근 카메라가 플레이어와 조준 선행 지점을 부드럽게 추적 | `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs` | 카메라 1대 |
| 스테이지 적 등록·클리어 | 구현 완료 | 생존 적을 등록하고 0명이 되면 전투를 막고 리플레이 요청 | `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs` | 적 3명 고정 콘텐츠 |
| 사망·재시작 | 구현 완료 | 플레이어 사망 시 전투를 막고 `R`로 현재 씬 재로드 | `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs` | 체크포인트 없음 |
| 스테이지 리플레이 | 부분 구현 | 카메라·렌더러·라인·등록 조명을 20Hz 현실 시간으로 기록하고, 일반 구간은 1.00배 월드 시간, `DEADLINE`은 0.8~2.0초 시네마틱과 해제 후 0.50배 후속 구간으로 매핑해 프록시 재생한다. ViewCone은 기록된 보간 포즈에서 매 렌더 프레임 재계산하며 `V`로 암흑/전체 시야를 전환 | `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs` | Deadline 중 카메라를 진입 포즈에 고정하고 해제 후 0.2초 동안 복귀. 종료/스킵/다음 씬 없음, 최신 수동 시각 품질·프레임 비용 확인 불가 |
| HUD | 부분 구현 | IMGUI로 적 수, 체력 `HEALTH 3/3`, 실시간, 월드 배율, 대시, `DEADLINE`, 무기, 리플레이 `VIEW DARK`/`VIEW FULL`과 조작법 표시 | `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs` | 디버그 HUD, 로컬라이징/해상도 대응 없음 |
| Stage1/Stage2 콘텐츠 | 부분 구현 | 두 씬 모두 플레이어 1, 이동 연사형 2, 근접 추격형 1, 권총·샷건 픽업 2, Navigation 1을 같은 위치에 배치 | `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Prefabs/PistolPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab` | 조명만 밝음/어두움으로 다름 |
| 씬 전환 | 미구현 | 현재 씬 재시작 외에 다른 씬을 로드하는 코드가 없음 | `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs` | `Stage1 → Stage2` 흐름 필요 여부 확인 |
| 메인 메뉴·일시정지·설정 | 미구현 | 관련 씬, UI, 입력, 코드가 없음 | `ProjectDeltatime/Assets/_Project` | 계획 필요 |
| 일반 아이템·인벤토리 | 미구현 | 무기 1개 즉시 장비/교환 외 슬롯·목록·소모품 시스템 없음 | `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs` | 계획 필요 |
| 퀘스트 | 미구현 | 관련 데이터와 코드가 없음 | `ProjectDeltatime/Assets/_Project` | 계획 필요 |
| 세이브/로드 | 미구현 | 런타임 저장 API와 저장 데이터가 없음 | `ProjectDeltatime/Assets/_Project/Scripts` | 계획 필요 |
| 사운드 | 미구현 | `AudioSource`, `AudioClip`, 오디오 에셋이 없고 `Audio` 폴더가 비어 있음 | `ProjectDeltatime/Assets/_Project/Audio` | 계획 필요 |
| 게임패드·리바인딩 | 미구현 | `Keyboard&Mouse` 제어 스킴만 정의 | `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions` | 목표 플랫폼 확인 필요 |
| 자동 테스트 | 부분 구현 | 커스텀 배치 스모크 코드를 공통 적 상태 API에 맞게 컴파일 가능하도록 갱신했으나 이번 기능 변경에서는 실행하지 않음 | `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs` | Unity 컴파일·씬 생성기 정적 검증만 통과, 런타임 결과 확인 불가 |

## 4. 핵심 게임 루프

```mermaid
flowchart TD
    A["Stage1 또는 Stage2 로드"] --> B["권총 8발·적 3명으로 시작"]
    B --> C["이동·조준·대시로 위치와 월드 시간 조절"]
    C --> D["사격 / 무기 투척 / DEADLINE 행동 준비"]
    D --> E["적 공격 회피·기절·무장 해제"]
    E --> F["바닥 교환 또는 공중 무기 가로채기"]
    F --> G{"플레이어 생존?"}
    G -- "아니오" --> H["YOU DIED"]
    H --> I["R: 현재 씬 재시작"]
    G -- "예" --> J{"남은 적 0명?"}
    J -- "아니오" --> C
    J -- "예" --> K["전투 비활성화·시각 리플레이 반복"]
    K --> I
```

### 4.1 게임 시작

- `EditorBuildSettings.asset`의 활성 씬 순서는 `Stage1`, `Stage2`다.
- 별도 부트스트랩이나 메인 메뉴는 없다.
- 각 씬은 플레이어 1명, 권총 픽업 1개, 이동 연사형 2명, 지속 추격 근접형 1명, 엄폐물과 베이크된 NavMesh가 있는 방으로 시작한다.
- 플레이어는 권총, 사격 시작 적 2명은 자동소총, 근접 시작 적 1명은 획득·투척 가능한 `MeleeWeapon.asset`을 장비하고 시작한다.

### 4.2 플레이어의 주요 행동

- `WASD`: 이동
- 마우스 이동: 조준 및 플레이어 회전
- 마우스 왼쪽: 권총·샷건은 단발, 자동소총은 홀드 연사, 빈손은 주먹 공격
- 마우스 오른쪽: 현재 무기 투척
- `Space`: 이동 방향 대시
- `E`: 공중 무기 가로채기, 바닥 무기 획득 또는 교환
- 리플레이 중 `V`: 암흑 시야와 전체 시야 전환
- `R`: 현재 씬 재시작
- `Q` 키 Down: 탄환·이동 상태와 무관한 `DEADLINE` 진입 조건
- `DEADLINE` 중 공격/투척: 최대 2개 행동 준비. 근접 공격은 준비 시 방향과 무기 수치를 저장하고 해제 시 판정
- `DEADLINE` 중 마우스 회전: 월드 전체를 최저 배율로 진행, 마우스 정지 시 다시 완전 정지
- `DEADLINE` 중 이동: 하드 프리즈 해제 및 준비 행동 진행

### 4.3 적 또는 장애물과의 상호작용

- 총기를 장비한 적은 몸체 기준 시야선으로 플레이어를 감지하고 NavMesh로 접근/후퇴해 6~9 거리를 유지한다. 6 거리 미만에서는 플레이어를 바라본 채 70% 속도로 후퇴하며, 이동과 병행해 0.65 월드초 조준·무기별 점사·1.15 월드초 쿨다운을 진행한다.
- 근접 무기를 장비한 적은 1.45 거리 안에서 0.42 월드초 선딜 동안 플레이어를 바라보며 35% 속도로 따라붙은 후 피해 3을 주고, 플레이어가 1.9 밖으로 벗어나거나 장애물에 가려지면 공격을 취소한다.
- 빈손 적은 보이는 플레이어가 3 거리 안이면 무기 탐색보다 접근과 주먹 공격을 우선한다. 주먹은 거리 1.2, 선딜 0.35, 후딜 0.6 월드초, 피해 1이다. 그 밖에서는 0.25 월드초마다 반경 8 안에서 완전한 NavMesh 경로가 있는 바닥 무기를 예약한다.
- 빈손 적은 장전된 총기를 우선하되 총기 경로가 가장 가까운 근접 무기보다 2 이상 길면 근접 무기를 선택한다. 탄약이 없는 총기는 바닥에 내려놓고 다시 판단한다.
- 벽·엄폐물은 사선, 투사체, 대시, 시야 부채꼴, 공중 드롭의 경로를 막는다.
- 플레이어 투사체는 적을, 적 투사체는 플레이어를 공격한다. 같은 팩션과 발사 원본은 무시한다.
- 던진 무기는 적을 죽이지 않고 2 월드초 기절시키며 현재 장비를 공중으로 드롭한다. 회복한 적은 빈손 전투 또는 재무장 판단을 재개한다.

### 4.4 보상과 성장

- 적이 떨어뜨린 현재 총기 또는 근접 무기를 회수·교환·가로채는 전투 중 무기 순환이 즉시 보상으로 구현되어 있다.
- 바닥에 있는 권총 8발 픽업도 사용할 수 있다.
- 점수, 경험치, 레벨업, 영구 성장, 통화, 해금은 **미구현**이다.
- 따라서 현재 보상 구조는 전투 중 자원 순환에 한정된다.

### 4.5 실패와 재시작

- 플레이어 최대 체력은 3이다. 적 주먹은 피해 1이므로 세 번 누적 피격 시 사망하고, 총기와 근접 무기는 피해 3으로 기존 즉사를 유지한다.
- 사망하면 `StageController`가 `PlayerDead`로 바뀌고 플레이어 전투가 비활성화된다.
- HUD에 `YOU DIED`와 재시작 안내가 표시된다.
- `R`은 현재 활성 씬을 다시 로드한다.

### 4.6 게임 종료 조건

- 적 세 명을 모두 제거하면 현재 스테이지는 클리어되고 곧바로 리플레이 상태로 진입한다.
- 리플레이는 마지막 프레임을 0.65초 유지한 뒤 처음부터 반복한다.
- 명시적인 게임 완료 화면, 다음 스테이지, 타이틀 복귀, 리플레이 종료 조건은 **미구현**이다.

## 5. 주요 시스템

### 5.1 플레이어 시스템

- **시스템 목적:** 플레이어 생존, 입력, 이동, 조준, 대시, 전투 기능을 한 GameObject에 구성한다.
- **현재 동작 방식:** `Player` 오브젝트에 입력·체력·이동·조준·대시·전투·`DEADLINE`·무기 컨트롤러가 함께 붙고 직렬화 참조로 연결된다.
- **주요 클래스:** `PlayerInputReader`, `PlayerHealth`, `PlayerMovement`, `PlayerAim`, `PlayerDash`, `PlayerCombat`, `DeadlineController`, `WeaponController`
- **데이터 흐름:** 입력 리더 → 각 플레이어 행동 컴포넌트 → `WorldTimeActivity`, `WeaponController`, `WorldTimeController`
- **다른 시스템과의 의존성:** 카메라, 월드 시간, 무기 프리팹, 스테이지, HUD
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Player`
- **개선이 필요한 부분:** 책임이 단일 Player GameObject의 직접 참조에 집중되어 있고, 플레이어 상태 전환을 통합 관리하는 상태 머신은 없다.

### 5.2 이동 및 조작

- **시스템 목적:** 키보드·마우스로 3D 평면 이동과 조준을 제공한다.
- **현재 동작 방식:** Input System의 `Gameplay` 액션 맵을 매 프레임 폴링한다. 일반 이동은 동적 Rigidbody의 `linearVelocity`로 평면 속도를 지정하고, 사망·하드 프리즈·비활성화 시 평면 속도를 0으로 만든다. 대시는 축소한 월드 캡슐을 이동 방향으로 캐스트해 시작점이 벽에 맞닿거나 0.03 이내로 겹쳐도 안전 거리까지만 `MovePosition`한다. 조준은 카메라 Ray의 가장 가까운 비트리거 콜라이더 지점을 사용하고, 플레이어 자신의 콜라이더는 건너뛴다. 적·벽·바닥은 거리 순서로 같은 조준점 규칙을 사용하며, 충돌 대상이 없을 때만 지면 Plane 교차점으로 fallback한다.
- **주요 클래스:** `PlayerControls`, `PlayerInputReader`, `PlayerMovement`, `PlayerAim`, `PlayerDash`
- **데이터 흐름:** `PlayerControls.inputactions` → 생성된 `PlayerControls.cs` → `PlayerInputReader` → 이동/조준/대시
- **다른 시스템과의 의존성:** 월드 활동량, 체력, 하드 프리즈, 카메라
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Scripts/Player`
- **개선이 필요한 부분:** 게임패드, 리바인딩, 입력 장치 변경, 일시정지 입력, UI 입력은 없다.

### 5.3 월드 시간 및 `DEADLINE`

- **시스템 목적:** 플레이어 행동량에 따라 월드 진행 속도를 조절하고, 임박한 피격 순간에 원인들을 준비할 수 있는 하드 프리즈를 제공한다.
- **현재 동작 방식:** **부분 구현**. 활동량을 0~1로 합산해 0.02~1.0 배율을 보간한다. `DEADLINE`은 플레이어가 살아 있고 전투가 활성화됐으며 충전·재사용 대기 조건을 만족할 때 `Q` 키 Down 프레임에 회전 허용 토큰 기반 하드 프리즈를 획득한다. 탄환 존재·충돌 예측·플레이어 이동·입력 해제는 발동 조건에 포함하지 않는다. 성공 발동 직후 씬당 최대 2회 충전 중 1회를 차감하며, 충전 0에서는 Q 안내를 만들지 않는다. 충전은 씬 `Awake`에서 초기화되고 리플레이의 비활성화/재활성화로는 회복하지 않는다. 데드라인 중 `WorldTimeActivity.AimTurn`이 0.0001보다 크면 `WorldTimeController.minimumTimeScale`로 월드 전체가 진행하고, 마우스 정지 시 `CurrentTimeScale`과 `WorldDeltaTime`은 0으로 돌아간다. 일반 하드 프리즈 또는 0.2초 공중 가로채기 프리즈가 겹치면 완전 정지가 우선한다.
- **주요 클래스:** `WorldTimeActivity`, `WorldTimeController`, `PlayerMovement`, `DeadlineController`
- **데이터 흐름:** 이동/조준/행동 펄스 → 목표 월드 배율 → `WorldDeltaTime` → 적·투사체·투척/드롭 무기. Q 입력 → `PlayerInputReader.DeadlinePressed` → `DeadlineController` → 회전 허용 하드 프리즈 토큰 → 조준 활동 여부에 따른 0 또는 최저 배율 → 준비 행동 해제
- **다른 시스템과의 의존성:** 입력, 플레이어 Rigidbody 이동, 체력, 플레이어 전투, 투사체 정적 레지스트리, HUD
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`
- **개선이 필요한 부분:** 전용 자동 테스트와 튜토리얼이 없고, 행동 두 개 제한·재사용 시간의 최종 기획 의도가 문서로 확정되지 않았다.

### 5.4 전투

- **시스템 목적:** 팩션 기반 총기·근접 공격, 투사체 충돌, 무기 투척과 `DEADLINE` 준비 공격을 제공한다.
- **현재 동작 방식:** `WeaponController`가 현재 `WeaponKind`, 탄약과 실제/월드 시간 사용 간격을 관리한다. 플레이어 총기 일반 발사·`DEADLINE` 준비 발사·투척은 총구 위치에서 `PlayerAim.AimPoint`의 `x/z`로 수평 방향을 계산한다. 총기는 성공한 매 발사 때 발사 순번을 증가시키고, 무기 시드·발사 순번·펠릿 인덱스와 축별 채널 상수를 조합한 상태 없는 해시로 독립 수평·수직 탄도 산포를 결정한 뒤 투사체를 만든다. 샷건은 기존 대칭 수평 팬 각도에 펠릿별 수평·수직 산포를 더하며, 근접 무기는 공통 부채꼴 판정으로 시야가 확보된 가장 가까운 적대 대상 하나를 친다. 투척 무기는 장비를 즉시 해제하고 충돌 또는 최대 거리에서 픽업으로 변환된다.
- **주요 클래스:** `WeaponController`, `MeleeAttackResolver`, `Projectile`, `ThrownWeapon`, `CombatQuery`, `DamageHit`, `StunHit`
- **데이터 흐름:** 입력/AI → 무기 컨트롤러 → 투사체·근접 판정 또는 투척 무기 → `IDamageable`/`IStunnable` → 체력/AI/스테이지
- **다른 시스템과의 의존성:** `WeaponDefinition`, 월드 시간, 프리팹, 팩션, 히트 플래시
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Combat`, `ProjectDeltatime/Assets/_Project/Scripts/Core`
- **개선이 필요한 부분:** 재장전·조준점/카메라 반동·연속 발사 누적 반동·명중 수치·효과음·피격 경직과 근접 공격 애니메이션이 없다.

### 5.5 적 AI

- **시스템 목적:** 공통 경로 탐색/이동 위에서 현재 장비와 상황에 따른 총기·근접 무기·주먹 전투와 재무장을 제공한다.
- **현재 동작 방식:** `EnemyPerception`이 몸체 기준 시야선과 최근 확인 위치를 관리하고 `EnemyMotor`가 베이크된 NavMesh 경로를 따라 Kinematic Rigidbody를 월드 시간으로 이동한다. `EnemyCombatant`는 공격 상태와 이동 모드를 분리하고 현재 장비로 공격 방식을 결정한다. 빈손일 때는 근거리 플레이어를 우선 주먹으로 상대하거나 경로 길이와 예약을 사용해 바닥 무기를 찾는다. `EnemyShooter`와 `EnemyChaser`는 시작 장비/속도 구분용 얇은 래퍼다.
- **주요 클래스:** `EnemyBehavior`, `EnemyCombatant`, `EnemyPerception`, `EnemyMotor`, `EnemyShooter`, `EnemyChaser`, `EnemyHealth`, `EnemyWeaponDrop`
- **데이터 흐름:** 플레이어 위치/생존 + 현재 장비 + 바닥 픽업 → 시야선/경로 길이/예약 → 월드 시간 이동/회전 → 장비별 공격. 피격/기절 → 공통 행동 중단·현재 무기 드롭 → 회복 후 빈손 판단/재무장 → 스테이지 통지
- **다른 시스템과의 의존성:** 플레이어, 월드 시간, AI Navigation, 3D 물리, 무기, 시야 부채꼴, 스테이지
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Enemies`
- **개선이 필요한 부분:** 전술적 엄폐 선택, 협동, 스폰, 난이도 변화가 없다. 플레이어 시야 밖에서도 AI 추적과 공격이 계속될 수 있으므로 의도 확인이 필요하다. 재무장과 픽업 경쟁은 플레이 테스트 미실행으로 체감·교착 여부가 확인되지 않았다.

### 5.6 체력 및 피해

- **시스템 목적:** 플레이어 누적 체력과 생존/변경/사망 이벤트, 대시 무적, 적 사망·기절을 제공한다.
- **현재 동작 방식:** 플레이어는 최대 체력 3에서 `DamageHit.Damage`를 차감하고 0에서 사망하며 대시 중 피해를 무시한다. 적은 별도 누적 HP 없이 유효 피해 한 번에 사망한다.
- **주요 클래스:** `PlayerHealth`, `EnemyHealth`, `IDamageable`, `IStunnable`
- **데이터 흐름:** 충돌 → `DamageHit`/`StunHit` → 체력 → 사망/기절 이벤트와 시각 변화
- **다른 시스템과의 의존성:** 스테이지, 적 AI, 무기 드롭, HUD, 대시
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Core/CombatContracts.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`
- **개선이 필요한 부분:** 플레이어 체력 회복·피격 무적·체력 바 애니메이션은 없고, 적은 여전히 누적 체력을 사용하지 않는다.

### 5.7 아이템·무기·인벤토리

- **시스템 목적:** 무기 자원 순환과 즉시 장비 교환을 제공한다.
- **현재 동작 방식:** 바닥 픽업은 무기 정의와 탄약, 적 예약 소유자를 보유한다. 플레이어는 예약을 무시하고 획득/교환한다. 빈손 적은 장전된 총기를 우선하되 경로 차이가 2 이상이면 가까운 근접 무기를 선택하고 한 픽업을 예약한다. 공중 드롭은 적이 가로채지 않으며 플레이어가 잡으면 이전 무기를 바닥에 생성한다.
- **주요 클래스:** `WeaponDefinition`, `WeaponPickup`, `InterceptableWeapon`, `EnemyWeaponDrop`, `WeaponController`
- **데이터 흐름:** ScriptableObject 종류/수치 + 탄약 → 픽업/예약/공중 드롭 → 플레이어 또는 적 무기 컨트롤러 장비 → 사격/근접 공격/투척
- **다른 시스템과의 의존성:** 적 기절/사망, 플레이어 상호작용, 월드 시간, 프리팹
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Prefabs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat`
- **개선이 필요한 부분:** 인벤토리 슬롯, 소모품, 드롭 테이블, 재장전은 없다.

### 5.8 스테이지 및 게임 진행 관리

- **시스템 목적:** 생존 적 수, 플레이 시간, 클리어/사망 상태, 재시작을 관리한다.
- **현재 동작 방식:** `EnemyHealth`가 활성화 시 자신을 등록하고 사망 시 제거한다. 생존 적 0명이 되면 전투를 비활성화하고 리플레이를 요청한다.
- **주요 클래스:** `StageController`
- **데이터 흐름:** 적 등록/사망 → 생존 집합 → 스테이지 상태 → 플레이어 전투 및 리플레이 → HUD
- **다른 시스템과의 의존성:** 적 체력, 플레이어 체력/전투, 입력, 리플레이, SceneManager
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`
- **개선이 필요한 부분:** 다음 씬 전환, 결과 화면, 스테이지 데이터, 체크포인트, 스폰 웨이브가 없다.

### 5.9 리플레이

- **시스템 목적:** 스테이지 클리어까지의 시각적 전투를 정상 월드 시간으로 재생하되, `DEADLINE`의 느린 조준·준비 구간은 읽을 수 있는 시네마틱 길이로 보존한다.
- **현재 동작 방식:** **부분 구현**. 20Hz 현실 시간 샘플마다 현실·월드 타임스탬프와 `DeadlineController.IsActive`를 함께 기록하며, Deadline 진입·해제 프레임은 즉시 추가 기록한다. 리플레이 시작 시 일반 구간은 월드 시간 차이, Deadline 활성 구간은 `현실 길이 / 0.50`을 0.8~2.0초로 제한한 길이, 해제 후 0.75 월드 초는 0.50배 길이로 프레젠테이션 시간축을 구성한다. 시각·조명·ViewCone은 프레젠테이션 시점에서 대응하는 현실 샘플 시점으로 보간한다. Deadline 중 카메라는 진입 샘플의 위치·회전·FOV·배경색으로 고정하고, 해제 후 첫 0.2초에는 기록 카메라로 보간 복귀한다. ViewCone은 위치·회전·스케일만 트랙에 저장하며, 삼각형 토폴로지는 프록시 생성 때 한 번 복제한 뒤 암흑 시야 리플레이의 매 `LateUpdate`에 기록된 보간 포즈와 정적 `VisionObstacle` Raycast로 정점·Bounds·Normals를 다시 계산한다. 적 몸체와 장착 무기는 실제 Renderer 상태와 별도로 논리적 전체 시야 가시성을 기록한다. 클리어 시 대부분의 `MonoBehaviour`를 끄고 원본을 숨긴 뒤 암흑 시야로 프록시를 반복 재생한다. 리플레이 중 `V`를 누르면 ViewCone·동적 조명 프록시를 숨기고 안개 제거, Trilight 환경광과 그림자 없는 Directional Fill Light를 적용해 시야 밖의 생존 적 몸체·장착 무기를 표시하며, 전체 시야에서는 ViewCone Raycast를 수행하지 않는다. 다시 `V`를 누르면 같은 재생 시점의 암흑 시야 메시와 조명을 즉시 복원한다.
- **주요 클래스:** `StageReplayController`
- **데이터 흐름:** 라이브 렌더러·카메라·조명·ViewCone 포즈·적 논리 가시성 → 샘플 트랙 → 클리어 요청 → 라이브 시뮬레이션 비활성화 → 프록시 재생 중 ViewCone Raycast 재계산 → `V` 입력에 따른 암흑/전체 시야 적용
- **다른 시스템과의 의존성:** 스테이지, 입력, 카메라, `VisionCone` 런타임 조명과 메시, `EnemyCombatant`, HUD, 전역 `RenderSettings`
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`
- **개선이 필요한 부분:** ViewCone 정점 기록 메모리는 제거했지만 암흑 시야 리플레이에서는 매 렌더 프레임 97회 Raycast와 메시 Bounds/Normals 갱신이 발생한다. 일반 시각 샘플의 색상·라인 배열 할당, 기록량 상한, 매 캡처의 전체 렌더러 검색, 장시간 스테이지의 메모리·CPU 성능, 리플레이 종료/스킵, 오디오 재생은 미구현이다.

### 5.10 카메라 및 시야

- **시스템 목적:** 조준 방향을 선행하는 원근 탑다운 화면과 제한 시야를 제공한다.
- **현재 동작 방식:** 카메라는 플레이어 + 조준 방향 2.25 지점을 지수 보간으로 추적한다. `VisionCone`은 96개 구간의 동적 메시와 장애물 Raycast를 사용한다. 적은 60도·거리 12.5 부채꼴 또는 지면 반경 4 원형 시야 안에 있으면서 장애물에 가리지 않았을 때 몸체와 장착 무기가 렌더링된다. 런타임 손전등 밝기는 7.5이고, 플레이어 기준 높이 1의 원형 Point Light는 지면 반경 4가 되도록 실제 `Light.range`를 계산하며 밝기 4, `ForcePixel`, Soft Shadow 강도 0.85를 사용한다. 두 조명은 매 `LateUpdate`에 플레이어를 추적하고 리플레이에 등록된다. 리플레이 암흑 시야도 저장한 포즈와 정적 `VisionObstacle`을 사용해 같은 메시 계산을 매 프레임 수행하며, `V` 전체 시야에서는 ViewCone과 두 동적 조명을 숨기고 정적 환경 조명은 유지한다.
- **주요 클래스:** `TopDownCameraController`, `VisionCone`, `WorldTimeVisualFeedback`
- **데이터 흐름:** 플레이어 위치/조준 → 카메라 초점. 플레이어 Transform/장애물 Layer → 시야 메시·조명·적 렌더러 가시성
- **다른 시스템과의 의존성:** 입력, 적 AI 렌더러, 리플레이, `VisionObstacle` Layer
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`
- **개선이 필요한 부분:** 카메라 충돌/줌/흔들림이 없고, 시야 밖 적의 공격 가능 여부를 기획적으로 확정해야 한다.

### 5.11 UI 및 피드백

- **시스템 목적:** 프로토타입 상태와 조작법, 전투 경고를 화면에 표시한다.
- **현재 동작 방식:** 런타임 IMGUI로 텍스트 패널과 진행 막대를 직접 그린다. 월드가 느릴수록 화면에 어두운 오버레이를 적용하며, 리플레이에서는 `VIEW DARK`/`VIEW FULL` 상태와 `V Toggle Full View` 안내를 표시한다.
- **주요 클래스:** `GameHud`, `WorldTimeVisualFeedback`, `HitFlash`
- **데이터 흐름:** 스테이지/시간/대시/`DEADLINE`/무기/리플레이 상태 → HUD. 충돌 이벤트 → `HitFlash`
- **다른 시스템과의 의존성:** 거의 모든 런타임 상태 시스템
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`
- **개선이 필요한 부분:** Canvas/UI Toolkit 기반 제품 UI, 해상도 대응, 색약/접근성, 로컬라이징, 입력 아이콘 전환이 없다. HUD 문자열의 `STAGE CLEAR ?? REPLAY`는 표시 문자 검토가 필요하다.

### 5.12 이벤트

- **시스템 목적:** 핵심 상태 변경을 직접 호출과 C# 이벤트로 전달한다.
- **현재 동작 방식:** 플레이어 체력 변경/사망은 `PlayerHealth.HealthChanged`/`Died`, 장비 변경은 `WeaponController.EquipmentChanged`, `DEADLINE` 해제는 `DeadlineController.Released` 이벤트를 사용한다. 적 사망과 스테이지 등록은 직접 메서드 호출이다.
- **주요 클래스:** `PlayerHealth`, `DeadlineController`, `StageController`, `PlayerCombat`
- **데이터 흐름:** 체력/`DEADLINE` → 이벤트 구독자. 적 체력 → 스테이지 직접 통지
- **다른 시스템과의 의존성:** 전투, 무기 쿨다운, 스테이지
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`
- **개선이 필요한 부분:** 공통 이벤트 버스는 없으며, 직접 참조와 호출 방식이 혼재한다. 현재 규모에서는 동작하지만 시스템 확장 시 결합도 관리가 필요하다.

### 5.13 데이터 관리

- **시스템 목적:** 무기 수치와 씬/프리팹 구성을 에셋으로 직렬화한다.
- **현재 동작 방식:** 권총·자동소총·샷건·근접 무기 수치는 `WeaponDefinition` ScriptableObject에 종류별로 저장된다. 총기는 기본 수평 팬 각도와 별도 결정적 수평·수직 산포 최대각·시드를 함께 저장하며, 한 최대각을 두 축에 공통 적용한다. 자동소총만 자동 발사 모드이며, 샷건은 8펠릿·총 퍼짐 18도의 반자동 모드다. 적 행동 수치는 각 씬의 공통 `EnemyCombatant` 필드에 직렬화된다.
- **주요 클래스:** `WeaponDefinition`, `PrototypeSceneBuilder`
- **데이터 흐름:** `Pistol.asset`/`AutomaticRifle.asset`/`Shotgun.asset`/`MeleeWeapon.asset` → 플레이어·적 무기 컨트롤러/픽업/드롭. 에디터 빌더 상수 → 씬·프리팹·머티리얼 직렬화
- **다른 시스템과의 의존성:** 전투 전반, 콘텐츠 생성 도구
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`
- **개선이 필요한 부분:** 적/스테이지/시간/플레이어 수치용 데이터 에셋은 없고, 빌더 재실행 시 수동 씬 수정이 덮어써질 수 있다.

### 5.14 세이브/로드

- **시스템 목적:** **계획 필요**
- **현재 동작 방식:** **미구현**
- **주요 클래스:** 없음
- **데이터 흐름:** 없음
- **다른 시스템과의 의존성:** 스테이지 진행, 설정, 성장 시스템이 추가될 경우 필요
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Scripts`에서 `PlayerPrefs`, 파일 저장, JSON 저장 로직 없음
- **개선이 필요한 부분:** 저장 대상, 슬롯, 버전 호환, 실패 복구 정책부터 결정해야 한다.

### 5.15 퀘스트

- **시스템 목적:** **계획 필요**
- **현재 동작 방식:** **미구현**
- **주요 클래스:** 없음
- **데이터 흐름:** 없음
- **다른 시스템과의 의존성:** 목표/진행/보상 시스템이 정해질 경우 스테이지와 UI에 의존
- **근거 파일:** `ProjectDeltatime/Assets/_Project`에 퀘스트 관련 코드·데이터 없음
- **개선이 필요한 부분:** 이 프로젝트에 퀘스트가 필요한 장르인지 먼저 확인해야 한다.

### 5.16 사운드

- **시스템 목적:** **계획 필요**
- **현재 동작 방식:** **미구현**
- **주요 클래스:** 없음
- **데이터 흐름:** 없음
- **다른 시스템과의 의존성:** 사격, 투척, 피격, 대시, `DEADLINE`, 클리어, 리플레이
- **근거 파일:** `ProjectDeltatime/Assets/_Project/Audio`가 비어 있고 코드/씬에 `AudioSource`가 없음
- **개선이 필요한 부분:** 오디오 이벤트, 믹서, 월드 시간에 따른 피치 정책, 리플레이 오디오 정책을 결정해야 한다.

## 6. 씬 및 콘텐츠 구조

### 6.1 씬 목록

| 빌드 순서 | 씬 | 역할 | 확인된 차이 | 상태 |
|---:|---|---|---|---|
| 0 | `Stage1` | 밝은 조명 프로필의 전투 방 | Ambient 1.0, Directional 0.9, Map Fill 1.5, 안개 35~70 | 부분 구현 |
| 1 | `Stage2` | 어두운 조명/암흑 시야 프로필의 동일 전투 방 | Ambient 0.35, Directional 0.06, Map Fill 0, 안개 19~42 | 부분 구현 |

근거 파일: `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`

### 6.2 씬 전환 흐름

```mermaid
flowchart LR
    A["빌드 시작: Stage1"] --> B["Stage1 전투"]
    B --> C["적 전멸"]
    C --> D["Stage1 리플레이 반복"]
    B --> E["R: Stage1 재로드"]
    F["Stage2"] --> G["에디터 직접 실행 또는 별도 로드 필요"]
    G --> H["Stage2 전투"]
    H --> I["Stage2 리플레이 반복"]
    H --> J["R: Stage2 재로드"]
    D -. "자동 전환 미구현" .-> F
```

### 6.3 각 씬의 주요 오브젝트

두 씬은 동일한 구성 요소 수를 가지며 권총과 샷건 픽업을 각각 하나씩 배치한다.

- `Systems`: `WorldTimeActivity`, `WorldTimeController`, `StageReplayController`, `StageController`
- `Player`: 3D Rigidbody, 입력, 체력, 이동, 조준, 대시, 전투, `DEADLINE`, 무기
- `Vision Cone`: 동적 메시, 시야 머티리얼, 런타임 조명 생성
- `Main Camera`: 원근 카메라, 탑다운 추적, 월드 시간 시각 피드백
- `Navigation`: `NavMeshSurface`와 외부 `StageNavigation.asset` 참조
- `Enemy West`, `Enemy East`: 거리 유지·4발 점사를 수행하는 이동 연사형 2명
- `Enemy Center`: 플레이어 현재 위치를 계속 따라가는 근접 추격형 1명
- `Pistol Pickup`: 탄약 8발 권총 픽업 1개
- `Shotgun Pickup`: 탄약 6발 샷건 픽업 1개. `ShotgunPickup.prefab`이 `Shotgun.asset` GUID를 직접 참조한다.
- `Industrial Room`: 바닥, 외벽 4개, 중앙 엄폐물 3개, 상자 더미 2개, 바닥 가이드
- `Directional Key Light`, `Blue Bay Light`, `Red Alert Light`
- `Debug HUD`

### 6.4 프리팹 구조

| 프리팹 | 주요 구성 | 역할 |
|---|---|---|
| `Projectile.prefab` | `LineRenderer`, `Projectile` | 팩션별 탄환 이동·충돌·트레일 |
| `WeaponPickup.prefab` | Cube, Trigger Collider, `WeaponPickup` | 바닥 무기 보관·교환 |
| `PistolPickup.prefab` | Cube, Trigger Collider, `WeaponPickup`, `Pistol.asset` 참조 | Stage1/Stage2 시작 권총 픽업 |
| `ShotgunPickup.prefab` | Cube, Trigger Collider, `WeaponPickup`, `Shotgun.asset` 참조 | Stage1/Stage2 시작 샷건 픽업 |
| `ThrownWeapon.prefab` | Cube, `LineRenderer`, `ThrownWeapon` | 플레이어 무기 투척·기절·착지 |
| `InterceptableWeapon.prefab` | Body, Trigger Sphere, Trail, Prediction, Landing Marker, `InterceptableWeapon` | 적 드롭 무기의 포물선 비행·예측·가로채기 |

근거 파일: `ProjectDeltatime/Assets/_Project/Prefabs`

### 6.5 ScriptableObject

| 에셋 | 타입 | 확인된 데이터 |
|---|---|---|
| `Pistol.asset` | `WeaponDefinition` | 반자동 총기, 탄창 8, 발사 간격 0.24초, 탄속 17, 피해 3, 1발, 총 퍼짐 0도, 결정적 수평·수직 산포 축당 최대 ±1.5도(시드 101), 적 점사 1발, 투사체 반경 0.08 |
| `AutomaticRifle.asset` | `WeaponDefinition` | 자동 발사 총기, 탄창 30, 발사 간격 0.12초, 탄속 16, 피해 3, 1발, 총 퍼짐 0도, 결정적 수평·수직 산포 축당 최대 ±1.5도(시드 211), 적 점사 4발, 투사체 반경 0.075 |
| `Shotgun.asset` | `WeaponDefinition` | 반자동 총기, 탄창 6, 발사 간격 0.75초, 탄속 16, 펠릿 피해 1, 8펠릿, 총 퍼짐 18도(좌우 ±9도), 펠릿별 결정적 수평·수직 산포 축당 최대 ±1도(시드 307), 투사체 반경 0.075 |
| `MeleeWeapon.asset` | `WeaponDefinition` | 근접, 탄약 없음, 피해 3, 거리 1.45, 정면 반각 35도, 사용 간격 0.72초 |

### 6.6 현재 확인된 콘텐츠

- 전투 방 레이아웃 1종
- 조명 프로필 2종
- 적 유형 2종: 이동 연사형, 지속 추격 근접형
- 무기 데이터 4종: 권총, 자동소총, 샷건, 근접 무기
- 픽업/투척/공중 드롭 표현
- 프로토타입 머티리얼 14개, 커스텀 시야 셰이더 3개
- `VisionAlwaysVisible`, `VisionHiddenArea`, `VisionStencilWriter` 머티리얼과 셰이더는 현재 씬/프리팹에서 직접 참조되지 않는다.
- `Circle.png`, `Square.png`, `PrototypeRoom3DPreview.png`도 현재 씬/프리팹에서 직접 참조되지 않는다.

## 7. 플레이어 경험

### 7.1 조작 방법

| 입력 | 동작 | 구현 상태 |
|---|---|---|
| `W`, `A`, `S`, `D` | 이동 | 구현 완료 |
| 마우스 이동 | 지면 조준·플레이어 회전 | 구현 완료 |
| 마우스 왼쪽 | 권총·샷건 단발, 자동소총 홀드 연사, 빈손 주먹 / `DEADLINE` 중 Down 기반 공격 준비 | 구현 완료: 컴파일·씬 연결 확인, 실제 연사·산탄·주먹·`DEADLINE` 연계는 미실행으로 확인 불가 |
| 마우스 오른쪽 | 무기 투척 / `DEADLINE` 중 투척 준비 | 구현 완료 |
| `Q` | `DEADLINE` 즉시 발동 | 부분 구현: 충전·재사용 대기·하드 프리즈 조건을 만족하면 탄환·이동 상태와 무관하게 발동 |
| `Space` | 이동 방향 대시 | 구현 완료 |
| `E` | 공중 가로채기 또는 바닥 획득/교환 | 부분 구현: 바닥 교환은 기존 테스트 확인, 공중 가로채기는 최신 테스트 미검증 |
| `V` | 리플레이 암흑/전체 시야 토글 | 구현 완료: 입력·씬 연결·컴파일 확인, 실제 시각 품질 확인 불가 |
| `R` | 현재 씬 재시작 | 구현 완료 |

### 7.2 목표

- 현재 시스템이 판정하는 목표는 생존한 적 3명을 모두 제거하는 것이다.
- 내러티브 목표, 임무 텍스트, 제한 시간, 점수 목표는 없다.

### 7.3 게임 진행 방식

- 플레이어는 항상 실제 시간 기준 이동 속도를 유지한다.
- 플레이어 행동량에 따라 적과 투사체의 월드 진행 속도가 변한다.
- 탄약은 발사로 줄며 자동 재장전은 없다.
- 무기를 던지면 즉시 비무장 상태가 되고, 플레이어는 바닥 또는 공중 무기를 다시 확보해야 한다. 적도 기절에서 회복한 뒤 주먹으로 싸우거나 바닥 무기를 찾아 재무장한다.
- 적이 모두 사망하면 조작 가능한 전투 대신 암흑 시야 리플레이가 반복되며, 선택한 `V` 전체 시야 상태는 반복 구간이 바뀌어도 유지된다.

### 7.4 피드백

- 조준 방향 라인
- 적 조준 경고 라인
- 플레이어/적 탄환의 팩션별 색상 트레일
- 저속 시간에서 길어지는 투사체·투척 트레일
- 피격/기절/가로채기 위치의 `HitFlash`
- 대시 중 무적
- `DEADLINE` Q 발동 안내, 하드 프리즈, 행동 수 초과 피드백
- 공중 무기 비행 궤적과 착지 마커
- 어두운 화면 오버레이와 시야 스폿/근거리 조명
- 클리어 후 재생 포즈와 Raycast로 ViewCone을 재계산하는 시각 리플레이와 `V` 전체 시야 전환

### 7.5 UI 정보 구조

- 좌측 상단 상태 패널: 적 수, 체력, 실제 플레이 시간, 월드 배율 또는 리플레이 시간과 `VIEW DARK`/`VIEW FULL`, 대시 상태, `DEADLINE` 상태, 무기/탄약 또는 근접 표시
- 화면 중앙: 사망/클리어 메시지 또는 `DEADLINE` 행동 수·해제 안내
- 화면 상단 중앙: 사용 가능할 때 `PRESS Q TO DEADLINE` 안내
- 화면 하단: 전체 키보드·마우스 조작법
- 별도 메뉴, 설정, 일시정지, 인벤토리, 결과 화면은 없다.

### 7.6 예상되는 사용자 경험

- **추정:** 플레이어는 계속 움직이면 적탄이 정상 속도에 가까워지고, 멈추거나 조준을 덜 움직이면 거의 정지한 월드를 관찰하게 된다.
- **추정:** 탄약이 부족해질수록 무기를 던져 적을 무장 해제하고 그 무기를 가로채는 행동이 핵심 생존 수단이 된다.
- **추정:** `Stage1`의 밝은 환경은 규칙 학습, `Stage2`의 어두운 환경은 시야 제약 강화에 사용할 수 있다. 현재 자동 진행이 없으므로 이 역할은 확정되지 않았다.

### 7.7 확인되지 않은 부분

- 난이도 곡선과 플레이 시간 목표
- `Stage1`과 `Stage2`의 공식 순서 및 역할
- `DEADLINE`의 사용자 대상 명칭과 튜토리얼 문구
- 리플레이 스킵/종료 방식
- 적이 플레이어 시야 밖에서 공격하는 것이 의도인지 여부
- 최종 UI, 아트, 사운드, 내러티브 방향

## 8. 기술 구조

### 8.1 시스템 관계

```mermaid
flowchart TD
    IA["PlayerControls.inputactions"] --> IR["PlayerInputReader"]
    IR --> PM["PlayerMovement"]
    IR --> PA["PlayerAim"]
    IR --> PD["PlayerDash"]
    IR --> PC["PlayerCombat"]
    IR --> DC["DeadlineController"]
    IR --> SC["StageController"]

    PM --> WA["WorldTimeActivity"]
    PA --> WA
    PD --> WA
    PC --> WA
    WA --> WT["WorldTimeController"]

    WT --> ES["EnemyShooter"]
    WT --> EC["EnemyChaser"]
    WT --> EM["EnemyMotor"]
    WT --> PR["Projectile"]
    WT --> TW["ThrownWeapon"]
    WT --> IW["InterceptableWeapon"]
    DC --> WT
    PR --> DC

    PC --> WC["WeaponController"]
    EP["EnemyPerception"] --> ES
    EP --> EC
    EM --> ES
    EM --> EC
    ES --> WC
    WC --> PR
    WC --> TW
    TW --> EH["EnemyHealth"]
    PR --> PH["PlayerHealth / EnemyHealth"]
    EH --> IW
    IW --> PC

    EH --> SC
    PH --> SC
    SC --> RP["StageReplayController"]
    RP --> HUD["GameHud"]
    WT --> HUD
    DC --> HUD
```

### 8.2 주요 클래스와 책임

| 클래스 | 책임 |
|---|---|
| `PlayerInputReader` | Input System 액션 폴링과 1프레임 버튼 상태 제공 |
| `WorldTimeActivity` | 이동·조준·행동 펄스 활동량 보관 |
| `WorldTimeController` | 커스텀 월드 시간 계산과 하드 프리즈 토큰 관리 |
| `DeadlineController` | 임박한 투사체 탐색, 정지 트리거, 행동 준비/해제 |
| `WeaponController` | 현재 무기 종류/탄약/사용 쿨다운, 장비 변경 이벤트, 사격·즉시/준비 근접 공격·투척 |
| `MeleeAttackResolver` | 전방 부채꼴과 시야선에서 가장 가까운 적대 대상 하나 판정 |
| `Projectile` | 월드 시간 기반 이동, SphereCast 충돌, 피해와 트레일 표시 |
| `ThrownWeapon` | 기절 투척물 이동과 바닥 픽업 변환 |
| `InterceptableWeapon` | 적 드롭 무기의 포물선, 장애물, 예측, 가로채기 |
| `EnemyBehavior` | 적 유형 공통 기절·장비 해제·재무장·사망 수명주기 |
| `EnemyCombatant` | 현재 장비에 따른 총기/근접 무기/주먹 상태, 이동 모드, 무기 경로 탐색·예약 |
| `EnemyPerception` | 플레이어 거리, 시야선, 최근 확인 위치 |
| `EnemyMotor` | NavMesh 경로 계산, 월드 시간 Rigidbody 이동, 회전, 충돌, 적 간 분리 |
| `EnemyShooter` | 자동소총 시작 적을 표시하는 `EnemyCombatant` 래퍼 |
| `EnemyChaser` | 근접 무기 시작 적을 표시하는 `EnemyCombatant` 래퍼 |
| `StageController` | 적 생존 집합과 스테이지 상태 |
| `StageReplayController` | 카메라/렌더러/라인/조명 샘플 기록과 프록시 재생 |
| `VisionCone` | 시야 메시, 가시성 판정, 런타임 시야 조명 |
| `PrototypeSceneBuilder` | 두 씬, NavMeshData, 프리팹, 머티리얼, 권총/자동소총/샷건/근접 무기 데이터와 무기별 시작 픽업 재생성 및 검증 |

### 8.3 싱글턴 사용 여부

- 전형적인 `Instance` 싱글턴은 없다.
- `Projectile`은 현재 활성 투사체를 정적 리스트로 유지하며 서브시스템 초기화 시 비운다.
- `CombatQuery`는 상태 없는 정적 유틸리티다.
- 대부분의 시스템은 씬에 존재하는 인스턴스를 직렬화 참조 또는 `Configure`로 연결한다.

### 8.4 이벤트 구조

- C# 이벤트:
  - `PlayerHealth.HealthChanged` → 체력 UI 확장 지점
  - `PlayerHealth.Died` → `StageController`
  - `WeaponController.EquipmentChanged` → `EnemyCombatant`
  - `DeadlineController.Released` → `PlayerCombat`
- 직접 호출:
  - `EnemyHealth` → `StageController.NotifyEnemyDied`
  - `StageController` → `PlayerCombat.SetCombatEnabled`, `StageReplayController.RequestReplay`
  - 전투 객체 → `IDamageable.ReceiveHit`, `IStunnable.ReceiveStun`
- `UnityEvent`와 공통 이벤트 버스는 사용하지 않는다.

### 8.5 데이터 저장 방식

- 정적 게임 데이터: `WeaponDefinition` ScriptableObject
- 콘텐츠/설정 데이터: Unity 씬, 프리팹, 머티리얼, `ProjectSettings`
- 입력 데이터: `.inputactions`와 자동 생성된 C# 래퍼
- 런타임 진행 데이터: 메모리 내 컴포넌트 필드
- 영구 저장: 없음

### 8.6 외부 패키지

| 패키지 | 버전 | 사용 확인 |
|---|---:|---|
| `com.unity.inputsystem` | 1.14.2 | 플레이어 입력에 직접 사용 |
| `com.unity.multiplayer.center` | 1.0.0 | 코드 사용은 확인되지 않음 |
| Unity 내장 모듈 | 1.0.0 | Physics, UI/IMGUI, AI 모듈 등이 manifest에 포함 |

Unity 버전: `6000.1.13f1`

### 8.7 Tags, Layers, Scenes 설정

- 사용자 정의 Tag: 없음
- 사용자 정의 Layer: 인덱스 8 `VisionObstacle`
- 씬의 Layer 사용: 각 씬에서 Default 30개, `VisionObstacle` 13개 GameObject
- Sorting Layer: `Default`만 존재
- 활성 Input Handler: 새 Input System
- 레거시 `InputManager.asset`에는 기본 축 18개가 남아 있으나 런타임 입력 코드는 새 Input System을 사용한다.
- 빌드 씬: `Stage1`, `Stage2`
- 에디터 직렬화: Force Text
- 제품명/회사명: `Deltatime` / `DefaultCompany`
- 기본 화면 크기: 1920×1080, 창 크기 조절 비활성, 백그라운드 실행 비활성
- 활성 Color Space: Gamma
- 번들 버전: 1.0

근거 파일: `ProjectDeltatime/ProjectSettings/TagManager.asset`, `ProjectDeltatime/ProjectSettings/ProjectSettings.asset`, `ProjectDeltatime/ProjectSettings/EditorSettings.asset`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`

### 8.8 확장 시 주의점

- 월드 객체의 시간 진행에는 `UnityEngine.Time.deltaTime` 대신 `WorldTimeController.WorldDeltaTime`을 사용해야 현재 콘셉트가 유지된다.
- 플레이어 조작은 의도적으로 `unscaledDeltaTime`을 사용한다. 신규 플레이어 행동이 월드 시간에 종속되어야 하는지는 별도 결정이 필요하다.
- `PrototypeSceneBuilder`를 재실행하면 두 씬, `StageNavigation.asset`, 핵심 프리팹/머티리얼과 권총/자동소총/근접 무기 수치를 다시 생성 또는 갱신한다. 수동 씬·NavMesh 수정과 빌더 상수의 소유권을 분리해야 한다.
- 적의 실제 이동량과 상태 타이머는 `WorldDeltaTime`을 사용해야 하며, NavMesh는 경로만 제공하고 Transform을 자동 이동시키지 않는다.
- 신규 가시성 장애물은 Layer 8 `VisionObstacle`에 배치해야 시야 메시와 공중 드롭 충돌 예측에 반영된다.
- 새 런타임 조명은 리플레이에 보여야 한다면 `StageReplayController.RegisterLight`로 등록해야 한다.
- 새 렌더러 타입은 현재 리플레이가 지원하는 `MeshRenderer`, `SkinnedMeshRenderer`, `LineRenderer`인지 확인해야 한다.
- 전체 시야에서 별도 가시성 규칙이 필요한 적 시각 요소는 `EnemyCombatant.TryGetReplayVisibility`와 녹화 정책을 함께 갱신해야 하며, 경고선·일반 이펙트는 자동으로 강제 표시되지 않는다.
- 새 무기는 `WeaponDefinition`만 추가하는 것으로 끝나지 않고 투사체/투척 프리팹과 HUD 표현 호환성을 검토해야 한다.

### 8.9 기술 부채

- 이번 기능의 `EnemyCombatant`, `MeleeAttackResolver`, `MeleeWeapon.asset`과 메타 파일은 작업 트리에서 미추적 상태이므로 변경 확정 시 함께 추적해야 한다.
- 정식 테스트 어셈블리와 단위/플레이 모드 테스트는 없다. 커스텀 배치 스모크는 2026-08-03에 통과했지만, 마우스 클릭별 조준점·총구 탄도 같은 입력 세부 조건을 직접 대조하는 테스트는 없다.
- `StageReplayController`는 20Hz마다 전체 활성 GameObject의 렌더러를 검색하고 기록 길이에 상한이 없어 긴 플레이에서 비용이 증가한다. ViewCone 정점 샘플은 제거했지만 암흑 시야 리플레이의 매 프레임 Raycast·Normals 재계산 비용은 프로파일링이 필요하며, 동적 `VisionObstacle`이 추가되면 과거 상태와 달라질 수 있다. 일반 재질 색상·라인 배열은 변경 샘플마다 할당된다.
- 리플레이가 시작되면 대부분의 `MonoBehaviour`를 끄며, 현재 반복 리플레이 구조에서는 복구 경로가 없다.
- 플레이어/적/시간/스테이지 밸런스 수치가 씬 컴포넌트와 코드 기본값에 분산되어 있다.
- 런타임 코드는 단일 기본 어셈블리에 있고 `.asmdef` 경계가 없다.
- HUD가 IMGUI 디버그 구현이며 제품 UI 구조가 없다.
- 사용되지 않는 것으로 확인된 시야 스텐실 머티리얼/셰이더와 생성 이미지가 남아 있다.
- `Assets/_Project/Tests` 폴더는 비어 있다.
- `Stage1`과 `Stage2`가 조명 외에는 동일하여 콘텐츠 중복 관리 위험이 있다.
- `DamageHit.Damage`가 현재 생명력 계산에 사용되지 않는다.
- `TopDownCameraController`의 `input` 참조는 설정 검증에 사용되지만 추적 로직에서는 직접 사용하지 않는다.

## 9. 밸런스 및 수치

| 항목 | 값 | 정의 위치 | 설명 |
|---|---:|---|---|
| 플레이어 이동 속도 | 6 | `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity` | 실제 시간 기준 거리/초 |
| 대시 최대 거리 | 3.5 | 같은 씬의 `PlayerDash` | 벽 충돌 시 단축 |
| 대시 속도 | 22 | 같은 씬의 `PlayerDash` | 실제 시간 기준 |
| 대시 지속 시간 | 0.16초 | 같은 씬의 `PlayerDash` | 실제 시간 |
| 대시 쿨다운 | 0.8초 | 같은 씬의 `PlayerDash` | 실제 시간 |
| 대시 활동 펄스 | 1.0 / 0.22초 | 같은 씬의 `PlayerDash` | 월드 시간 활동량 |
| 최소 월드 배율 | 0.02배 | 같은 씬의 `WorldTimeController` | 완전 정지가 아닌 기본 저속 |
| `DEADLINE` 회전 중 월드 배율 | 최소 월드 배율과 동일한 0.02배 | `WorldTimeController` | 데드라인 전용 토큰만 존재하고 `AimTurn > 0.0001`일 때. 일반/캐치 하드 프리즈가 겹치면 0배 우선 |
| 최대 월드 배율 | 1.0배 | 같은 씬의 `WorldTimeController` | 활동량 1 이상 |
| 시간 보간 속도 | 8 | 같은 씬의 `WorldTimeController` | 지수 보간 계수 |
| 이동/조준/펄스 가중치 | 각 1 | 같은 씬의 `WorldTimeController` | 합산 후 0~1 제한 |
| 조준 최대 활동 각속도 | 360도/초 | 같은 씬의 `PlayerAim` | 이 값에서 조준 활동량 1 |
| 권총 탄창 | 8발 | `ProjectDeltatime/Assets/_Project/Pistol.asset` | 시작/바닥 픽업 최대 탄약 |
| 권총 발사 간격 | 0.24초 | `ProjectDeltatime/Assets/_Project/Pistol.asset` | 플레이어는 실제 시간, 적은 월드 시간 시계를 전달 |
| 권총 탄속 | 17 | `ProjectDeltatime/Assets/_Project/Pistol.asset` | 월드 시간 기준 |
| 권총 피해 | 3 | `ProjectDeltatime/Assets/_Project/Pistol.asset` | 플레이어 최대 체력과 같아 적 사용 시 즉사 |
| 권총 결정적 수평·수직 산포 | 축당 최대 ±1.5도, 시드 101 | 같은 에셋 | 축별 독립 해시로 성공한 발사마다 새로 계산, 조준점 반동 없음 |
| 투사체 반경 | 0.08 | `ProjectDeltatime/Assets/_Project/Pistol.asset` | SphereCast 반경 |
| 자동소총 탄창 | 30발 | `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset` | 이동 연사형 시작 탄약 |
| 자동소총 발사 간격 | 0.12 월드초 | 같은 에셋 | 적 4발 점사 내 발사 간격 |
| 자동소총 탄속 | 16 | 같은 에셋 | 월드 시간 기준 |
| 자동소총 피해 | 3 | 같은 에셋 | 플레이어 최대 체력과 같아 즉사 |
| 자동소총 결정적 수평·수직 산포 | 축당 최대 ±1.5도, 시드 211 | 같은 에셋 | 플레이어와 적 AI의 공용 발사 경로에 적용 |
| 자동소총 투사체 반경 | 0.075 | 같은 에셋 | SphereCast 반경 |
| 샷건 탄창 | 6발 | `ProjectDeltatime/Assets/_Project/Shotgun.asset` | Stage1/Stage2 시작 픽업 탄약도 6발 |
| 샷건 발사 간격/탄속 | 0.75초 / 16 | 같은 에셋 | 반자동, 월드 시간 기준 투사체 이동 |
| 샷건 펠릿 피해/수/총 퍼짐 | 1 / 8 / 18도 | 같은 에셋 | 좌우 ±9도의 대칭 팬 패턴 |
| 샷건 펠릿 추가 결정적 수평·수직 산포 | 축당 최대 ±1도, 시드 307 | 같은 에셋 | 각 펠릿의 수평 팬 각도에 독립적으로 더함 |
| 플레이어 빈손 주먹 범위/반각/간격/피해 | 1.2 / 35도 / 0.6초 / 1 | `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs` | 실제 시간 쿨다운, `DEADLINE`에서는 기존 준비/해제 경로 |
| 투사체 최대 수명 | 4 월드초 | `ProjectDeltatime/Assets/_Project/Prefabs/Projectile.prefab` | 미충돌 시 제거 |
| 투척 무기 속도 | 7 | `ProjectDeltatime/Assets/_Project/Prefabs/ThrownWeapon.prefab` | 월드 시간 기준 |
| 투척 무기 최대 거리 | 6 | 같은 프리팹 | 도달 시 픽업 생성 |
| 투척 무기 기절 | 2 월드초 | 같은 프리팹 | 모든 적이 현재 장비를 드롭하고 회복 후 빈손 판단 재개 |
| 바닥 픽업 반경 | 1.25 | `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity` | 플레이어 중심 |
| 공중 가로채기 반경 | 1.15 | 같은 씬의 `PlayerCombat` | 플레이어 중심 |
| 가로채기 입력 버퍼 | 0.18초 | 같은 씬의 `PlayerCombat` | 실제 시간 |
| 가로채기 프리즈 | 0.2초 | 같은 씬의 `PlayerCombat` | 실제 시간 하드 프리즈 |
| 적 드롭 탄약 | 현재 남은 탄약 | 같은 씬의 `EnemyWeaponDrop` | 현재 장비를 드롭하며 재무장 뒤 다시 드롭 가능 |
| 공중 드롭 비행 시간 | 0.85 월드초 | `ProjectDeltatime/Assets/_Project/Prefabs/InterceptableWeapon.prefab` | 포물선 진행 |
| 공중 드롭 수평 거리 | 3 | 같은 프리팹 | 장애물에 막히면 단축 |
| 공중 드롭 호 높이 | 1.25 | 같은 프리팹 | 포물선 추가 높이 |
| 궤적 예측 점 | 16개 | 같은 프리팹 | 장애물까지 표시 |
| `DEADLINE` 발동 키 | `Q` | `PlayerControls.inputactions` | 키 Down 프레임에 즉시 발동 |
| 실제 이동 최소 변위 | 0.001m/물리 스텝 | 같은 씬의 `PlayerMovement` | 일반 이동 입력 방향 성분, 관용 시간 없음 |
| `DEADLINE` 재준비 | 0.35 월드초 | 같은 씬의 `DeadlineController` | 해제 후 |
| `DEADLINE` 씬당 최대 충전 | 2회 | 같은 씬의 `DeadlineController` | 성공 발동 시 1회 차감, 씬 재로드 시 초기화, 리플레이 중 회복 없음 |
| 준비 행동 최대 수 | 2개 | 같은 씬의 `DeadlineController` | 사격/근접 공격/투척 합계 |
| 플레이어 최대 체력 | 3 | 같은 씬의 `PlayerHealth` | `CurrentHealth`가 피해량만큼 감소 |
| 사격 적 탐지 거리 | 18 | 같은 씬의 `EnemyPerception` | 시야선 필요 |
| 사격 적 이동 속도 | 3.4 | 같은 씬의 `EnemyMotor` | `WorldDeltaTime` 기준 |
| 총기 장비 적 선호 거리 | 6~9 | 같은 씬의 `EnemyCombatant` | 시작 유형과 무관하게 미만 후퇴, 초과 추적 |
| 총기 장비 적 후퇴 속도 배율 | 70% | 같은 씬의 `EnemyCombatant` | 플레이어를 바라보며 조준·점사·쿨다운과 병행 |
| 총기 장비 적 조준 시간 | 0.65 월드초 | 같은 씬의 `EnemyCombatant` | 정면 오차 허용 후 감소 |
| 자동소총 적 점사 | 4발 | `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset` | 권총은 적 사용 시 1발 |
| 총기 장비 적 쿨다운 | 1.15 월드초 | 같은 씬의 `EnemyCombatant` | 점사 후 |
| 사격 적 회전 속도 | 220도/월드초 | 같은 씬의 `EnemyMotor` | 이동과 목표 회전 |
| 총기 장비 적 정면 허용 오차 | 6도 | 같은 씬의 `EnemyCombatant` | 경고선/조준 진행 조건 |
| 근접 적 탐지 거리 | 20 | 같은 씬의 `EnemyPerception` | 시야선 또는 최근 확인 위치 추격 |
| 근접 적 이동 속도 | 4.8 | 같은 씬의 `EnemyMotor` | 플레이어 현재 위치를 계속 갱신 |
| 근접 무기 공격 거리/취소 거리 | 1.45 / 1.9 | `MeleeWeapon.asset`와 같은 씬의 `EnemyCombatant` | 범위 이탈 시 다시 추적 |
| 근접 무기 선딜/후딜 | 0.42 / 0.72 월드초 | 같은 씬의 `EnemyCombatant`, `MeleeWeapon.asset` | 선딜 중 목표 회전과 저속 추적 |
| 근접 선딜 이동 속도 배율 | 35% | 같은 씬의 `EnemyCombatant` | 충돌 안전 이동으로 플레이어를 계속 추적 |
| 근접 무기 공격 피해 | 3 | `MeleeWeapon.asset` | 플레이어 최대 체력과 같아 즉사 |
| 주먹 우선 판단 거리 | 3 | 같은 씬의 `EnemyCombatant` | 보이는 플레이어가 이 거리 안이면 무기 탐색 중단 |
| 주먹 범위/선딜/후딜 | 1.2 / 0.35 / 0.6 월드초 | 같은 씬의 `EnemyCombatant` | 빈손 공격 |
| 주먹 피해 | 1 | 같은 씬의 `EnemyCombatant` | 세 번 피격 시 플레이어 사망 |
| 적 무기 탐색 반경/주기 | 8 / 0.25 월드초 | 같은 씬의 `EnemyCombatant` | 완전한 NavMesh 경로 후보만 선택 |
| 총기 경로 허용 차이 | 2 | 같은 씬의 `EnemyCombatant` | 총기 경로가 근접 무기보다 2 이상 길면 근접 무기 선택 |
| 근접 적 회전 속도 | 260도/월드초 | 같은 씬의 `EnemyMotor` | 추격과 목표 회전 |
| 시야각 | 60도 | 같은 씬의 `VisionCone` | 전체 각도 |
| 시야 거리 | 12.5 | 같은 씬의 `VisionCone` | 장애물 전 최대 |
| 시야 메시 세그먼트 | 96 | 같은 씬의 `VisionCone` | 매 LateUpdate 재구성 |
| 부채꼴 손전등 밝기 | 7.5 | 같은 씬의 `VisionCone` | 거리 12.5, Soft Shadow 사용 |
| 원형 근거리 조명 지면 반경 | 4 | 같은 씬의 `VisionCone` | 높이를 포함해 실제 Point Light 범위를 계산 |
| 원형 근거리 조명 밝기/높이 | 4 / 플레이어 기준 1 | 같은 씬의 `VisionCone` | `ForcePixel`, Soft Shadow 강도 0.85 |
| 리플레이 캡처 | 20Hz | 같은 씬의 `StageReplayController` | 현실·월드 시간과 Deadline 활성 상태를 함께 기록 |
| Deadline 시네마틱 | 0.50배, 최소 0.8초 / 최대 2초 | 같은 씬의 `StageReplayController` | 현실 기록 길이를 이 범위로 리타이밍하고 카메라 고정 |
| Deadline 해제 후 | 0.75 월드 초, 0.50배 | 같은 씬의 `StageReplayController` | 카메라는 첫 0.2초 동안 복귀 |
| 리플레이 끝 유지 | 0.65초 | 같은 씬의 `StageReplayController` | 이후 반복 |
| 전체 시야 환경광 | Sky 0.30/0.34/0.40, Equator 0.22/0.25/0.30, Ground 0.12/0.14/0.17 | 같은 씬의 `StageReplayController` | Ambient 1, Reflection 0.35, Fog 비활성화 |
| 전체 시야 Fill Light | RGB 0.78/0.86/1, 강도 0.65, 회전 50/-30/0 | 같은 씬의 `StageReplayController` | 그림자 없는 Directional, 정적 환경 조명 유지 |
| 전체 시야 카메라 배경 | RGB 0.025/0.04/0.065 | 같은 씬의 `StageReplayController` | 전체 시야 중 기록 배경색 대신 고정 |
| 적 수 | 3명 | `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity` | 이동 연사형 2명, 근접 추격형 1명 |
| 방 크기 | 20 × 18 | `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs` | 바닥 스케일 |
| 카메라 FOV | 49도 | 같은 빌더/씬 | 원근 카메라 |
| 카메라 조준 선행 | 2.25 | 같은 씬의 `TopDownCameraController` | 조준 방향 거리 |

두 씬에서 공통 시스템 수치는 동일하다. `Stage2`의 차이는 조명/안개 수치뿐이다.

## 10. 미구현 및 개선 과제

| 과제 | 현재 상태 | 필요한 작업 | 관련 파일 | 우선순위 | 완료 조건 |
|---|---|---|---|---|---|
| 최신 작업 트리 통합 검증 | 2026-08-03 Unity 컴파일·Stage1/Stage2 생성 검증·커스텀 플레이 모드 스모크 통과. 직접 클릭 기반 조준 탄도는 확인 불가 | 바닥·벽·적·플레이어 자신 클릭, 벽 뒤 적 가림, `DEADLINE` 준비 발사의 총구 탄도를 직접 입력으로 검증 | `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs` | P0 | 현재 파일 기준 핵심 런타임 시나리오가 재현 가능하게 통과하고 변경 이력에 결과 기록 |
| 미추적 핵심 에셋 정리 | 공통 전투/근접 판정 스크립트와 근접 무기 에셋·메타가 미추적 상태 | 변경 확정 시 코드/에셋과 메타를 함께 추적하고 GUID 참조 재확인 | `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset` | P0 | `git status`에서 의도치 않은 누락이 없고 씬/에셋 참조 GUID가 정상 |
| `DEADLINE` 자동 테스트 | 씬 연결만 확인, 전용 테스트 없음 | Q 발동, 2개 제한, 해제, 쿨다운, 사망/대시·캐치 프리즈 중 중단 테스트 | `DeadlineController.cs`, `PlayerCombat.cs`, `PlayerInputReader.cs` | P1 | 정상/경계/실패 경로가 자동화되고 최신 테스트 통과 |
| 공중 가로채기 자동 테스트 | 코드·프리팹·씬은 존재, 최신 플레이 결과 없음 | 입력 버퍼, 가장 가까운 무기, 교환 드롭, 장애물 착지, 프리즈 검증 | `InterceptableWeapon.cs`, `EnemyWeaponDrop.cs`, `PlayerCombat.cs` | P1 | 가로채기와 착지 흐름이 반복 가능한 테스트로 통과 |
| 스테이지 전환/종료 흐름 | 현재 리플레이 무한 반복과 현재 씬 재시작만 가능 | `Stage1 → Stage2`, 결과 화면, 리플레이 스킵/다음 단계 정책 결정 및 구현 | `StageController.cs`, `StageReplayController.cs`, `EditorBuildSettings.asset` | P1 | 클리어 후 사용자가 정의된 다음 상태로 이동 가능 |
| Stage1/Stage2 역할 차별화 | 조명 외 동일 콘텐츠 | 학습/도전 역할 확정, 적·배치·규칙·목표 차별화 또는 단일 씬+프로필화 | 두 씬, `PrototypeSceneBuilder.cs` | P1 | 두 씬의 존재 이유가 기획과 데이터에서 명확하거나 중복이 제거됨 |
| 핵심 규칙 온보딩 | 하단 조작 텍스트 외 튜토리얼 없음 | 시간 규칙, `DEADLINE`, 투척/가로채기를 단계적으로 설명 | `GameHud.cs`, 신규 튜토리얼 시스템 | P1 | 신규 플레이어가 외부 설명 없이 핵심 루프를 수행 가능 |
| 체력 피드백 확장 | 플레이어 HP 3과 숫자 HUD, 적은 원힛 사망 | 피격 무적·체력 회복·시각/음향 피드백 및 적 HP 정책 설계 | `CombatContracts.cs`, `PlayerHealth.cs`, `EnemyHealth.cs`, `GameHud.cs` | P1 | 피해 종류와 누적 체력이 플레이·HUD·테스트에서 일관되게 확인 |
| 제품용 UI | IMGUI 디버그 HUD | Canvas/UI Toolkit 전환, 반응형 배치, 상태 우선순위, 접근성 | `GameHud.cs` | P2 | 목표 해상도에서 겹침 없이 모든 상태와 입력 장치가 표시 |
| 사운드 | 전면 미구현 | 사격·피격·대시·프리즈·클리어 이벤트와 믹서/피치 정책 구현 | `Assets/_Project/Audio`, 전투/시간/리플레이 코드 | P2 | 핵심 행동에 오디오 피드백이 있고 시간/리플레이 정책 검증 |
| 리플레이 성능·수명 관리 | 전체 렌더러 검색, 무제한 기록 | 프로파일링, 기록 상한/링 버퍼, 명시 등록, 복구/종료 경로 설계 | `StageReplayController.cs` | P2 | 목표 플레이 시간과 기기에서 메모리/프레임 예산 충족 |
| 테스트 구조화 | 커스텀 스모크 1개, Tests 폴더 비어 있음 | 런타임/에디터 asmdef와 Unity Test Framework 도입 검토 | `Assets/_Project/Tests`, `Scripts/Editor` | P2 | CI에서 단위·플레이 모드 테스트를 독립 실행 가능 |
| 게임패드·리바인딩 | 키보드/마우스만 지원 | 목표 플랫폼 확정 후 액션 바인딩, 포인터 대체, UI 아이콘 추가 | `PlayerControls.inputactions`, `GameHud.cs` | P2 | 지원 장치로 전체 루프와 메뉴 조작 가능 |
| 미사용 에셋 정리 | 스텐실 시야 에셋과 생성 이미지가 직접 참조되지 않음 | 보존 목적 확인 후 문서화/재사용/삭제 결정 | `Assets/_Project/Materials/Vision*`, `Assets/_Project/Shaders/Vision*`, `Assets/_Project/Art/Generated` | P2 | 각 에셋의 사용처가 있거나 승인된 정리 완료 |
| 데이터 중심 밸런스 | 수치가 씬과 코드에 분산 | 플레이어/적/스테이지/시간 설정 데이터화 | `PrototypeSceneBuilder.cs`, 각 런타임 컴포넌트 | P3 | 빌더 재생성 없이 데이터 에셋으로 밸런스 조정 가능 |
| 세이브/로드 | 미구현 | 게임 구조 확정 후 저장 대상과 포맷 설계 | 신규 저장 시스템 | P3 | 요구되는 진행/설정이 버전 호환 형태로 저장·복구 |
| 인벤토리/퀘스트/성장 | 관련 시스템 없음 | 제품 범위에 필요한지 결정 후 별도 기획 | 신규 시스템 | P3 | 포함 여부가 결정되고, 포함 시 데이터·UI·테스트 완료 |

## 11. 의사결정 기록

| 결정 | 확인된 내용 | 근거 |
|---|---|---|
| 3D 물리 사용 | 플레이어·적은 `Rigidbody`, 충돌은 3D Physics, 씬 검증은 `Rigidbody2D` 0개를 요구 | `PrototypeSceneBuilder.cs`, `PrototypePlayModeSmokeTest.cs` |
| 전역 시간 배율 미사용 | `Time.timeScale`은 1로 유지하고 별도 `WorldDeltaTime`을 계산 | `WorldTimeController.cs`, 스모크 테스트 |
| 플레이어와 월드 시간 분리 | 플레이어 이동은 동적 Rigidbody 속도, 대시는 `fixedUnscaledDeltaTime`, 적·투사체는 world time 사용. 전역 `Time.timeScale`은 1 유지 | 플레이어/적/전투 코드 |
| NavMesh는 경로만 담당 | AI Navigation의 베이크된 경로를 사용하되 적 Transform 자동 이동은 사용하지 않고 `EnemyMotor`가 Kinematic Rigidbody를 `WorldDeltaTime`으로 이동 | `EnemyMotor.cs`, `StageNavigation.asset` |
| 적 행동 수명주기 공통화 | 사격형과 근접형이 `EnemyBehavior`의 기절·무장 해제·사망 상태를 공유하고 `EnemyHealth`는 구체 적 유형에 의존하지 않음 | `EnemyBehavior.cs`, `EnemyHealth.cs` |
| 직접 참조 기반 조립 | 싱글턴 없이 씬 직렬화 참조와 `Configure`로 시스템 연결 | 씬과 빌더 |
| 무기 데이터 ScriptableObject화 | 권총·자동소총·샷건·근접 무기의 종류와 공격 수치, 발사 모드·펠릿 수·기본 수평 팬 각도·결정적 수평·수직 산포 최대각/시드는 `WeaponDefinition` 에셋에 저장 | `WeaponDefinition.cs`, `Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`, `MeleeWeapon.asset` |
| 팩션·인터페이스 기반 피해 | `CombatFaction`, `IDamageable`, `IStunnable`로 전투 대상 분리 | `CombatContracts.cs` |
| 적 기절은 현재 장비 드롭 | 모든 적이 기절하면 현재 무기와 남은 탄약을 공중 드롭하고, 회복 뒤 빈손 전투/재무장 판단을 재개 | `EnemyHealth.cs`, `EnemyBehavior.cs`, `EnemyCombatant.cs`, `EnemyWeaponDrop.cs` |
| 적 공격 방식은 현재 장비가 결정 | 시작 유형은 이동 속도와 시작 장비만 정하며 총기/근접 무기/빈손 공격은 공통 전투 컴포넌트가 선택 | `EnemyCombatant.cs`, `EnemyShooter.cs`, `EnemyChaser.cs` |
| 플레이어 체력 3·무기 즉사 유지 | 주먹 피해는 1, 총기와 근접 무기 피해는 3으로 설정해 주먹 세 번과 무기 한 번의 사망 규칙을 사용 | `PlayerHealth.cs`, `Pistol.asset`, `AutomaticRifle.asset`, `MeleeWeapon.asset` |
| `DEADLINE`은 Q 키 기반 토큰 하드 프리즈 | Q 키 Down으로 즉시 발동하며 탄환 존재·충돌 예측·실제 이동·입력 해제는 사용하지 않는다. 충전·재사용 대기·캐치 프리즈·사망·리플레이 제한은 유지하고, 발동 후에는 기존처럼 이동 입력으로 해제한다 | `PlayerControls.inputactions`, `PlayerInputReader.cs`, `DeadlineController.cs` |
| `DEADLINE` 회전은 최저 배율 위험을 수반 | 데드라인 전용 토큰만 활성인 상태에서 조준이 회전하면 월드 전체가 `minimumTimeScale`로 진행하고, 조준을 멈추면 0배로 완전 정지한다. 일반 하드 프리즈와 공중 가로채기 프리즈는 회전 중에도 0배를 우선한다 | `WorldTimeController.cs`, `DeadlineController.cs`, `PlayerAim.cs`, `PlayerCombat.cs` |
| `DEADLINE`은 씬당 2회 충전 스킬 | 성공 발동 시에만 1회를 차감하며, 실패·행동 준비·해제는 차감하지 않는다. 씬 재로드는 2회로 초기화하지만 리플레이는 충전을 회복하지 않고 0회일 때 Q 안내·발동을 막는다 | `DeadlineController.cs`, `GameHud.cs`, `PrototypeSceneBuilder.cs` |
| 클리어 보상은 리플레이 | 적 0명 시 전투를 끄고 시각 리플레이를 반복 | `StageController.cs`, `StageReplayController.cs` |
| 리플레이 전체 시야는 선택형 토글 | 리플레이는 기록된 암흑 시야로 시작하고 `V`로 ViewCone·동적 조명을 제거한 밝은 전체 시야를 전환한다. 반복 중 선택을 유지하고 씬 재시작 시 기본 암흑 시야로 초기화 | `PlayerControls.inputactions`, `StageController.cs`, `StageReplayController.cs`, `GameHud.cs` |
| ViewCone은 리플레이 중 재계산 | 정점 배열을 20Hz로 저장하지 않고, 기록된 보간 포즈와 현재 정적 `VisionObstacle` Raycast로 프록시 메시를 매 렌더 프레임 계산한다. 전체 시야에서는 메시를 숨기고 계산하지 않는다 | `VisionCone.cs`, `StageReplayController.cs` |
| 제한 시야와 조명 결합 | 동적 시야 메시와 60도 손전등, 양 스테이지 공통 지면 반경 4 원형광을 사용. 적 렌더러는 부채꼴·원형 시야의 합집합과 공통 장애물 Raycast로 토글하며, 원형광은 Point Light 거리 감쇠와 실시간 Soft Shadow로 부드러운 경계·장애물 차폐를 구성 | `VisionCone.cs`, `EnemyCombatant.cs`, 두 씬 |
| 에디터 빌더가 프로토타입 콘텐츠 생성 | 메뉴/배치 메서드로 씬·프리팹·머티리얼·데이터·빌드 설정을 생성 | `PrototypeSceneBuilder.cs` |
| 두 스테이지는 조명 프로필로 분리 | 동일 오브젝트/수치에 밝은 Stage1과 어두운 Stage2 프로필 적용 | 두 씬과 빌더 |

## 12. 확인이 필요한 질문

1. 공식 장르, 한 줄 소개, 세계관, 프로젝트의 최종 제품 범위는 무엇인가?
2. `Deltatime`의 핵심은 “움직일 때 시간이 흐름”, `DEADLINE`, 무기 순환 중 무엇이 최우선 기둥인가?
3. `Stage1`은 밝은 튜토리얼, `Stage2`는 암흑 시야 본편으로 의도된 것인가?
4. `Stage1` 클리어 후 `Stage2`로 자동 전환해야 하는가, 아니면 두 씬은 비교용인가?
5. 리플레이는 자동 종료, 반복, 스킵, 속도 조절 중 어떤 정책이 필요한가?
6. `DEADLINE`의 최대 준비 행동 2개와 재준비 0.35 월드초는 확정 수치인가?
7. 플레이어 시야 밖의 적이 탐지·조준·발사할 수 있는 현재 동작이 의도인가?
8. 플레이어 HP 3과 “주먹 3회/무기 1회” 규칙에 피격 무적이나 회복 수단이 필요한가?
9. 빈손 적의 3 거리 주먹 우선과 총기 경로 차이 2의 무기 선택 가중치는 확정 수치인가?
10. 공중 가로채기 시 기존 무기를 플레이어 위치에 즉시 떨어뜨리는 교환 규칙이 확정인가?
11. 무기 종류, 재장전, 탄약 공급, 드롭 확률은 어떻게 확장할 예정인가?
12. 점수, 등급, 성장, 보상, 저장, 퀘스트가 제품 범위에 포함되는가?
13. 목표 플랫폼과 지원 입력 장치는 무엇인가?
14. 사운드가 월드 시간에 맞춰 느려져야 하는지, 플레이어 행동음은 실제 시간으로 유지할지 정책이 필요한가?
15. `PrototypeSceneBuilder` 재생성을 콘텐츠 제작의 공식 워크플로로 유지할 것인가?
16. 현재 `feature/EnemyAI`의 공통 적 전투·근접 무기 미커밋/미추적 변경을 어떤 단위로 확정할 것인가?
17. CI와 자동 테스트의 필수 통과 기준은 무엇인가?

## 13. 변경 이력

| 날짜 | 문서 버전 | 변경 내용 | 관련 기능 |
|---|---:|---|---|
| 2026-08-04 | 1.3.4 | 일반 플레이어 총기 발사에서 빈 탄약 시도도 기존 발사와 같은 시간 활동을 발생시키고, 자동소총 홀드는 무기 사용 간격마다만 이를 반복하도록 보완 | 전투, 월드 시간, 입력, `DEADLINE` 분리 |
| 2026-08-03 | 1.3.3 | 권총·자동소총·샷건의 공용 발사 경로를 수평·수직 독립 결정적 산포로 확장하고 샷건 수평 팬 패턴에 펠릿별 수직 산포를 결합 | 전투, 무기 데이터, 적 AI 사격, Stage1/Stage2 정적 검증 |
| 2026-08-03 | 1.3.2 | 마우스 광선의 최근 물리 표면을 조준점으로 선택하고, 플레이어 총기·투척이 총구에서 그 조준점의 수평 좌표를 향하도록 보정 | 조준, 총기·투척 탄도, 벽 가림, `DEADLINE` 준비 발사 |
| 2026-08-02 | 1.3.1 | 권총·자동소총·샷건에 무기 시드·발사 순번·펠릿 인덱스 기반의 결정적 좌우 탄도 산포를 추가하고, 샷건 대칭 팬 패턴에 펠릿별 산포를 결합 | 전투, 무기 데이터, 적 AI 사격, Stage1/Stage2 정적 검증 |
| 2026-08-02 | 1.3.0 | 자동소총 LMB 홀드 연사, 8펠릿·18도 샷건, 빈손 플레이어 주먹, 무기별 시작 픽업과 정적 검증 범위를 반영 | 전투, 무기 데이터/픽업, 입력, HUD, Stage1/Stage2 |
| 2026-08-02 | 1.2.9 | 일반 월드 시간 재생과 Deadline 전용 현실 시간 시네마틱을 분리하고, 해제 후 슬로모션·카메라 고정·HUD 단계 표시·집중 스모크 검증을 추가 | 리플레이, DEADLINE, HUD, 스모크 테스트 |
| 2026-08-02 | 1.2.8 | 데드라인의 발동 조건을 실제 이동·임박 탄환·입력 해제에서 Q 키 Down 즉시 발동으로 전환하고, 입력·HUD·투사체 정리·Stage1/Stage2 정적 검증을 갱신 | 데드라인 입력, HUD, 투사체 |
| 2026-08-02 | 1.2.7 | 데드라인을 성공 발동 때만 차감되는 씬당 최대 2회 충전 스킬로 전환하고 HUD·Stage1/Stage2 직렬화·정적 검증에 충전 상태를 반영 | 데드라인 충전, HUD, 씬 빌더 |
| 2026-08-02 | 1.2.6 | 데드라인 전용 하드 프리즈가 조준 회전 중에만 씬의 최소 월드 배율을 허용하고, 일반·캐치 프리즈와 겹치면 완전 정지가 우선하도록 갱신 | 데드라인, 월드 시간, 동시 해방 |
| 2026-08-02 | 1.2.5 | ViewCone 정점 샘플 기록을 제거하고, 암흑 시야 리플레이 중 기록된 포즈와 정적 장애물 Raycast로 메시를 매 프레임 재계산하도록 변경 | 리플레이 메모리, ViewCone Raycast, V 전체 시야 |
| 2026-08-01 | 1.2.4 | ViewCone의 20Hz 동적 정점 녹화·보간과 `V` 리플레이 전체 시야, 시야 밖 적 몸체·장착 무기 표시 및 HUD 상태를 반영 | 리플레이 메시, 입력, 조명/안개, 적 가시성, HUD |
| 2026-08-01 | 1.2.3 | `DEADLINE` 진입을 입력 해제만으로 판단하지 않고 마지막 물리 스텝의 실제 입력 방향 변위가 0.001m 이상인 경우로 제한 | 실제 이동 판정, 벽 접촉 발동 억제, 위협 강조/HUD |
| 2026-08-01 | 1.2.2 | 적 렌더링 판정을 장애물에 가리지 않은 부채꼴 시야 또는 플레이어 주변 원형 반경 4의 합집합으로 확장 | 적 가시성, 원형 근거리 시야, 벽·엄폐물 차폐 |
| 2026-08-01 | 1.2.1 | Stage1·Stage2의 플레이어 주변 Point Light를 지면 반경 4·밝기 4·높이 1로 통일하고, 높이를 반영한 실제 범위 계산과 `ForcePixel`·Soft Shadow 강도 0.85를 반영 | 시야 조명, 벽·엄폐물 차폐, 리플레이 조명 |
| 2026-08-01 | 1.2.0 | 현재 장비 기반 공통 적 전투, 모든 적의 현재 무기 드롭·재무장, 빈손 주먹, 플레이어 HP 3·근접 무기와 `DEADLINE` 준비 공격, 새 무기 데이터와 검증 한계 반영 | 적 AI, 무기 탐색/예약, 근접 전투, 체력/HUD, 씬/밸런스 |
| 2026-07-31 | 1.1.1 | 밀착 시 근접 공격 시야 판정 수정, 선딜 중 35% 추적, 라이플 적의 공격/이동 상태 분리와 70% 후퇴 사격 반영 | 근접 공격, 후퇴 사격, 적 시야, 씬/밸런스 |
| 2026-07-31 | 1.1.0 | NavMesh 기반 적 이동, 거리 유지 자동소총 점사형 2명, 플레이어 지속 추격 근접형 1명, 공통 적 행동 수명주기와 최신 배치 스모크 결과 반영 | 적 이동, AI Navigation, 연사형, 근접 추격형, 기절/무장 해제, 씬/밸런스 |
| 2026-07-30 | 1.0.0 | 프로젝트 전체 구조, 코드, 씬, 프리팹, ScriptableObject, 입력, 설정, 패키지, 테스트 로그, Git 상태를 기준선으로 문서화 | 전체 프로젝트, 월드 시간, `DEADLINE`, 전투, 무기 가로채기, 적 AI, 시야, 리플레이, 스테이지 |

이후 기능 변경은 `Docs/FEATURE_CHANGELOG.md`에 먼저 또는 동시에 기록하고, 이 문서의 구현 현황·시스템·수치·과제·의사결정·변경 이력을 함께 갱신한다.
