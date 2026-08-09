# Step 03: 손님 머리 위를 정리한다 (P5)

- **영역:** `presentation` — `Presentation` 어셈블리 + `Customer.prefab` + `Tests.EditMode` · `Tests.PlayMode`
- **선행 단계:** **step-01 필수** — 거기서 만든 `bar-slot` 스프라이트를 여기서 붙인다
- **후행 단계:** 없음

---

## 목적

필드에서 손님 위에 남을 것은 **포화도 · 소화 중 · 먹는 중** 셋뿐이다. 이름(=유형)과 대역을
상시 표시하지 않는다.

두 가지를 한 단계에서 한다. **둘 다 «손님 위 표시물» 이고 서로 자리를 두고 다투기 때문**이다 —
라벨을 지우면서 포화도 칸을 키워야 «비었다» 가 아니라 «정리됐다» 가 된다.

1. **머리 위 라벨을 지운다** — 대역은 정보 창(M6.5)이, 유형은 스프라이트(M5)가 진다
2. **포화도를 손님 오른쪽 세로바로 옮기고 칸마다 배경판을 깐다** — 지금 화면에 그려지는 칸은
   **약 1픽셀**이고, 그나마 테이블 스프라이트 위에 얹혀 있다

> **«먹는 중» 표시물을 새로 만들지 않는다.** `CustomerView.Apply` 가 상태별 몸통 색을 이미
> 칠한다(먹는 중 노란빛 · 소화 중 회색빛 · 대기 파란빛). 전용 배지가 필요하다는 결정이 따로
> 서면 그때 만든다 (README D3).

### 실측 — 지금 무엇이 어디 있나 (전부 자리 기준 로컬 좌표)

| 물체 | y 범위 | x 범위 | 정렬 순서 |
|---|---|---|---|
| 테이블 (`table.png` 64×64, 중앙 피벗, y `-0.85`) | **`-1.85` … `+0.15`** | **`-1` … `+1`** | `-10` |
| 손님 몸통 (32×32) | `-0.5` … `+0.5` | `-0.5` … `+0.5` | `0` |
| 포화도 바 (지금) | `≈ -0.62` | 칸 8개 `-0.49`…`+0.49`, 간격 `0.14`, scale **`0.12`** | `10` |
| 소화 배지 (16×16, y `0.85`) | `+0.6` … `+1.1` | — | `10` |
| **벨트 레일** (월드) | 월드 `-0.7` … `+0.7` | 월드 `±8.25` | `-50` |

읽어야 할 세 가지:

- **칸이 0.03 유닛(≈1px)으로 그려진다.** `bar-cell` 은 8×8@PPU32 = 0.25 유닛인데 scale `0.12`
  가 곱해진다. 간격이 `0.14` 라 **칸보다 틈이 네 배 넓다** — «작고 애매하다» 의 정체다
- **테이블이 좌우 ±1 유닛을 덮는다.** 손님 몸통은 ±0.5 뿐이라 **좌우 어느 쪽에 두어도 테이블
  위**다. 벗어나려면 |x| > 1 인데 자리 2·3 은 간격이 2 유닛뿐이라 이웃 테이블에 닿는다
- **머리 위로 쌓는 길은 예산이 없다.** 머리 끝(월드 `-1.5`)에서 벨트 레일 바닥(`-0.7`)까지
  0.8 유닛인데 배지가 이미 0.5 를 쓴다. 배지를 올려 그 아래 포화도를 넣으면 **레일까지
  0.05 유닛(≈1.6px)** 만 남고, 그 위로 초밥이 지나간다

**그래서 세로바 + 배경판이다.** 세로바는 배지와 **다른 축**을 쓰므로 상태가 바뀌어도 재배치가
없고, 배경판은 «테이블 위냐 벽 위냐» 를 아예 무의미하게 만든다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (라벨 제거)
- `Assets/Code/Scripts/Presentation/Customers/Customer.prefab` — 수정 (`BandLabel` 삭제 · 칸 크기)
- `Assets/Tests/PlayMode/Customers/CustomerViewTests.cs` — 수정 (대역 테스트 4건)
- `Assets/Tests/EditMode/Customers/CustomerPrefabTests.cs` — 수정 (`BandLabel` 테스트 2건)
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 수정 (아래 2건)
- `.claude/domain/customer-kinds.md` — 수정 (**빠뜨리면 다음 세션이 어긋난 문서를 읽는다**)

### 1) `CustomerView` 에서 지울 것

```csharp
private const string BandLabelName = "BandLabel";   // 삭제
[SerializeField] private TMP_Text _bandLabel;        // 삭제
public string BandText { get; private set; }         // 삭제
```

