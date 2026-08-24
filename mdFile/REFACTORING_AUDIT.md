# ProjectDeltatime 리팩터링 감사

## 1. 범위와 상태

- 감사일: 2026-08-14 (KST)
- 대상 브랜치: `feature/Refactoring`
- Unity 버전: 6000.1.13f1
- 변경 성격: 기능·밸런스·입력·UI·오디오·직렬화 값을 바꾸지 않는 구조 리팩터링
- 전체 상태: **구현 완료**
- 보류 콘텐츠: Stage3, Stage4, Stage6 및 관련 씬 빌더·에셋을 삭제하지 않고 보존했다.

## 2. 기준선과 저장소 위생

- 작업 시작 시 Git 작업 트리는 깨끗했고 Unity 배치 컴파일은 종료 코드 0이었다. 근거: `ProjectDeltatime/Logs/Validation/RefactorBaselineCompile.log`.
- 추적 중이던 `.DS_Store` 4개를 제거하고 `ProjectDeltatime/.gitignore`에 재유입 방지 규칙을 추가했다. 짝 `.meta`는 저장소에 존재하지 않았다.
- 문서에서 인용되지 않은 프로젝트 루트 로그 95개를 정확한 목록으로 확인한 후 제거했다. 문서에서 인용된 92개 로그는 보존했다.
- 이후 검증 산출물은 `ProjectDeltatime/Logs/Validation/`에 생성했다. 이 폴더의 로그와 XML은 검증 산출물이며 Git 추적 대상이 아니다.
- 코드·에셋 참조가 없는 `com.unity.multiplayer.center 1.0.0`을 제거하고 실제 잠금 버전인 `com.unity.test-framework 1.5.1`을 직접 의존성으로 선언했다. 근거: `ProjectDeltatime/Packages/manifest.json`, `ProjectDeltatime/Packages/packages-lock.json`.

## 3. 어셈블리와 테스트 경계

