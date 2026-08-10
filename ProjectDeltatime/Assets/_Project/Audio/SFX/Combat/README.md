# 전투 공용 효과음

상태: **구현 완료**. `SoundManager`와 `DeltatimeSoundLibrary`를 통해 런타임 전투 이벤트에 연결되어 있다.

## 포함 파일과 재생 규칙

| 이벤트 | Unity 에셋 | 원본 팩 | 재생 규칙 |
| --- | --- | --- | --- |
| 주먹 적중 | `Impact/SFX_Punch_Hit_01.ogg`, `02.ogg` | Kenney Impact Sounds 1.0 | 실제 적에게 피해가 적용된 위치에서 변형 한 개 재생 |
| 야구방망이 휘두름 | `Swing/SFX_Bat_Swing_01.wav`, `02.wav` | OpenGameArt Swishes Sound Pack (artisticdude, CC0) | 유효한 방망이 공격 시작 시 공격자 위치에서 변형 한 개 재생. 대상 부재·사거리·시야 판정 실패에도 재생 |
| 야구방망이 적중 | `Impact/SFX_Bat_Hit_01.ogg`, `02.ogg` | Kenney Impact Sounds 1.0 | 실제 적에게 피해가 적용된 위치에서 변형 한 개 재생 |
| 무기 투척 | `SFX_Weapon_Throw.ogg` | Kenney RPG Audio | 투척물 생성 성공 시 총구 위치에서 재생 |

방망이 휘두름·적중·투척은 3D 거리 감쇠와 작은 피치 변형을 사용한다. 방망이 휘두름은 `MeleeAttackExecution`의 일반 애니메이션 경로와 `WeaponController`의 애니메이터 없는 즉시 판정 경로 모두에서 한 번만 재생되며, 적중 시에는 기존 적중음이 별도로 겹친다. 무기 획득·교체·교환과 예약된 적의 픽업에는 플레이어 피드백음을 재생하지 않는다. `SFX_Weapon_Pickup.ogg`는 현재 런타임 라이브러리에 연결하지 않으며, 추후 별도 청감 검토 뒤에만 재도입한다.

## 출처 및 라이선스

주먹·방망이 적중·투척 원본은 로컬 `C:\Users\HuiYong\Downloads\효과음`의 `kenney_impact-sounds`, `kenney_rpg-audio`, `kenney_digital-audio`에서 선별했다. 각 팩에 포함된 `License.txt`는 Creative Commons Zero(CC0)이며 개인·교육·상업 프로젝트 사용을 허용하고, Kenney 또는 `www.kenney.nl` 표기는 선택 사항이라고 명시한다. 방망이 휘두름 원본은 [OpenGameArt Swishes Sound Pack](https://opengameart.org/content/swishes-sound-pack)의 `swish-5.wav`, `swish-6.wav`이며, 해당 팩도 CC0으로 명시되어 있다.

연결 근거: `Assets/_Project/Scripts/Combat/MeleeAttackExecution.cs`, `Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `Assets/_Project/Scripts/Combat/WeaponController.cs`, `Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `Assets/_Project/Scripts/Audio/SoundManager.cs`, `Assets/_Project/Resources/DeltatimeSoundLibrary.asset`.
