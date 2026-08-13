# Deltatime 역기획서

> 📌 이 문서는 Unity 프로젝트의 현재 코드·씬·프리팹·ScriptableObject·Input Action·Build Settings·테스트 로그를 근거로 작성한 Notion 입력용 역기획서다.

## Notion 페이지 속성

| 속성 | 값 |
|---|---|
| 프로젝트 | Deltatime |
| 문서 유형 | 실제 구현 기준 역기획서 |
| 분석 기준일 | 2026-08-12 (KST) |
| Unity 버전 | 6000.1.13f1 |
| 현재 상태 | 핵심 전투 루프가 구현된 3D 프로토타입 |
| 기준 저장소 | `C:\Users\HuiYong\UnityProjects\ProjectDeltatime` |
| 실제 Unity 프로젝트 | `ProjectDeltatime/` |
| 상태 범례 | 구현 완료 / 부분 구현 / 미구현 / 계획 필요 / 확인 불가 |

## 1. 역기획 요약

### 한 문장 요약

Deltatime은 플레이어의 이동·조준 활동으로 월드 시간을 조절하고, 제한 시야와 총기·근접·투척 전투를 수행한 뒤, `DEADLINE`으로 행동을 준비해 실행하는 3D 탑다운 액션 슈터 프로토타입이다.

### 핵심 게임 루프

1. 제한 시야 안에서 적과 무기 위치를 확인한다.
2. 이동·조준·정지로 월드 시간 배율을 조절한다.
3. 총기, 근접 공격, 투척, 대시를 조합해 적을 상대한다.
4. 필요할 때 Q로 `DEADLINE`을 발동한다.
5. 최대 2개의 행동을 준비한 뒤 이동으로 시간을 다시 흘려보내 행동을 실행한다.
6. 적을 모두 제거하면 행동 기록을 Replay로 확인한다.
7. `N`으로 다음 씬으로 이동한다.

### 현재 활성 진행

`MainScene → Tutorial → Stage1 → Stage2 → Stage5 → EndingScene → MainScene`

현재 Build Settings에는 다음 6개 씬만 활성화되어 있다.

1. `MainScene`
2. `Tutorial`
3. `Stage1`
4. `Stage2`
5. `Stage5`
6. `EndingScene`

Stage3, Stage4, Stage6은 씬·에셋·전용 데이터가 보존되어 있지만 현재 진행 경로와 Build Settings에서는 제외되어 있다.

## 2. 게임 개요

### 장르

- 3D 탑다운/쿼터뷰 액션 슈터 프로토타입
- 장르명과 최종 상업적 포지셔닝은 저장소만으로 확정할 수 없음

### 핵심 콘셉트

- 플레이어의 움직임과 조준 활동이 월드 시간 배율에 영향을 준다.
- 시간이 느려진 상태에서 적의 공격과 위치를 읽고 대응한다.
- `DEADLINE`은 행동을 미리 준비한 뒤 이동으로 실행하는 별도 전투 상태다.
- 적 전멸 또는 사망 후 이전 행동을 Replay로 재생한다.

### 플레이어 경험 목표

| 경험 | 구현 상태 | 근거 |
|---|---|---|
| 시간 감속을 활용한 조준·회피 | 구현 완료 | `Assets/_Project/Scripts/Time/WorldTimeController.cs`, `Player/PlayerAim.cs` |
| 제한 시야 기반 탐색 | 구현 완료 | `Assets/_Project/Scripts/Vision/VisionCone.cs` |
| 총기·근접·투척의 전환 | 구현 완료 | `Assets/_Project/Scripts/Combat/WeaponController.cs`, 무기 정의 에셋 |
| DEADLINE 행동 준비·실행 | 구현 완료 | `Assets/_Project/Scripts/Player/DeadlineController.cs` |
| Replay를 통한 결과 확인 | 부분 구현 | `Assets/_Project/Scripts/Replay/StageReplayController.cs`, Replay 로그 |
| 장기 성장·저장·퀘스트 | 미구현 | 관련 런타임 데이터와 API 확인 불가 |

