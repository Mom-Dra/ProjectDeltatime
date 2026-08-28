# 기능 변경 기록

이 문서는 코드, 씬, 프리팹, ScriptableObject, 입력, UI, 밸런스, 패키지 또는 프로젝트 설정의 기능 변경을 추적한다.

## 기록 규칙

- 기능 추가, 수정, 삭제가 끝나기 전에 해당 변경을 기록한다.
- 실제 파일과 테스트 결과에서 확인된 내용만 적는다.

## 2026-08-28 - 전투 씬 NavMesh 스폰 구멍 제거

- 변경 유형: NavMesh 베이크 입력 수정, 활성 내비게이션 에셋 전용 재베이크, 스폰·경로 검증 강화
- 변경 내용: **구현 완료**. `PhysicsColliders`를 수집하는 Stage1/2·Stage3/4·StageBattingCage 빌더가 플레이어·적·초기 `WeaponPickup` Collider를 베이크 직전에만 비활성화하고 `try/finally`로 복구하도록 공용화했다. 이에 따라 캐릭터가 이동한 뒤 시작 위치에 남던 NavMesh 구멍을 제거하면서 벽·펜스·기둥·가구 Collider의 기존 장애물 판정은 유지한다. Tutorial과 Stage5/6의 기존 동적 루트 제외·가구 `Not Walkable` 정책은 변경하지 않았다.
- 변경 내용: **구현 완료**. 전체 씬을 재생성하지 않는 `Rebake Stage 1 + Stage 2 Navigation`과 `Rebake Stage - Underground Batting Cage Navigation` 메뉴/CLI를 추가하고 `StageNavigation.asset`, `StageBattingCageNavigation.asset`만 갱신했다. 정적·PlayMode 검증은 플레이어·적·초기 픽업 위치에서 직접 하부 NavMesh의 수평 오차 `0.1m` 이하와 모든 적→플레이어 `PathComplete`를 검사한다. 런타임 `EnemyMotor`, `WorldDeltaTime`, 물리 충돌과 공개 API는 변경하지 않았다.
- 영향을 받은 시스템: Stage1/2 공유 NavMesh, StageBattingCage NavMesh, 비활성 Stage3/4 재생성 경로, 플레이어·적·무기 픽업 스폰 검증, 전투 씬 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/SceneBuilderInfrastructure.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/StageBattingCageSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/StageNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/StageBattingCageNavigation.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `PROJECT_DESIGN_DOCUMENT.md`를 1.10.7로 갱신해 동적 Collider 제외 정책, 전용 재베이크, 0.1m 직접 하부·완전 경로 검증, 실제 회귀 결과와 비활성 Stage3/4 미실행 상태를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 컴파일, Stage1/2 공유 NavMesh 전용 재베이크와 양 씬의 플레이어 1·적 3·픽업 2 직접 하부/완전 경로 검증, StageBattingCage 전용 재베이크와 플레이어 1·적 6 직접 하부/완전 경로 검증이 종료 코드 0으로 통과했다. StageBattingCage PlayMode 스모크와 Stage6 PlayMode 스모크(완전 경로 5/5)도 통과했다. Stage2 전체 스모크는 새 NavMesh 검증을 통과한 뒤 기존 투척 무기 수치·6m 착지·리플레이 본 포즈 3건에서 실패했다. Tutorial은 기존 Synty 인스턴스 216/262, Stage5는 기존 픽업 1/2에서 NavMesh 검사 전에 중단됐다. `dotnet build Assembly-CSharp-Editor.csproj --no-restore`는 `Temp/obj/.../project.assets.json`이 없어 컴파일 전에 중단됐으며 Unity 자체 컴파일은 통과했다. 로그: `ProjectDeltatime/NavMeshStage12RebakeFinal.log`, `ProjectDeltatime/NavMeshBattingCageRebake.log`, `ProjectDeltatime/NavMeshBattingCageSmoke.log`, `ProjectDeltatime/NavMeshPrototypeSmoke.log`, `ProjectDeltatime/NavMeshTutorialRegression.log`, `ProjectDeltatime/NavMeshStage5Regression.log`, `ProjectDeltatime/NavMeshStage6Regression.log`.
- 남은 작업: **부분 구현/확인 불가**. 비활성 `Stage3_NoUse`와 현재 씬이 없는 Stage4는 재생성하지 않아 실제 신규 NavMesh 에셋 검증을 **미실행**으로 유지한다. Stage2·Tutorial·Stage5의 기존 비-NavMesh 기준선 실패는 별도 작업이며, Unity Scene 뷰에서 파란 NavMesh가 각 스폰 바닥을 연속적으로 덮는지에 대한 최종 육안 확인은 **확인 불가**다.

## 2026-08-26 - StageBattingCage 바닥·펜스·소품 배치 정렬

- 변경 유형: 스테이지 환경 배치 정리(바닥 무봉임 타일링, 펜스 피벗 보산 정렬, 소품 그리드 스냅)
- 변경 내용: **구현 완료**. `StageBattingCageSceneBuilder`의 배치 상수를 프리팹 실제 치수에 맞췄다. `SM_Bld_Floor_Combined_01`은 2.5m 타일(피벗이 모서리, BoxCollider 2.5×0.1×2.5)이므로 기존 3m 간격 7×6 배치(0.5m 틈 발생)를 2.5m 간격 9×8=72장으로 바꿔 x −11.25~+11.25, z −10~+10을 틈 없이 완전히 덮고 y=0으로 평평하게 유지한다. `SM_Prop_Fence_Wire_01`은 몸체 길이 약 2.668m에 피벗이 한쪽 끝(로컬 몸체 중심 x ≈ −1.241)이라 90도 회전 시 지그재그가 생겼으므로, 몸체 중심 기준 배치 헬퍼 `PlaceFence`와 균등 분포 헬퍼 `FenceRunCenter`를 추가해 남/북 8패널(z=±9.8), 서/동 7패널(x=±10.8)을 충돌 경계 안면과 정확히 일치시키고 균일 이음새로 모서리가 닫힌 직사각형을 만들었다. 댄싱 케이지 2·스포츠 가방 4·스피커 2·무대 조명 4도 2.5m 그리드 좌표로 스냅하고 45도 단위 회전과 원점 대칭(점대칭) 배치를 유지한다. 적 6기·플레이어 스폰, 충돌 블로커, NavMesh 베이크 방식, 초기 시야 검증 대상은 변경하지 않았다.
- 영향을 받은 시스템: StageBattingCage 환경 아키텍처(바닥 42→72장, 펜스 26→30판), 시각 전용 프리팹 배치. 게임플레이·내비게이션·물리는 무변경(프리팹 콜라이더는 `CharacterSceneSetup.DisableColliders`로 비활성화)
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/StageBattingCageSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/StageBattingCage.unity`, `ProjectDeltatime/Assets/_Project/Art/Generated/StageBattingCagePreview.png`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **해당 없음**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`에는 StageBattingCage 레이아웃(타일·펜스 좌표)을 다루는 항목이 없어 확인했고 문서 수정은 하지 않았다. 좌표 수준 배치는 본 변경 기록으로 추적한다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 `BuildAndValidateFromCommandLine`이 씬 재생성과 정적 검증(적 6/6, 픽업 0, 캐릭터 비주얼 7, 비전 블로커 7, Synty 인스턴스 117개 ≥ 75, 포인트 라이트 4, NavMesh 완전 경로, 초기 시야 스태거)을 통과했다. `CapturePreviewFromCommandLine`으로 프리뷰를 다시 촬영해 바닥 틈 제거와 펜스 4면 직선 정렬·모서리 마감을 육안 확인했다. `StageBattingCagePlayModeSmokeTest.RunFromCommandLine`도 통과했다(6방향 근접 적, 월드 시간, 리플레이, Stage5 전환). 로그: `ProjectDeltatime/StageBattingCageBuild.log`, `ProjectDeltatime/StageBattingCagePreview.log`, `ProjectDeltatime/StageBattingCageSmoke.log`.
- 남은 작업: **확인 불가**. 실제 Game View에서 카메라 각도별 바닥 질감 반복과 펜스 이음새 가독성을 사용자가 최종 확인해야 한다. 소품 세부 좌표는 취향에 따라 조정 여지가 있다.


## 2026-08-26 - 전투 타격감 1차 개선 — 절제된 강함

- 변경 유형: 전투 카메라·월드 히트스톱·절차형 VFX·피격 화면·적 사망 반응 추가, 전투음 거리 감쇠 보정, 회귀 테스트 확장
- 변경 내용: **구현 완료**. `CombatFeedbackController`를 기존 게임플레이 카메라에 런타임 자동 구성해 성공한 플레이어 총기 발사의 무기별 카메라 임펄스와 0.07초 `MuzzleFlash`, 실제 적중의 0.14초 확장 링·방사형 스파크 `HitFlash`와 무기별 `RequestHardFreeze`, 실제 플레이어 피해의 0.18초 붉은 가장자리 비네트·피격 임펄스를 단일 경로에서 처리한다. 추적 기준과 흔들림 출력은 분리해 비스케일 감쇠 후 카메라 드리프트가 없고, 샷건 발사는 펠릿 수와 무관하게 발사 임펄스 한 번과 최대값 방식 히트스톱을 사용한다. 대시 무적·0 피해·빗나간 근접·환경 충돌에는 피해 히트스톱을 만들지 않으며 전역 `Time.timeScale`은 1을 유지한다. 새 미디어 에셋과 물리적 플레이어 반동은 추가하지 않았다.
- 변경 내용: **구현 완료**. 권총·자동소총·샷건·근접 무기 에셋과 `PrototypeSceneBuilder` 재생성 경로에 합의한 위치/회전/지속/히트스톱/총구 크기 값을 저장했다. 적 사망은 AI·Collider·피해 판정·드롭·KILL/CLEAR를 즉시 처리하고 시각 오브젝트만 비스케일 0.32초 동안 공격 방향 0.22m·최대 12도 반응 뒤 제거하며, 마지막 킬 리플레이 요청도 0.32초 늦춘다. `EnemyHealth`의 중복 HitFlash를 제거하고 투사체·근접·투척 충돌이 한 번씩만 생성하도록 통합했다. `PlayerHealth.Damaged` 이벤트는 실제 피해에만 발생한다.
- 변경 내용: **구현 완료**. `SoundManager`의 발사·휘두름·근접 적중·투척 API에 호환 가능한 선택적 `CombatFaction`을 추가하고, 풀링된 소스를 매 재생 초기화해 플레이어 관련 전투음은 `spatialBlend 0.25/minDistance 10m`, 적 전투음은 `1/2m`를 사용한다. `MuzzleFlash`와 두 Renderer의 `HitFlash`는 생성 즉시 리플레이에 등록하며 기존 `Replay - Hit Flash` 추적 이름도 유지한다.
- 영향을 받은 시스템: 플레이어·적 총기/근접/투척 전투, TopDown 카메라 추적, WorldTime 하드 프리즈, 적 사망·스테이지 클리어·리플레이 시작, 플레이어 체력·대시 무적, 절차형 전투 VFX, SoundManager 공간 감쇠, 무기 ScriptableObject·재생성 경로, EditMode/PlayMode/씬 스모크 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CombatFeedbackController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/MuzzleFlash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Utilities/HitFlash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/CombatFeedbackTests.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.6으로 갱신해 런타임 자동 구성, 무기별 직렬화 값, 실제 피해 전용 하드 프리즈·피격 화면, 적 사망/리플레이 지연, 오디오 감쇠·호환 API, 범위 제외와 검증 상태를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 자체 C# 컴파일이 통과했다. `CoreBehaviorTests` EditMode 25/25, 새 `CombatFeedbackTests` PlayMode 4/4, `WorldTimeContractTests` 3/3, `ElevationFireAimTests` 3/3이 통과했고 SoundManager·Stage1 캐릭터 애니메이션·Replay PlayMode 스모크도 통과했다. 새 테스트는 발사당 총구/카메라 1회, 샷건 펠릿 히트스톱 최대값, 플레이어 위치·전역 시간 불변, 근접 빗나감/적중 단일 HitFlash, 적 즉시 판정 제거와 0.32초 시각 반응, 대시 무적 무피드백, 플레이어 실제 피격, 마지막 킬 즉시 이벤트/0.32초 리플레이 지연을 확인했다. SoundManager 스모크는 실제 풀링 `AudioSource`의 플레이어 `0.25/10m`, 적 `1/2m`를 확인했다. `dotnet build ProjectDeltatime.sln`은 기존 Unity 생성 `.csproj`가 삭제된 `Assets/TutorialInfo/Scripts/Readme.cs`와 설치되지 않은 패키지 어셈블리를 참조해 실패했으며 이번 런타임 코드 오류로 판정하지 않는다. 로그: `ProjectDeltatime/CombatFeedbackEditMode.log`, `ProjectDeltatime/CombatFeedbackPlayMode.log`, `ProjectDeltatime/CombatFeedbackSoundSmoke.log`, `ProjectDeltatime/CombatFeedbackStage1AnimationSmoke.log`, `ProjectDeltatime/CombatFeedbackReplaySmoke.log`.
- 남은 작업: **확인 불가**. 실제 키보드·마우스 플레이에서 권총·자동소총·샷건·야구방망이·주먹·플레이어 피격의 상대 강도, 연사 시 멀미 여부, 헤드폰/스피커 거리감, 적 사망 반응과 마지막 킬 리플레이 연결감을 수동 확인해야 한다. 필요하면 코드 변경 없이 네 무기 에셋 직렬화 값만 조정한다. 물리적 넉백·래그돌·재질별 충돌음/파티클·새 오디오 에셋·흔들림 설정 UI는 이번 범위의 **미구현** 항목으로 유지한다.

## 2026-08-26 - DEADLINE 해제음 단일화

- 변경 유형: DEADLINE 효과음 선택 정책 수정, SoundLibrary 직렬화·재생성 경로·스모크 검증 갱신
- 변경 내용: **구현 완료**. `DeltatimeSoundLibrary.asset`의 `deadlineReleaseClips`와 `SoundLibraryBuilder`가 `SFX_Deadline_Release2.mp3` 하나만 등록하도록 변경했다. `SoundLibrary.GetDeadlineReleaseClip()`과 `SoundManager.PlayDeadlineRelease()` 공개 인터페이스, 진입 충격·단발 시간 왜곡·BGM 덕킹 해제·재생 음량은 유지한다. 기존 `SFX_Deadline_Release.mp3`와 `.meta`는 삭제하지 않고 복구용 미사용 에셋으로 보존한다.
- 영향을 받은 시스템: Tutorial·전체 Stage의 DEADLINE 성공·실패 공통 해제음, SoundLibrary 리소스, 오디오 빌더, SoundManager PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.5로 갱신해 `Release2` 단일 해제음, 기존 에셋 보존, 유지되는 오디오 정책과 검증 결과를 기록했다.
- 테스트 결과: **구현 완료**. 정적 검증에서 저장된 SoundLibrary와 빌더가 `SFX_Deadline_Release2.mp3`만 참조함을 확인했다. Unity 6000.1.13f1 배치 컴파일과 SoundLibrary 재생성·검증이 통과했고, SoundManager PlayMode 스모크는 `Release2` 선택, `DEADLINE` 진입·해제 상태 및 MainScene→Tutorial→Stage1→EndingScene BGM 회귀를 통과했다. 로그: `ProjectDeltatime/DeadlineReleaseSingleBuild.log`, `ProjectDeltatime/DeadlineReleaseSingleSoundSmoke.log`.
- 남은 작업: **확인 불가**. 실제 스피커·헤드폰에서 해제마다 `Release2`만 한 번 들리는지와 체감 음량은 사용자 청감 검증이 필요하다. `SFX_Deadline_Release2.mp3`의 원본 페이지와 라이선스도 배포 전에 확인해야 한다.

## 2026-08-26 - Tutorial Rework 외벽·전광판·환풍기 겹침 보정

- 변경 유형: 후보 환경 시각 단순화, 후보 전용 월드 시간 앵커 배치, 전광판 런타임 호환 및 비중첩 정적 검증 추가
- 변경 내용: **부분 구현**. `Assets/_Project/Scenes/TutorialRework/Tutorial.unity` 후보에서 연속 벽과 같은 위치에 중복되던 Synty 측면·끝 벽 모듈 및 상부 트림을 제거하고 `Gameplay Boundaries`의 단일 벽 4개만 외곽으로 사용한다. 벽 장착 조명 프리팹도 없애고 구역별 중앙 Point Light만 유지한다. 게이트 상태 전광판 6개와 시간 구역 제어 전광판 2개를 모두 제거했으며, 후보 정적 검증은 이름에 `Display` 또는 `Screen`이 있는 환경 오브젝트를 허용하지 않는다. `Architecture`는 6개 게이트의 좌우 기둥·상부 빔 18개만 갖는다. 공식 Tutorial의 전광판과 스크롤 연출은 유지하고, `WorldTimeVisualFeedback`는 후보의 `Architecture` 계층에서만 전광판 설정을 조용히 생략한다. 후보의 세 환풍기는 `(-5.25, 0, -31.5)`, `(5.25, 0, 19)`, `(-5.25, 0, 39)`로 옮겨 게이트·안전 경계·적과 분리했으며 공식 Tutorial의 기존 좌표는 변경하지 않았다. 후보 검증은 환풍기 Renderer Bounds가 바닥 표시 외 다른 환경 Renderer와 교차하면 실패한다.
- 영향을 받은 시스템: Tutorial Rework Architecture·Lighting·Bay 01, WorldTimeAmbientFan 후보 배치, WorldTimeVisualFeedback 전광판 스크롤, 후보 정적 검증·PlayMode 스모크·3구간 프리뷰
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WorldTimeAmbientSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/TutorialNavigation.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **부분 구현**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.4로 갱신해 단일 외벽, 전광판 완전 제거, 후보 전용 환풍기 좌표·Renderer Bounds 비중첩 정책, 공식 씬 보존과 검증 결과를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 후보 재생성·C# 컴파일·정적 검증이 통과했다. 정적 검증은 외벽 4개, 게이트 프레임 18개, 전광판 0개, 후보 전용 환풍기 좌표 3개와 환경 Renderer 비중첩, 기존 gameplay anchor·NavMesh·중앙 동선·Layer 8 정책을 확인했다. 최초 PlayMode 재검증에서 후보에 전광판이 없다는 기존 `WorldTimeVisualFeedback` 오류를 발견해 후보 전용 생략 처리를 추가했고, 이후 전체 스모크가 이동/정지 월드 시간, 전투·투척·DEADLINE·체크포인트, 무제한 시야, Animator, 세 환풍기 반응과 전역 `Time.timeScale == 1`을 통과했다. 남부·중부·북부 768px PNG를 다시 생성해 이중 외벽·전광판·환풍기/프롭 겹침이 사라진 것을 직접 확인했다. 로그: `ProjectDeltatime/TutorialReworkBuild.log`, `ProjectDeltatime/TutorialReworkSmoke.log`, `ProjectDeltatime/TutorialReworkPreview.log`, `ProjectDeltatime/Logs/Validation/TutorialReworkCaptures`.
- 남은 작업: **확인 불가**. 사용자가 실제 Game View에서 처음부터 끝까지 플레이하며 단일 외벽의 공간감, 환풍기 가시성·환경음 거리감과 전광판 없는 시간 구역의 가독성을 최종 승인해야 한다. 후보의 공식 승격은 계속 **계획 필요**이며 MainScene·Build Settings·공식 `Tutorial.unity`는 변경하지 않았다.

## 2026-08-26 - Tutorial Rework 후보 씬 신규 제작

- 변경 유형: 신규 후보 씬·전용 NavMesh 생성, Tutorial 빌더 프로필화, 정적/PlayMode/시각 검증 확장
- 변경 내용: **부분 구현**. 공식 `Assets/_Project/Scenes/Tutorial.unity`, MainScene 진입 대상과 Build Settings를 변경하지 않고 `Assets/_Project/Scenes/TutorialRework/Tutorial.unity` 후보를 새로 만들었다. Stage1의 플레이어·카메라·게임 시스템 기반과 기존 `TutorialDirector`의 7단계 진행 순서·난이도·gameplay anchor 좌표를 그대로 사용한다. 환경은 `Architecture`, `Wayfinding`, `Lighting`, `Gameplay Boundaries`, `Bay 01`~`Bay 07`, `World Time Ambient Anchors`로 구분하고, 폭 14m·길이 97m의 청회색 산업형 공간에 청록 진행선·균일 조명·철창 게이트·한글 표지·세 환풍기를 배치했다. 기능과 무관한 느슨한 프롭을 제외하고 벽 정렬·좌우 대칭을 적용했으며, 중앙 `|x| < 3.4m`에는 필수 게이트·동적 적 외 정적 Collider를 두지 않는다. 장식 프리팹 Collider는 비활성화하고 필요한 경계만 Layer 8 `VisionObstacle`를 사용하며 Synty 프리팹 상한은 120개다. 파일명을 `Tutorial.unity`로 유지해 기존 BGM과 월드 시간의 씬 이름 판정을 공유한다.
- 영향을 받은 시스템: Tutorial 후보 환경·NavMesh·Wayfinding·Lighting·VisionObstacle, TutorialSceneBuilder 라이브/후보 프로필, 정적 검증, 씬 경로 지정 PlayMode 스모크, 3구간 프리뷰 캡처
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/Tutorial.unity.meta`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/TutorialNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialRework/TutorialNavigation.asset.meta`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **부분 구현**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.3으로 갱신해 후보의 환경·충돌·프롭 정책, 기존 진행 계약과 공식 경로 보존, 실제 검증 결과, 사용자 승인 후 승격 계획을 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 생성·C# 컴파일·정적 검증이 Director·HUD·게이트 6개·트리거 3개·표적 2개·지급기 3개·적 5개, 기존 gameplay anchor와 철창 규격, 전용 NavMesh 완전 경로, 필수 계층·랜드마크·표지·조명·출구, 프롭 상한, 중앙 동선과 Layer 8 정책을 통과했다. 후보 경로 전체 PlayMode 스모크는 이동/정지 월드 시간, 조준/대시, 근접, 권총, 투척 기절·무장 해제·드롭·공중 회수, `DEADLINE` 두 원인 제한·이동 해제·체크포인트, 무제한 시야, 캐릭터 Animator, 세 환풍기 월드 시간 반응과 전역 `Time.timeScale == 1`을 통과했다. 남부·중부·북부 768px PNG 3개를 생성해 표지·표적·지급기·적의 가시성과 정돈된 중앙 통로를 직접 확인했다. 별도 `dotnet build`는 Unity 생성 프로젝트가 삭제된 `Assets/TutorialInfo/Scripts/Readme.cs`를 참조하는 기존 문제로 실패했지만 Unity 자체 컴파일은 통과했다. 로그: `ProjectDeltatime/TutorialReworkBuild.log`, `ProjectDeltatime/TutorialReworkSmoke.log`, `ProjectDeltatime/TutorialReworkPreview.log`, `ProjectDeltatime/Logs/Validation/TutorialReworkCaptures`.
- 남은 작업: **확인 불가**. 사용자가 실제 키보드·마우스로 처음부터 Stage1 전환까지 플레이하며 단계별 가독성·조명·충돌·공간감과 세 환풍기 환경음을 확인해야 한다. 승인 후 후보를 공식 Tutorial 진입 대상으로 승격하는 작업은 **계획 필요**이며, 이번 변경에서는 공식 씬·MainScene·Build Settings를 유지했다.

## 2026-08-26 - 실외기 날개 리플레이 반영 보정

- 변경 유형: 리플레이 포함/제외 계층 정책 확장, 환풍기 프리팹·씬 검증 갱신, Edit/PlayMode·Stage2 전용 리플레이 스모크 추가
- 변경 내용: **구현 완료**. 공개 마커 `Deltatime.Replay.ReplayIncluded`와 공용 계층 판정 정책을 추가했다. Renderer에서 부모 방향으로 가장 가까운 `ReplayIncluded` 또는 `ReplayExcluded`가 우선하며 같은 Transform에 두 마커가 있으면 포함이 우선한다. `StageReplayController`의 녹화 후보 판정과 `TrackedExcludedVisualCount`가 같은 정책을 사용한다. 환풍기 루트의 `ReplayExcluded`와 정적 외함은 유지하고 분리된 날개 Transform에만 `ReplayIncluded`를 붙였다. 날개 Renderer의 위치·회전은 기존 캐릭터·투사체와 같은 정규화 리플레이 시간축으로 샘플링해 프록시로 재생하며, 라이브 날개 Renderer는 리플레이 중 숨긴다. `WorldTimeAmbientAnchor`는 기존 `DisableLiveSimulation`에서 비활성화되고 `OnDisable`에서 3D 루프를 즉시 정지한다. 원본 Synty 에셋, Collider 비활성, 맵별 배치 위치·수량, 월드 시간·오디오 공개 API와 수치는 변경하지 않았다.
- 영향을 받은 시스템: StageReplayController 일반 Renderer 기록·진단, 환풍기 리플레이 시각, WorldTimeAmbientFan 프리팹, Tutorial·Stage1·Stage2·Stage5 멱등형 앵커 적용·정적 검증, 환경음 리플레이 정지, Edit/PlayMode 및 Stage2 캡처 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayIncluded.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayExcluded.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WorldTimeAmbientSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WorldTimeAmbientReplayPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Prefabs/Time/WorldTimeAmbientFan.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/CoreBehaviorTests.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/WorldTimeContractTests.cs`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Ambience/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.2로 갱신해 가장 가까운 포함/제외 마커 정책, 외함 제외·날개 기록·환경음 정지, 정규화 리플레이 시간축과 실제 자동·캡처 검증 결과를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 배치 컴파일과 멱등형 적용·정적 검증이 통과했고 Tutorial 3개, Stage1·Stage2·Stage5 각 2개의 앵커에서 루트 `ReplayExcluded`, 날개 단일 `ReplayIncluded`, 필수 참조, 3D 오디오 설정, Collider 비활성을 확인했다. `CoreBehaviorTests` EditMode 25/25는 일반 제외, 외부 포함 재포함, 더 가까운 내부 제외, 동일 Transform 포함 우선순위를 통과했다. `WorldTimeContractTests` PlayMode 3/3은 제외 외함 아래의 회전 날개를 실제 기록해 라이브 Renderer 숨김, 프록시 활성·각도 변화, 제외 진단 0, 앵커 비활성·환경음 정지를 통과했다. 기존 리플레이 시간축, SoundManager, DEADLINE 스모크도 통과했다. Stage2 전용 스모크는 두 환풍기를 확인하고 서로 다른 두 재생 시점 사이 프록시 날개가 71.960도 진행했으며 라이브 환경음 정지를 확인했다. 조명을 보완한 두 근접 캡처를 직접 확인해 팬 가시성을 확인했다. 기존 범용 Stage2 Replay 스모크는 새 환풍기 검사 전에 기존 애니메이터 컨트롤러 변경 이벤트 기대값 2/실제값 1에서 실패했다. 로그: `ProjectDeltatime/WorldTimeAmbientReplayBuild.log`, `ProjectDeltatime/WorldTimeAmbientReplayEditMode.log`, `ProjectDeltatime/WorldTimeAmbientReplayPlayMode.log`, `ProjectDeltatime/WorldTimeAmbientReplayTimeAxis.log`, `ProjectDeltatime/WorldTimeAmbientReplaySoundSmoke.log`, `ProjectDeltatime/WorldTimeAmbientReplayDeadlineSmoke.log`, `ProjectDeltatime/WorldTimeAmbientReplayFocusedSmoke2.log`, `ProjectDeltatime/WorldTimeAmbientReplayStage2Smoke.log`.
- 남은 작업: **확인 불가**. 실제 사용자 조작으로 Stage2 리플레이를 진입·탐색하며 외함과 날개의 화면 겹침, 재생 체감, 무음 여부를 확인하지 못했다. 범용 Stage2 Replay 스모크의 애니메이터 컨트롤러 변경 이벤트 기준선 불일치는 별도 범위에서 원인을 확인해야 한다.

## 2026-08-25 - 월드 시간 환경 기준점 연출

- 변경 유형: 월드 시간 시각·3D 환경음 피드백 추가, 진행 맵 씬·빌더 갱신, 오디오 에셋 가공, Edit/PlayMode·정적·캡처 검증 추가
- 변경 내용: **구현 완료**. 공용 `WorldTimeAmbientAnchor`가 분리된 팬 날개를 초당 240도 기준 `WorldDeltaTime`으로 회전시킨다. 일반 반응값은 `CurrentTimeScale`, 하드 프리즈는 0이며 비스케일 0.15초 동안 전환한다. 피치 `0.45→1.0`과 볼륨 계수는 `sqrt(s)`, 로우패스는 `500→16,000Hz`의 `s`, 기본 볼륨은 `0.22`이고 `SoundManager`의 Master·SFX 사용자 배율을 곱한다. 비활성화와 리플레이 라이브 시뮬레이션 중단 시 루프를 즉시 정지하며 `CurrentPitch`, `CurrentCutoffFrequency`, `CurrentOutputVolume`, `IsLoopPlaying` 진단값과 `Configure(WorldTimeController)`를 공개한다. Pixabay 산업용 팬 원본은 0.25초 equal-power 끝점 크로스페이드, 48kHz 모노 OGG, 약 26.966초, 피크 -3dBFS로 가공했다. Synty 원본은 수정하지 않고 Collider 비활성·`ReplayExcluded`·3D AudioSource·AudioLowPassFilter를 포함한 프로젝트 프리팹으로 감쌌다. Tutorial 3개, Stage1·Stage2·Stage5 각 2개를 전용 루트에 배치했으며 전체 씬 빌더와 멱등형 `Apply World Time Ambient Anchors` 경로가 같은 헬퍼를 사용한다.
- 영향을 받은 시스템: Tutorial·Stage1·Stage2·Stage5 환경, 월드 시간·`DEADLINE`, 3D 오디오, Master/SFX 설정, Replay 라이브 시뮬레이션, Synty 환풍기 시각, 씬 빌더·정적 검증·근접 캡처, 기존 DEADLINE 시각 스모크의 현재 진행 맵 목록
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeAmbientAnchor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WorldTimeAmbientSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/DeadlineVisualFeedbackPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Prefabs/Time/WorldTimeAmbientFan.prefab`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Ambience/SFX_WorldTime_IndustrialFan_Loop.ogg`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Ambience/README.md`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/CoreBehaviorTests.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/WorldTimeContractTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.1로 갱신해 팬 배치 수·위치 정책, `WorldDeltaTime` 회전, 시간 배율별 전용 환경음 매핑, 하드 프리즈·리플레이 정책, 출처·가공 에셋과 실제 검증 결과를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 배치 컴파일은 `Tundra build success`와 종료 코드 0으로 통과했고, 전용 적용·검증은 Tutorial 3개와 Stage1·Stage2·Stage5 각 2개의 위치·필수 참조·모노 클립·3D 설정(Doppler 0/2.5~18m)·Collider 비활성·`ReplayExcluded`를 통과했다. `CoreBehaviorTests` EditMode 24/24와 `WorldTimeContractTests` PlayMode 2/2가 시간 배율 1.0·0.5·0.02 순서, 0.15초 무음, 실제 팬 회전·해제 복원·비활성화 정지·전역 `Time.timeScale` 유지를 통과했다. SoundManager 스모크와 Stage6를 현재 진행에서 제외한 DEADLINE 시각 스모크가 통과했다. 네 960×720 근접 캡처를 생성·직접 확인해 각 맵에서 팬이 식별됨을 확인했고 Collider 비활성 정적 검증으로 NavMesh·이동 비침범을 확인했다. 전체 Tutorial 스모크는 기존 Synty 216/262, Prototype 스모크는 기존 투척 수치·6m 착지·리플레이 본 포즈 3건, Stage5 스모크는 기존 픽업 1/2 불일치로 실패했다. `dotnet build`는 Unity 밖의 기존 생성 프로젝트가 삭제된 `Assets/TutorialInfo/Scripts/Readme.cs`와 여러 패키지 참조를 유지해 실패했으나, Unity 자체 C# 컴파일은 통과했다. 로그: `ProjectDeltatime/WorldTimeAmbientBuild.log`, `ProjectDeltatime/WorldTimeAmbientEditMode.log`, `ProjectDeltatime/WorldTimeAmbientPlayMode.log`, `ProjectDeltatime/WorldTimeAmbientSoundSmoke.log`, `ProjectDeltatime/WorldTimeAmbientDeadlineSmoke.log`, `ProjectDeltatime/WorldTimeAmbientTutorialSmoke.log`, `ProjectDeltatime/WorldTimeAmbientPrototypeSmoke.log`, `ProjectDeltatime/WorldTimeAmbientStage5Smoke.log`, `ProjectDeltatime/WorldTimeAmbientCapture.log`.
- 남은 작업: **확인 불가**. 실제 스피커·헤드폰과 플레이어 조작으로 `정지 → 이동 → DEADLINE → 해제` 순서의 먹먹함·음량·3D 거리감, 런타임 애니메이션 자세에서 Stage1·Stage2 팬 주변의 시각 겹침과 실제 통과 동선을 수동 확인해야 한다. 기존 Tutorial·Prototype·Stage5 스모크 기준선 실패와 Unity 생성 `.csproj`의 오래된 참조는 별도 범위에서 정리해야 한다.

## 2026-08-24 - MainScene 가로형 로고·메뉴·설정 리디자인

- 변경 유형: MainScene 이미지·Unity UI 전면 교체, 그래픽·입력·오디오 설정 및 Credits 추가, 런타임 키 안내·사운드 배율 연동
- 변경 내용: **구현 완료**. ImageGen 내장 모드로 왼쪽 40% 메뉴 여백과 오른쪽 주인공·붉은 탄환·청록 총구/시작 연기를 갖는 `mainMenuBackground.png`, 투명 단일 행 회백/적색 `DELTA TIME` 가로형 `titleLogoWide.png`를 제작했다. MainScene은 두 새 Sprite를 사용하고 기존 `background.png`·`titleLogo.png`는 복구용으로만 보존한다. `START/OPTION/CREDITS/EXIT` 실제 Button, 선택 적색 바·경계·포인터, 마우스·키보드 탐색, 저장된 Next Stage 단축 시작과 모달 중 차단을 연결했다. OPTION은 해상도·창 모드·Quality·VSync, 이동 4방향과 Fire/Throw/Dash/Deadline/Interact/Restart/Next Stage 재바인딩, Master/BGM/SFX 슬라이더를 제공한다. 설정은 초안의 APPLY/CANCEL/RESET DEFAULTS 흐름과 PlayerPrefs 저장을 사용하고, Escape 취소·중복 거부·장치 제한을 적용한다. `PlayerControlsFactory`는 모든 런타임 입력 인스턴스에 저장 JSON을 적용하며 HUD·Tutorial·상호작용 키캡·Ending 안내도 현재 키를 표시한다. `SoundManager`는 기존 믹스에 사용자 3채널 배율을 곱한다. 영문 Credits와 EndingScene의 메뉴/모달 제거도 반영했다.
- 영향을 받은 시스템: MainScene/EndingScene 이미지와 Canvas, Build Settings 시작 흐름, Unity UI 탐색·모달, Screen/Quality/VSync, Input System 바인딩 오버라이드, PlayerPrefs, SoundManager BGM/SFX 믹스, GameHud·Tutorial·Ending 키 안내, 한글화 빌더, 정적·EditMode·PlayMode·Game View 캡처 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Image/mainMenuBackground.png`, `ProjectDeltatime/Assets/_Project/Image/titleLogoWide.png`, `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Scenes/EndingScene.unity`, `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuOptionsController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameSettingsService.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerControlsFactory.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/EndingSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.10.0으로 갱신해 새 이미지, 네 버튼, Option 초안/저장/재바인딩 정책, Credits, 동적 키 안내, SoundManager 배율, Ending 호환성과 최신 검증 상태를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 `MainSceneBuilder.BuildAndValidateFromCommandLine` 1·2회차가 새 에셋 임포터, 참조, 네 Button 콜백, 세 Option 페이지, Credits, Build Settings와 반응형 안전 범위를 통과했다. `MainMenuSettingsEditModeTest`가 초안 격리, 기본값, 무효 해상도·Quality·VSync·볼륨 보정, 바인딩 JSON 왕복, 중복 키와 Escape/장치 제한을 통과했다. `MainMenuPlayModeSmokeTest`가 임시 Next Stage=`K` 저장값의 Main/Ending 표시, Option/Credits 열기·닫기, 모달 중 START 차단, SoundManager 0.8/0.6/0.4 배율과 Tutorial 전환을 통과하고 원 설정을 복원했다. Main/GRAPHICS/KEYS/AUDIO/CREDITS를 1920×1080, 1280×720, 2560×1080, 1024×768에서 캡처한 20개 PNG의 크기 검증이 통과했고 직접 확인한 기준·4:3·울트라와이드 캡처에서 로고 잘림, 메뉴·주인공 겹침, 모달 잘림이 없었다. 로그: `ProjectDeltatime/MainMenuBuild1.log`, `ProjectDeltatime/MainMenuBuild2Retry.log`, `ProjectDeltatime/MainMenuSettingsEditMode.log`, `ProjectDeltatime/MainMenuPlayModeSmoke.log`, `ProjectDeltatime/MainMenuVisualCaptureAllTabs.log`.
- 남은 작업: **확인 불가**. Windows 외 OS 및 실제 standalone 빌드에서의 해상도/Fullscreen 전환과 플레이어가 직접 수행하는 전체 키·마우스 리바인딩의 체감 검증은 필요하다. 소셜 아이콘은 연결 URL이 없어 요청 범위대로 추가하지 않았다.

## 2026-08-24 - MonoSingleton 공용 베이스 클래스 추가

- 변경 유형: 런타임 공용 유틸리티 클래스 신규 추가
- 변경 내용: **구현 완료**. 제네릭 `MonoSingleton<T>` 베이스 클래스를 `Deltatime.Core` 네임스페이스에 추가했다. 지연 접근 `Instance` 프로퍼티는 `FindFirstObjectByType`(비활성 포함)으로 기존 인스턴스를 찾고 없으면 자식 없는 신규 GameObject를 생성해 컴포넌트를 붙인다. `Awake`에서 중복 인스턴스를 파괴하고 `HasInstance`로 파괴 전 접근을 판별하며, `OnApplicationQuit` 이후에는 재생성하지 않는다. `PersistAcrossScenes` 가상 프로퍼티(기본 `true`)로 `DontDestroyOnLoad` 적용을 선택할 수 있고 부모가 있는 오브젝트에는 적용하지 않는다. 현재 이 클래스를 상속하는 시스템은 없으며 기존 `SoundManager` 등 수동 싱글턴은 유지된다.
- 영향을 받은 시스템: 신규 공용 기반만 추가했으며 기존 런타임 동작과 씬·프리팹·에셋은 변경하지 않았다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Core/MonoSingleton.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md` 14장 리팩터링 이후 기술 구조에 공용 베이스 클래스 항목을 추가했다.
- 테스트 결과: **미실행**. CLI 환경에서 Unity 에디터 배치 컴파일을 실행하지 못했다. 스크립트 `.meta`도 다음 에디터 포커스에서 자동 생성된다. 최초 에디터 진입 시 컴파일 성공 여부 확인이 필요하다.
- 남은 작업: **계획 필요**. Unity 에디터 컴파일 검증, 필요 시 `SoundManager` 등 기존 수동 싱글턴의 단계적 채택 검토.

## 2026-08-18 - 총구 높이 기준 바닥·천장 수평 발사

- 변경 유형: 총기 조준 버그 수정, 발사 원점 높이 보정, PlayMode 회귀 테스트 보강
- 변경 내용: **구현 완료**. 이전 바닥·천장 보정은 `FireAimPoint`의 Y를 플레이어 Rigidbody 높이로 고정해, 실제 총구가 그보다 위·아래일 때 여전히 투사체가 바닥 또는 천장을 향하는 문제가 있었다. `PlayerAim`은 수평 표면 또는 Collider 없는 폴백인지 기록하고, `GetFireDirectionFrom(origin)`가 그 경우에만 목표 Y를 전달받은 발사 원점의 `origin.y`로 맞춘다. 따라서 바닥 클릭은 총구 높이와 무관하게 수평이며, 적 등 `IDamageable` Collider와 수직·경사 벽은 실제 접점에 대한 기존 3D 조준을 유지한다.
- 영향을 받은 시스템: 플레이어 일반·`DEADLINE` 총기 발사, 총구 보정값이 있는 모든 권총·자동소총·샷건, 바닥·천장·Collider 없음 클릭, 고저차 적 명중, Stage5 컷어웨이 조준, PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/ElevationFireAimTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.9.6으로 갱신해 수평 표면·폴백의 목표 Y가 플레이어 루트가 아닌 실제 총구 원점이라는 최종 규칙과 회귀 결과를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 PlayMode `ElevationFireAimTests` 3/3이 총구 Y와 플레이어 Y가 다른 수평 바닥 클릭·Collider 없음 폴백의 정확히 0인 Y 방향, 위·아래 `IDamageable` target의 3D 방향·명중, 숨은 전경 Collider 건너뛰기, 적 총기 상·하 방향을 통과했다.
- 남은 작업: **확인 불가**. 실제 Stage5 Game View에서 바닥·천장·벽 클릭과 단상 위·아래 적 명중의 조준 감각은 수동 확인이 필요하다. Stage5 전체 스모크의 기존 weapon pickup 수 1개/기대 2개 불일치는 별도 범위다.

## 2026-08-18 - 바닥·천장 클릭 총기 수평 보정

- 변경 유형: 총기 조준 정책 보정, 바닥 오발 방지, PlayMode 회귀 테스트 확장
- 변경 내용: **구현 완료**. `PlayerAim`은 가장 가까운 유효 Collider가 적 등 `IDamageable`이거나 표면 법선의 절대 Y가 `0.7` 미만인 벽/경사면이면 실제 3D 접점을 계속 사용한다. 법선의 절대 Y가 `0.7` 이상인 수평 바닥·천장 접점은 클릭 X/Z와 플레이어 Rigidbody의 현재 Y를 결합한다. 따라서 바로 앞 바닥 클릭은 총구를 아래로 꺾지 않으며, 높은/낮은 적 Collider와 벽의 실제 고저차 조준은 유지한다. `ShadowsOnly` 컷어웨이 제외, 자기 Collider 제외, Collider 없는 클릭의 수평 평면 폴백과 캐릭터·무기 모델 yaw 규칙은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 총기 조준점·발사 방향, 바닥·천장·벽 Collider 클릭, 고저차 적 명중, `DEADLINE` 총기 발사, Stage5 컷어웨이 조준, PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/ElevationFireAimTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.9.5로 갱신해 적·벽의 실제 접점 3D 조준과 바닥·천장 X/Z 수평 보정의 구분, 법선 임계값 `0.7`, 유지되는 제외·폴백 규칙을 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 PlayMode `ElevationFireAimTests` 3/3이 위·아래 `IDamageable` target의 3D 방향·명중, 숨은 전경 Collider 건너뛰기, 수평 바닥 클릭의 X/Z 보존·Y 보정·수평 총구 방향, Collider 없는 클릭 폴백, 적 총기 상·하 방향을 통과했다. Unity 스크립트 컴파일도 성공했으며 기존 `MainMenuButtonFeedback.cs`의 중복 `TMPro` using 경고만 보고됐다.
- 남은 작업: **확인 불가**. 실제 Stage5 Game View에서 바닥·천장·벽을 각각 클릭했을 때의 조준 감각과 단상 위·아래 적 명중은 수동 확인이 필요하다. Stage5 전체 스모크의 기존 weapon pickup 수 1개/기대 2개 불일치는 별도 범위다.

## 2026-08-17 - 고저차 대응 3D 투사체 조준

- 변경 유형: 총기 조준 리팩터링, 고저차 명중 보정, PlayMode 회귀 테스트·Stage5 검증 갱신
- 변경 내용: **구현 완료**. `PlayerAim`에 실제 총기용 `FireAimPoint`와 `GetFireDirectionFrom(origin)`을 추가했다. 마우스 카메라 Ray의 가장 가까운 활성 비트리거 Collider 실제 접점을 선택하되 플레이어 자신의 Collider와 Renderer가 `ShadowsOnly`인 전경 컷어웨이 Collider는 건너뛰고, 유효 Collider가 없으면 기존 플레이어 Y 수평 평면으로 폴백한다. 기존 `AimPoint`·`AimDirection`·`GetPlanarDirectionFrom`은 캐릭터 yaw, 근접, 투척, 카메라·HUD용 수평 조준으로 유지한다. 플레이어 일반·`DEADLINE` 총기 발사와 적 총기 발사는 총구에서 실제 목표 높이를 포함하는 3D 방향을 사용하며, 적 경고선도 같은 3D 선분을 표시한다. 반동과 캐릭터/무기 모델의 yaw 정렬은 수평 동작을 유지하고, 중력·곡사·유도·선행 조준·모델 pitch는 추가하지 않았다.
- 영향을 받은 시스템: 플레이어 포인터 조준, 권총·자동소총·샷건 일반/`DEADLINE` 발사, 적 총기 점사·경고선, 투사체 SphereCast 명중, Stage5 컷어웨이 검증, PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/ElevationFireAimTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.9.4로 갱신해 수평 조준과 3D 총기 발사의 분리, `ShadowsOnly` 컷어웨이 예외, 폴백, 적 발사·경고선, 범위 제외 항목과 검증 상태를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 배치 PlayMode `ElevationFireAimTests` 3/3이 위·아래 target 3D 투사체 명중, visible Collider 높이·hidden foreground 건너뛰기·수평 평면 폴백, 적 총기 상·하 방향을 통과했다. Stage6 PlayMode 스모크는 NavMesh complete paths 5/5로 통과했다. Stage5 PlayMode 스모크는 새 조준 검증 전에 기존 씬의 weapon pickup 수 1개/기대 2개 불일치로 실패했다. `dotnet build ProjectDeltatime.sln`은 이번 변경과 무관한 `Assets/TutorialInfo/Scripts/Readme.cs` 누락 참조로 실패했다.
- 남은 작업: **확인 불가**. 실제 Stage5의 단상 위·아래에서 마우스로 적·바닥·벽을 클릭했을 때의 명중감과, yaw만 유지하는 무기 모델 시각은 수동 Game View로 확인해야 한다. Stage5 전체 스모크는 기존 pickup 수 기준을 현재 저장 씬에 맞출지 별도 범위에서 결정해야 한다.

## 2026-08-17 - Replay UI와 In-Game Cyber HUD 통합

- 변경 유형: 브랜치 통합, 상태별 HUD 렌더링, 공용 상호작용 안내, 문서 재정리
- 변경 내용: **구현 완료**. `codex/ui-replay-integration`은 `feature/InGameUI`의 Cyber HUD·아이콘·Safe Area·반응형 레이아웃을 일반 InGame과 Tutorial의 기준으로 유지하고, `GameHud`가 `StageReplayController.IsReplaying`이면 Cyber HUD를 그리기 전에 반환하여 Replay 프레임·타임라인·이벤트 마커만 표시한다. Replay 타임라인 기록과 Kill/DEADLINE/Clear/Dead 이벤트 연결·HUD 전용 스모크는 유지했다. `PlayerCombat`의 `PickUp`·`Swap`·`Catch` 상태는 공용 `CyberHudRenderer.DrawWeaponInteractionPrompt`로 옮겨 E 키와 한국어 행동명을 Cyber 스타일로 표시한다. 일반 HUD에서는 조작 안내 위, Tutorial에서는 수업 패널 위에 Safe Area와 현재 배율을 따라 배치하며, 안내 대상이 없거나 Tutorial 완료 또는 Replay 중에는 표시하지 않는다.
- 영향을 받은 시스템: `GameHud`, `TutorialHud`, 공용 Cyber HUD 렌더러, Replay 타임라인·이벤트, 플레이어 무기 상호작용 정책, HUD 아이콘/무기 아이콘 참조, EditMode·Replay·Tutorial HUD 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/CyberHudPresentation.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombatSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/HudPresentationTests.cs`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/CoreBehaviorTests.cs`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md` 1.9.3에 이 통합 규칙을 최신 HUD 기준으로 추가했다. 이전 브랜치의 테스트 기록은 이 통합의 최신 결과로 사용하지 않는다.
- 테스트 결과: **확인 불가**. Unity 6000.1.13f1 배치 컴파일과 선택 EditMode 테스트를 새 worktree에서 요청했으나, 라이선스 entitlement는 확인된 뒤에도 Package Manager가 `Library/PackageCache`를 재구성하는 중 `ENOSPC: no space left on device`로 종료 코드 1을 반환했고 결과 XML을 생성하지 않았다. 따라서 실제 컴파일·테스트는 실행됐다고 판정할 수 없다. Replay 일반/Replay HUD 전용, Tutorial/InGame HUD 스모크와 1920×1080·1280×720 캡처도 디스크 공간 확보 전에는 미실행이다. 통합 코드의 `git diff --check`는 문서 충돌 해결 후 다시 실행 대상으로 남긴다.
- 남은 작업: **계획 필요**. 디스크 공간을 확보한 환경에서 컴파일, `HudPresentationTests`, `CoreBehaviorTests`, Replay 일반·HUD 전용 스모크, Tutorial 스모크, `HudVisualCapture`를 새로 실행하고 결과를 이 항목에 갱신해야 한다.

## 2026-08-17 - In-Game HUD 외곽 프레임 제거

- 변경 유형: UI 시각 정리, 공용 OnGUI 렌더링·레이아웃 검증 갱신
- 변경 내용: **구현 완료**. 본편과 Tutorial의 인게임 HUD에서 화면 네 모서리 브래킷, 화면 가장자리 중간 눈금과 이를 연결하던 외곽 프레임을 제거했다. 정보 패널의 절단 모서리·청흑색 바탕·가는 청록 윤곽과 모든 상태/조작/수업 정보는 유지한다. 따라서 화면 프레임형 각진 UI는 별도로 구현한 리플레이 UI에만 남고, 인게임 HUD는 독립형 상태 패널만 표시한다.
- 영향을 받은 시스템: 본편·Tutorial HUD 공용 OnGUI 렌더링, Safe Area 레이아웃, HUD EditMode 배치 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/CyberHudPresentation.cs`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/HudPresentationTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.9.2로 갱신해 외곽 프레임은 리플레이 전용이며 인게임 HUD에서 사용하지 않는 현재 표현 규칙을 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 `HudAssetBuilder.BuildAndValidateFromCommandLine`으로 컴파일·HUD 아이콘 연결 검증을 통과했고, `HudPresentationTests` EditMode 21/21이 통과했다. `HudVisualCapture`로 Stage1·Tutorial의 1920×1080/1280×720 캡처 4건을 새로 생성·검증했으며, 실제 이미지에서 화면 가장자리 브래킷과 외곽 프레임이 제거되고 독립형 정보 패널이 안전 영역 안에 유지됨을 확인했다. Prototype/Tutorial 전체 스모크는 외곽 프레임 시각 변경과 직접 관계가 없어 재실행하지 않았다.
- 남은 작업: **확인 불가**. Stage5·Stage6 및 앰버 완료/주의 상태의 별도 Game View 캡처는 이전 HUD 변경 범위부터 미실행이다.

## 2026-08-17 - 리플레이 톤 In-Game HUD 및 아이콘 재설계

- 변경 유형: 기능 수정, UI 시각 통합, 반응형 레이아웃, ImageGen 에셋 교체, 에디터 검증·테스트·캡처 도구 갱신
- 변경 내용: **구현 완료**. 본편 `GameHud`와 `TutorialHud`의 공용 OnGUI HUD를 리플레이 장면과 같은 어두운 산업형 전술 인터페이스로 재설계했다. `Screen.safeArea` 기반 분절형 코너 프레임, 절단 모서리 청흑색 패널, 가는 청록 선, Noto Sans KR, 실제 윤곽 키캡을 적용한다. 청록은 기본·활성 상태에, 앰버는 클리어·튜토리얼 완료·저체력·탄약/충전 소진 같은 완료/주의 상태에만 사용한다. 스테이지, 체력, `DEADLINE`, 무기/탄약, 시계 다이얼과 라이브·리플레이 배율의 위치·데이터 및 중앙 안내와 수업 흐름은 유지했고, 리플레이 전용 타임라인·`RECORDED VIEW`·`NORMAL`은 추가하지 않았다. 기존 아이콘 8종은 ImageGen으로 다시 제작해 투명 배경의 암회색/회백색 기술 실루엣과 제한된 청록 표시만 남겼으며, HUD 렌더 단계에서 낮은 명도의 회백색으로 틴트한다. 게임플레이, 입력, 무기 교체, 밸런스와 `Time.timeScale`은 변경하지 않았다.
- 영향을 받은 시스템: 본편·Tutorial HUD, 공용 OnGUI 렌더링, 진행 순번 표기, 플레이어 체력, `DEADLINE`, 무기/탄약, 라이브·리플레이 월드 시간, Safe Area/해상도 레이아웃, HUD 아이콘 임포트·연결, Prototype/Tutorial 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/CyberHudPresentation.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialHud.cs`, `ProjectDeltatime/Assets/_Project/Art/UI/HudIcons`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/HudAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/HudVisualCapture.cs`, `ProjectDeltatime/Assets/_Project/Tests/EditMode/HudPresentationTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: **구현 완료**. `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.9.1로 갱신해 산업형 전술 HUD의 프레임·패널·상태 색상·아이콘 규칙, 적용 범위와 실제 검증 상태를 기록했다.
- 테스트 결과: **부분 구현**. Unity 6000.1.13f1 배치 아이콘 임포트·컴파일·PNG 알파·Sprite/밉맵/256px/Clamp/Bilinear·`HudIconSet`/네 무기 연결 검증이 통과했다. HUD EditMode 21/21과 Stage1·Tutorial 1920×1080/1280×720 캡처 4건의 실제 PNG 크기·필수 아이콘 연결 검증이 통과했으며, 네 캡처를 직접 확인해 아이콘 식별성, 글자 잘림, 패널 비중첩, 어두운 배경 대비를 확인했다. Prototype 전체 스모크는 기존 투척 속도/거리/기절 수치·6m 착지·리플레이 본 포즈 3건으로 실패했고, Tutorial 전체 스모크는 기존 Synty 프리팹 수 216/262에서 Play Mode 전에 실패했다. 신규 HUD/아이콘 오류는 보고되지 않았다.
- 남은 작업: **계획 필요**. 기존 Prototype/Tutorial 스모크 기준선을 현재 저장 콘텐츠와 맞출지 별도 기능 범위에서 결정해야 한다. Stage5·Stage6 스모크와 앰버 완료/주의 상태의 별도 캡처는 이번 변경 후 **미실행/확인 불가**다. 색약/접근성, 로컬라이징, 게임패드·입력 장치별 아이콘과 Canvas/UI Toolkit 전환 여부도 제품 UI 단계에서 결정해야 한다.

## 2026-08-15 - 바닥 무기 아웃라인 리플레이 잔류 수정

- 변경 유형: 버그 수정, 리플레이 시각 등록, PlayMode 회귀 테스트 갱신
- 변경 내용: **구현 완료**. 동적으로 생성되거나 무기 교환으로 월드 모델이 바뀐 `WeaponPickup`은 모델과 아웃라인 렌더러 구성 완료 후 `ReplayVisualRegistry`에 전체 렌더러 계층을 즉시 등록한다. 따라서 렌더러 탐색 간격이 `0`인 Stage1·Tutorial에서도 적 드롭·투척 착지·가로채기 교환 등으로 게임 중 생성된 원본 바닥 무기와 황금색 아웃라인이 리플레이 전환 시 함께 숨겨지고, 기록된 리플레이 프록시만 표시된다.
- 영향을 받은 시스템: StageReplayController 렌더러 등록, 동적 바닥 무기, 무기 교환, 적 드롭·투척 착지, Tutorial, 바닥 무기 아웃라인
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/WeaponPickupOutlineTests.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.8.2로 갱신해 동적 바닥 픽업·아웃라인의 이벤트 기반 리플레이 등록과 원본 숨김 정책을 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 PlayMode `WeaponPickupOutlineTests` 3/3 통과. 신규 회귀 테스트는 초기 탐색 뒤 생성된 픽업, `fallbackRendererDiscoveryInterval = 0`, 리플레이 시작 조건에서 원본 무기와 생성된 아웃라인 렌더러가 모두 비활성화되는지 확인한다.
- 남은 작업: **확인 불가**. 실제 Stage1·Tutorial 플레이에서 적 드롭·투척 착지 후 리플레이 화면을 사람 눈으로 확인하는 수동 평가는 미실행이다.

## 2026-08-15 - 바닥 무기 황금색 아웃라인

- 변경 유형: 기능 추가, 시각 피드백, 프리팹·에디터 빌더·테스트 갱신
- 변경 내용: **구현 완료**. 모든 `WeaponPickup`이 현재 월드 모델의 `MeshRenderer`·`SkinnedMeshRenderer`를 대상으로 원본 메시·본을 공유하는 inverted-hull 렌더러를 Play Mode에서 생성한다. Built-in 전용 셰이더와 공유 머티리얼은 고정 황금색 `(1, 0.55, 0.035, 1)`, 화면 기준 2px, `Cull Front`, `ZTest LEqual`, `ZWrite Off`, 렌더 큐 3050을 사용한다. 모든 서브메시를 그리며 원본 머티리얼은 바꾸지 않고 그림자·프로브·모션 벡터·추가 콜라이더를 만들지 않는다. 직접 배치·적 드롭 착지·투척 착지·교환·튜토리얼 지급으로 생긴 바닥 총기와 근접 무기에 적용하며 비행 중 `ThrownWeapon`·`InterceptableWeapon`은 제외한다.
- 영향을 받은 시스템: 바닥 무기 가시성, 무기 획득·교환, 적 드롭·투척 착지, Tutorial 무기 지급, Built-in 렌더링, 픽업 프리팹 재생성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponPickupOutline.cs`, `ProjectDeltatime/Assets/_Project/Shaders/WeaponPickupOutline.shader`, `ProjectDeltatime/Assets/_Project/Materials/WeaponPickupOutline.mat`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Prefabs/WeaponPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/*Pickup.prefab`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Tests/PlayMode/WeaponPickupOutlineTests.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.8.1로 갱신하고 바닥 무기 아웃라인의 적용 대상, 깊이·제한 시야 정책, 렌더링 수치와 검증 상태를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 임포트·컴파일 종료 코드 0, `BuildPlaceableWeaponPickups` 재생성·검증 종료 코드 0, EditMode 17/17 통과, PlayMode 전체 3/3 통과(전용 아웃라인 2건 포함). 최초 전용 PlayMode 실행은 저장 프리팹 컴포넌트 초기화 순서로 1건 실패했으나 활성 조건을 보정한 뒤 최종 실행이 통과했다. 새 셰이더 임포트 오류는 없었다.
- 남은 작업: **확인 불가**. Stage1과 Tutorial의 1920×1080 Game View에서 실제 2px 두께·황금색 대비·벽 가림·제한 시야 경계의 육안 평가는 **미실행**이다. 상호작용 거리, 콜라이더, 무기 밸런스와 월드 시간 동작은 변경하지 않았다.

## 2026-08-14 - 전면 리팩터링 1단계: 기준선·저장소 위생·어셈블리 경계

- 변경 유형: 저장소 정리, 패키지 의존성 정리, 어셈블리 경계 추가, 검증 기반 추가
- 변경 내용: **구현 완료**. 추적 중인 `.DS_Store` 4개를 제거하고 재유입을 차단했다. 문서에서 인용되지 않은 루트 로그 95개를 제거하고 이후 검증 로그 경로를 `Logs/Validation`로 통일했다. 사용되지 않는 `com.unity.multiplayer.center`를 제거하고 `com.unity.test-framework 1.5.1`을 직접 의존성으로 선언했다. Runtime, Editor, EditMode, PlayMode Assembly Definition과 테스트 전용 `InternalsVisibleTo`, `.editorconfig`를 추가했다. `AssetDatabase.GetDependencies` 기반 읽기 전용 에셋 감사 도구를 추가했다.
- 영향을 받은 시스템: 저장소 위생, Unity 패키지, 컴파일 경계, 테스트, 에셋 관리
- 관련 파일: `ProjectDeltatime/.gitignore`, `ProjectDeltatime/.editorconfig`, `ProjectDeltatime/Packages/manifest.json`, `ProjectDeltatime/Packages/packages-lock.json`, `ProjectDeltatime/Assets/_Project/Deltatime.Runtime.asmdef`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Deltatime.Editor.asmdef`, `ProjectDeltatime/Assets/_Project/Tests/`, `ProjectDeltatime/Assets/_Project/Scripts/AssemblyInfo.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ProjectAssetDependencyAudit.cs`, `mdFile/REFACTORING_AUDIT.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md` 1.8.0에 현재 어셈블리·테스트 경계와 보수적 에셋 감사 정책을 기록했다.
- 테스트 결과: **구현 완료**. 기준선 및 단계별 Unity 6000.1.13f1 배치 컴파일 종료 코드 0. 에셋 감사는 루트 77개, 의존성 1,554개, 후보 15개, 누락 빌더 경로 3개, 고아 `.meta` 0개를 보고했다.
- 남은 작업: **계획 필요**. 감사 후보 15개는 삭제하지 않았으며 실제 사용 여부를 사람의 콘텐츠 판단과 함께 재검토해야 한다.

## 2026-08-14 - 전면 리팩터링 2단계: 스모크·SceneBuilder 공용 기반

- 변경 유형: 에디터 도구 리팩터링, 중복 제거
- 변경 내용: **구현 완료**. 기존 10개 PlayMode 스모크의 콜백 수명과 씬 열기/PlayMode 진입을 `CommandLineSmokeRunner`로 통합했다. SceneBuilder 공통 기능을 실행, 검증, 캐릭터 설정, NavMesh, 프리뷰 캡처 역할로 분리했다. 모든 기존 메뉴와 `RunFromCommandLine()` 진입점은 유지했다.
- 영향을 받은 시스템: Prototype, Tutorial, Stage1/3/4/5/6, Replay, SoundManager, DEADLINE 스모크, Main/Prototype/Tutorial/Stage3/4/5/6/WeaponCalibration/Ending 빌더
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/CommandLineSmokeRunner.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SceneBuilderInfrastructure.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/*PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/*SceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md` 1.8.0에 에디터 검증과 SceneBuilder 공용 구조를 기록했다.
- 테스트 결과: **부분 구현**. Stage1 캐릭터 애니메이션, Stage6, SoundManager 스모크는 통과했고 Replay는 일괄 1차 실패 후 단독 재실행 통과했다. Prototype 투척 설정, Tutorial Synty 개수, Stage3/4 파일명, Stage5 픽업 개수, DEADLINE visual의 Build Settings상 Stage6 제외는 기존 또는 현재 콘텐츠 기준 실패로 분류했다. 자세한 서명은 `mdFile/REFACTORING_AUDIT.md`에 기록했다.
- 남은 작업: **확인 불가**. 리팩터링 전 독립 임시 프로젝트의 빌더 산출물을 작업 시작 전에 보존하지 않아 전후 YAML/GUID 바이트 대조는 수행하지 못했다. 현재 Git에는 씬·프리팹·ScriptableObject 변경이 없다.

## 2026-08-14 - 전면 리팩터링 3단계: 런타임 책임 분해

- 변경 유형: 런타임 구조 리팩터링, 내부 순수 로직 추출, 회귀 테스트 추가
- 변경 내용: **구현 완료**. `StageReplayController`, `EnemyCombatant`, `TutorialDirector`, `PlayerCombat`, `EnemyMotor`, 월드/DEADLINE 시각 피드백의 기존 `MonoBehaviour`, 직렬화 필드, 공개 API를 유지하면서 내부 협력 타입으로 녹화·재생·상태·선택·표현 계산을 분리했다. 리플레이는 내부 `IReplayCaptureSink`와 활성 레지스트리를 사용하고 호환용 `StageReplayController.ActiveRecorder`를 유지한다. 신규 씬 컴포넌트나 공개 게임플레이 API는 추가하지 않았다.
- 영향을 받은 시스템: 리플레이, 애니메이션 프록시, 투사체/무기 시각 등록, 적 AI, 무기 회수, 튜토리얼 진행, 플레이어 전투, Rigidbody/NavMesh 이동, 월드 시간 시각 피드백
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplaySubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombatSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMovementMath.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/VisualFeedbackState.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md` 1.8.0에 façade와 내부 협력 객체의 현재 책임, 호환성 정책, 20Hz/64MiB 보존을 기록했다.
- 테스트 결과: **구현 완료**. 최종 Unity 배치 컴파일 종료 코드 0, EditMode 17/17 통과, PlayMode 테스트 어셈블리 1/1 통과, Replay 단독 스모크 통과. 입력, 밸런스, 씬·프리팹·ScriptableObject에는 Git 변경이 없다.
- 남은 작업: **계획 필요**. 현재 콘텐츠 기준으로 실패하는 Prototype/Tutorial/Stage3/4/Stage5/DEADLINE visual 스모크는 기능 수정 범위를 분리해 처리해야 한다.

## 2026-08-14 - 전면 리팩터링 4단계: 감사 문서와 최종 검증

- 변경 유형: 감사 문서 추가, 설계 문서 갱신, 최종 검증
- 변경 내용: **구현 완료**. 기준선, 제거/보존 항목, 에셋 후보, 아키텍처, 알려진 실패와 검증 결과를 `mdFile/REFACTORING_AUDIT.md`에 기록하고 설계 문서를 1.8.0으로 갱신했다.
- 영향을 받은 시스템: 프로젝트 문서, 품질 기준선, 향후 유지보수
- 관련 파일: `mdFile/REFACTORING_AUDIT.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: 기존 의사결정과 변경 이력을 보존하고 리팩터링 이후 기술 구조를 추가했다.
- 테스트 결과: **부분 구현**. Unity 컴파일, EditMode, PlayMode, 통과 가능한 기존 스모크와 에셋 감사를 실행했다. Stage6 성능 벤치마크는 90프레임 워밍업/300프레임 샘플을 종료 코드 0으로 완료했으나 실제 배치 해상도 321×531로 인해 1080p 60 FPS 판정은 **확인 불가**다. 참고값은 CPU 평균 21.67ms/p95 48.97ms, GPU 평균 17.80ms/p95 44.99ms다. 최종 Git 상태, diff, diff check는 작업 종료 직전에 확정한다.
- 남은 작업: **확인 불가**. 실제 Game View 체감과 독립 빌더 산출물 전후 비교는 이번 자동 검증 범위에서 확인하지 못했다.

## 2026-08-13 - 적·무기 설계 문서 구조 정리

- 변경 유형: 기획서 구조 개선, 구현 기준 수치 정리
- 변경 내용: **문서화 완료**. 플레이어 설계의 `규칙 → 공통 스탯 → 유형별 스탯 → 행동별 처리 → 구현 상태` 흐름을 적 설계에 적용했다. 적은 감지·마지막 목격 위치·NavMesh 이동·월드 시간·기절·무장 해제·재무장·사망을 공통 규칙과 표로 분리하고, 원거리형·추적형·빈손 적의 수치와 행동을 각각 정리했다. 무기는 `WeaponDefinition` 기준의 공통 규칙과 상호작용 표를 추가하고, 권총·자동소총·샷건·근접 무기의 직렬화 수치·역할·적 AI 점사·투척·가로채기·재장전 상태를 분리해 기록했다. 강아지형 적과 재장전은 현재 근거가 없어 각각 **계획 필요**, **미구현**으로 남겼다.
- 영향을 받은 시스템: 적 감지·이동·전투 AI, 적 기절·무장 해제·재무장·무기 드롭, 플레이어·적 무기 사용, 투사체·근접·투척 판정, 기획서의 전투·무기·적 상태 기준
- 관련 파일: `mdFile/PROJECT_DESIGN_DOCUMENT_NOTION_FILLED.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `EnemyPerception.cs`, `EnemyMotor.cs`, `EnemyCombatant.cs`, `EnemyHealth.cs`, `EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `WeaponController.cs`, `Projectile.cs`, `ThrownWeapon.cs`, `InterceptableWeapon.cs`, `WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`, `MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `Stage2.unity`, `Stage5.unity`, `Stage6.unity`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT_NOTION_FILLED.md`의 적·무기 설계를 플레이어 설계와 같은 상세 구조로 확장했다. `mdFile/PROJECT_DESIGN_DOCUMENT.md`의 전투·무기·적 요약도 원거리형 탐지 `18m`, 추적형 탐지 `20m`, 실제 무기 상호작용 및 투척 수치와 일치하도록 갱신했으며 문서 버전을 `1.7.1`, 마지막 분석일을 `2026-08-13`으로 갱신했다.
- 테스트 결과: **정적 대조 완료**. `EnemyCombatant.cs`, `EnemyMotor.cs`, `EnemyPerception.cs`, `WeaponDefinition.cs`, `WeaponController.cs`, 투척 관련 프리팹과 Pistol·AutomaticRifle·Shotgun·MeleeWeapon ScriptableObject, Stage1·Stage2·Stage5·Stage6 직렬화 값을 대조했다. 이번 변경은 문서 작업이므로 Unity 컴파일·PlayMode 스모크·실제 키보드/마우스 전투는 **미실행**이다.
- 남은 작업: **확인 불가**. 실제 플레이테스트로 적의 예고선·거리 유지·주먹 우선·무기 재무장과 네 무기의 명중감·손 그립·투척 가로채기 가독성을 확인해야 한다. 재장전과 강아지형 적은 별도 기획이 필요하다.

## 2026-08-12 - 실제 구현 기준 역기획 및 기획서 기준선 갱신

- 변경 유형: 문서 역기획, 구현 상태 정합성 보정
- 변경 내용: **문서화 완료**. AGENTS.md를 기준으로 현재 코드·저장 씬·프리팹·ScriptableObject·Input Action·Build Settings·에디터 스모크 코드·기존 테스트 로그를 대조했다. MainScene→Tutorial→Stage1→Stage2→Stage5→EndingScene→MainScene의 현재 활성 흐름, 월드 시간·`DEADLINE`, 제한 시야, Replay, 무기·적·튜토리얼·HUD·오디오의 실제 동작과 수치를 역기획 기준선으로 정리했다. 저장된 Stage3/4 씬 파일명(`Stage3_NoUse.unity`, `Stage_NoUse.unity`)과 Builder/Smoke 코드가 참조하는 `Stage3.unity`/`Stage4.unity` 사이의 불일치도 기록했다. 코드·씬·프리팹·ScriptableObject·입력 설정은 수정하지 않았고 Builder도 실행하지 않았다.
- 영향을 받은 시스템: 프로젝트 진행 흐름 문서, 전투·무기·적·월드 시간·DEADLINE·시야·Replay·튜토리얼·스테이지·HUD·오디오·카메라·애니메이션·성능 검증 상태의 문서 기준선
- 관련 파일: `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`, `AGENTS.md`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageSceneFlow.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.7.0으로 갱신했다. 기존 변경 이력과 의사결정은 보존하고, 2026-08-12 최신 역기획 기준선·전체 루프·씬 흐름·시스템 의존 관계·수치·상태 표·근거 파일·불일치·미검증 항목·후속 과제를 추가했다. 과거 문서의 사운드 미구현 문장은 현재 `SoundManager` 구현과 충돌하므로 현재 구현 기준으로 보정했다.
- 테스트 결과: **기존 로그 확인**. `TutorialSmoke.log`, `Stage5FinalSmoke.log`, `Stage6Smoke.log`, `ReplayAnimatorPlayModeFinal5.log`, `DeadlineVisualFeedbackSmoke.log`, `SoundManagerStageBgmSmoke.log`의 기존 결과 범위를 문서에 반영했다. `ReplayVisionPrototypeSmoke.log`와 `ReplayVisionStage5Smoke.log`의 실패 이력, `Stage6PerformanceBenchmark.log`의 1080p 판정 불가도 함께 기록했다. 이번 변경은 문서 작업이므로 Unity 컴파일·PlayMode 스모크·수동 Game View/청감 테스트는 **미실행**이다.
- 남은 작업: **확인 불가**. Stage3/4 파일명과 Builder/Smoke 경로를 최신 저장 씬 기준으로 정리할지 결정하고, Replay 실패 항목, 실제 입력·청감·HUD/시야 가독성, 1920×1080 Stage6 성능, 재장전·저장·게임패드·사용자 음량 설정의 기획 필요성을 후속 검증한다.

## 2026-08-10 - 스테이지 HUD 상·하단 재배치

- 변경 유형: GameHud 레이아웃 수정, 기획 문서 갱신
- 변경 내용: **구현 완료**. 리플레이 결과·조작 안내와 활성 `DEADLINE` 행동 안내를 우상단에서 가운데 상단으로 옮겼다. 좌상단 상태 패널은 적·시간·대시·`DEADLINE` 충전만 표시하도록 `330×178`로 축소했으며, 체력과 무기/탄약은 하단 조작 안내 위 14px 간격의 `330×76` 좌하단 패널로 분리했다. 입력·전투·리플레이 동작과 일반 사망/클리어 중앙 메시지는 유지했다.
- 영향을 받은 시스템: GameHud 리플레이·DEADLINE 안내 위치, 체력·무기/탄약 상태 표시, 스테이지 화면 가독성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.61로 갱신해 상태별 HUD 영역과 패널 크기를 기록했다.
- 테스트 결과: **구현 완료**. 정적 대조로 리플레이·활성 DEADLINE이 같은 가운데 상단 좌표 계산을 사용하고, 체력·무기/탄약이 좌하단 `vitalStatus` 패널로 분리되며 좌상단 상태 문자열에서 제외되는지 확인하고 `git diff --check`를 통과했다. Unity 컴파일·Play Mode 및 실제 Game View 가독성 확인은 **미실행/확인 불가**다.
- 남은 작업: **확인 불가**. 목표 해상도에서 상단 중앙 안내와 좌하단 체력·무기 패널, 하단 조작 안내 사이의 시각적 간격과 가독성을 수동 확인해야 한다.

## 2026-08-10 - 스테이지 BGM 볼륨 추가 하향

- 변경 유형: 사용자 청감 피드백 반영, 스테이지 BGM 믹스 재조정 및 회귀 검사 갱신
- 변경 내용: **구현 완료**. 사용자 피드백에 따라 Stage1~Stage6 공용 `StageBgm` 기본 출력을 `0.50`에서 `0.35`로 더 낮췄다(직전 값 대비 30% 감소). MainScene·Tutorial·엔딩 BGM 기본 출력 `0.55`와 `DEADLINE` 덕킹 배율 `0.4`는 유지한다. 따라서 `DEADLINE` 중 스테이지 BGM 출력은 `0.14`다. `SoundManagerPlayModeSmokeTest`의 Stage1 출력 검증값도 `0.35`로 갱신했다.
- 영향을 받은 시스템: Stage1~Stage6 BGM 출력, `DEADLINE` BGM 덕킹, 씬 간 BGM 크로스페이드, 오디오 PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.62로 갱신해 현재 스테이지 전용 기본 출력 `0.35`와 과거 조정 이력을 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 `SoundManagerPlayModeSmokeTest.RunFromCommandLine`이 MainScene→Tutorial→Stage1→EndingScene 흐름과 Stage1 `StageBgm`의 크로스페이드 완료 후 출력 `0.35`를 검사해 종료 코드 0으로 통과했다(`ProjectDeltatime/SoundManagerStageBgmSmoke.log`). `git diff --check`도 통과했다.
- 남은 작업: **확인 불가**. 실제 스테이지에서 일반·`DEADLINE` 상태의 BGM 체감 볼륨을 수동 확인해야 한다.

## 2026-08-10 - 스테이지 BGM 볼륨 소폭 하향

- 변경 유형: 스테이지 BGM 믹스 조정, 오디오 회귀 검사 및 기획 문서 갱신
- 변경 내용: **구현 완료**. Stage1~Stage6가 공유하는 `StageBgm` 기본 출력을 `0.55`에서 `0.50`으로 낮췄다(약 9% 감소). MainScene·Tutorial·엔딩 BGM 기본 출력 `0.55`와 `DEADLINE` 덕킹 배율 `0.4`는 유지한다. 크로스페이드 중에는 각 BGM `AudioSource`가 실제 재생 중인 클립별 출력을 사용하므로, 스테이지 진입·이탈 전환에서도 스테이지 곡에만 낮아진 값이 적용된다. `SoundManagerPlayModeSmokeTest`는 MainScene→Tutorial→Stage1→EndingScene 흐름에서 Stage1의 BGM 선택 및 출력 `0.50`을 검사한다.
- 영향을 받은 시스템: Stage1~Stage6 BGM 출력, 씬 간 BGM 크로스페이드, `DEADLINE` BGM 덕킹, 오디오 PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.60으로 갱신해 스테이지 전용 기본 출력 `0.50`, 비스테이지 BGM `0.55` 유지, 크로스페이드·덕킹 정책과 확인 상태를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 `SoundManagerPlayModeSmokeTest.RunFromCommandLine`은 MainScene→Tutorial→Stage1→EndingScene 흐름과 Stage1 `StageBgm`의 크로스페이드 완료 후 출력 `0.50`을 검사해 종료 코드 0으로 통과했다(`ProjectDeltatime/SoundManagerStageBgmSmoke.log`). `git diff --check`도 통과했다. `dotnet build ProjectDeltatime.sln`은 기존 누락 파일 `Assets/TutorialInfo/Scripts/Readme.cs` 및 Unity 패키지 참조 경고로 실패했으므로 이 변경의 .NET 빌드 결과는 **확인 불가**다.
- 남은 작업: **확인 불가**. 실제 스테이지에서 일반·`DEADLINE` 상태의 BGM 체감 볼륨과 메뉴/튜토리얼/엔딩 전환 시 상대 음량을 수동 확인해야 한다.

## 2026-08-10 - 스테이지 DEADLINE 발동 안내 제거

- 변경 유형: GameHud 안내 문구 제거, 기획 문서 갱신
- 변경 내용: **구현 완료**. 일반 스테이지에서 `DEADLINE`이 사용 가능한 경우 상단 중앙에 표시되던 `Q를 눌러 DEADLINE 발동` 안내를 제거했다. 활성 `DEADLINE`의 우상단 행동 수·이동 실행 안내는 유지하며, `Q` 바인딩·충전·재사용 대기·발동·전투 동작은 변경하지 않았다.
- 영향을 받은 시스템: GameHud 일반 플레이 상단 안내, DEADLINE 활성 행동 패널, 게임플레이 화면 가독성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.59로 갱신해 제거된 상단 발동 안내와 유지되는 활성 안내를 기록했다.
- 테스트 결과: **구현 완료**. 정적 대조로 비활성 `DEADLINE`은 피드백을 그리지 않고, 활성 상태만 기존 우상단 패널을 그리는지 확인했다. `git diff --check`를 통과했다. Unity 컴파일·Play Mode와 실제 Game View 확인은 **미실행/확인 불가**다.
- 남은 작업: **확인 불가**. 실제 스테이지에서 상단 중앙 안내가 사라지고 `DEADLINE` 활성 뒤 행동 안내가 유지되는지 수동 확인해야 한다.

## 2026-08-10 - Stage5 왼쪽 아래 단상 계단 NavMesh 복구

- 변경 유형: Stage5 이동 경로 버그 수정, 현재 씬 NavMesh 재베이크 도구 추가, 기획 문서 갱신
- 변경 내용: **구현 완료**. 왼쪽 아래 단상의 `SM_Bld_Steps_01` 콜라이더가 런타임 이동을 위해 비활성화된 상태로 재베이크되어 계단 경로가 NavMesh에서 사라지던 문제를 수정했다. `Tools/Prototype/Rebake Current Stage 5 Navigation`은 현재 열린 Stage5만 대상으로 계단 콜라이더를 베이크 전에 복원하고, 가구 상면을 제외한 NavMesh 생성 뒤에는 `NavMeshGroundMovement`의 높이 투영을 유지하도록 계단 물리 콜라이더를 다시 비활성화한다. 전체 Stage5를 원본 씬에서 재생성하지 않는다.
- 영향을 받은 시스템: Stage5 왼쪽 아래 단상 계단의 이동 경로, NavMeshSurface Physics Collider 수집, Rigidbody/NavMesh 높이 이동, 카메라·남쪽 컷어웨이 NavMesh 종속값
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Level/NavMeshGroundMovement.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.58로 갱신해 Stage5 단상 계단의 높이 이동·현재 씬 재베이크 순서·검증 상태를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드에서 계단 콜라이더 복원 → NavMesh 재베이크 → 런타임 계단 콜라이더 6개 재비활성화 → 전용 NavMesh 참조·삼각형·계단 주변 NavMesh·가구 상면 제외·높이 이동 구성 검증을 통과했다. 임시 명령 제거 후 Unity 컴파일도 오류 없이 완료됐으며 로그는 `ProjectDeltatime/Stage5LeftPlatformRepair.log`, `ProjectDeltatime/Stage5LeftPlatformRepairCompile.log`에 남았다.
- 남은 작업: **확인 불가**. 실제 Play Mode에서 플레이어가 왼쪽 아래 단상 계단을 오르내리는 수동 조작은 미실행이다. 전체 Stage5 구조 검증은 현재 저장 씬의 무기 픽업 수가 기존 기대값 2개보다 적은 1개인 구성 문제 때문에 별도로 통과하지 못하며, 이 문제는 이번 수정 범위 밖이다.

## 2026-08-10 - 야구방망이 휘두름음 전용 Swish 교체

- 변경 유형: 근접 전투 오디오 자산 교체, 사운드 라이브러리 빌더·기획 문서 갱신
- 변경 내용: **구현 완료**. 기존 Kenney `knifeSlice` 기반 OGG 두 개를 OpenGameArt Swishes Sound Pack의 CC0 `swish-5.wav`, `swish-6.wav`로 교체해 `SFX_Bat_Swing_01.wav`, `SFX_Bat_Swing_02.wav`에 배치했다. 기존 방망이 스윙 슬롯의 Unity `.meta` GUID를 유지했으므로 `DeltatimeSoundLibrary`의 두 `batSwingClips` 참조와 런타임 재생 경로는 바뀌지 않는다. `SoundLibraryBuilder`도 새 WAV 경로를 사용한다.
- 영향을 받은 시스템: 방망이 휘두름 3D 효과음, 사운드 라이브러리 에셋 참조, 에디터 사운드 라이브러리 재구축
- 관련 파일: `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_01.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_02.wav`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/README.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.57로 갱신해 실제 Swish 원본·현재 WAV 파일·검증 제한을 기록했다.
- 테스트 결과: **확인 불가**. 두 WAV 원본과 프로젝트 사본의 SHA-256이 각각 일치하고, 보존한 `.meta` GUID 두 개가 `DeltatimeSoundLibrary.asset`의 `batSwingClips` 참조와 일치함을 정적으로 확인했다. Unity 6000.1.13f1 배치 재구축·PlayMode 스모크는 열린 다른 Unity 인스턴스 때문에 **미실행**이다.
- 남은 작업: **미실행**. 열린 Unity에서 컴파일이 끝난 뒤 `SoundLibraryBuilder.BuildAndValidateFromCommandLine`, `SoundManagerPlayModeSmokeTest.RunFromCommandLine` 및 실제 Game View 청감을 실행해야 한다.

## 2026-08-10 - Stage5 현재 씬 NavMesh 재베이크

- 변경 유형: 씬 내비게이션 데이터 재생성
- 변경 내용: **구현 완료**. 저장된 현재 `Stage5.unity`의 기존 NavMeshSurface 설정을 기준으로 `Stage5Navigation.asset`을 다시 베이크했다. 베이크 중 가구 콜라이더 81개를 임시 `Not Walkable` 처리해 상단 표면이 이동면으로 생성되지 않게 했고, 재베이크된 NavMesh 경계에 맞춰 Stage5 카메라와 남쪽 외곽 컷어웨이의 NavMesh 종속 값을 다시 저장했다. Stage4 또는 Synty 원본 씬을 다시 복제하거나 현재 씬의 게임플레이 구성을 재생성하지 않았다.
- 영향을 받은 시스템: Stage5 이동 경로 데이터, 가구 상단 이동면 제외, 카메라 이동 경계, 남쪽 외곽 컷어웨이
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`
- 기획서 반영 내용: 검토 완료. 기존 Stage5 이동·카메라 설계의 구현 데이터 재생성이므로 `mdFile/PROJECT_DESIGN_DOCUMENT.md` 변경은 없다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드에서 전용 NavMesh 검증을 통과했다. 전용 에셋 참조, 삼각형 생성, 가구 상단 NavMesh 제외를 확인했으며, 로그는 `ProjectDeltatime/Stage5NavMeshRebake.log`에 남았다. 임시 배치 명령 제거 후 Unity 배치 모드가 종료 코드 0으로 프로젝트를 다시 로드했다.
- 남은 작업: **확인 불가**. 전체 Stage5 구조 검증은 현재 저장된 씬이 무기 픽업 1개만 포함해 기존 기대값(2개)을 충족하지 않아 통과하지 못했다. 이 구성 문제는 NavMesh 재베이크 범위 밖이므로 변경하지 않았으며, 실제 Play Mode 이동·경로 추적 수동 검증도 미실행이다.

## 2026-08-10 - Tutorial·리플레이·DEADLINE HUD 텍스트 잘림 보정

- 변경 유형: HUD 타이포그래피·레이블 영역 수정, 기획 문서 갱신
- 변경 내용: **구현 완료**. Tutorial 하단 진행 패널의 청록색 단계 제목을 23pt에서 20pt로 낮추고 높이를 32px에서 36px으로 늘렸으며, 안내·진행 텍스트 영역도 이에 맞춰 재배치했다. 우상단 리플레이 결과·조작 안내와 활성 `DEADLINE` 행동 안내는 일반 중앙 메시지의 24pt 대신 전용 20pt 메시지 글꼴을 사용해 여러 줄이 패널에서 잘리지 않게 했다. 일반 중앙 메시지의 크기와 다른 게임 로직은 유지했다.
- 영향을 받은 시스템: TutorialHud 하단 진행 패널, GameHud 리플레이·DEADLINE 우상단 오버레이, UI 가독성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.56으로 갱신해 상태별 텍스트 크기·영역 보정과 확인 상태를 기록했다.
- 테스트 결과: **구현 완료**. 정적 대조로 Tutorial 제목의 20pt/36px 레이블, 우상단 리플레이·DEADLINE의 전용 20pt 메시지 스타일 연결을 확인하고 `git diff --check`를 통과했다. Unity 컴파일·Play Mode 및 실제 Game View 가독성 확인은 **미실행/확인 불가**다.
- 남은 작업: **확인 불가**. 목표 해상도에서 Tutorial의 긴 단계 제목·지시문, 리플레이 4줄, DEADLINE 3줄의 실제 줄바꿈과 여백을 수동 확인해야 한다.

## 2026-08-10 - 야구방망이 빗나감 휘두름 효과음

- 변경 유형: 근접 전투 오디오 추가, 사운드 라이브러리·PlayMode 회귀 검사·기획 문서 갱신
- 변경 내용: **구현 완료**. CC0 Kenney RPG Audio의 `knifeSlice.ogg`, `knifeSlice2.ogg`를 `SFX_Bat_Swing_01.ogg`, `SFX_Bat_Swing_02.ogg`로 추가했다. `MeleeAttackExecution`은 유효한 방망이 공격 시작마다 공격자 위치에서 두 클립 중 하나를 재생하며, 대상이 없거나 사거리·시야 판정에 실패해도 재생한다. 실제 적중 시 기존 `SFX_Bat_Hit_01/02.ogg`는 그대로 별도 재생한다. 애니메이터 없는 `WeaponController` 즉시 판정 경로도 같은 휘두름음을 한 번 재생하며, 주먹·총기·투척에는 적용하지 않는다. `SoundLibrary` 구성·검증과 빌더·Resources 에셋 참조, `SoundManagerPlayModeSmokeTest`의 빗나감 1회·적중 시 휘두름/적중음 동시 재생 검증을 추가했다.
- 영향을 받은 시스템: 플레이어·적 방망이 근접 공격, 애니메이션/즉시 근접 판정, 3D 공간 효과음 풀, 사운드 라이브러리 직렬화, 오디오 PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_01.ogg`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_02.ogg`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundLibrary.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/README.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.55로 갱신해 휘두름음의 대상 무관 재생·적중음 공존·적용/제외 범위·검증 제한을 기록했다.
- 테스트 결과: **확인 불가**. 원본 `knifeSlice` 두 파일과 프로젝트 사본의 SHA-256은 각각 일치했고, 새 Unity `.meta` GUID가 `DeltatimeSoundLibrary.asset`의 `batSwingClips` 두 참조와 일치한다. 새 코드·빌더·스모크의 정적 연결도 확인했다. Unity 6000.1.13f1 배치 사운드 라이브러리/PlayMode 스모크는 열린 다른 Unity 인스턴스 때문에 `HandleProjectAlreadyOpenInAnotherInstance`에서 실행되지 않았다. `dotnet build Assembly-CSharp-Editor.csproj`는 기존 누락 파일 `Assets/TutorialInfo/Scripts/Readme.cs` 때문에 실패했으며, 새 코드의 컴파일 결과를 독립 확인할 수 없다.
- 남은 작업: **미실행**. 열린 Unity에서 컴파일 완료 후 `SoundLibraryBuilder.BuildAndValidateFromCommandLine`과 `SoundManagerPlayModeSmokeTest.RunFromCommandLine`을 실행하고, Game View에서 빗나감·적중 각각의 음량과 두 소리의 구분을 수동 확인해야 한다.

## 2026-08-10 - 리플레이·DEADLINE HUD 우상단 배치

- 변경 유형: HUD 레이아웃 수정, 기획 문서 갱신
- 변경 내용: **구현 완료**. 리플레이 중 중앙에 표시되던 결과·조작 안내 패널과 활성 `DEADLINE` 중 하단 중앙에 표시되던 행동 수·실행 안내 패널을 우상단으로 옮겼다. 두 패널은 화면 우측·상단에서 각각 18px 여백을 두고 최대 폭 330px을 사용한다. 일반 플레이와 리플레이가 아닌 사망·클리어 메시지 위치, 입력·전투·리플레이 동작은 변경하지 않았다.
- 영향을 받은 시스템: GameHud 리플레이 결과·재시작/다음 스테이지 안내, DEADLINE 행동 수·이동 실행 안내, 게임플레이 화면 가독성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.54로 갱신해 HUD 현황과 상태별 중앙·우상단 패널 배치를 기록했다.
- 테스트 결과: **구현 완료**. 정적 대조로 리플레이 패널이 `replay.IsReplaying`일 때 우상단 계산을 사용하고, 활성 `DEADLINE` 패널도 같은 우상단 계산을 사용하는지 확인했다. `git diff --check`는 통과했다. Unity 컴파일·Play Mode와 실제 Game View 해상도별 가독성 확인은 **미실행/확인 불가**다.
- 남은 작업: **확인 불가**. 목표 해상도에서 리플레이 4줄 안내와 DEADLINE 3줄 안내의 줄바꿈·가독성, 게임 화면 시야 확보를 수동 확인해야 한다.

## 2026-08-10 - UI 마우스 버튼 표기 통일

- 변경 유형: UI 조작 안내 문구 수정, 기획 문서 갱신
- 변경 내용: **구현 완료**. 게임플레이 HUD와 Tutorial 단계 안내에 표시되는 모든 LMB/RMB 문구를 각각 `LMB - 좌 클릭`, `RMB - 우 클릭` 형식으로 통일했다. 실제 Input System 좌·우 버튼 바인딩과 공격·투척 동작은 변경하지 않았다.
- 영향을 받은 시스템: 게임플레이 HUD 하단 조작 안내, Tutorial 근접·권총·투척·DEADLINE 단계 지시
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.53으로 갱신해 조작 표와 HUD 설명에 동일 표기를 기록했다.
- 테스트 결과: **구현 완료**. 정적 문자열 검색으로 런타임 UI의 모든 LMB/RMB 안내가 새 표기를 사용하는지 확인하고 `git diff --check`를 통과했다. Unity 컴파일·Play Mode 및 실제 Game View 줄바꿈·가독성 확인은 문구 변경만으로 **미실행/확인 불가**다.
- 남은 작업: **확인 불가**. 목표 해상도의 실제 Game View에서 길어진 HUD·Tutorial 문구의 줄바꿈과 가독성을 수동 확인해야 한다.

## 2026-08-10 - Stage5 이후 EndingScene 직행 및 Stage6 임시 제외

- 변경 유형: 진행 경로 수정, Build Settings 수정
- 변경 내용: **구현 완료**. 본편 진행 목록을 `Stage1 → Stage2 → Stage5 → EndingScene → MainScene`으로 변경했다. Stage5 클리어 후 `StageController`가 `EndingScene`을 다음 목적지로 선택하도록 `StageSceneFlow`에서 Stage6를 임시 제외했으며, `GameBuildSceneCatalog`와 직렬화된 `EditorBuildSettings.asset`에서도 Stage6 씬을 제외했다. Stage6 씬·에셋·스크립트는 삭제하지 않고 보존한다.
- 영향을 받은 시스템: StageController의 다음 씬 전환, 본편 플레이 경로, Unity Build Settings, Stage5/EndingScene 빌드 도구
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Level/StageSceneFlow.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/GameBuildSceneCatalog.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.52로 갱신하고 Stage6 에셋 보존·진행/빌드 임시 제외 상태와 현재 경로를 기록했다.
- 테스트 결과: **부분 구현**. StageSceneFlow·빌드 카탈로그·직렬화된 Build Settings의 정적 대조는 통과했다. `dotnet build ProjectDeltatime.sln`은 기존 생성 C# 프로젝트의 누락 파일 `Assets/TutorialInfo/Scripts/Readme.cs` 참조로 실패했다. Unity PlayMode 클리어 입력 검증은 실행 중인 Unity 인스턴스가 있어 **미실행/확인 불가**다.
- 남은 작업: **계획 필요**. Stage6를 다시 활성화할 때 `StageSceneFlow`, `GameBuildSceneCatalog`, `ProjectSettings/EditorBuildSettings.asset`에 Stage6를 복원하고 Stage5→Stage6→EndingScene 경로를 재검증해야 한다.
- 실행하지 않은 테스트는 `미실행`, 결과를 확인할 수 없으면 `확인 불가`로 적는다.
- 기획서에 영향이 있으면 `mdFile/PROJECT_DESIGN_DOCUMENT.md`의 변경 위치를 구체적으로 적는다.
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

## 2026-08-10 - EndingScene BGM_Ending 선택 검증

- 변경 유형: 씬 전환 BGM 회귀 검사 추가, 오디오 구현 상태 문서 갱신
- 변경 내용: **구현 완료**. 기존 `SoundManager`는 `EndingScene` 진입 시 `SoundLibrary.EndingBgm`을 선택하고 비반복 재생한다. `SoundManagerPlayModeSmokeTest`는 MainScene에서 Tutorial을 거쳐 EndingScene을 로드한 뒤 현재 BGM이 `EndingBgm`인지 검증하도록 확장했다. `DeltatimeSoundLibrary.asset`의 `endingBgm` 참조 GUID는 `BGM_Ending.mp3`와 일치하며, EndingScene에는 활성 `AudioListener`가 있다.
- 영향을 받은 시스템: 씬별 BGM 선택, MainScene·Tutorial·EndingScene 전환, 오디오 PlayMode 회귀 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `ProjectDeltatime/Assets/_Project/Audio/BGM/BGM_Ending.mp3`, `ProjectDeltatime/Assets/_Project/Scenes/EndingScene.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.51로 갱신해 EndingScene의 `BGM_Ending` 선택 경로, 정적 참조 확인 및 새 PlayMode 검증 범위를 기록했다.
- 테스트 결과: 소스의 `EndingScene → EndingBgm` 분기, SoundLibrary GUID와 `BGM_Ending.mp3.meta` GUID, EndingScene의 활성 `AudioListener`를 정적으로 대조했고 관련 파일 대상 `git diff --check`는 **통과**했다. 새 `SoundManagerPlayModeSmokeTest`는 다른 Unity 인스턴스가 프로젝트를 열고 있어 **미실행**이다.
- 남은 작업: **확인 불가**. 열린 Unity에서 `SoundManagerPlayModeSmokeTest.RunFromCommandLine`을 실행하고, Game View에서 EndingScene 진입 직후 `BGM_Ending`의 실제 재생·볼륨을 수동 확인해야 한다.

## 2026-08-10 - MainScene N 키 Tutorial 시작 및 안내 텍스트

- 변경 유형: MainScene 키보드 시작 입력·TMP 안내 텍스트 추가, 씬 빌더·기획 문서 갱신
- 변경 내용: **구현 완료**. `MainMenuController`가 `PlayerControls.Gameplay.NextStage`의 `N` 키 입력을 감지해 기존 버튼과 같은 `Play()`를 호출한다. 유효한 Build Settings 대상 확인 뒤에만 UI 클릭음을 재생하고 Tutorial을 로드하며, 연속 버튼/키 입력에도 전환 요청은 한 번만 실행된다. MainScene Canvas에는 Noto Sans KR TMP `TutorialKeyHint`를 추가해 `N 키를 눌러 튜토리얼 시작`을 표시한다. `MainSceneBuilder`는 이 텍스트의 폰트·문구·좌상단 앵커·비입력 속성·다중 화면비 안전 영역을 구성·검증한다.
- 영향을 받은 시스템: MainScene 시작 입력, Tutorial 씬 전환, UI 클릭음, Canvas TMP 안내 텍스트, MainScene 씬 생성·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.50으로 갱신해 MainScene의 `N` 키 Tutorial 시작, 동일한 클릭음·대상 검증 경로, `TutorialKeyHint` 문구와 현재 검증 상태를 반영했다.
- 테스트 결과: 소스와 `MainScene.unity`의 `NextStage` N 바인딩·`Play()` 경로, `TutorialKeyHint` TMP 문구·폰트 GUID·앵커·크기, YAML 오브젝트 참조를 정적으로 대조해 **통과**했다. Unity 6000.1.13f1 배치 `MainSceneBuilder.BuildAndValidateFromCommandLine`은 다른 Unity 인스턴스가 프로젝트를 열고 있어 `HandleProjectAlreadyOpenInAnotherInstance` 단계에서 실행되지 않아 **미실행**이다.
- 남은 작업: **확인 불가**. 열린 Unity에서 컴파일 완료 후 MainScene Game View에서 `N` 키로 Tutorial 전환, 클릭음 1회 재생, 안내 문구의 한글 글리프·줄바꿈·해상도별 가시성을 수동 확인해야 한다.

## 2026-08-10 - 리플레이 전체 시야 전환 제거

- 변경 유형: 리플레이 시야 정책 단순화, 입력·HUD·직렬화·자동 검증·기획 문서 갱신
- 변경 내용: **구현 완료**. 리플레이는 기록된 암흑 시야로 고정된다. 전체 시야 토글과 `V` 바인딩, 환경광·안개·Fill Light 변경, 적 강제 표시 데이터와 전용 API를 제거했다. ViewCone 재계산과 두 동적 시야 조명 프록시는 리플레이 시작·Deadline·반복 구간에 계속 적용한다. 일반 플레이의 제한 시야와 Tutorial의 무제한 시야는 유지한다.
- 영향을 받은 시스템: StageReplayController, VisionCone, EnemyCombatant, PlayerControls 입력, StageController, GameHud, Prototype·Stage5·Stage6 씬 직렬화와 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.6.49로 갱신해 리플레이 암흑 시야 고정, 제거된 `V` 조작과 전체 시야 조명 정책을 반영했다.
- 테스트 결과: Unity 6000.1.13f1 배치 컴파일은 **통과**했고 Stage6 PlayMode 스모크는 **통과**했다. Prototype 스모크의 새 암흑 시야 검증은 실패하지 않았으나, 기존 투척 무기 속도·정착 거리와 본 포즈 진단 불일치로 전체 결과는 **실패**했다. Stage5 스모크는 리플레이 검증 전에 기존 남쪽 컷어웨이 비활성 렌더러 오류로 **실패**했다.
- 남은 작업: **확인 불가**. 실제 Game View에서 클리어·사망 리플레이의 ViewCone 경계와 조명 가독성을 수동 확인하고, 투척 무기·본 포즈·Stage5 컷어웨이 기존 스모크 실패를 별도 해결한 뒤 전체 회귀를 재실행해야 한다.

## 2026-08-10 - 적 공격 경고선 이동 추적

- 변경 유형: 적 공격 경고선 런타임 갱신 보강, PlayMode 회귀 검사 및 기획 문서 갱신
- 변경 내용: **구현 완료**. `EnemyCombatant.LateUpdate`가 가시성 갱신 뒤 현재 표시 중인 경고선을 다시 설정한다. 총기 적의 선은 현재 `WeaponController.Muzzle`에서 현재 대상 위치까지, 근접 공격 준비선은 기존 몸체 높이에서 현재 대상 위치까지 이어진다. 따라서 총기 적이 경고선을 표시한 뒤 추격·후퇴·회전해도 시작점이 최초 월드 좌표에 남지 않는다. 총기 경고선의 기존 표시 조건과 사망·기절·상태 전환·시야 밖 숨김, 공격 판정 및 사격 방향은 변경하지 않았다.
- 영향을 받은 시스템: 적 총기·근접 공격 경고선, 적 이동·회전 시 시각 피드백, Stage2 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.48로 갱신해 표시 중인 총기·근접 경고선의 프레임별 원점·대상 갱신, 회귀 검사와 검증 제한을 기록했다.
- 테스트 결과: **확인 불가**. `PrototypePlayModeSmokeTest`에 적 이동 뒤 경고선 시작·끝점이 최신 총구·대상 좌표와 일치하는지 검사하는 회귀 검사를 추가했지만, Unity 6000.1.13f1 배치 컴파일과 스모크는 다른 Unity 인스턴스가 프로젝트를 열고 있어 `HandleProjectAlreadyOpenInAnotherInstance` 단계에서 실행되지 않았다. 대체 `dotnet build Assembly-CSharp-Editor.csproj`도 기존 누락 파일 `Assets/TutorialInfo/Scripts/Readme.cs` 참조로 실패했다.
- 남은 작업: **미실행**. 열린 Unity에서 컴파일이 끝난 뒤 `PrototypePlayModeSmokeTest.RunFromCommandLine`과 실제 Game View를 실행해, 레이저 표시 후 이동·회전하는 총기 적의 총구·대상 추적을 확인한다.

## 2026-08-10 - DEADLINE 냉정한 시간 정지 시각 효과

- 변경 유형: Built-in 풀스크린 셰이더·런타임 카메라 피드백·행동 노드·리플레이 비활성화·자동 스모크 추가, 기획 문서 갱신
- 변경 내용: **구현 완료**. `WorldTimeVisualFeedback`가 런타임에 게임플레이 카메라의 `DeadlineVisualFeedback`을 생성·연결해 기존 씬과 프리팹을 재생성하지 않는다. `DeadlineController.Activated`·`Released`를 구독하고 `Time.unscaledDeltaTime`으로 0.14초 진입 링·플래시, 채도 55%·가장자리 18% 청록 틴트·비네트·미세 노이즈 유지, 0.24초 정상 해제 복원파를 표시한다. 플레이어와 조준 중심부는 상대적으로 선명하게 유지한다. 플레이어 위의 행동 노드 2개는 준비 행동 수만큼 청록색으로 채우며 세 번째 행동 거절 때 주황색으로 점멸한다. 효과 중 기존 월드 시간 암전 오버레이만 억제하고 IMGUI HUD는 후처리 뒤에 유지한다. 사망·컴포넌트 비활성화 같은 비정상 중단은 해제파 없이 즉시 초기화하며, 리플레이의 기존 라이브 시뮬레이션 비활성화 흐름에서도 효과를 끄고 과거 `DEADLINE` 구간을 재현하지 않는다. Resources 기반 셰이더가 누락되거나 미지원이면 원본 화면을 출력하고 오류를 한 번만 남긴다. 공개 진단 `CurrentPhase`, `EffectBlend`, `DisplayedActionCount`, `IsShaderReady`와 `Configure(DeadlineController)`를 추가했다. 색수차·카메라 흔들림·FOV 변경·월드 오브젝트 외곽선·궤적 강조는 이번 범위에서 제외했다.
- 영향을 받은 시스템: 라이브 `DEADLINE` 진입·유지·해제 피드백, 게임플레이 카메라 Built-in 후처리, 행동 준비 UI, 월드 시간 암전 오버레이, 리플레이 라이브 시뮬레이션 비활성화
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/DeadlineVisualFeedback.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/Assets/_Project/Resources/Shaders/DeadlineScreenEffect.shader`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/DeadlineVisualFeedbackPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.47로 갱신해 구현 상태, 진입·유지·해제·비정상 중단·리플레이 정책, 자동 연결과 근거 경로, 자동 검증 결과 및 수동 확인 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 컴파일이 `Tundra build success`와 종료 코드 0으로 완료됐다. `DeadlineVisualFeedbackPlayModeSmokeTest.RunFromCommandLine`은 Stage1 진입·유지·행동 1/2개·세 번째 거절·정상 해제, 비스케일 전환과 `Time.timeScale == 1`, 비정상 비활성화 즉시 초기화, 해제 후 비활성 상태를 검증했다. Tutorial·Stage2·Stage5·Stage6에서 런타임 컴포넌트 연결과 셰이더 준비를 확인했고, `StageReplayController.DisableLiveSimulation` 뒤 비활성·초기화도 통과했다. 스모크 중 Stage5·Stage6의 기존 AudioListener 부재 경고와 일부 씬의 기존 Missing Script 경고가 출력됐지만 새 어설션은 모두 통과했다.
- 남은 작업: **확인 불가**. Stage1의 밝은 환경과 Stage5·Stage6의 어두운 네온 환경에서 실제 조준·적 식별·HUD 가독성, 링 강도·노이즈 체감과 여러 대상 해상도 품질은 사람 눈으로 수동 확인해야 한다.

## 2026-08-10 - Tutorial 좌측 전광판 월드 시간 스크롤

- 변경 유형: Synty 전광판 셰이더 시간 소스 교체, 런타임 머티리얼 오버라이드 추가, 기획 문서 갱신
- 변경 내용: **구현 완료**. Tutorial 카메라의 기존 `WorldTimeVisualFeedback`가 `Gate 01`~`06 Status Display` 안에서 `LED_Panel_06` 이름의 화면 머티리얼 슬롯을 런타임 복제하고 `Deltatime/World Time Emissive Scroll` 셰이더로 교체한다. 복제본은 원래 텍스처·색·`_Speed`를 유지하면서 `WorldTimeController.WorldElapsedTime`을 사용하므로, 기존 아래 방향 화면 이동은 보존되고 월드 시간 감속·하드 프리즈에 같은 비율로 반응한다. 전역 `Time.timeScale`, 원본 Synty 머티리얼, 씬 직렬화 머티리얼은 바꾸지 않는다.
- 영향을 받은 시스템: Tutorial 좌측 상태 전광판 화면 애니메이션, `WorldTimeController` 월드 시간 피드백, `DEADLINE` 하드 프리즈 시각 일관성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/Assets/_Project/Shaders/WorldTimeEmissiveScroll.shader`, `ProjectDeltatime/Assets/_Project/Shaders/WorldTimeEmissiveScroll.shader.meta`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.46으로 갱신해 여섯 상태 전광판의 `WorldElapsedTime` 기반 스크롤, 전역 시간 미변경 원칙과 수동 검증 범위를 기록했다.
- 테스트 결과: **미실행**. 사용자 요청에 따라 Unity 배치 컴파일·정적 검증·PlayMode 스모크를 실행하지 않았다.
- 남은 작업: **확인 불가**. 실제 Tutorial에서 전광판이 기존과 같은 아래 방향으로 움직이면서, 정지 상태·행동 상태·`DEADLINE` 하드 프리즈에 맞춰 감속·정지하는지 사용자가 확인해야 한다.

## 2026-08-10 - Tutorial 진행 게이트 철창 시각 개선

- 변경 유형: Tutorial 게이트 시각 계층·전용 적용 경로·정적 및 PlayMode 검증 보강, 기획 문서 갱신
- 변경 내용: **부분 구현**. `TutorialSceneBuilder`가 각 게이트에 폭 `0.24m`·높이 `2.45m`·깊이 `0.18m`, 중심 간격 `0.74m`인 세로 철창 17개와 기존 상·하단 레일을 생성하도록 바꿨고, 넓은 7개 셔터 판넬과 판넬별 상태 스트립은 생성하지 않는다. `Tools/Tutorial/Apply Bar Gate Visuals`와 명령줄 진입점은 Tutorial 씬의 여섯 게이트 시각 하위 오브젝트만 갱신하며 NavMesh와 나머지 환경은 재생성하지 않는다. 기존 충돌체·Layer 8·`TutorialGate` 상승/개방 로직은 유지한다. 단, 현재 열린 Unity 인스턴스가 프로젝트를 잠가 배치 적용이 시작 전에 중단되어 저장된 `Tutorial.unity`는 아직 기존 시각이다.
- 영향을 받은 시스템: Tutorial 여섯 진행 게이트의 외형, 환경 전용 갱신 경로, 게이트 정적·PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md` 1.6.46에 철창 규격·전용 갱신 경로·현재 저장 씬 적용 보류 상태를 기록했다.
- 테스트 결과: **미실행**. 사용자 요청에 따라 정적 검증과 Tutorial PlayMode 스모크를 실행하지 않았다. Unity 배치 적용은 다른 Unity 인스턴스가 프로젝트를 열고 있어 `HandleProjectAlreadyOpenInAnotherInstance` 단계에서 중단됐다.
- 남은 작업: **확인 불가**. 열린 Unity에서 `Tools/Tutorial/Apply Bar Gate Visuals`를 실행해 저장 씬에 반영한 뒤, 실제 철창 외형과 여섯 게이트의 상승·개방 동작을 사용자가 확인해야 한다.

## 2026-08-10 - 샷건 펠릿 수 4발 밸런스 조정

- 변경 유형: 샷건 발사체 수 하향 조정, 무기 데이터·재생성 검증·기획 문서 갱신
- 변경 내용: **구현 완료**. 샷건 한 발의 펠릿 수를 8개에서 4개로 낮췄다. 펠릿별 피해 1, 총 퍼짐 18도(반각 9도의 원형 콘), 펠릿별 반경 지터 최대 1도, 시드 307, 탄창 6, 발사 간격 0.75초, 탄속 16, 최대 사거리 14m와 플레이어 이동 반동 0m는 유지한다. `PrototypeSceneBuilder`의 생성값과 저장 데이터 검증 기대값도 4로 통일했다.
- 영향을 받은 시스템: 샷건 일반 발사·`DEADLINE` 준비 발사, 적 샷건 발사, 결정적 원형 콘 산포, Stage1/Stage2 재생성·정적 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.45로 갱신해 현재 샷건 동작 설명·무기 데이터 표를 4펠릿으로 바꾸고 변경 이력에 밸런스 조정을 기록했다. 과거 8펠릿 도입 이력은 보존했다.
- 테스트 결과: **정적 검증 통과**. `Shotgun.asset`의 `projectileCount: 4`, `PrototypeSceneBuilder`의 생성값 4·저장 데이터 검증 기대값 4와 기존 피해·산포·지터·시드·탄창·발사 간격·사거리를 대조했고 `git diff --check`도 통과했다. Unity 배치 컴파일은 프로젝트가 이미 열려 있어 실행할 수 없어 **확인 불가**다. 기존 사용자 변경이 있는 `Stage2.unity`를 보호하기 위해 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`과 Play Mode 스모크는 **미실행**이다.
- 남은 작업: **확인 불가**. 실제 플레이에서 4펠릿 샷건의 근거리 피해 체감과 난이도 밸런스는 수동 확인이 필요하다.

## 2026-08-10 - Tutorial 바닥 표지 렌더 복구 및 인게임 HUD 여백 보정

- 변경 유형: Tutorial TextMesh 렌더 머티리얼 복구, 한글 HUD 레이아웃·글자 크기 조정, 정적 참조 검증 보강
- 변경 내용: **구현 완료**. `Bay Label 01`~`07`의 한글 문구와 Noto Sans KR Bold `TextMesh.font`를 유지하면서 각 `MeshRenderer.sharedMaterial`을 같은 Bold 폰트 머티리얼로 연결하고 Renderer를 활성화했다. 생성 경로와 한글화 재적용 경로가 모두 폰트·머티리얼을 함께 설정하며 정적 검증도 이를 확인한다. `GameHud`의 좌상단 상태 패널은 `330×248`, 상태 글자는 14pt 및 `300×188` 영역으로 조정했다. 중앙 스테이지 결과와 `DEADLINE` 패널은 24pt 텍스트, 확대된 패널과 내부 여백으로 한글 3줄의 상하 잘림을 방지한다.
- 영향을 받은 시스템: Tutorial 바닥 TextMesh와 MeshRenderer, Noto Sans KR Bold 렌더링, GameHud 상태·결과·DEADLINE 피드백 레이아웃
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/KoreanUiLocalizationBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.44`로 갱신해 바닥 표지의 폰트 머티리얼 복구, HUD 고정 치수와 사용자 수동 확인 범위를 반영했다.
- 테스트 결과: **미실행**. 사용자 요청에 따라 자동 테스트와 배치 검증을 실행하지 않았다. 실제 Game View에서 표지 가시성과 좌상단·중앙 패널의 줄바꿈·잘림은 사용자가 직접 확인할 예정이다.
- 남은 작업: **확인 불가**. 사용자 수동 확인 전까지 실제 해상도별 HUD 가독성과 Tutorial 바닥 표지의 카메라 거리별 가시성은 확인 불가다.

## 2026-08-10 - UI 한글화 및 Noto Sans KR 적용

- 변경 유형: 공용 UI 폰트 에셋·TMP 기본 폰트 등록, 메인 메뉴·게임플레이 HUD·튜토리얼 HUD·무기 표시·바닥 표지 한글화, 에디터 적용/정적 검증 진입점 추가
- 변경 내용: **구현 완료**. Noto Sans KR Regular·Bold 참조와 Bold 기반 동적 SDF TMP 폰트 에셋을 공용 `KoreanUiFontSettings`로 등록했다. MainScene의 `PLAY`는 `게임 시작`으로 바꾸고 TMP 폰트를 연결했다. `GameHud`·`TutorialHud`의 사용자 표시 문구와 무기 표시는 한글화했으며, 네 무기 정의는 `권총`·`자동소총`·`샷건`·`근접 무기`로 통일했다. 튜토리얼 바닥 표지는 Bold 폰트로 `01 시간`, `02 대시`, `03 근접`, `04 권총`, `05 투척`, `06 DEADLINE`, `출구`를 표시한다. `DEADLINE`은 고유명 영문 표기를 유지한다. `KoreanUiLocalizationBuilder`는 같은 변경을 반복 적용해도 같은 결과가 되도록 적용 및 정적 참조 검증 메뉴/명령줄 진입점을 제공한다.
- 영향을 받은 시스템: TMP 기본 글꼴과 MainScene 버튼, 런타임 IMGUI 게임플레이·튜토리얼 HUD, `WeaponDefinition` 표시명, Tutorial TextMesh 바닥 표지, 로컬라이제이션 에디터 작업 경로
- 관련 파일: `ProjectDeltatime/Assets/_Project/Font/Noto_Sans_KR/NotoSansKR-Regular.otf`, `ProjectDeltatime/Assets/_Project/Font/Noto_Sans_KR/NotoSansKR-Bold.otf`, `ProjectDeltatime/Assets/_Project/Font/Noto_Sans_KR/NotoSansKR-Bold SDF.asset`, `ProjectDeltatime/Assets/_Project/Resources/KoreanUiFontSettings.asset`, `ProjectDeltatime/Assets/TextMesh Pro/Resources/TMP Settings.asset`, `ProjectDeltatime/Assets/_Project/Scripts/UI/KoreanUiFontSettings.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/KoreanUiLocalizationBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.43`으로 갱신해 공용 폰트, 한글 사용자 표기, `DEADLINE` 영문 유지, 적용/정적 검증 경로와 수동 확인 범위를 기록했다.
- 테스트 결과: **미실행**. 사용자 요청에 따라 자동 테스트와 배치 검증을 실행하지 않았다. 실제 Game View의 한글 글리프, 줄바꿈, 잘림은 사용자가 직접 확인할 예정이다.
- 남은 작업: **확인 불가**. 사용자의 Game View 수동 확인 전까지 해상도별 텍스트 레이아웃과 동적 TMP 글리프 생성 결과는 확인 불가다. 언어 전환과 범용 로컬라이제이션 시스템은 이번 범위 밖이며 **계획 필요**다.

## 2026-08-10 - 사망 리플레이·선택 스테이지 진행 및 엔딩 화면

- 변경 유형: 리플레이 결과 처리 확장, 입력·HUD·씬 흐름 추가, Build Settings 정리, 엔딩 UI 추가
- 변경 내용: **구현 완료**. `StageSceneFlow`가 `Stage1 → Stage2 → Stage5 → Stage6 → EndingScene` 목적지를 중앙 관리한다. `N`의 `NextStage` 입력을 추가해 클리어 리플레이 중에만 다음 목적지로 즉시 이동하게 했고, Stage6 뒤에는 `EndingScene`을 연다. `StageController`는 플레이어 사망 시에도 전투를 비활성화한 뒤 리플레이를 요청하며, 사망 리플레이에서는 `N`을 무시하고 `R`로 현재 스테이지를 재시작한다. 클리어·사망 리플레이 모두 `V` 시야 전환을 유지하고 HUD는 결과별 `N`·`R` 안내를 표시한다. MainScene의 기존 배경·로고를 복제한 `EndingScene`은 `STAGE CLEAR`, `Press N to return to Main Menu`를 표시하며 `N`으로 MainScene을 연다. Build Settings와 관련 씬 빌더는 `MainScene`, `Tutorial`, `Stage1`, `Stage2`, `Stage5`, `Stage6`, `EndingScene`만 유지하고 Stage3·Stage4 에셋은 삭제하지 않은 채 현재 빌드와 진행에서 제외한다.
- 영향을 받은 시스템: Player Input System 생성 래퍼, 스테이지 상태·전투 비활성화, 리플레이 시야 전환, HUD 결과 안내, 씬 전환·Build Settings, MainScene 기반 완료 화면
- 관련 파일: `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageSceneFlow.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/EndingSceneController.cs`, `ProjectDeltatime/Assets/_Project/Scenes/EndingScene.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/EndingSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/GameBuildSceneCatalog.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.42`로 갱신해 현재 고정 진행 경로, 사망/클리어 리플레이의 키 정책, EndingScene, Stage3·Stage4의 비삭제·비진행 상태, Build Settings 구성과 수동 검증 범위를 반영했다.
- 테스트 결과: **미실행**. 사용자 요청에 따라 자동·Play Mode 테스트와 수동 플레이를 실행하지 않았다. `EndingSceneBuilder.BuildFromCommandLine`으로 EndingScene 에셋을 생성했지만, 이후 입력 조건을 조정했으므로 해당 실행 결과를 현재 변경의 검증 근거로 사용하지 않는다.
- 남은 작업: **확인 불가**. 사용자가 사망 후 리플레이와 `R` 재시작, 클리어 리플레이의 `N` 진행 순서, Stage6 뒤 EndingScene 및 EndingScene의 `N` 복귀, 리플레이의 `V` 시야 전환과 결과별 HUD 안내를 수동으로 확인해야 한다.

## 2026-08-10 - Stage2 Synty 적 프리팹 적용

- 변경 유형: Stage2 적 시각 교체, 씬 생성·정적 검증 보강
- 변경 내용: **구현 완료**. `Enemy West`·`Enemy Center`·`Enemy East`의 Capsule 전투 프록시 아래에 Synty Polygon Nightclubs의 Bartender Male·Bouncer Male·Party Male 02 프리팹 시각을 각각 연결했다. 전투 프록시의 Collider·Rigidbody·AI·무기 참조는 유지하고 MeshRenderer만 `ShadowsOnly`로 바꿨으며, 프리팹 내부 Collider는 비활성화했다. `PrototypeSceneBuilder.ApplyStage2Characters`는 Stage2의 기존 플레이어 Business Male 시각을 유지하면서 과거 Stage1 이름의 중복 시각 자식을 정리하고 적만 멱등 교체한다.
- 영향을 받은 시스템: Stage2 적 시각, Humanoid Animator·캐릭터 애니메이션, 적 피격/가시성 피드백, Capsule 전투 프록시, Stage2 씬 생성 경로
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Prefabs/Characters/SM_Chr_Bartender_Male_01.prefab`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Prefabs/Characters/SM_Chr_Bouncer_Male_01.prefab`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Prefabs/Characters/SM_Chr_Party_Male_02.prefab`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.41`로 갱신해 Stage2 적의 Synty 프리팹 구성, 보존하는 Capsule 프록시 역할, 정적 검증 및 통합 스모크 결과를 기록했다.
- 테스트 결과: Unity 6000.1.13f1 배치 컴파일과 `PrototypeSceneBuilder.ApplyStage2Characters`가 통과했다. 세 프리팹 참조, `CharacterVisualController` 바인딩, Animator, `ShadowsOnly` 프록시, 시각 Collider 비활성화를 정적으로 검증했다. `PrototypePlayModeSmokeTest.RunFromCommandLine`은 이번 변경과 별개인 투척 무기 수치·6m 착지 및 리플레이 본 포즈 기록 검사에서 실패했다. 로그: `ProjectDeltatime/Stage2CharacterReplacement.log`, `ProjectDeltatime/Stage2SyntyEnemySmoke.log`.
- 남은 작업: **확인 불가**. 실제 Game View에서 세 적의 크기·회전·무기 손 그립 및 전투 중 애니메이션 체감, 그리고 기존 스모크 실패 원인의 별도 수정·재실행이 필요하다.

## 2026-08-10 - 무기 획득 효과음 완전 제거

- 변경 유형: 전투 SFX 재생 정책 수정, 런타임 사운드 라이브러리 정리
- 변경 내용: **구현 완료**. 플레이어가 빈손일 때의 최초 무기 획득음을 포함해 모든 무기 획득·교체·교환 효과음 재생을 제거했다. `WeaponPickup`은 장비 상태만 갱신하고 음향 호출을 하지 않으며, `SoundLibrary`·`SoundManager`·빌더에서도 `SFX_Weapon_Pickup.ogg`의 런타임 참조를 제거했다. 해당 파일은 후보 음원으로 보존하되 현재 게임에서 재생하지 않는다.
- 영향을 받은 시스템: 플레이어 무기 최초 획득·교체·교환 피드백, 적 예약 픽업, SoundLibrary 에셋 구성
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundLibrary.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.40`으로 갱신해 무기 획득·교체·교환 효과음이 모두 미사용임을 반영했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드의 `SoundLibraryBuilder.BuildAndValidateFromCommandLine`이 사운드 라이브러리를 재생성·검증했고, `SoundManagerPlayModeSmokeTest.RunFromCommandLine`은 MainScene의 BGM·전투 SFX·DEADLINE 단발 시간 왜곡·PLAY 클릭음과 Tutorial 전환을 통과했다. `DeltatimeSoundLibrary.asset`과 런타임 스크립트에서 `weaponPickupClip`·`PlayWeaponPickup` 참조가 없음을 정적으로 확인했다.
- 남은 작업: **확인 불가**. 추후 무기 획득 피드백이 필요해질 경우 현재 `SFX_Weapon_Pickup.ogg` 대신 게임의 짧은 조작 리듬에 맞는 후보를 별도 청감 비교 후 선택한다.

## 2026-08-10 - DEADLINE 시간 왜곡 단발화 및 무기 교체음 제거

- 변경 유형: DEADLINE SFX 재생 정책 수정, 무기 픽업·교체 피드백 조정
- 변경 내용: **구현 완료**. `SoundManager.PlayDeadlineEnter`의 `SFX_Deadline_Enter_TimeWarp` 재생 소스를 비반복으로 바꿔 DEADLINE 진입 때 한 번만 재생되게 했다. `WeaponPickup`은 플레이어가 빈손일 때만 획득음을 재생하고, 기존 무기를 보유한 상태에서 다른 무기로 교체·교환할 때는 효과음을 재생하지 않는다. 적의 예약 픽업은 기존처럼 플레이어 효과음을 내지 않는다.
- 영향을 받은 시스템: DEADLINE 진입 음향, 전역 SFX 수명, 플레이어 무기 획득·교체 피드백
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/README.md`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.39`로 갱신해 DEADLINE 시간 왜곡의 단발 정책과 무기 최초 획득·교체의 분리된 음향 규칙을 반영했다.
- 테스트 결과: Unity 컴파일과 `SoundManagerPlayModeSmokeTest.RunFromCommandLine`으로 DEADLINE 진입 뒤 시간 왜곡 `AudioSource.loop == false`를 검증했다. 무기 교체 무음 분기는 `WeaponPickup`의 기존 무기 정의 존재 여부 조건으로 정적 확인했다.
- 남은 작업: **확인 불가**. 실제 플레이에서 시간 왜곡음의 길이와 DEADLINE 유지 시간의 청감, 빈손 최초 획득음과 무기 교체 무음의 체감은 수동 확인이 필요하다.

## 2026-08-10 - 적 전투 식별 원 시야 동기화

- 변경 유형: 적 시야 표시 버그 수정, 리플레이 전체 시야 계약 및 PlayMode 회귀 검증 보강
- 변경 내용: **구현 완료**. `EnemyCombatant`가 적 루트의 직접 자식 `Combat Identity Ring` Renderer를 선택적으로 캐시한다. 제한 시야에서는 적 본체와 동일한 `VisionCone.ContainsWorldPoint(...) && !IsDead` 결과로 식별 원을 갱신해 시야 밖·장애물 뒤 적의 위치가 원형 발판으로 노출되지 않는다. Stage2처럼 링이 없는 적은 건너뛰며, `VisionCone.HasUnlimitedVision`이 참인 Tutorial·WeaponCalibration은 기존 표시 상태를 유지한다. 기존 `TryGetReplayVisibility`는 식별 원도 생존 적의 논리 가시성으로 인식해 전체 시야 리플레이에서 본체와 함께 표시한다. 씬·프리팹·빌더 직렬화는 변경하지 않았다.
- 영향을 받은 시스템: 제한 시야 적 렌더링, Combat Identity Ring, 장애물 차폐, 전체 시야 리플레이 Renderer 가시성, Stage5 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.38`로 갱신해 제한 시야에서 적 식별 원도 본체·캐릭터·장착 무기와 같은 가시성 판정을 따르고, Tutorial·WeaponCalibration은 제외하며, 전체 시야 리플레이에서 생존 적 식별 원을 표시하는 규칙을 반영했다.
- 테스트 결과: Unity 6000.1.13f1 배치 컴파일은 통과했다. `Stage5PlayModeSmokeTest`의 새 식별 원 가시성·전체 시야 리플레이 계약 검증은 뒤이은 기존 남쪽 컷어웨이 검사 전까지 통과했으나, 기존 `Stage5 south cutaway has an inactive exterior renderer` 실패로 전체 스모크는 실패했다. `Stage6PlayModeSmokeTest`는 NavMesh 완전 경로 5/5 및 런타임 검증을 포함해 통과했다. 로그: `ProjectDeltatime/EnemyIndicatorStage5Smoke.log`, `ProjectDeltatime/EnemyIndicatorStage6Smoke.log`.
- 남은 작업: **확인 불가**. Stage5 남쪽 컷어웨이의 기존 비활성 exterior Renderer 실패를 별도 수정한 뒤 전체 Stage5 스모크를 재실행해야 한다. 실제 플레이에서 시야 경계·장애물 뒤 식별 원 숨김과 전체 시야 리플레이 전환의 시각 품질은 수동 확인이 필요하다.

## 2026-08-10 - 리플레이 Animator 프록시·이벤트 트랙 및 메모리 예산

- 변경 유형: 리플레이 애니메이션 데이터 구조 전면 교체, 본 포즈 기록 제거, 정상속도 프록시 재생, 메모리 진단·상한 및 자동 검증 추가
- 변경 내용: **구현 완료**. `StageReplayController.VisualTrack`의 `BonePose`, `SkinnedMeshRenderer`별 source/proxy bone 배열, 독립 프록시 뼈 계층, 본 위치·회전·스케일 캡처/중복 비교/보간 적용과 `bones × 512` 리스트 선할당을 모두 삭제했다. `SkinnedMeshRenderer`는 더 이상 일반 Renderer 트랙으로 만들지 않으며 `ReplayAnimationTrack`이 `CharacterAnimationController.VisualRoot`를 actor당 한 번 복제해 동일 Avatar, RuntimeAnimatorController, 시각 모델과 공유 골격을 사용한다. 녹화 데이터는 중복을 제거한 actor Transform, 렌더러 활성·색, Float/Bool/Int 파라미터, 명시적 Set/Reset Trigger, Controller·활성 이벤트와 레이어 `fullPathHash`/`normalizedTime`/weight/최소 전이 정보 체크포인트로 구성한다. `CharacterAnimationController`의 `MoveX`, `MoveY`, `Roll`, `AttackA`, `AttackB`와 Controller 할당을 공용 파라미터/Trigger 어댑터로 정리했고, `Animator.parameters`는 등록·Controller 변경·희소 체크포인트에서만 읽는다. **구현 완료**. 프록시는 script-free 시각 루트만 복제하고 Animator 외 `Behaviour`, Collider, Rigidbody 충돌을 비활성화하며 `ReplayAnimatorProxyRegistry`와 `MeleeAttackImpactBehaviour` 가드가 공격 판정 콜백을 차단한다. 재생 Animator는 자동 진행을 0으로 유지하고 presentation-time 이벤트 사이의 `Time.unscaledDeltaTime` 기반 정상 표시 델타만 `Animator.Update`에 1배로 공급하므로 라이브 `WorldTimeController` 감속을 다시 적용하지 않는다. 초기/희소 소스 체크포인트와 최초 정상 재생에서 만든 presentation 체크포인트로 loop/seek 복원 경계를 유지한다. 기존 `ReplayRecordingClock.SourceElapsedTime`/`ReplayElapsedTime`, `BuildPresentationTimeline`, 카메라·일반 Transform·Line/VFX·조명·ViewCone, Deadline 카메라 복귀와 전역 `Time.timeScale == 1` 정책은 유지했다.
- 영향을 받은 시스템: 리플레이 녹화 payload·재생 시간축·Animator/SkinnedMeshRenderer·장비 Controller·공격 StateMachineBehaviour, 카메라/Renderer/Line/VFX/조명 회귀, 런타임 탐색·메모리 수명 정책, EditMode/PlayMode 테스트
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayAnimationTrack.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayAnimatorProxyRegistry.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayMemoryStatistics.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/MeleeAttackImpactBehaviour.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayTimeAxisEditModeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.37`로 갱신해 세 시간축과 source→presentation 매핑 유지, actor 단위 Animator 프록시/이벤트/체크포인트 구조, 프록시 안전 경계, 본 포즈 0건, 기본 소스 300초/추정 payload 64MiB 명시 중단, Renderer 탐색 정책, 호환성과 남은 프로파일 범위를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 컴파일은 오류 없이 완료됐다. `ReplayTimeAxisEditModeTest.RunFromCommandLine`은 정상 플레이 소스 6초와 강한 0.2배 플레이 소스 30초의 표시 길이가 모두 6초인지, 10초 hard freeze가 표시 길이를 늘리지 않는지, 이벤트 시간이 역행하지 않는지, 300초/64MiB 예산 판정이 각각 명시적 중단 사유를 반환하는지와 `Time.timeScale == 1`을 검증해 통과했다. `ReplayPlayModeSmokeTest.RunFromCommandLine`은 Stage2에서 실제 `MeleeWeapon → AutomaticRifle` 장비/Controller/손 모델 교체와 `AttackA`, `AttackB`, `Roll`, `MoveX`/`MoveY`를 기록하고, 원본 `CharacterAnimationController` 제거 뒤에도 별도 프록시 Animator가 비스케일 정상 시간으로 상태 전이하는지, 프록시가 registry에 등록됐고 게임플레이 Behaviour/Collider/Rigidbody가 비활성인지, 본 포즈 통계 0, actor/event/checkpoint/Transform payload 통계, 명시 등록 visual track 증가, 정상/Deadline 강한 감속 길이 압축, 이벤트 순서, HitFlash 2개, Deadline 카메라 복귀 비역행과 `Time.timeScale == 1`을 검증해 통과했다. 체크포인트는 Unity 6의 전이 duration 단위를 기록해 normalized 전이와 fixed-duration 전이를 각각 `CrossFade`/`CrossFadeInFixedTime`으로 복원한다. 최신 로그: `ProjectDeltatime/ReplayAnimatorFinalCompile5.log`, `ProjectDeltatime/ReplayAnimatorTimeAxisFinal5.log`, `ProjectDeltatime/ReplayAnimatorPlayModeFinal5.log`.
- 남은 작업: **부분 구현**. 실제 키보드/마우스 장시간 클리어의 사람 눈 기반 시각 품질, 다수 애니메이션 actor로 300초/64MiB까지 채우는 목표 하드웨어 프로파일, 상한 도달 HUD 알림은 **확인 불가/계획 필요**다. 현재 에셋에 없는 사격·피격·사망·투척/획득 전용 모션은 **미구현**이며, script가 포함된 시각 루트는 부작용 방지를 위해 등록을 거부하므로 향후 필요한 경우 명시적인 replay visual prefab/허용 목록 설계가 **계획 필요**다. 외부 저장 리플레이 포맷은 없고 기존 공개 진단 `RecordedAnimatedPoseCount`는 API 호환을 위해 남기되 항상 0을 반환한다.

## 2026-08-10 - Deadline 리플레이 카메라 떨림 제거

- 변경 유형: 리플레이 카메라 보간 버그 수정, 자동 검증 보강
- 변경 내용: **구현 완료**. 정규속도 리플레이가 20Hz 인접 샘플별 프레젠테이션 세그먼트를 생성한 뒤, Deadline 해제 후 카메라 복귀 배율을 각 세그먼트의 `PresentationStart`에서 다시 계산해 경계마다 카메라가 Deadline 진입 앵커 방향으로 되돌아가던 문제를 수정했다. Deadline 해제 시점의 단일 `CameraRecoveryStart`를 모든 `DeadlineAftermath` 세그먼트에 저장하고, 0.2초 복귀 배율을 이 공통 시각에서 계산한다. `CurrentCameraRecoveryBlend` 진단값은 Deadline에서 0, 후속 복귀에서 단조 증가, 일반 구간에서 1로 갱신된다. 시간축, 카메라 앵커, 기존 0.2초 복귀 길이와 일반 플레이 밸런스는 변경하지 않았다.
- 영향을 받은 시스템: `StageReplayController` 프레젠테이션 세그먼트, Deadline 카메라 고정·해제 후 복귀, 리플레이 PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.36`으로 갱신하고 전체 후속 구간이 공유하는 카메라 복귀 시작 시각, 진행도 진단값과 자동 검증 근거를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 최종 배치 컴파일이 종료 코드 0으로 완료됐다. `ReplayPlayModeSmokeTest.RunFromCommandLine`에서 정상/강한 감속 시간축, 공격·이동·피격 VFX·스킨 뼈 포즈·이벤트 순서 검증과 함께, 여러 20Hz 후속 세그먼트에서 `CurrentCameraRecoveryBlend`가 역행하지 않고 실제 증가하는지 확인해 통과했다. 로그: `ProjectDeltatime/ReplayCameraStabilityCompile.log`, `ProjectDeltatime/ReplayCameraStabilitySmoke2.log`.
- 남은 작업: 실제 키보드/마우스 클리어 후 사람 눈으로 보는 전체 화면 안정성 평가는 **확인 불가**다. 리플레이 종료/스킵 및 장시간 녹화 성능은 기존 **계획 필요** 항목으로 유지한다.

## 2026-08-10 - 리플레이 정상속도 시간축 및 스킨 애니메이션 재현

- 변경 유형: 리플레이 시간축 버그 수정, 애니메이션 스냅샷 확장, 짧은 VFX 등록, HUD·자동 검증 갱신
- 변경 내용: **구현 완료**. `ReplayRecordingClock`을 추가해 녹화 소스 순서용 비스케일 실시간과 정상속도 표시용 실제 `WorldDeltaTime` 누적 시간을 분리했다. `StageReplayController`는 전체 일반 구간의 평균 배율로 한 번에 선형 환산하던 구조와 Deadline 0.5배·최소/최대 길이 강제를 중단하고, 20Hz 인접 샘플마다 정규 표시 시간↔소스 시간을 매핑한다. 재생 진행은 계속 `Time.unscaledDeltaTime`을 사용하므로 비활성화 전의 `WorldTimeController.CurrentTimeScale`, 하드 프리즈, 전역 스케일의 영향을 받지 않는다. 기존 Deadline 직렬화 필드와 공개 단계/진단 API는 씬·에디터 호환을 위해 보존하지만 길이 값은 이제 정규화된 월드 진행량을 뜻한다. `SkinnedMeshRenderer` 프록시는 트랙 생성 순간 한 번 `BakeMesh`한 정적 메시에서 원본 공유 스킨 메시+독립 뼈 계층으로 교체하고, 각 샘플의 뼈 위치·회전·스케일을 값 형식 버퍼에 기록해 재생 프레임마다 보간한다. 따라서 Animator의 이동·Roll·공격 트리거와 전이가 만든 최종 포즈가 오브젝트 생성/비활성/제거 뒤에도 녹화된 가시 구간에서 재현된다. 기록 중 스킨 Animator는 `AlwaysAnimate`로 설정하고 종료 시 원래 culling 모드로 복원한다. `VisualSample`을 값 형식으로 바꾸고 동일 재질/라인 배열을 재사용해 애니메이션 샘플의 프레임별 관리 힙 할당을 피했다. 수명 0.12초의 `HitFlash`는 `ActiveRecorder`에 생성 시 즉시 등록하고, 런타임 투사체·투척·공중 무기는 재사용 목록 기반 `RegisterRendererHierarchy`로 자식 렌더러를 한 번에 등록한다. 이 경로는 20Hz/Stage6 fallback 탐색 간격 사이 누락을 막으며 프레임별 `Find`나 호출별 렌더러 배열 할당을 추가하지 않는다. HUD는 Deadline을 포함한 모든 리플레이 구간을 `NORMALIZED 1.00x`로 표시한다.
- 영향을 받은 시스템: 리플레이 녹화 데이터와 표시 시간축, `WorldTimeController` 연동, Deadline 카메라 단계, 카메라·이동·투사체·투척/픽업·라인/VFX·조명 프록시의 공통 소스 타임스탬프, 캐릭터 Animator/SkinnedMeshRenderer, 전체 시야, HUD, 배치 테스트
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayRecordingClock.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Utilities/HitFlash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayTimeAxisEditModeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/ReplayPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.35`로 갱신하고 세 시간축 정의, 정상속도 구간별 매핑, Deadline 기존 배속 정책 대체, 스킨 뼈 포즈·Animator culling·짧은 VFX 등록 정책, 실제 수치, 호환성, 성능 부채, 자동 검증 근거와 의사결정을 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 최신 배치 컴파일이 종료 코드 0으로 완료됐다. `ReplayTimeAxisEditModeTest.RunFromCommandLine`은 월드 진행 6초를 정상 플레이(소스 6초)와 강한 0.2배 플레이(소스 30초)로 기록해 두 표시 길이가 모두 6초인지, 이동→감속 공격→하드 프리즈→피격→이동 이벤트의 표시 시간이 역행하지 않는지, `Time.timeScale == 1`인지 검증해 통과했다. `ReplayPlayModeSmokeTest.RunFromCommandLine`은 Stage2의 정상 구간과 1.1초 Deadline 강한 감속 구간 양쪽에서 공격 `AttackA` 트리거, 플레이어 이동, `HitFlash`를 실제 녹화하고 정규화된 길이 단축, 뼈 포즈 변화/활성 스킨 프록시, 두 피격 VFX 트랙, 소스 이벤트 단조 증가, 전역 스케일 불변을 검증해 통과했다. 최신 로그: `ProjectDeltatime/ReplayFinalCompile3.log`, `ProjectDeltatime/ReplayTimeAxisEditMode2.log`, `ProjectDeltatime/ReplayPlayModeSmoke3.log`. 최신 `PrototypePlayModeSmokeTest` 회귀 실행은 리플레이 관련 어설션을 모두 통과했으나 현재 투척 거리 4m와 테스트의 과거 6m 기대값이 충돌한 기존 2건 때문에 전체 종료 코드는 실패했다. 로그: `ProjectDeltatime/ReplayPrototypeRegression2.log`.
- 남은 작업: **부분 구현**. 현재 소스 에셋에 권총 사격, 피격, 사망, 투척/획득 전용 캐릭터 애니메이션이 없어 리플레이는 존재하는 이동·Roll·상체 공격과 피격 색/`HitFlash`만 재현한다. 블렌드 셰이프 전용 애니메이션은 별도 샘플이 없으며 현재 캐릭터에서 사용 여부를 **확인 불가**로 남긴다. 무제한 녹화 길이와 뼈 포즈 메모리, Stage1~Stage5의 20Hz 전체 렌더러 검색, Stage6 0.25초 fallback, 반복 리플레이 종료/복구 경로는 기존 **계획 필요** 성능·수명 과제다. 실제 키보드/마우스 클리어 후 전체 씬의 시각 품질은 **확인 불가**다.

## 2026-08-10 - 전역 SoundManager 및 자동 BGM·전투·DEADLINE 효과음 연결

- 변경 유형: 런타임 오디오 시스템 추가, BGM 자동 라우팅, 전투·DEADLINE 이벤트 연결, 효과음 에셋 추가
- 변경 내용: **구현 완료**. `RuntimeInitializeOnLoadMethod`로 생성되고 씬 전환 뒤에도 유지되는 `SoundManager`와 `Resources/DeltatimeSoundLibrary`를 추가했다. `MainScene`·`Tutorial`·`Stage*`·엔딩 계열 씬에 BGM 4종을 자동 매핑하고 0.25초 크로스페이드·반복 정책을 적용했다. 권총·자동소총·샷건은 성공한 발사 1회마다 무기별 변형 한 개를 3D 재생하며, 주먹·야구방망이는 실제 피해 적중 시에만 전용 3D 효과음을 낸다. 무기 투척과 플레이어 획득·교환에도 전용 효과음을 연결했다. `DeadlineController.Activated`·`Released`에 진입 충격·반복 시간 왜곡·해제 변형을 연결하고 활성 중 BGM을 약 -8 dB로 덕킹한다. 오디오 전환은 비스케일 시간을 사용하며 전역 `Time.timeScale`은 변경하지 않는다.
- 영향을 받은 시스템: 메인 메뉴·Tutorial·Stage1~Stage6·엔딩 BGM, 플레이어·적 총기 발사, 근접 피해 판정, 무기 투척·획득, DEADLINE 하드 프리즈 피드백, 씬 전환과 오디오 수명
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundLibrary.cs`, `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Audio/BGM`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Combat`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundManagerPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.34`로 갱신해 BGM 자동 선택·크로스페이드, 전투 3D SFX, DEADLINE 2D 레이어·덕킹, 비스케일 시간 정책과 구현 근거를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `SoundLibraryBuilder.BuildAndValidateFromCommandLine`이 C# 컴파일과 모든 BGM·무기 정의·발사음·타격음·투척·획득·DEADLINE 참조 검증을 통과했다. `SoundManagerPlayModeSmokeTest.RunFromCommandLine`은 `MainScene` BGM 선택, 라이브러리 로드, 권총·주먹·야구방망이·투척·획득 재생 API와 DEADLINE 진입·해제 상태 전환을 실제 PlayMode에서 통과했다. 기존 `TutorialPlayModeSmokeTest`는 현재 사용자 편집 `Tutorial.unity`의 Synty 프리팹 수가 검증 기대 262개가 아닌 216개여서 PlayMode 진입 전에 중단됐으며, 해당 씬은 보존했다. 로그: `ProjectDeltatime/SoundLibraryBuild.log`, `ProjectDeltatime/SoundManagerSmoke.log`, `ProjectDeltatime/TutorialAudioSmoke.log`.
- 남은 작업: **확인 불가**. 실제 스피커·헤드폰에서 BGM 대비 총성·타격음·DEADLINE 레이어의 청감과 장시간 Stage 전환을 수동 확인해야 한다. 사용자 조절식 마스터·BGM·SFX 볼륨과 별도 `AudioMixer` 에셋은 **계획 필요**다. Tutorial 전체 회귀 스모크는 사용자 씬의 프리팹 수와 검증 기준이 다시 일치한 뒤 재실행해야 한다.

## 2026-08-10 - MainScene PLAY 버튼 클릭음 연결

- 변경 유형: UI 클릭 효과음 연결, SoundLibrary 참조 확장
- 변경 내용: **구현 완료**. 기존 MainScene PLAY 버튼의 `MainMenuController.Play` 실행 직전에 `SoundManager.PlayUiClick`을 호출하도록 연결했다. `Assets/_Project/Audio/SFX/Click/click.ogg`를 `DeltatimeSoundLibrary`의 UI 클릭 클립으로 등록했으며, Build Settings 대상 씬이 유효할 때 클릭음이 재생된 뒤 Tutorial 씬 전환이 진행된다. MainScene의 투명 버튼·hover/press 시각 피드백과 기존 단일 persistent 리스너는 유지했다.
- 영향을 받은 시스템: MainScene UI, 씬 전환, 전역 UI SFX, SoundLibrary 에셋
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundLibrary.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SoundLibraryBuilder.cs`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Click/click.ogg`, `ProjectDeltatime/Assets/_Project/Resources/DeltatimeSoundLibrary.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`의 현재 구현 상태와 분석 기준에 MainScene PLAY 클릭음 매핑·재생 상태를 추가했다.
- 테스트 결과: **구현 완료**. Unity `SoundLibraryBuilder.BuildAndValidateFromCommandLine`이 클릭음 참조를 포함한 라이브러리 저장·검증을 통과했고, `SoundManagerPlayModeSmokeTest.RunFromCommandLine`이 MainScene BGM·SoundManager 로드, `MainMenuController.Play`의 UI 클릭음 호출, Tutorial 씬 전환까지 통과했다. 씬 전환 뒤 Tutorial에 AudioListener가 없어 Unity 경고가 한 번 기록됐지만 클릭음 연결 검증은 통과했다. 로그: `ProjectDeltatime/SoundLibraryBuild.log`, `ProjectDeltatime/SoundManagerSmoke.log`.
- 남은 작업: **확인 불가**. 실제 스피커·헤드폰에서 클릭음의 볼륨과 MainScene BGM 대비 청감은 수동 확인이 필요하다. `click.ogg`의 원본 라이선스 메타데이터가 프로젝트 문서에 없으므로 배포 전 출처를 확인해야 한다.

## 2026-08-10 - BGM 4종 Unity 배치

- 변경 유형: BGM 에셋 정리·배치, Unity 스트리밍 임포트 설정, 씬별 사용 정책 문서화
- 변경 내용: **구현 완료**. `sector_MainScene.mp3`, `pulse_tutorial.mp3`, `ruskerdax_-_savage_ambush_Stage.mp3`, `title_EndingScene.mp3`를 각각 `BGM_MainMenu.mp3`, `BGM_Tutorial.mp3`, `BGM_Stage_Action.mp3`, `BGM_Ending.mp3`로 이름을 정리해 `Assets/_Project/Audio/BGM`에 배치했다. 네 MP3가 Unity `AudioImporter`로 인식되는 것을 확인하고 `Load Type: Streaming`, `3D Sound: Off`, 스테레오 유지, 프리로드 해제 설정을 적용했다. 메뉴·Tutorial·Stage는 반복, 엔딩은 비반복으로 사용하는 기준을 README에 기록했다. 씬별 `AudioSource`·BGM `AudioMixer` 연결은 **미구현**이다.
- 영향을 받은 시스템: 메인 메뉴·Tutorial·Stage1~Stage6·엔딩 음악 자산, 장시간 BGM 메모리 사용, 씬 전환 페이드·반복 정책 준비
- 관련 파일: `ProjectDeltatime/Assets/_Project/Audio/BGM/BGM_MainMenu.mp3`, `ProjectDeltatime/Assets/_Project/Audio/BGM/BGM_Tutorial.mp3`, `ProjectDeltatime/Assets/_Project/Audio/BGM/BGM_Stage_Action.mp3`, `ProjectDeltatime/Assets/_Project/Audio/BGM/BGM_Ending.mp3`, `ProjectDeltatime/Assets/_Project/Audio/BGM/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.33`으로 갱신해 BGM 4개 매핑, 길이, 반복·스트리밍 정책, 런타임 연결 상태와 근거 파일을 반영했다.
- 테스트 결과: **정적 검증 통과**. 원본 MP3 4개의 프레임을 분석해 약 56.9초·71.1초·121.9초·425.6초 길이를 확인했고, Unity 배치 임포트 후 네 `.meta`가 모두 `AudioImporter`를 포함하는 것을 확인했다. BGM의 실제 씬 재생·반복·페이드 청감은 **미실행**이다.
- 남은 작업: **부분 구현**. MainScene·Tutorial·Stage1~Stage6·Ending의 `AudioSource` 연결, 씬 전환 페이드, BGM AudioMixer 그룹·DEADLINE 덕킹, 실제 재생·반복 체감 검증이 남아 있다.

## 2026-08-10 - DEADLINE 진입·시간 왜곡·해제 효과음 배치

- 변경 유형: DEADLINE 전용 음향 에셋 추가, 이벤트별 재생 기준·라이선스 문서화
- 변경 내용: **구현 완료**. Pixabay Content License로 표기된 세 MP3를 DEADLINE 전용 Unity 에셋 폴더에 역할별 이름으로 복사했다. `SFX_Deadline_Enter_Impact`는 Q 입력에 따른 활성화 즉시의 저음 충격, `SFX_Deadline_Enter_TimeWarp`는 그 충격과 겹치는 시간 왜곡, `SFX_Deadline_Release`는 하드 프리즈 해제를 맡는다. README에 전역 2D 재생, 시간 왜곡 -8 dB 상대 볼륨, BGM -8 dB 덕킹, 현재 코드 연결 지점과 원본 페이지를 기록했다. 런타임 `AudioSource`/`AudioMixer` 연결은 **미구현**이다.
- 영향을 받은 시스템: DEADLINE 활성·하드 프리즈 해제 피드백, 전역 SFX 믹싱, 향후 BGM 덕킹, 에셋 라이선스 추적
- 관련 파일: `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Enter_Impact.mp3`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Enter_TimeWarp.mp3`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Release.mp3`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Deadline/README.md`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.32`로 갱신해 세 DEADLINE 효과음의 역할·배치·재생 기준, 런타임 연결 상태 및 근거 파일을 반영했다.
- 테스트 결과: **미실행**. 파일 복사 전 원본 존재를 확인했으나, Unity Editor 임포트·AudioSource 재생·Q 진입 및 이동 해제의 실제 Play Mode 청감은 아직 실행하지 않았다.
- 남은 작업: **부분 구현**. `DeadlineController` 활성화 상승 에지 또는 신규 `Activated` 이벤트에 진입 충격·시간 왜곡을, 기존 `Released` 이벤트에 해제음을 연결한다. AudioMixer SFX/BGM 그룹·덕킹과 성공 전용 보상음, 실제 DEADLINE 플레이 중 볼륨·타이밍 체감 검증도 남아 있다.

## 2026-08-10 - Tutorial 외벽 복원 및 훈련 시설 정렬

- 변경 유형: Tutorial 환경 아트 재구성, 측면 외벽·조명 복원, NavMesh 재베이크·검증 갱신
- 변경 내용: **구현 완료**. 동·서 `Tutorial Wall`을 다시 렌더링하고, `Synty Tutorial Set`에 양측 벽 패널 40개, 상부 트림 40개, 천장 에지 20개, 균일 간격의 벽 조명 20개를 복원했다. 벽 패널은 규칙적으로 반복하고 중앙 데크·게이트·바닥 진행 표지를 향한 시야를 비워 훈련 시설의 정돈된 구성으로 맞췄다. 벽 배관·환기구 같은 산발적 벽 장식은 재도입하지 않았다. 기존 외벽 Collider는 계속 Layer 8 `VisionObstacle`이며, `TutorialTargetDummy`의 빈손 Synty 시각·숨긴 원통 판정 프록시, `TutorialDirector`, 게이트·무기 지급기·HUD·월드 시간·DEADLINE 직렬화 참조는 유지했다. 환경 프리팹 검증 수는 142개에서 262개로 갱신했다.
- 영향을 받은 시스템: Tutorial 환경 렌더링·카메라 가시성, Physics Collider, Layer 8 `VisionObstacle`, NavMesh, Tutorial 정적·PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.31`로 갱신해 양측 외벽·트림·조명의 규칙적 배치, 262개 환경 프리팹, VisionObstacle·NavMesh 유지 및 검증 상태를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `TutorialSceneBuilder.ApplyEnvironmentRedesignFromCommandLine`이 저장 Tutorial 씬에 외벽을 적용하고 `TutorialNavigation.asset`을 재베이크했다. `TutorialSceneBuilder.CapturePreviewFromCommandLine`과 `ValidateFromCommandLine`은 양측 벽 모듈·조명, 6개 게이트, 빈손 Synty 표적 2개, Layer 8 정책 및 중앙 NavMesh 완전 경로를 검증하고 통과했다. 첫 `TutorialPlayModeSmokeTest.RunFromCommandLine`은 월드 시간 활동 샘플이 `0.02x`에 머문 단발 실패가 있었으나, 동일 저장 씬의 즉시 재실행은 WorldDeltaTime, 타입별 표적 판정, 무기 지급, 게이트, 투척·공중 회수, Vision, 애니메이션, `DEADLINE` 체크포인트 복구까지 통과했다. 로그: `ProjectDeltatime/TutorialFacilityWallRestore.log`, `ProjectDeltatime/TutorialFacilityWallPreview.log`, `ProjectDeltatime/TutorialFacilityWallFinalValidate.log`, `ProjectDeltatime/TutorialFacilityWallSmoke.log`, `ProjectDeltatime/TutorialFacilityWallSmokeRetry.log`.
- 남은 작업: **확인 불가**. 실제 Game View에서 사람이 양측 벽의 조명 대비, 게이트 상승 시 메시 관통, 표적 피격 색 피드백 및 처음부터 Stage1 전환까지의 체감을 수동 확인할 필요가 있다. 첫 스모크의 월드 시간 활동 샘플 단발 실패가 재현되는지도 연속 실행으로 확인이 필요하다. 입력 액션·HUD·전투 밸런스·튜토리얼 진행 순서는 변경하지 않았다.

## 2026-08-10 - 무기 발사음 에셋 추출 및 Unity 배치

- 변경 유형: CC0 무기 발사음 선별·편집, Unity 오디오 에셋 추가, 출처·임포트 기준 문서화
- 변경 내용: **구현 완료**. 로컬 `Prepared SFX Library`의 장시간 녹음에서 첫 발의 피크를 기준으로 권총 2종, 자동소총 단발 2종, 샷건 3종을 분리했다. 모든 출력은 48 kHz/24-bit 스테레오 PCM WAV이며, 원본보다 약 -2.5 dB의 공통 게인과 종료 20 ms 페이드아웃을 적용했다. 권총은 0.520초, 자동소총은 0.350초, 샷건은 0.930초로 잘라 현재 `WeaponDefinition`의 권총 0.24초·자동소총 0.12초·샷건 0.75초 사용 간격에 맞는 단발 큐로 준비했다. 원본 출처·라이선스·권장 Unity 임포트 설정은 같은 폴더의 `README.md`에 기록했다. 런타임 `AudioSource`/`AudioMixer` 및 발사 이벤트 연결은 **미구현**이다.
- 영향을 받은 시스템: 권총·자동소총·샷건의 향후 발사 피드백, Unity 오디오 임포트, 에셋 라이선스 추적
- 관련 파일: `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Pistol/SFX_Pistol_Fire_01.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Pistol/SFX_Pistol_Fire_02.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Rifle/SFX_Rifle_Fire_01.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Rifle/SFX_Rifle_Fire_02.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_01.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_02.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_03.wav`, `ProjectDeltatime/Assets/_Project/Audio/SFX/Weapons/README.md`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.30`으로 갱신해 7개 발사음 에셋의 준비 상태, 포맷·길이, 런타임 연결 미구현 상태와 근거 파일을 반영했다.
- 테스트 결과: **정적 검증 통과**. 7개 출력 WAV가 모두 48 kHz·24-bit·스테레오 PCM이며, 권총 2종 0.520초·자동소총 2종 0.350초·샷건 3종 0.930초인 것을 헤더와 프레임 수로 확인했다. Unity Editor 임포트 및 실제 재생 테스트는 **미실행**이다.
- 남은 작업: **부분 구현**. `WeaponController`와 적 사격 경로에 무기별 무작위 발사음 재생, AudioMixer 그룹·볼륨·거리 감쇠, 피격·근접·투척·획득·UI·월드 시간/DEADLINE·BGM 사운드 에셋 선별 및 실제 Play Mode 체감 검증이 남아 있다.

## 2026-08-10 - Tutorial 측면 벽 제거 및 비무장 표적 시각 교체

- 변경 유형: Tutorial 환경 아트 축소, `TutorialTargetDummy` 시각 프레젠테이션 교체, NavMesh 재베이크·검증 갱신
- 변경 내용: **구현 완료**. `Synty Tutorial Set`에서 동·서쪽 벽, 상부 트림·루프 에지, 벽 부착 조명, 벽 설비를 제거해 중앙 훈련 데크 양옆을 열었다. `East/West Tutorial Wall` 오브젝트는 씬에서 제거·명칭 교체했고, 동일 위치에는 렌더링하지 않는 `Tutorial East/West Boundary Collider`만 남겨 Physics·Layer 8 `VisionObstacle`·NavMesh 경계를 보존했다. `Melee Training Target`, `Pistol Training Target`의 기존 원통 GameObject·Collider·`TutorialTargetDummy` 직렬화 참조와 공격 종류는 유지하되 Renderer를 숨기고, 빈손 `SM_Gen_Chr_Business_Male_01` Synty 프리팹을 자식 시각으로 연결했다. 피격 허용/거절 색 피드백도 해당 캐릭터 Renderer로 이전했다. 환경 프리팹 검증 수는 측면 아트 제거에 맞춰 264개에서 142개로 갱신했다.
- 영향을 받은 시스템: Tutorial 환경 렌더링·카메라 가시성, `TutorialTargetDummy` 피격 시각 피드백, Physics Collider, Layer 8 `VisionObstacle`, NavMesh, Tutorial 정적·PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Business_Male_01.prefab`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.29`로 갱신해 개방형 중앙 데크, 보이지 않는 양측 경계 Collider, 빈손 근접·Pistol 표적 시각, 142개 환경 프리팹 및 검증 상태를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `TutorialSceneBuilder.ApplyEnvironmentRedesignFromCommandLine`이 저장 Tutorial 씬에 변경을 적용하고 `TutorialNavigation.asset`을 재베이크했다. `TutorialSceneBuilder.ValidateFromCommandLine`은 142개 환경 프리팹, 제거된 측면 벽 아트, 보이지 않는 양측 경계 Collider, 빈손 Synty 표적 2개, 6개 게이트, Layer 8 정책 및 중앙 NavMesh 완전 경로를 검증하고 통과했다. 남/북 카메라 프리뷰를 캡처해 양옆이 열린 데크와 게이트·동선 표지를 확인했다. `TutorialPlayModeSmokeTest.RunFromCommandLine`은 월드 시간, 타입별 근접/총기 표적 판정, 무기 지급, 게이트, 투척·공중 회수, Vision, 애니메이션, `DEADLINE` 체크포인트 복구까지 통과했다. 로그: `ProjectDeltatime/TutorialOpenDeckAndTargets.log`, `ProjectDeltatime/TutorialOpenDeckPreview.log`, `ProjectDeltatime/TutorialOpenDeckFinalValidate.log`, `ProjectDeltatime/TutorialOpenDeckSmoke.log`.
- 남은 작업: **확인 불가**. 실제 Game View에서 사람이 근접·Pistol 표적에 타격해 캐릭터 메시의 허용/거절 색 피드백과 개방된 측면의 최종 조명 체감을 수동 확인할 필요가 있다. 입력 액션, HUD, 전투 밸런스, 튜토리얼 진행 순서는 변경하지 않았다.

## 2026-08-10 - Tutorial 폐쇄형 지하 훈련 시설 환경 리디자인

- 변경 유형: Tutorial 환경 아트 전면 개선, 공간 구성·길찾기 표식 정리, NavMesh 재베이크, 재생성·검증 경로 갱신
- 변경 내용: **구현 완료**. 기존 `TutorialDirector`, 6개 `TutorialGate`, 2개 `TutorialTargetDummy`, 3개 `TutorialWeaponDispenser`, 트리거·적·플레이어·카메라·HUD·WorldTime·DEADLINE 직렬화 참조와 각 기능 오브젝트의 진행 위치는 유지했다. `Synty Tutorial Set`은 하나의 폐쇄형 나이트클럽·지하 훈련 시설로 다시 구성해 `PolygonNightclubs` 프리팹 264개를 배치했다. 어두운 벽 배킹과 연속 상부 트림, 게이트 기둥·빔, 벽 상태 화면, 튜브등·바닥등, 설비 배관·환기구, DJ 제어 부스, 장비 캐비닛, 벤치·상자·테이블·스피커·출구 설비를 양측 서비스 베이에 정렬했다. 중앙 이동 폭은 비워 둔 채 어두운 훈련 데크, 청록 경계·점선·진행 화살표, `01 TIME`~`06 DEADLINE`·`EXIT` 바닥 표지와 목표 패드를 연속 배치했다. 기존 불투명 단일 큐브 게이트는 같은 `TutorialGate`·BoxCollider를 사용하는 투시형 분절 셔터 자식 시각으로 바꾸고, 과도하게 큰 월드 타임 십자 시계는 같은 `TutorialTimeProbe`를 유지한 소형 계기판으로 정리했다. 외곽 소품 Collider는 Layer 8 `VisionObstacle` 정책을 유지하며 전용 `TutorialNavigation.asset`을 다시 베이크했다. 전체 Tutorial 빌더를 실행하지 않고 저장 씬의 환경 계층만 교체하는 `Apply Environment Redesign` 경로를 추가했으며, 향후 `Build Tutorial` 재생성도 동일한 환경 구성을 만든다. `MainScene`이 첫 활성 씬인 현재 Build Settings 순서도 검증 기대값에 반영했다.
- 영향을 받은 시스템: Tutorial 환경 렌더링, 공간 동선·목표 가독성, 게이트 시각, 월드 타임 시각 오브젝트, 조명, Physics Collider, Layer 8 `VisionObstacle`, NavMesh, 카메라 프리뷰, Tutorial 정적·PlayMode 검증, Build Settings 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Prefabs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.28`로 갱신해 264개 Synty 환경 모듈, 단일 훈련 시설 양식, 중앙 진행 표지, 분절 게이트, 월드 타임 계기판, VisionObstacle/NavMesh 정책, 최신 정적·PlayMode 검증과 `MainScene → Tutorial` 빌드 순서를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 환경 전용 갱신이 스크립트 컴파일, Tutorial 씬 저장, `TutorialNavigation.asset` 재베이크와 정적 검증을 통과했다. 정적 검증은 6개 게이트·3개 지급기·2개 타깃·3개 트리거·플레이어/WorldTime/DEADLINE/카메라·애니메이션 캐릭터 6명·Synty 랜드마크·분절 셔터 6개·Layer 8 장애물·7개 중앙 체크포인트의 NavMesh 완전 경로·현재 Build Settings 순서를 확인했다. 남쪽 시작/학습 구간과 북쪽 DEADLINE/출구 프리뷰를 캡처해 표지 방향, 시계 크기, 주요 동선과 외곽 가구 배치를 직접 확인했다. 최신 `TutorialPlayModeSmokeTest.RunFromCommandLine`은 WorldDeltaTime, 6개 게이트 위치/개방, 이동·조준·대시, 타깃·근접/Pistol 지급, 투척 기절·무장 해제·드롭·공중 회수, 무제한 Vision, 애니메이션 프로필, Q `DEADLINE` 2원인 실행·이동 해제와 체크포인트 복구를 통과했다. 로그: `ProjectDeltatime/TutorialEnvironmentRedesign3.log`, `ProjectDeltatime/TutorialEnvironmentPreview3.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **확인 불가**. 실제 대상 해상도의 Game View에서 사람이 키보드·마우스로 처음부터 Stage1 전환까지 플레이하며 표지 글자 크기, 게이트 상승 시 메시 관통, 카메라별 조명 대비와 가구 밀도를 최종 확인할 필요가 있다. 입력 액션·HUD·전투 밸런스·튜토리얼 순서는 이번 작업에서 변경하지 않았다.

## 2026-08-10 - MainScene Play TextMeshProUGUI 전환

- 변경 유형: 메인 메뉴 레이블 렌더링 컴포넌트 교체
- 변경 내용: **구현 완료**. `PlayLabel`의 레거시 `UnityEngine.UI.Text`를 제거하고 `TextMeshProUGUI`로 전환했다. `MainSceneBuilder`는 기본 TMP 폰트 에셋, 흰색 굵은 `PLAY` 텍스트, 가운데 정렬, 줄바꿈 없음과 overflow 표시를 설정하며, 기존 hover 확대·로고 빨간색 눌림 피드백은 TMP 레이블을 직접 갱신하도록 유지한다.
- 영향을 받은 시스템: MainScene 텍스트 렌더링, Canvas 포인터 피드백
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuButtonFeedback.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.27`로 갱신해 `TextMeshProUGUI` 렌더링 전환을 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `MainSceneBuilder.BuildAndValidateFromCommandLine`이 컴파일 오류 없이 완료됐고, TMP 레이블, feedback 레이블 참조, `1.08` hover 배율, `RGB(224, 28, 28)` 눌림 색상, 투명 입력 영역과 기존 다중 화면비 안전 영역을 검증하고 통과했다. 로그: `ProjectDeltatime/MainSceneBuildValidate.log`.
- 남은 작업: **미실행**. 실제 Game View에서 TMP 글꼴 외형·hover 확대·눌림 색상 및 `Tutorial` 전환을 수동 확인한다.

## 2026-08-10 - MainScene Play 포인터 피드백

- 변경 유형: 메인 메뉴 Play 텍스트 hover·press 상호작용 추가
- 변경 내용: **구현 완료**. `MainMenuButtonFeedback`은 투명 Play 클릭 영역의 포인터 enter/exit/down/up을 받아 레이블을 hover 중 `1.08`배로 키우고, 누르는 동안 로고 이미지에서 추출한 빨간색 `RGB(224, 28, 28)`으로 바꾼다. 버튼 배경은 계속 투명이며 mouse up 또는 exit 시 흰색·원래 크기로 복귀한다.
- 영향을 받은 시스템: MainScene UI 상호작용, Canvas 포인터 입력, 게임 시작 흐름
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Image/logo.png`, `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuButtonFeedback.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.26`으로 갱신해 흰색 텍스트 기본 상태, `1.08` hover 확대, 로고 빨간색 눌림 상태와 복귀 동작을 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `MainSceneBuilder.BuildAndValidateFromCommandLine`이 feedback 레이블 연결, `1.08` hover 배율, `RGB(224, 28, 28)` 눌림 색상, 투명·입력 가능 `Image`, 버튼 상태 전환 없음과 기존 다중 화면비 안전 영역을 검증하고 통과했다. 로그: `ProjectDeltatime/MainSceneBuildValidate.log`.
- 남은 작업: **미실행**. 실제 대상 디스플레이의 Game View에서 hover 확대, 누르고 있는 동안의 빨간색 표시, release·exit 복귀 및 `Tutorial` 전환을 수동 확인한다.

## 2026-08-10 - 반응형 MainScene 타이틀·Play 메뉴

- 변경 유형: 메인 메뉴 씬 구성, Canvas 반응형 레이아웃 적용, 빌드 시작 씬 및 Play 씬 전환 추가
- 변경 내용: **구현 완료**. 사용자가 만든 `MainScene`의 배경과 로고 이미지는 유지하고, 로고를 Canvas의 좌측 상단 안전 여백 `(72, 56)`에 비율 보존 배치했다. 배경은 원본 비율 `1672:941`을 유지한 채 부모를 덮도록 설정했고, Canvas는 기준 해상도 `1920×1080`, 화면 폭/높이 일치값 `0.5`의 `Scale With Screen Size`를 사용한다. 로고 아래에는 검은색 `PLAY` 텍스트가 들어간 흰색 버튼 하나만 추가했다. 버튼은 `Tutorial` 씬을 로드하며, `MainScene`은 Build Settings의 첫 번째 활성 씬이다.
- 영향을 받은 시스템: 게임 시작 흐름, Canvas UI 입력, 해상도·화면비 대응, Build Settings 씬 순서
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Image/background.png`, `ProjectDeltatime/Assets/_Project/Image/logo.png`, `ProjectDeltatime/Assets/_Project/Scripts/UI/MainMenuController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.24`로 갱신해 MainScene의 시작 흐름, Canvas 기반 화면비 대응, 씬 목록 및 UI 정보 구조를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `MainSceneBuilder.BuildAndValidateFromCommandLine`이 스크립트 컴파일, Canvas 스케일러, 비율 보존 배경, 타이틀·버튼 앵커/크기, 흰색 버튼·`PLAY` 레이블, `Tutorial` 연결, Build Settings 순서를 검증했다. 추가로 1920×1080, 2560×1080, 1080×1920, 1024×768, 3840×2160 좌표계에서 타이틀과 버튼이 화면 안전 영역 안에 놓이는지 검증했다. 로그: `ProjectDeltatime/MainSceneBuildValidate.log`.
- 남은 작업: **미실행**. 실제 대상 디스플레이에서의 Game View 시각 폴리시와 마우스·키보드로 버튼을 눌러 `Tutorial`이 로드되는 수동 확인은 아직 실행하지 않았다.

## 2026-08-10 - MainScene Play 텍스트 단독 표시

- 변경 유형: 메인 메뉴 Play 버튼 시각 조정
- 변경 내용: **구현 완료**. Play 버튼의 `Image`는 알파 0의 투명 입력 영역으로 유지하고, 버튼 상태 전환을 `None`으로 설정해 포인터 hover·press에도 배경이 표시되지 않게 했다. `PLAY` 레이블은 흰색 굵은 텍스트로 표시한다. 버튼 크기·좌측 앵커·`Tutorial` 로드 동작은 유지한다.
- 영향을 받은 시스템: MainScene UI 렌더링, Canvas 입력, 게임 시작 흐름
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/MainSceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.25`로 갱신해 배경 없는 흰색 Play 텍스트와 투명 입력 영역을 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `MainSceneBuilder.BuildAndValidateFromCommandLine`이 투명·입력 가능 Play Image, 상태 전환 없음, 흰색 `PLAY` 레이블, `Tutorial` 연결과 기존 다중 화면비 안전 영역을 검증하고 통과했다. 로그: `ProjectDeltatime/MainSceneBuildValidate.log`.
- 남은 작업: **미실행**. 실제 대상 디스플레이에서의 Game View 시각 폴리시와 마우스·키보드로 버튼을 눌러 `Tutorial`이 로드되는 수동 확인은 아직 실행하지 않았다.

## 2026-08-10 - 메인 플레이어 Business Male 모델 교체

- 변경 유형: 메인 플레이어 시각 프리팹 교체, Humanoid Animator·무기 프레젠터 재바인딩, 씬·재생성 경로 검증 확장
- 변경 내용: **구현 완료**. Tutorial 및 Stage1~Stage6의 `Player` 게임플레이 루트는 유지하고 시각 자식만 `Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Business_Male_01.prefab`으로 교체했다. `PlayerCharacterModelEditorSetup`은 정확한 프리팹 인스턴스, 유효한 Humanoid Avatar, `CharacterAnimationLibrary` Controller, Root Motion 비활성화, 시각 Collider 비활성화를 확인한다. `CharacterAnimationController.Configure`는 새 시각 루트를 명시적으로 받아 교체 후 대시 종료 회전이 파괴된 이전 모델을 참조하지 않게 했다. `CharacterAnimationEditorSetup`은 새 루트를 전달하고, `WeaponVisualPresenter`는 새 모델의 `RightHand`에 기존 권총·자동소총·샷건·근접 무기 시각과 `Weapon Muzzle`을 다시 장착한다. `PrototypeSceneBuilder`는 Stage2에도 캐릭터를 적용하며 Stage1·Stage3·Stage4·Stage5·Stage6 빌더의 플레이어 프리팹 경로도 같은 Business Male 모델로 갱신했다.
- 영향을 받은 시스템: 모든 플레이 가능 씬의 플레이어 렌더링, Humanoid Animator 프로필, 대시 시각 루트, 오른손 무기·총구 프레젠터, Stage1~Stage6 재생성 경로
- 관련 파일: `ProjectDeltatime/Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Business_Male_01.prefab`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PlayerCharacterModelEditorSetup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationEditorSetup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.23`으로 갱신해 모든 플레이 가능 씬의 메인 플레이어 모델, Animator·무기 연결, 정적/PlayMode 검증 범위와 남은 수동 확인 항목을 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `PlayerCharacterModelEditorSetup.ApplyBusinessMalePlayerModelFromCommandLine`이 Tutorial 및 Stage1~Stage6에서 Business Male 프리팹 경로, Humanoid Animator, Runtime Animator Controller, Root Motion 비활성화, 시각 Collider 비활성화를 검증하며 저장했다. 이어 `TutorialPlayModeSmokeTest.RunFromCommandLine`이 기존 월드 시간·투척/무장 해제·공중 회수·`DEADLINE` 진행과 애니메이션 프로필을, `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 장비별 Animator 전환, 오른손 무기·총구 계층, 조준 정렬, 투척/드롭 시각 및 근접 타격 프레임을 통과했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`도 NavMesh 완전 경로 5/5와 런타임 초기화를 통과했고, `PrototypeSceneBuilder.CapturePreviewFromCommandLine`의 Stage1 정적 프리뷰에서 Business Male 모델의 직립 배치를 확인했다. 로그: `ProjectDeltatime/BusinessMalePlayerModelApply.log`, `ProjectDeltatime/BusinessMaleTutorialSmoke.log`, `ProjectDeltatime/BusinessMaleStage1AnimationSmoke.log`, `ProjectDeltatime/BusinessMaleStage6Smoke.log`, `ProjectDeltatime/BusinessMaleStage1Preview.log`.
- 남은 작업: **확인 불가**. 모든 씬의 실제 게임 뷰에서 Business Male 모델의 손가락 그립, 무기·환경 메시 관통, 카메라 거리별 비율은 수동 확인이 필요하다. 권총 사격·피격·사망·투척/획득 전용 애니메이션은 기존과 같이 **미구현**이다.

## 2026-08-10 - Synty 튜토리얼 맵·캐릭터 애니메이션 적용

- 변경 유형: 튜토리얼 비주얼 전면 개선, 캐릭터 모델·Animator 적용, 맵 구성 및 검증 확장
- 변경 내용: **구현 완료**. `TutorialSceneBuilder`가 최신 Stage1에서 상속한 Party Female 01 플레이어와 Bartender Male·Bouncer Male·Party Male 02 기반 적 시각을 투척 적 1명·DEADLINE 적 4명까지 유지해 총 6명의 Synty 캐릭터를 구성한다. 플레이어의 시작 장비를 비운 뒤 모든 캐릭터에 `CharacterAnimationLibrary`, `DeltatimeCharacter.controller`, 장비별 Override Controller를 다시 연결하고, 프리팹 Collider·Root Motion은 비활성화했다. 기존 7단계 직선 동선은 유지하되 `Synty Tutorial Set` 아래 PolygonNightclubs의 바닥 60개, 벽 46개, 구역 기둥·바닥등 28개, DJ 부스·냉장고·벤치·상자·바 엄폐·덤스터·출구 표지를 포함한 연결 프리팹 145개로 실내 훈련장을 재구성했다. 기존 프리미티브 바닥·외벽·사격 레일은 보이지 않는 충돌/NavMesh 프록시로 남겼고 큰 Synty 소품에는 Layer 8 `VisionObstacle` 콜라이더를 추가했다. 시작부터 출구까지 7개 체크포인트의 NavMesh 완전 경로와 캐릭터·랜드마크·프리팹 개수를 정적 검증한다.
- 영향을 받은 시스템: Tutorial 씬 시각, 플레이어·적 Animator와 장비 프로필, 충돌체, Layer 8 시야 장애물, NavMesh, 조명·카메라 프리뷰, 튜토리얼 스모크 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Animation/DeltatimeCharacter.controller`, `ProjectDeltatime/Assets/_Project/Animation/CharacterAnimationLibrary.asset`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Prefabs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.22`로 갱신해 Tutorial의 Synty 환경 구성, 캐릭터 모델·애니메이션 연결, Physics 프록시, VisionObstacle, NavMesh 경로와 최신 검증 범위를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`이 스크립트 컴파일, 씬 재생성, Synty 프리팹 145개, 애니메이션 캐릭터 6명, 전용 `TutorialNavigation.asset`, 7개 중앙 체크포인트 완전 경로, Layer 8 장애물과 빌드 순서를 검증하고 통과했다. `TutorialPlayModeSmokeTest.RunFromCommandLine`은 6개 Humanoid Animator 초기화, 비무장/근접/Pistol 프로필 전환, 근접 공격 트리거, 실제 NavMesh 이동의 `MoveX`/`MoveY` 로코모션 블렌드와 기존 월드 시간·표적·투척·공중 회수·Q `DEADLINE`·체크포인트 복구 흐름을 통과했다. 46도 게임 카메라 각도의 남쪽 훈련 구간과 북쪽 DEADLINE 아레나 프리뷰도 직접 확인했다. 전역 `Time.timeScale`은 변경하지 않았다.
- 남은 작업: **확인 불가**. 사람이 키보드·마우스로 처음부터 Stage1 전환까지 플레이하는 최종 체감, 캐릭터 손·무기 관통, 게이트 크기와 각 구역 미술 밀도의 최종 폴리시는 수동 확인이 필요하다. 권총 사격·피격·사망·투척/획득 전용 애니메이션은 기존과 같이 **미구현**이다.

## 2026-08-09 - 플레이어 조준 방향 가이드선 제거

- 변경 유형: 플레이어 조준 시각 피드백 제거
- 변경 내용: **구현 완료**. `PlayerAim`의 청록색 조준 `LineRenderer` 갱신과 `PrototypeSceneBuilder`의 해당 렌더러 생성·연결을 제거했다. 현재 `Stage1`~`Stage6`, `Tutorial`, `WeaponCalibration`에 저장돼 있던 레거시 렌더러는 즉시 표시되지 않도록 비활성화했다. 플레이어 회전, 조준점 계산, 월드 시간 조준 활동량, 총구·투사체 방향은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 조준 시각, 씬 직렬화, `PrototypeSceneBuilder` 재생성 경로
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/WeaponCalibration.unity`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.21`로 갱신해 조준 방향 라인 피드백 제거와 남은 디버그 Ray의 범위를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일과 Stage6 Play Mode 스모크를 통과했다. 코드에 조준 방향 렌더러 갱신·생성 경로가 남지 않고, 8개 대상 씬의 레거시 렌더러가 모두 비활성화된 것도 정적으로 확인했다. 모든 씬의 실제 화면 수동 확인은 **미실행**이다.
- 남은 작업: Unity Play Mode에서 모든 씬의 플레이어 주변에 청록색 조준선이 표시되지 않는지 수동 확인한다.

## 2026-08-09 - 근접 무기 손 모델 보정값 적용

- 변경 유형: 근접 무기 오른손 모델 로컬 Transform 보정
- 변경 내용: **구현 완료**. `MeleeWeapon.asset`의 `heldModelLocalPosition`을 `(0.019, 0.021, 0.093)`, `heldModelLocalEulerAngles`를 `(189.308, -24.15198, -6.239014)`로 갱신했다. `heldModelLocalScale` `(1, 1, 1)`과 기존 총구 보정값은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어·적 근접 무기 오른손 장착 시각, 야구방망이 손 그립
- 관련 파일: `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.20`으로 갱신해 근접 무기 손 모델 직렬화 보정값과 타격 판정의 비영향 범위를 기록했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 손 그립·이동·공격 중 시각 정렬은 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 근접 무기 장착 상태의 손 그립과 공격 중 모델 관통 여부를 수동 확인한다.

## 2026-08-09 - 플레이어 투척 무기 비행거리 축소

- 변경 유형: 투척 무기 밸런스 조정
- 변경 내용: **구현 완료**. `ThrownWeapon.maximumTravelDistance`와 `ThrownWeapon.prefab`의 직렬화 값을 6m에서 4m로 낮췄다. `PrototypeSceneBuilder`가 이후 프리팹을 재생성할 때도 같은 4m를 적용한다. 속도 7, 충돌 반경 0.25, 기절 시간 2 월드초와 충돌 시 즉시 기절·착지·픽업 변환은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 무기 투척, 기절 판정 도달 범위, 바닥 무기 착지·회수, 월드 시간 기반 투척 이동
- 관련 파일: `ProjectDeltatime/Assets/_Project/Prefabs/ThrownWeapon.prefab`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.19`로 갱신해 실제 최대 거리 4m, 유지된 속도·기절 시간, 밸런스 표를 반영했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일이 성공했고, 프리팹 직렬화값·컴포넌트 기본값·프리팹 재생성값이 모두 4m인 것을 정적으로 확인했다.
- 남은 작업: Play Mode에서 벽 비충돌 상태의 투척물이 총구 시작점에서 최대 4m에 착지하는지와 실제 조작 체감을 수동 확인한다.

## 2026-08-09 - Automatic Rifle 손 모델·총구 보정값 적용

- 변경 유형: Automatic Rifle 손 모델 및 실제 발사 총구 로컬 Transform 보정
- 변경 내용: **구현 완료**. `AutomaticRifle.asset`의 `heldModelLocalPosition`을 `(-0.227, 0.013, -0.188)`, `heldModelLocalEulerAngles`를 `(-4.056, 65.2, -85.452)`, `heldModelLocalScale`을 `(1.2, 1.2, 1.2)`로 갱신했다. `heldMuzzleLocalPosition`은 `(0, 0.061, 0.96)`로 갱신했고, 총구 로컬 회전 `(0, 0, 0)`은 유지했다.
- 영향을 받은 시스템: 플레이어·적 Automatic Rifle 오른손 장착 시각, Rifle `Weapon Muzzle` 위치, 투사체 생성 시작점
- 관련 파일: `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.17`로 갱신해 Automatic Rifle 손 모델·총구 직렬화 보정값과 수동 확인 범위를 기록했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 Rifle 손 그립·총구 축·투사체 출발점은 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Rifle 장착 상태의 손 그립, 총구 시각 위치, 실제 투사체 생성 위치를 수동 확인한다.

## 2026-08-09 - 무기 픽업 모델 경계형 Trigger Collider

- 변경 유형: 무기 픽업 상호작용 범위 정밀화 및 프리팹 생성 검증 확장
- 변경 내용: **구현 완료**. `BuildPlaceableWeaponPickups`가 각 `Weapon Model Visual` 아래 활성 Renderer의 월드 경계 8개 꼭짓점을 픽업 로컬 좌표로 변환·합산해 `BoxCollider.center`와 `size`를 저장하도록 변경했다. 기존 공통 `1×1×1` 크기를 권총 `(0.042992774, 0.27395654, 0.42000002)`, 자동소총 `(0.069229424, 0.33086163, 0.9599999)`, 샷건 `(0.063373215, 0.23060584, 0.9200001)`, 근접 무기 `(0.06476646, 0.064766586, 0.91999996)`의 실제 모델 경계 크기로 교체했다. Trigger 방식은 유지하며, 최소 축 크기만 `0.01`로 제한한다. 프리팹 검증은 정의·탄약·트리거·모델뿐 아니라 재계산한 경계와 Collider 직렬화 값의 일치도 확인한다.
- 영향을 받은 시스템: 씬에 배치한 무기 픽업의 상호작용 Trigger 범위, 바닥 무기 모델 시각, 콘텐츠 생성 도구
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Prefabs/PistolPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/AutomaticRiflePickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/MeleeWeaponPickup.prefab`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.18`로 갱신해 모델 경계 기반 Collider 정책, 실제 직렬화 크기·중심, 자동 검증 범위를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드에서 `Deltatime.EditorTools.PrototypeSceneBuilder.BuildPlaceableWeaponPickups`를 실행해 프리팹 4종을 재생성하고 정의·탄약·Trigger Collider·월드 모델·계산 경계 일치 검증을 통과했다. 컴파일 오류 및 프리팹 검증 실패는 없었다.
- 남은 작업: 새 빈 씬에 네 프리팹을 각각 배치한 뒤, 실제 플레이어 획득 가능 거리와 적의 무기 탐색·예약 체감을 Play Mode로 수동 확인한다.

## 2026-08-09 - 직접 배치용 무기 픽업 프리팹 4종

- 변경 유형: 콘텐츠 제작 워크플로 및 무기 픽업 프리팹 추가
- 변경 내용: **구현 완료**. `PistolPickup.prefab`과 `ShotgunPickup.prefab`을 월드 모델 포함 구성으로 재생성하고, `AutomaticRiflePickup.prefab`, `MeleeWeaponPickup.prefab`을 추가했다. 네 프리팹은 각각 대응 `WeaponDefinition`, 최대 시작 탄약(권총 8발, 자동소총 30발, 샷건 6발, 근접 무기 0발), Trigger `BoxCollider`, `Weapon Model Visual` 자식을 직렬화한다. `Tools/Prototype/Build Placeable Weapon Pickups` 및 `BuildPlaceableWeaponPickups`는 네 프리팹을 함께 생성하고 정의·탄약·트리거·월드 모델 포함을 검증한다.
- 영향을 받은 시스템: 씬 콘텐츠 제작, 플레이어/적 무기 획득·교환, 바닥 무기 월드 시각
- 관련 파일: `ProjectDeltatime/Assets/_Project/Prefabs/PistolPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/AutomaticRiflePickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/MeleeWeaponPickup.prefab`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.16`으로 갱신해 네 직접 배치용 프리팹의 구성, 생성 메뉴, 정적 검증 범위를 기록했다.
- 테스트 결과: **구현 완료**. Unity 6000.1.13f1 배치 모드에서 `Deltatime.EditorTools.PrototypeSceneBuilder.BuildPlaceableWeaponPickups`를 실행해 프리팹 4종 생성과 정의·탄약·Trigger Collider·월드 모델 검증을 통과했다. 기존 `FindObjectOfType` API 사용 경고가 있었으나 컴파일 오류와 생성 검증 실패는 없었다.
- 남은 작업: 새 빈 씬에서 네 프리팹을 수동 배치한 뒤 플레이어 획득·교환과 적 재무장 흐름을 Play Mode로 확인한다.

## 2026-08-09 - Shotgun 손 모델·총구 보정값 적용

- 변경 유형: Shotgun 손 모델 및 실제 발사 총구 로컬 Transform 보정
- 변경 내용: **구현 완료**. `Shotgun.asset`의 `heldModelLocalPosition`을 `(0.044, 0.118, -0.037)`, `heldModelLocalEulerAngles`를 `(2.878, 68.211, -91.666)`로 갱신했다. `heldModelLocalScale`은 이미 일치하는 `(1, 1, 1)`을 유지했고, `heldMuzzleLocalPosition`은 `(0, 0.071, 0.92)`로 갱신했다. 총구 로컬 회전 `(0, 0, 0)`은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어·적 Shotgun 오른손 장착 시각, Shotgun `Weapon Muzzle` 위치, 투사체 생성 시작점
- 관련 파일: `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.15`로 갱신해 Shotgun 손 모델·총구 직렬화 보정값과 수동 확인 범위를 기록했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 Shotgun 손 그립·총구 축·투사체 출발점은 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Shotgun 장착 상태의 손 그립, 총구 시각 위치, 실제 투사체 생성 위치를 수동 확인한다.

## 2026-08-09 - Pistol Animator Idle 매핑 복구

- 변경 유형: Pistol Animator Override Controller 클립 참조 재연결
- 변경 내용: **구현 완료**. `Pistol.overrideController`의 기본 Idle 클립 Override를 이전 `Characters@Pistol Idle.fbx`에서 현재 `Pistol_Handgun Locomotion Pack/pistol idle.fbx`로 다시 연결했다. 전진·후진·좌·우 방향 이동 Override는 각각 현재 `pistol walk`, `pistol walk backward`, `pistol strafe`, `pistol strafe (2)` 클립을 이미 참조하고 있어 변경하지 않았고, 공용 Roll·Attack 매핑도 유지했다.
- 영향을 받은 시스템: Pistol 장착 시 Idle 및 방향 이동 Animator 프로필
- 관련 파일: `ProjectDeltatime/Assets/_Project/Animation/Pistol.overrideController`, `ProjectDeltatime/Assets/Animations/Pistol_Handgun Locomotion Pack/pistol idle.fbx`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.13`으로 갱신해 Pistol Override의 Idle·방향 이동 매핑과 수동 확인 항목을 기록했다.
- 테스트 결과: **부분 통과**. Override의 Idle GUID가 현재 `pistol idle.fbx` GUID와 일치하고, 네 방향 이동 GUID가 각 소스 FBX와 일치하는지 정적으로 확인했다. Unity Play Mode는 **미실행**이다.
- 남은 작업: Play Mode에서 Pistol 장착 후 Idle, 전진, 후진, 좌·우 이동 전환을 수동 확인한다.

## 2026-08-09 - Pistol 손 모델 최종 장착 보정

- 변경 유형: Pistol 손 모델 Position/Rotation 보정값 갱신
- 변경 내용: **구현 완료**. `Pistol.asset`의 `heldModelLocalPosition`을 `(0.08, 0.03, -0.039)`, `heldModelLocalEulerAngles`를 `(11.737, 65.521, -448.114)`, `heldModelLocalScale`을 `(0.65, 0.65, 0.65)`로 설정했다. 실제 발사 총구 로컬 보정값은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 Pistol 오른손 장착 시각, Tactical Pistol 모델 위치·회전·크기
- 관련 파일: `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`의 Pistol 손 모델 직렬화 보정값을 새 값으로 갱신했다.
- 테스트 결과: **미실행**. 에셋 직렬화 값은 정적으로 확인했으나 Unity Play Mode에서 최종 손 그립과 총구 시각 정렬은 아직 재확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Pistol 장착 상태의 손 그립·총구 위치·조준 방향을 수동 확인한다.

## 2026-08-09 - 총기 탄환 생성 위치를 시각 총구로 통일

- 변경 유형: 총기 탄환 생성 위치 변경
- 변경 내용: **구현 완료**. `WeaponController`의 일반 발사와 `DEADLINE` 준비 발사가 기존 Player 루트 `muzzle.position` 대신 공용 `Muzzle.position`을 사용하도록 변경했다. 커스텀 시각 무기가 장착된 Pistol은 `RightHand → Weapon Aim Pivot → Held Weapon Model → Weapon Muzzle`의 실제 시각 총구에서 탄환이 생성되며, 커스텀 총구가 없으면 기존 직렬화 총구로 폴백한다.
- 영향을 받은 시스템: 플레이어 Pistol·Rifle·Shotgun 탄환 생성 위치, 총구 시각과 발사 시작점의 일치
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`에 실제 탄환 생성 위치가 `WeaponController.Muzzle` 프로퍼티를 따른다는 내용을 반영했다.
- 테스트 결과: **미실행**. 소스 참조와 `git diff --check`는 확인했으나 Unity Play Mode에서 Pistol 총구와 탄환 생성 위치가 일치하는지 아직 확인하지 않았다.
- 남은 작업: WeaponCalibration Play Mode에서 Pistol·Rifle·Shotgun의 발사 시각 위치와 투사체 출발점을 수동 확인한다.

## 2026-08-09 - 플레이어 Pistol 시각 루트 Y축 보정 제거

- 변경 유형: Pistol 장착 애니메이션의 임시 시각 회전 보정 제거, Stage1 애니메이션 스모크 원복
- 변경 내용: **구현 완료**. Pistol 장착 시에만 적용하던 `+36.1°` 시각 루트 Y축 보정, 관련 공개 상태값, 대시 회전 보정과 Stage1 스모크 검증을 제거했다. 모든 장비 프로필은 다시 기존 시각 루트 기준 회전과 대시 방향 회전만 사용한다. 게임플레이 `Player` Rigidbody 루트, 이동·조준·발사 방향, `WeaponVisualPresenter`의 총구 조준 보정은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 Pistol Idle/이동/대시 시각, Humanoid 오른손·손끝 기준축, Stage1 애니메이션 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.14`로 갱신해 Pistol 전용 Y축 보정 제거와 남은 정렬 과제를 기록했다.
- 테스트 결과: **부분 통과**. Pistol 전용 보정 필드·속성·회전 적용·스모크 단언이 제거됐고 `git diff --check`를 통과했다. Unity Play Mode 스모크는 **미실행**이다.
- 남은 작업: **계획 필요**. Pistol 손끝·몸체 forward 정렬은 시각 루트 일괄 회전 대신 기준 포즈 보정, 조준 상체 레이어 또는 IK 방식 중 하나를 정해 구현해야 한다.

## 2026-08-09 - 플레이어 몸체 전방 Debug Ray

- 변경 유형: 플레이어 방향 디버그 시각화 추가
- 변경 내용: **구현 완료**. `PlayerAim.Update`가 플레이어 루트 위치의 Y축 0.08m 위에서 `transform.forward` 방향으로 1.5m 길이의 초록색 `Debug.DrawRay`를 매 프레임 그린다. 기존 조준 `LineRenderer`는 변경하지 않아 몸체 forward와 마우스 조준선을 독립적으로 비교할 수 있다. Ray는 디버그 표시만 수행하며 이동·회전·월드 시간·무기 판정은 변경하지 않는다.
- 영향을 받은 시스템: 플레이어 조준·회전의 Scene/Game Gizmos 디버그 표시
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.11`로 갱신해 Ray 시작점·방향·길이·색상·기능 영향 범위와 검증 상태를 기록했다.
- 테스트 결과: **부분 통과**. `PlayerAim.cs`에서 초록색 Ray의 시작점·`transform.forward` 방향·1.5m 길이를 정적으로 확인했고 변경 파일의 `git diff --check`를 통과했다. Unity 배치 컴파일은 사용자 승인 거부로 **미실행**했다. 이후 .NET 정적 빌드는 Unity가 생성하는 `Temp/obj/Assembly-CSharp/project.assets.json` 부재로 시작되지 않아 컴파일 결과는 **확인 불가**다.
- 남은 작업: **확인 불가**. Play Mode에서 Scene 뷰 또는 Game 뷰의 Gizmos를 켜고, 초록색 Ray가 플레이어 몸체의 기대 전방축을 가리키는지 수동 확인이 필요하다.

## 2026-08-09 - 플레이어 총기 Aim Pivot 조준 시각 보정

- 변경 유형: 플레이어 총기 오른손 장착 계층 재구성, 마우스 조준 방향 시각 보정, Stage1 Play Mode 무기 검증 확장
- 변경 내용: **구현 완료**. `WeaponVisualPresenter`의 런타임 계층을 `RightHand → Weapon Aim Pivot → Held Weapon Model → Weapon Muzzle`로 변경했다. 기존 `WeaponDefinition`의 손 모델 Position/Rotation/Scale은 `Held Weapon Model`에 그대로 적용한다. `LateUpdate`에서 Animator가 갱신한 손 포즈 뒤에 Aim Pivot을 기본 로컬 Transform으로 되돌리고, 현재 `Weapon Muzzle.forward`와 `PlayerAim`의 조준점을 향하는 방향을 수평면에 투영해 계산한 Y축 회전만 Pivot에 적용한다. 보정은 `PlayerAim`이 있는 플레이어의 권총·자동소총·샷건에만 적용하며, 근접 무기와 적 장비에는 적용하지 않는다. `PlayerDash.IsDashing` 중에는 Pivot을 기본 회전으로 유지해 대시 방향 구르기 시각을 우선한다. `Weapon Muzzle`은 계속 실제 발사 시작점이고 `PlayerCombat`의 기존 마우스 조준점 기반 탄환 방향 계산은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 권총·자동소총·샷건 오른손 시각, 총구 수평 전방축, 구르기 시각, Stage1 무기 장착·바닥·투척·공중 드롭 자동 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.10`으로 갱신해 Aim Pivot 계층, 플레이어 총기 전용 LateUpdate 수평 조준 보정, 대시 중 해제, 탄환 판정 비변경, 검증 결과와 손/IK 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 권총·자동소총·샷건의 현재 `Weapon Muzzle`이 오른손 아래 Aim Pivot 계층에 있는지, 수평 전방축과 `PlayerAim` 방향의 각도 오차가 0.25도 이내인지, 근접 무기 Pivot이 기본 회전인지 확인했다. 기존 바닥 픽업·플레이어 투척·적 무장 해제 공중 드롭 모델과 근접 타이밍 검증도 함께 통과했다. 이어 `WeaponCalibrationSceneBuilder.ValidateFromCommandLine` 정적 검증을 통과했다. 로그: `ProjectDeltatime/AimPivotStage1Smoke.log`, `ProjectDeltatime/AimPivotWeaponCalibrationValidate.log`.
- 남은 작업: **확인 불가**. 손가락·왼손 IK는 추가하지 않았으므로 이동·공격 애니메이션 중 손의 미세한 관통과 구르기 중 실제 시각 자연스러움은 Play Mode 수동 확인이 필요하다.

## 2026-08-09 - Tactical Pistol 손·총구 수동 보정값 적용

- 변경 유형: 권총 무기 모델 Transform 보정값 갱신
- 변경 내용: **구현 완료**. `Pistol.asset`의 `heldModelLocalPosition`을 `(0.058, -0.009, -0.007)`, `heldModelLocalEulerAngles`를 `(-11.904, 73.839, 185.269)`, `heldModelLocalScale`을 `(0.65, 0.65, 0.65)`으로 저장했다. `heldMuzzleLocalPosition`은 `(0, 0.112, 0.42)`, `heldMuzzleLocalEulerAngles`는 `(0, 0, 0)`으로 저장했다. 따라서 `WeaponVisualPresenter`가 Humanoid 오른손에 생성하는 Tactical Pistol 모델과 그 내부 `Weapon Muzzle`이 해당 로컬 Transform을 사용한다.
- 영향을 받은 시스템: 플레이어/적 권총 오른손 시각, 플레이어 권총 투사체 시작점, 적 권총 경고선·사격 원점, WeaponCalibration 수동 보정
- 관련 파일: `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponCalibrationSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.9`로 갱신해 실제 직렬화된 권총 손 모델·총구 로컬 Transform과 검증/수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `WeaponCalibrationSceneBuilder.ValidateFromCommandLine`이 변경된 `Pistol.asset`을 재임포트한 뒤 저장된 `WeaponCalibration` 씬을 열어 정적 검증을 통과했다. 로그: `ProjectDeltatime/WeaponCalibrationPistolPoseValidate.log`.
- 남은 작업: **확인 불가**. 정적 검증은 에셋 로드와 보정 씬 구성을 확인한다. 실제 플레이 화면에서 권총의 손 그립·총구 축이 의도와 일치하는지는 수동 확인이 필요하다.

## 2026-08-09 - 샷건 플레이어 이동 반동 제거

- 변경 유형: 샷건 밸런스 조정, 무기 데이터·씬 빌더 검증·기획 문서 갱신
- 변경 내용: **구현 완료**. `Shotgun.asset`의 `playerRecoilDistance`를 `0.35m`에서 `0m`로 변경했다. 따라서 일반 발사와 `DEADLINE` 준비 발사 해제 모두 `PlayerCombat`의 공용 반동 대기 경로를 통과하더라도 `PlayerMovement`에 이동량이 등록되지 않아 플레이어를 뒤로 밀지 않는다. `PrototypeSceneBuilder`의 생성값과 저장 데이터 검증값도 0m로 맞췄다.
- 영향을 받은 시스템: 샷건 발사, 플레이어 이동, `DEADLINE` 준비/해제 발사, 무기 데이터, Stage1/Stage2 씬 재생성·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.8`로 갱신해 샷건의 플레이어 이동 반동 0m, 일반·`DEADLINE` 해제 발사의 무이동 정책, 데이터 표와 변경 이력을 반영했다.
- 테스트 결과: **부분 통과**. Unity 6000.1.13f1 배치 컴파일 명령이 종료 코드 0으로 완료했다. `Shotgun.asset`의 직렬화 값과 `PrototypeSceneBuilder`의 생성·검증값은 모두 0m임을 정적으로 확인했다. `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`과 PlayMode 스모크는 기존 작업 트리의 저장 씬 변경을 재생성으로 덮어쓸 수 있어 **미실행**했다.
- 남은 작업: **확인 불가**. 별도 보존 지점에서 빌더·PlayMode 스모크를 실행하고, 일반 발사와 `DEADLINE` 해제 발사 후 플레이어 위치가 유지되는지 수동 확인이 필요하다.

## 2026-08-09 - 적 없는 전용 무기 보정 씬

- 변경 유형: 무기 시각 보정 전용 씬·에디터 빌더 추가, 보정 창 안내 갱신
- 변경 내용: **구현 완료**. `WeaponCalibration.unity`는 Stage1을 별도 씬으로 저장한 뒤 플레이어·카메라·공간·월드 시간·기존 무기 픽업은 유지하고, 모든 적과 `StageController`, `StageReplayController`, 레거시 `GameHud`를 제거한다. `VisionCone`은 무제한 시야가 되어 리플레이 시야 조명에 의존하지 않는다. `Build Weapon Calibration Scene`은 이 구성을 Stage1에서 다시 생성하고, `Open Weapon Calibration Scene`은 기존 씬을 열어 Player를 선택하고 무기 보정 창을 연다. 무기 손/총구/월드 모델 수치는 기존처럼 `WeaponDefinition` 에셋에 저장되므로 이 씬의 재생성과 분리된다. 보정 창의 안내도 Stage1 대신 WeaponCalibration Play Mode를 사용하도록 변경했다. 이 에디터 전용 씬은 Build Settings에 추가하지 않는다.
- 영향을 받은 시스템: 무기 모델·총구 위치 보정, 플레이어 전투/이동/Animator 수동 시험, 시야 연출, 에디터 씬 생성·정적 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/WeaponCalibration.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponCalibrationSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponModelCalibrationWindow.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.7`로 갱신해 보정 씬의 구성·메뉴·재생성 범위·Build Settings 제외 정책과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `WeaponCalibrationSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 씬 생성 뒤 정확히 한 명의 무장 플레이어·월드 시간·카메라·캐릭터 Animator, 무제한 `VisionCone`, 적/StageController/StageReplayController/GameHud 0개를 정적 검증했다. 안내 문구 수정 뒤 `ValidateFromCommandLine`으로 저장된 씬을 다시 열어 같은 정적 검증을 통과했다. 스크립트 컴파일도 두 실행에서 `Tundra build success`로 완료했다. 로그: `ProjectDeltatime/WeaponCalibrationBuild.log`, `ProjectDeltatime/WeaponCalibrationValidate.log`.
- 남은 작업: **확인 불가**. 자동 검증은 씬 구성과 참조 제거만 확인한다. 실제 Play Mode에서 각 무기의 손 그립, 총구 축, 투척/드롭 월드 모델 크기와 조작 감각은 사용자가 보정 창으로 수동 확인해야 한다.

## 2026-08-08 - 무기 모델·총구 보정 창

- 변경 유형: 무기 시각 보정 Editor 도구 추가, 실제 발사/경고선 원점 모델 총구 연동, PlayMode 회귀 검증 확장
- 변경 내용: **구현 완료**. `Tools/Prototype/Animation/Calibrate Weapon Models` Editor 창에서 Pistol·Automatic Rifle·Shotgun·Melee Weapon의 오른손 모델과 바닥/투척/공중 드롭 모델의 위치·회전·스케일, 모델 내부 실제 발사 총구의 위치·회전을 편집한다. 창은 Play Mode에서 선택 무기를 플레이어에게 즉시 장착하고 값을 변경할 때 해당 `WeaponDefinition` 에셋에 저장하며, 현재 장비 모델을 즉시 갱신한다. `WeaponVisualPresenter`는 손 모델 안에 `Weapon Muzzle` 자식을 만들고, `WeaponController`는 그 위치를 우선 총구로 사용한다. 플레이어의 탄환 시작점·조준점 방향 계산과 적의 경고선·사격 원점이 조정한 모델 총구 위치를 사용한다. 총구 회전은 모델 축/Gizmo용이며 탄환 방향은 기존 조준점/대상 방향을 유지한다. Scene Gizmos는 선택된 프레젠터의 총구 위치와 전방 축을 청록색으로 표시한다. 이후 무기 모델 빌드는 기존 손/월드/총구 보정값을 유지한다.
- 영향을 받은 시스템: 무기 ScriptableObject 보정 데이터, 플레이어/적 총기 발사 원점·조준·경고선, Humanoid 오른손 모델, 바닥 픽업·투척·공중 드롭 모델, Unity Editor 도구
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/WeaponModelCalibrationWindow.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.6`으로 갱신해 보정 창 사용 범위, `Weapon Muzzle` 우선 발사 원점, Gizmo 표시, 자동 검증과 수동 보정 필요 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildWeaponModelsFromCommandLine`이 네 무기 정의의 기본 모델 총구 오프셋을 생성했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 네 무기의 손 모델·`Weapon Muzzle` 자식·바닥·투척·공중 드롭 모델 및 Cube/Body 비활성화를 통과했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 적 사격 원점 변경 뒤 NavMesh 완전 경로 5/5와 런타임 초기화를 통과했다. 로그: `ProjectDeltatime/WeaponCalibrationBuild.log`, `ProjectDeltatime/WeaponCalibrationFinalSmoke.log`, `ProjectDeltatime/WeaponCalibrationStage6Smoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 총구 Transform 연결과 게임플레이 회귀를 확인하지만, 각 Synty 캐릭터의 손가락 그립과 사용자가 의도한 총구 축·비행 방향/크기는 수동 Play Mode에서 보정 창으로 조절해야 한다.

## 2026-08-08 - 권총·자동소총·샷건 모델 적용

- 변경 유형: 신규 무기 FBX 정규화, 무기 정의 시각 에셋 연결, 손·바닥·투척·공중 드롭 검증 확장
- 변경 내용: **구현 완료**. `Assets/MR POLY/Low Poly Weapons Set/Models`의 `Tactical Pistol.fbx`, `Assault Rifle.fbx`, `Pump Shotgun.fbx`를 각각 0.42m, 0.96m, 0.92m 길이의 `TacticalPistol.prefab`, `AssaultRifle.prefab`, `PumpShotgun.prefab`으로 정규화했다. `Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`의 held/world 모델 참조와 오프셋을 설정했으므로, `WeaponVisualPresenter`, `WeaponPickup`, `WeaponFlightVisualPresenter`가 동일 모델을 오른손, 바닥, 플레이어 투척, 적 무장 해제 공중 드롭에 사용한다. 기존 Cube는 모델을 가진 세 정의에서 숨겨진다.
- 영향을 받은 시스템: 플레이어·적 장비 시각, 바닥 무기 픽업·교환, 플레이어 무기 투척, 적 기절·무장 해제·공중 드롭/가로채기, 무기 정의
- 관련 파일: `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Tactical Pistol.fbx`, `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx`, `ProjectDeltatime/Assets/MR POLY/Low Poly Weapons Set/Models/Pump Shotgun.fbx`, `ProjectDeltatime/Assets/_Project/Animation/TacticalPistol.prefab`, `ProjectDeltatime/Assets/_Project/Animation/AssaultRifle.prefab`, `ProjectDeltatime/Assets/_Project/Animation/PumpShotgun.prefab`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.5`로 갱신해 세 총기 모델의 생성·참조 범위, 네 무기 자동 검증과 실제 그립/비행 방향 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildWeaponModelsFromCommandLine`이 세 프리팹과 ScriptableObject 참조를 생성했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 권총·자동소총·샷건·야구방망이마다 오른손·바닥 픽업·`ThrownWeapon`·`InterceptableWeapon`의 모델 생성과 Cube/Body 비활성화를 통과했다. 로그: `ProjectDeltatime/WeaponModelBuild.log`, `ProjectDeltatime/WeaponModelsSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 모델 연결·생성만 확인한다. Synty 손가락 그립, 무기별 손 위치/방향, 회전 중 비행 방향·크기는 사용자가 수동으로 확인한 뒤 `CharacterAnimationAssetBuilder.ConfigureFirearmWeaponVisuals`의 오프셋과 각 무기 정의의 값으로 조정해야 한다.

## 2026-08-08 - 투척·공중 드롭 무기 모델 표시

- 변경 유형: 무기 비행 시각 교체, 투척·무장 해제 공중 드롭 공통화, Stage1 PlayMode 스모크 확장
- 변경 내용: **구현 완료**. `WeaponFlightVisualPresenter`가 `WeaponDefinition.worldVisualPrefab`을 가진 무기를 비행 루트의 자식으로 생성한다. `ThrownWeapon`(플레이어 투척)과 `InterceptableWeapon`(적 기절·무장 해제 공중 드롭)은 이를 초기화 시 적용하고, 모델이 있으면 기존 Cube/Body 렌더러를 숨긴다. 따라서 `MeleeWeapon.asset`의 `BaseballBat_Raw_Wood_Clean.prefab`이 바닥 픽업뿐 아니라 플레이어가 던진 무기와 적에게서 날아온 공중 무기에도 표시된다. 월드 모델이 없는 정의는 기존 Cube fallback을 그대로 사용한다.
- 영향을 받은 시스템: 플레이어 무기 투척, 적 기절·무장 해제·공중 드롭, 공중 무기 가로채기, 무기 ScriptableObject 월드 시각
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponFlightVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.4`로 갱신해 투척·공중 드롭의 월드 모델/fallback 정책, 자동 검증 결과와 실제 비행 방향 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 야구방망이 정의로 `ThrownWeapon`과 `InterceptableWeapon`을 각각 초기화해 `Flying Weapon Model` 생성과 기존 Cube/Body 비활성화를 확인했다. 기존 오른손·바닥 모델 및 근접 타격 시점 검증도 함께 통과했다. 로그: `ProjectDeltatime/WeaponFlightSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 모델 생성과 fallback 숨김만 확인한다. 실제 플레이에서 방망이가 회전할 때의 방향·크기·궤적 체감은 수동 확인 후 필요하면 `MeleeWeapon.asset`의 world 모델 오프셋/회전/스케일을 조정해야 한다.

## 2026-08-08 - 근접 타격 프레임 동기화·야구방망이 모델 적용

- 변경 유형: 근접 피해 판정 시점 변경, 상체 Animator 레이어 동기화, 근접 무기 손/바닥 시각 에셋 교체, Stage1 스모크 확장
- 변경 내용: **구현 완료**. 플레이어의 빈손·근접 무기와 적의 빈손·근접 무기는 입력/AI 공격 시작 시 `MeleeAttackExecution`에 판정을 보류한다. 생성된 `Upper Body Attack` 레이어의 두 공격 상태는 `MeleeAttackImpactBehaviour`를 가지며 정규화 시간 0.48에서 보류된 판정을 정확히 한 번 실행한다. 하체 방향 이동 레이어는 공격 중에도 유지된다. Animator가 없는 씬은 즉시 피해를 주는 호환 경로를 유지한다. `BaseballBat_Raw_Wood(Clean)`은 길이 0.92m 기준의 `BaseballBat_Raw_Wood_Clean.prefab`으로 정규화되어 `MeleeWeapon.asset`에 연결되고, Humanoid 오른손 및 `WeaponPickup`의 바닥 표시에서 사용된다.
- 영향을 받은 시스템: 플레이어/적 근접 전투·DEADLINE 준비 근접 공격, Animator Controller/Override, 장비 시각 표시, 바닥 무기 픽업, Stage1 및 Stage3~Stage6 캐릭터 씬
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/MeleeAttackImpactBehaviour.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/WeaponVisualPresenter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Animation/BaseballBat_Raw_Wood_Clean.prefab`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.6.3으로 갱신해 근접 타격 프레임, 야구방망이 오른손/바닥 표시, 자동 검증 결과와 수동 조정 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 캐릭터 26명을 구성했다. `PrototypeSceneBuilder.ValidateStage1CharacterAnimationsFromCommandLine`은 상체 레이어·실행 컴포넌트를 통과했고, `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 오른손 야구방망이·바닥 픽업 모델 생성, 타격 후 0.18초 내 무피해, 타격 후 0.85초 내 1회 피해를 통과했다. 공통 변경 이후 `Stage6PlayModeSmokeTest.RunFromCommandLine`도 NavMesh 완전 경로 5/5와 런타임 초기화를 통과했다. 로그: `ProjectDeltatime/MeleeTimingBuild.log`, `ProjectDeltatime/MeleeTimingStatic.log`, `ProjectDeltatime/MeleeTimingSmoke.log`, `ProjectDeltatime/MeleeTimingStage6Smoke.log`.
- 남은 작업: **확인 불가**. 실제 플레이에서 각 Synty 캐릭터의 손가락 그립, 방망이 방향/크기, 0.48 정규화 시점의 타격 체감은 수동 확인 후 필요하면 `ConfigureMeleeWeaponVisual`의 오프셋과 각 공격 상태의 타격 시점을 조정해야 한다.

## 2026-08-08 - 대시 방향 제자리 구르기 보정

- 변경 유형: 캐릭터 구르기 클립 보정, 플레이어 대시 시각 방향 처리, Animator 에셋 빌더 검증 강화
- 변경 내용: **구현 완료**. 원본 `Ch03_nonPBR@Stand To Roll` Humanoid 클립을 복제한 `DeltatimeRollInPlace.anim`에서 `Animator.RootT.x`와 `Animator.RootT.z` 곡선을 시작값으로 고정했다. 따라서 Root Motion을 적용하지 않는 게임플레이 캡슐과 별개로 Synty 시각 모델이 구르기 중 전진했다가 원래 위치로 되돌아오는 현상을 제거한다. `PlayerDash`는 현재 대시 방향을 공개하고, `CharacterAnimationController`는 구르기 시작 때 그 방향을 저장해 구르기 상태가 끝날 때까지 0.5초 동안 시각 루트를 실제 대시 방향으로 회전한다. 조준 방향과 좌우/후방 대시 방향이 달라도 모델은 실제 이동 방향으로 구른 뒤 조준 방향으로 복귀한다.
- 영향을 받은 시스템: 플레이어 대시 시각, 캐릭터 Animator Roll 상태, Humanoid Root Transform 곡선, Stage1·Stage3~Stage6 캐릭터 Animator 에셋
- 관련 파일: `ProjectDeltatime/Assets/_Project/Animation/DeltatimeRollInPlace.anim`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.2`로 갱신해 RootT 이동 제거, 0.5초 대시 방향 시각 정렬, 에셋 빌드 검증과 남은 수동 전이 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 `DeltatimeRollInPlace.anim` 생성과 `RootT.x/z` 상수 곡선 검증, 26명 Animator 재연결을 완료했다. 이어 `PrototypeSceneBuilder.ValidateStage1CharacterAnimationsFromCommandLine`과 `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`이 Stage1 유효 Avatar·Animator·장비 프로필 전환을 통과했다. 로그: `ProjectDeltatime/RollFixBuild.log`, `ProjectDeltatime/RollFixStatic.log`, `ProjectDeltatime/RollFixSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 루트 이동 곡선과 Animator 초기화·전환을 확인하지만, 실제 키보드/마우스로 조준을 유지한 전진·후진·좌우 대시에서 발 미끄러짐, 회전 전환, 0.5초 유지 시간이 자연스러운지는 수동 플레이로 확인해야 한다. 권총 전용 사격, 피격·사망·무기 투척/획득 애니메이션은 계속 **미구현**이다.

## 2026-08-08 - Stage1 플레이어·적 캐릭터 Animator 적용

- 변경 유형: Stage1 캐릭터 시각·Animator 적용, Prototype 빌더·정적 검증·전용 PlayMode 스모크 추가
- 변경 내용: **구현 완료**. Stage1의 기존 플레이어 1명·원거리 적 2명·근접 적 1명 캡슐 루트를 물리·전투 권한으로 유지하면서, Party Female 01·Bartender Male 01·Bouncer Male 01·Party Male 02 Synty 프리팹을 시각 자식으로 연결했다. 시각 프리팹 Collider와 Root Motion을 끄고 `CharacterAnimationController` 및 `CharacterVisualController`를 설정해 이동·구르기·지원되는 공격·장비 교체 애니메이션과 가시성·피격 색 피드백을 전달한다. 플레이어는 청록, 원거리 적은 적색, 근접 적은 주황 역할 링으로 구분한다. `PrototypeSceneBuilder`의 Stage1+Stage2 재생성 경로에서도 Stage1 저장본에만 같은 연결을 넣으며, 현재 Stage1만 갱신하는 `Tools/Prototype/Animation/Apply Characters To Stage 1` 메뉴를 추가했다.
- 영향을 받은 시스템: Stage1 플레이어·적 시각, Humanoid Animator, 비무장/권총/소총·샷건/근접 장비 프로필, 적 월드 시간 재생 속도, 피격·가시성 피드백, Prototype 씬 빌더, 정적·PlayMode 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage1CharacterAnimationPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterVisualController.cs`, `ProjectDeltatime/Assets/_Project/Animation`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.1`로 갱신해 Stage1 4명 적용, 전체 적용 수 26명, 역할 링·시각 피드백, Stage2/Tutorial 미적용 상태와 전용 검증 결과를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.ApplyStage1CharactersFromCommandLine`이 Stage1 배우 4명의 유효 Humanoid Avatar, 활성 Animator, Root Motion Off, `UnscaledTime`, 장비별 Controller, 비활성 시각 Collider, 역할 링과 `CharacterVisualController`를 검증했다. `Stage1CharacterAnimationPlayModeSmokeTest.RunFromCommandLine`은 Play Mode Animator 초기화, 필수 `MoveX`/`MoveY`/`Roll`/`AttackA`/`AttackB` 파라미터, 적 `CurrentTimeScale` 재생, 플레이어 Unarmed→Pistol→Rifle→Shotgun→Melee→Pistol 전환을 확인했다. 로그: `ProjectDeltatime/Stage1CharacterAnimationBuild.log`, `ProjectDeltatime/Stage1CharacterAnimationSmoke.log`.
- 남은 작업: **미구현/확인 불가**. Stage2와 Tutorial에는 아직 Synty 캐릭터 시각·Animator를 적용하지 않았다. 권총 전용 사격, 피격·사망·무기 투척/획득 애니메이션은 기존과 같이 미구현이다. 실제 키보드/마우스로 전후좌우 이동·구르기·각 무기 교체/공격을 반복했을 때의 방향, 전이, 발 미끄러짐, 팔·프로토타입 무기 관통은 사용자의 수동 테스트가 필요하다.

## 2026-08-08 - 플레이어·적 무기 프로필 캐릭터 애니메이션

- 변경 유형: 캐릭터 Animator 신규 구현, 애니메이션 FBX 리그·루프 설정 정규화, 씬·무기 데이터·빌더·PlayMode 검증 갱신
- 변경 내용: **부분 구현**. `Assets/Animations`의 Generic FBX를 Synty 캐릭터와 호환되는 Humanoid로 재임포트하고 이동/Idle 클립은 루프, 구르기·공격 클립은 비루프로 설정했다. 공용 `DeltatimeCharacter.controller`는 `MoveX`/`MoveY` 2D 방향 Blend Tree와 `Roll`, 교대 `AttackA`/`AttackB` 상태를 가진다. Pistol/Rifle/Melee `AnimatorOverrideController`가 비무장 기본 클립을 장비 자세로 교체하고, `WeaponDefinition.animationStyle`은 Pistol=권총, Automatic Rifle·Shotgun=소총, Melee Weapon=근접 프로필을 지정한다. `CharacterAnimationController`는 플레이어의 실제 이동과 조준 기준 로컬 방향, 적의 실제 이동 방향, 대시 시작, 무기/비무장 공격 이벤트, 장비 교체를 Animator에 전달한다. Root Motion은 기존 Rigidbody/NavMesh 코드 이동과 중복되지 않도록 끄고, 적 Animator 속도는 `WorldTimeController.CurrentTimeScale`, 플레이어는 실제 시간과 하드 프리즈를 따른다. Stage3 4명, Stage4~Stage6 각 6명으로 총 22명의 Synty 플레이어·적에 적용했다.
- 영향을 받은 시스템: 플레이어 이동·대시·공격, 적 추격·후퇴·근접/총기 공격, 장비 교체·재무장, 월드 시간, Synty 캐릭터 시각, Stage3~Stage6 저장 씬·씬 빌더, Stage6 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/Animations`, `ProjectDeltatime/Assets/_Project/Animation`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterAnimationLibrary.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationAssetBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/CharacterAnimationEditorSetup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.6.0`으로 갱신하고 애니메이션 구현 상태, 장비별 프로필, Root Motion/월드 시간 정책, Stage3~Stage6 적용 범위, 검증 결과와 미구현 클립을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `CharacterAnimationAssetBuilder.BuildAndApplyFromCommandLine`이 FBX Humanoid 임포트, 기본 Controller와 세 Override 및 Library 생성, 총 22명 씬 연결을 완료했다. 이어 확장한 `Stage6PlayModeSmokeTest.RunFromCommandLine`이 Synty 플레이어·적 6개의 유효 Humanoid Avatar, 활성 Animator, Root Motion Off, UnscaledTime 업데이트, 필수 파라미터·클립, Unarmed/Pistol/Rifle/Melee 런타임 전환을 확인했고 기존 NavMesh 완전 경로 5/5·카메라·성능 예산·리플레이 회귀와 함께 통과했다. 로그: `ProjectDeltatime/CharacterAnimationBuild.log`, `ProjectDeltatime/CharacterAnimationSmoke.log`.
- 남은 작업: **미구현/확인 불가**. 권총 팩에 전용 사격 클립이 없어 권총 사격 중에는 이동 자세를 유지한다. 피격·사망·무기 투척/획득 애니메이션과 Stage1/Stage2/Tutorial의 Synty 캐릭터 시각 적용은 미구현이다. Pistol/Rifle 팩의 이름만으로 구분한 `strafe`/`strafe (2)` 좌우 방향, 0.16초 대시와 가속 재생한 Roll의 실제 체감, 손에 든 프로토타입 무기와 팔의 관통은 수동 플레이로 확인해야 한다.

## 2026-08-08 - 샷건 14m 최대 사거리

- 변경 유형: 샷건 투사체 사거리 제한, 무기 데이터·정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition.maximumProjectileDistance`를 추가하고, `WeaponController`가 이를 펠릿별 `Projectile.Initialize`에 전달한다. `Projectile`은 매 프레임 남은 이동 가능 거리를 계산해 해당 프레임 이동과 SphereCast 거리를 모두 제한한다. 따라서 사거리 안의 벽·적 충돌은 기존처럼 먼저 명중·제거되고, 충돌이 없으면 샷건 펠릿은 총구 기준 이동거리 14m에서 명중 플래시 없이 제거된다. 권총·자동소총·근접 무기의 값은 0m이므로 기존 공용 `Projectile.prefab`의 4 월드초 수명 규칙을 유지한다.
- 영향을 받은 시스템: 샷건 펠릿 이동·충돌·제거, 일반 발사·적 무기 재사용·`DEADLINE` 준비 발사, 무기 ScriptableObject, Stage1/Stage2 무기 정의 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.7`로 갱신해 샷건 14m 제한, 충돌 우선 순서, 다른 총기의 4 월드초 fallback, 자동 검증 범위와 수동 확인 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 `Tundra build success (9.81 seconds)`와 `Stage1 and Stage2 validation passed.`를 확인했다. 빌더는 샷건의 최대 사거리 14m와 권총·자동소총의 0m fallback 값을 검증한다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`은 `Prototype play-mode smoke test passed.`로 완료했다. 로그: `ProjectDeltatime/ShotgunRangeBuild.log`, `ProjectDeltatime/ShotgunRangeSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 정의값과 컴파일을 확인하지만, 실제 조작으로 14m 직전/직후 펠릿 제거, 원거리 벽 충돌 우선순위, `DEADLINE` 준비 발사와 적이 쏜 샷건의 사거리 체감은 별도 플레이 검증이 필요하다.

## 2026-08-08 - 샷건 원형 콘 산포·플레이어 반동 리팩터링

- 변경 유형: 샷건 탄도 패턴·플레이어 이동 반동 리팩터링, 무기 데이터·빌더 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponSpreadPattern`이 기존 `WeaponController`의 좌우 팬/축별 회전 계산을 대체한다. 다중 펠릿은 원형 콘 단면을 `sqrt` 반경으로 채워 면적 밀도를 균등하게 하고, 무기 시드·발사 순번으로 전체 패턴을 결정적으로 회전한다. 샷건은 8펠릿·총 퍼짐 18도(반각 9도) 안에서 펠릿별 최대 1도 반경 지터를 적용하므로 좌우 부채꼴이 아닌 발사축 중심의 원형 콘으로 퍼진다. `WeaponDefinition.playerRecoilDistance`를 추가해 샷건만 0.35m 후방 이동 반동을 사용하며 권총·자동소총·근접 무기는 0m다. `PlayerCombat`은 실제 플레이어 총기 발사 때만 반동을 대기시키고, `DEADLINE` 준비 발사는 해제 뒤에 대기 반동을 적용한다. 적 사격에는 플레이어 반동을 적용하지 않는다.
- 영향을 받은 시스템: 샷건·권총·자동소총 공용 탄도 산포, 플레이어 샷건 이동 반동, `DEADLINE` 준비/해제 발사, Stage1/Stage2 무기 데이터 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponSpreadPattern.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.6`으로 갱신해 원형 콘 산포 규칙, 샷건 반동 값과 적용 범위, 빌더 검증, 자동 검증 결과 및 수동 확인 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`을 실행해 `Tundra build success (6.59 seconds)`와 `Stage1 and Stage2 validation passed.`를 확인했다. 빌더는 샷건 8펠릿이 반각 9도 이내에 있고 수평·수직 양방향으로 분포하며 동일 입력에서 결정적인지 확인한다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`은 `Prototype play-mode smoke test passed.`로 완료했다. 로그: `ProjectDeltatime/ShotgunSpreadBuild2.log`, `ProjectDeltatime/ShotgunSpreadSmoke.log`.
- 남은 작업: **확인 불가**. 자동 검증은 산포 수학·통합 전투 흐름을 확인하지만, 실제 조작으로 0.35m 반동의 체감과 벽/경사면 근처의 이동 제한, 다양한 거리에서의 원형 펠릿 명중 분포는 별도 수동 플레이 검증이 필요하다.

## 2026-08-08 - Tutorial 공중 무기 회수 DEADLINE 진행·무제한 시야

- 변경 유형: Tutorial 진행 조건·시야 정책 개선, HUD 안내·씬 구성·PlayMode 회귀 검사·문서 갱신
- 변경 내용: **구현 완료**. 투척 수업 적의 기절·무장 해제·공중 드롭이 확인된 뒤 플레이어가 공중 `InterceptableWeapon`을 E로 잡아 어떤 무기든 보유하면, `TutorialDirector`가 즉시 `DeadlineApproach`로 진행하고 투척 수업 적을 비활성화한다. 따라서 DEADLINE 앞 Pistol 지급기는 무기를 놓친 경우의 보조 수단일 뿐 진행 필수 조건이 아니다. Tutorial의 `VisionCone`은 무제한 시야 모드로 설정되어 적 가시성 판정이 시야각·거리·장애물에 제한되지 않으며, 시야 부채꼴 오버레이와 런타임 시야 조명도 비활성화한다.
- 영향을 받은 시스템: Tutorial 투척·공중 무기 회수·DEADLINE 진입, Tutorial HUD, 적 가시성, 시야 오버레이·조명, Tutorial 씬 빌더, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.5`로 갱신해 공중 무기 회수 기반 DEADLINE 진행과 Tutorial 무제한 시야 정책을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 적의 실제 공중 `InterceptableWeapon`을 회수한 뒤 `DeadlineApproach`로 진행하는지와 Tutorial 시야 오버레이 비활성·시야 제한 밖 점의 가시성을 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: 실제 키보드/마우스로 적의 공중 무기를 E로 가로챈 직후 DEADLINE 안내·게이트가 진행되는지, 포위전 시작 전후에도 모든 적과 공간이 시야 제한 없이 보이는지 수동 확인해야 한다. 최종 입력·시각 체감은 **확인 불가**다.

## 2026-08-08 - Tutorial DEADLINE 사망 체크포인트 재시작

- 변경 유형: Tutorial 사망 재시작 흐름 개선, DEADLINE 전투 상태 복구, HUD·PlayMode 회귀 검사·문서 갱신
- 변경 내용: **구현 완료**. `TutorialDirector`는 DEADLINE 단계에서 플레이어가 사망한 상태로 R을 누르면 체크포인트 요청을 유지한 채 Tutorial 씬을 다시 로드한다. 새 `TutorialDirector`는 요청을 한 번 소비해 DEADLINE 단계로 즉시 복귀시키고, 플레이어 기본 체력, 원래 위치의 적 4명, 최대 탄약 Pistol, 최대 DEADLINE 충전, 닫힌 출구 게이트를 복구한다. DEADLINE 이외 구간의 사망과 생존 중 R은 기존처럼 Tutorial 첫 단계부터 다시 시작한다. 사망 HUD도 DEADLINE 전용 재시작 문구로 바뀐다.
- 영향을 받은 시스템: Tutorial 사망/R 재시작, DEADLINE 포위전 상태, 플레이어 무기·탄약, 적 배치, 게이트, Tutorial HUD, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.4`로 갱신해 DEADLINE 사망 시 체크포인트 재시작 범위와 자동 검증 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 최신 코드 기준으로 실행했다. 스모크는 권총을 비운 상태에서 DEADLINE 체크포인트 복구를 호출하고 DEADLINE 단계, 최대 충전, 최대 탄약 Pistol, 리셋 지점, 닫힌 출구를 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 플레이어 사망 뒤 R 입력이 씬을 다시 로드하고 해당 체크포인트를 소비하는 전체 입력 경로는 수동 확인이 필요하다. 따라서 실제 전투 체감과 사망 화면 전환은 **확인 불가**다.

## 2026-08-08 - Tutorial 게이트 소거·투척 수업 사살 방지·Pistol 회수 경로 수정

- 변경 유형: Tutorial 진행 막힘·가시성 버그 수정, 적 피해 정책 보강, PlayMode 회귀 검사 확장, 문서 갱신
- 변경 내용: **구현 완료**. 열린 `TutorialGate`는 Collider가 즉시 꺼진 뒤 상승 애니메이션의 목적지에 도달하면 Renderer도 비활성화돼 화면에서 사라진다. `TutorialDirector`는 투척 수업 적의 `EnemyHealth` 피해를 비활성화하므로 LMB Pistol 사격으로 적이 파괴되어 수업이 막히지 않는다. 안내 문구는 LMB 사격 대신 RMB Pistol 투척을 명시한다. 무기 드롭 이벤트와 생존·기절·무장 해제·무기 없음 상태가 모두 확인되면 Gate 5 - Arena Entrance를 즉시 열어 Gate 너머 Pistol 지급기 때문에 발생하던 순환 진행 조건을 제거한다.
- 영향을 받은 시스템: Tutorial 게이트 Renderer/Collider, 투척 수업 적 피해·기절·무장 해제, Pistol 회수 동선, Tutorial 안내 문구, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialGate.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.3`으로 갱신해 게이트 소거, 투척 수업 사살 방지, Gate 5 즉시 개방 규칙과 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 열린 Gate 6 Renderer 소거, 투척 수업 적의 사살 방지, 기절·무장 해제·드롭 뒤 Gate 5 개방을 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 열린 게이트의 시각적 소거, LMB 사격, RMB 투척 뒤 Gate 5 개방·Pistol 회수·DEADLINE 진입 체감을 확인하지 않았다. 최종 수동 진행 결과는 **확인 불가**다.

## 2026-08-08 - Tutorial 게이트 초기화 순서·Pistol 경로 차단 수정

- 변경 유형: 진행 경로 버그 수정, 런타임 게이트 위치 회귀 검사 추가, 문서 갱신
- 변경 내용: **구현 완료**. `TutorialDirector`의 초기 상태 적용이 `TutorialGate.Awake`보다 먼저 실행될 수 있어, 게이트가 닫힌 기준 위치를 기록하기 전에 원점으로 이동하던 문제를 수정했다. `TutorialGate.SetOpen`은 최초 호출에서 현재 로컬 좌표를 닫힌 기준으로 먼저 저장한다. 따라서 Gate 3 - Melee(`z = -1`)를 포함한 여섯 게이트가 중앙 통로에 겹치지 않으며, 근접 표적 적중 후 열린 Gate 3을 지나 Pistol 지급 위치(`z = 3`)로 이동할 수 있다.
- 영향을 받은 시스템: Tutorial 게이트 초기화, 근접→Pistol 진행 경로, 런타임 Collider 위치, Tutorial PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialGate.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.2`로 갱신해 게이트 좌표 보존 규칙과 여섯 게이트 Z 좌표 회귀 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. 스모크는 여섯 게이트의 원래 Z 좌표와 열린 Gate 6의 Renderer 소거를 확인했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Gate 3을 통과해 Pistol 지급 위치까지 이동하는 체감은 확인하지 않았다. 최종 수동 동선 결과는 **확인 불가**다.

## 2026-08-08 - Tutorial 대시 출구 판정·Pistol 즉시 지급 보정

- 변경 유형: 진행 판정 버그 수정, 무기 지급 안정화, HUD 피드백·PlayMode 자동 검증 보강, 문서 갱신
- 변경 내용: **구현 완료**. 대시 출구는 조준 회전 목표를 채운 뒤 발생한 `PlayerDash.IsDashing`을 기록하고, 플레이어가 출구 트리거를 통과할 때 이 기록을 사용해 다음 단계로 진행한다. 대시가 0.16초 후 끝나 트리거 진입 프레임에는 `IsDashing == false`가 된 경우에도 이전 성공 대시가 무효화되지 않는다. `TutorialWeaponDispenser.SetAvailable(true)`는 다음 Update를 기다리지 않고 즉시 Pistol 픽업을 생성하며, Tutorial HUD 진행 문구는 `Pistol 생성됨`·`Pistol 장비 완료`를 표시한다.
- 영향을 받은 시스템: Tutorial 조준/대시 게이트, 플레이어 트리거 진행, Pistol 지급·픽업, Tutorial HUD, PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialWeaponDispenser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.1`로 갱신해 대시 기록 기반 출구 판정, Pistol 즉시 생성/HUD 상태, 자동 검증과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`과 `TutorialPlayModeSmokeTest.RunFromCommandLine`을 실행했다. PlayMode 스모크는 Pistol 지급기를 활성화한 직후 생성된 픽업이 Pistol 정의를 가지는지 확인하고, 기존 월드 시간·무기 타입·투척 기절/드롭·Q `DEADLINE` 행동 2개 제한·이동 해제 회귀도 통과했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 대시 출구를 다양한 타이밍·방향에서 넘고, Pistol 생성 위치와 HUD 문구가 처음 플레이하는 사용자에게 충분히 눈에 띄는지 확인하지 않았다. 따라서 최종 입력 체감과 가시성은 **확인 불가**다.

## 2026-08-08 - 핵심 메커니즘 순차 Tutorial 씬

- 변경 유형: 신규 튜토리얼 씬·런타임 진행 시스템·HUD·전용 NavMesh·빌드 진입점·정적/PlayMode 자동 검증·기존 빌더 빌드 순서 호환·문서 갱신
- 변경 내용: **구현 완료**. 빌드 인덱스 0의 `Tutorial` 씬을 추가했다. 7단계 직선형 코스가 실제 행동 결과를 기준으로 이동/정지 월드 시간, 마우스 조준/Space 대시, E 근접 무기/LMB 적중, E Pistol/LMB 적중, RMB 투척으로 적 기절·무장 해제·공중 드롭 후 Pistol 회복, 적 4명이 사방에서 포위한 Q `DEADLINE`의 원인 2개 준비와 이동 해제, 북쪽 출구 탈출을 순서대로 해제한다. 실패한 `DEADLINE` 시도는 적·플레이어 위치와 충전을 복구하고, 성공 출구 통과 후 전투를 잠근 뒤 2초 후 Stage1을 로드한다. 사망 시 R로 Tutorial을 재시작한다. 본편 전멸 리플레이가 자체 탈출 완료를 가로채지 않도록 Tutorial의 `StageController`와 레거시 `GameHud`는 제거하고 `VisionCone` 의존성용 리플레이 컴포넌트만 보존했다. `TutorialHud`는 한국어 단계 지시·판정 진행도·월드 배율·무기/탄약·충전을 표시한다. 전역 `Time.timeScale`은 변경하지 않으며 회전 프로브·적·투사체 등 월드 진행은 기존 `WorldDeltaTime` 정책을 유지한다. `EnemyWeaponDrop`에는 드롭 결과 이벤트, `DeadlineController`에는 비활성 상태의 튜토리얼 재시도용 충전 복구 API를 추가했다.
- 영향을 받은 시스템: 플레이어 입력/이동/조준/대시/전투, 월드 시간, 무기 픽업·지급·투척, 적 기절·무장 해제·드롭, `DEADLINE`, 한국어 IMGUI HUD, 게이트/트리거 진행, NavMesh, 빌드 설정, Stage1 전환, Prototype 및 Stage3~Stage6 씬 빌더
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`, `ProjectDeltatime/Assets/_Project/Scenes/TutorialNavigation.asset`, 해당 `.meta`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/*`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/TutorialPlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.5.0`으로 갱신해 Tutorial 구현/검증 상태, 7단계 진행, 4인 포위 `DEADLINE` 연출, 씬·전환 흐름, 조작/UI/기술 클래스, 빌드 순서, 남은 사용자 검증 항목을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `TutorialSceneBuilder.BuildAndValidateFromCommandLine`으로 직접 참조, 게이트 6개, 트리거 3개, 타입 표적 2개, 지급기 3개, 적 5명, 전용 `TutorialNavigation.asset`, 활성 카메라 1대, Layer 8 장애물, 무장 없는 시작, `Tutorial → Stage1 → … → Stage6` 빌드 순서를 검증했다. `TutorialPlayModeSmokeTest.RunFromCommandLine`은 이동/정지 월드 배율과 `WorldDeltaTime` 프로브, 근접/총기 타입 판정, 투척 기절·무장 해제·드롭, Q 바인딩, `DEADLINE` 발동·행동 2개 제한·이동 해제, `Time.timeScale == 1`을 통과했다. `Stage6SceneBuilder.ValidateStage1Through5RegressionFromCommandLine`도 Stage1~Stage5 읽기 전용 회귀를 통과했다. 로그: `ProjectDeltatime/TutorialBuild.log`, `ProjectDeltatime/TutorialSmoke.log`, `ProjectDeltatime/TutorialStageRegression.log`.
- 남은 작업: **미실행**. 사람이 처음부터 끝까지 키보드/마우스로 진행하며 각 게이트 판정 여유, 한국어 문구 가독성, 투척 무기 재획득 동선, 4인 포위전 난이도와 실패 재시도, 완료 후 Stage1 전환 연출을 확인하지 않았다. 따라서 최종 온보딩 난이도와 시각·조작 체감은 **확인 불가**다. 공중 가로채기는 안내 문구와 기존 시스템을 유지하지만 별도 필수 튜토리얼 판정 단계는 **미구현**이다.

## 2026-08-08 - Stage5 전경 Collider 조준 간섭 제거

- 변경 유형: 플레이어 조준 버그 수정, Stage5 컷어웨이 상호작용 보정, PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. `PlayerAim`은 카메라 포인터 광선의 Physics Raycast를 제거하고 플레이어 Rigidbody의 현재 Y 높이 수평 평면에 직접 투영한다. Stage5 화면 하단의 전경 가구·외벽은 `Stage5SouthExteriorCutaway`가 필요할 때 Renderer만 `ShadowsOnly`로 숨기며, Collider와 Layer 8 `VisionObstacle`은 계속 유지한다. 따라서 숨은 가구 Collider가 카메라 광선에 먼저 맞아 플레이어가 엉뚱한 방향을 바라보던 문제가 발생하지 않는다. 투사체·근접 판정·적 시야의 기존 Physics Raycast는 변경하지 않았다.
- 영향을 받은 시스템: 모든 스테이지의 마우스 조준·플레이어 회전·총구 기준 수평 발사 방향, Stage5 전경 컷어웨이, 충돌·적 시야 보존, Stage5 PlayMode 스모크
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.8`로 갱신해 마우스 조준을 배우 현재 높이 평면 투영으로 명시하고, 2026-08-03의 물리 표면 조준 규칙이 이 변경으로 대체됐음을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5PlayModeSmokeTest.RunFromCommandLine`을 실행해 Stage5 초기화·가구 상면 NavMesh 제외·경로·높이 이동·컷어웨이와 함께, 카메라 광선을 가로막는 임시 Collider가 있어도 조준점이 플레이어 높이 평면에 남는 회귀 검증을 통과했다. 로그: `ProjectDeltatime/Stage5AimForegroundSmoke.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Stage5 화면 하단의 가구·외벽 뒤를 조준할 때의 회전·총알 충돌·카메라 선행 체감과 Stage1~Stage6 전체 조준 체감은 자동화 범위 밖이므로 **확인 불가**다.

## 2026-08-08 - Stage5·Stage6 가구 상면 NavMesh 제외

- 변경 유형: NavMesh 베이크 버그 수정, 적 이동 경로 보정, 카메라 고도 경계 보정, 정적·PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. Stage5·Stage6 빌더는 활성 환경 Collider 중 테이블·의자·스툴·소파·부스·바/카운터·냉장고·선반·캐비닛·책상·화분·기둥·소품 등 보행 상면을 만들 수 있는 가구 소스에만 베이크 중 일시 `NavMeshModifier(area = Not Walkable, applyToChildren = false)`를 적용하고, 베이크 직후 모두 제거한다. 따라서 바닥과 의도된 계단/스텝은 유지하면서 테이블·의자 등의 상면에는 NavMesh가 생성되지 않는다. 환경 Physics Collider와 Layer 8 `VisionObstacle` 구성은 보존한다. 빌더 정적 검증과 PlayMode 스모크는 대상 가구 Collider 상단 중심에 NavMesh를 샘플할 수 없음을 확인한다. Stage6의 가구 제외 후 달라진 후보 분포에서 카메라 밖 스폰을 막기 위해 플레이어 시작 후보를 NavMesh 외곽에서 3m 안쪽으로 제한했다. 또한 `TopDownCameraController`는 Y 범위가 1m 이상인 다층 NavMesh에서 현재 포커스 고도로 화면 발자국을 계산해 상단 플랫폼의 플레이어가 화면 밖으로 밀리지 않게 했으며, 낮은 Y 범위의 Stage5는 기존 공통 평면 경계 계산을 유지한다.
- 영향을 받은 시스템: Stage5·Stage6 NavMesh 베이크와 저장 에셋, 적 추격/경로 탐색, 플레이어·적 높이 이동, 가구 충돌·시야 장애물 보존, Stage6 역할 스폰, Stage5·Stage6 탑다운 카메라 경계, 정적 검증, 전용 PlayMode 스모크. Stage1~Stage4의 NavMesh 및 카메라 경계 기본값은 변경하지 않았다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.7`로 갱신해 Stage5·Stage6 가구 상면 NavMesh 제외 정책, 임시 Modifier 제거, 충돌/시야 보존, Stage6 다층 카메라 고도 경계와 가구 상면 회귀 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 가구 소스 제외 구성을 적용한 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 실행해 가구 상단 NavMesh 부재, 6명 구성 및 완전 경로를 검증했다. 이후 최종 `TopDownCameraController` 고도 범위 보정 코드는 Unity 컴파일을 포함한 `Stage5PlayModeSmokeTest.RunFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`으로 실행해 가구 상단 샘플, 계단/플랫폼 높이 이동, 실제 Rigidbody 물리 이동, 카메라 경계, 적 경로를 함께 통과했다. 로그: `ProjectDeltatime/Stage5FurnitureNavMeshBuild.log`, `ProjectDeltatime/Stage6FurnitureNavMeshBuild.log`, `ProjectDeltatime/Stage5FurnitureNavMeshSmoke.log`, `ProjectDeltatime/Stage6FurnitureNavMeshSmoke.log`.
- 남은 작업: **미실행**. 에디터 NavMesh 시각화와 실제 키보드/마우스 조작으로 모든 가구 유형 주변의 장시간 적 추격·회피, 테이블·의자 사이의 경로 체감, 다양한 종횡비의 카메라 체감은 자동화 범위 밖이므로 **확인 불가**다.

## 2026-08-07 - Stage5·Stage6 NavMesh Rigidbody 바닥 간격 보존

- 변경 유형: 버그 수정, 이동 투영 API 보강, PlayMode 스모크 회귀 검사, 문서 갱신
- 변경 내용: **구현 완료**. `NavMeshGroundMovement`가 최초 유효 NavMesh 샘플에서 Rigidbody 루트와 바닥 표면의 Y 간격을 런타임에 한 번 저장하고, 이후 일반 이동·대시·적 추격의 투영 목표 Y에 더하도록 수정했다. 비활성화/재활성화하면 간격을 다시 캡처한다. 따라서 계단·단상 NavMesh 표면 좌표를 캡슐 중심에 직접 적용해 플레이어가 바닥에 관통하고 물리 보정으로 떨리던 문제가 해소된다. 기존 `TryProjectDisplacement`는 바닥 표면 좌표를 반환하는 의미를 유지하며, `TryProjectRigidbodyDisplacement`는 보정된 루트 목표를 제공한다.
- 영향을 받은 시스템: Stage5·Stage6 플레이어 일반 이동·대시, 적 NavMesh 추격, 동적 Rigidbody 캡슐의 바닥 접촉, 계단·단상 높이차 이동, PlayMode 물리 회귀 검증. `NavMeshGroundMovement`가 없는 Stage1~Stage4의 평면 이동은 변경하지 않았다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Level/NavMeshGroundMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.6`으로 갱신해 루트-NavMesh 표면 간격 보존, 비활성화 후 재캡처, 표면/루트 투영 API 구분, 실제 Rigidbody 물리 프레임 검증을 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`, `Stage5PlayModeSmokeTest.RunFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `Stage5SceneBuilder.ValidateStage1Through4RegressionFromCommandLine`, `Stage6SceneBuilder.ValidateStage1Through5RegressionFromCommandLine`을 변경 후 실행했다. Stage5·Stage6 스모크는 국소 NavMesh 이동 목표가 초기 루트-바닥 간격을 유지하는지, 실제 Rigidbody 이동 뒤 고정 물리 프레임에서 수평 이동·루트 간격·캡슐 하단의 바닥 비관통이 유지되는지를 확인해 통과했다. 로그: `ProjectDeltatime/Stage5GroundMovementBuild.log`, `ProjectDeltatime/Stage6GroundMovementBuild.log`, `ProjectDeltatime/Stage5GroundMovementFinalSmoke.log`, `ProjectDeltatime/Stage6GroundMovementFinalSmoke.log`, `ProjectDeltatime/Stage1Through4GroundMovementRegression.log`, `ProjectDeltatime/Stage1Through5GroundMovementRegression.log`.
- 남은 작업: **미실행**. 실제 키보드/마우스로 Stage5·Stage6의 일반 이동·대시·적 추격을 계단/단상에서 장시간 반복하는 수동 플레이와 다양한 화면 비율 체감 검증은 자동화 범위 밖이다. 따라서 장시간 조작 감각은 **확인 불가**다.

## 2026-08-07 - Stage5·Stage6 시야 방해·높이차 이동·카메라·배경 최적화

- 변경 유형: Stage5 전경 컷어웨이·Stage5/Stage6 NavMesh 높이차 이동·Stage6 카메라 근접 구도·화면 밖 차량 파티클 비활성·빌더/스모크·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. 공용 선택형 `NavMeshGroundMovement`를 추가해 Stage5·Stage6에만 연결된 플레이어 1명과 적 5명이 NavMesh 구간을 따라 XZ와 Y를 함께 이동하도록 했다. 대시와 적 추격도 같은 보정을 사용하며, 계단 고도 단차에서는 완전 NavMesh 경로의 다음 코너만 허용한다. 두 빌더는 NavMesh 베이크 뒤 실제 계단/스텝 콜라이더를 런타임 이동 차단에서만 해제하고, Rigidbody의 Y 고정은 풀되 중력은 끈다. 비활성화 수는 Stage5 `6`, Stage6 `16`이다. `Stage5SouthExteriorCutaway`는 남쪽 외벽뿐 아니라 카메라→플레이어 선분을 실제로 가리는 전경 테이블·의자·소품 Renderer만 `ShadowsOnly`로 전환하고, 가림이 해소되면 원래 Renderer 상태를 복원한다. Collider, Layer 8 `VisionObstacle`, NavMesh, 조명은 유지한다. Stage6 카메라는 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 조준 선행 `1.25`, FOV `48`, 주 연결 전투 NavMesh XZ 경계 제한으로 Stage5와 통일했다. 고도 이동 중에는 현재 NavMesh 높이를 반영해 화면 경계를 계산한다. `Background_FX`의 `FX_Background_Cars_01` 8개는 복제한 Stage6 씬에서 비활성화해 렌더링·시뮬레이션·업데이트를 중단하고, 원본 데모 및 `BackgroundCity` 계층은 보존한다.
- 영향을 받은 시스템: Stage5 남쪽/전경 가시성, Stage5·Stage6 플레이어 일반 이동·대시·적 추격, Rigidbody 제약, 계단·단상·플랫폼 NavMesh 경로, 탑다운 카메라 경계·높이 추적, Stage6 배경 파티클, Stage5/Stage6 빌더·정적 검증·플레이 모드 스모크·미리보기. `NavMeshGroundMovement`를 연결하지 않는 Stage1~Stage4는 기존 평면 이동을 유지한다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Level/NavMeshGroundMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 `1.4.5`로 갱신해 Stage5 전경 컷어웨이, 양 스테이지의 계단·단상·플랫폼 높이 이동, 실제 Stage6 카메라 직렬화 값과 NavMesh 경계, 배경 차량 8개 비활성, 자동·수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`과 `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 최종 구성으로 실행해 각각 계단/스텝 Collider `6`·`16`개, 플레이어·적 `6`개의 높이 이동 구성, Stage6 FOV `48`, 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 경계 제한 On, 차량 `8`개 비활성을 정적으로 확인했다. `Stage5PlayModeSmokeTest.RunFromCommandLine`과 `Stage6PlayModeSmokeTest.RunFromCommandLine`은 숨김 Unity 에디터에서 실행해 계단 상·하단의 완전 경로·Y 이동, Stage5 남쪽/전경 컷어웨이의 Renderer 전환과 Collider·VisionObstacle 보존, 실제 도달 가능한 네 방향 NavMesh 경계의 카메라·플레이어 가시성, Stage6 차량 비활성을 확인해 통과했다. `ValidateStage1Through4RegressionFromCommandLine`과 `ValidateStage1Through5RegressionFromCommandLine`도 통과했다. `Stage5Preview.png`, `Stage6Preview.png`는 최종 씬 기준 1280×720으로 재생성해 근접 구도, 식별 원, 차량 미노출을 직접 검토했다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 양 스테이지의 일반 이동·대시·적 추격을 계단/단상 상·하단에서 장시간 왕복하고, Stage5 남쪽 전경 컷어웨이와 Stage6 근접 구도·차량 미노출을 1280×720 외 화면 비율에서도 확인해야 한다. 따라서 최종 조작 감각과 극단적 화면 비율의 연출은 **확인 불가**다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-07 - Stage5 메인 홀 정리·남쪽 외벽 컷어웨이

- 변경 유형: Stage5 환경 큐레이션·오른쪽 별관 제외·전용 NavMesh/카메라 재생성·남쪽 외벽 런타임 가시성·스모크·미리보기·문서 갱신
- 변경 내용: **구현 완료**. `Stage5SceneBuilder`가 공식 다이브 바 사본을 만든 직후 오른쪽 별관(`x ≥ 5`, `z ≥ -2.5`)의 프리팹·렌더러·국소 조명·반사 프로브를 비활성화하고 메인 홀 동쪽 경계 벽은 유지한다. NavMesh 수집 볼륨도 경계 서쪽으로 잘라 별관 콜라이더가 남아 있어도 플레이 영역에 포함되지 않게 했다. 가구는 정확히 테이블 7개와 테이블당 가까운 좌석 2개, 바 스툴 4개만 활성화해 총 좌석 18개로 고정한다. 새 `Stage5SouthExteriorCutaway`는 남쪽 NavMesh 경계에서 3.00m 안쪽에 들어오면 전면 외벽 렌더러를 `ShadowsOnly`로 전환하고 3.75m 안쪽으로 복귀하면 원래 그림자 모드를 복원한다. 이 동작은 Collider와 Layer 8 `VisionObstacle`을 변경하지 않는다. 새 메인 홀 NavMesh는 중심 `(-2.42, 0.63, 0.00)`, 크기 `(13.83, 1.08, 23.67)`이며, Stage5 카메라는 FOV `48`, 오프셋 `(0, 11.12, -6.10)`, 포커스 `(0, 0, 1.42)`, 조준 선행 `1.25`로 다시 직렬화됐다.
- 영향을 받은 시스템: Stage5 다이브 바 환경 렌더링·가구 밀도·조명·콜라이더·시야 장애물·NavMesh·카메라 경계·남쪽 경계 가시성·플레이어/적 경로·에디터 빌더·플레이 모드 스모크·미리보기. Stage1~Stage4와 Stage6의 런타임 환경/카메라/바닥 원 외형은 유지한다.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/Stage5SouthExteriorCutaway.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.4.4로 갱신해 Stage5의 메인 홀 전용 환경 정책, 테이블 7개·좌석 18개, 별관 제외 범위, NavMesh·카메라 실제 직렬화 값, 남쪽 외벽 컷어웨이 임계값과 충돌/시야 보존, 자동 검증과 수동 확인 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 2회 실행해 종료 코드 0과 동일한 가구 수·별관 활성 구성·NavMesh·카메라 검증을 확인했다. 정적 검증은 활성 테이블 7개·좌석 18개, 별관 내부 활성 Renderer/Light/Collider 0개, 동쪽 경계 벽 유지, 메인 홀 NavMesh, 플레이어→적 완전 경로 5/5, 남쪽 컷어웨이 직렬화 구성을 검사한다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 동·서·남·북 카메라 경계와 함께 남쪽 접근 시 외벽 `ShadowsOnly`, 복귀 시 원래 모드 복원, VisionObstacle 콜라이더 수 불변을 확인해 통과했다. `ValidateStage1Through4RegressionFromCommandLine`, `Stage6SceneBuilder.BuildAndValidateFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `ValidateStage1Through5RegressionFromCommandLine`도 종료 코드 0으로 통과했다. 1280×720 `Stage5Preview.png`를 재생성해 메인 홀 가구 밀도와 별관 미노출을 직접 확인했다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 남쪽 경계까지 이동하며 외벽 컷어웨이 전환 시점, 장시간 조준·사격·픽업·투척·`DEADLINE` 중 가시성, 모든 종횡비의 카메라 체감은 아직 확인하지 않았다. 따라서 최종 조작 감각은 **확인 불가**다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-07 - Stage5 카메라 경계·전투 식별 표시 개선

- 변경 유형: Stage5 카메라 프레이밍·NavMesh 기반 화면 경계·전투 식별 바닥 원 렌더링·Stage5/Stage6 빌더 및 스모크·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. `TopDownCameraController`에 기본 비활성인 `constrainToBounds`와 `cameraBounds` 직렬화 설정을 추가했다. 일반 추적과 `SnapToTarget`은 플레이어 위치·전방 포커스·조준 선행을 합친 최종 포커스에 같은 제한 계산을 사용한다. 제한 계산은 현재 해상도 종횡비·카메라 FOV·회전에서 화면 네 모서리를 경계 지면에 투영해 XZ 범위를 구하고, 한 축의 화면 폭이 저장 경계보다 크면 그 축을 중앙에 고정한다. Stage5 빌더는 실제 NavMesh 깊이에서 높이 `깊이×0.47`, 후방 거리 `(높이-0.55)/tan(60도)`, 전방 포커스 `min(깊이×0.06, 1.5)`를 계산한다. 최종 직렬화 값은 FOV `48`, 오프셋 `(0, 11.4367, -6.2854)`, 포커스 `(0, 0, 1.46)`, 조준 선행 `1.25`, 약 60도 하향각이다. XZ 카메라 경계는 실제 NavMesh AABB와 같은 중심 `(0, -0.3333)`, 크기 `(18.6667, 24.3333)`이다. 플레이어 청록색·원거리 적 적색·추적형 적 주황색의 Stage5 전용 `Unlit/Color` 머티리얼 3개를 생성하고 여섯 바닥 원의 그림자 투사·수신, 라이트 프로브, 반사 프로브를 껐다. 깊이 판정은 유지하므로 벽과 가구에는 정상적으로 가려진다. Stage1~4의 새 경계 설정 기본값은 비활성이며, `Stage6SceneBuilder`는 Stage5에서 옮긴 카메라 제한을 끄고 기존 Stage6 역할별 링 머티리얼과 그림자 Off·프로브 Blend·모션 Object 설정을 복원한다.
- 영향을 받은 시스템: Stage5 Main Camera·탑다운 추적·현재 화면 지면 투영·NavMesh 외곽 카메라 제약·플레이어/적 역할 식별 렌더링·그림자/프로브 설정·Stage5/Stage6 씬 자동 생성·정적 및 플레이 모드 검증·Stage5 미리보기. 공개 API와 Stage1~4/Stage6 런타임 카메라·표시 외형은 유지.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Materials/Stage5PlayerMarker.mat`, `ProjectDeltatime/Assets/_Project/Materials/Stage5RangedEnemyMarker.mat`, `ProjectDeltatime/Assets/_Project/Materials/Stage5ChaserEnemyMarker.mat`, 각 머티리얼 `.meta`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/LightingData.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-0.exr`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-1.exr`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/ReflectionProbe-2.exr`, 해당 생성 에셋의 `.meta`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.4.3으로 갱신해 Stage5 전용 선택형 화면 경계, FOV·오프셋·포커스·조준 선행·실제 XZ 경계 값, 역할별 Unlit 바닥 표시와 일반 깊이 가림, Stage6 복원 정책, 자동/수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1에서 최신 스크립트 컴파일과 최종 구성의 `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 두 번 실행해 두 실행 모두 종료 코드 0과 같은 카메라·경계·여섯 링 설정 검증을 확인했다. 정적 검증은 16:9 화면 네 모서리 지면 투영, 플레이어 viewport 잔존, 여섯 링의 에셋 경로·`Unlit/Color`·역할 색상·그림자/프로브 비활성을 포함한다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 플레이어를 NavMesh 동·서·남·북에서 캐릭터 반경 0.5m 안쪽의 실제 도달 가능한 가장자리로 임시 이동시킨 뒤 카메라 지면 범위가 경계 안에 남고 플레이어 중심이 화면에 보이는지 확인하고 원래 위치를 복원했으며 종료 코드 0으로 통과했다. `ValidateStage1Through4RegressionFromCommandLine`, Stage6 `BuildAndValidateFromCommandLine`, `Stage6PlayModeSmokeTest.RunFromCommandLine`, `ValidateStage1Through5RegressionFromCommandLine`도 각각 종료 코드 0으로 통과했다. Stage6 회귀 빌드는 렌더러 2,081, 최상위 프리팹 1,922, 콜라이더 1,017, `VisionObstacle` 277, NavMesh 정점 1,532/인덱스 2,064를 보존했다. 1280×720 `Stage5Preview.png`를 재생성해 확대된 캐릭터, 약 60도 구도, 청록/적색/주황 Unlit 원의 조명 독립 가독성, 정상 환경 가림과 외부 배경 노출 억제를 직접 확인했다. Stage6 회귀 빌드가 해당 씬을 다시 저장하면서 전용 LightingData와 반사 프로브 생성 산출물도 함께 직렬화됐다.
- 남은 작업: **미실행**. 실제 키보드/마우스로 장시간 이동·조준·사격·픽업·투척·`DEADLINE`을 플레이하며 모든 임의 종횡비에서 카메라 경계의 체감과 표시 가독성을 확인하지 않았다. 따라서 16:9 외 극단적 화면 비율의 최종 연출은 **확인 불가**다. Synty 이동·조준·사격·근접·피격·사망 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환·결과 화면은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook` 60 FPS 전용 최적화

- 변경 유형: Stage6 런타임 그림자 예산·리플레이 Renderer 탐색 제한·플레이 모드 스모크·300프레임 성능 벤치마크·미리보기·문서 갱신
- 변경 내용: **부분 구현**. `Systems`에 연결되는 `Stage6PerformanceController`를 추가했다. 저장된 공식 Rooftop 데모·프리팹·도시/조명 계층은 수정하지 않으며, Stage6 실행 중에만 `QualitySettings` 그림자 거리를 40m로 설정하고 cascade를 최대 2, 그림자 해상도를 Medium 이하로 제한한 뒤 씬 종료 시 원래 값을 복원한다. `BackgroundCity`와 그 하위 `Background_FX`/`Background_Planes` Renderer는 계속 렌더링하면서 그림자 투사·수신만 끄며, 원래 그림자가 있던 환경 Point Light 중 플레이어에 가까운 최대 2개만 0.25초마다 원래 Shadow 유형을 유지한다. 환경 포인트 라이트의 색·강도·범위·활성 상태와 반사 프로브·Global Volume·Fog·Skybox·두 Roof Layer는 그대로다. 플레이어 시야 Spot/근거리 Point Light 2개의 Soft Shadow도 유지한다. `StageReplayController`에는 opt-in 동적 루트 탐색을 추가해 기본 Stage1~5는 기존 20Hz 전수 Renderer 탐색을 유지하고, Stage6만 `Systems`, Player, 적 5, Pickup 2의 9개 직렬화 루트를 20Hz에 탐색한다. 비루트 투사체·투척 무기·드롭 Pickup은 0.25초 fallback 전수 탐색으로 등록하며 `ReplayExcluded` 정적 환경은 즉시 제외한다. `Stage6PerformanceBenchmark`는 워밍업 90프레임 뒤 300프레임 CPU/GPU 평균·p95와 구성 수를 기록한다.
- 영향을 받은 시스템: Stage6 런타임 품질 설정·URP 그림자·도시 배경 Renderer·환경 Point Light·시야 Soft Shadow·Stage6 리플레이 Renderer 등록·스모크·성능 측정. Stage1~Stage5의 리플레이 기본 경로와 저장된 Stage6 데모 환경은 변경하지 않음.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Performance/Stage6PerformanceController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PerformanceBenchmark.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.4.2로 갱신해 Stage6 전용 성능 정책, 9개 리플레이 동적 루트와 fallback, 원본 환경 보존, 스모크 구성, 실제 벤치마크 수치와 1080p 60 FPS 판정 상태를 기록했다.
- 테스트 결과: Unity 6000.1.13f1 최신 스크립트 컴파일과 `Stage6SceneBuilder.BuildAndValidateFromCommandLine` 2회 연속 실행이 종료 코드 0으로 통과했다. 두 번 모두 환경 Renderer 2,081/2,081, 최상위 프리팹 1,922/1,922, Point Light 30, 반사 프로브 4, NavMesh 정점 1,532/인덱스 2,064, 플레이어→적 완전 경로 5/5를 보존했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 환경 그림자 Point Light 최대 2, Soft Shadow 시야 라이트 2, 리플레이 동적 루트 9/fallback 0.25초와 기존 전투·NavMesh·리플레이 검증을 포함해 통과했고, `ValidateStage1Through5RegressionFromCommandLine`도 읽기 전용으로 통과했다. 1280×720 `Stage6Preview.png`를 재생성해 도시 배경·두 Roof Layer·바/라운지/통로·난간과 근거리 전투 배치가 유지되는 것을 직접 확인했다. 최신 `Stage6PerformanceBenchmark.RunFromCommandLine`은 RTX 3050 Laptop GPU에서 GPU timing을 획득했으나 배치 Game View 실제 해상도가 321×531으로 1920×1080 조건을 만들지 못했다. 300프레임 CPU 평균/p95는 40.87/77.86ms, GPU 평균/p95는 35.65/72.55ms였고, 런타임 구성은 Renderer 2,124, 환경 그림자 Point Light 2, 시야 Soft Shadow 2, 동적 루트 9, fallback 0.25초로 확인됐다. 따라서 이 비-1080p 샘플은 16.7ms도 넘으며 RTX 3050 Laptop·1080p 60 FPS 안정화는 **확인 불가**다.
- 남은 작업: **미실행**. 실제 1920×1080 Game View 또는 독립 Windows Player에서 같은 300프레임 전투 시나리오를 측정해 평균·p95 16.7ms 기준을 판정해야 한다. 실제 키보드/마우스의 장시간 이동·조준·사격·Pickup·투척·`DEADLINE` 중 그림자/카메라 체감도 **미실행**이다. Synty 캐릭터 애니메이션은 기존처럼 **부분 구현**이며 Stage6 이후 자동 전환은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook` 카메라 전투 가독성 조정

- 변경 유형: Stage6 카메라 프레이밍·정적 viewport 검증·씬/미리보기·문서 갱신
- 변경 내용: **구현 완료**. `Stage6SceneBuilder`가 전체 NavMesh를 한 화면에 담기 위해 사용하던 높이·후방 거리·FOV 계산을 전방 전투 범위 42% 기준으로 교체했다. 주 연결 NavMesh의 실제 경계에서 `cameraOffset`은 `(0, 42.04, -14.29)`에서 `(0, 30.15, -10.85)`로, `cameraFocusOffset`은 `(12.92, 0.44, 18.47)`에서 `(7.10, 0.44, 10.16)`으로, FOV는 `61.8`에서 `55.0`으로 변경됐다. 카메라는 전체 먼 전장보다 플레이어와 시작–중앙 연결부 교전을 우선하며, 기존 충돌 검사·활성 Main Camera 1대·`TopDownCameraController`·`WorldTimeVisualFeedback`·Demo Skybox/Clear Flags 정책은 유지한다. 빌더는 동적 계산값과 직렬화 값 일치, FOV 상한, 플레이어의 하단 전투 viewport 위치를 검증해 전역 조감도로의 회귀를 막는다.
- 영향을 받은 시스템: Stage6 Main Camera·탑다운 추적·NavMesh 기반 프레이밍·씬 정적 검증·미리보기. 전투 배치·NavMesh·스폰·조명·리플레이 구조는 변경하지 않음.
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.4.1로 갱신해 Stage6의 전투 가독성 우선 카메라 계산, 실제 직렬화 값, viewport 검증, 최신 미리보기와 구현 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 스크립트 컴파일은 첫 Stage6 빌더 실행에서 `Tundra build success`로 완료됐다. `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 2회 연속 종료 코드 0으로 실행해 새 프레이밍·환경 렌더러 2,081/2,081·NavMesh 정점 1,532/인덱스 2,064·플레이어→적 완전 경로 5/5를 확인했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`은 첫 직접 실행이 씬 로드 후 Unity 배치 프로세스 종료 콜백 없이 멈춰 해당 프로세스만 종료했으며, 새 배치 프로세스의 재시도는 종료 코드 0과 `Stage6 play-mode smoke test passed.`로 통과했다. Stage1~Stage5 읽기 전용 회귀와 1280×720 미리보기 재생성도 통과했고, 새 미리보기에서 플레이어·시작–중앙 교전, 바·난간·도시 배경이 함께 읽히는지 직접 검토했다. 스모크에는 원본 데모 문의 음수 스케일 `BoxCollider` 경고 1건이 있었지만 게임플레이 Error·Exception·Assert는 없었다.
- 남은 작업: **미실행**. 실제 키보드/마우스 조작으로 장시간 이동·조준·사격·픽업·투척·`DEADLINE`과 다층 난간 구간의 카메라 추적 체감은 아직 확인하지 않았다. Synty 캐릭터 애니메이션은 기존과 같이 **부분 구현**이고, Stage6 이후 자동 전환은 **미구현**이다.

## 2026-08-06 - Stage6 `Neon Overlook`

- 변경 유형: 신규 전투 스테이지·공식 Synty 데모 씬 복제·전용 NavMesh·에디터 자동 빌더·플레이 모드 스모크·미리보기·빌드 설정·문서 추가
- 변경 내용: **구현 완료**. 공식 `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Scenes/Demo_RooftopBar_01.unity`를 Unity 씬 저장 API로 `Stage6.unity`에 복제하고 `Scene`, `Roof_Layer`, `Roof_Layer_02`, `Background_FX`, `Background_Planes`, `BackgroundCity`, `Lighting (URP)`, `Lighting (BIRP)`, `Global Volume`, 반사 프로브를 월드 변환과 원본 활성 상태를 바꾸지 않은 채 `Stage 6 - Neon Overlook` 아래에 보존했다. 환경 루트는 `ReplayExcluded`로 표시했다. 소스 데모 카메라의 Clear Flags와 배경색을 기록한 뒤 제거하고, Stage5에서 검증된 게임플레이 루트만 Additive로 이동했다. 이동 전 `Dive Bar Character` 시각 6개를 제거했으며 Stage5 환경·NavMesh·조명 데이터는 가져오거나 저장하지 않았다. 공식 데모의 활성 URP 방향광을 측정해 게임플레이 `Directional Key Light`에 적용하고 데모 방향광 컴포넌트만 비활성화했으며, 포인트 라이트 30개·반사 프로브 4개·도시 배경·안개·볼륨은 유지했다. `WorldTimeVisualFeedback`에는 `preserveSceneRenderSettings: true`, Map Fill 강도 0과 빈 위치 배열을 적용했다. 소스 데모의 `Global Volume` 프로필 GUID가 실제 에셋 없이 직렬화되어 있어, 원본과 Stage1~5를 수정하지 않고 공식 Synty 볼륨 프로필을 Stage6 전용 `Stage6VolumeProfile.asset`으로 복제해 Missing Object Reference를 제거했다. 실제 데모 콜라이더를 분석해 플레이 구역 1,017개를 유지하고 완전 차폐 구조물 277개에만 Layer 8 `VisionObstacle`을 적용했으며, 배경 7개와 작은 장식 280개의 이동 방해 콜라이더를 비활성화했다. 새 `Stage6Navigation.asset`을 Bake한 뒤 가장 큰 연결 영역에서 플레이어 1명·원거리형 3명·추적형 2명·픽업 2개를 역할별로 배치하고 `NavMesh.SamplePosition`으로 보정했다. 기존 게임플레이 캡슐에는 정확한 `Overlook Character` 시각 프리팹 6개만 자식으로 연결했으며 프리팹 Collider·Rigidbody·Animator·Root Motion은 비활성화했다. 카메라 FOV 61.8, 오프셋 `(0, 42.04, -14.29)`, 포커스 오프셋 `(12.92, 0.44, 18.47)`은 NavMesh bounds와 플레이 영역 중심에서 계산했다. 빌드 설정은 `Stage1 → Stage2 → Stage3 → Stage4 → Stage5 → Stage6` 순서이며 자동 전환은 추가하지 않았다.
- 영향을 받은 시스템: Stage6 씬·Synty 옥상 환경·URP 조명/안개/볼륨/반사·물리 충돌·Layer 8 시야 장애물·NavMesh·플레이어/적/무기 픽업·`DEADLINE`·카메라·월드 시간 시각 피드백·리플레이 정적 환경 제외·빌드 설정·에디터 자동 검증·플레이 모드 스모크·미리보기
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6Navigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage6/Stage6VolumeProfile.asset`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage6PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage6Preview.png`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.4.0으로 갱신해 Stage6 `Neon Overlook`의 공식 `Demo_RooftopBar_01` 복제 원칙, Stage4 수제 7×7 단층 옥상과 다른 공식 다층 레이아웃, 적 5명·픽업 2개·`DEADLINE` 2회, 전용 NavMesh, 도시 배경·조명·안개·반사 프로브 보존, 정적 환경 `ReplayExcluded`, Stage1~Stage6 빌드 순서, 자동 전환 **미구현**, 캐릭터 애니메이션 **부분 구현**, 최신 자동/수동 검증 상태를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 컴파일에서 `Tundra build success`를 확인했다. `Stage6SceneBuilder.BuildAndValidateFromCommandLine`을 두 번 연속 종료 코드 0으로 실행해 멱등성을 확인했다. 소스/복제 환경 렌더러 2,081/2,081개, 최상위 프리팹 인스턴스 1,922/1,922개, 포인트 라이트 30개, 반사 프로브 4개와 필수 루트·원본 활성 상태·Missing Script/Object Reference 부재를 검증했다. 전용 NavMesh는 정점 1,532개·인덱스 2,064개이고 가장 큰 연결 전투 영역은 393개 삼각형, 고도 범위 약 2.08m이며 플레이어에서 적 5명까지 `PathComplete` 5/5와 시작 시 열린 시야선을 확인했다. 정적/스모크 검증은 플레이어 1명, 적·`EnemyMotor` 각 5개, 원거리형 3개, 추적형 2개, 픽업 2개, `DEADLINE` 2회, `CharacterVisualController`와 정확한 `Overlook Character` 이름 각 6개, 리플레이 추적 시야 조명 2개, `Replay Vision Cone` 1개, 정적 환경 리플레이 프록시 제외를 통과했다. `Stage6PlayModeSmokeTest.RunFromCommandLine`과 `ValidateStage1Through5RegressionFromCommandLine`도 종료 코드 0으로 통과했다. 스모크 콘솔에는 원본 데모 문 오브젝트의 음수 스케일 `BoxCollider` 경고 1건이 있었지만 Error·Exception·Assert는 없었다. `Stage6Preview.png`를 1280×720으로 생성해 다층 옥상, 바·라운지·통로·난간, 플레이어/적 배치와 도시 야경이 가려지지 않는지 직접 시각 검토해 통과했다. 원본 `Demo_RooftopBar_01`과 Stage1~Stage5 저장 씬은 변경되지 않았다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 시각 프리팹으로 연결되어 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. Stage6 이후 Stage7이나 `Stage1 → … → Stage6` 자동 전환·결과 화면·리플레이 종료 흐름은 없다. **미실행**. 실제 키보드/마우스 이동·조준·사격·픽업·투척·`DEADLINE`·클리어 리플레이의 전체 수동 플레이는 실행하지 않았다. 따라서 장시간 플레이 체감, 난간/다층 경로에서의 전투 품질, 최종 캐릭터 애니메이션 품질은 **확인 불가**다.

## 2026-08-05 - Stage5 `Undertow Dive`

- 변경 유형: 신규 스테이지·공식 Synty 데모 환경 복제·전투 배치·NavMesh·카메라/환경 조명 보존·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. 공식 `ProjectDeltatime/Assets/Synty/PolygonNightclubs/Scenes/Demo_DiveBar_01.unity`을 Unity 씬 저장 API로 `Stage5.unity`에 복제하고, 원본의 `Scene`, `Roof_Layer`, `Lighting (URP)`, 반사 프로브·볼륨 계층과 실제 건축/가구 프리팹 배치·재질·Skybox·Exp2 안개·국소 조명을 보존했다. Stage4에서는 검증된 `Systems`, `Debug HUD`, `Player`, 적 5개, 픽업 2개, `Navigation`, `Main Camera`, `Directional Key Light` 루트만 Additive 이동하고 옥상 환경과 기존 `Rooftop Character` 시각은 가져오지 않았다. 다이브 바의 바·좌석·서비스룸·기계식 황소 구역·좁은 통로가 전투선을 나누도록 플레이어 1명, 원거리형 3명, 근접형 2명, 권총·샷건 픽업 각 1개를 연결된 실내 NavMesh에 배치했다. 여섯 전투 루트에는 서로 다른 Synty 캐릭터 시각을 연결하고 프리팹 콜라이더·Animator·루트 모션을 비활성화했으며 `CharacterVisualController`의 시야·피격·기절 피드백은 유지했다. 실제 데모의 바닥·벽·계단·바·대형 가구 Physics Collider로 전용 `Stage5Navigation.asset`을 베이크했고, 작은 장식 충돌은 이동 방해에서 제외했다. 환경 루트에는 `ReplayExcluded`를 적용했다. `WorldTimeVisualFeedback`에는 기본값이 기존 Stage1~4 동작을 유지하는 씬 RenderSettings 보존 옵션을, `TopDownCameraController`에는 기본 0인 포커스 오프셋을 추가해 Stage5에서만 데모 환경 연출과 NavMesh 중심 구도를 유지했다. 빌드 설정은 `Stage1 → Stage2 → Stage3 → Stage4 → Stage5` 순서다. 원본 데모와 Stage1~4 씬은 저장하거나 재생성하지 않았다.
- 영향을 받은 시스템: Stage5 씬/빌드 설정, Synty 환경·캐릭터 시각, NavMesh 경로 탐색, 실제 구조물 충돌·Layer 8 `VisionObstacle`, 플레이어/적/픽업·`DEADLINE`, 탑다운 카메라, 월드 시간 시각 피드백, 환경 조명·안개, 리플레이 정적 환경 제외, 에디터 생성/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage5Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage5Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage5PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/TopDownCameraController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeVisualFeedback.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.7로 갱신해 Stage5 `Undertow Dive`의 **구현 완료** 상태, 공식 데모 복제/보존 원칙, 실제 공간 기반 전투 구성, 전용 NavMesh 경계·스폰·카메라 FOV, 환경 조명 보존, 정적 환경 리플레이 제외, 다섯 씬 빌드 순서, 자동 전환 **미구현**, 캐릭터 애니메이션 **부분 구현**, 자동/수동 검증 범위를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 최신 스크립트 컴파일이 `Tundra build success`로 완료됐다. `Stage5SceneBuilder.BuildAndValidateFromCommandLine`을 2회 연속 실행해 멱등성을 확인했으며, 데모 환경 렌더러 1,400개 이상, 활성 카메라 1대, 플레이어 1명, 적/모터 각 5개(원거리형 3/근접형 2), 픽업 2개, `DEADLINE` 2회, Synty 시각/컨트롤러 각 6개, 실제 구조물 `VisionObstacle`, Missing Script/Object Reference 부재, 빌드 순서를 정적으로 검증했다. 전용 NavMesh는 정점 635·인덱스 909이며 플레이어와 적 5명의 스폰 샘플 및 모든 플레이어→적 경로가 `PathComplete`이고 초기 시야선 1개 이상이 열려 있음을 확인했다. `Stage5PlayModeSmokeTest.RunFromCommandLine`은 공통 시스템 초기화·생존 적 5명·리플레이 시야 조명 2개·정적 환경 제외·전용 NavMesh 참조·6개 정확한 캐릭터 이름을 확인하고 `Stage5 play-mode smoke test passed.`로 완료됐다. `Stage5SceneBuilder.ValidateStage1Through4RegressionFromCommandLine`은 Stage1/Stage2, Stage3, Stage4 저장 씬 검증을 모두 통과했다. `Stage5Preview.png`는 1280×720으로 생성한 뒤 바·좌석·서비스룸·기계식 황소 구역과 전투 배치가 한 화면에 들어오는지 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 완화 정적 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3 → Stage4 → Stage5` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **미실행**. 실제 키보드/마우스 이동·조준·사격·대시·픽업·투척·`DEADLINE`·적 전멸 조작은 자동화하지 않았다. 따라서 체감 난이도, 구조물 모서리에서의 실제 충돌 감각, 클리어 리플레이 최종 시각 품질은 **확인 불가**다. 작업 시작 전부터 수정된 `Demo_DiveBar_01/LightingData.asset`과 `Demo_NightClub_01/LightingData.asset`의 의도는 **확인 불가**이며 이번 변경과 분리해 보존했다.

## 2026-08-05 - Stage4 `Last Call Rooftop`

- 변경 유형: 신규 스테이지·Synty 환경/캐릭터 콘텐츠·NavMesh·리플레이 시각 최적화·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. Stage3 씬·NavMesh·빌더를 참조하거나 변경하지 않고 Stage2의 공통 런타임 연결만 임시 기반으로 사용해 `Last Call Rooftop`을 추가했다. `ProjectDeltatime/Assets/Synty/PolygonNightclubs`의 바닥 모듈, 난간, 바, 소파, 야외 테이블, 화분, 화로와 조명을 사용해 7×7 옥상 테라스를 구성했다. 플레이어는 남쪽 입구에서 시작하며 서쪽 서비스 카운터·동쪽 라운지·북쪽 바·중앙 테이블 엄폐를 기준으로 이동 연사형 3명과 근접 추격형 2명을 배치했다. 권총·샷건 픽업 각 1개와 씬당 `DEADLINE` 2회를 유지했고, 카메라 FOV는 56도다. Synty 캐릭터 6개는 기존 검증된 게임플레이 캡슐의 시각 자식으로 두고 `CharacterVisualController`로 시야 가시성·피격·기절 색을 동기화했다. 정적 환경 루트에는 `ReplayExcluded`를 적용해 리플레이 프록시 추적에서 제외했으며, 플레이어·적·픽업·시야 조명 기록은 유지한다. 전용 `Stage4Navigation.asset`을 베이크하고 빌드 설정의 인덱스 3에 Stage4를 등록했다.
- 영향을 받은 시스템: 씬/빌드 설정, NavMesh 경로 탐색, 플레이어·적 Synty 시각 피드백, 제한 시야 장애물, 환경 조명, 카메라, 픽업·`DEADLINE`·리플레이 초기화, 리플레이 렌더러 추적, 에디터 콘텐츠 빌드/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage4.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage4Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage4Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage4PlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Visuals/CharacterVisualController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplayExcluded.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.6으로 갱신해 Stage4의 독립 생성 원칙, 옥상 전투 공간 분석, 씬/오브젝트/콘텐츠 구조, 빌드 순서, Layer·NavMesh·리플레이 제외 정책, 실제 직렬화 수치, 구현 상태, 자동 전환 미구현, 시각 애니메이션 한계와 검증 근거를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `Stage4SceneBuilder.BuildAndValidateFromCommandLine`이 최신 스크립트 컴파일과 씬 루트·공통 시스템·적 5명(이동 연사형 3/근접형 2)·픽업 2개·`DEADLINE` 2회·전용 NavMesh·Synty 프리팹/시각 6개·환경 조명 4개·`VisionObstacle` 13개·빌드 순서를 정적으로 검증해 종료 코드 0으로 완료됐다. `Stage4PlayModeSmokeTest.RunFromCommandLine`은 플레이어 생존, 월드 시간/스테이지/리플레이 초기화, 적/모터 각 5개, 픽업 2개, 리플레이 등록 시야 조명 2개, 캐릭터 시각 6개, 플레이어와 적 5명의 NavMesh 스폰, 정적 환경의 리플레이 추적 제외를 확인하고 `Stage4 play-mode smoke test passed.`로 완료됐다. `Stage4Preview.png`는 생성 후 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 완화 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3 → Stage4` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **확인 불가**. 실제 키보드/마우스 전투 감각, 옥상 난간·라운지 엄폐에서 적 경로/사격 압박, 적 전멸과 클리어 리플레이의 최종 시각 품질은 수동 플레이 검증이 필요하다. **확인 필요**. 배치 스모크 종료 뒤 기존 `WorldTimeVisualFeedback.OnValidate`의 Map Fill Light 생성 중 Unity 진단이 출력되었으나 스모크 어설션은 통과했으므로 별도 원인 확인이 필요하다.

## 2026-08-05 - Stage3 `Afterimage Club`

- 변경 유형: 신규 스테이지·Synty 환경/캐릭터 콘텐츠·NavMesh·빌드 설정·자동 검증·문서 갱신
- 변경 내용: **구현 완료**. `ProjectDeltatime/Assets/Synty/PolygonNightclubs`의 모듈형 바닥·벽, 바, DJ 부스, 대형 스피커, 소파, 테이블, 의자, 디스코볼과 무대 조명을 사용해 `Stage3`를 추가했다. 게임의 제한 시야와 행동량 기반 월드 시간에 맞춰 중앙 댄스 플로어는 개방 교전 공간, 서쪽 바는 긴 사격선과 연속 엄폐, 동쪽 라운지는 짧게 끊기는 엄폐, 북쪽 DJ 부스는 근접 압박 지점으로 구성했다. 플레이어는 남쪽에서 시작하고 서쪽·동쪽에 이동 연사형 2명, 북쪽 중앙에 근접 추격형 1명을 배치했으며 권총·샷건 픽업 각 1개와 씬당 `DEADLINE` 2회를 유지했다. Synty Party Female 01, Bartender Male, Bouncer Male, Party Male 02를 기존 검증된 게임플레이 캡슐의 시각 자식으로 연결하고 프리팹 콜라이더·루트 모션을 비활성화했다. 마젠타·시안·바이올렛·블루 환경 포인트 조명 4개와 FOV 52 카메라를 적용했다. 독립 `Stage3SceneBuilder`가 Stage1/Stage2를 재생성하지 않고 Stage3와 전용 `Stage3Navigation.asset`만 관리하며, 빌드 설정에는 Stage1·Stage2 다음 순서로 Stage3를 등록한다.
- 영향을 받은 시스템: 씬/빌드 설정, NavMesh 경로 탐색, 플레이어·적 시각, 제한 시야 장애물, 환경 조명, 카메라, 픽업·`DEADLINE`·리플레이 초기화, 에디터 콘텐츠 빌드/검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scenes/Stage3.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage3Navigation.asset`, `ProjectDeltatime/Assets/_Project/Art/Generated/Stage3Preview.png`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3SceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Stage3PlayModeSmokeTest.cs`, `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`, `ProjectDeltatime/Assets/Synty/PolygonNightclubs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.5로 갱신해 Stage3의 전투 공간 분석, 씬/오브젝트/콘텐츠 구조, 빌드 순서, Layer·NavMesh 정책, 실제 직렬화 수치, 구현 상태, 미구현 전환 흐름, 캐릭터 애니메이션 한계와 검증 근거를 기록했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 최신 스크립트 컴파일과 `Stage3SceneBuilder.ValidateSavedStage3`가 종료 코드 0으로 완료됐고, 씬 루트·공통 시스템·적 3명(이동 연사형 2/근접형 1)·픽업 2개·`DEADLINE` 2회·전용 NavMesh·Synty 프리팹과 캐릭터 렌더러·환경 조명 4개를 정적으로 검증했다. `Stage3PlayModeSmokeTest`는 플레이어 생존, 월드 시간/스테이지/리플레이 초기화, 적/모터 각 3개, 픽업 2개, 리플레이 등록 시야 조명 2개, 캐릭터 시각 4개, 플레이어와 적 3명의 NavMesh 스폰을 확인하고 `Stage3 play-mode smoke test passed.`로 완료됐다. 기존 `PrototypeSceneBuilder.ValidateSavedPrototypeRoom`도 Stage1/Stage2를 재생성하지 않은 채 종료 코드 0과 `Stage1 and Stage2 validation passed.`를 확인했다. `Stage3Preview.png`는 생성 후 시각 검토했다.
- 남은 작업: **부분 구현**. Synty 캐릭터는 정적 완화 포즈이며 이동·조준·사격·근접·피격·사망 애니메이션과 손 무기 부착이 없다. **미구현**. `Stage1 → Stage2 → Stage3` 자동 진행, 결과 화면, 리플레이 스킵/다음 단계가 없다. **확인 불가**. 실제 키보드/마우스 전투 감각, 바·라운지 엄폐에서 적 경로/사격 압박, 적 전멸과 클리어 리플레이의 최종 시각 품질은 수동 플레이 검증이 필요하다.

## 2026-08-04 - 빈 탄약 발사 시도의 시간 활동 반영

- 변경 유형: 플레이어 총기 입력·월드 시간 활동 처리 보완, 컴파일 검증·문서 갱신
- 변경 내용: **구현 완료**. 공용 `WeaponController`의 기존 `TryFire` 반환값은 실제 투사체 발사 성공 여부로 유지하고, `out bool fireAttempted` overload를 추가했다. 총기 구성과 참조가 유효하고 사용 간격이 지난 빈 탄약 발사 시도는 `fireAttempted`만 `true`로 반환하며, 탄약·투사체·발사 순번은 변경하지 않는다. 이때 다음 사용 시각을 무기 사용 간격만큼 전진시켜 자동소총 홀드 중에도 빈 발사 활동 펄스가 매 프레임이 아니라 발사 간격마다 한 번만 발생한다. `PlayerCombat`은 일반 발사에서 실제 발사 성공 또는 이 유효한 빈 탄약 발사 시도에 기존 `fireActivity`와 `fireActivityDuration`을 그대로 적용한다. 근접 무기·빈손 주먹의 성공 펄스는 유지하며, `DEADLINE`은 기존 `TryStageFire`를 사용하므로 탄약이 없으면 행동 준비·슬롯 소비·시간 활동이 발생하지 않는다.
- 영향을 받은 시스템: 플레이어 반자동/자동 총기 입력, 월드 시간 활동 펄스, 빈 탄약 자동소총 홀드 간격, 기존 적 AI 사격·근접/빈손 공격·`DEADLINE` 준비 발사 보존
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.4로 갱신해 일반 총기 발사에서 빈 탄약 시도도 기존 발사와 같은 시간 활동을 발생시키되, 투사체·탄약·`DEADLINE` 준비 동작은 바꾸지 않는 규칙과 검증 범위를 기록했다.
- 테스트 결과: **Unity 스크립트 배치 컴파일 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success (9.03 seconds), 6 items updated, 219 evaluated`와 종료 코드 0을 확인했다. 일반 `TryFire` 호출자는 기존 bool 반환 경로를 유지하고, 플레이어만 새 overload의 빈 탄약 시도 신호로 시간 활동 펄스를 호출하며 `TryStageFire`는 변경되지 않은 것을 정적으로 대조했다. 정식 Unity Test Framework가 없고 기존 `PrototypePlayModeSmokeTest`가 실제 LMB 빈 탄약 입력·활동 펄스 간격을 대조하지 않으므로, 해당 플레이 모드 시나리오는 **미실행**이다.
- 남은 작업: **확인 불가**. 실제 조작으로 탄약 0인 권총/샷건 클릭 시 시간 배율 체감, 탄약 0인 자동소총 홀드의 발사 간격별 펄스, 빈 발사 직후 무기 교체 시 체감, 기존 `DEADLINE` 빈 탄약 거부 결과는 별도 플레이 모드 검증이 필요하다.

## 2026-08-03 - 3축 결정적 탄도 산포 확장

- 변경 유형: 총기 탄도 산포 확장, 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. 공용 `WeaponController`가 기존 대칭 수평 팬과 수평 산포를 적용한 뒤, 그 방향의 로컬 수직 축을 기준으로 추가 회전해 수직 산포를 적용한다. `spreadJitterAngle`은 새 직렬화 필드 없이 수평·수직 각각의 최대 산포각으로 재사용한다. 권총과 자동소총은 축당 최대 ±1.5도, 샷건은 기존 18도 수평 팬의 각 펠릿에 축당 최대 ±1도를 적용한다. 수평·수직 산포는 무기 시드·발사 순번·펠릿 인덱스에 서로 다른 채널 상수를 더한 독립 결정적 해시 결과이며, Unity 전역 `Random`을 사용하지 않는다. 일반 발사·`DEADLINE` 준비 발사·적 자동소총 점사는 같은 공용 발사 경로를 유지한다. 조준점·카메라·플레이어 회전·누적 반동과 무기 에셋 값/GUID는 변경하지 않았다.
- 영향을 받은 시스템: 권총·자동소총·샷건 투사체 방향, 샷건 펠릿 패턴, 적 자동소총 점사, `DEADLINE` 준비 발사, 정적 씬 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.3으로 갱신해 수평 팬과 독립 수평·수직 결정적 산포, 무기별 축당 산포값, 적용 대상과 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 재생성·검증한 뒤 종료 코드 0으로 완료했다. 기존 권총·자동소총·샷건의 발사 모드·펠릿 수·산포 수치·시드와 Stage1/Stage2 픽업 GUID 참조를 확인했고, 수평/수직 채널 상수의 분리, 기존 `<Mouse>/leftButton` 바인딩과 `DEADLINE`의 Down 기반 준비 분기, Unity 전역 `Random` 미사용을 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 상하 산포의 시각적 체감과 명중 분포, 샷건 펠릿 원형 분포, 적 자동소총 점사와 `DEADLINE` 준비 발사의 3축 탄도 결과는 플레이 모드 테스트를 생략했으므로 확인하지 않았다.

## 2026-08-03 - 총구 기준 마우스 조준 보정

- 변경 유형: 플레이어 조준·총기/투척 발사 방향 보정, 씬 빌더 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `PlayerAim`은 마우스 광선에 맞은 가장 가까운 비트리거 콜라이더(플레이어 자신과 자식 콜라이더 제외)의 정확한 `RaycastHit.point`를 조준점으로 저장한다. 적·벽·바닥·엄폐물은 같은 거리 우선 규칙을 따르므로 벽이 먼저 맞으면 벽 뒤 적을 조준하지 않는다. 콜라이더가 없을 때만 기존 `y=0` 지면 평면 투영을 사용한다. `PlayerCombat`은 총기 일반 발사·`DEADLINE` 준비 발사·무기 투척에서 플레이어 중심 방향 대신 `WeaponController.Muzzle`에서 조준점까지의 `x/z` 방향을 사용하며, `y` 성분은 항상 0으로 유지한다. 근접 공격, 적 AI 사격, `WeaponController`의 쿨다운·무기별 산포, `Projectile`의 WorldTime SphereCast는 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 마우스 조준, 총기·투척 탄도, 벽 가림 판정, `DEADLINE` 준비 발사, Stage1/Stage2 생성 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.2로 갱신해 최근 물리 표면 조준점, 총구 기준 수평 발사, 벽 가림과 기존 산포 유지 정책을 반영했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 생성 씬의 `aimCollisionMask: ~0`과 기존 무기 산포 설정을 포함해 Stage1/Stage2를 재생성·검증한 뒤 종료 코드 0으로 완료했다. 이어 `PrototypePlayModeSmokeTest.RunFromCommandLine`도 `Prototype play-mode smoke test passed.`로 완료했다. 생성기는 기존 저장 씬의 레이아웃·머티리얼까지 광범위하게 재작성하므로, 이번 기능과 무관한 생성 산출물은 보존하지 않았다. 기존 씬은 새 직렬화 필드가 없더라도 코드 기본값 `~0`으로 동작한다.
- 남은 작업: **확인 불가**. 스모크는 일반 통합 흐름만 확인하므로, 실제 입력으로 바닥·벽·적 클릭의 시각적 조준점, 플레이어 자신 클릭 시 다음 표면 선택 또는 fallback, 벽 뒤 적 가림, 산포를 포함한 장거리 명중 감각과 `DEADLINE` 중 준비 발사의 탄도는 별도 확인이 필요하다.

## 2026-08-02 - 무기별 결정적 좌우 산포

- 변경 유형: 총기 탄도 보정, 무기 데이터·씬 빌더 정적 검증·문서 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 기본 팬 각도와 분리된 `spreadJitterAngle`, `spreadSeed`를 추가했다. 공용 `WeaponController`는 실제로 발사에 성공한 순간에만 발사 순번을 하나 늘리고, 무기 시드·발사 순번·펠릿 인덱스를 조합한 상태 없는 해시로 `[-산포 최대각, +산포 최대각]`의 좌우 오프셋을 결정한다. 권총과 자동소총은 최대 ±1.5도(시드 101/211), 샷건은 기존 18도 대칭 팬의 각 펠릿에 최대 ±1도(시드 307)를 더한다. Unity 전역 `Random`은 사용하지 않으며, 조준점·플레이어 회전·카메라는 변경하지 않았다. 일반 발사와 `DEADLINE` 준비 발사는 같은 발사 경로로 방향을 확정하고, 적 자동소총도 같은 무기 정의·컨트롤러를 사용하므로 같은 산포 규칙을 적용한다.
- 영향을 받은 시스템: 플레이어·적 총기 투사체 방향, 자동소총 점사, 샷건 펠릿 패턴, `DEADLINE` 준비 발사, 무기 ScriptableObject, Stage1/Stage2 생성·저장 씬 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.1로 갱신해 총기 발사 경로, 무기별 실제 산포·시드, 샷건 팬 패턴, 조준점 반동 제외 범위, 정적 검증 결과와 런타임 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 재생성한 뒤 `ValidateSavedPrototypeRoom` 검증을 종료 코드 0으로 완료했다. 권총/자동소총의 `spreadAngle: 0`, `spreadJitterAngle: 1.5`, 샷건의 `spreadAngle: 18`, `projectileCount: 8`, `spreadJitterAngle: 1`과 세 시드 101/211/307, 샷건 정의 GUID → 픽업 프리팹 → 두 저장 씬 참조, 기존 `<Mouse>/leftButton` 바인딩과 `DEADLINE`의 Down 기반 준비 분기를 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 연속 발사 산포 체감, 샷건 펠릿 명중 분포, 적 자동소총 점사, `DEADLINE`에서 확정된 방향의 행동 준비·해제 결과는 플레이 모드 테스트를 생략했으므로 확인하지 않았다.

## 2026-08-02 - 플레이어 자동 연사·샷건·빈손 주먹 공격

- 변경 유형: 전투 기능 확장, 무기 데이터·픽업 콘텐츠 추가, 입력/HUD/정적 검증 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 반자동/자동 발사 모드, 투사체 수, 총 퍼짐을 추가했다. 권총은 반자동 1발, 자동소총은 자동 1발이며 플레이어는 LMB 홀드로 자동소총만 발사 간격마다 연사한다. 샷건은 반자동·탄창 6·발사 간격 0.75초·탄속 16·펠릿 피해 1·8펠릿·총 퍼짐 18도(좌우 ±9도)로 추가했고, 각 발의 펠릿은 재현 가능한 대칭 고정 패턴으로 생성한다. 빈손 플레이어는 LMB Down으로 거리 1.2·반각 35도·피해 1·간격 0.6초의 주먹 공격을 사용하며 기존 `MeleeAttackResolver`와 `DEADLINE` 준비/해제 경로를 재사용한다. Stage1/Stage2에는 권총과 샷건 정의 GUID를 각각 보관하는 시작 픽업 프리팹을 배치했다.
- 영향을 받은 시스템: 플레이어 입력·전투, 총기 투사체 생성, 근접 판정, 무기 픽업/교환/드롭 호환성, HUD 조작 안내, ScriptableObject, 프리팹, Stage1/Stage2, 에디터 빌더 정적 검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Shotgun.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/PistolPickup.prefab`, `ProjectDeltatime/Assets/_Project/Prefabs/ShotgunPickup.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.3.0으로 갱신해 자동소총 홀드 연사, 샷건 수치·펠릿 패턴, 빈손 주먹, 무기별 시작 픽업, LMB 안내와 검증 한계를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 스크립트 컴파일에서 `Tundra build success`를 확인했고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`과 `ValidateSavedPrototypeRoom`이 Stage1/Stage2를 종료 코드 0으로 재생성·검증했다. 권총/자동소총/샷건의 직렬화 발사 모드·투사체 수·퍼짐, 샷건 정의 GUID → `ShotgunPickup.prefab` → 두 저장 씬의 참조, 기존 `PlayerControls.inputactions`와 생성 `PlayerControls.cs`의 `<Mouse>/leftButton` 바인딩을 정적으로 대조했다. 플레이 모드와 `PrototypePlayModeSmokeTest`는 사용자 요청에 따라 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 자동 연사 체감, 산탄 명중, 빈손 주먹 적중, 샷건 획득/교환/투척/가로채기와 `DEADLINE` 중 행동 준비·이동 해제의 런타임 결과는 후속 플레이 검증이 필요하다.

## 2026-08-02 - Deadline 전용 시네마틱 리플레이 시간축

- 변경 유형: 리플레이 시간축·카메라 연출·HUD·씬 직렬화·플레이 모드 스모크 검증·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController`가 20Hz 현실 시간 샘플에 현실·월드 타임스탬프와 Deadline 활성 상태를 기록하고, 시작 시 일반 월드 시간·Deadline 시네마틱·해제 후 슬로모션을 결합한 프레젠테이션 시간축을 생성한다. Deadline 활성 구간은 `현실 길이 / 0.50`을 0.8~2.0초로 제한하며, 해제 후 0.75 월드 초는 0.50배로 재생한다. Deadline 중 카메라는 진입 포즈로 고정되고 해제 후 0.2초 동안 기록 카메라로 보간 복귀한다. HUD는 `REPLAY 1.00x`, `DEADLINE CINEMATIC`, `DEADLINE AFTERMATH 0.50x`를 현재 단계에 따라 표시한다.
- 영향을 받은 시스템: `StageReplayController`, `DeadlineController` 상태 기록, 카메라 리플레이, 시각·조명·ViewCone 샘플 보간, `GameHud`, Stage1/Stage2 리플레이 직렬화, `PrototypeSceneBuilder`, `PrototypePlayModeSmokeTest`
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.9로 갱신해 하이브리드 시간축, 0.50배/0.8~2.0초/0.75 월드 초/0.2초 기본값, HUD 단계와 자동 검증 범위를 반영했다.
- 테스트 결과: **통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일 `Tundra build success`, `BuildAndValidateFromCommandLine`의 씬 재생성·검증, `PrototypePlayModeSmokeTest`를 통과했다. 스모크는 약 1초의 0.02배 Deadline을 최대 2초, 짧은 Deadline을 최소 0.8초, 해제 후 0.75 월드 초를 1.5초로 변환하고 재생 카메라의 고정·복귀를 확인한다.
- 남은 작업: **확인 불가**. 실제 플레이에서 `Q → 조준 회전 → 행동 준비 → 이동 해제 → 적 전멸`의 연출 품질과 `R` 재시작을 수동 확인해야 한다. ViewCone의 97회 Raycast와 메시 재계산 비용은 Unity Profiler로 별도 측정이 필요하다.

## 2026-08-02 - Q 키 기반 데드라인 발동 전환

- 변경 유형: 입력·데드라인 게임플레이·HUD·투사체 정리 수정
- 변경 내용: **부분 구현**. `Q` 키 Down 프레임에 `DEADLINE`을 즉시 발동하도록 전환했다. 기존의 실제 이동·이동 입력 해제·임박 적 탄환·충돌 예측 조건과 탄환 선점·강조를 제거했다. 충전 2회, 성공 발동 차감, 0.35 월드초 재준비, 최대 2개 행동 준비, 이동 해방, 조준 회전 중 최저 월드 배율 및 캐치 프리즈 우선은 유지한다.
- 영향을 받은 시스템: `PlayerControls`, `PlayerInputReader`, `DeadlineController`, `GameHud`, `Projectile`, `PrototypeSceneBuilder`, Stage1/Stage2 직렬화
- 관련 파일: `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/Projectile.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.8로 갱신해 Q 키 발동, 탄환·이동 조건 제거, `PRESS Q TO DEADLINE` HUD 안내, 기존 충전·동시 해방·시간 정지 규칙 유지를 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 모두 종료 코드 0으로 완료됐다. Q 바인딩과 생성 래퍼 일치, 두 씬의 기존 탄환·이동 트리거 필드 제거, `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`, 필수 참조를 확인했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**한다.
- 남은 작업: **확인 불가**. 탄환이 없는 상태·정지·이동·벽 접촉 중 Q 즉시 발동, `2/2 → 1/2 → 0/2`, 충전 소진·쿨다운·캐치 프리즈 중 Q 무시, 행동 두 개 동시 해방과 조준 회전 위험 속도는 사용자 플레이 확인이 필요하다.

## 2026-08-02 - 데드라인 씬당 충전 횟수 제한

- 변경 유형: 데드라인 게임플레이·밸런스·HUD·씬 직렬화 수정
- 변경 내용: **부분 구현**. `DeadlineController`에 직렬화된 `maximumCharges: 2`와 런타임 `chargesRemaining`을 추가했다. 성공적인 적 탄환 claim과 하드 프리즈 획득 뒤에만 1회를 차감하며, 실패한 발동 시도·행동 슬롯 사용·해제는 차감하지 않는다. 씬 로드 `Awake`에서 충전을 초기화하고 리플레이의 비활성화/재활성화로는 회복하지 않는다. 충전이 0이면 위협 강조·발동 안내를 중단하며, 기존 0.35 월드초 재준비와 행동 슬롯 2개는 유지한다.
- 영향을 받은 시스템: `DeadlineController`, `GameHud`, `PrototypeSceneBuilder`, Stage1/Stage2 직렬화, 데드라인 위협 안내
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.7로 갱신해 씬당 2회, 성공 발동 차감, 씬 재로드 초기화, 리플레이 중 미회복, HUD와 밸런스 값을 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 종료 코드 0으로 완료됐다. 두 씬의 `maximumCharges: 2`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`와 데드라인 필수 참조를 확인했다. 빌더가 만든 기능 무관한 대규모 씬·머티리얼 재직렬화는 복원하고 충전 필드만 유지했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 첫·두 번째 발동의 `2/2 → 1/2 → 0/2`, 세 번째 위협의 안내·발동 차단, 실패 시 미차감, 씬 재시작 회복, 리플레이 미회복 및 동시 해방·캐치·대시·사망 회귀는 사용자 플레이 확인이 필요하다.

## 2026-08-02 - 데드라인 회전 중 최저 시간 배율

- 변경 유형: 시간 시스템·데드라인 게임플레이 수정, 문서 갱신
- 변경 내용: `WorldTimeController.AcquireHardFreeze(bool)`에 데드라인 전용 조준 허용 토큰을 추가했다. 이 토큰만 활성이고 `WorldTimeActivity.AimTurn > 0.0001`이면 월드 전체가 씬의 `minimumTimeScale`(Stage1/Stage2 현재 0.02배)로 진행하며, 마우스를 멈추면 다시 0배 완전 정지한다. 일반 토큰 또는 `RequestHardFreeze` 기반 가로채기 프리즈가 겹치면 완전 정지를 우선한다. `DeadlineController`만 조준 허용 토큰을 요청하며 전역 `Time.timeScale`은 변경하지 않는다.
- 영향을 받은 시스템: `WorldTimeController`, `WorldTimeActivity`, `DeadlineController`, 적·투사체·투척 무기의 `WorldDeltaTime` 진행, 동시 해방, 공중 가로채기 프리즈
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `PROJECT_DESIGN_DOCUMENT.md`를 1.2.6으로 갱신해 데드라인 중 조준 회전은 최저 월드 배율, 마우스 정지는 완전 정지, 캐치 프리즈 우선 규칙과 0.02배 기준을 반영했다.
- 테스트 결과: **정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 컴파일과 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`, `ValidateSavedPrototypeRoom`이 종료 코드 0으로 완료됐다. Stage1/Stage2의 `minimumTimeScale: 0.02`, `rearmWorldDuration: 0.35`, `maximumStagedActions: 2`와 하드 프리즈 호출 경로를 정적으로 확인했다. 빌더 실행은 기존 기준과 다른 대규모 씬·머티리얼 재직렬화를 만들었으나 기능과 무관해 복원했으며, 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 데드라인 발동 후 마우스 정지 시 0배, 회전 시 0.02배, 재정지 시 0배 복귀와 회전 중 적 탄환·투척물·준비 투사체의 저속 진행을 사용자 플레이로 확인해야 한다. 캐치 정지 중 회전해도 0배 유지, 이동 해제 후 기존 활동량 배율 복귀, 동시 해방·사망·리플레이 회귀도 수동 확인이 필요하다.

## 2026-08-02 - ViewCone 리플레이 실시간 재계산 전환

- 변경 유형: 리플레이 메모리 최적화, ViewCone 재현 방식 변경, 테스트·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController.VisualTrack`에서 ViewCone의 `DynamicMeshVertices`·정점 수·`ArrayPool<Vector3>` 기반 샘플 저장과 보간 적용을 제거했다. 대신 `VisionCone.RebuildReplayMesh(Mesh, Vector3, Quaternion)`가 기존 96방향 `VisionObstacle` Raycast 수식을 재사용해 기록된 보간 위치·회전 기준으로 프록시 메시의 정점·Bounds·Normals를 매 재생 `LateUpdate`에 갱신한다. 프록시 메시의 삼각형 토폴로지는 최초 복제 시 유지하고 `MarkDynamic`으로 갱신한다. Full View에서는 ViewCone이 숨겨진 기존 경로에서 즉시 반환하므로 Raycast·메시 계산이 발생하지 않으며, `V`로 암흑 시야를 복원하면 현재 재생 시점의 메시를 즉시 재계산한다. 20Hz 포즈 기록, 동적 조명, 반복 재생, `R` 재시작은 유지한다.
- 영향을 받은 시스템: 리플레이 샘플 메모리, ViewCone 메시/Physics Raycast, 암흑·전체 시야 토글, 리플레이 진단값, 커스텀 스모크 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.5로 갱신하고 리플레이 기록·재생 흐름, 시야 재현 방식, 성능·동적 장애물 한계, 통합 검증 과제, 의사결정과 변경 이력에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일이 `Tundra build success`와 종료 코드 0으로 완료됐으며 로그는 `ProjectDeltatime/ReplayVisionRecomputeCompile.log`다. `StageReplayController.cs`에 `DynamicMesh`·`ArrayPool`·정점 캡처 버퍼 참조가 남아 있지 않은 것, `VisionCone.RebuildReplayMesh`가 프록시 메시와 기록된 보간 포즈를 받는 것, Full View의 ViewCone 조기 숨김 경로, 갱신된 `TrackedReplayVisionConeCount` 스모크 검사 코드를 정적으로 확인했다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 리플레이에서 벽·엄폐물에 따른 ViewCone 경계가 플레이 결과와 일치하는지, 97회 Raycast와 Bounds/Normals 재계산의 프레임 비용, `V` 전환 직후의 메시 복원, 반복 재생·`R` 회귀는 수동 플레이 확인이 필요하다. 현재 Stage1·Stage2의 `VisionObstacle`은 정적 벽·엄폐물이라는 전제이며, 향후 이동·생성·파괴되는 장애물이 같은 레이어에 추가되면 과거 시야와 달라질 수 있어 별도 상태 기록 또는 정책 결정이 필요하다.

## 2026-08-01 - 리플레이 ViewCone 재현 및 전체 시야 토글

- 변경 유형: 리플레이 버그 수정, 기능 추가, 입력·HUD·씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `StageReplayController`가 20Hz 캡처 시 `VisionCone`의 고정 삼각형 토폴로지는 프록시 생성 때 한 번만 복제하고, 동적으로 바뀌는 정점은 재사용 버퍼와 `ArrayPool<Vector3>` 대여 배열로 변경 샘플에 저장해 두 시점 사이를 보간한다. 리플레이는 기존 암흑 시야로 시작하고 `V`를 누르면 `IsOmniscientViewEnabled`를 전환해 ViewCone과 녹화된 Spot/Near Light 프록시를 숨긴다. 전체 시야는 Fog를 끄고 지정된 Trilight 환경광·반사 강도·카메라 배경과 그림자 없는 Directional Fill Light를 적용하며, 다시 `V`를 누르면 저장한 `RenderSettings`와 현재 재생 시점의 카메라·ViewCone·동적 조명을 즉시 복원한다. 적 몸체와 현재 장착 무기는 `EnemyCombatant.TryGetReplayVisibility`가 제공하는 논리 표시 상태를 실제 Renderer 가시성과 별도로 녹화해 전체 시야에서 시야 밖 생존 적을 표시하고, 사망·파괴·무장 해제 시점은 유지한다. 경고선과 일반 이펙트는 강제 표시하지 않는다. 반복 재생 중 선택 상태는 유지되고 `R` 씬 재시작 시 기본 암흑 시야로 초기화된다.
- 영향을 받은 시스템: 20Hz 시각 리플레이, ViewCone 동적 메시, 플레이어 Spot/Near Light, 전역 Fog·Ambient·Reflection, 카메라 배경, 적 몸체·장착 무기 가시성, Input System, 스테이지 상태 전달, HUD, Stage1/Stage2 직렬화, 에디터 빌더·스모크 검사
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`, `ProjectDeltatime/Assets/_Project/Input/PlayerControls.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Input/PlayerInputReader.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.4로 갱신하고 구현 현황, 리플레이·시야·HUD 동작, `V` 조작, 전체 시야 조명 수치, 확장 주의점·기술 부채·통합 검증 과제·의사결정·변경 이력에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드 스크립트 컴파일이 `Tundra build success`와 종료 코드 0으로 완료되었으며 로그는 `ProjectDeltatime/ReplayVisionCompile2.log`다. `PlayerControls.inputactions`의 `ReplayVisionToggle`/`<Keyboard>/v`와 생성 래퍼, `PlayerInputReader`·`StageController` 전달 경로, HUD 문자열, 두 씬의 `captureRate: 20`과 전체 시야 직렬화 값, 빌더의 동일 설정 경로를 정적으로 확인했다. 씬 빌더는 기존 씬을 재생성하지 않았다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 리플레이에서 벽·엄폐물에 따라 변한 ViewCone 경계가 잘림 없이 이어지는지, `V` 전환 순간 Fog·조명·배경과 시야 밖 적/장착 무기가 올바르게 표시되는지, 해제 시 같은 재생 시점이 복구되는지, 사망·무장 해제 타이밍과 반복·`R` 회귀의 시각 품질은 수동 플레이 확인이 필요하다. 기록 길이 상한, 매 틱 전체 Renderer 검색, 일반 색상·라인 샘플 배열 할당은 **부분 구현**인 성능 최적화 과제로 남는다.

## 2026-08-01 - 데드라인 실제 이동 판정 수정

- 변경 유형: 버그 수정, 데드라인 발동 조건 개선, 씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `PlayerMovement`가 일반 이동 입력을 적용한 마지막 물리 스텝의 Rigidbody 시작·종료 위치를 비교해 입력 방향으로 0.001m 이상 이동했을 때만 `IsPhysicallyMoving`을 공개하도록 했다. `DeadlineController`는 이 실제 이동 자격이 있던 플레이어가 이동 입력을 놓은 경우에만 위협 탄환을 검사·선점한다. 벽을 정면으로 계속 밀어 실제 변위가 없으면 탄환 강조와 `RELEASE TO DEADLINE` 안내를 지우고 입력 해제에도 발동하지 않는다. 벽을 따라 실제로 미끄러지는 이동은 인정하며, 이미 발동한 데드라인을 이동 입력으로 해제하는 기존 규칙은 유지한다.
- 영향을 받은 시스템: 플레이어 Rigidbody 이동 표본, `DEADLINE` 진입·해제, 위협 탄환 강조, HUD 안내, Stage1/Stage2 직렬화·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.3으로 갱신하고 월드 시간 및 `DEADLINE`의 현재 동작, 실제 이동 최소 변위 0.001m, 실제 이동 기반 진입 결정을 반영했다. 기존 “이동 중 정지” 의도 질문은 실제 물리 이동 후 입력 해제로 확정되어 확인 필요 목록에서 제거했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 `Tundra build success`, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`의 Stage1/Stage2 생성 성공과 `ValidateSavedPrototypeRoom`의 두 저장 씬 검증 통과를 확인했다. 두 씬에 `minimumPhysicalDisplacement: 0.001`과 `DeadlineController.movement`의 유효한 `PlayerMovement` 참조가 직렬화된 것을 확인했다. 기존 `FindObjectOfType` 사용 중단 경고와 빌더의 머티리얼 렌더 큐·NavMeshData 이름 경고는 남아 있으나 컴파일 오류는 없다. 사용자 요청에 따라 플레이 모드와 `PrototypePlayModeSmokeTest`는 **미실행**했다.
- 남은 작업: **확인 불가**. 열린 공간 이동 후 정상 발동, 벽 정면 밀기 후 미발동·안내 제거, 벽을 따른 대각선 이동 인정, 발동 후 벽 방향 입력을 통한 해제, 대시·캐치·사망·리플레이 회귀는 사용자 플레이 확인이 필요하다.

## 2026-08-01 - 원형 근거리 적 가시성 확장

- 변경 유형: 적 렌더링 판정 개선, 문서 갱신
- 변경 내용: **구현 완료**. `VisionCone.ContainsWorldPoint(Vector3)`를 기존 부채꼴 단독 판정에서 원형 근거리 또는 부채꼴 시야의 합집합 판정으로 확장했다. 대상이 `nearLightGroundRadius` 안에 있으면 방향과 관계없이 시야 후보가 되고, 원형 밖에서는 기존 거리·각도 조건을 사용한다. 두 경우 모두 기존 `VisionObstacle` Raycast를 통과해야 최종 가시 상태가 된다. `EnemyCombatant`의 몸체·장착 무기·경고선 토글 경로는 변경하지 않아 확장된 판정이 기존 렌더링 규칙에 그대로 적용된다.
- 영향을 받은 시스템: 플레이어 시야 판정, 적 몸체·장착 무기 렌더링, 공격 경고선, 벽·엄폐물 차폐
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.2로 갱신하고 핵심 콘셉트, 구현 현황, 카메라·시야 동작, 기술 결정과 변경 이력에 부채꼴·원형 시야 합집합 기반 적 가시성을 반영했다.
- 테스트 결과: **정적 검증 통과**. 원형 반경과 부채꼴 조건이 논리합으로 결합된 뒤 공통 장애물 Raycast를 사용하는 것, `EnemyCombatant`가 `ContainsWorldPoint` 결과로 몸체·장착 무기를 토글하고 비가시 상태에서 경고선을 숨기는 기존 경로가 유지된 것을 확인했다. Stage1/Stage2의 반경 4·밝기 4·높이 1과 손전등 60도·거리 12.5·밝기 7.5도 변경되지 않았다. 사용자 요청에 따라 Unity 배치 모드 씬 검증과 플레이 모드 스모크 테스트는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 플레이에서 뒤·옆의 반경 4 적 표시, 반경 경계의 깜빡임, 벽·엄폐물 뒤 차폐와 이동·회전 중 갱신 결과는 런타임 테스트를 생략해 확인하지 않았다.

## 2026-08-01 - 플레이어 주변 원형 조명 강화

- 변경 유형: 시야 조명 개선, 씬 직렬화·문서 갱신
- 변경 내용: **구현 완료**. `VisionCone`의 기존 근거리 조명을 플레이어 기준 높이 1에 배치되는 Point Light로 유지하면서, 지면 반경 4가 되도록 높이를 포함한 실제 `Light.range`를 계산하게 변경했다. 밝기는 4, 렌더 모드는 `ForcePixel`, 그림자는 Soft·강도 0.85로 설정해 거리 감쇠 경계와 `VisionObstacle` 벽·엄폐물의 실시간 그림자 차폐를 사용한다. 기존 `nearLightRange`는 `nearLightGroundRadius`로 이름을 바꾸고 `FormerlySerializedAs`를 적용했으며, Stage1과 Stage2 모두 반경 4·밝기 4·높이 1을 직렬화했다. 60도·거리 12.5·밝기 7.5의 부채꼴 손전등과 스테이지별 맵 보조광 프로필은 변경하지 않았다.
- 영향을 받은 시스템: 플레이어 시야 조명, `VisionObstacle` 벽·엄폐물 차폐, Stage1/Stage2 조명 프로필, 리플레이 조명
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.1로 갱신하고 핵심 콘셉트, 구현 현황, 카메라·시야 동작, 실제 밸런스 수치, 기술 결정과 변경 이력에 양 스테이지 공통 원형광을 반영했다.
- 테스트 결과: **정적 검증 통과**. 코드 기본값과 Stage1/Stage2의 `nearLightIntensity: 4`, `nearLightGroundRadius: 4`, `nearLightHeight: 1`이 일치하고, 두 씬의 손전등 60도·거리 12.5·밝기 7.5 및 Stage1/Stage2 맵 보조광 프로필이 유지되는 것을 확인했다. 현재 선택된 Ultra 품질의 픽셀 조명·실시간 그림자 지원과 리플레이 프록시가 Light의 범위·그림자·렌더 모드를 복제하는 경로도 정적으로 확인했다. 사용자 요청에 따라 Unity 배치 모드 씬 검증과 플레이 모드 스모크 테스트는 **미실행**했다.
- 남은 작업: **확인 불가**. 실제 플레이에서 방향과 무관한 원형 밝기, 벽 반대편 차폐, 손전등과의 밝기 대비, 이동·회전 추적, 리플레이의 위치·밝기·그림자 재현은 런타임 테스트를 생략해 확인하지 않았다. 그림자가 비활성화된 Low 이하 품질에서는 벽 차폐가 보장되지 않는다.

## 2026-08-01 - 적 무기 드롭·재무장·주먹 공격 확장

- 변경 유형: 기능 추가, 적 전투 AI 통합, 플레이어 전투/체력 확장, 무기 데이터·씬·프리팹 갱신
- 변경 내용: **구현 완료**. `WeaponDefinition`에 `WeaponKind(Firearm/Melee)`, 근접 범위·각도·사용 간격과 적 점사 수를 추가하고 피해 3의 `MeleeWeapon.asset`을 생성했다. `EnemyCombatant`가 현재 장비에 따라 총기 거리 유지·70% 후퇴 사격, 0.42 월드초 선딜·35% 저속 추적 근접 공격, 빈손 주먹 공격과 무기 탐색을 선택한다. 모든 적은 던진 무기에 기절하면 현재 장비/탄약을 공중 드롭하며, 회복 뒤 플레이어가 3 거리 안이면 주먹을 우선하고 그 밖에서는 반경 8의 완전한 NavMesh 경로 픽업을 0.25 월드초마다 예약·탐색한다. 장전된 총기를 우선하되 경로가 가까운 근접 무기보다 2 이상 길면 근접 무기를 고른다. 플레이어는 근접 무기를 획득·즉시 공격·투척할 수 있고 `DEADLINE`에서 방향/수치가 저장된 근접 공격을 준비할 수 있다. 플레이어 최대 체력을 3으로 변경하고 `CurrentHealth`, `HealthChanged`, HUD 체력과 `LMB Attack` 안내를 추가했다. 주먹 피해는 1, 총기/근접 무기 피해는 3이다.
- 영향을 받은 시스템: 무기 데이터/시각 표현, 플레이어 사격·근접 공격·투척·`DEADLINE`, 플레이어 체력/HUD, 적 공통 전투 상태/이동 모드, 기절·무장 해제·재무장, NavMesh 경로 길이, 바닥 픽업 예약/경쟁, 공중 무기 드롭, Stage1/Stage2 생성·검증
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Combat/InterceptableWeapon.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyWeaponDrop.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombat.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/Pistol.asset`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`, `ProjectDeltatime/Assets/_Project/Prefabs/InterceptableWeapon.prefab`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.2.0으로 갱신하고 현재 장비 기반 적 공격, 드롭/재무장/주먹/픽업 예약, 플레이어 근접 전투와 체력 3, 실제 에셋·씬 밸런스, 검증 한계를 구현 현황·시스템·콘텐츠·조작·기술 구조·수치·과제·의사결정에 반영했다.
- 테스트 결과: **Unity 컴파일 및 정적 검증 통과**. Unity 6000.1.13f1 배치 모드에서 스크립트 어셈블리 빌드가 `Tundra build success`로 완료되었고, `PrototypeSceneBuilder.BuildAndValidateFromCommandLine`이 Stage1/Stage2를 생성해 연사 시작 적 2, 근접 시작 적 1, `EnemyCombatant`/`EnemyMotor`/`EnemyPerception` 각 3, 전체 `WeaponController` 4, `EnemyWeaponDrop` 3과 NavMeshData를 검증했다. 두 씬의 `maximumHealth: 3`, 세 적의 공통 드롭 참조, `retreatMoveSpeedMultiplier: 0.7`, `windupMoveSpeedMultiplier: 0.35`, `weaponSearchRadius: 8`, `weaponSearchInterval: 0.25`, `firearmPathTolerance: 2`, 근접 시작 적의 `MeleeWeapon.asset` GUID를 정적으로 확인했다. `Pistol.asset`/`AutomaticRifle.asset`의 피해 3과 적 점사 1/4, `MeleeWeapon.asset`의 피해 3·거리 1.45·반각 35·간격 0.72를 확인했다. 최신 소스 컴파일 로그는 `ProjectDeltatime/EnemyRearmFinalCompile.log`, 씬 생성/정적 검증 로그는 `ProjectDeltatime/EnemyRearmBuild.log`다. 사용자 요청에 따라 플레이 테스트와 `PrototypePlayModeSmokeTest`는 **미실행**했으며 과거 로그를 이번 결과로 재사용하지 않았다.
- 남은 작업: **확인 불가**. 근접 무기 드롭·재획득, 시작 유형과 다른 무기 사용, 주먹 세 번 피격, 근거리 주먹 우선, 원거리 무기 탐색, 여러 적의 픽업 경쟁, 플레이어 근접 공격과 `DEADLINE` 해제 판정은 플레이/스모크 테스트를 생략했으므로 런타임 결과를 확인하지 않았다. 새 애니메이션·효과음·근접 무기 전용 모델은 **미구현**이며 기존 큐브/경고선 표현을 사용한다.

## 2026-07-31 - 근접 공격 판정 및 라이플 후퇴 사격 개선

- 변경 유형: 버그 수정, 적 AI 동작 개선, 씬 직렬화 갱신
- 변경 내용: **구현 완료**. `EnemyPerception`의 시야 원점을 무기 끝에서 적 몸체로 변경해 밀착 시 목표 Raycast가 끊기던 근접 공격 판정을 수정했다. `EnemyChaser`는 0.42 월드초 선딜 중 플레이어를 바라보며 기본 속도의 35%로 계속 추적한다. `EnemyShooter`는 공격 단계와 이동 모드를 분리해 6 거리 미만에서 플레이어를 바라보며 기본 속도의 70%로 후퇴하고, 후퇴 중에도 조준·4발 점사·쿨다운을 진행한다.
- 영향을 받은 시스템: 적 시야 판정, NavMesh 이동/회전, 근접 공격, 자동소총 조준·점사, Stage1/Stage2 직렬화
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.1.1로 갱신하고 라이플 적의 공격/이동 병렬 상태, 후퇴 속도 70%, 근접 선딜 추적 속도 35%, 몸체 기준 시야 판정과 최신 검증 한계를 반영했다.
- 테스트 결과: Unity Editor가 변경 스크립트를 컴파일해 소스 변경 시각 이후 `Library/ScriptAssemblies/Assembly-CSharp.dll`을 갱신한 것을 확인했다. `PrototypeSceneBuilder`의 사격/근접 감지 원점이 적 몸체를 사용하도록 구성된 것과 Stage1/Stage2의 사격 적 2명 `retreatMoveSpeedMultiplier: 0.7`, 근접 적 1명 `windupMoveSpeedMultiplier: 0.35`, 세 적의 몸체 Transform 기반 `sightOrigin`을 정적으로 확인했다. 사용자 요청에 따라 플레이 테스트와 `PrototypePlayModeSmokeTest`는 **미실행**했으며, 기존 로그는 이번 변경의 결과로 사용하지 않았다.
- 남은 작업: **확인 불가**. 실제 조작 중 근접 공격 적중감, 후퇴 중 조준·연사 체감과 벽에 막힌 후퇴 상황은 플레이 테스트를 생략해 확인하지 않았다.

## 2026-07-31 - NavMesh 기반 이동 연사형·지속 추격 근접형 적

- 변경 유형: 기능 추가, 적 AI 구조 개선, 씬·데이터·패키지 갱신, 회귀 검사 확장
- 변경 내용: **구현 완료**. 공통 `EnemyBehavior`, `EnemyPerception`, `EnemyMotor`를 추가해 기절/무장 해제/사망, 시야선/최근 위치, NavMesh 경로와 월드 시간 Rigidbody 이동을 분리했다. `EnemyShooter`를 6~9 거리 유지, 추적/후퇴, 0.65 월드초 조준, 자동소총 4발 점사형으로 확장했다. `EnemyChaser`는 플레이어 현재 위치를 계속 갱신해 추격하고 1.45 범위에서 0.42 월드초 선딜 후 근접 피해를 준다. 두 씬은 연사형 2명과 근접 추격형 1명으로 재구성했다.
- 영향을 받은 시스템: 적 AI, 적 체력/기절/무장 해제, 3D 물리 이동, 월드 시간, NavMesh, 총기/탄약/드롭, 씬 생성/검증, 스모크 테스트, 리플레이 기록 대상
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyBehavior.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyPerception.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMotor.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyShooter.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyChaser.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyHealth.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypeSceneBuilder.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`, `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`, `ProjectDeltatime/Assets/_Project/Scenes/StageNavigation.asset`, `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`, `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`, `ProjectDeltatime/Packages/manifest.json`, `ProjectDeltatime/Packages/packages-lock.json`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`를 1.1.0으로 갱신하고 구현 현황, 적 AI 구조, 씬 구성, 클래스 책임, 확장 주의점, 자동소총/이동/근접 공격 밸런스, 의사결정과 최신 테스트 근거를 반영했다.
- 테스트 결과: Unity 6000.1.13f1 배치 모드에서 `PrototypeSceneBuilder.BuildAndValidateFromCommandLine` 통과. 텍스트 직렬화된 Stage1/Stage2와 외부 `StageNavigation.asset`을 생성하고 연사형 2, 근접 추격형 1, `EnemyMotor`/`EnemyPerception` 각 3, NavMeshSurface 1을 검증했다. 이어 `PrototypePlayModeSmokeTest` 통과. 적 누적 이동과 NavMesh 경로 획득, 근접형 추격 상태, 두 유형의 기절/회복 후 무장 해제, 사격형 2개의 중복 없는 공중 무기 드롭, 적 전멸 후 리플레이를 확인했다. 로그: `ProjectDeltatime/EnemyMovementBuild.log`, `ProjectDeltatime/EnemyMovementSmoke.log`.
- 남은 작업: **부분 구현**. 근접 무기는 시각 표현과 직접 피해만 제공하며 획득/투척/드롭 가능한 무기 데이터는 없다. 런타임 동적 NavMesh 재베이크, 전술적 엄폐 선택, 적 협동/재무장, 플레이어 시야 밖 공격 정책, 수동 플레이 기반 체감 밸런스 확인이 남았다.

## 2026-07-30 - 프로젝트 현황 기준선 문서화

- 변경 유형: 문서 추가
- 변경 내용: 현재 Unity 프로젝트의 구현 상태를 코드·에셋·설정·Git 상태 기준으로 분석하고 최초 기획서와 기능 변경 기록 양식을 생성했다.
- 영향을 받은 시스템: 문서 관리 규칙, 전체 기능 기준선
- 관련 파일: `mdFile/PROJECT_DESIGN_DOCUMENT.md`, `mdFile/FEATURE_CHANGELOG.md`, `AGENTS.md`
- 기획서 반영 내용: 프로젝트 개요, 구현 현황, 핵심 루프, 주요 시스템, 씬/콘텐츠, 플레이어 경험, 기술 구조, 밸런스, 우선순위 과제, 의사결정, 확인 질문을 1.0.0 기준으로 작성했다.
- 테스트 결과: 문서 작성 작업이므로 런타임 테스트 미실행. 기존 `ProjectDeltatime/Logs/CodexSmoke.log`의 2026-07-30 18:07 통과는 확인했으나, 22:13까지 이어진 최신 기능 변경보다 이전 결과이므로 현재 작업 트리의 최신 통과 결과는 확인 불가다.
- 남은 작업: Unity Editor를 종료할 수 있는 시점에 최신 작업 트리로 배치 스모크 테스트를 실행하고 결과를 별도 기능 항목에 기록한다.

## 2026-07-30 - 플레이어 벽 충돌 안정화

- 변경 유형: 버그 수정, 회귀 검사 추가
- 변경 내용: 일반 이동을 동적 Rigidbody의 `MovePosition` 위치 강제 이동에서 `linearVelocity` 평면 속도 제어로 변경해 벽 접촉 시 물리 보정과 위치 목표가 반복 충돌하지 않게 했다. 대시는 실제 플레이어 캡슐보다 0.03 작은 캡슐을 캐스트하고 캐스트 거리에서 스킨을 다시 빼도록 변경해, 벽에 맞닿거나 최대 0.03 겹친 시작 상태에서도 안전 거리를 0으로 제한한다. 대시 시작·종료 시 잔여 선형 속도도 제거한다.
- 영향을 받은 시스템: 플레이어 일반 이동, 대시, 3D 물리 충돌, 플레이 모드 스모크 테스트
- 관련 파일: `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/PrototypePlayModeSmokeTest.cs`
- 기획서 반영 내용: `mdFile/PROJECT_DESIGN_DOCUMENT.md`의 구현 현황, 이동 및 조작, 통합 검증, 플레이어와 월드 시간 분리 설명을 현재 구현으로 갱신했다.
- 테스트 결과: Unity 6000.1.13f1 배치 모드에서 `PrototypePlayModeSmokeTest` 통과. 빈 공간의 0.5 대시 거리가 축소되지 않고, North Wall에 0.01 겹친 시작 상태의 안전 거리가 0.001 이하인지 확인했다. 로그: `ProjectDeltatime/Logs/CodexWallCollisionSmoke.log`. 기존 `FindObjectOfType` 사용에 대한 폐기 예정 경고는 있으나 컴파일 오류와 스모크 실패는 없었다.
- 남은 작업: 키보드를 길게 눌러 벽을 미는 상황의 화면상 체감과 여러 프레임의 위치 진동은 헤드리스 스모크 범위 밖이므로 Unity Editor 수동 플레이에서 최종 확인이 필요하다.
