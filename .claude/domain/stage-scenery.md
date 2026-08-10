# 스테이지 배경 — 타일맵과 그림 파이프라인

스테이지의 «가게» 를 무엇이 그리는가, 그리고 원본 그림이 애셋이 되기까지 무엇이 자동으로
정해지는가. 게임 규칙은 하나도 담지 않는다 — 여기 있는 것은 전부 화면 이야기다.

---

## 1. 배경은 타일맵이 그린다 (M7)

한 장짜리 배경 스프라이트(`bg-wall`)를 늘려 쓰던 것을 **1 유닛 = 32 픽셀 타일**로 바꿨다.
`Stage01` 의 `Stage/Tiles` 아래에 판이 넷 있다.

| 판 | 정렬 순서 | y 오프셋 | 담는 것 |
|---|---|---|---|
| `Floor` | -100 | 0 | 바닥. 벽 바로 아래 한 줄만 «벽과 맞닿는» 타일 |
| `Wall` | -90 | 0 | 벽 **한 줄** + 바닥 장식물 |
| `Belt` | -50 | **-0.5** | 가로로 흐르는 벨트 |
| `BeltProps` | -45 | **-0.5** | 초밥이 드나드는 구멍 |

- **벨트 두 판이 따로인 이유**: 한 칸에 타일은 하나뿐이라, 구멍을 벨트와 같은 판에 놓으면
  그 칸의 벨트가 지워져 구멍 뒤가 뚫린다.
- **벨트 판만 반 칸 내려간다**: 칸은 정수 좌표에만 놓이는데 벨트의 중심은 `y=0` 이다.
- 정렬 순서는 테이블(-10)·초밥·손님(0 이상)보다 **뒤**여야 한다.

## 2. 타일 일곱 장의 뜻

원본 파일 이름(`map-tile-3`)은 쓰임을 말해 주지 않는다. 그 뜻이 사는 곳은
`StageTileId` **한 곳뿐**이며, 아래가 그 대응이다 (기획자 확인).

| 원본 | 타일 | 쓰임 |
|---|---|---|
| `map-tile-1` | `Floor` | 바닥 |
| `map-tile-2` | `FloorWallEdge` | 벽과 맞닿는 바닥 줄 (위 가장자리에 띠가 있다) |
| `map-tile-3` | `WallCornerLeft` | 벽의 왼쪽 끝 |
| `map-tile-4` | `WallCornerRight` | 벽의 오른쪽 끝 |
| `map-tile-5` | `Window` | 벽에 뚫린 창문 |
| `map-tile-6` | `Decor` | 바닥 장식물 (초밥이 놓인 작은 대) |
| `map-tile-7` | `Wall` | 벽 |
| `belt_tileset` 0·1 | `BeltHorizontal`·`BeltVertical` | 벨트. 세로는 아직 안 쓴다 |
| `sushi_outgoing-tileset` 1·0 | `BeltMouthLeft`·`BeltMouthRight` | 구멍. **어두운 쪽이 안이고, 거기서 초밥이 나온다** |

**벽은 한 줄이다.** `WallCornerLeft` → `Wall`/`Window` 반복 → `WallCornerRight`.
쌓아 올리는 벽이 아니다.

## 3. 화면 밖까지 칠하되, 구멍은 화면 안에

- 바닥·벽은 가시 범위(16:9 에서 ±8.9)보다 **넓게** 칠한다. 브라우저 창은 아무 화면비나
  될 수 있고, 모자라면 카메라 배경색이 띠로 비친다.
- **구멍은 그 여백에 놓으면 안 된다.** 벨트의 시작·끝(`BeltStart` x=-8, `BeltEnd` x=+8)
  바로 바깥인 `-9`·`+8` 이다. 여백 칸에 놓았다가 16:9 에서 아무에게도 안 보였다.

## 4. 다시 칠하는 법

```
SushiRailBite/Art/Rebuild Stage Tilemap
```