그리고 `Bind` 안의 `BandText = $"{CardCaption.NameOf(data)}\n…"` · `HudLabel.Write(_bandLabel, …)`
두 갈래(정상·null)와 `Resolve` 의 `HudLabel.Resolve(transform, _bandLabel, BandLabelName)`.

- **`using TMPro;` 가 남는지 확인한다.** 다른 데서 안 쓰면 지운다 — 린트가 잡는다
- **`CardCaption.NameOf` 호출이 여기서 사라진다.** 그 함수는 카드 넷이 계속 쓰므로 지우지 않는다
- 클래스 doc 의 *"대역과 대기를 손님 옆에 둔 이유"* 문단을 **«왜 뺐는지» 로 바꾼다.** 지우기만
  하면 다음 사람이 M2.5 문서를 읽고 되돌린다. 문구는 «가리되 이름을 부르지 않는다»
  ([`rules/scripts.md`](../../.claude/rules/scripts.md) §7)

### 2) `Customer.prefab` — 세로바로 옮기고 배경판을 깐다

ClaudeBridge 로 처리한다 (`.claude-bridge/inbox/` → `./scripts/bridge-run.sh`).

`Prefab.Open` → `GameObject.Delete`(`BandLabel`) → `GameObject.SetTransform`(바·칸) →
`GameObject.Create`+`Component.Add`(배경판 8장) → `Prefab.Save` → `Prefab.Close`

**배치 (제안 — 사람이 화면에서 확정한다)**

| 대상 | 지금 | 제안 |
|---|---|---|
| `SaturationBar` 위치 | `(0, -0.62, 0)` | **`(0.65, 0, 0)`** — 축을 맞바꾼 꼴이다 |
| 칸 `i` 위치 | `x = (i-3.5)×0.14`, `y = 0` | **`x = 0`, `y = (i-3.5)×0.14`** |
| 칸 `localScale` | `0.12` | **`0.46`** (0.25×0.46 ≈ 0.115 → 간격 0.14 와 거의 맞닿는다) |

- **`Cell0` 이 맨 아래**여야 한다 (`y = -0.49`). `SaturationBarView` 는 배열 순서대로 칠하므로
  순서가 뒤집히면 **게이지가 위에서부터 찬다** — 예외도 경고도 없고 화면에서만 드러난다
- **여덟 칸을 전부 같은 스케일로.** 하나만 빠지면 그 칸만 작은데 «7칸 찼다» 로 보여 버그처럼
  읽힌다
- 검산: 바 폭은 배경판 기준 `0.65 ± 0.086` → 몸통 오른쪽 끝 `0.5` 에서 **약 2px 띄워진다**.
  세로 범위는 `±0.576` 이라 배지 바닥(`+0.6`) 아래에서 멈춘다. 이웃 손님(Δx=2)의 몸통 왼쪽
  끝은 `+1.5` 라 **0.76 유닛 여유**가 있다

**배경판 8장**

- 칸 각각의 **자식**으로 만든다. `SpriteRenderer` + `bar-slot`(step-01), 정렬 순서 **`9`**
  (칸은 10, 몸통 0, 테이블 −10 — 칸 뒤·몸통 앞)
- 자식이라 **칸이 꺼지면 함께 꺼진다.** `SaturationBarView.Bind` 가
  `VisibleCells(max, capacity)` 로 칸 수를 손님마다 다르게 켜므로(소식가 3칸 · 먹보 8칸),
  **판 하나를 통짜로 깔면 소식가에게 빈 판이 길게 남는다.** 자식으로 두면 길이가 저절로 맞고
  **`SaturationBarView` 는 한 줄도 안 바뀐다**
- 로컬 위치 `(0,0,0)`, 로컬 스케일 `1` — 칸의 `0.46` 을 그대로 물려받는다. 별도 배율을 주면
  칸과 판이 따로 자란다
- 12×12 판이 scale 0.46 에서 0.1725 유닛이라 **간격 0.14 보다 커서 판끼리 겹친다** — 그래서
  한 줄기 기둥으로 보인다. `bar-slot` 이 불투명이어야 하는 이유가 이것이다(겹친 자리가 두 번
  어두워지면 줄무늬가 생긴다)

**`DigestingBadge`(y `0.85`, 머리 위)는 건드리지 않는다** — 이제 세로바와 축이 갈렸다.

### 3) 테스트 — 지우지 말고 **옮긴다**

`BandText` 가 사라지므로 아래는 **컴파일이 깨진다.** 깨진 것을 지우는 것이 아니라, **무엇을
지키려던 테스트였는지**를 새 계약으로 옮긴다 ([`rules/tests.md`](../../.claude/rules/tests.md) §7).

