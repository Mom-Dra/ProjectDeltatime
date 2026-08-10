# 전투 공용 효과음

상태: **구현 완료**. `SoundManager`와 `DeltatimeSoundLibrary`를 통해 런타임 전투 이벤트에 연결되어 있다.

## 포함 파일과 재생 규칙

| 이벤트 | Unity 에셋 | 원본 팩 | 재생 규칙 |
| --- | --- | --- | --- |
| 주먹 적중 | `Impact/SFX_Punch_Hit_01.ogg`, `02.ogg` | Kenney Impact Sounds 1.0 | 실제 적에게 피해가 적용된 위치에서 변형 한 개 재생 |
| 야구방망이 적중 | `Impact/SFX_Bat_Hit_01.ogg`, `02.ogg` | Kenney Impact Sounds 1.0 | 실제 적에게 피해가 적용된 위치에서 변형 한 개 재생 |
| 무기 투척 | `SFX_Weapon_Throw.ogg` | Kenney RPG Audio | 투척물 생성 성공 시 총구 위치에서 재생 |
| 무기 획득·교환 | `SFX_Weapon_Pickup.ogg` | Kenney Digital Audio | 플레이어의 획득·교환 성공 시 전역 재생 |

적중·투척은 3D 거리 감쇠와 작은 피치 변형을 사용한다. 빗나간 근접 공격과 예약된 적의 픽업에는 플레이어 피드백음을 재생하지 않는다.

## 출처 및 라이선스

원본은 로컬 `C:\Users\HuiYong\Downloads\효과음`의 `kenney_impact-sounds`, `kenney_rpg-audio`, `kenney_digital-audio`에서 선별했다. 각 팩에 포함된 `License.txt`는 Creative Commons Zero(CC0)이며 개인·교육·상업 프로젝트 사용을 허용하고, Kenney 또는 `www.kenney.nl` 표기는 선택 사항이라고 명시한다.

연결 근거: `Assets/_Project/Scripts/Combat/MeleeAttackResolver.cs`, `Assets/_Project/Scripts/Combat/WeaponController.cs`, `Assets/_Project/Scripts/Combat/WeaponPickup.cs`, `Assets/_Project/Scripts/Audio/SoundManager.cs`, `Assets/_Project/Resources/DeltatimeSoundLibrary.asset`.
