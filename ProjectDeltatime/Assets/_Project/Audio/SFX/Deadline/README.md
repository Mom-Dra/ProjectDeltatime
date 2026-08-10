# DEADLINE 효과음 에셋

상태: **구현 완료**. `DeadlineController.Activated`·`Released`와 영속 `SoundManager`에 런타임 연결되어 있다.

## 이벤트 매핑

| 이벤트 | Unity 에셋 | 원본 파일 | 재생 시점 |
| --- | --- | --- | --- |
| DEADLINE 진입 충격 | `SFX_Deadline_Enter_Impact.mp3` | `black_kumizhi-low-thumpy-kick-reverb-hit-494833.mp3` | Q 입력으로 `DeadlineController.IsActive`가 `false → true`가 되는 즉시 |
| DEADLINE 시간 왜곡 | `SFX_Deadline_Enter_TimeWarp.mp3` | `chrysalyn-cinematic-whoosh-transition-impact-562431.mp3` | 진입 충격과 동시에, 약 -8 dB 낮게 겹쳐 재생 |
| DEADLINE 해제 | `SFX_Deadline_Release.mp3` | `dragon-studio-futuristic-transition-499653.mp3` | `DeadlineController.Released` 시점 |
| DEADLINE 해제 변형 | `SFX_Deadline_Release2.mp3` | 로컬 추가 파일, 원본 확인 필요 | `DeadlineController.Released` 시점에 기본 해제음과 무작위 선택 |

## Unity 재생 기준

- 모두 플레이어·HUD에 귀속된 2D 전역 효과음으로 재생한다. 월드 위치를 따르는 3D 감쇠는 사용하지 않는다.
- 진입 충격은 0초에 재생해 Q 입력의 즉각성을 보장한다. 시간 왜곡은 별도 `AudioSource`에서 동시에 한 번만 재생해 충격음을 가리지 않게 한다.
- 해제음은 성공·실패와 관계없이 월드 하드 프리즈가 풀리는 순간 한 번만 재생한다. Tutorial 성공 전용 보상음은 별도 에셋으로 추가한다.
- DEADLINE 활성 중에는 `SoundManager`가 BGM을 볼륨 페이드로 약 -8 dB 덕킹한다. SFX 재생 속도는 월드 시간에 연동하지 않는다.

## 구현 연결 지점

`ProjectDeltatime/Assets/_Project/Scripts/Player/DeadlineController.cs`의 `Activated`·`Released` 이벤트를 `SoundManager`가 씬 로드 때 자동 구독한다. 진입 시 충격음과 단발 시간 왜곡음을 시작하고, 해제 시 시간 왜곡 재생이 남아 있으면 멈춘 뒤 해제 변형 한 개를 재생한다.

## 출처 및 라이선스

원본은 로컬 `C:\Users\HuiYong\Downloads\효과음\deadLine`에서 복사했다. 아래 Pixabay 원본 페이지는 Pixabay Content License에 따라 무료 사용으로 표기되어 있다. 실제 출시 전에는 적용 시점의 라이선스 조건을 다시 확인한다.

- [Low Thumpy Kick Reverb Hit — Black_Kumizhi](https://pixabay.com/sound-effects/technology-low-thumpy-kick-reverb-hit-494833/)
- [Cinematic Whoosh Transition Impact — Chrysalyn](https://pixabay.com/users/chrysalyn-55695220/)
- [Futuristic Transition — DRAGON-STUDIO](https://pixabay.com/sound-effects/technology-futuristic-transition-499653/)

`SFX_Deadline_Release2.mp3`는 로컬에 추가된 파일이라 원본 페이지와 라이선스가 **확인 불가**다. 배포 전 출처를 확인해야 한다.
