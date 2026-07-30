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