## 3. 게임 시작·진행·종료

### 시작

- `MainScene`이 Build Settings의 첫 씬이다.
- 화면의 `PLAY` 버튼 또는 `N` 키로 `Tutorial`을 로드한다.
- 씬 전환 전 Build Settings 등록 여부를 확인한다.
- UI 클릭 또는 시작 입력에는 UI 클릭음이 연결되어 있다.

### 스테이지 진행

- Tutorial 완료 후 약 2초 뒤 Stage1을 로드한다.
- Stage1, Stage2, Stage5는 적 전멸 후 Replay 상태로 진입한다.
- Replay 중 `N`을 누르면 다음 스테이지를 로드한다.
- 마지막 활성 스테이지인 Stage5 클리어 후 `EndingScene`으로 이동한다.

### 사망·재시작

- 일반 Stage에서 플레이어가 사망하면 `PlayerDead` 상태가 된다.
- `R`은 현재 씬을 다시 로드한다.
- Tutorial의 DEADLINE 포위전 사망은 체크포인트 복구 흐름을 사용한다.

### 종료

- `EndingScene`에서 `N`을 누르면 `MainScene`으로 복귀한다.
- 저장·엔딩 선택·메타 진행은 구현되지 않았다.

근거: `Assets/_Project/Scripts/Level/StageSceneFlow.cs`, `StageController.cs`, `UI/MainMenuController.cs`, `UI/EndingSceneController.cs`, `ProjectSettings/EditorBuildSettings.asset`

## 4. 조작 및 입력 체계

| 행동 | 입력 | 실제 연결 | 상태 |
|---|---|---|---|
| 이동 | WASD | `PlayerMovement` | 구현 완료 |
| 조준 | 마우스 위치 | `PlayerAim` | 구현 완료 |
| 발사 | 마우스 왼쪽 버튼 | `PlayerCombat`, `WeaponController` | 구현 완료 |
| 투척 | 마우스 오른쪽 버튼 | `WeaponController` | 구현 완료 |
| 대시 | Space | `PlayerDash` | 구현 완료 |
| DEADLINE | Q | `DeadlineController` | 구현 완료 |
| 상호작용·픽업 | E | `WeaponPickup`, Tutorial 상호작용 | 구현 완료 |
| 재시작 | R | 현재 씬 재로드 | 구현 완료 |
| 다음 단계 | N | 다음 씬 또는 MainScene | 구현 완료 |

- 현재 제어 스킴은 `Keyboard&Mouse` 하나다.
- 게임패드와 리바인딩은 구현되지 않았다.
- `V` 전체 시야 전환 입력은 현재 없다.

근거: `Assets/_Project/Input/PlayerControls.inputactions`

## 5. 플레이어 시스템

### 이동

- 코드 기본 이동 속도: `6`
- Rigidbody 기반 이동
- Stage5·Stage6에서는 `NavMeshGroundMovement`로 NavMesh의 높이 차를 반영
- 이동 자체에 `WorldDeltaTime`을 곱하지 않고, 입력 활동이 월드 시간 배율을 변화시킴

### 조준

- 카메라 포인터 광선을 플레이어 현재 Y 높이의 수평 평면에 투영한다.
- 총구에서 조준점 방향으로 수평 발사한다.
- Stage5 전경 Collider가 플레이어 조준을 방해하지 않도록 현재 높이 평면을 사용한다.

### 대시

| 수치 | 값 |
|---|---:|
| 대시 거리 | 3.5m |
| 대시 속도 | 22m/s |
| 지속 시간 | 0.16s |
| 쿨다운 | 0.8s |
| 대시 무적 | 적용 |

### 체력·사망

