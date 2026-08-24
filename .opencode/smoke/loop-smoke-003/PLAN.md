# Plan

## Goal
`artifact.txt`를 단계적으로 생성하고 확장하여 각 단계의 정확한 줄 내용과 순서를 검증하는 스모크 테스트를 수행한다.

## Tasks

- [ ] T1: alpha 아티팩트 생성
  - 변경 범위: `.opencode/smoke/loop-smoke-003/artifact.txt`를 새로 생성한다.
  - 수용 기준: 파일 내용이 정확히 `alpha` 한 줄이며 줄 끝에 개행이 있다.
  - 검증 명령: `printf 'alpha\n' | cmp -s - .opencode/smoke/loop-smoke-003/artifact.txt`
  - 관련 파일: `.opencode/smoke/loop-smoke-003/artifact.txt`

- [ ] T2: beta 줄 추가
  - 변경 범위: T1에서 생성한 `.opencode/smoke/loop-smoke-003/artifact.txt`에 두 번째 줄 `beta`를 추가한다.
  - 수용 기준: 최종 파일 내용이 정확히 첫 번째 줄 `alpha`, 두 번째 줄 `beta`이며 각 줄 끝에 개행이 있다.
  - 검증 명령: `printf 'alpha\nbeta\n' | cmp -s - .opencode/smoke/loop-smoke-003/artifact.txt`
  - 관련 파일: `.opencode/smoke/loop-smoke-003/artifact.txt`
