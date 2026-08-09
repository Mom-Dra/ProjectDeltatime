# 기능 변경 기록

이 문서는 코드, 씬, 프리팹, ScriptableObject, 입력, UI, 밸런스, 패키지 또는 프로젝트 설정의 기능 변경을 추적한다.

## 기록 규칙

- 기능 추가, 수정, 삭제가 끝나기 전에 해당 변경을 기록한다.
- 실제 파일과 테스트 결과에서 확인된 내용만 적는다.
- 실행하지 않은 테스트는 `미실행`, 결과를 확인할 수 없으면 `확인 불가`로 적는다.
- 기획서에 영향이 있으면 `docs/PROJECT_DESIGN_DOCUMENT.md`의 변경 위치를 구체적으로 적는다.
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

## 2026-08-09 - Shotgun 손 모델·총구 보정값 적용

- 변경 유형: Shotgun 손 모델 및 실제 발사 총구 로컬 Transform 보정
- 변경 내용: **구현 완료**. `Shotgun.asset`의 `heldModelLocalPosition`을 `(0.044, 0.118, -0.037)`, `heldModelLocalEulerAngles`를 `(2.878, 68.211, -91.666)`로 갱신했다. `heldModelLocalScale`은 이미 일치하는 `(1, 1, 1)`을 유지했고, `heldMuzzleLocalPosition`은 `(0, 0.071, 0.92)`로 갱신했다. 총구 로컬 회전 `(0, 0, 0)`은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어·적 Shotgun 오른손 장착 시각, Shotgun `Weapon Muzzle` 위치, 투사체 생성 시작점
- 관련 파일: `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.15`로 갱신해 Shotgun 손 모델·총구 직렬화 보정값과 수동 확인 범위를 기록했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 Shotgun 손 그립·총구 축·투사체 출발점은 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Shotgun 장착 상태의 손 그립, 총구 시각 위치, 실제 투사체 생성 위치를 수동 확인한다.

## 2026-08-09 - Pistol Animator Idle 매핑 복구

- 변경 유형: Pistol Animator Override Controller 클립 참조 재연결
- 변경 내용: **구현 완료**. `Pistol.overrideController`의 기본 Idle 클립 Override를 이전 `Characters@Pistol Idle.fbx`에서 현재 `Pistol_Handgun Locomotion Pack/pistol idle.fbx`로 다시 연결했다. 전진·후진·좌·우 방향 이동 Override는 각각 현재 `pistol walk`, `pistol walk backward`, `pistol strafe`, `pistol strafe (2)` 클립을 이미 참조하고 있어 변경하지 않았고, 공용 Roll·Attack 매핑도 유지했다.
- 영향을 받은 시스템: Pistol 장착 시 Idle 및 방향 이동 Animator 프로필
- 관련 파일: `ProjectDeltatime/Assets/_Project/Animation/Pistol.overrideController`, `ProjectDeltatime/Assets/Animations/Pistol_Handgun Locomotion Pack/pistol idle.fbx`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.13`으로 갱신해 Pistol Override의 Idle·방향 이동 매핑과 수동 확인 항목을 기록했다.
- 테스트 결과: **부분 통과**. Override의 Idle GUID가 현재 `pistol idle.fbx` GUID와 일치하고, 네 방향 이동 GUID가 각 소스 FBX와 일치하는지 정적으로 확인했다. Unity Play Mode는 **미실행**이다.
- 남은 작업: Play Mode에서 Pistol 장착 후 Idle, 전진, 후진, 좌·우 이동 전환을 수동 확인한다.

## 2026-08-09 - Pistol 손 모델 최종 장착 보정

- 변경 유형: Pistol 손 모델 Position/Rotation 보정값 갱신
- 변경 내용: **구현 완료**. `Pistol.asset`의 `heldModelLocalPosition`을 `(0.08, 0.03, -0.039)`, `heldModelLocalEulerAngles`를 `(11.737, 65.521, -448.114)`, `heldModelLocalScale`을 `(0.65, 0.65, 0.65)`로 설정했다. 실제 발사 총구 로컬 보정값은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 Pistol 오른손 장착 시각, Tactical Pistol 모델 위치·회전·크기
- 관련 파일: `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`의 Pistol 손 모델 직렬화 보정값을 새 값으로 갱신했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 최종 손 그립과 총구 시각 정렬은 아직 재확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Pistol 장착 상태의 손 그립·총구 위치·조준 방향을 수동 확인한다.

## 2026-08-09 - 총기 탄환 생성 위치를 시각 총구로 통일

- 변경 유형: 총기 탄환 생성 위치 변경
- 변경 내용: **구현 완료**. `WeaponController`의 일반 발사와 `DEADLINE` 준비 발사가 기존 Player 루트 `muzzle.position` 대신 공용 `Muzzle.position`을 사용하도록 변경했다. 커스텀 시각 무기가 장착된 Pistol은 `RightHand → Weapon Aim Pivot → Held Weapon Model → Weapon Muzzle`의 실제 시각 총구에서 탄환이 생성되며, 커스텀 총구가 없으면 기존 직렬화 총구로 폴백한다.
- 영향을 받은 시스템: 플레이어 Pistol·Rifle·Shotgun 탄환 생성 위치, 총구 시각과 발사 시작점의 일치
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`에 실제 탄환 생성 위치가 `WeaponController.Muzzle` 프로퍼티를 따른다는 내용을 반영했다.
- 테스트 결과: **미실행**. 소스 참조와 `git diff --check`는 확인했으나 Unity Play Mode에서 Pistol 총구와 탄환 생성 위치가 일치하는지 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Pistol·Rifle·Shotgun의 발사 시각 위치와 투사체 출발점을 수동 확인한다.

## 2026-08-09 - 플레이어 Pistol 시각 루트 Y축 보정 제거

- 변경 유형: Pistol 장착 애니메이션의 임시 시각 회전 보정 제거, Stage1 애니메이션 스모크 원복
- 변경 내용: **구현 완료**. Pistol 장착 시에만 적용하던 `+36.1°` 시각 루트 Y축 보정, 관련 공개 상태값, 대시 회전 보정과 Stage1 스모크 검증을 제거했다. 모든 장비 프로필은 다시 기존 시각 루트 기준 회전과 대시 방향 회전만 사용한다. 게임플레이 `Player` Rigidbody 루트, 이동·조준·발사 방향, `WeaponVisualPresenter`의 총구 조준 보정은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 Pistol Idle/이동/대시 시각, Humanoid 오른손·손끝 기준축, Stage1 애니메이션 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.14`로 갱신해 Pistol 전용 Y축 보정 제거와 남은 정렬 과제를 기록했다.
- 테스트 결과: **부분 통과**. Pistol 전용 보정 필드·속성·회전 적용·스모크 단언이 제거됐고 `git diff --check`를 통과했다. Unity Play Mode 스모크는 **미실행**이다.
- 남은 작업: **계획 필요**. Pistol 손끝·몸체 forward 정렬은 시각 루트 일괄 회전 대신 기준 포즈 보정, 조준 상체 레이어 또는 IK 방식 중 하나를 정해 구현해야 한다.

## 2026-08-09 - 플레이어 몸체 전방 Debug Ray

- 변경 유형: 플레이어 방향 디버그 시각화 추가
- 변경 내용: **구현 완료**. `PlayerAim.Update`가 플레이어 루트 위치의 Y축 0.08m 위에서 `transform.forward` 방향으로 1.5m 길이의 초록색 `Debug.DrawRay`를 매 프레임 그린다. 기존 조준 `LineRenderer`는 변경하지 않아 몸체 forward와 마우스 조준선을 독립적으로 비교할 수 있다. Ray는 디버그 표시만 수행하며 이동·회전·월드 시간·무기 판정은 변경하지 않는다.
- 영향을 받은 시스템: 플레이어 조준·회전의 Scene/Game Gizmos 디버그 표시
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.11`로 갱신해 Ray 시작점·방향·길이·색상·기능 영향 범위와 검증 상태를 기록했다.
- 테스트 결과: **부분 통과**. `PlayerAim.cs`에서 초록색 Ray의 시작점·`transform.forward` 방향·1.5m 길이를 정적으로 확인했고 변경 파일의 `git diff --check`를 통과했다. Unity 배치 컴파일은 사용자 승인 거부로 **미실행**했다. 이후 .NET 정적 빌드는 Unity가 생성하는 `Temp/obj/Assembly-CSharp/project.assets.json` 부재로 시작되지 않아 컴파일 결과는 **확인 불가**다.
- 남은 작업: **확인 불가**. Play Mode에서 Scene 뷰 또는 Game 뷰의 Gizmos를 켜고, 초록색 Ray가 플레이어 몸체의 기대 전방축을 가리키는지 수동 확인이 필요하다.

## 2026-08-09 - 플레이어 총기 Aim Pivot 조준 시각 보정

- 변경 유형: 플레이어 총기 오른손 장착 계층 재구성, 마우스 조준 방향 시각 보정, Stage1 Play Mode 무기 검증 확장
- 변경 내용: **구현 완료**. `WeaponVisualPresenter`의 런타임 계층을 `RightHand → Weapon Aim Pivot → Held Weapon Model → Weapon Muzzle`로 변경했다. 기존 `WeaponDefinition`의 손 모델 Position/Rotation/Scale은 `Held Weapon Model`에 그대로 적용한다. `LateUpdate`에서 Animator가 갱신한 손 포즈 뒤에 Aim Pivot을 기본 로컬 Transform으로 되돌리고, 현재 `Weapon Muzzle.forward`와 `PlayerAim`의 조준점을 향하는 방향을 수평면에 투영해 계산한 Y축 회전만 Pivot에 적용한다. 보정은 `PlayerAim`이 있는 플레이어의 권총·자동소총·샷건에만 적용하며, 근접 무기와 적 장비에는 적용하지 않는다. `PlayerDash.IsDashing` 중에는 Pivot을 기본 회전으로 유지해 대시 방향 구르기 시각을 우선한다. `Weapon Muzzle`은 계속 실제 발사 시작점이고 `PlayerCombat`의 기존 마우스 조준점 기반 탄환 방향 계산은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 권총·자동소총·샷건 오른손 시각, 총구 수평 전방축, 구르기 시각, Stage1 무기 장착·바닥·투척·공중 드롭 자동 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.10`으로 갱신해 Aim Pivot 계층, 플레이어 총기 전용 LateUpdate 수평 조준 보정, 대시 중 해제, 탄환 판정 비변경, 검증 결과와 손/IK 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 권총·자동소총·샷건의 현재 `Weapon Muzzle`이 오른손 아래 Aim Pivot 계층에 있는지, 수평 전방축과 `PlayerAim` 방향의 각도 오차가 0.25도 이내인지, 근접 무기 Pivot이 기본 회전인지 확인했다. 기존 바닥 픽업·플레이어 투척·적 무장 해제 공중 드롭 모델과 근접 타이밍 검증도 함께 통과했다. 이어 `WeaponCalibrationSceneBuilder.ValidateFromCommandLine` 정적 검증을 통과했다. 로그: `ProjectDeltatime/AimPivotStage1Smoke.log`, `ProjectDeltatime/AimPivotWeaponCalibrationValidate.log`.
- 남은 작업: **확인 불가**. 손가락·왼손 IK는 추가하지 않았으므로 이동·공격 애니메이션 중 손의 미세한 관통과 구르기 중 실제 시각 자연스러움은 Play Mode 수동 확인이 필요하다.

## 2026-08-09 - Tactical Pistol 손·총구 수동 보정값 적용

- 변경 유형: 권총 무기 모델 Transform 보정값 갱신
- 변경 내용: **구현 완료**. `Pistol.asset`의 `heldModelLocalPosition`을 `(0.058, -0.009, -0.007)`, `heldModelLocalEulerAngles`를 `(-11.904, 73.839, 185.269)`, `heldModelLocalScale`을 `(0.65, 0.65, 0.65)`으로 저장했다. `heldMuzzleLocalPosition`은 `(0, 0.112, 0.42)`, `heldMuzzleLocalEulerAngles`는 `(0, 0, 0)`으로 저장했다. 따라서 `WeaponVisualPresenter`가 Humanoid 오른손에 생성하는 Tactical Pistol 모델과 그 내부 `Weapon Muzzle`이 해당 로컬 Transform을 사용한다.
- 영향을 받은 시스템: 플레이어/적 권총 오른손 시각, 플레이어 권총 투사체 시작점, 적 권총 경고선·사격 원점, WeaponCalibration 수동 보정
- 관련 파일: `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponCalibrationSceneBuilder.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.9`로 갱신해 실제 직렬화된 권총 손 모델·총구 로컬 Transform과 검증/수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `WeaponCalibrationSceneBuilder.ValidateFromCommandLine`이 변경된 `Pistol.asset`을 재임포트한 뒤 저장된 `WeaponCalibration` 씬을 열어 정적 검증을 통과했다. 로그: `ProjectDeltatime/WeaponCalibrationPistolPoseValidate.log`.
- 남은 작업: **확인 불가**. 정적 검증은 에셋 로드와 보정 씬 구성을 확인한다. 실제 플레이 화면에서 권총의 손 그립·총구 축이 의도와 일치하는지는 수동 확인이 필요하다.

## 2026-08-09 - 샷건 플레이어 이동 반동 제거

- 변경 유형: 샷건 밸런스 조정, 무기 데이터·씬 빌더 검증·기획 문서 갱신
- 변경 내용: **구현 완료**. `Shotgun.asset`의 `playerRecoilDistance`를 `0.35m`에서 `0m`로 변경했다. 따라서 일반 발사와 `DEADLINE` 준비 발사 해제 모두 `PlayerCombat`의 공용 반동 대기 경로를 통과하더라도 `PlayerMovement`에 이동량이 등록되지 않아 플레이어를 뒤로 밀지 않는다. `PrototypeSceneBuilder`의 생성값과 저장 데이터 검증값도 0m로 맞췄다.
- 영향을 받은 시스템: 샷건 발사, 플레이어 이동, `DEADLINE` 준비/해제 발사, 무기 데이터, Stage1/Stage2 씬 재생성·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.8`로 갱신해 샷건의 플레이어 이동 반동 0m, 일반·`DEADLINE` 해제 발사의 무이동 정책, 데이터 표와 변경 이력을 반영했다.
- 테스트 결과: **부분 통과**. Unity 6000.1.13f1 배치 컴파일 명령이 종료 코드 0으로 완료했다. `Shotgun.asset`의 직렬화 값과 `PrototypeSceneBuilder`의 생성·검증값은 모두 0m임을 정적으로 확인했다. `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`과 PlayMode 스모크는 기존 작업 트리의 저장 씬 변경을 재생성으로 덮어쓸 수 있어 **미실행**했다.
- 남은 작업: **확인 불가**. 별도 보존 지점에서 빌더·PlayMode 스모크를 실행하고, 일반 발사와 `DEADLINE` 해제 발사 후 플레이어 위치가 유지되는지 수동 확인이 필요하다.

