# 무기 발사음 에셋

상태: **구현 완료**. `SoundManager`와 `DeltatimeSoundLibrary`를 통해 런타임 발사 경로에 연결되어 있다.

## 포함 파일

| 게임 무기 | Unity 에셋 | 원본 녹음 |
| --- | --- | --- |
| Tactical Pistol | `Pistol/SFX_Pistol_Fire_01.wav` | `Walther PPQ/X_39P.wav` |
| Tactical Pistol | `Pistol/SFX_Pistol_Fire_02.wav` | `Bersa/F_47P.wav` |
| Assault Rifle | `Rifle/SFX_Rifle_Fire_01.wav` | `AR-15/D_32P.wav` |
| Assault Rifle | `Rifle/SFX_Rifle_Fire_02.wav` | `AK-47/C_28P.wav` |
| Pump Shotgun | `Shotgun/SFX_Shotgun_Fire_01.wav` | `Nova/O_21P.wav` |
| Pump Shotgun | `Shotgun/SFX_Shotgun_Fire_02.wav` | `Model 12/K_22P.wav` |
| Pump Shotgun | `Shotgun/SFX_Shotgun_Fire_03.wav` | `CD/H_21P.wav` |

## 가공 기준

- 각 원본에 포함된 여러 발 중 첫 발의 피크를 기준으로 분리했다.
- 96 kHz/24-bit 스테레오 원본을 48 kHz/24-bit 스테레오 PCM WAV로 변환했다.
- 발사 전 여유는 권총 70 ms, 소총 50 ms, 산탄총 80 ms로 남겼다. 총 길이는 각각 0.520초, 0.350초, 0.930초이다.
- 전체 이득을 약 -2.5 dB로 낮추고 끝 20 ms에 페이드아웃을 적용해 믹서 헤드룸과 무음 종료를 확보했다.

## 출처 및 라이선스

원본은 로컬 `C:\Users\HuiYong\Downloads\Prepared SFX Library`의 **The Free Firearm Sound Library**에서 선별했다. 배포 페이지는 [OpenGameArt](https://opengameart.org/content/the-free-firearm-sound-library)이며, 해당 라이브러리는 CC0로 표기되어 있다.

## Unity 임포트 권장값

- `Load Type`: Decompress On Load (짧은 플레이어 발사음)
- `Compression Format`: ADPCM 또는 Vorbis, 품질 비교 후 결정
- `Force To Mono`: 해제 (현재 원본 공간감 유지). 3D 공간화가 필요할 때만 재검토
- `Normalize`: 해제 (에셋 간 공통 게인으로 맞춘 상태)

런타임은 무기별 배열에서 무작위로 하나를 골라 성공한 발사 1회당 한 번 재생한다. 샷건도 펠릿마다 반복하지 않으며, 원본의 버스트 녹음은 현재 발사 구조와 겹치므로 사용하지 않는다. 재생 위치는 실제 `WeaponController.Muzzle`이고 3D 거리 감쇠와 작은 피치 변형을 사용한다.

연결 근거: `Assets/_Project/Scripts/Combat/WeaponController.cs`, `Assets/_Project/Scripts/Audio/SoundManager.cs`, `Assets/_Project/Resources/DeltatimeSoundLibrary.asset`.