- 플레이어 최대 체력: `3`
- 체력이 0 이하가 되면 사망 이벤트 발생
- StageController 또는 Tutorial 체크포인트가 후속 흐름을 처리
- 체력 회복 시스템은 확인되지 않음

근거: `Assets/_Project/Scripts/Player/PlayerMovement.cs`, `PlayerAim.cs`, `PlayerDash.cs`, `PlayerHealth.cs`, `Level/NavMeshGroundMovement.cs`

## 6. 전투 시스템

### 전투 흐름

1. 적을 Vision 시스템으로 발견한다.
2. 조준과 이동으로 시간을 감속하거나 정지에 가깝게 만든다.
3. 무기 사거리와 상태에 맞춰 총기·근접·투척을 선택한다.
4. 대시로 회피하거나 위치를 재조정한다.
5. 적을 제거하면 Replay를 확인한다.

### 전투 판정

- 총기 투사체는 실제 `Weapon Muzzle`에서 생성된다.
- 플레이어 조준은 현재 높이의 수평면을 사용한다.
- 근접 공격은 사거리·부채꼴·시야 조건을 판정한다.
- Animator가 있는 경우 공격 타격 이벤트에서 피해를 처리한다.
- Animator가 없는 경우 호환용 즉시 판정 경로를 사용한다.
- 실제 마우스 조작의 명중감과 난이도는 확인 불가다.

## 7. 무기 시스템

| 무기 | 주요 수치 | 동작 | 상태 |
|---|---|---|---|
| 권총 | 탄창 8, 간격 0.24s, 피해 3, 속도 17, 1발, 지터 ±1.5°, 시드 101 | 반자동 | 구현 완료 |
| 자동소총 | 탄창 30, 간격 0.12s, 피해 3, 속도 16, 지터 ±1.5°, 시드 211 | 자동 연사, 적 점사 4발 | 구현 완료 |
| 샷건 | 탄창 6, 간격 0.75s, 펠릿 피해 1, 4펠릿, 18° 콘, 최대 14m | 반자동 산탄 | 구현 완료 |
| 근접 무기 | 간격 0.72s, 피해 3, 사거리 1.45m, 반각 35° | 근접 판정 | 구현 완료 |

### 무기 획득·교환

- E로 무기 픽업을 획득한다.
- 현재 장비가 있으면 교환 또는 드롭 흐름을 사용한다.
- 플레이어 투척 시 현재 장비를 비우고 `ThrownWeapon`을 생성한다.
- 적은 기절·무장 해제 시 무기와 잔탄을 떨어뜨릴 수 있다.
- 공중 무기 가로채기 경로가 코드와 프리팹에 존재한다.

### 미확인·미구현

- 재장전 입력과 재장전 로직은 확인되지 않았다.
- 장기 인벤토리와 탄약 경제는 구현되지 않았다.
- 손 그립·총구 축·투척 비행 시각의 수동 품질은 확인 불가다.

근거: `Assets/_Project/Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`, `MeleeWeapon.asset`, `Assets/_Project/Scripts/Combat/WeaponDefinition.cs`, `WeaponController.cs`, `WeaponPickup.cs`, `ThrownWeapon.cs`

## 8. 적 시스템

### 감지

- 감지 거리: `18m`
- 장애물 Raycast 기반 시야 판정
- 마지막으로 확인한 플레이어 위치를 기록

### 행동 상태

- 감지
- 추적
- 무기 탐색
- 조준
- 점사 발사
- 공격 준비
- 공격
- 쿨다운
- 기절
- 사망

### 원거리 적

- 선호 전투 거리는 약 `6~9m`
- 최대 사거리의 약 90%까지 접근
- 너무 가까우면 후퇴
- 조준 시간 약 `0.65 world s`
- 원거리 공격 쿨다운 약 `1.15 world s`
- 발사 전 경고선 표시

### 근접·비무장 적