타일 애셋과 팔레트를 만들고(이미 있으면 갱신), `Stage01` 의 네 판을 **비운 뒤** 계획대로
다시 칠한다. **손으로 덧칠하는 길도 열려 있다** — `Assets/Level/Tiles/StageTilePalette.prefab`
을 타일 팔레트 창에서 고르면 된다. 다만 다음 재실행이 판을 비우므로, 남길 손질은
`StageTilePlan` 에 반영한다.

- 배치 규칙은 `StageTilePlan` 한 곳이고 EditMode 로 검증된다.
- 타일 애셋은 **지우고 다시 만들지 않는다.** 새로 만들면 GUID 가 바뀌어 칠해 둔 타일이
  통째로 사라진다.

## 5. 그림이 들어올 때 자동으로 정해지는 것

`PixelArtImportSettings` 가 경로로 판단한다 (`Assets/Art/Sprites/`,
`Assets/Art/Animations/`, `Assets/Level/Placeholder/`).

**임포터는 «빠지면 언제나 틀린 것» 만 정한다.** 필터·밉맵·PPU(32)·압축·알파·메시가 그것이다.

**자르기 모드는 시트일 때만 건드린다.** 낱장까지 `Single` 로 못박았다가 사람이 스프라이트
에디터에서 나눠 둔 UI 스프라이트 열 장이 도로 합쳐졌고, 그것을 물고 있던 프리팹이
**그림 없이 조용히** 떴다. 시트로 보는 것은 둘뿐이다:

- `Assets/Art/Animations/` **폴더 전체**
- 이름이 `tileset` 으로 끝나는 파일 — 원본이 `belt_tileset` 과 `sushi_outgoing-tileset`
  으로 **구분자가 갈려 있어** 접미에 `-` 를 넣으면 한쪽만 걸린다

**규칙을 고치면 `PixelArtTextureImporter.GetVersion()` 을 올린다.** 안 올리면 이미 들어온
텍스처는 다시 임포트되지 않아, 코드는 맞는데 화면만 옛 설정으로 남는다.

## 6. 애니메이션은 아직 아무도 재생하지 않는다 (M7)

`SushiRailBite/Art/Rebuild Customer Animations` 가 `Assets/Level/Animations/` 에 클립
일곱과 유형별 컨트롤러 셋을 만든다. 프레임 순서는 시트의 **왼→오** 이며 `SpriteSheetSlicer`
가 정한다.

- 손님 유형마다 `Idle` ↔ `Picking` 두 상태, 조건은 `bool Picking` 하나.
- **프리팹에 `Animator` 를 달지 않았고 코드도 이 값을 흔들지 않는다.** 붙일 때 손님 상태를
  새로 판정하지 말 것 — `CustomerLogic` 이 이미 들고 있다.
- `picking` 시트는 **뒷모습**이다. 지금 몸통에 나가는 그림은 `front` 한 장뿐이라
  (`CustomerData.Icon`), 붙이는 순간 앞뒤가 섞인다. 방향 처리와 함께 가야 한다.

## 7. 손님 그림은 `CustomerData.Icon` 이 정한다

프리팹의 그림은 **자리가 빌 때 되돌아갈 기본값**일 뿐이고, 실제로 나가는 것은 유형별
아이콘이다 (`CustomerView.ShowIcon`). 그래서 **아이콘 참조가 끊기면 예외도 로그도 없이
기본값이 그대로 남는다.**

실제로 겪었다 — 손님 그림을 4방향으로 다시 그리며 옛 파일이 지워졌고, 세 유형의 아이콘이
통째로 끊긴 채 커밋됐다. 지금은 `CardCatalogAssetTests.EveryCard_HasItsOwnIcon` 이 막는다.

**같은 형태의 사고가 소리에도 있었다.** `.wav` 를 `.mp3` 로 갈아 넣으면서 네 큐의 클립
참조가 끊겼고, 게임은 그냥 조용해졌다. → [`presentation-and-audio.md`](presentation-and-audio.md)
