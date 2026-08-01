# 기능 변경 기록

이 문서는 코드, 씬, 프리팹, ScriptableObject, 입력, UI, 밸런스, 패키지 또는 프로젝트 설정의 기능 변경을 추적한다.

## 기록 규칙

- 기능 추가, 수정, 삭제가 끝나기 전에 해당 변경을 기록한다.
- 실제 파일과 테스트 결과에서 확인된 내용만 적는다.
- 실행하지 않은 테스트는 `미실행`, 결과를 확인할 수 없으면 `확인 불가`로 적는다.
- 기획서에 영향이 있으면 `Docs/PROJECT_DESIGN_DOCUMENT.md`의 변경 위치를 구체적으로 적는다.
- 여러 기능이 독립적으로 바뀌면 날짜가 같아도 항목을 나눈다.
- 관련 파일은 저장소 루트 기준 경로로 적는다.

## YYYY-MM-DD - 기능명

- 변경 유형:
- 변경 내용:
- 영향을 받은 시스템:
- 관련 파일:
- 기획서 반영 내용:
- 테스트 결과:
- 남은 작업:

## 2026-08-02 - ViewCone 리플레이 실시간 재계산 전환

- 변경 유형: 리플레이 메모리 최적화, ViewCone 재현 방식 변경, 테스트·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController.VisualTrack`에서 ViewCone의 `DynamicMeshVertices`·정점 수·`ArrayPool<Vector3>` 기반 샘플 저장과 보간 적용을 제거했다. 대신 `VisionCone.RebuildReplayMesh(Mesh, Vector3, Quaternion)`가 기존 96방향 `VisionObstacle` Raycast 수식을 재사용해 기록된 보간 위치·회전 기준으로 프록시 메시의 정점·Bounds·Normals를 매 재생 `LateUpdate`에 갱신한다. 프록시 메시의 삼각형 토폴로지는 최초 복제 시 유지하고 `MarkDynamic`으로 갱신한다. Full View에서는 ViewCone이 숨겨진 기존 경로에서 즉시 반환하므로 Raycast·메시 계산이 발생하지 않으며, `V`로 암흑 시야를 복원하면 현재 재생 시점의 메시를 즉시 재계산한다. 20Hz 포즈 기록, 동적 조명, 반복 재생, `R` 재시작은 유지한다.
- 영향을 받은 시스템: 리플레이 샘플 메모리, ViewCone 메시/Physics Raycast, 암흑·전체 시야 토글, 리플레이 진단값, 커스텀 스모크 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.5로 갱신하고 리플레이 기록·재생 흐름, 시야 재현 방식, 성능·동적 장애물 한계, 통합 검증 과제, 의사결정과 변경 이력에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일이 `Tundra build success`와 종료 코드 0으로 완료됐으며 로그는 `ProjectDeltatime/ReplayVisionRecomputeCompile.log`다. `StageReplayController.cs`에 `DynamicMesh`·`ArrayPool`·정점 캡처 버퍼 참조가 남아 있지 않은 것, `VisionCone.RebuildReplayMesh`가 프록시 메시와 기록된 보간 포즈를 받는 것, Full View의 ViewCone 조기 숨김 경로, 갱신된 `TrackedReplayVisionConeCount` 스모크 검사 코드를 정적으로 확인했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 리플레이에서 벽·엄폐물에 따른 ViewCone 경계가 플레이 결과와 일치하는지, 97회 Raycast와 Bounds/Normals 재계산의 프레임 비용, `V` 전환 직후의 메시 복원, 반복 재생·`R` 회귀는 수동 플레이 확인이 필요하다. 현재 Stage1·Stage2의 `VisionObstacle`은 정적 벽·엄폐물이라는 전제이며, 향후 이동·생성·파괴되는 장애물이 같은 레이어에 추가되면 과거 시야와 달라질 수 있어 별도 상태 기록 또는 정책 결정이 필요하다.

## 2026-08-01 - 리플레이 ViewCone 재현 및 전체 시야 토글

- 변경 유형: 리플레이 버그 수정, 기능 추가, 입력·HUD·씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController`가 20Hz 캡처 시 `VisionCone`의 고정 삼각형 토폴로지는 프록시 생성 때 한 번만 복제하고, 동적으로 바뀌는 정점은 재사용 버퍼와 `ArrayPool<Vector3>` 대여 배열로 변경 샘플에 저장해 두 시점 사이를 보간한다. 리플레이는 기존 암흑 시야로 시작하고 `V`를 누르면 `IsOmniscientViewEnabled`를 전환해 ViewCone과 녹화된 Spot/Near Light 프록시를 숨긴다. 전체 시야는 Fog를 끄고 지정된 Trilight 환경광·반사 강도·카메라 배경과 그림자 없는 Directional Fill Light를 적용하며, 다시 `V`를 누르면 저장한 `RenderSettings`와 현재 재생 시점의 카메라·ViewCone·동적 조명을 즉시 복원한다. 적 몸체와 현재 장착 무기는 `EnemyCombatant.TryGetReplayVisibility`가 제공하는 논리 표시 상태를 실제 Renderer 가시성과 별도로 녹화해 전체 시야에서 시야 밖 생존 적을 표시하고, 사망·파괴·무장 해제 시점은 유지한다. 경고선과 일반 이펙트는 강제 표시하지 않는다. 반복 재생 중 선택 상태는 유지되고 `R` 씬 재시작 시 기본 암흑 시야로 초기화된다.
- 영향을 받은 시스템: 20Hz 시각 리플레이, ViewCone 동적 메시, 플레이어 Spot/Near Light, 전역 Fog·Ambient·Reflection, 카메라 배경, 적 몸체·장착 무기 가시성, Input System, 스테이지 상태 전달, HUD, Stage1/Stage2 직렬화, 에디터 빌더·스모크 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.4로 갱신하고 구현 현황, 리플레이·시야·HUD 동작, `V` 조작, 전체 시야 조명 수치, 확장 주의점·기술 부채·통합 검증 과제·의사결정·변경 이력에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일이 `Tundra build success`와 종료 코드 0으로 완료되었으며 로그는 `ProjectDeltatime/ReplayVisionCompile2.log`다. `PlayerControls.inputactions`의 `ReplayVisionToggle`/`<Keyboard>/v`와 생성 래퍼, `PlayerInputReader`·`StageController` 전달 경로, HUD 문자열, 두 씬의 `captureRate: 20`과 전체 시야 직렬화 값, 빌더의 동일 설정 경로를 정적으로 확인했다. 씬 빌더는 기존 씬을 재생성하지 않았다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 리플레이에서 벽·엄폐물에 따라 변한 ViewCone 경계가 잘림 없이 이어지는지, `V` 전환 순간 Fog·조명·배경과 시야 밖 적/장착 무기가 올바르게 표시되는지, 해제 시 같은 재생 시점이 복구되는지, 사망·무장 해제 타이밍과 반복·`R` 회귀의 시각 품질은 수동 플레이 확인이 필요하다. 기록 길이 상한, 매 틱 전체 Renderer 검색, 일반 색상·라인 샘플 배열 할당은 **부분 구현**인 성능 최적화 과제로 남는다.

