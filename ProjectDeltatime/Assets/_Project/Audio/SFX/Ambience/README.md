# 월드 시간 환경음 에셋

상태: **구현 완료**. 진행 맵의 `WorldTimeAmbientAnchor`가 전용 3D 루프로 재생하며 월드 시간 배율에 맞춰 피치·로우패스·볼륨을 조절한다.

## 에셋 매핑

| 용도 | Unity 에셋 | 원본 | 재생 정책 |
| --- | --- | --- | --- |
| 산업용 환풍기 루프 | `SFX_WorldTime_IndustrialFan_Loop.ogg` | `jadeallencook-large-industrial-fan-running-constantly-in-warehouse-environment-339216.mp3` | Tutorial·Stage1·Stage2·Stage5의 환경 기준점에서 3D 반복 |

## 가공 및 Unity 재생 기준

- 원본 MP3를 디코딩해 모노로 변환하고 DC 오프셋을 제거했다.
- 원본 앞뒤 0.25초를 equal-power 방식으로 교차 혼합하고 중복 구간을 제거해 약 27초의 이음새 없는 OGG Vorbis 루프로 만들었다.
- 가공 전 PCM 피크를 -3 dBFS로 정규화했다.
- 가공 결과가 이미 모노이므로 Unity의 `Force To Mono`와 추가 정규화는 끄고, 가공한 -3 dBFS 피크를 보존한다.
- `AudioSource`는 `Spatial Blend 1`, `Doppler 0`, 로그 감쇠, 최소 거리 `2.5m`, 최대 거리 `18m`, `Play On Awake Off`를 사용한다.
- 평상시 피치는 `0.45 → 1.0`, 로우패스는 `500 → 16,000Hz`, 볼륨은 `0.22 × sqrt(월드 시간 배율)`로 변한다. `DEADLINE` 하드 프리즈에서는 비스케일 시간 0.15초 동안 먹먹해진 뒤 무음이 된다.
- Master·SFX 사용자 배율만 적용하며 BGM·전투음·UI음의 믹스와 필터는 변경하지 않는다.
- 환풍기 정적 외함 루트는 `ReplayExcluded`, 분리된 날개 Transform은 `ReplayIncluded`다. 날개만 기존 정규화 리플레이 시간축의 Renderer 프록시로 기록·재생하며, 라이브 환경음은 리플레이 진입 시 즉시 정지한다.

## 출처 및 라이선스

- 원본: [Large Industrial Fan Running Constantly in Warehouse Environment — jadeallencook](https://pixabay.com/sound-effects/film-special-effects-large-industrial-fan-running-constantly-in-warehouse-environment-339216/)
- 라이선스: [Pixabay Content License](https://pixabay.com/service/license-summary/)
- 원본 페이지는 무료 사용과 수정 허용을 표기한다. 저장소에는 게임용으로 가공한 OGG만 포함하며, 실제 출시 전에는 적용 시점의 라이선스 조건을 다시 확인한다.

## 구현 근거

- `Assets/_Project/Scripts/Time/WorldTimeAmbientAnchor.cs`
- `Assets/_Project/Prefabs/Time/WorldTimeAmbientFan.prefab`
- `Assets/_Project/Scripts/Editor/WorldTimeAmbientSceneBuilder.cs`
- `Assets/_Project/Scripts/Replay/ReplayIncluded.cs`
- `Assets/_Project/Scripts/Replay/StageReplayController.cs`

## 검증 결과

- **구현 완료**: Unity 6000.1.13f1 임포트에서 48kHz 모노 클립, `Force To Mono Off`, 추가 정규화 Off, Compressed In Memory, Vorbis 품질 `0.7`, Preload를 확인했다.
- **구현 완료**: 전용 씬 검증이 Tutorial 3개, Stage1·Stage2·Stage5 각 2개의 클립 참조, 3D 설정, 로우패스, Collider 비활성, 루트 `ReplayExcluded`와 날개 단일 `ReplayIncluded`를 통과했다.
- **구현 완료**: EditMode 25/25와 PlayMode 3/3이 시간 배율별 매핑 순서, 0.15초 무음, 회전·복원·비활성화 정지와 포함/제외 계층 우선순위, 리플레이 날개 프록시 회전을 통과했다. SoundManager, 리플레이 시간축, 현재 진행 맵 대상 DEADLINE 시각 스모크도 통과했다.
- **구현 완료**: Stage2 전용 리플레이 스모크에서 두 환풍기의 라이브 날개 숨김, 프록시 활성, 환경음 정지와 두 재생 시점 사이 71.960도 회전을 확인했다. 조명을 보완한 두 근접 캡처에서 팬 가시성을 직접 확인했다.
- **부분 구현**: 기존 범용 Stage2 Replay 스모크는 환풍기 검사 전에 애니메이터 컨트롤러 변경 이벤트 기대값 2/실제값 1이라는 별도 기준선에서 실패했다.
- **확인 불가**: 실제 스피커·헤드폰에서 루프 이음새, 공간감, BGM·전투음 대비, `정지 → 이동 → DEADLINE → 해제`의 최종 청감은 수동 검증이 필요하다.
- **확인 불가**: 실제 사용자 조작 리플레이에서 외함·날개 화면 겹침과 재생 체감은 수동 검증이 필요하다.