- 비무장 공격 사거리 약 `1.2m`
- 근접 공격 준비·취소·쿨다운 상태를 사용
- 도달 가능한 무기 픽업을 NavMesh로 탐색
- 원거리 무기를 기본적으로 선호하고, 근접 무기가 충분히 가까우면 우선할 수 있음

근거: `Assets/_Project/Scripts/Enemies/EnemyPerception.cs`, `EnemyCombatant.cs`, `EnemyMotor.cs`, `EnemyHealth.cs`, `Enemies/EnemyWeaponDrop.cs`

## 9. 월드 시간 시스템

| 항목 | 현재 구현 |
|---|---|
| 최소 시간 배율 | 0.02 |
| 최대 시간 배율 | 1.0 |
| 보간 속도 | 8 |
| 시간 계산 | `unscaledDeltaTime × CurrentTimeScale` |
| 전역 Time.timeScale | 변경하지 않음 |
| 활동 입력 | 이동·조준 회전·펄스 |

### 시간 상태

- 활동량이 높을수록 시간 배율이 정상 속도에 가까워진다.
- 플레이어가 멈추고 조준 회전도 적으면 시간이 크게 느려진다.
- 하드 프리즈 토큰으로 월드 시간을 정지할 수 있다.
- `DEADLINE` 중에는 조준 회전만 최소 시간 배율까지 허용하고, 그 외에는 정지에 가깝게 유지한다.

근거: `Assets/_Project/Scripts/Time/WorldTimeController.cs`

## 10. DEADLINE 시스템

### 목적

시간이 느려진 상태에서 행동을 미리 준비하고, 이동을 통해 준비된 행동을 한 번에 실행하는 전투 개입 시스템이다.

### 규칙

| 규칙 | 값 |
|---|---:|
| 발동 입력 | Q Down |
| 최대 충전 | 2회 |
| 준비 행동 최대 | 2개 |
| 재무장 시간 | 0.35 world s |
| 이동 해제 기준 | 입력 크기 0.05 초과 |
| 초과 행동 피드백 | 0.18s 거절 피드백 |

### 상태 흐름

`Ready → Frozen → Armed → Released → Ready`

- Q를 누르면 충전 1회를 사용해 발동한다.
- 공격·행동을 최대 2개까지 준비한다.
- 세 번째 행동은 거절된다.
- 이동 입력이 기준치를 넘으면 준비된 행동을 실행하고 정상 시간으로 복귀한다.
- 사망·재시작·중단 시 상태를 초기화한다.

근거: `Assets/_Project/Scripts/Player/DeadlineController.cs`, `WorldTimeController.cs`, `UI/GameHud.cs`

## 11. 시야 및 시각 효과

### 일반 시야

| 항목 | 값 |
|---|---:|
| 시야각 | 60° |
| 시야 거리 | 12.5m |
| 근거리 원형 시야 | 반경 4m |
| 메시 세그먼트 | 96 |
| 장애물 레이어 | Layer 8 `VisionObstacle` |

- 원형 시야와 부채꼴 시야의 합집합으로 적을 표시한다.
- 장애물 Raycast가 시야를 차단한다.
- 런타임 Spot Light와 근거리 Point Light 프록시를 사용한다.
- Tutorial은 저장 씬에서 무제한 시야로 설정되어 있다.

### DEADLINE 시각 효과

- 진입 링·플래시
- 유지 중 청록색 틴트·비네트·노이즈
- 행동 노드 2개
- 초과 행동 거절 시 주황색 피드백
- 정상 해제 시 복원 링
- 셰이더 미지원 시 원본 화면으로 폴백

실제 목표 해상도에서의 시야 가독성과 시각 품질은 확인 불가다.

근거: `Assets/_Project/Scripts/Vision/VisionCone.cs`, `Time/WorldTimeVisualFeedback.cs`, `Time/DeadlineVisualFeedback.cs`, `Resources/Shaders/DeadlineScreenEffect.shader`

## 12. Replay 시스템