## 2026-08-09 - 적 없는 전용 무기 보정 씬

- 변경 유형: 무기 시각 보정 전용 씬·에디터 빌더 추가, 보정 창 안내 갱신
- 변경 내용: **구현 완료**. `WeaponCalibration.unity`는 Stage1을 별도 씬으로 저장한 뒤 플레이어·카메라·공간·월드 시간·기존 무기 픽업은 유지하고, 모든 적과 `StageController`, `StageReplayController`, 레거시 `GameHud`를 제거한다. `VisionCone`은 무제한 시야가 되어 리플레이 시야 조명에 의존하지 않는다. `Build Weapon Calibration Scene`은 이 구성을 Stage1에서 다시 생성하고, `Open Weapon Calibration Scene`은 기존 씬을 열어 Player를 선택하고 무기 보정 창을 연다. 무기 손/총구/월드 모델 수치는 기존처럼 `WeaponDefinition` 에셋에 저장되므로 이 씬의 재생성과 분리된다. 보정 창의 안내도 Stage1 대신 WeaponCalibration Play Mode를 사용하도록 변경했다. 이 에디터 전용 씬은 Build Settings에 추가하지 않는다.
- 영향을 받은 시스템: 무기 모델·총구 위치 보정, 플레이어 전투/이동/Animator 수동 시험, 시야 연출, 에디터 씬 생성·정적 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/WeaponCalibration.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponCalibrationSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponModelCalibrationWindow.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.7`로 갱신해 보정 씬의 구성·메뉴·재생성 범위·Build Settings 제외 정책과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `WeaponCalibrationSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 씬 생성 뒤 정확히 한 명의 무장 플레이어·월드 시간·카메라·캐릭터 Animator, 무제한 `VisionCone`, 적/StageController/StageReplayController/GameHud 0개를 정적 검증했다. 안내 문구 수정 뒤 `ValidateFromCommandLine`으로 저장된 씬을 다시 열어 같은 정적 검증을 통과했다. 스크립트 컴파일도 두 실행에서 `Tundra build success`로 완료했다. 로그: `ProjectDeltatime/WeaponCalibrationBuild.log`, `ProjectDeltatime/WeaponCalibrationValidate.log`.
- 남은 작업: **확인 불가**. 자동 검증은 씬 구성과 참조 제거만 확인한다. 실제 Play Mode에서 각 무기의 손 그립, 총구 축, 투척/드롭 월드 모델 크기와 조작 감각은 사용자가 보정 창으로 수동 확인해야 한다.

## 2026-08-08 - 무기 모델·총구 보정 창

- 변경 유형: 무기 시각 보정 Editor 도구 추가, 실제 발사/경고선 원점 모델 총구 연동, PlayMode 회귀 검증 확장
- 변경 내용: **구현 완료**. `Tools/Prototype/Animation/Calibrate Weapon Models` Editor 창에서 Pistol·Automatic Rifle·Shotgun·Melee Weapon의 오른손 모델과 바닥/투척/공중 드롭 모델의 위치·회전·스케일, 모델 내부 실제 발사 총구의 위치·회전을 편집한다. 창은 Play Mode에서 선택 무기를 플레이어에게 즉시 장착하고 값을 변경할 때 해당 `WeaponDefinition` 에셋에 저장하며, 현재 장비 모델을 즉시 갱신한다. `WeaponVisualPresenter`는 손 모델 안에 `Weapon Muzzle` 자식을 만들고, `WeaponController`는 그 위치를 우선 총구로 사용한다. 플레이어의 탄환 시작점·조준점 방향 계산과 적의 경고선·사격 원점이 조정한 모델 총구 위치를 사용한다. 총구 회전은 모델 축/Gizmo용이며 탄환 방향은 기존 조준점/대상 방향을 유지한다. Scene Gizmos는 선택된 프레젠터의 총구 위치와 전방 축을 청록색으로 표시한다. 이후 무기 모델 빌드는 기존 손/월드/총구 보정값을 유지한다.
- 영향을 받은 시스템: 무기 ScriptableObject 보정 데이터, 플레이어/적 총기 발사 원점·조준·경고선, Humanoid 오른손 모델, 바닥 픽업·투척·공중 드롭 모델, Unity Editor 도구
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponModelCalibrationWindow.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.6`으로 갱신해 보정 창 사용 범위, `Weapon Muzzle` 우선 발사 원점, Gizmo 표시, 자동 검증과 수동 보정 필요 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildWeaponModelsFromCommandLine`이 네 무기 정의의 기본 모델 총구 오프셋을 생성했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 네 무기의 손 모델·`Weapon Muzzle` 자식·바닥·투척·공중 드롭 모델 및 Cube/Body 비활성화를 통과했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 적 사격 원점 변경 뒤 NavMesh 완전 경로 5/5와 런타임 초기화를 통과했다. 로그: `ProjectDeltatime/WeaponCalibrationBuild.log`, `ProjectDeltatime/WeaponCalibrationFinalSmoke.log`, `ProjectDeltatime/WeaponCalibrationStage6Smoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 총구 Transform 연결과 게임플레이 회귀를 확인하지만, 각 Synty 캐릭터의 손가락 그립과 사용자가 의도한 총구 축·비행 방향/크기는 수동 Play Mode에서 보정 창으로 조절해야 한다.

## 2026-08-08 - 권총·자동소총·샷건 모델 적용

- 변경 유형: 신규 무기 FBX 정규화, 무기 정의 시각 에셋 연결, 손·바닥·투척·공중 드롭 검증 확장
- 변경 내용: **구현 완료**. `Assets/MR POLY/Low Poly Weapons Set/Models`의 `Tactical Pistol.fbx`, `Assault Rifle.fbx`, `Pump Shotgun.fbx`를 각각 0.42m, 0.96m, 0.92m 길이의 `TacticalPistol.prefab`, `AssaultRifle.prefab`, `PumpShotgun.prefab`으로 정규화했다. `Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`의 held/world 모델 참조와 오프셋을 설정했으므로, `WeaponVisualPresenter`, `WeaponPickup`, `WeaponFlightVisualPresenter`가 동일 모델을 오른손, 바닥, 플레이어 투척, 적 무장 해제 공중 드롭에 사용한다. 기존 Cube는 모델을 가진 세 정의에서 숨겨진다.
- 영향을 받은 시스템: 플레이어·적 장비 시각, 바닥 무기 픽업·교환, 플레이어 무기 투척, 적 기절·무장 해제·공중 드롭/가로채기, 무기 정의
- 관련 파일: `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Tactical Pistol.fbx`, `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx`, `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Pump Shotgun.fbx`, `ProjectDeltatime/Assets/_Project/Animation/TacticalPistol.prefab`, `ProjectDeltatime/Assets/_Project/Animation/AssaultRifle.prefab`, `ProjectDeltatime/Assets/_Project/Animation/PumpShotgun.prefab`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.5`로 갱신해 세 총기 모델의 생성·참조 범위, 네 무기 자동 검증과 실제 그립/비행 방향 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildWeaponModelsFromCommandLine`이 세 프리팹과 ScriptableObject 참조를 생성했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 권총·자동소총·샷건·야구방망이마다 오른손·바닥 픽업·`ThrownWeapon`·`InterceptableWeapon`의 모델 생성과 Cube/Body 비활성화를 통과했다. 로그: `ProjectDeltatime/WeaponModelBuild.log`, `ProjectDeltatime/WeaponModelsSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 모델 연결·생성만 확인한다. Synty 손가락 그립, 무기별 손 위치/방향, 회전 중 비행 방향·크기는 사용자가 수동으로 확인한 뒤 `CharacterAnimationAssetBuilder.ConfigureFirearmWeaponVisuals`의 오프셋과 각 무기 정의의 값으로 조정해야 한다.

## 2026-08-08 - 투척·공중 드롭 무기 모델 표시

- 변경 유형: 무기 비행 시각 교체, 투척·무장 해제 공중 드롭 공통화, Stage1 PlayMode 스모크 확장
- 변경 내용: **구현 완료**. `WeaponFlightVisualPresenter`가 `WeaponDefinition.worldVisualPrefab`을 가진 무기를 비행 루트의 자식으로 생성한다. `ThrownWeapon`(플레이어 투척)과 `InterceptableWeapon`(적 기절·무장 해제 공중 드롭)은 이를 초기화 시 적용하고, 모델이 있으면 기존 Cube/Body 렌더러를 숨긴다. 따라서 `MeleeWeapon.asset`의 `BaseballBat_Raw_Wood_Clean.prefab`이 바닥 픽업뿐 아니라 플레이어가 던진 무기와 적에게서 날아온 공중 무기에도 표시된다. 월드 모델이 없는 정의는 기존 Cube fallback을 그대로 사용한다.
- 영향을 받은 시스템: 플레이어 무기 투척, 적 기절·무장 해제·공중 드롭, 공중 무기 가로채기, 무기 ScriptableObject 월드 시각
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponFlightVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.4`로 갱신해 투척·공중 드롭의 월드 모델/fallback 정책, 자동 검증 결과와 실제 비행 방향 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 야구방망이 정의로 `ThrownWeapon`과 `InterceptableWeapon`을 각각 초기화해 `Flying Weapon Model` 생성과 기존 Cube/Body 비활성화를 확인했다. 기존 오른손·바닥 모델 및 근접 타격 시점 검증도 함께 통과했다. 로그: `ProjectDeltatime/WeaponFlightSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 모델 생성과 fallback 숨김만 확인한다. 실제 플레이에서 방망이가 회전할 때의 방향·크기·궤적 체감은 수동 확인 후 필요하면 `MeleeWeapon.asset`의 world 모델 오프셋/회전/스케일을 조정해야 한다.

## 2026-08-08 - 근접 타격 프레임 동기화·야구방망이 모델 적용

- 변경 유형: 근접 피해 판정 시점 변경, 상체 Animator 레이어 동기화, 근접 무기 손/바닥 시각 에셋 교체, Stage1 스모크 확장
- 변경 내용: **구현 완료**. 플레이어의 빈손·근접 무기와 적의 빈손·근접 무기는 입력/AI 공격 시작 시 `MeleeAttackExecution`에 판정을 보류한다. 생성된 `Upper Body Attack` 레이어의 두 공격 상태는 `MeleeAttackImpactBehaviour`를 가지며 정규화 시간 0.48에서 보류된 판정을 정확히 한 번 실행한다. 하체 방향 이동 레이어는 공격 중에도 유지된다. Animator가 없는 씬은 즉시 피해를 주는 호환 경로를 유지한다. `BaseballBat_Raw_Wood(Clean)`은 길이 0.92m 기준의 `BaseballBat_Raw_Wood_Clean.prefab`으로 정규화되어 `MeleeWeapon.asset`에 연결되고, Humanoid 오른손 및 `WeaponPickup`의 바닥 표시에서 사용된다.
- 영향을 받은 시스템: 플레이어/적 근접 전투·DEADLINE 준비 근접 공격, Animator Controller/Override, 장비 시각 표시, 바닥 무기 픽업, Stage1 및 Stage3~Stage6 캐릭터 씬
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/MeleeAttackImpactBehaviour.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Animation/BaseballBat_Raw_Wood_Clean.prefab`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.6.3으로 갱신해 근접 타격 프레임, 야구방망이 오른손/바닥 표시, 자동 검증 결과와 수동 조정 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 캐릭터 26명을 구성했다. `PrototypeSceneBuilder.ValidateStage1CharacterAnimationsFromCommandLine`은 상체 레이어·실행 컴포넌트를 통과했고, `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 오른손 야구방망이·바닥 픽업 모델 생성, 타격 후 0.18초 내 무피해, 타격 후 0.85초 내 1회 피해를 통과했다. 공통 변경 이후 `Stage6PlayModeSmokeTest.RunFromCommandLine`도 NavMesh 완전 경로 5/5와 런타임 초기화를 통과했다. 로그: `ProjectDeltatime/MeleeTimingBuild.log`, `ProjectDeltatime/MeleeTimingStatic.log`, `ProjectDeltatime/MeleeTimingSmoke.log`, `ProjectDeltatime/MeleeTimingStage6Smoke.log`.
- 남은 작업: **확인 불가**. 실제 플레이에서 각 Synty 캐릭터의 손가락 그립, 방망이 방향/크기, 0.48 정규화 시점의 타격 체감은 수동 확인 후 필요하면 `ConfigureMeleeWeaponVisual`의 오프셋과 각 공격 상태의 타격 시점을 조정해야 한다.

## 2026-08-08 - 대시 방향 제자리 구르기 보정

- 변경 유형: 캐릭터 구르기 클립 보정, 플레이어 대시 시각 방향 처리, Animator 에셋 빌더 검증 강화
- 변경 내용: **구현 완료**. 원본 `Ch03_nonPBR@Stand To Roll` Humanoid 클립을 복제한 `DeltatimeRollInPlace.anim`에서 `Animator.RootT.x`와 `Animator.RootT.z` 곡선을 시작값으로 고정했다. 따라서 Root Motion을 적용하지 않는 게임플레이 캡슐과 별개로 Synty 시각 모델이 구르기 중 전진했다가 원래 위치로 되돌아오는 현상을 제거한다. `PlayerDash`는 현재 대시 방향을 공개하고, `CharacterAnimationController`는 구르기 시작 때 그 방향을 저장해 구르기 상태가 끝날 때까지 0.5초 동안 시각 루트를 실제 대시 방향으로 회전한다. 조준 방향과 좌우/후방 대시 방향이 달라도 모델은 실제 이동 방향으로 구른 뒤 조준 방향으로 복귀한다.
- 영향을 받은 시스템: 플레이어 대시 시각, 캐릭터 Animator Roll 상태, Humanoid Root Transform 곡선, Stage1·Stage3~Stage6 캐릭터 Animator 에셋
- 관련 파일: `ProjectDeltatime/Assets/_Project/Animation/DeltatimeRollInPlace.anim`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.2`로 갱신해 RootT 이동 제거, 0.5초 대시 방향 시각 정렬, 에셋 빌드 검증과 남은 수동 전이 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 `DeltatimeRollInPlace.anim` 생성과 `RootT.x/z` 상수 곡선 검증, 26명 Animator 재연결을 완료했다. 이어 `PrototypeSceneBuilder.ValidateStage1CharacterAnimationsFromCommandLine`과 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 Stage1 유효 Avatar·Animator·장비 프로필 전환을 통과했다. 로그: `ProjectDeltatime/RollFixBuild.log`, `ProjectDeltatime/RollFixStatic.log`, `ProjectDeltatime/RollFixSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 루트 이동 곡선과 Animator 초기화·전환을 확인하지만, 실제 키보드/마우스로 조준을 유지한 전진·후진·좌우 대시에서 발 미끄러짐, 회전 전환, 0.5초 유지 시간이 자연스러운지는 수동 플레이로 확인해야 한다. 권총 전용 사격, 피격·사망·무기 투척/획득 애니메이션은 계속 **미구현**이다.