## 2026-08-01 - 데드라인 실제 이동 판정 수정

- 변경 유형: 버그 수정, 데드라인 발동 조건 개선, 씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `PlayerMovement`가 일반 이동 입력을 적용한 마지막 물리 스텝의 Rigidbody 시작·종료 위치를 비교해 입력 방향으로 0.001m 이상 이동했을 때만 `IsPhysicallyMoving`을 공개하도록 했다. `DeadlineController`는 이 실제 이동 자격이 있던 플레이어가 이동 입력을 놓은 경우에만 위협 탄환을 검사·선점한다. 벽을 정면으로 계속 밀어 실제 변위가 없으면 탄환 강조와 `RELEASE TO DEADLINE` 안내를 지우고 입력 해제에도 발동하지 않는다. 벽을 따라 실제로 미끄러지는 이동은 인정하며, 이미 발동한 데드라인을 이동 입력으로 해제하는 기존 규칙은 유지한다.
- 영향을 받은 시스템: 플레이어 Rigidbody 이동 표본, `DEADLINE` 진입·해제, 위협 탄환 강조, HUD 안내, Stage1/Stage2 직렬화·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.3으로 갱신하고 월드 시간 및 `DEADLINE`의 현재 동작, 실제 이동 최소 변위 0.001m, 실제 이동 기반 진입 결정을 반영했다. 기존 “이동 중 정지” 의도 질문은 실제 물리 이동 후 입력 해제로 확정되어 확인 필요 목록에서 제거했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`의 Stage1/Stage2 생성 성공과 `ValidateSavedPrototypeRoom`의 두 저장 씬 검증 통과를 확인했다. 두 씬에 `minimumPhysicalDisplacement: 0.001`과 `DeadlineController.movement`의 유효한 `PlayerMovement` 참조가 직렬화된 것을 확인했다. 기존 `FindObjectOfType` 사용 중단 경고와 빌더의 머티리얼 렌더 큐·NavMeshData 이름 경고는 남아 있으나 컴파일 오류는 없다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 열린 공간 이동 후 정상 발동, 벽 정면 밀기 후 미발동·안내 제거, 벽을 따른 대각선 이동 인정, 발동 후 벽 방향 입력을 통한 해제, 대시·캐치·사망·리플레이 회귀는 사용자 플레이 확인이 필요하다.

## 2026-08-01 - 원형 근거리 적 가시성 확장

- 변경 유형: 적 렌더링 판정 개선, 문서 갱신
- 변경 내용: **구현 완료**. `VisionCone.ContainsWorldPoint(Vector3)`를 기존 부채꼴 단독 판정에서 원형 근거리 또는 부채꼴 시야의 합집합 판정으로 확장했다. 대상이 `nearLightGroundRadius` 안에 있으면 방향과 관계없이 시야 후보가 되고, 원형 밖에서는 기존 거리·각도 조건을 사용한다. 두 경우 모두 기존 `VisionObstacle` Raycast를 통과해야 최종 가시 상태가 된다. `EnemyCombatant`의 몸체·장착 무기·경고선 토글 경로는 변경하지 않아 확장된 판정이 기존 렌더링 규칙에 그대로 적용된다.
- 영향을 받은 시스템: 플레이어 시야 판정, 적 몸체·장착 무기 렌더링, 공격 경고선, 벽·엄폐물 차폐
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.2로 갱신하고 핵심 콘셉트, 구현 현황, 카메라·시야 동작, 기술 결정과 변경 이력에 부채꼴·원형 시야 합집합 기반 적 가시성을 반영했다.
- 테스트 결과: **정적 검증 통과**. 원형 반경과 부채꼴 조건이 논리합으로 결합된 뒤 공통 장애물 Raycast를 사용하는 것, `EnemyCombatant`가 `ContainsWorldPoint` 결과로 몸체·장착 무기를 토글하고 비가시 상태에서 경고선을 숨기는 기존 경로가 유지된 것을 확인했다. Stage1/Stage2의 반경 4·밝기 4·높이 1과 손전등 60도·거리 12.5·밝기 7.5도 변경되지 않았다. 사용자 요청에 따라 Unity 배치 모드 씬 검증과 플레이 모드 스모크 테스트는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 플레이에서 뒤·옆의 반경 4 적 표시, 반경 경계의 깜빡임, 벽·엄폐물 뒤 차폐와 이동·회전 중 갱신 결과는 런타임 테스트를 생략해 확인하지 않았다.

## 2026-08-01 - 플레이어 주변 원형 조명 강화

- 변경 유형: 시야 조명 개선, 씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `VisionCone`의 기존 근거리 조명을 플레이어 기준 높이 1에 배치되는 Point Light로 유지하면서, 지면 반경 4가 되도록 높이를 포함한 실제 `Light.range`를 계산하게 변경했다. 밝기는 4, 렌더 모드는 `ForcePixel`, 그림자는 Soft·강도 0.85로 설정해 거리 감쇠 경계와 `VisionObstacle` 벽·엄폐물의 실시간 그림자 차폐를 사용한다. 기존 `nearLightRange`는 `nearLightGroundRadius`로 이름을 바꾸고 `FormerlySerializedAs`를 적용했으며, Stage1과 Stage2 모두 반경 4·밝기 4·높이 1을 직렬화했다. 60도·거리 12.5·밝기 7.5의 부채꼴 손전등과 스테이지별 맵 보조광 프로필은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 시야 조명, `VisionObstacle` 벽·엄폐물 차폐, Stage1/Stage2 조명 프로필, 리플레이 조명
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.1로 갱신하고 핵심 콘셉트, 구현 현황, 카메라·시야 동작, 실제 밸런스 수치, 기술 결정과 변경 이력에 양 스테이지 공통 원형광을 반영했다.
- 테스트 결과: **정적 검증 통과**. 코드 기본값과 Stage1/Stage2의 `nearLightIntensity: 4`, `nearLightGroundRadius: 4`, `nearLightHeight: 1`이 일치하고, 두 씬의 손전등 60도·거리 12.5·밝기 7.5 및 Stage1/Stage2 맵 보조광 프로필이 유지되는 것을 확인했다. 현재 선택된 Ultra 품질의 픽셀 조명·실시간 그림자 지원과 리플레이 프록시가 Light의 범위·그림자·렌더 모드를 복제하는 경로도 정적으로 확인했다. 사용자 요청에 따라 Unity 배치 모드 씬 검증과 플레이 모드 스모크 테스트는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 플레이에서 방향과 무관한 원형 밝기, 벽 반대편 차폐, 손전등과의 밝기 대비, 이동·회전 추적, 리플레이의 위치·밝기·그림자 재현은 런타임 테스트를 생략해 확인하지 않았다. 그림자가 비활성화된 Low 이하 품질에서는 벽 차폐가 보장되지 않는다.

## 2026-08-01 - 적 무기 드롭·재무장·주먹 공격 확장

- 변경 유형: 기능 추가, 적 전투 AI 통합, 플레이어 전투/체력 확장, 무기 데이터·씬·프리팹 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 `WeaponKind(Firearm/Melee)`, 근접 범위·각도·사용 간격과 적 점사 수를 추가하고 피해 3의 `MeleeWeapon.asset`을 생성했다. `EnemyCombatant`가 현재 장비에 따라 총기 거리 유지·70% 후퇴 사격, 0.42 월드초 선딜·35% 저속 추적 근접 공격, 빈손 주먹 공격과 무기 탐색을 선택한다. 모든 적은 던진 무기에 기절하면 현재 장비/탄약을 공중 드롭하며, 회복 뒤 플레이어가 3 거리 안이면 주먹을 우선하고 그 밖에서는 반경 8의 완전한 NavMesh 경로 픽업을 0.25 월드초마다 예약·탐색한다. 장전된 총기를 우선하되 경로가 가까운 근접 무기보다 2 이상 길면 근접 무기를 고른다. 플레이어는 근접 무기를 획득·즉시 공격·투척할 수 있고 `DEADLINE`에서 방향/수치가 저장된 근접 공격을 준비할 수 있다. 플레이어 최대 체력을 3으로 변경하고 `CurrentHealth`, `HealthChanged`, HUD 체력과 `LMB Attack` 안내를 추가했다. 주먹 피해는 1, 총기/근접 무기 피해는 3이다.
- 영향을 받은 시스템: 무기 데이터/시각 표현, 플레이어 사격·근접 공격·투척·`DEADLINE`, 플레이어 체력/HUD, 적 공통 전투 상태/이동 모드, 기절·무장 해제·재무장, NavMesh 경로 길이, 바닥 픽업 예약/경쟁, 공중 무기 드롭, Stage1/Stage2 생성·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/InterceptableWeapon.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.2.0으로 갱신하고 현재 장비 기반 적 공격, 드롭/재무장/주먹/픽업 예약, 플레이어 근접 전투와 체력 3, 실제 에셋·씬 밸런스, 검증 한계를 구현 현황·시스템·콘텐츠·조작·기술 구조·수치·과제·의사결정에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 어셈블리 빌드가 `Tundra build success`로 완료되었고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 생성해 연사 시작 적 2, 근접 시작 적 1, `EnemyCombatant`/`EnemyMotor`/`EnemyPerception` 각 3, 전체 `WeaponController` 4, `EnemyWeaponDrop` 3과 NavMeshData를 검증했다. 두 씬의 `maximumHealth: 3`, 세 적의 공통 드롭 참조, `retreatMoveSpeedMultiplier: 0.7`, `windupMoveSpeedMultiplier: 0.35`, `weaponSearchRadius: 8`, `weaponSearchInterval: 0.25`, `firearmPathTolerance: 2`, 근접 시작 적의 `MeleeWeapon.asset` GUID를 정적으로 확인했다. `Pistol.asset`/`AutomaticRifle.asset`의 피해 3과 적 점사 1/4, `MeleeWeapon.asset`의 피해 3·거리 1.45·반각 35·간격 0.72를 확인했다. 최신 소스 컴파일 로그는 `ProjectDeltatime/EnemyRearmFinalCompile.log`, 씬 생성/정적 검증 로그는 `ProjectDeltatime/EnemyRearmBuild.log`다. 사용자 요청에 따라 플레이 테스트와 `PrototypePlayModeSmokeTest`는 **미실행**했으며 과거 로그를 이번 결과로 재사용하지 않았다.
- 남은 작업: **확인 불가**. 근접 무기 드롭·재획득, 시작 유형과 다른 무기 사용, 주먹 세 번 피격, 근거리 주먹 우선, 원거리 무기 탐색, 여러 적의 픽업 경쟁, 플레이어 근접 공격과 `DEADLINE` 해제 판정은 플레이/스모크 테스트를 생략했으므로 런타임 결과를 확인하지 않았다. 새 애니메이션·효과음·근접 무기 전용 모델은 **미구현**이며 기존 큐브/경고선 표현을 사용한다.

## 2026-07-31 - 근접 공격 판정 및 라이플 후퇴 사격 개선

- 변경 유형: 버그 수정, 적 AI 동작 개선, 씬 직렬화 갱신
- 변경 내용: **구현 완료**. `EnemyPerception`의 시야 원점을 무기 끝에서 적 몸체로 변경해 밀착 시 목표 Raycast가 끊기던 근접 공격 판정을 수정했다. `EnemyChaser`는 0.42 월드초 선딜 중 플레이어를 바라보며 기본 속도의 35%로 계속 추적한다. `EnemyShooter`는 공격 단계와 이동 모드를 분리해 6 거리 미만에서 플레이어를 바라보며 기본 속도의 70%로 후퇴하고, 후퇴 중에도 조준·4발 점사·쿨다운을 진행한다.
- 영향을 받은 시스템: 적 시야 판정, NavMesh 이동/회전, 근접 공격, 자동소총 조준·점사, Stage1/Stage2 직렬화
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.1.1로 갱신하고 라이플 적의 공격/이동 병렬 상태, 후퇴 속도 70%, 근접 선딜 추적 속도 35%, 몸체 기준 시야 판정과 최신 검증 한계를 반영했다.
- 테스트 결과: Unity Editor가 변경 스크립트를 컴파일해 소스 변경 시각 이후 `Library/ScriptAssemblies/Assembly-CSharp.dll`을 갱신한 것을 확인했다. `PrototypeSceneBuilder`의 사격/근접 감지 원점이 적 몸체를 사용하도록 구성된 것과 Stage1/Stage2의 사격 적 2명 `retreatMoveSpeedMultiplier: 0.7`, 근접 적 1명 `windupMoveSpeedMultiplier: 0.35`, 세 적의 몸체 Transform 기반 `sightOrigin`을 정적으로 확인했다. 사용자 요청에 따라 플레이 테스트와 `PrototypePlayModeSmokeTest`는 **미실행**했으며, 기존 로그는 이번 변경의 결과로 사용하지 않았다.
- 남은 작업: **확인 불가**. 실제 조작 중 근접 공격 적중감, 후퇴 중 조준·연사 체감과 벽에 막힌 후퇴 상황은 플레이 테스트를 생략해 확인하지 않았다.

## 2026-07-31 - NavMesh 기반 이동 연사형·지속 추격 근접형 적

- 변경 유형: 기능 추가, 적 AI 구조 개선, 씬·데이터·패키지 갱신, 회귀 검사 확장
- 변경 내용: **구현 완료**. 공통 `EnemyBehavior`, `EnemyPerception`, `EnemyMotor`를 추가해 기절/무장 해제/사망, 시야선/최근 위치, NavMesh 경로와 월드 시간 Rigidbody 이동을 분리했다. `EnemyShooter`를 6~9 거리 유지, 추적/후퇴, 0.65 월드초 조준, 자동소총 4발 점사형으로 확장했다. `EnemyChaser`는 플레이어 현재 위치를 계속 갱신해 추격하고 1.45 범위에서 0.42 월드초 선딜 후 근접 피해를 준다. 두 씬은 연사형 2명과 근접 추격형 1명으로 재구성했다.
- 영향을 받은 시스템: 적 AI, 적 체력/기절/무장 해제, 3D 물리 이동, 월드 시간, NavMesh, 총기/탄약/드롭, 씬 생성/검증, 스모크 테스트, 리플레이 기록 대상
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyPerception.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Scenes/StageNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Packages/manifest.json`, `ProjectDeltatime/Packages/packages-lock.json`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.1.0으로 갱신하고 구현 현황, 적 AI 구조, 씬 구성, 클래스 책임, 확장 주의점, 자동소총/이동/근접 공격 밸런스, 의사결정과 최신 테스트 근거를 반영했다.
- 테스트 결과: Unity 6000.1.13f1 배치 모드에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine` 통과. 텍스트 직렬화된 Stage1/Stage2와 외부 `StageNavigation.asset`을 생성하고 연사형 2, 근접 추격형 1, `EnemyMotor`/`EnemyPerception` 각 3, NavMeshSurface 1을 검증했다. 이어 `PrototypePlayModeSmokeTest` 통과. 적 누적 이동과 NavMesh 경로 획득, 근접형 추격 상태, 두 유형의 기절/회복 후 무장 해제, 사격형 2개의 중복 없는 공중 무기 드롭, 적 전멸 후 리플레이를 확인했다. 로그: `ProjectDeltatime/EnemyMovementBuild.log`, `ProjectDeltatime/EnemyMovementSmoke.log`.
- 남은 작업: **부분 구현**. 근접 무기는 시각 표현과 직접 피해만 제공하며 획득/투척/드롭 가능한 무기 데이터는 없다. 런타임 동적 NavMesh 재베이크, 전술적 엄폐 선택, 적 협동/재무장, 플레이어 시야 밖 공격 정책, 수동 플레이 기반 체감 밸런스 확인이 남았다.

## 2026-07-30 - 프로젝트 현황 기준선 문서화

- 변경 유형: 문서 추가
- 변경 내용: 현재 Unity 프로젝트의 구현 상태를 코드·에셋·설정·Git 상태 기준으로 분석하고 최초 기획서와 기능 변경 기록 양식을 생성했다.
- 영향을 받은 시스템: 문서 관리 규칙, 전체 기능 기준선
- 관련 파일: `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`, `AGENTS.md`
- 기획서 반영 내용: 프로젝트 개요, 구현 현황, 핵심 루프, 주요 시스템, 씬/콘텐츠, 플레이어 경험, 기술 구조, 밸런스, 우선순위 과제, 의사결정, 확인 질문을 1.0.0 기준으로 작성했다.
- 테스트 결과: 문서 작성 작업이므로 런타임 테스트 미실행. 기존 `ProjectDeltatime/Logs/CodexSmoke.log`의 2026-07-30 18:07 통과는 확인했으나, 22:13까지 이어진 최신 기능 변경보다 이전 결과이므로 현재 작업 트리의 최신 통과 결과는 확인 불가다.
- 남은 작업: Unity Editor를 종료할 수 있는 시점에 최신 작업 트리로 배치 스모크 테스트를 실행하고 결과를 별도 기능 항목에 기록한다.

## 2026-07-30 - 플레이어 벽 충돌 안정화

- 변경 유형: 버그 수정, 회귀 검사 추가
- 변경 내용: 일반 이동을 동적 Rigidbody의 `MovePosition` 위치 강제 이동에서 `linearVelocity` 평면 속도 제어로 변경해 벽 접촉 시 물리 보정과 위치 목표가 반복 충돌하지 않게 했다. 대시는 실제 플레이어 캡슐보다 0.03 작은 캡슐을 캐스트하고 캐스트 거리에서 스킨을 다시 빼도록 변경해, 벽에 맞닿거나 최대 0.03 겹친 시작 상태에서도 안전 거리를 0으로 제한한다. 대시 시작·종료 시 잔여 선형 속도도 제거한다.
- 영향을 받은 시스템: 플레이어 일반 이동, 대시, 3D 물리 충돌, 플레이 모드 스모크 테스트
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`의 구현 현황, 이동 및 조작, 통합 검증, 플레이어와 월드 시간 분리 설명을 현재 구현으로 갱신했다.
- 테스트 결과: Unity 6000.1.13f1 배치 모드에서 `PrototypePlayModeSmokeTest` 통과. 빈 공간의 0.5 대시 거리가 축소되지 않고, North Wall에 0.01 겹친 시작 상태의 안전 거리가 0.001 이하인지 확인했다. 로그: `ProjectDeltatime/Logs/CodexWallCollisionSmoke.log`. 기존 `FindObjectOfType` 사용에 대한 폐기 예정 경고는 있으나 컴파일 오류와 스모크 실패는 없었다.
- 남은 작업: 키보드를 길게 눌러 벽을 미는 상황의 화면상 체감과 여러 프레임의 위치 진동은 헤드리스 스모크 범위 밖이므로 Unity Editor 수동 플레이에서 최종 확인이 필요하다.