### 처리 흐름

1. 라이브 플레이 중 플레이어·적·픽업·시야 조명·생성 투사체를 기록한다.
2. 적 전멸 또는 플레이어 사망 시 라이브 전투를 비활성화한다.
3. 기록된 동적 프록시와 Animator 상태를 재생한다.
4. 암흑 시야와 리플레이 HUD를 표시한다.
5. `N` 또는 `R` 입력으로 다음 단계 또는 재시작을 선택한다.

### 구현 방식

- 기본 소스 샘플링: 20Hz
- 본 포즈 전체를 저장하지 않고 Animator 상태·Trigger·체크포인트로 재현
- 생성되는 투사체·투척물·짧은 VFX는 생성 시 등록
- 리플레이 시 ViewCone을 기록된 위치 기준으로 재계산
- 현재 `V` 전체 시야 토글은 없음

### 리스크

- `ReplayVisionPrototypeSmoke.log`: 실패 이력
- `ReplayVisionStage5Smoke.log`: 실패 이력
- `ReplayVisionStage6Smoke.log`: 통과 이력
- 전체 수동 시각 품질과 모든 스테이지 회귀는 확인 불가

근거: `Assets/_Project/Scripts/Replay/StageReplayController.cs`, `ReplayAnimationTrack.cs`, `ReplayMemoryStatistics.cs`

## 13. Tutorial

### 단계

1. TimeMovement
2. AimAndDash
3. Melee
4. Pistol
5. ThrowAndRecover
6. DeadlineApproach
7. Deadline
8. Complete

### 진행 조건

- 이동과 정지로 시간 배율 변화 확인
- 조준 회전 후 대시 및 입력 확인
- E로 근접 무기 획득 후 근접 표적 적중
- Pistol 획득 후 총기 표적 적중
- RMB 투척으로 적 기절·무장 해제·무기 드롭
- E로 공중 또는 바닥 무기 회수
- 4인 포위전에서 Q 발동
- 행동 2개 준비 후 이동 해제
- 성공 시 출구 개방 및 Stage1 전환

### 실패·재시작

- 일반 튜토리얼 구간의 R은 처음부터 재시작한다.
- DEADLINE 포위전 사망 시 체크포인트에서 복구한다.
- 복구 시 플레이어 체력, 적 4명, 권총 탄약, DEADLINE 충전, 출구 상태를 초기화한다.

근거: `Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`, `TutorialGate.cs`, `TutorialWeaponDispenser.cs`, `TutorialPlayModeSmokeTest.cs`, `Scenes/Tutorial.unity`

## 14. 스테이지 구조

| 씬 | 역할 | 현재 상태 |
|---|---|---|
| MainScene | 타이틀·게임 시작 | 구현 완료 |
| Tutorial | 7단계 조작·전투 학습 | 구현 완료 |
| Stage1 | 밝은 조명 프로필의 전투 방 | 구현 완료 |
| Stage2 | 어두운 조명·제한 시야 전투 방 | 구현 완료 |
| Stage3 Afterimage Club | 나이트클럽 전투 공간 | 부분 구현 |
| Stage4 Last Call Rooftop | 옥상 바 전투 공간 | 부분 구현 |
| Stage5 Undertow Dive | 다이브 바·단상·고저차 전투 공간 | 부분 구현 |
| Stage6 Neon Overlook | 다층 네온 옥상 전투 공간 | 부분 구현 |
| EndingScene | 엔딩·MainScene 복귀 | 구현 완료 |

### 파일명·검증 경로 불일치

- 실제 저장 씬: `Assets/_Project/Scenes/Stage3_NoUse.unity`
- 실제 저장 씬: `Assets/_Project/Scenes/Stage_NoUse.unity` — 씬 내부 이름은 Stage 4
- Builder와 Smoke Test의 일부 참조: `Assets/_Project/Scenes/Stage3.unity`, `Stage4.unity`
- 현재 파일명 기준의 최신 재생성·스모크 결과는 확인 불가