| 지금 | 무엇을 지키던 것인가 | 어디로 |
|---|---|---|
| `CustomerViewTests.Bind_Customer_ShowsNameAndTargetingBand` | 라벨이 실제로 그려진다 | **폐기** — 그리지 않기로 했다 |
| `…Bind_BigEaterData_ShowsItsOwnNameAndBand` | 상수를 돌려주지 않는다 | **폐기** (같은 이유) |
| `…Bind_DataWithoutDisplayName_FallsBackToAssetName` | 이름 폴백 | **`CardCaptionTests` 에 있는지 확인.** 없으면 거기로 옮긴다 — 카드 넷이 여전히 쓰는 규칙이다 |
| `…Bind_Null_ClearsBandText` | 자리를 갈아탈 때 직전 손님이 안 남는다 | **아이콘·포화도 칸·배지**로 같은 계약이 이미 있는지 보고, 없으면 남긴다 |
| `CustomerPrefabTests.Prefab_BandLabel_IsTmpText` · `_HasKoreanFont` | 화면에 나오는 라벨이다 | **뒤집는다** ↓ |
| `Stage01SceneTests.Play_Scene_OccupiedSeatShowsBand` | 씬을 지나 라벨이 보인다 | **뒤집는다** ↓ |

**«뒤집는다» 는 이런 뜻이다** — 다시 들어오는 것을 막는 테스트를 남긴다:

```csharp
// CustomerPrefabTests
[Test] public void Prefab_HasNoAlwaysOnLabel()
// Customer.prefab 아래의 TMP_Text 는 DigestingBadge 하위(RemainingLabel) 하나뿐이다
```

- **«BandLabel 자식이 없다» 로만 쓰지 않는다.** 이름을 바꿔 다시 넣으면 통과한다. **개수와
  위치**를 본다
- 실패 메시지에 «상시 라벨을 다시 넣지 않는다» 는 판단을 적는다 — 다음 사람이 테스트만 보고
  이유를 알 수 있어야 한다

### 3b) 세로바가 실제로 세로바인지 보는 테스트

`CustomerPrefabTests` 에 더한다. **로직이 안 바뀌므로 여기서 잡지 못하면 아무도 못 잡는다.**

| 테스트 | 무엇을 지키나 |
|---|---|
| `Prefab_SaturationCells_AreStackedVertically` | 칸의 `x` 가 전부 같고 `y` 가 서로 다르다 — 가로 배치로 되돌아가면 죽는다 |
| `Prefab_SaturationCells_FillFromTheBottom` | **`_cells[i]` 의 `y` 가 `i` 에 대해 증가**한다. 뒤집히면 게이지가 위에서부터 찬다 |
| `Prefab_SaturationBar_SitsBesideTheBody` | 바의 `x` 가 몸통 오른쪽 끝(`+0.5`)보다 크다 |
| `Prefab_EverySaturationCell_HasABacking` | 칸마다 `bar-slot` 을 문 자식이 하나 있다 |
| `Prefab_CellBacking_DrawsBehindItsCell` | 배경판 정렬 순서 < 칸, 그리고 **> 몸통** |

**공허하게 통과하지 않게** ([`rules/tests.md`](../../.claude/rules/tests.md) §3):

- **«전부 같다» 만 보지 않는다.** 칸 여덟이 모두 같은 좌표인 구현(=한 자리에 겹쳐 있음)도
  «x 가 같다» 를 통과한다. **`y` 가 서로 다르다**를 같은 테스트에 함께 박는다
- **채움 방향을 볼 때는 «증가한다» 를 본다.** 「첫 칸이 맨 아래」만 보면 나머지 일곱이 뒤죽박죽
  이어도 통과한다
- **정렬 순서를 볼 때는 세 층을 다 본다.** 배경판 < 칸만 보면 배경판이 몸통 **뒤로** 숨어도
  통과하는데, 그러면 화면에서 판이 사라진다

### 4) 이 변경이 **가리는** 테스트 하나 (중요)

`Stage01SceneTests.Play_Scene_LabelsOnCustomersStayOffCanvas` 는 끝에서 이렇게 단언한다:

```
Assert.IsTrue(checkedAny, "Canvas 밖 라벨이 하나도 없다 — 대역·소화 라벨이 사라졌거나 …");
```

**이 테스트는 통과한 채로 남는다** — 소화 배지의 `RemainingLabel` 이 여전히 Canvas 밖에 있기
때문이다. 하지만 **메시지가 «대역» 을 말하는 채로 남으면 거짓**이 되므로 문구를 고친다.

> **확인은 돌려 보고 한다.** «통과할 것이다» 는 예측이다 — M4 에서 세 번 빗나갔다. 실행해서
> `checkedAny` 가 실제로 `true` 인지 보고, **아니면 그 사실을 보고한다** (라벨이 하나도 없으면
> 그 테스트는 그때부터 헛돈다).