- `Deltatime.Runtime`, `Deltatime.Editor`, `Deltatime.Tests.EditMode`, `Deltatime.Tests.PlayMode` Assembly Definition을 추가했다. 생성 파일 `PlayerControls.cs`는 `Deltatime.Runtime`에 포함되며 직접 수정하지 않았다.
- 런타임 내부 타입은 테스트 어셈블리에만 `InternalsVisibleTo`로 공개했다. 새 게임플레이 공개 API는 추가하지 않았다.
- `.editorconfig`를 추가했으나 기존 코드의 일괄 포맷팅은 수행하지 않았다.
- 근거: `ProjectDeltatime/Assets/_Project/Deltatime.Runtime.asmdef`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/Deltatime.Editor.asmdef`, `ProjectDeltatime/Assets/_Project/Tests/`, `ProjectDeltatime/Assets/_Project/Scripts/AssemblyInfo.cs`.

## 4. 에셋 의존성 감사

- 상태: **구현 완료**. 읽기 전용 `AssetDatabase.GetDependencies` 감사 도구를 추가했다. 씬, Build Settings, Resources, Input, 사전 로드 에셋, 빌더 경로 리터럴을 루트로 사용하며 자동 삭제하지 않는다.
- 실행 결과: 의존 루트 77개, 해석된 의존성 1,554개, 미사용 후보 15개, 누락 빌더 경로 3개, 고아 `.meta` 0개.
- 미사용 후보 15개는 삭제하지 않았다.
  - `Assets/_Project/Art/Generated/Circle.png`
  - `Assets/_Project/Art/Generated/EnemyRoles/EnemyRole_Firearm.png`
  - `Assets/_Project/Art/Generated/EnemyRoles/EnemyRole_Melee.png`
  - `Assets/_Project/Art/Generated/EnemyRoles/EnemyRole_Unarmed.png`
  - `Assets/_Project/Art/Generated/PrototypeRoom3DPreview.png`
  - `Assets/_Project/Art/Generated/Square.png`
  - `Assets/_Project/Audio/SFX/Combat/SFX_Weapon_Pickup.ogg`
  - `Assets/_Project/Image/logo.png`
  - `Assets/_Project/Materials/VisionAlwaysVisible.mat`
  - `Assets/_Project/Materials/VisionHiddenArea.mat`
  - `Assets/_Project/Materials/VisionStencilWriter.mat`
  - `Assets/_Project/Shaders/VisionAlwaysVisible.shader`
  - `Assets/_Project/Shaders/VisionHiddenArea.shader`
  - `Assets/_Project/Shaders/VisionStencilWriter.shader`
  - `Assets/_Project/Shaders/WorldTimeEmissiveScroll.shader`
- 누락 빌더 경로는 `Stage1Preview.png`, `Stage3.unity`, `Stage4.unity`이다. Stage3/4 실제 파일명과 빌더 리터럴의 기존 불일치는 수정하지 않았다.
- 근거: `ProjectDeltatime/Assets/_Project/Scripts/Editor/ProjectAssetDependencyAudit.cs`, `ProjectDeltatime/Logs/Validation/AssetDependencyAudit.txt`.

## 5. 에디터 도구 리팩터링

- 상태: **구현 완료**. 기존 10개 PlayMode 스모크의 콜백 등록·해제, 씬 열기, PlayMode 진입을 `CommandLineSmokeRunner`로 통합했다. 기존 메뉴와 `RunFromCommandLine()` 진입점은 유지했다.
- SceneBuilder 공통 기능을 `SceneBuildCommand`, `SceneValidation`, `CharacterSceneSetup`, `NavigationSceneSetup`, `PreviewCapture`로 분리했다. 기존 씬 경로, 메뉴, 공개 CLI 진입점은 유지했다.
- 현재 저장된 씬·프리팹·ScriptableObject에는 변경이 발생하지 않았다. 리팩터링 전 빌더 산출물의 독립 임시 프로젝트 YAML/GUID 대조는 기준선 사본을 작업 시작 전에 만들지 못해 **확인 불가**이며, 빌더 자체 실행으로 현재 에셋을 재생성하지 않았다.
- 근거: `ProjectDeltatime/Assets/_Project/Scripts/Editor/CommandLineSmokeRunner.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Editor/SceneBuilderInfrastructure.cs`.

## 6. 런타임 책임 분해

- 상태: **구현 완료**. 기존 `MonoBehaviour`, 직렬화 필드, 공개 멤버와 씬 참조를 유지했다.
- 리플레이: `StageReplayController`를 façade로 유지하고 녹화 세션, 시각 레지스트리, 타임라인, 재생 세션, 애니메이션 기록/재생을 내부 타입으로 분리했다. `IReplayCaptureSink`, `ReplayVisualRegistry`를 도입했고 `StageReplayController.ActiveRecorder`는 호환용으로 유지했다. 20Hz 기록과 64MiB 예산 값은 변경하지 않았다.
- 적 AI: `EnemyCombatant`에서 상태 타이머, 총기 사거리 판단, 무기 선택, 경고선 표현을 내부 협력 객체로 분리했다. 기존 상속, enum, 공개 상태, `Configure`는 유지했다.
- 튜토리얼: 단계 진행, 투척 회수, DEADLINE 시나리오 상태와 안내 판단을 내부 객체로 분리했다. 기존 단계, 공개 검증 API와 직렬화 필드는 유지했다.
- 2차 핫스폿: `PlayerCombat`의 공격/무기 선택 판단, `EnemyMotor`의 평면 경로 계산, 월드 시간과 DEADLINE의 시각 상태 계산을 내부 순수 로직으로 분리했다.
- 근거: `ProjectDeltatime/Assets/_Project/Scripts/Replay/ReplaySubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerCombatSubsystems.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyMovementMath.cs`, `ProjectDeltatime/Assets/_Project/Scripts/Time/VisualFeedbackState.cs`.

## 7. 검증 결과와 알려진 실패

- 최종 Unity 배치 컴파일: **구현 완료**, 종료 코드 0. 근거: `ProjectDeltatime/Logs/Validation/RuntimeRefactorCompileFixed.log`.
- EditMode: **구현 완료**, 17/17 통과. Stage 흐름, 월드 시간 토큰, 무기 산포, 리플레이 시계·예산·타임라인·재생 홀드/루프, 적 상태/사거리, 튜토리얼 DEADLINE 게이트를 검증했다. 근거: `ProjectDeltatime/Logs/Validation/EditModeResults.xml`.
- PlayMode 테스트 어셈블리: **구현 완료**, 1/1 통과. 하드 프리즈 중 `Time.timeScale == 1`과 `WorldDeltaTime == 0`을 검증했다. 근거: `ProjectDeltatime/Logs/Validation/PlayModeResults.xml`.
- 기존 스모크 통과: Stage1 캐릭터 애니메이션, Stage6, SoundManager. Replay는 일괄 실행 1차에서 진단 조건 실패 후 동일 코드·동일 씬 단독 재실행에서 통과했다.
- 기존 또는 현재 콘텐츠 기준 실패:
  - Prototype: 투척 무기 속도/거리/기절 시간과 정확히 6m 정착 검증 실패. 같은 서명이 과거 `ReplayPrototypeRegression2.log`, `ReplayVisionPrototypeSmoke.log`, `Stage2SyntyEnemySmoke.log`에도 존재한다. 리플레이 본 애니메이션 조건도 함께 실패했다.
  - Tutorial: Synty 프리팹 216개, 기대값 262개. 같은 서명이 과거 `TutorialAudioSmoke.log`에도 존재한다.
  - Stage3/4: 빌더/스모크가 기대하는 `Stage3.unity`, `Stage4.unity` 파일이 없는 기존 파일명 불일치.
  - Stage5: 현재 저장 씬의 무기 픽업 1개, 스모크 기대값 2개.
  - DEADLINE visual: 활성 Build Settings에서 보류 Stage6가 제외되어 Stage5 다음 `Stage6` 이름 로드가 실패한다. Build Settings의 `MainScene → Tutorial → Stage1 → Stage2 → Stage5 → EndingScene` 순서는 보존했다.
- Stage6 성능 벤치마크: **부분 구현**. 90프레임 워밍업과 300프레임 샘플을 완료했고 프로세스는 종료 코드 0이었다. 배치 Game View 실제 해상도가 321×531이라 1080p 60 FPS 판정은 **확인 불가**다. 참고 측정은 CPU 평균 21.67ms/p95 48.97ms, GPU 평균 17.80ms/p95 44.99ms, 렌더러 2,131개, 환경 그림자 Point Light 2개, 시야 Soft Light 2개, 리플레이 동적 루트 9개, fallback 0.25초다. 근거: `ProjectDeltatime/Logs/Validation/Stage6PerformanceBenchmark.log`.

## 8. 보존 확인

- 입력 액션, 밸런스 값, UI, 오디오, `Time.timeScale`, 리플레이 기록률·예산, 씬/프리팹/ScriptableObject GUID를 의도적으로 변경하지 않았다.
- Git 기준 씬, 프리팹, ScriptableObject 변경은 없다.
- Synty, Deliverables, Stage3/4/6 및 감사 후보 에셋은 모두 보존했다.
