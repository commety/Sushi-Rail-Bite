# Step 07: 카드 128×192 — 비용 · 이름 · 아이콘 · 스탯 4구획

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** 없음
- **후행 단계:** step-08 (접히는 손패는 커진 카드를 전제한다)

---

## 목적

카드가 **덱빌딩 카드처럼** 정보를 다 담게 한다. 지금은 이름 한 줄과 수치 한 줄뿐이라 «무엇을 노리는 손님인지» 를 카드만 보고 알 수 없다.

`CardView` 는 틀 하나이고 **덱 보기 · 보상 선택 · 손패 · 백과사전 네 화면이 같은 프리팹을 쓴다** (M6 D3). 크기를 바꾸면 네 곳이 동시에 바뀐다 — 그것이 요구의 의도다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/UI/CardCaption.cs` — 수정 (스탯 행 · 비용)
- `Assets/Code/Scripts/Presentation/UI/CardView.cs` — 수정 (비용 라벨 추가)
- `Assets/Code/Scripts/Presentation/UI/Card.prefab` — 수정 (128×192 · 4구획)
- `Assets/Code/Scripts/Presentation/Customers/CustomerCard.prefab` — 수정 (96×128 오버라이드 → 128×192)
- `Assets/Tests/EditMode/UI/CardCaptionTests.cs` — 수정
- `Assets/Tests/EditMode/UI/CardPrefabTests.cs` — 수정
- `Assets/Tests/EditMode/Customers/CustomerCardPrefabTests.cs` — 수정
- `Assets/Tests/PlayMode/UI/CardViewTests.cs` — 수정
- `Assets/Art/Sprites/UI/card-frame.png.meta` — **사람이 인스펙터에서** (§7-3, RULE-03)

### 규격 (요구 원문 그대로)

```
┌──────────────── 128 ────────────────┐
│                        ╭───╮        │ ← 영입 비용 (동전 16px, 우상단)
│                        │ 60│        │
│                        ╰───╯        │
├─────────────────────────────────────┤
│              먹  보                  │ ← 이름 (최소 12px)
├─────────────────────────────────────┤
│              ┌────┐                 │
│              │32×32│                │ ← 아이콘 (패딩 8px, 총 40×40)
│              └────┘                 │
├─────────────────────────────────────┤
│  범위      6                        │
│  대역   100~300                     │ ← 스탯 (행 단위)      192
│  포화도    8                        │
│  소화      4초                      │
└─────────────────────────────────────┘
```

**아이콘은 확대하지 않는다.** 손님·초밥 스프라이트가 실측 **32×32** 라 요구의 «32×32 이미지 공간» 과 1:1 이다 — 원문이 물었던 «4배 확대하는가» 는 실측으로 사라진 질문이다. `Icon` 자식은 지금 64×64 이므로 **40×40 컨테이너 + 32×32 이미지**로 줄인다.

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    public static class CardCaption
    {
        /// <summary>카드 우상단 동전 안에 들어갈 값. 손님만 비용이 있다.</summary>
        public static string CostOf(CustomerData customer);

        /// <summary>초밥에는 영입 비용이 없다. 빈 문자열이다 — 동전이 안 뜬다.</summary>
        public static string CostOf(SushiData sushi);

        /// <summary>스탯을 <b>행 단위</b>로 만든다. 줄바꿈은 여기서 넣는다.</summary>
        public static string DetailOf(CustomerData customer);   // 기존 시그니처, 내용이 바뀐다
        public static string DetailOf(SushiData sushi);
    }
}
```

`CardView` 에 `CostLabel` 을 더한다 — `NameLabel`·`DetailLabel` 과 같은 «인스펙터가 비면 자기 하위에서 이름으로 찾기» 규약이다.

**비용이 빈 문자열이면 동전 아이콘을 끈다.** 안 끄면 초밥 카드에 값 없는 동전이 남는다.

### 스탯 행의 내용

| 카드 | 행 |
|---|---|
| 손님 | `범위 {Reach}` / `대역 {Min}~{Max}` / `포화도 {MaxSaturation}` / `소화 {DigestSeconds}초` / (step-01 뒤) `인구수 {Population}` |
| 초밥 | `가격 {Price}` / `포화 {SaturationAmount}` |

**손님과 초밥의 행 수가 다르다.** 라벨 하나에 줄바꿈으로 넣고 세로 정렬을 위쪽으로 둔다 — 행마다 라벨을 두면 초밥 카드에 빈 행이 남는다.

> **초밥은 4행을 채우지 못한다.** 그래도 카드 크기는 같다 — 두 종류가 나란히 놓이는 화면(백과사전)이 있어 크기가 갈리면 목록이 들쭉날쭉해진다.

### 프레임 9-slice (§7-3 — 사람이 한다)