### 5) 도메인 문서 갱신 — 완료 조건이다

[`.claude/domain/customer-kinds.md`](../../.claude/domain/customer-kinds.md) 가
*"어느 손님이 무엇을 노리는지가 자리 옆에 있어야 한다"* 를 **M2.5 의 존재 이유**로 적어 두었다.
이 단계가 그것을 무효로 만든다.

- 그 문장을 지우지 말고 **«그랬다가 이렇게 바뀌었다»** 로 갱신한다. 역할을 정보 창(M6.5)과
  유형 스프라이트(M5)가 나눠 진다는 것, 그리고 **«한 번에 한 손님만 볼 수 있다» 는 차이가
  남는다**는 것을 적는다 — 되돌릴 일이 생기면 그 문장이 근거가 된다
- `.claude/INDEX.md` 의 `keywords` 가 여전히 맞는지 본다

### 밸런스 수치

없다. 칸 크기·바 위치는 연출 수치이며 프리팹에 산다 (M5 D7).

### 제약

- **RULE-03 — `.meta` 를 편집하지 않는다**
- **`Runtime` 을 건드리지 않는다.** `CustomerLogic`·`CustomerRuntimeState` 는 이 변경을 모른다
- **`SaturationBarView` 의 로직을 바꾸지 않는다.** 배경판을 칸의 자식으로 두는 이유가 그것이다 —
  코드가 판을 켜고 끄기 시작하면 «칸 수» 라는 진실이 두 곳에 생긴다
- **씬 파일을 열지 않는다.** 자리의 손님은 프리팹 인스턴스다. 씬은 step-06 의 몫이다
- **`DigestingBadge` · `RemainingLabel` 을 건드리지 않는다** — 남기기로 한 셋 중 하나다
- **`bar-slot` 스프라이트를 여기서 만들지 않는다** — step-01 의 결과물을 물기만 한다.
  없으면 step-01 이 안 끝난 것이므로 **멈추고 보고한다**
- 배경판을 **`SaturationBar` 의 자식(칸의 형제)으로 두지 않는다.** 그러면 칸이 꺼져도 판이
  남아 소식가에게 빈 판이 길게 붙는다

### 완료 판정

- [ ] `grep -rn "BandText\|BandLabel" Assets/` → **0건** (문서의 «그랬다가» 서술은 제외)
- [ ] `Customer.prefab` 에 `m_Name: BandLabel` 이 없고, `bar-slot` 을 문 렌더러가 **8개** 있다
- [ ] 칸 8개의 `m_LocalScale` 이 **전부 같은 값**이고, `y` 가 `Cell0` 부터 **증가**한다
- [ ] `./tests/preflight.sh` 전부 `[ok]`
- [ ] `Play_Scene_LabelsOnCustomersStayOffCanvas` 의 `checkedAny` 가 **실제로 true** 인지 실행으로 확인
- [ ] **공허 확인**: 칸 순서를 뒤집기 / 배경판 정렬 순서를 `-20` 으로 낮추기 / 배경판 하나만
      빼기를 **하나씩** 주입하고 지목한 테스트가 죽는지 본다. **실측으로** 보고한다
- [ ] `customer-kinds.md` 갱신 diff 를 보고에 포함 — **사람 승인 대기** (도메인 문서는 직접
      커밋하지 않는다, `/task-done` STEP 5)
- [ ] **화면으로 확인** — 세 손님(소식·기본·먹보)을 **모두 앉혀** 칸 수가 다를 때 판 길이가
      따라오는지 본다. 한 유형만 보면 이 단계의 핵심이 확인되지 않는다

### 예상 커밋 메시지

```
refactor(customer): move the saturation bar beside the customer
```

라벨 제거와 세로바를 **두 커밋으로 나눠도 된다.** 다만 라벨만 지운 중간 상태는 손님 위가
휑하므로, 나눈다면 **연달아** 올린다.

---

## 금지 사항

- 테스트를 **지우고 끝내지 않는다.** 무엇을 지키던 것인지 옮기거나, 옮길 곳이 없으면 폐기 사유를
  보고에 적는다
- 몸통 상태 색(`_idleColor`·`_eatingColor`·`_digestingColor`·`_waitingColor`)을 바꾸지 않는다
- 손님 스프라이트·아이콘을 바꾸지 않는다
- 새 표시물(먹는 중 배지 등)을 만들지 않는다
- 배경판을 만들려고 `bar-cell` 에 테두리를 그려 넣지 않는다 — 칸은 런타임에 통째로 틴트된다