### Stage5·Stage6

- 전용 NavMesh 에셋 보유
- 계단·단상·플랫폼의 NavMesh 높이 이동 지원
- Stage6 스모크에서 적 5명까지 완전 경로 `5/5` 기록
- Stage6 성능 예산 코드는 존재하지만 1920×1080 60FPS 달성은 확인 불가

근거: `ProjectSettings/EditorBuildSettings.asset`, `Scenes/Stage1.unity`, `Stage2.unity`, `Stage3_NoUse.unity`, `Stage_NoUse.unity`, `Stage5.unity`, `Stage6.unity`, `Stage5Navigation.asset`, `Stage6Navigation.asset`

## 15. UI/HUD

### GameHud

- IMGUI 기반
- 좌상단 상태 패널: `330×178`
- 좌하단 체력·무기·탄약 패널: `330×76`
- 중앙 상단: Replay 결과·조작 안내·활성 DEADLINE 안내
- 표시 정보: 적 수, 실시간/월드/Replay 시간, 대시, 체력, 무기, 탄약, DEADLINE 충전
- 조작 안내: LMB, RMB, Q, Space, E, R, N

### 미검증

- 실제 Game View에서의 해상도별 줄바꿈
- HUD와 시야·전투 화면의 겹침 여부
- 최종 UI 폴리시와 접근성

근거: `Assets/_Project/Scripts/UI/GameHud.cs`, `TutorialHud.cs`

## 16. 사운드 및 음악

### 현재 구현

- 영속 `SoundManager`
- 씬별 Main/Tutorial/Stage/Ending BGM 선택
- BGM 크로스페이드
- Stage BGM 기본 출력 `0.35`
- 비스테이지 BGM 기본 출력 `0.55`
- DEADLINE 덕킹 배율 `0.4`
- 총기 발사음
- 주먹·야구방망이 스윙/적중음
- 무기 투척음
- UI 클릭음
- DEADLINE 진입·시간 왜곡·해제음

### 상태

- 런타임 연결: 구현 완료
- BGM 선택·출력 스모크: 기존 로그 확인
- AudioMixer: 확인 불가
- 사용자 음량 설정: 계획 필요
- 실제 청감·공간감·밸런스: 확인 불가

근거: `Assets/_Project/Scripts/Audio/SoundManager.cs`, `SoundLibrary.cs`, `Resources/DeltatimeSoundLibrary.asset`, `Audio/BGM`, `Audio/SFX`, `SoundManagerStageBgmSmoke.log`

## 17. 카메라·애니메이션·연출

### 카메라

- 원근 탑다운 시점
- 플레이어 추적
- Stage5/6 FOV 약 `48°`
- 약 60° 하향 구도
- NavMesh 기반 화면 경계

### 애니메이션

- Humanoid Animator
- 방향 이동 Blend Tree
- Roll
- 근접 상체 공격
- 장비별 Controller/Override
- 무기 손 장착 프레젠터
- Replay에서는 상태·이벤트·체크포인트 기반 복원

### 미완성·미검증

- 권총 전용 사격 상체 클립
- 피격·사망·투척·획득 전용 애니메이션
- 손가락 그립과 메시 관통
- 실제 공격 프레임 체감

근거: `Assets/_Project/Scripts/Visuals/CharacterAnimationController.cs`, `MeleeAttackImpactBehaviour.cs`, `WeaponVisualPresenter.cs`, `Assets/_Project/Animation/DeltatimeRollInPlace.anim`

## 18. 데이터·에셋·에디터 도구

### 주요 데이터

- 입력: `Assets/_Project/Input/PlayerControls.inputactions`
- 무기: `Pistol.asset`, `AutomaticRifle.asset`, `Shotgun.asset`, `MeleeWeapon.asset`
- 씬: `Assets/_Project/Scenes/`
- NavMesh: `*Navigation.asset`
- 사운드 라이브러리: `Assets/_Project/Resources/DeltatimeSoundLibrary.asset`