## 2026-08-08 - Stage1 플레이어·적 캐릭터 Animator 적용

- 변경 유형: Stage1 캐릭터 시각·Animator 적용, Prototype 빌더·정적 검증·전용 PlayMode 스모크 추가
- 변경 내용: **구현 완료**. Stage1의 기존 플레이어 1명·원거리 적 2명·근접 적 1명 캡슐 루트를 물리·전투 권한으로 유지하면서, Party Female 01·Bartender Male 01·Bouncer Male 01·Party Male 02 Synty 프리팹을 시각 자식으로 연결했다. 시각 프리팹 Collider와 Root Motion을 끄고 `CharacterAnimationController` 및 `CharacterVisualController`를 설정해 이동·구르기·지원되는 공격·장비 교체 애니메이션과 가시성·피격 색 피드백을 전달한다. 플레이어는 청록, 원거리 적은 적색, 근접 적은 주황 역할 링으로 구분한다. `PrototypeSceneBuilder`의 Stage1+Stage2 재생성 경로에서도 Stage1 저장본에만 같은 연결을 넣으며, 현재 Stage1만 갱신하는 `Tools/Prototype/Animation/Apply Characters To Stage 1` 메뉴를 추가했다.
- 영향을 받은 시스템: Stage1 플레이어·적 시각, Humanoid Animator, 비무장/권총/소총·샷건/근접 장비 프로필, 적 월드 시간 재생 속도, 피격·가시성 피드백, Prototype 씬 빌더, 정적·PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterVisualController.cs`, `ProjectDeltatime/Assets/_Project/Animation`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.1`로 갱신해 Stage1 4명 적용, 전체 적용 수 26명, 역할 링·시각 피드백, Stage2/Tutorial 미적용 상태와 전용 검증 결과를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.ApplyStage1CharactersFromCommandLine`이 Stage1 배우 4명의 유효 Humanoid Avatar, 활성 Animator, Root Motion Off, `UnscaledTime`, 장비별 Controller, 비활성 시각 Collider, 역할 링과 `CharacterVisualController`를 검증했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 Play Mode Animator 초기화, 필수 `MoveX`/`MoveY`/`Roll`/`AttackA`/`AttackB` 파라미터, 적 `CurrentTimeScale` 재생, 플레이어 Unarmed→Pistol→Rifle→Shotgun→Melee→Pistol 전환을 확인했다. 로그: `ProjectDeltatime/Stage1CharacterAnimationBuild.log`, `ProjectDeltatime/Stage1CharacterAnimationSmoke.log`.
- 남은 작업: **미구현/확인 불가**. Stage2와 Tutorial에는 아직 Synty 캐릭터 시각·Animator를 적용하지 않았다. 권총 전용 사격, 피격·사망·무기 투척/획득 애니메이션은 기존과 같이 미구현이다. 실제 키보드/마우스로 전후좌우 이동·구르기·각 무기 교체/공격을 반복했을 때의 방향, 전이, 발 미끄러짐, 팔·프로토타입 무기 관통은 사용자의 수동 테스트가 필요하다.

## 2026-08-08 - 플레이어·적 무기 프로필 캐릭터 애니메이션

- 변경 유형: 캐릭터 Animator 신규 구현, 애니메이션 FBX 리그·루프 설정 정규화, 씬·무기 데이터·빌더·PlayMode 검증 갱신
- 변경 내용: **부분 구현**. `Assets/Animations`의 Generic FBX를 Synty 캐릭터와 호환되는 Humanoid로 재임포트하고 이동/Idle 클립은 루프, 구르기·공격 클립은 비루프로 설정했다. 공용 `DeltatimeCharacter.controller`는 `MoveX`/`MoveY` 2D 방향 Blend Tree와 `Roll`, 교대 `AttackA`/`AttackB` 상태를 가진다. Pistol/Rifle/Melee `AnimatorOverrideController`가 비무장 기본 클립을 장비 자세로 교체하고, `WeaponDefinition.animationStyle`은 Pistol=권총, Automatic Rifle·Shotgun=소총, Melee Weapon=근접 프로필을 지정한다. `CharacterAnimationController`는 플레이어의 실제 이동과 조준 기준 로컬 방향, 적의 실제 이동 방향, 대시 시작, 무기/비무장 공격 이벤트, 장비 교체를 Animator에 전달한다. Root Motion은 기존 Rigidbody/NavMesh 코드 이동과 중복되지 않도록 끄고, 적 Animator 속도는 `WorldTimeController.CurrentTimeScale`, 플레이어는 실제 시간과 하드 프리즈를 따른다. Stage3 4명, Stage4~Stage6 각 6명으로 총 22명의 Synty 플레이어·적에 적용했다.
- 영향을 받은 시스템: 플레이어 이동·대시·공격, 적 추격·후퇴·근접/총기 공격, 장비 교체·재무장, 월드 시간, Synty 캐릭터 시각, Stage3~Stage6 저장 씬·씬 빌더, Stage6 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/Animations`, `ProjectDeltatime/Assets/_Project/Animation`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationLibrary.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationEditorSetup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.0`으로 갱신하고 애니메이션 구현 상태, 장비별 프로필, Root Motion/월드 시간 정책, Stage3~Stage6 적용 범위, 검증 결과와 미구현 클립을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 FBX Humanoid 임포트, 기본 Controller와 세 Override 및 Library 생성, 총 22명 씬 연결을 완료했다. 이어 확장한 `Stage6PlayModeSmokeTest.RunFromCommandLine`이 Synty 플레이어·적 6개의 유효 Humanoid Avatar, 활성 Animator, Root Motion Off, UnscaledTime 업데이트, 필수 파라미터·클립, Unarmed/Pistol/Rifle/Melee 런타임 전환을 확인했고 기존 NavMesh 완전 경로 5/5·카메라·성능 예산·리플레이 회귀와 함께 통과했다. 로그: `ProjectDeltatime/CharacterAnimationBuild.log`, `ProjectDeltatime/CharacterAnimationSmoke.log`.
- 남은 작업: **미구현/확인 불가**. 권총 팩에 전용 사격 클립이 없어 권총 사격 중에는 이동 자세를 유지한다. 피격·사망·무기 투척/획득 애니메이션과 Stage1/Stage2/Tutorial의 Synty 캐릭터 시각 적용은 미구현이다. Pistol/Rifle 팩의 이름만으로 구분한 `strafe`/`strafe (2)` 좌우 방향, 0.16초 대시와 가속 재생한 Roll의 실제 체감, 손에 든 프로토타입 무기와 팔의 관통은 수동 플레이로 확인해야 한다.

## 2026-08-08 - 샷건 14m 최대 사거리

- 변경 유형: 샷건 투사체 사거리 제한, 무기 데이터·정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition.maximumProjectileDistance`를 추가하고, `WeaponController`가 이를 펠릿별 `Projectile.Initialize`에 전달한다. `Projectile`은 매 프레임 남은 이동 가능 거리를 계산해 해당 프레임 이동과 SphereCast 거리를 모두 제한한다. 따라서 사거리 안의 벽·적 충돌은 기존처럼 먼저 명중·제거되고, 충돌이 없으면 샷건 펠릿은 총구 기준 이동거리 14m에서 명중 플래시 없이 제거된다. 권총·자동소총·근접 무기의 값은 0m이므로 기존 공용 `Projectile.prefab`의 4 월드초 수명 규칙을 유지한다.
- 영향을 받은 시스템: 샷건 펠릿 이동·충돌·제거, 일반 발사·적 무기 재사용·`DEADLINE` 준비 발사, 무기 ScriptableObject, Stage1/Stage2 무기 정의 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.7`로 갱신해 샷건 14m 제한, 충돌 우선 순서, 다른 총기의 4 월드초 fallback, 자동 검증 범위와 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 `Tundra build success (9.81 seconds)`와 `Stage1 and Stage2 validation passed.`를 확인했다. 빌더는 샷건의 최대 사거리 14m와 권총·자동소총의 0m fallback 값을 검증한다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`은 `Prototype play-mode smoke test passed.`로 완료했다. 로그: `ProjectDeltatime/ShotgunRangeBuild.log`, `ProjectDeltatime/ShotgunRangeSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 정의값과 컴파일을 확인하지만, 실제 조작으로 14m 직전/직후 펠릿 제거, 원거리 벽 충돌 우선순위, `DEADLINE` 준비 발사와 적이 쏜 샷건의 사거리 체감은 별도 플레이 검증이 필요하다.

## 2026-08-08 - 샷건 원형 콘 산포·플레이어 반동 리팩터링

- 변경 유형: 샷건 탄도 패턴·플레이어 이동 반동 리팩터링, 무기 데이터·빌더 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponSpreadPattern`이 기존 `WeaponController`의 좌우 팬/축별 회전 계산을 대체한다. 다중 펠릿은 원형 콘 단면을 `sqrt` 반경으로 채워 면적 밀도를 균등하게 하고, 무기 시드·발사 순번으로 전체 패턴을 결정적으로 회전한다. 샷건은 8펠릿·총 퍼짐 18도(반각 9도) 안에서 펠릿별 최대 1도 반경 지터를 적용하므로 좌우 부채꼴이 아닌 발사축 중심의 원형 콘으로 퍼진다. `WeaponDefinition.playerRecoilDistance`를 추가해 샷건만 0.35m 후방 이동 반동을 사용하며 권총·자동소총·근접 무기는 0m다. `PlayerCombat`은 실제 플레이어 총기 발사 때만 반동을 대기시키고, `DEADLINE` 준비 발사는 해제 뒤에 대기 반동을 적용한다. 적 사격에는 플레이어 반동을 적용하지 않는다.
- 영향을 받은 시스템: 샷건·권총·자동소총 공용 탄도 산포, 플레이어 샷건 이동 반동, `DEADLINE` 준비/해제 발사, Stage1/Stage2 무기 데이터 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponSpreadPattern.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.6`으로 갱신해 원형 콘 산포 규칙, 샷건 반동 값과 적용 범위, 빌더 검증, 자동 검증 결과 및 수동 확인 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 `Tundra build success (6.59 seconds)`와 `Stage1 and Stage2 validation passed.`를 확인했다. 빌더는 샷건 8펠릿이 반각 9도 이내에 있고 수평·수직 양방향으로 분포하며 동일 입력에서 결정적인지 확인한다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`은 `Prototype play-mode smoke test passed.`로 완료했다. 로그: `ProjectDeltatime/ShotgunSpreadBuild2.log`, `ProjectDeltatime/ShotgunSpreadSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 산포 수학·통합 전투 흐름을 확인하지만, 실제 조작으로 0.35m 반동의 체감과 벽/경사면 근처의 이동 제한, 다양한 거리에서의 원형 펠릿 명중 분포는 별도 수동 플레이 검증이 필요하다.

## 2026-08-08 - Tutorial 공중 무기 회수 DEADLINE 진행·무제한 시야

- 변경 유형: Tutorial 진행 조건·시야 정책 개선, HUD 안내·씬 구성·PlayMode 회귀 검사·문서 갱신
- 변경 내용: **구현 완료**. 투척 수업 적의 기절·무장 해제·공중 드롭이 확인된 뒤 플레이어가 공중 `InterceptableWeapon`을 E로 잡아 어떤 무기든 보유하면, `TutorialDirector`가 즉시 `DeadlineApproach`로 진행하고 투척 수업 적을 비활성화한다. 따라서 DEADLINE 앞 Pistol 지급기는 무기를 놓친 경우의 보조 수단일 뿐 진행 필수 조건이 아니다. Tutorial의 `VisionCone`은 무제한 시야 모드로 설정되어 적 가시성 판정이 시야각·거리·장애물에 제한되지 않으며, 시야 부채꼴 오버레이와 런타임 시야 조명도 비활성화한다.
- 영향을 받은 시스템: Tutorial 투척·공중 무기 회수·DEADLINE 진입, Tutorial HUD, 적 가시성, 시야 오버레이·조명, Tutorial 씬 빌더, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.5`로 갱신해 공중 무기 회수 기반 DEADLINE 진행과 Tutorial 무제한 시야 정책을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 적의 실제 공중 `InterceptableWeapon`을 회수한 뒤 `DeadlineApproach`로 진행하는지와 Tutorial 시야 오버레이 비활성·시야 제한 밖 점의 가시성을 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: 실제 키보드/마우스로 적의 공중 무기를 E로 가로챈 직후 DEADLINE 안내·게이트가 진행되는지, 포위전 시작 전후에도 모든 적과 공간이 시야 제한 없이 보이는지 수동 확인해야 한다. 최종 입력·시각 체감은 **확인 불가**다.

## 2026-08-08 - Tutorial DEADLINE 사망 체크포인트 재시작

- 변경 유형: Tutorial 사망 재시작 흐름 개선, DEADLINE 전투 상태 복구, HUD·PlayMode 회귀 검사·문서 갱신
- 변경 내용: **구현 완료**. `TutorialDirector`는 DEADLINE 단계에서 플레이어가 사망한 상태로 R을 누르면 체크포인트 요청을 유지한 채 Tutorial 씬을 다시 로드한다. 새 `TutorialDirector`는 요청을 한 번 소비해 DEADLINE 단계로 즉시 복귀시키고, 플레이어 기본 체력, 원래 위치의 적 4명, 최대 탄약 Pistol, 최대 DEADLINE 충전, 닫힌 출구 게이트를 복구한다. DEADLINE 이외 구간의 사망과 생존 중 R은 기존처럼 Tutorial 첫 단계부터 다시 시작한다. 사망 HUD도 DEADLINE 전용 재시작 문구로 바뀐다.
- 영향을 받은 시스템: Tutorial 사망/R 재시작, DEADLINE 포위전 상태, 플레이어 무기·탄약, 적 배치, 게이트, Tutorial HUD, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.4`로 갱신해 DEADLINE 사망 시 체크포인트 재시작 범위와 자동 검증 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 최신 코드 기준으로 실행했다. 스모크는 권총을 비운 상태에서 DEADLINE 체크포인트 복구를 호출하고 DEADLINE 단계, 최대 충전, 최대 탄약 Pistol, 리셋 지점, 닫힌 출구를 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 플레이어 사망 뒤 R 입력이 씬을 다시 로드하고 해당 체크포인트를 소비하는 전체 입력 경로는 수동 확인이 필요하다. 따라서 실제 전투 체감과 사망 화면 전환은 **확인 불가**다.

## 2026-08-08 - Tutorial 게이트 소거·투척 수업 사살 방지·Pistol 회수 경로 수정

- 변경 유형: Tutorial 진행 막힘·가시성 버그 수정, 적 피해 정책 보강, PlayMode 회귀 검사 확장, 문서 갱신
- 변경 내용: **구현 완료**. 열린 `TutorialGate`는 Collider가 즉시 꺼진 뒤 상승 애니메이션의 목적지에 도달하면 Renderer도 비활성화돼 화면에서 사라진다. `TutorialDirector`는 투척 수업 적의 `EnemyHealth` 피해를 비활성화하므로 LMB Pistol 사격으로 적이 파괴되어 수업이 막히지 않는다. 안내 문구는 LMB 사격 대신 RMB Pistol 투척을 명시한다. 무기 드롭 이벤트와 생존·기절·무장 해제·무기 없음 상태가 모두 확인되면 Gate 5 - Arena Entrance를 즉시 열어 Gate 너머 Pistol 지급기 때문에 발생하던 순환 진행 조건을 제거한다.
- 영향을 받은 시스템: Tutorial 게이트 Renderer/Collider, 투척 수업 적 피해·기절·무장 해제, Pistol 회수 동선, Tutorial 안내 문구, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialGate.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.3`으로 갱신해 게이트 소거, 투척 수업 사살 방지, Gate 5 즉시 개방 규칙과 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 열린 Gate 6 Renderer 소거, 투척 수업 적의 사살 방지, 기절·무장 해제·드롭 뒤 Gate 5 개방을 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 열린 게이트의 시각적 소거, LMB 사격, RMB 투척 뒤 Gate 5 개방·Pistol 회수·DEADLINE 진입 체감을 확인하지 않았다. 최종 수동 진행 결과는 **확인 불가**다.