`card-frame.png` 는 **48×64 이고 `spriteBorder` 가 `0,0,0,0`** 이다. 128×192 로 늘리면 픽셀 아트의 각이 죽는다.

- 에이전트는 **`.meta` 를 직접 편집하지 않는다** (RULE-03)
- 아키텍트에게 요청할 것: `card-frame.png` · `card-frame-disabled.png` 두 장의 Sprite Editor 에서 **Border 를 `(8, 8, 8, 8)`** 로 지정. `panel.png` 가 이미 같은 값으로 쓰이고 있다
- `Image.type` 은 `Sliced`, `Pixels Per Unit Multiplier` 는 1 유지
- **테두리가 들어오기 전에는 카드가 뭉개져 보인다.** 그것은 이 단계의 실패가 아니라 승인 대기 상태다 — 보고에 명시한다

### 폰트 커버리지

`KoreanFontCoverageTests` 의 카드 문구 줄에 `범위 대역 포화도 소화 가격 초 인구수` 를 더한다. **실측 결과 누락 0자** — 재굽기 없음.

### 밸런스 수치

전부 **연출 수치**다 (프리팹 / `RectTransform`). SO 로 빼지 않는다 — 그 프리팹 하나에만 의미가 있다 (M5 D7).

| 값 | 어디에 |
|---|---|
| 128×192 · 40×40 · 패딩 8 · 이름 12 · 동전 16 | `Card.prefab` 의 `RectTransform` |

### 제약

- **카드가 등급으로 갈라지지 않는다** (M6 D3). 프리팹 배리언트를 늘리지 않는다
- **`Show(SushiData)` 와 `Show(CustomerData)` 를 `bool` 하나로 합치지 않는다.** 합치면 뷰 안에 분기가 생기고 그 분기가 곧 판정이 된다 (`CardView` 의 기존 주석)
- 문구 조립은 `CardCaption` 한 곳이다 (README D10)
- `CardPrefabTests` 는 **크기·색·간격을 단언하지 않는다** — 그 테스트의 클래스 주석이 이미 그렇게 정해 두었다. **자식 존재와 컴포넌트만** 본다
- 아이콘이 비면 프리팹 그림을 그대로 둔다 (기존 폴백 유지)

### 테스트 계획

```
CardCaptionTests
  DetailOf_Customer_HasOneLinePerStat        ← 줄 수를 센다. 문자열 통째 비교가 아니라
  DetailOf_Customer_ContainsBandBothEnds     ← 폭이 배정 순위를 가르므로 양끝 다 나와야 한다
  DetailOf_Sushi_HasPriceAndSaturation
  CostOf_Customer_ReturnsRecruitCost
  CostOf_Sushi_ReturnsEmpty                  ← 반례. 상수를 돌려주는 구현을 배제한다

CardViewTests
  Show_Customer_WritesCost
  Show_Sushi_HidesTheCoin                    ← 값 없는 동전이 남지 않는다
  Clear_ResetsCost                           ← 재사용 첫 프레임에 직전 카드 비용이 번쩍이지 않는다

CardPrefabTests
  Prefab_HasCostLabelChild
  Prefab_HasCoinChild

CustomerCardPrefabTests
  Prefab_RootSize_MatchesCardPrefab          ← 96×128 오버라이드가 남아 있으면 잡는다
```

> **`Prefab_RootSize_MatchesCardPrefab` 은 예외적으로 크기를 단언한다.** 여기서 보는 것은 «레이아웃 값» 이 아니라 **두 프리팹이 어긋났는가**다 — `CustomerCard.prefab` 이 오버라이드로 옛 크기를 붙들고 있으면 손패만 작은 카드가 되는데, 그 형태는 화면에서만 드러난다.

### 완료 판정

- [ ] `grep -n "m_SizeDelta: {x: 128, y: 192}" Assets/Code/Scripts/Presentation/UI/Card.prefab`
- [ ] `grep -n "value: 96" Assets/Code/Scripts/Presentation/Customers/CustomerCard.prefab` — **0건**
- [ ] `grep -n "CostLabel" Assets/Code/Scripts/Presentation/UI/CardView.cs` — 상수 + Resolve
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] 네 화면(덱 보기·보상·손패·백과사전)이 **같은 프리팹**을 쓰는지 `grep` 으로 확인
- [ ] §7-3 (9-slice) 요청을 보고하고 **멈춘다.** 승인 전에는 프레임이 늘어난 채로 보인다

### 예상 커밋 메시지

```
feat(ui): lay the card out in four blocks at 128x192
```

---

## 금지 사항

- `Assets/Art/**/*.meta` 를 편집하지 않는다 (RULE-03). 9-slice 는 사람이 한다.
- 카드 프레임 원본을 다시 그리지 않는다 (README D8 — 9-slice 로 간다).
- 손패 접힘을 여기서 건드리지 않는다 (step-08).
- 프리팹 배리언트를 늘리지 않는다.