### 에디터 도구

- `PrototypeSceneBuilder`
- `TutorialSceneBuilder`
- `Stage3SceneBuilder`
- `Stage4SceneBuilder`
- `Stage5SceneBuilder`
- `Stage6SceneBuilder`
- 각종 정적 검증 및 PlayMode Smoke Test

Builder는 씬·프리팹·머티리얼·무기 데이터·빌드 설정을 재생성할 수 있으므로 이번 역기획에서는 실행하지 않았다.

## 19. 테스트 및 검증 현황

| 검증 대상 | 기존 근거 | 결과 | 이번 작업에서 실행 |
|---|---|---|---|
| Tutorial | `TutorialSmoke.log` | PlayMode 스모크 통과 | 미실행 |
| Stage5 | `Stage5FinalSmoke.log` | PlayMode 스모크 통과 | 미실행 |
| Stage6 | `Stage6Smoke.log` | NavMesh 경로 5/5 포함 통과 | 미실행 |
| Replay Animator | `ReplayAnimatorPlayModeFinal5.log` | 통과 | 미실행 |
| DEADLINE 화면 효과 | `DeadlineVisualFeedbackSmoke.log` | 통과 | 미실행 |
| SoundManager | `SoundManagerStageBgmSmoke.log` | BGM 선택·출력 통과 | 미실행 |
| Replay Vision Prototype | `ReplayVisionPrototypeSmoke.log` | 실패 이력 | 미실행 |
| Replay Vision Stage5 | `ReplayVisionStage5Smoke.log` | 실패 이력 | 미실행 |
| Replay Vision Stage6 | `ReplayVisionStage6Smoke.log` | 통과 이력 | 미실행 |
| Stage6 성능 | `Stage6PerformanceBenchmark.log` | 1080p 판정 불가 | 미실행 |

> ⚠️ 기존 로그는 과거 실행 결과다. 이번 문서 변경의 새 테스트 결과로 표현하지 않는다.

## 20. 기능별 구현 상태

| 기능 | 상태 | 핵심 리스크 |
|---|---|---|
| 전체 진행·입력·플레이어·전투·무기·적 | 구현 완료 | 자동 검증과 실제 체감의 차이 |
| 월드 시간·DEADLINE·시야 | 구현 완료 | 최종 시각 가독성 미검증 |
| Replay | 부분 구현 | Prototype/Stage5 시야 실패 이력 |
| Stage3/4 | 부분 구현 | 저장 파일명과 Builder/Smoke 경로 불일치 |
| Stage5/6 | 부분 구현 | 콘텐츠는 있으나 현재 진행에서 제외 |
| HUD·사운드·카메라 | 구현 완료 | 실제 화면·청감 미검증 |
| 애니메이션 | 부분 구현 | 전용 사격·피격·사망 애니메이션 부족 |
| Stage6 성능 | 부분 구현 | 1920×1080 60FPS 확인 불가 |
| 저장·퀘스트·인벤토리 | 미구현 | 장기 진행 설계 없음 |
| 게임패드·리바인딩·사용자 음량 | 계획 필요 | 목표 플랫폼·제품형 설정 미정 |
| 실제 전 과정 체감 | 확인 불가 | 수동 입력·청감·가독성 미검증 |

## 21. 기존 문서와 현재 구현의 차이