## 2026-08-08 - Tutorial 게이트 초기화 순서·Pistol 경로 차단 수정

- 변경 유형: 진행 경로 버그 수정, 런타임 게이트 위치 회귀 검사 추가, 문서 갱신
- 변경 내용: **구현 완료**. `TutorialDirector`의 초기 상태 적용이 `TutorialGate.Awake`보다 먼저 실행될 수 있어, 게이트가 닫힌 기준 위치를 기록하기 전에 원점으로 이동하던 문제를 수정했다. `TutorialGate.SetOpen`은 최초 호출에서 현재 로컬 좌표를 닫힌 기준으로 먼저 저장한다. 따라서 Gate 3 - Melee(`z = -1`)를 포함한 여섯 게이트가 중앙 통로에 겹치지 않으며, 근접 표적 적중 후 열린 Gate 3을 지나 Pistol 지급 위치(`z = 3`)로 이동할 수 있다.
- 영향을 받은 시스템: Tutorial 게이트 초기화, 근접→Pistol 진행 경로, 런타임 Collider 위치, Tutorial PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialGate.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.2`로 갱신해 게이트 좌표 보존 규칙과 여섯 게이트 Z 좌표 회귀 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 여섯 게이트의 원래 Z 좌표와 열린 Gate 6의 Renderer 소거를 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Gate 3을 통과해 Pistol 지급 위치까지 이동하는 체감은 확인하지 않았다. 최종 수동 동선 결과는 **확인 불가**다.

## 2026-08-08 - Tutorial 대시 출구 판정·Pistol 즉시 지급 보정

- 변경 유형: 진행 판정 버그 수정, 무기 지급 안정화, HUD 피드백·PlayMode 자동 검증 보강, 문서 갱신
- 변경 내용: **구현 완료**. 대시 출구는 조준 회전 목표를 채운 뒤 발생한 `PlayerDash.IsDashing`을 기록하고, 플레이어가 출구 트리거를 통과할 때 이 기록을 사용해 다음 단계로 진행한다. 대시가 0.16초 후 끝나 트리거 진입 프레임에는 `IsDashing == false`가 된 경우에도 이전 성공 대시가 무효화되지 않는다. `TutorialWeaponDispenser.SetAvailable(true)`는 다음 Update를 기다리지 않고 즉시 Pistol 픽업을 생성하며, Tutorial HUD 진행 문구는 `Pistol 생성됨`·`Pistol 장비 완료`를 표시한다.
- 영향을 받은 시스템: Tutorial 조준/대시 게이트, 플레이어 트리거 진행, Pistol 지급·픽업, Tutorial HUD, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialWeaponDispenser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.1`로 갱신해 대시 기록 기반 출구 판정, Pistol 즉시 생성/HUD 상태, 자동 검증과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. PlayMode 스모크는 Pistol 지급기를 활성화한 직후 생성된 픽업이 Pistol 정의를 가지는지 확인하고, 기존 월드 시간·무기 타입·투척 기절/드롭·Q `DEADLINE` 행동 2개 제한·이동 해제 회귀도 통과했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 대시 출구를 다양한 타이밍·방향에서 넘고, Pistol 생성 위치와 HUD 문구가 처음 플레이하는 사용자에게 충분히 눈에 띄는지 확인하지 않았다. 따라서 최종 입력 체감과 가시성은 **확인 불가**다.

## 2026-08-08 - 핵심 메커니즘 순차 Tutorial 씬

