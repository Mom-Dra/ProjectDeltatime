# BGM 에셋

상태: **구현 완료**. `SoundManager`와 `DeltatimeSoundLibrary`가 씬별 런타임 재생을 자동 처리한다.

## 씬 매핑

| 용도 | Unity 에셋 | 원본 파일 | 길이 | 반복 |
| --- | --- | --- | ---: | --- |
| 메인 메뉴 | `BGM_MainMenu.mp3` | `sector_MainScene.mp3` | 약 56.9초 | 사용 |
| Tutorial | `BGM_Tutorial.mp3` | `pulse_tutorial.mp3` | 약 71.1초 | 사용 |
| Stage1~Stage6 | `BGM_Stage_Action.mp3` | `ruskerdax_-_savage_ambush_Stage.mp3` | 약 121.9초 | 사용 |
| 엔딩·크레딧 | `BGM_Ending.mp3` | `title_EndingScene.mp3` | 약 425.6초 | 사용 안 함 |

## Unity 임포트 기준

- 모든 BGM은 `Load Type: Streaming`으로 설정해 장시간 엔딩 트랙도 메모리에 한 번에 올리지 않는다.
- `3D Sound: Off`, `Force To Mono: Off`로 전역 스테레오 음악으로 재생한다.
- `Preload Audio Data: Off`를 사용하고 씬 전환 또는 재생 직전에 로드한다.
- 메인 메뉴·Tutorial·Stage 곡은 `AudioSource.loop`를 켜고, 씬 전환 시 0.25초 크로스페이드로 교체한다. Stage 사이에서는 같은 곡을 이어 재생한다. 엔딩은 loop를 끄고 한 번만 재생한다.
- DEADLINE 발동 중에는 `SoundManager`가 BGM 볼륨을 약 -8 dB 덕킹하고, 하드 프리즈가 풀리면 원래 볼륨으로 복귀한다.

## 런타임 연결

- 라이브러리: `Assets/_Project/Resources/DeltatimeSoundLibrary.asset`
- 재생 관리자: `Assets/_Project/Scripts/Audio/SoundManager.cs`
- 씬 매핑: `MainScene`, `Tutorial`, `Stage*`, `EndingScene`·`Ending`·`Credits`

## 출처

원본은 로컬 `C:\Users\HuiYong\Downloads\bgm`에서 복사했다. `ruskerdax_-_savage_ambush_Stage.mp3`의 원본 계열은 OpenGameArt의 [Savage Ambush](https://opengameart.org/content/savage-ambush)에서 확인할 수 있다. 나머지 파일은 이 프로젝트에 제공된 로컬 파일명만으로 원본 페이지·라이선스를 확인할 수 없으므로, 배포 전 라이선스를 별도로 확인한다.