| 기존 문서 내용 | 현재 구현 | 판단 |
|---|---|---|
| 사운드 전면 미구현 | `SoundManager`, BGM/SFX 에셋, 관련 스모크 존재 | 현재 문서에서는 런타임 오디오 구현 완료로 보정 |
| Stage3/4를 `Stage3.unity`/`Stage4.unity`로 표기 | 실제 저장 파일은 `Stage3_NoUse.unity`/`Stage_NoUse.unity` | 최신 재생성·검증 경로 확인 불가 |
| Stage6을 일반 진행에 포함하는 과거 서술 | 현재 진행은 Stage1→Stage2→Stage5 | 현재 Build Settings와 `StageSceneFlow`를 우선 |
| Stage2/Tutorial 캐릭터 시각 미구현이라는 과거 기록 | 현재 씬과 Tutorial 스모크에 Humanoid 시각·Animator 연결 | 최신 상태는 기본 캐릭터 연출 구현, 전용 애니메이션은 부분 구현 |
| 과거 스모크 통과 기록 | 최신 실패 로그와 파일명 불일치도 존재 | 실행 시점별 근거로만 사용 |

## 22. 미구현·확인 불가·후속 과제

### 미구현

- 재장전
- 저장/로드
- 퀘스트
- 일반 인벤토리·성장

### 확인 불가

- 실제 키보드·마우스 전 과정 체감
- 최종 해상도 HUD·시야 가독성
- 손 그립·메시 관통·Replay 시각 품질
- 사운드 청감과 믹싱 밸런스
- 1920×1080 Stage6 성능
- 최종 서사와 상업적 장르 의도

### 후속 기획 과제

1. Stage3/4 씬 파일명과 Builder/Smoke 경로를 통일한다.
2. Stage3/4/6을 본편 진행에 편입할지 결정한다.
3. 재장전·탄약 경제·인벤토리 정책을 설계한다.
4. Replay Prototype/Stage5 실패 원인을 최신 저장 씬 기준으로 재현한다.
5. 독립 플레이어의 1920×1080 환경에서 Stage6 성능을 재측정한다.
6. 실제 플레이 테스트로 Tutorial 동선·문구·실패 복구를 검증한다.
7. AudioMixer·사용자 음량·게임패드·리바인딩 지원 여부를 결정한다.

## 23. 근거 파일

### 프로젝트·설정

- `AGENTS.md`
- `ProjectDeltatime/ProjectSettings/ProjectVersion.txt`
- `ProjectDeltatime/ProjectSettings/EditorBuildSettings.asset`
- `ProjectDeltatime/Assets/_Project/Input/PlayerControls.inputactions`

### 핵심 시스템

- `ProjectDeltatime/Assets/_Project/Scripts/Level/StageSceneFlow.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Level/StageController.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerMovement.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerAim.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerDash.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Player/PlayerHealth.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Time/WorldTimeController.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Replay/StageReplayController.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Vision/VisionCone.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Audio/SoundManager.cs`

### 전투·튜토리얼·UI

- `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponDefinition.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Combat/WeaponController.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Combat/ThrownWeapon.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyCombatant.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Enemies/EnemyPerception.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/Tutorial/TutorialDirector.cs`
- `ProjectDeltatime/Assets/_Project/Scripts/UI/GameHud.cs`

### 씬·데이터

- `ProjectDeltatime/Assets/_Project/Scenes/MainScene.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Tutorial.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage1.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage2.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage3_NoUse.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage_NoUse.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage5.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/Stage6.unity`
- `ProjectDeltatime/Assets/_Project/Scenes/EndingScene.unity`
- `ProjectDeltatime/Assets/_Project/Pistol.asset`
- `ProjectDeltatime/Assets/_Project/AutomaticRifle.asset`
- `ProjectDeltatime/Assets/_Project/Shotgun.asset`
- `ProjectDeltatime/Assets/_Project/MeleeWeapon.asset`

## 원문 및 변경 기록

- 원문 기획서: `docs/PROJECT_DESIGN_DOCUMENT.md`
- 기능 변경 기록: `docs/FEATURE_CHANGELOG.md`

이 Notion용 문서는 원문 기획서의 최신 구현 기준선을 복사·정리한 입력본이다. 변경 이력과 세부 근거가 필요하면 원문 기획서와 기능 변경 기록을 함께 확인한다.