- 변경 유형: 신규 튜토리얼 씬·런타임 진행 시스템·HUD·전용 NavMesh·빌드 진입점·정적/PlayMode 자동 검증·기존 빌더 빌드 순서 호환·문서 갱신
- 변경 내용: **구현 완료**. 빌드 인덱스 0의 `Tutorial` 씬을 추가했다. 7단계 직선형 코스가 실제 행동 결과를 기준으로 이동/정지 월드 시간, 마우스 조준/Space 대시, E 근접 무기/LMB 적중, E Pistol/LMB 적중, RMB 투척으로 적 기절·무장 해제·공중 드롭 후 Pistol 회복, 적 4명이 사방에서 포위한 Q `DEADLINE`의 원인 2개 준비와 이동 해제, 북쪽 출구 탈출을 순서대로 해제한다. 실패한 `DEADLINE` 시도는 적·플레이어 위치와 충전을 복구하고, 성공 출구 통과 후 전투를 잠근 뒤 2초 후 Stage1을 로드한다. 사망 시 R로 Tutorial을 재시작한다. 본편 전멸 리플레이가 자체 탈출 완료를 가로채지 않도록 Tutorial의 `StageController`와 레거시 `GameHud`는 제거하고 `VisionCone` 의존성용 리플레이 컴포넌트만 보존했다. `TutorialHud`는 한국어 단계 지시·판정 진행도·월드 배율·무기/탄약·충전을 표시한다. 전역 `Time.timeScale`은 변경하지 않으며 회전 프로브·적·투사체 등 월드 진행은 기존 `WorldDeltaTime` 정책을 유지한다. `EnemyWeaponDrop`에는 드롭 결과 이벤트, `DeadlineController`에는 비활성 상태의 튜토리얼 재시도용 충전 복구 API를 추가했다.
- 영향을 받은 시스템: 플레이어 입력/이동/조준/대시/전투, 월드 시간, 무기 픽업·지급·투척, 적 기절·무장 해제·드롭, `DEADLINE`, 한국어 IMGUI HUD, 게이트/트리거 진행, NavMesh, 빌드 설정, Stage1 전환, Prototype 및 Stage3~Stage6 씬 빌더
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, 해당 `.meta`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/*`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.0`으로 갱신해 Tutorial 구현/검증 상태, 7단계 진행, 4인 포위 `DEADLINE` 연출, 씬·전환 흐름, 조작/UI/기술 클래스, 빌드 순서, 남은 사용자 검증 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`으로 직접 참조, 게이트 6개, 트리거 3개, 타입 표적 2개, 지급기 3개, 적 5명, 전용 `TutorialNavigation.asset`, 활성 카메라 1대, Layer 8 장애물, 무장 없는 시작, `Tutorial → Stage1 → … → Stage6` 빌드 순서를 검증했다. `TutorialPlayModeSmokeTest.RunFromCommandLine`은 이동/정지 월드 배율과 `WorldDeltaTime` 프로브, 근접/총기 타입 판정, 투척 기절·무장 해제·드롭, Q 바인딩, `DEADLINE` 발동·행동 2개 제한·이동 해제, `Time.timeScale == 1`을 통과했다. `Stage6SceneBuilder.ValidateStage1Through5RegressionFromCommandLine`도 Stage1~Stage5 읽기 전용 회귀를 통과했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`, `ProjectDeltatime/TutorialStageRegression.log`.
- 남은 작업: **미실행**. 사람이 처음부터 끝까지 키보드/마우스로 진행하며 각 게이트 판정 여유, 한국어 문구 가독성, 투척 무기 재획득 동선, 4인 포위전 난이도와 실패 재시도, 완료 후 Stage1 전환 연출을 확인하지 않았다. 따라서 최종 온보딩 난이도와 시각·조작 체감은 **확인 불가**다. 공중 가로채기는 안내 문구와 기존 시스템을 유지하지만 별도 필수 튜토리얼 판정 단계는 **미구현**이다.

## 2026-08-08 - Stage5 전경 Collider 조준 간섭 제거

- 변경 유형: 플레이어 조준 버그 수정, Stage5 컷어웨이 상호작용 보정, PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. `PlayerAim`은 카메라 포인터 광선의 Physics Raycast를 제거하고 플레이어 Rigidbody의 현재 Y 높이 수평 평면에 직접 투영한다. Stage5 화면 하단의 전경 가구·외벽은 `Stage5SouthExteriorCutaway`가 필요할 때 Renderer만 `ShadowsOnly`로 숨기며, Collider와 Layer 8 `VisionObstacle`은 계속 유지한다. 따라서 숨은 가구 Collider가 카메라 광선에 먼저 맞아 플레이어가 엉뚱한 방향을 바라보던 문제가 발생하지 않는다. 투사체·근접 판정·적 시야의 기존 Physics Raycast는 변경하지 않았다.
- 영향을 받은 시스템: 모든 스테이지의 마우스 조준·플레이어 회전·총구 기준 수평 발사 방향, Stage5 전경 컷어웨이, 충돌·적 시야 보존, Stage5 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.8`로 갱신해 마우스 조준을 배우 현재 높이 평면 투영으로 명시하고, 2026-08-03의 물리 표면 조준 규칙이 이 변경으로 대체됐음을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5PlayModeSmokeTest.RunFromCommandLine`을 실행해 Stage5 초기화·가구 상면 NavMesh 제외·경로·높이 이동·컷어웨이와 함께, 카메라 광선을 가로막는 임시 Collider가 있어도 조준점이 플레이어 높이 평면에 남는 회귀 검증을 통과했다. 로그: `ProjectDeltatime/Stage5AimForegroundSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Stage5 화면 하단의 가구·외벽 뒤를 조준할 때의 회전·총알 충돌·카메라 선행 체감과 Stage1~Stage6 전체 조준 체감은 자동화 범위 밖이므로 **확인 불가**다.

## 2026-08-08 - Stage5·Stage6 가구 상면 NavMesh 제외

- 변경 유형: NavMesh 베이크 버그 수정, 적 이동 경로 보정, 카메라 고도 경계 보정, 정적·PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. Stage5·Stage6 빌더는 활성 환경 Collider 중 테이블·의자·스툴·소파·부스·바/카운터·냉장고·선반·캐비닛·책상·화분·기둥·소품 등 보행 상면을 만들 수 있는 가구 소스에만 베이크 중 일시 `NavMeshModifier(area = Not Walkable, applyToChildren = false)`를 적용하고, 베이크 직후 모두 제거한다. 따라서 바닥과 의도된 계단/스텝은 유지하면서 테이블·의자 등의 상면에는 NavMesh가 생성되지 않는다. 환경 Physics Collider와 Layer 8 `VisionObstacle` 구성은 보존한다. 빌더 정적 검증과 PlayMode 스모크는 대상 가구 Collider 상단 중심에 NavMesh를 샘플할 수 없음을 확인한다. Stage6의 가구 제외 후 달라진 후보 분포에서 카메라 밖 스폰을 막기 위해 플레이어 시작 후보를 NavMesh 외곽에서 3m 안쪽으로 제한했다. 또한 `TopDownCameraController`는 Y 범위가 1m 이상인 다층 NavMesh에서 현재 포커스 고도로 화면 발자국을 계산해 상단 플랫폼의 플레이어가 화면 밖으로 밀리지 않게 했으며, 낮은 Y 범위의 Stage5는 기존 공통 평면 경계 계산을 유지한다.
- 영향을 받은 시스템: Stage5·Stage6 NavMesh 베이크와 저장 에셋, 적 추격/경로 탐색, 플레이어·적 높이 이동, 가구 충돌·시야 장애물 보존, Stage6 역할 스폰, Stage5·Stage6 탑다운 카메라 경계, 정적 검증, 전용 PlayMode 스모크. Stage1~Stage4의 NavMesh 및 카메라 경계 기본값은 변경하지 않았다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.7`로 갱신해 Stage5·Stage6 가구 상면 NavMesh 제외 정책, 임시 Modifier 제거, 충돌/시야 보존, Stage6 다층 카메라 고도 경계와 가구 상면 회귀 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 가구 소스 제외 구성을 적용한 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 실행해 가구 상단 NavMesh 부재, 6명 구성 및 완전 경로를 검증했다. 이후 최종 `TopDownCameraController` 고도 범위 보정 코드는 Unity 컴파일을 포함한 `Stage5PlayModeSmokeTest.RunFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`으로 실행해 가구 상단 샘플, 계단/플랫폼 높이 이동, 실제 Rigidbody 물리 이동, 카메라 경계, 적 경로를 함께 통과했다. 로그: `ProjectDeltatime/Stage5FurnitureNavMeshBuild.log`, `ProjectDeltatime/Stage6FurnitureNavMeshBuild.log`, `ProjectDeltatime/Stage5FurnitureNavMeshSmoke.log`, `ProjectDeltatime/Stage6FurnitureNavMeshSmoke.log`.
- 남은 작업: **미실행**. 에디터 NavMesh 시각화와 실제 키보드/마우스 조작으로 모든 가구 유형 주변의 장시간 적 추격·회피, 테이블·의자 사이의 경로 체감, 다양한 종횡비의 카메라 체감은 자동화 범위 밖이므로 **확인 불가**다.

## 2026-08-07 - Stage5·Stage6 NavMesh Rigidbody 바닥 간격 보존

- 변경 유형: 버그 수정, 이동 투영 API 보강, PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. `NavMeshGroundMovement`가 최초 유효 NavMesh 샘플에서 Rigidbody 루트와 바닥 표면의 Y 간격을 런타임에 한 번 저장하고, 이후 일반 이동·대시·적 추격의 투영 목표 Y에 더하도록 수정했다. 비활성화/재활성화하면 간격을 다시 캡처한다. 따라서 계단·단상 NavMesh 표면 좌표를 캡슐 중심에 직접 적용해 플레이어가 바닥에 관통하고 물리 보정으로 떨리던 문제가 해소된다. 기존 `TryProjectDisplacement`는 바닥 표면 좌표를 반환하는 의미를 유지하며, `TryProjectRigidbodyDisplacement`는 보정된 루트 목표를 제공한다.
- 영향을 받은 시스템: Stage5·Stage6 플레이어 일반 이동·대시, 적 NavMesh 추격, 동적 Rigidbody 캡슐의 바닥 접촉, 계단·단상 높이차 이동, PlayMode 물리 회귀 검증. `NavMeshGroundMovement`가 없는 Stage1~Stage4의 평면 이동은 변경하지 않았다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Level/NavMeshGroundMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.6`으로 갱신해 루트-NavMesh 표면 간격 보존, 비활성화 후 재캡처, 표면/루트 투영 API 구분, 실제 Rigidbody 물리 프레임 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`, `Stage5PlayModeSmokeTest.RunFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `Stage5SceneBuilder.ValidateStage1Through4RegressionFromCommandLine`, `Stage6SceneBuilder.ValidateStage1Through5RegressionFromCommandLine`을 변경 후 실행했다. Stage5·Stage6 스모크는 국소 NavMesh 이동 목표가 초기 루트-바닥 간격을 유지하는지, 실제 Rigidbody 이동 뒤 고정 물리 프레임에서 수평 이동·루트 간격·캡슐 하단의 바닥 비관통이 유지되는지를 확인해 통과했다. 로그: `ProjectDeltatime/Stage5GroundMovementBuild.log`, `ProjectDeltatime/Stage6GroundMovementBuild.log`, `ProjectDeltatime/Stage5GroundMovementFinalSmoke.log`, `ProjectDeltatime/Stage6GroundMovementFinalSmoke.log`, `ProjectDeltatime/Stage1Through4GroundMovementRegression.log`, `ProjectDeltatime/Stage1Through5GroundMovementRegression.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Stage5·Stage6의 일반 이동·대시·적 추격을 계단/단상에서 장시간 반복하는 수동 플레이와 다양한 화면 비율 체감 검증은 자동화 범위 밖이다. 따라서 장시간 조작 감각은 **확인 불가**다.

## 2026-08-07 - Stage5·Stage6 시야 방해·높이차 이동·카메라·배경 최적화

- 변경 유형: Stage5 전경 컷어웨이·Stage5/Stage6 NavMesh 높이차 이동·Stage6 카메라 근접 구도·화면 밖 차량 파티클 비활성·빌더/스모크·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. 공용 선택형 `NavMeshGroundMovement`를 추가해 Stage5·Stage6에만 연결된 플레이어 1명과 적 5명이 NavMesh 구간을 따라 XZ와 Y를 함께 이동하도록 했다. 대시와 적 추격도 같은 보정을 사용하며, 계단 고도 단차에서는 완전 NavMesh 경로의 다음 코너만 허용한다. 두 빌더는 NavMesh 베이크 뒤 실제 계단/스텝 콜라이더를 런타임 이동 차단에서만 해제하고, Rigidbody의 Y 고정은 풀되 중력은 끈다. 비활성화 수는 Stage5 `6`, Stage6 `16`이다. `Stage5SouthExteriorCutaway`는 남쪽 외벽뿐 아니라 카메라→플레이어 선분을 실제로 가리는 전경 테이블·의자·소품 Renderer만 `ShadowsOnly`로 전환하고, 가림이 해소되면 원래 Renderer 상태를 복원한다. Collider, Layer 8 `VisionObstacle`, NavMesh, 조명은 유지한다. Stage6 카메라는 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 조준 선행 `1.25`, FOV `48`, 주 연결 전투 NavMesh XZ 경계 제한으로 Stage5와 통일했다. 고도 이동 중에는 현재 NavMesh 높이를 반영해 화면 경계를 계산한다. `Background_FX`의 `FX_Background_Cars_01` 8개는 복제한 Stage6 씬에서 비활성화해 렌더링·시뮬레이션·업데이트를 중단하고, 원본 데모 및 `BackgroundCity` 계층은 보존한다.
- 영향을 받은 시스템: Stage5 남쪽/전경 가시성, Stage5·Stage6 플레이어 일반 이동·대시·적 추격, Rigidbody 제약, 계단·단상·플랫폼 NavMesh 경로, 탑다운 카메라 경계·높이 추적, Stage6 배경 파티클, Stage5/Stage6 빌더·정적 검증·플레이 모드 스모크·미리보기. `NavMeshGroundMovement`를 연결하지 않는 Stage1~Stage4는 기존 평면 이동을 유지한다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Level/NavMeshGroundMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.5`로 갱신해 Stage5 전경 컷어웨이, 양 스테이지의 계단·단상·플랫폼 높이 이동, 실제 Stage6 카메라 직렬화 값과 NavMesh 경계, 배경 차량 8개 비활성, 자동·수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`과 `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 최종 구성으로 실행해 각각 계단/스텝 Collider `6`·`16`개, 플레이어·적 `6`개의 높이 이동 구성, Stage6 FOV `48`, 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 경계 제한 On, 차량 `8`개 비활성을 정적으로 확인했다. `Stage5PlayModeSmokeTest.RunFromCommandLine`과 `Stage6PlayModeSmokeTest.RunFromCommandLine`은 숨김 Unity 에디터에서 실행해 계단 상·하단의 완전 경로·Y 이동, Stage5 남쪽/전경 컷어웨이의 Renderer 전환과 Collider·VisionObstacle 보존, 실제 도달 가능한 네 방향 NavMesh 경계의 카메라·플레이어 가시성, Stage6 차량 비활성을 확인해 통과했다. `ValidateStage1Through4RegressionFromCommandLine`과 `ValidateStage1Through5RegressionFromCommandLine`도 통과했다. `Stage5Preview.png`, `Stage6Preview.png`는 최종 씬 기준 1280×720으로 재생성해 근접 구도, 식별 원, 차량 미노출을 직접 검토했다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 양 스테이지의 일반 이동·대시·적 추격을 계단/단상 상·하단에서 장시간 왕복하고, Stage5 남쪽 전경 컷어웨이와 Stage6 근접 구도·차량 미노출을 1280×720 외 화면 비율에서도 확인해야 한다. 따라서 최종 조작 감각과 극단적 화면 비율의 연출은 **확인 불가**다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-07 - Stage5 메인 홀 정리·남쪽 외벽 컷어웨이

- 변경 유형: Stage5 환경 큐레이션·오른쪽 별관 제외·전용 NavMesh/카메라 재생성·남쪽 외벽 런타임 가시성·스모크·미리보기·문서 갱신
- 변경 내용: **구현 완료**. `Stage5SceneBuilder`가 공식 다이브 바 사본을 만든 직후 오른쪽 별관(`x ≥ 5`, `z ≥ -2.5`)의 프리팹·렌더러·국소 조명·반사 프로브를 비활성화하고 메인 홀 동쪽 경계 벽은 유지한다. NavMesh 수집 볼륨도 경계 서쪽으로 잘라 별관 콜라이더가 남아 있어도 플레이 영역에 포함되지 않게 했다. 가구는 정확히 테이블 7개와 테이블당 가까운 좌석 2개, 바 스툴 4개만 활성화해 총 좌석 18개로 고정한다. 새 `Stage5SouthExteriorCutaway`는 남쪽 NavMesh 경계에서 3.00m 안쪽에 들어오면 전면 외벽 렌더러를 `ShadowsOnly`로 전환하고 3.75m 안쪽으로 복귀하면 원래 그림자 모드를 복원한다. 이 동작은 Collider와 Layer 8 `VisionObstacle`을 변경하지 않는다. 새 메인 홀 NavMesh는 중심 `(-2.42, 0.63, 0.00)`, 크기 `(13.83, 1.08, 23.67)`이며, Stage5 카메라는 FOV `48`, 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 조준 선행 `1.25`로 다시 직렬화됐다.
- 영향을 받은 시스템: Stage5 다이브 바 환경 렌더링·가구 밀도·조명·콜라이더·시야 장애물·NavMesh·카메라 경계·남쪽 경계 가시성·플레이어/적 경로·에디터 빌더·플레이 모드 스모크·미리보기. Stage1~Stage4와 Stage6의 런타임 환경/카메라/바닥 원 외형은 유지한다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.4.4로 갱신해 Stage5의 메인 홀 전용 환경 정책, 테이블 7개·좌석 18개, 별관 제외 범위, NavMesh·카메라 실제 직렬화 값, 남쪽 외벽 컷어웨이 임계값과 충돌/시야 보존, 자동 검증과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 2회 실행해 종료 코드 0과 동일한 가구 수·별관 활성 구성·NavMesh·카메라 검증을 확인했다. 정적 검증은 활성 테이블 7개·좌석 18개, 별관 내부 활성 Renderer/Light/Collider 0개, 동쪽 경계 벽 유지, 메인 홀 NavMesh, 플레이어→적 완전 경로 5/5, 남쪽 컷어웨이 직렬화 구성을 검사한다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 동·서·남·북 카메라 경계와 함께 남쪽 접근 시 외벽 `ShadowsOnly`, 복귀 시 원래 모드 복원, VisionObstacle 콜라이더 수 불변을 확인해 통과했다. `ValidateStage1Through4RegressionFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `ValidateStage1Through5RegressionFromCommandLine`도 종료 코드 0으로 통과했다. 1280×720 `Stage5Preview.png`를 재생성해 메인 홀 가구 밀도와 별관 미노출을 직접 확인했다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 남쪽 경계까지 이동하며 외벽 컷어웨이 전환 시점, 장시간 조준·사격·픽업·투척·`DEADLINE` 중 가시성, 모든 종횡비의 카메라 체감은 아직 확인하지 않았다. 따라서 최종 조작 감각은 **확인 불가**다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-07 - Stage5 카메라 경계·전투 식별 표시 개선

- 변경 유형: Stage5 카메라 프레이밍·NavMesh 기반 화면 경계·전투 식별 바닥 원 렌더링·Stage5/Stage6 빌더 및 스모크·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. `TopDownCameraController`에 기본 비활성인 `constrainToBounds`와 `cameraBounds` 직렬화 설정을 추가했다. 일반 추적과 `SnapToTarget`은 플레이어 위치·전방 포커스·조준 선행을 합친 최종 포커스에 같은 제한 계산을 사용한다. 제한 계산은 현재 해상도 종횡비·카메라 FOV·회전에서 화면 네 모서리를 경계 지면에 투영해 XZ 범위를 구하고, 한 축의 화면 폭이 저장 경계보다 크면 그 축을 중앙에 고정한다. Stage5 빌더는 실제 NavMesh 깊이에서 높이 `깊이×0.47`, 후방 거리 `(높이-0.55)/tan(60도)`, 전방 포커스 `min(깊이×0.06, 1.5)`를 계산한다. 최종 직렬화 값은 FOV `48`, 오프셋 `(0, 11.4367, -6.2854)`, 포커스 `(0, 0, 1.46)`, 조준 선행 `1.25`, 약 60도 하향각이다. XZ 카메라 경계는 실제 NavMesh AABB와 같은 중심 `(0, -0.3333)`, 크기 `(18.6667, 24.3333)`이다. 플레이어 청록색·원거리 적 적색·추적형 적 주황색의 Stage5 전용 `Unlit/Color` 머티리얼 3개를 생성하고 여섯 바닥 원의 그림자 투사·수신, 라이트 프로브, 반사 프로브를 껐다. 깊이 판정은 유지하므로 벽과 가구에는 정상적으로 가려진다. Stage1~4의 새 경계 설정 기본값은 비활성이며, `Stage6SceneBuilder`는 Stage5에서 옮긴 카메라 제한을 끄고 기존 Stage6 역할별 링 머티리얼과 그림자 Off·프로브 Blend·모션 Object 설정을 복원한다.
- 영향을 받은 시스템: Stage5 Main Camera·탑다운 추적·현재 화면 지면 투영·NavMesh 외곽 카메라 제약·플레이어/적 역할 식별 렌더링·그림자/프로브 설정·Stage5/Stage6 씬 자동 생성·정적 및 플레이 모드 검증·Stage5 미리보기. 공개 API와 Stage1~4/Stage6 런타임 카메라·표시 외형은 유지.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Materials/Stage5PlayerMarker.mat`, `ProjectDeltatime/Assets/_Project/Materials/Stage5RangedEnemyMarker.mat`, `ProjectDeltatime/Assets/_Project/Materials/Stage5ChaserEnemyMarker.mat`, 각 머티리얼 `.meta`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/LightingData.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-0.exr`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-1.exr`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-2.exr`, 해당 생성 에셋의 `.meta`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.4.3으로 갱신해 Stage5 전용 선택형 화면 경계, FOV·오프셋·포커스·조준 선행·실제 XZ 경계 값, 역할별 Unlit 바닥 표시와 일반 깊이 가림, Stage6 복원 정책, 자동/수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 스크립트 컴파일과 최종 구성의 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 두 번 실행해 두 실행 모두 종료 코드 0과 같은 카메라·경계·여섯 링 설정 검증을 확인했다. 정적 검증은 16:9 화면 네 모서리 지면 투영, 플레이어 viewport 잔존, 여섯 링의 에셋 경로·`Unlit/Color`·역할 색상·그림자/프로브 비활성을 포함한다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 플레이어를 NavMesh 동·서·남·북에서 캐릭터 반경 0.5m 안쪽의 실제 도달 가능한 가장자리로 임시 이동시킨 뒤 카메라 지면 범위가 경계 안에 남고 플레이어 중심이 화면에 보이는지 확인하고 원래 위치를 복원했으며 종료 코드 0으로 통과했다. `ValidateStage1Through4RegressionFromCommandLine`, Stage6 `BuildAndValidateFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `ValidateStage1Through5RegressionFromCommandLine`도 각각 종료 코드 0으로 통과했다. Stage6 회귀 빌드는 렌더러 2,081, 최상위 프리팹 1,922, 콜라이더 1,017, `VisionObstacle` 277, NavMesh 정점 1,532/인덱스 2,064를 보존했다. 1280×720 `Stage5Preview.png`를 재생성해 확대된 캐릭터, 약 60도 구도, 청록/적색/주황 Unlit 원의 조명 독립 가독성, 정상 환경 가림과 외부 배경 노출 억제를 직접 확인했다. Stage6 회귀 빌드가 해당 씬을 다시 저장하면서 전용 LightingData와 반사 프로브 생성 산출물도 함께 직렬화됐다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 장시간 이동·조준·사격·픽업·투척·`DEADLINE`을 플레이하며 모든 임의 종횡비에서 카메라 경계의 체감과 표시 가독성을 확인하지 않았다. 따라서 16:9 외 극단적 화면 비율의 최종 연출은 **확인 불가**다. Synty 이동·조준·사격·근접·피격·사망 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook` 60 FPS 전용 최적화

- 변경 유형: Stage6 런타임 그림자 예산·리플레이 Renderer 탐색 제한·플레이 모드 스모크·300프레임 성능 벤치마크·미리보기·문서 갱신
- 변경 내용: **부분 구현**. `Systems`에 연결되는 `Stage6PerformanceController`를 추가했다. 저장된 공식 Rooftop 데모·프리팹·도시/조명 계층은 수정하지 않으며, Stage6 실행 중에만 `QualitySettings` 그림자 거리를 40m로 설정하고 cascade를 최대 2, 그림자 해상도를 Medium 이하로 제한한 뒤 씬 종료 시 원래 값을 복원한다. `BackgroundCity`와 그 하위 `Background_FX`/`Background_Planes` Renderer는 계속 렌더링하면서 그림자 투사·수신만 끄며, 원래 그림자가 있던 환경 Point Light 중 플레이어에 가까운 최대 2개만 0.25초마다 원래 Shadow 유형을 유지한다. 환경 포인트 라이트의 색·강도·범위·활성 상태와 반사 프로브·Global Volume·Fog·Skybox·두 Roof Layer는 그대로다. 플레이어 시야 Spot/근거리 Point Light 2개의 Soft Shadow도 유지한다. `StageReplayController`에는 opt-in 동적 루트 탐색을 추가해 기본 Stage1~5는 기존 20Hz 전수 Renderer 탐색을 유지하고, Stage6만 `Systems`, Player, 적 5, Pickup 2의 9개 직렬화 루트를 20Hz에 탐색한다. 비루트 투사체·투척 무기·드롭 Pickup은 0.25초 fallback 전수 탐색으로 등록하며 `ReplayExcluded` 정적 환경은 즉시 제외한다. `Stage6PerformanceBenchmark`는 워밍업 90프레임 뒤 300프레임 CPU/GPU 평균·p95와 구성 수를 기록한다.
- 영향을 받은 시스템: Stage6 런타임 품질 설정·URP 그림자·도시 배경 Renderer·환경 Point Light·시야 Soft Shadow·Stage6 리플레이 Renderer 등록·스모크·성능 측정. Stage1~Stage5의 리플레이 기본 경로와 저장된 Stage6 데모 환경은 변경하지 않음.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Performance/Stage6PerformanceController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PerformanceBenchmark.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.4.2로 갱신해 Stage6 전용 성능 정책, 9개 리플레이 동적 루트와 fallback, 원본 환경 보존, 스모크 구성, 실제 벤치마크 수치와 1080p 60 FPS 판정 상태를 기록했다.
- 테스트 결과: Unity 6000.1.13f1 최신 스크립트 컴파일과 `Stage6SceneBuilder.BuildAndValidateFromCommandLine` 2회 연속 실행이 종료 코드 0으로 통과했다. 두 번 모두 환경 Renderer 2,081/2,081, 최상위 프리팹 1,922/1,922, Point Light 30, 반사 프로브 4, NavMesh 정점 1,532/인덱스 2,064, 플레이어→적 완전 경로 5/5를 보존했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 환경 그림자 Point Light 최대 2, Soft Shadow 시야 라이트 2, 리플레이 동적 루트 9/fallback 0.25초와 기존 전투·NavMesh·리플레이 검증을 포함해 통과했고, `ValidateStage1Through5RegressionFromCommandLine`도 읽기 전용으로 통과했다. 1280×720 `Stage6Preview.png`를 재생성해 도시 배경·두 Roof Layer·바/라운지/통로·난간과 근거리 전투 배치가 유지되는 것을 직접 확인했다. 최신 `Stage6PerformanceBenchmark.RunFromCommandLine`은 RTX 3050 Laptop GPU에서 GPU timing을 획득했으나 배치 Game View 실제 해상도가 321×531으로 1920×1080 조건을 만들지 못했다. 300프레임 CPU 평균/p95는 40.87/77.86ms, GPU 평균/p95는 35.65/72.55ms였고, 런타임 구성은 Renderer 2,124, 환경 그림자 Point Light 2, 시야 Soft Shadow 2, 동적 루트 9, fallback 0.25초로 확인됐다. 따라서 이 비-1080p 샘플은 16.7ms도 넘으며 RTX 3050 Laptop·1080p 60 FPS 안정화는 **확인 불가**다.
- 남은 작업: **미실행**. 실제 1920×1080 Game View 또는 독립 Windows Player에서 같은 300프레임 전투 시나리오를 측정해 평균·p95 16.7ms 기준을 판정해야 한다. 실제 키보드/마우스의 장시간 이동·조준·사격·Pickup·투척·`DEADLINE` 중 그림자/카메라 체감도 **미실행**이다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook` 카메라 전투 가독성 조정

- 변경 유형: Stage6 카메라 프레이밍·정적 viewport 검증·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. `Stage6SceneBuilder`가 전체 NavMesh를 한 화면에 담기 위해 사용하던 높이·후방 거리·FOV 계산을 전방 전투 범위 42% 기준으로 교체했다. 주 연결 NavMesh의 실제 경계에서 `cameraOffset`은 `(0, 42.04, -14.29)`에서 `(0, 30.15, -10.85)`로, `cameraFocusOffset`은 `(12.92, 0.44, 18.47)`에서 `(7.10, 0.44, 10.16)`으로, FOV는 `61.8`에서 `55.0`으로 변경됐다. 카메라는 전체 먼 전장보다 플레이어와 시작–중앙 연결부 교전을 우선하며, 기존 충돌 검사·활성 Main Camera 1대·`TopDownCameraController`·`WorldTimeVisualFeedback`·Demo Skybox/Clear Flags 정책은 유지한다. 빌더는 동적 계산값과 직렬화 값 일치, FOV 상한, 플레이어의 하단 전투 viewport 위치를 검증해 전역 조감도로의 회귀를 막는다.
- 영향을 받은 시스템: Stage6 Main Camera·탑다운 추적·NavMesh 기반 프레이밍·씬 정적 검증·미리보기. 전투 배치·NavMesh·스폰·조명·리플레이 구조는 변경하지 않음.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.4.1로 갱신해 Stage6의 전투 가독성 우선 카메라 계산, 실제 직렬화 값, viewport 검증, 최신 미리보기와 구현 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 스크립트 컴파일은 첫 Stage6 빌더 실행에서 `Tundra build success`로 완료됐다. `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 2회 연속 종료 코드 0으로 실행해 새 프레이밍·환경 렌더러 2,081/2,081·NavMesh 정점 1,532/인덱스 2,064·플레이어→적 완전 경로 5/5를 확인했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 첫 직접 실행이 씬 로드 후 Unity 배치 프로세스 종료 콜백 없이 멈춰 해당 프로세스만 종료했으며, 새 배치 프로세스의 재시도는 종료 코드 0과 `Stage6 play-mode smoke test passed.`로 통과했다. Stage1~Stage5 읽기 전용 회귀와 1280×720 미리보기 재생성도 통과했고, 새 미리보기에서 플레이어·시작–중앙 교전, 바·난간·도시 배경이 함께 읽히는지 직접 검토했다. 스모크에는 원본 데모 문의 음수 스케일 `BoxCollider` 경고 1건이 있었지만 게임플레이 Error·Exception·Assert는 없었다.
- 남은 작업: **미실행**. 실제 키보드/마우스 조작으로 장시간 이동·조준·사격·픽업·투척·`DEADLINE`과 다층 난간 구간의 카메라 추적 체감은 아직 확인하지 않았다. Synty 캐릭터 애니메이션은 기존과 같이 **부분 구현**이고, Stage6 이후 자동 전환은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook`

- 변경 유형: 신규 전투 스테이지·공식 Synty 데모 씬 복제·전용 NavMesh·에디터 자동 빌더·플레이 모드 스모크·미리보기·빌드 설정·문서 추가
- 변경 내용: **구현 완료**. 공식 `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Scenes/Demo_RooftopBar_01.unity`를 Unity 씬 저장 API로 `Stage6.unity`에 복제하고 `Scene`, `Roof_Layer`, `Roof_Layer_02`, `Background_FX`, `Background_Planes`, `BackgroundCity`, `Lighting (URP)`, `Lighting (BIRP)`, `Global Volume`, 반사 프로브를 월드 변환과 원본 활성 상태를 바꾸지 않은 채 `Stage 6 - Neon Overlook` 아래에 보존했다. 환경 루트는 `ReplayExcluded`로 표시했다. 소스 데모 카메라의 Clear Flags와 배경색을 기록한 뒤 제거하고, Stage5에서 검증된 게임플레이 루트만 Additive로 이동했다. 이동 전 `Dive Bar Character` 시각 6개를 제거했으며 Stage5 환경·NavMesh·조명 데이터는 가져오거나 저장하지 않았다. 공식 데모의 활성 URP 방향광을 측정해 게임플레이 `Directional Key Light`에 적용하고 데모 방향광 컴포넌트만 비활성화했으며, 포인트 라이트 30개·반사 프로브 4개·도시 배경·안개·볼륨은 유지했다. `WorldTimeVisualFeedback`에는 `preserveSceneRenderSettings: true`, Map Fill 강도 0과 빈 위치 배열을 적용했다. 소스 데모의 `Global Volume` 프로필 GUID가 실제 에셋 없이 직렬화되어 있어, 원본과 Stage1~5를 수정하지 않고 공식 Synty 볼륨 프로필을 Stage6 전용 `Stage6VolumeProfile.asset`으로 복제해 Missing Object Reference를 제거했다. 실제 데모 콜라이더를 분석해 플레이 구역 1,017개를 유지하고 완전 차폐 구조물 277개에만 Layer 8 `VisionObstacle`을 적용했으며, 배경 7개와 작은 장식 280개의 이동 방해 콜라이더를 비활성화했다. 새 `Stage6Navigation.asset`을 Bake한 뒤 가장 큰 연결 영역에서 플레이어 1명·원거리형 3명·추적형 2명·픽업 2개를 역할별로 배치하고 `NavMesh.SamplePosition`으로 보정했다. 기존 게임플레이 캡슐에는 정확한 `Overlook Character` 시각 프리팹 6개만 자식으로 연결했으며 프리팹 Collider·Rigidbody·Animator·Root Motion은 비활성화했다. 카메라 FOV 61.8, 오프셋 `(0, 42.04, -14.29)`, 포커스 오프셋 `(12.92, 0.44, 18.47)`은 NavMesh bounds와 플레이 영역 중심에서 계산했다. 빌드 설정은 `Stage1 → Stage2 → Stage3 → Stage4 → Stage5 → Stage6` 순서이며 자동 전환은 추가하지 않았다.
- 영향을 받은 시스템: Stage6 씬·Synty 옥상 환경·URP 조명/안개/볼륨/반사·물리 충돌·Layer 8 시야 장애물·NavMesh·플레이어/적/무기 픽업·`DEADLINE`·카메라·월드 시간 시각 피드백·리플레이 정적 환경 제외·빌드 설정·에디터 자동 검증·플레이 모드 스모크·미리보기
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/Stage6VolumeProfile.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.4.0으로 갱신해 Stage6 `Neon Overlook`의 공식 `Demo_RooftopBar_01` 복제 원칙, Stage4 수제 7×7 단층 옥상과 다른 공식 다층 레이아웃, 적 5명·픽업 2개·`DEADLINE` 2회, 전용 NavMesh, 도시 배경·조명·안개·반사 프로브 보존, 정적 환경 `ReplayExcluded`, Stage1~Stage6 빌드 순서, 자동 전환 **미구현**, 캐릭터 애니메이션 **부분 구현**, 최신 자동/수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 컴파일에서 `Tundra build success`를 확인했다. `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 두 번 연속 종료 코드 0으로 실행해 멱등성을 확인했다. 소스/복제 환경 렌더러 2,081/2,081개, 최상위 프리팹 인스턴스 1,922/1,922개, 포인트 라이트 30개, 반사 프로브 4개와 필수 루트·원본 활성 상태·Missing Script/Object Reference 부재를 검증했다. 전용 NavMesh는 정점 1,532개·인덱스 2,064개이고 가장 큰 연결 전투 영역은 393개 삼각형, 고도 범위 약 2.08m이며 플레이어에서 적 5명까지 `PathComplete` 5/5와 시작 시 열린 시야선을 확인했다. 정적/스모크 검증은 플레이어 1명, 적·`EnemyMotor` 각 5개, 원거리형 3개, 추적형 2개, 픽업 2개, `DEADLINE` 2회, `CharacterVisualController`와 정확한 `Overlook Character` 이름 각 6개, 리플레이 추적 시야 조명 2개, `Replay Vision Cone` 1개, 정적 환경 리플레이 프록시 제외를 통과했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`과 `ValidateStage1Through5RegressionFromCommandLine`도 종료 코드 0으로 통과했다. 스모크 콘솔에는 원본 데모 문 오브젝트의 음수 스케일 `BoxCollider` 경고 1건이 있었지만 Error·Exception·Assert는 없었다. `Stage6Preview.png`를 1280×720으로 생성해 다층 옥상, 바·라운지·통로·난간, 플레이어/적 배치와 도시 야경이 가려지지 않는지 직접 시각 검토해 통과했다. 원본 `Demo_RooftopBar_01`과 Stage1~Stage5 저장 씬은 변경되지 않았다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 시각 프리팹으로 연결되어 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. Stage6 이후 Stage7이나 `Stage1 → … → Stage6` 자동 전환·결과 화면·리플레이 종료 흐름은 없다. **미실행**. 실제 키보드/마우스 이동·조준·사격·픽업·투척·`DEADLINE`·클리어 리플레이의 전체 수동 플레이는 실행하지 않았다. 따라서 장시간 플레이 체감, 난간/다층 경로에서의 전투 품질, 최종 캐릭터 애니메이션 품질은 **확인 불가**다.

## 2026-08-05 - Stage5 `Undertow Dive`

- 변경 유형: 신규 스테이지·공식 Synty 데모 환경 복제·전투 배치·NavMesh·카메라/환경 조명 보존·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. 공식 `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Scenes/Demo_DiveBar_01.unity`을 Unity 씬 저장 API로 `Stage5.unity`에 복제하고, 원본의 `Scene`, `Roof_Layer`, `Lighting (URP)`, 반사 프로브·볼륨 계층과 실제 건축/가구 프리팹 배치·재질·Skybox·Exp2 안개·국소 조명을 보존했다. Stage4에서는 검증된 `Systems`, `Debug HUD`, `Player`, 적 5개, 픽업 2개, `Navigation`, `Main Camera`, `Directional Key Light` 루트만 Additive 이동하고 옥상 환경과 기존 `Rooftop Character` 시각은 가져오지 않았다. 다이브 바의 바·좌석·서비스룸·기계식 황소 구역·좁은 통로가 전투선을 나누도록 플레이어 1명, 원거리형 3명, 근접형 2명, 권총·샷건 픽업 각 1개를 연결된 실내 NavMesh에 배치했다. 여섯 전투 루트에는 서로 다른 Synty 캐릭터 시각을 연결하고 프리팹 콜라이더·Animator·루트 모션을 비활성화했으며 `CharacterVisualController`의 시야·피격·기절 피드백은 유지했다. 실제 데모의 바닥·벽·계단·바·대형 가구 Physics Collider로 전용 `Stage5Navigation.asset`을 베이크했고, 작은 장식 충돌은 이동 방해에서 제외했다. 환경 루트에는 `ReplayExcluded`를 적용했다. `WorldTimeVisualFeedback`에는 기본값이 기존 Stage1~4 동작을 유지하는 씬 RenderSettings 보존 옵션을, `TopDownCameraController`에는 기본 0인 포커스 오프셋을 추가해 Stage5에서만 데모 환경 연출과 NavMesh 중심 구도를 유지했다. 빌드 설정은 `Stage1 → Stage2 → Stage3 → Stage4 → Stage5` 순서다. 원본 데모와 Stage1~4 씬은 저장하거나 재생성하지 않았다.
- 영향을 받은 시스템: Stage5 씬/빌드 설정, Synty 환경·캐릭터 시각, NavMesh 경로 탐색, 실제 구조물 충돌·Layer 8 `VisionObstacle`, 플레이어/적/픽업·`DEADLINE`, 탑다운 카메라, 월드 시간 시각 피드백, 환경 조명·안개, 리플레이 정적 환경 제외, 에디터 생성/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.7로 갱신해 Stage5 `Undertow Dive`의 **구현 완료** 상태, 공식 데모 복제/보존 원칙, 실제 공간 기반 전투 구성, 전용 NavMesh 경계·스폰·카메라 FOV, 환경 조명 보존, 정적 환경 리플레이 제외, 다섯 씬 빌드 순서, 자동 전환 **미구현**, 캐릭터 애니메이션 **부분 구현**, 자동/수동 검증 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 최신 스크립트 컴파일이 `Tundra build success`로 완료됐다. `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 2회 연속 실행해 멱등성을 확인했으며, 데모 환경 렌더러 1,400개 이상, 활성 카메라 1대, 플레이어 1명, 적/모터 각 5개(원거리형 3/근접형 2), 픽업 2개, `DEADLINE` 2회, Synty 시각/컨트롤러 각 6개, 실제 구조물 `VisionObstacle`, Missing Script/Object Reference 부재, 빌드 순서를 정적으로 검증했다. 전용 NavMesh는 정점 635·인덱스 909이며 플레이어와 적 5명의 스폰 샘플 및 모든 플레이어→적 경로가 `PathComplete`이고 초기 시야선 1개 이상이 열려 있음을 확인했다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 공통 시스템 초기화·생존 적 5명·리플레이 시야 조명 2개·정적 환경 제외·전용 NavMesh 참조·6개 정확한 캐릭터 이름을 확인하고 `Stage5 play-mode smoke test passed.`로 완료됐다. `Stage5SceneBuilder.ValidateStage1Through4RegressionFromCommandLine`은 Stage1/Stage2, Stage3, Stage4 저장 씬 검증을 모두 통과했다. `Stage5Preview.png`는 1280×720으로 생성한 뒤 바·좌석·서비스룸·기계식 황소 구역과 전투 배치가 한 화면에 들어오는지 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 완화 정적 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3 → Stage4 → Stage5` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **미실행**. 실제 키보드/마우스 이동·조준·사격·대시·픽업·투척·`DEADLINE`·적 전멸 조작은 자동화하지 않았다. 따라서 체감 난이도, 구조물 모서리에서의 실제 충돌 감각, 클리어 리플레이 최종 시각 품질은 **확인 불가**다. 작업 시작 전부터 수정된 `Demo_DiveBar_01/LightingData.asset`과 `Demo_NightClub_01/LightingData.asset`의 의도는 **확인 불가**이며 이번 변경과 분리해 보존했다.

## 2026-08-05 - Stage4 `Last Call Rooftop`

- 변경 유형: 신규 스테이지·Synty 환경/캐릭터 콘텐츠·NavMesh·리플레이 시각 최적화·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. Stage3 씬·NavMesh·빌더를 참조하거나 변경하지 않고 Stage2의 공통 런타임 연결만 임시 기반으로 사용해 `Last Call Rooftop`을 추가했다. `ProjectDeltatime/Assets/Synty/PolygonNightclubs`의 바닥 모듈, 난간, 바, 소파, 야외 테이블, 화분, 화로와 조명을 사용해 7×7 옥상 테라스를 구성했다. 플레이어는 남쪽 입구에서 시작하며 서쪽 서비스 카운터·동쪽 라운지·북쪽 바·중앙 테이블 엄폐를 기준으로 이동 연사형 3명과 근접 추격형 2명을 배치했다. 권총·샷건 픽업 각 1개와 씬당 `DEADLINE` 2회를 유지했고, 카메라 FOV는 56도다. Synty 캐릭터 6개는 기존 검증된 게임플레이 캡슐의 시각 자식으로 두고 `CharacterVisualController`로 시야 가시성·피격·기절 색을 동기화했다. 정적 환경 루트에는 `ReplayExcluded`를 적용해 리플레이 프록시 추적에서 제외했으며, 플레이어·적·픽업·시야 조명 기록은 유지한다. 전용 `Stage4Navigation.asset`을 베이크하고 빌드 설정의 인덱스 3에 Stage4를 등록했다.
- 영향을 받은 시스템: 씬/빌드 설정, NavMesh 경로 탐색, 플레이어·적 Synty 시각 피드백, 제한 시야 장애물, 환경 조명, 카메라, 픽업·`DEADLINE`·리플레이 초기화, 리플레이 렌더러 추적, 에디터 콘텐츠 빌드/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage4Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterVisualController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayExcluded.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.6으로 갱신해 Stage4의 독립 생성 원칙, 옥상 전투 공간 분석, 씬/오브젝트/콘텐츠 구조, 빌드 순서, Layer·NavMesh·리플레이 제외 정책, 실제 직렬화 수치, 구현 상태, 자동 전환 미구현, 시각 애니메이션 한계와 검증 근거를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `Stage4SceneBuilder.BuildAndValidateFromCommandLine`이 최신 스크립트 컴파일과 씬 루트·공통 시스템·적 5명(이동 연사형 3/근접형 2)·픽업 2개·`DEADLINE` 2회·전용 NavMesh·Synty 프리팹/시각 6개·환경 조명 4개·`VisionObstacle` 13개·빌드 순서를 정적으로 검증해 종료 코드 0으로 완료됐다. `Stage4PlayModeSmokeTest.RunFromCommandLine`은 플레이어 생존, 월드 시간/스테이지/리플레이 초기화, 적/모터 각 5개, 픽업 2개, 리플레이 등록 시야 조명 2개, 캐릭터 시각 6개, 플레이어와 적 5명의 NavMesh 스폰, 정적 환경의 리플레이 추적 제외를 확인하고 `Stage4 play-mode smoke test passed.`로 완료됐다. `Stage4Preview.png`는 생성 후 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 완화 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3 → Stage4` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **확인 불가**. 실제 키보드/마우스 전투 감각, 옥상 난간·라운지 엄폐에서 적 경로/사격 압박, 적 전멸과 클리어 리플레이의 최종 시각 품질은 수동 플레이 검증이 필요하다. **확인 필요**. 배치 스모크 종료 뒤 기존 `WorldTimeVisualFeedback.OnValidate`의 Map Fill Light 생성 중 Unity 진단이 출력되었으나 스모크 어설션은 통과했으므로 별도 원인 확인이 필요하다.

## 2026-08-05 - Stage3 `Afterimage Club`

- 변경 유형: 신규 스테이지·Synty 환경/캐릭터 콘텐츠·NavMesh·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. `ProjectDeltatime/Assets/Synty/PolygonNightclubs`의 모듈형 바닥·벽, 바, DJ 부스, 대형 스피커, 소파, 테이블, 의자, 디스코볼과 무대 조명을 사용해 `Stage3`를 추가했다. 게임의 제한 시야와 행동량 기반 월드 시간에 맞춰 중앙 댄스 플로어는 개방 교전 공간, 서쪽 바는 긴 사격선과 연속 엄폐, 동쪽 라운지는 짧게 끊기는 엄폐, 북쪽 DJ 부스는 근접 압박 지점으로 구성했다. 플레이어는 남쪽에서 시작하고 서쪽·동쪽에 이동 연사형 2명, 북쪽 중앙에 근접 추격형 1명을 배치했으며 권총·샷건 픽업 각 1개와 씬당 `DEADLINE` 2회를 유지했다. Synty Party Female 01, Bartender Male, Bouncer Male, Party Male 02를 기존 검증된 게임플레이 캡슐의 시각 자식으로 연결하고 프리팹 콜라이더·루트 모션을 비활성화했다. 마젠타·시안·바이올렛·블루 환경 포인트 조명 4개와 FOV 52 카메라를 적용했다. 독립 `Stage3SceneBuilder`가 Stage1/Stage2를 재생성하지 않고 Stage3와 전용 `Stage3Navigation.asset`만 관리하며, 빌드 설정에는 Stage1·Stage2 다음 순서로 Stage3를 등록한다.
- 영향을 받은 시스템: 씬/빌드 설정, NavMesh 경로 탐색, 플레이어·적 시각, 제한 시야 장애물, 환경 조명, 카메라, 픽업·`DEADLINE`·리플레이 초기화, 에디터 콘텐츠 빌드/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage3Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3PlayModeSmokeTest.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `Docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.5로 갱신해 Stage3의 전투 공간 분석, 씬/오브젝트/콘텐츠 구조, 빌드 순서, Layer·NavMesh 정책, 실제 직렬화 수치, 구현 상태, 미구현 전환 흐름, 캐릭터 애니메이션 한계와 검증 근거를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 최신 스크립트 컴파일과 `Stage3SceneBuilder.ValidateSavedStage3`가 종료 코드 0으로 완료됐고, 씬 루트·공통 시스템·적 3명(이동 연사형 2/근접형 1)·픽업 2개·`DEADLINE` 2회·전용 NavMesh·Synty 프리팹과 캐릭터 렌더러·환경 조명 4개를 정적으로 검증했다. `Stage3PlayModeSmokeTest`는 플레이어 생존, 월드 시간/스테이지/리플레이 초기화, 적/모터 각 3개, 픽업 2개, 리플레이 등록 시야 조명 2개, 캐릭터 시각 4개, 플레이어와 적 3명의 NavMesh 스폰을 확인하고 `Stage3 play-mode smoke test passed.`로 완료됐다. 기존 `PrototypeSceneBuilder.ValidateSavedPrototypeRoom`도 Stage1/Stage2를 재생성하지 않은 채 종료 코드 0과 `Stage1 and Stage2 validation passed.`를 확인했다. `Stage3Preview.png`는 생성 후 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 완화 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **확인 불가**. 실제 키보드/마우스 전투 감각, 바·라운지 엄폐에서 적 경로/사격 압박, 적 전멸과 클리어 리플레이의 최종 시각 품질은 수동 플레이 검증이 필요하다.

## 2026-08-04 - 빈 탄약 발사 시도의 시간 활동 반영

- 변경 유형: 플레이어 총기 입력·월드 시간 활동 처리 보완, 컴파일 검증·문서 갱신
- 변경 내용: **구현 완료**. 공용 `WeaponController`의 기존 `TryFire` 반환값은 실제 투사체 발사 성공 여부로 유지하고, `out bool fireAttempted` overload를 추가했다. 총기 구성과 참조가 유효하고 사용 간격이 지난 빈 탄약 발사 시도는 `fireAttempted`만 `true`로 반환하며, 탄약·투사체·발사 순번은 변경하지 않는다. 이때 다음 사용 시각을 무기 사용 간격만큼 전진시켜 자동소총 홀드 중에도 빈 발사 활동 펄스가 매 프레임이 아니라 발사 간격마다 한 번만 발생한다. `PlayerCombat`은 일반 발사에서 실제 발사 성공 또는 이 유효한 빈 탄약 발사 시도에 기존 `fireActivity`와 `fireActivityDuration`을 그대로 적용한다. 근접 무기·빈손 주먹의 성공 펄스는 유지하며, `DEADLINE`은 기존 `TryStageFire`를 사용하므로 탄약이 없으면 행동 준비·슬롯 소비·시간 활동이 발생하지 않는다.
- 영향을 받은 시스템: 플레이어 반자동/자동 총기 입력, 월드 시간 활동 펄스, 빈 탄약 자동소총 홀드 간격, 기존 적 AI 사격·근접/빈손 공격·`DEADLINE` 준비 발사 보존
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.4로 갱신해 일반 총기 발사에서 빈 탄약 시도도 기존 발사와 같은 시간 활동을 발생시키되, 투사체·탄약·`DEADLINE` 준비 동작은 바꾸지 않는 규칙과 검증 범위를 기록했다.
- 테스트 결과: **Unity 스크립트 배치 컴파일 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success (9.03 seconds), 6 items updated, 219 evaluated`와 종료 코드 0을 확인했다. 일반 `TryFire` 호출자는 기존 bool 반환 경로를 유지하고, 플레이어만 새 overload의 빈 탄약 시도 신호로 시간 활동 펄스를 호출하며 `TryStageFire`는 변경되지 않은 것을 정적으로 대조했다. 정식 Unity Test Framework가 없고 기존 `PrototypePlayModeSmokeTest`가 실제 LMB 빈 탄약 입력·활동 펄스 간격을 대조하지 않으므로, 해당 플레이 모드 시나리오는 **미실행**이다.
- 남은 작업: **확인 불가**. 실제 조작으로 탄약 0인 권총/샷건 클릭 시 시간 배율 체감, 탄약 0인 자동소총 홀드의 발사 간격별 펄스, 빈 발사 직후 무기 교체 시 체감, 기존 `DEADLINE` 빈 탄약 거부 결과는 별도 플레이 모드 검증이 필요하다.

## 2026-08-03 - 3축 결정적 탄도 산포 확장

- 변경 유형: 총기 탄도 산포 확장, 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. 공용 `WeaponController`가 기존 대칭 수평 팬과 수평 산포를 적용한 뒤, 그 방향의 로컬 수직 축을 기준으로 추가 회전해 수직 산포를 적용한다. `spreadJitterAngle`은 새 직렬화 필드 없이 수평·수직 각각의 최대 산포각으로 재사용한다. 권총과 자동소총은 축당 최대 ±1.5도, 샷건은 기존 18도 수평 팬의 각 펠릿에 축당 최대 ±1도를 적용한다. 수평·수직 산포는 무기 시드·발사 순번·펠릿 인덱스에 서로 다른 채널 상수를 더한 독립 결정적 해시 결과이며, Unity 전역 `Random`을 사용하지 않는다. 일반 발사·`DEADLINE` 준비 발사·적 자동소총 점사는 같은 공용 발사 경로를 유지한다. 조준점·카메라·플레이어 회전·누적 반동과 무기 에셋 값/GUID는 변경하지 않았다.
- 영향을 받은 시스템: 권총·자동소총·샷건 투사체 방향, 샷건 펠릿 패턴, 적 자동소총 점사, `DEADLINE` 준비 발사, 정적 씬 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.3으로 갱신해 수평 팬과 독립 수평·수직 결정적 산포, 무기별 축당 산포값, 적용 대상과 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 재생성·검증한 뒤 종료 코드 0으로 완료했다. 기존 권총·자동소총·샷건의 발사 모드·펠릿 수·산포 수치·시드와 Stage1/Stage2 픽업 GUID 참조를 확인했고, 수평/수직 채널 상수의 분리, 기존 `<Mouse>/leftButton` 바인딩과 `DEADLINE`의 Down 기반 준비 분기, Unity 전역 `Random` 미사용을 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 상하 산포의 시각적 체감과 명중 분포, 샷건 펠릿 원형 분포, 적 자동소총 점사와 `DEADLINE` 준비 발사의 3축 탄도 결과는 플레이 모드 테스트를 생략했으므로 확인하지 않았다.

## 2026-08-03 - 총구 기준 마우스 조준 보정

- 변경 유형: 플레이어 조준·총기/투척 발사 방향 보정, 씬 빌더 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `PlayerAim`은 마우스 광선에 맞은 가장 가까운 비트리거 콜라이더(플레이어 자신과 자식 콜라이더 제외)의 정확한 `RaycastHit.point`를 조준점으로 저장한다. 적·벽·바닥·엄폐물은 같은 거리 우선 규칙을 따르므로 벽이 먼저 맞으면 벽 뒤 적을 조준하지 않는다. 콜라이더가 없을 때만 기존 `y=0` 지면 평면 투영을 사용한다. `PlayerCombat`은 총기 일반 발사·`DEADLINE` 준비 발사·무기 투척에서 플레이어 중심 방향 대신 `WeaponController.Muzzle`에서 조준점까지의 `x/z` 방향을 사용하며, `y` 성분은 항상 0으로 유지한다. 근접 공격, 적 AI 사격, `WeaponController`의 쿨다운·무기별 산포, `Projectile`의 WorldTime SphereCast는 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 마우스 조준, 총기·투척 탄도, 벽 가림 판정, `DEADLINE` 준비 발사, Stage1/Stage2 생성 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.2로 갱신해 최근 물리 표면 조준점, 총구 기준 수평 발사, 벽 가림과 기존 산포 유지 정책을 반영했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 생성 씬의 `aimCollisionMask: ~0`과 기존 무기 산포 설정을 포함해 Stage1/Stage2를 재생성·검증한 뒤 종료 코드 0으로 완료했다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`도 `Prototype play-mode smoke test passed.`로 완료했다. 생성기는 기존 저장 씬의 레이아웃·머티리얼까지 광범위하게 재작성하므로, 이번 기능과 무관한 생성 산출물은 보존하지 않았다. 기존 씬은 새 직렬화 필드가 없더라도 코드 기본값 `~0`으로 동작한다.
- 남은 작업: **확인 불가**. 스모크는 일반 통합 흐름만 확인하므로, 실제 입력으로 바닥·벽·적 클릭의 시각적 조준점, 플레이어 자신 클릭 시 다음 표면 선택 또는 fallback, 벽 뒤 적 가림, 산포를 포함한 장거리 명중 감각과 `DEADLINE` 중 준비 발사의 탄도는 별도 확인이 필요하다.

## 2026-08-02 - 무기별 결정적 좌우 산포

- 변경 유형: 총기 탄도 보정, 무기 데이터·씬 빌더 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 기본 팬 각도와 분리된 `spreadJitterAngle`, `spreadSeed`를 추가했다. 공용 `WeaponController`는 실제로 발사에 성공한 순간에만 발사 순번을 하나 늘리고, 무기 시드·발사 순번·펠릿 인덱스를 조합한 상태 없는 해시로 `[-산포 최대각, +산포 최대각]`의 좌우 오프셋을 결정한다. 권총과 자동소총은 최대 ±1.5도(시드 101/211), 샷건은 기존 18도 대칭 팬의 각 펠릿에 최대 ±1도(시드 307)를 더한다. Unity 전역 `Random`은 사용하지 않으며, 조준점·플레이어 회전·카메라는 변경하지 않았다. 일반 발사와 `DEADLINE` 준비 발사는 같은 발사 경로로 방향을 확정하고, 적 자동소총도 같은 무기 정의·컨트롤러를 사용하므로 같은 산포 규칙을 적용한다.
- 영향을 받은 시스템: 플레이어·적 총기 투사체 방향, 자동소총 점사, 샷건 펠릿 패턴, `DEADLINE` 준비 발사, 무기 ScriptableObject, Stage1/Stage2 생성·저장 씬 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.1로 갱신해 총기 발사 경로, 무기별 실제 산포·시드, 샷건 팬 패턴, 조준점 반동 제외 범위, 정적 검증 결과와 런타임 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 재생성한 뒤 `ValidateSavedPrototypeRoom` 검증을 종료 코드 0으로 완료했다. 권총/자동소총의 `spreadAngle: 0`, `spreadJitterAngle: 1.5`, 샷건의 `spreadAngle: 18`, `projectileCount: 8`, `spreadJitterAngle: 1`과 세 시드 101/211/307, 샷건 정의 GUID → 픽업 프리팹 → 두 저장 씬 참조, 기존 `<Mouse>/leftButton` 바인딩과 `DEADLINE`의 Down 기반 준비 분기를 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 연속 발사 산포 체감, 샷건 펠릿 명중 분포, 적 자동소총 점사, `DEADLINE`에서 확정된 방향의 행동 준비·해제 결과는 플레이 모드 테스트를 생략했으므로 확인하지 않았다.

## 2026-08-02 - 플레이어 자동 연사·샷건·빈손 주먹 공격

- 변경 유형: 전투 기능 확장, 무기 데이터·픽업 콘텐츠 추가, 입력/HUD/정적 검증 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 반자동/자동 발사 모드, 투사체 수, 총 퍼짐을 추가했다. 권총은 반자동 1발, 자동소총은 자동 1발이며 플레이어는 LMB 홀드로 자동소총만 발사 간격마다 연사한다. 샷건은 반자동·탄창 6·발사 간격 0.75초·탄속 16·펠릿 피해 1·8펠릿·총 퍼짐 18도(좌우 ±9도)로 추가했고, 각 발의 펠릿은 재현 가능한 대칭 고정 패턴으로 생성한다. 빈손 플레이어는 LMB Down으로 거리 1.2·반각 35도·피해 1·간격 0.6초의 주먹 공격을 사용하며 기존 `MeleeAttackResolver`와 `DEADLINE` 준비/해제 경로를 재사용한다. Stage1/Stage2에는 권총과 샷건 정의 GUID를 각각 보관하는 시작 픽업 프리팹을 배치했다.
- 영향을 받은 시스템: 플레이어 입력·전투, 총기 투사체 생성, 근접 판정, 무기 픽업/교환/드롭 호환성, HUD 조작 안내, ScriptableObject, 프리팹, Stage1/Stage2, 에디터 빌더 정적 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/PistolPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: `docs/PROJECT_DESIGN_DOCUMENT.md`를 1.3.0으로 갱신해 자동소총 홀드 연사, 샷건 수치·펠릿 패턴, 빈손 주먹, 무기별 시작 픽업, LMB 안내와 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 스크립트 컴파일에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`과 `ValidateSavedPrototypeRoom`이 Stage1/Stage2를 종료 코드 0으로 재생성·검증했다. 권총/자동소총/샷건의 직렬화 발사 모드·투사체 수·퍼짐, 샷건 정의 GUID → `ShotgunPickup.prefab` → 두 저장 씬의 참조, 기존 `PlayerControls.inputactions`와 생성 `PlayerControls.cs`의 `<Mouse>/leftButton` 바인딩을 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 자동 연사 체감, 산탄 명중, 빈손 주먹 적중, 샷건 획득/교환/투척/가로채기와 `DEADLINE` 중 행동 준비·이동 해제의 런타임 결과는 후속 플레이 검증이 필요하다.

## 2026-08-02 - Deadline 전용 시네마틱 리플레이 시간축

- 변경 유형: 리플레이 시간축·카메라 연출·HUD·씬 직렬화·플레이 모드 스모크 검증·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController`가 20Hz 현실 시간 샘플에 현실·월드 타임스탬프와 Deadline 활성 상태를 기록하고, 시작 시 일반 월드 시간·Deadline 시네마틱·해제 후 슬로모션을 결합한 프레젠테이션 시간축을 생성한다. Deadline 활성 구간은 `현실 길이 / 0.50`을 0.8~2.0초로 제한하며, 해제 후 0.75 월드 초는 0.50배로 재생한다. Deadline 중 카메라는 진입 포즈로 고정되고 해제 후 0.2초 동안 기록 카메라로 보간 복귀한다. HUD는 `REPLAY 1.00x`, `DEADLINE CINEMATIC`, `DEADLINE AFTERMATH 0.50x`를 현재 단계에 따라 표시한다.
- 영향을 받은 시스템: `StageReplayController`, `DeadlineController` 상태 기록, 카메라 리플레이, 시각·조명·ViewCone 샘플 보간, `GameHud`, Stage1/Stage2 리플레이 직렬화, `PrototypeSceneBuilder`, `PrototypePlayModeSmokeTest`
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `docs/PROJECT_DESIGN_DOCUMENT.md`, `docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.9로 갱신해 하이브리드 시간축, 0.50배/0.8~2.0초/0.75 월드 초/0.2초 기본값, HUD 단계와 자동 검증 범위를 반영했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일 `Tundra build success`, `BuildAndValidateFromCommandLine`의 씬 재생성·검증, `PrototypePlayModeSmokeTest`를 통과했다. 스모크는 약 1초의 0.02배 Deadline을 최대 2초, 짧은 Deadline을 최소 0.8초, 해제 후 0.75 월드 초를 1.5초로 변환하고 재생 카메라의 고정·복귀를 확인한다.
- 남은 작업: **확인 불가**. 실제 플레이에서 `Q → 조준 회전 → 행동 준비 → 이동 해제 → 적 전멸`의 연출 품질과 `R` 재시작을 수동 확인해야 한다. ViewCone의 97회 Raycast와 메시 재계산 비용은 Unity Profiler로 별도 측정이 필요하다.

## 2026-08-02 - Q 키 기반 데드라인 발동 전환

- 변경 유형: 입력·데드라인 게임플레이·HUD·투사체 정리 수정
- 변경 내용: **부분 구현**. `Q` 키 Down 프레임에 `DEADLINE`을 즉시 발동하도록 전환했다. 기존의 실제 이동·이동 입력 해제·임박 적 탄환·충돌 예측 조건과 탄환 선점·강조를 제거했다. 충전 2회, 성공 발동 차감, 0.35 월드초 재준비, 최대 2개 행동 준비, 이동 해방, 조준 회전 중 최저 월드 배율 및 캐치 프리즈 우선은 유지한다.
- 영향을 받은 시스템: `PlayerControls`, `PlayerInputReader`, `DeadlineController`, `GameHud`, `Projectile`, `PrototypeSceneBuilder`, Stage1/Stage2 직렬화
- 관련 파일: `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.8로 갱신해 Q 키 발동, 탄환·이동 조건 제거, `PRESS Q TO DEADLINE` HUD 안내, 기존 충전·동시 해방·시간 정지 규칙 유지를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 모두 종료 코드 0으로 완료됐다. Q 바인딩과 생성 래퍼 일치, 두 씬의 기존 탄환·이동 트리거 필드 제거, `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`, 필수 참조를 확인했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**한다.
- 남은 작업: **확인 불가**. 탄환이 없는 상태·정지·이동·벽 접촉 중 Q 즉시 발동, `2/2 → 1/2 → 0/2`, 충전 소진·쿨다운·캐치 프리즈 중 Q 무시, 행동 두 개 동시 해방과 조준 회전 위험 속도는 사용자 플레이 확인이 필요하다.

## 2026-08-02 - 데드라인 씬당 충전 횟수 제한

- 변경 유형: 데드라인 게임플레이·밸런스·HUD·씬 직렬화 수정
- 변경 내용: **부분 구현**. `DeadlineController`에 직렬화된 `maximumCharges: 2`와 런타임 `chargesRemaining`을 추가했다. 성공적인 적 탄환 claim과 하드 프리즈 획득 뒤에만 1회를 차감하며, 실패한 발동 시도·행동 슬롯 사용·해제는 차감하지 않는다. 씬 로드 `Awake`에서 충전을 초기화하고 리플레이의 비활성화/재활성화로는 회복하지 않는다. 충전이 0이면 위협 강조·발동 안내를 중단하며, 기존 0.35 월드초 재준비와 행동 슬롯 2개는 유지한다.
- 영향을 받은 시스템: `DeadlineController`, `GameHud`, `PrototypeSceneBuilder`, Stage1/Stage2 직렬화, 데드라인 위협 안내
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.7로 갱신해 씬당 2회, 성공 발동 차감, 씬 재로드 초기화, 리플레이 중 미회복, HUD와 밸런스 값을 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 종료 코드 0으로 완료됐다. 두 씬의 `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`와 데드라인 필수 참조를 확인했다. 빌더가 만든 기능 무관한 대규모 씬·머티리얼 재직렬화는 복원하고 충전 필드만 유지했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 첫·두 번째 발동의 `2/2 → 1/2 → 0/2`, 세 번째 위협의 안내·발동 차단, 실패 시 미차감, 씬 재시작 회복, 리플레이 미회복 및 동시 해방·캐치·대시·사망 회귀는 사용자 플레이 확인이 필요하다.

## 2026-08-02 - 데드라인 회전 중 최저 시간 배율

- 변경 유형: 시간 시스템·데드라인 게임플레이 수정, 문서 갱신
- 변경 내용: `WorldTimeController.AcquireHardFreeze(bool)`에 데드라인 전용 조준 허용 토큰을 추가했다. 이 토큰만 활성이고 `WorldTimeActivity.AimTurn > 0.0001`이면 월드 전체가 씬의 `minimumTimeScale`(Stage1/Stage2 현재 0.02배)로 진행하며, 마우스를 멈추면 다시 0배 완전 정지한다. 일반 토큰 또는 `RequestHardFreeze` 기반 가로채기 프리즈가 겹치면 완전 정지를 우선한다. `DeadlineController`만 조준 허용 토큰을 요청하며 전역 `Time.timeScale`은 변경하지 않는다.
- 영향을 받은 시스템: `WorldTimeController`, `WorldTimeActivity`, `DeadlineController`, 적·투사체·투척 무기의 `WorldDeltaTime` 진행, 동시 해방, 공중 가로채기 프리즈
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `Docs/PROJECT_DESIGN_DOCUMENT.md`, `Docs/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.6으로 갱신해 데드라인 중 조준 회전은 최저 월드 배율, 마우스 정지는 완전 정지, 캐치 프리즈 우선 규칙과 0.02배 기준을 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 종료 코드 0으로 완료됐다. Stage1/Stage2의 `minimumTimeScale: 0.02`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`와 하드 프리즈 호출 경로를 정적으로 확인했다. 빌더 실행은 기존 기준과 다른 대규모 씬·머티리얼 재직렬화를 만들었으나 기능과 무관해 복원했으며, 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 데드라인 발동 후 마우스 정지 시 0배, 회전 시 0.02배, 재정지 시 0배 복귀와 회전 중 적 탄환·투척물·준비 투사체의 저속 진행을 사용자 플레이로 확인해야 한다. 캐치 정지 중 회전해도 0배 유지, 이동 해제 후 기존 활동량 배율 복귀, 동시 해방·사망·리플레이 회귀도 수동 확인이 필요하다.

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
