# Step 03: 재사용 카드 틀 · 이름 폴백 통합

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** 없음 (step-01 의 SO 를 컴파일 의존하지 않는다)
- **후행 단계:** step-04(손패) · step-06(덱 패널) · step-08(보상) · step-10(백과사전)이 전부 이 프리팹을 쓴다

---

## 목적

M6 에서 카드가 뜨는 화면이 **넷**이다 — 손패, 덱 보기, 보상 선택, 백과사전. 각자 다르게 그리면 같은 초밥이 화면마다 다르게 보이고, 카드 모양을 고칠 때 네 곳을 고치게 된다.

**틀은 하나다** (README D3). 등급으로 갈라지지 않고, 아이콘·이름·수치만 데이터에서 온다.

이 단계가 어셈블리 참조를 하나 늘리므로(§7), **M6 의 uGUI 는 여기서부터 가능해진다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### §7 승인 먼저 — 어셈블리 참조 추가

```diff
  // Assets/Code/Scripts/Presentation/Presentation.asmdef
   "references": [
     "Runtime",
     "Runtime.Data",
     "Unity.TextMeshPro",
-    "Unity.InputSystem"
+    "Unity.InputSystem",
+    "UnityEngine.UI"
   ]
```

`Tests.EditMode.asmdef` · `Tests.PlayMode.asmdef` 에도 같은 항목을 더한다.

**왜 필요한가:** `Button` · `Image` · `Slider` · `Toggle` · `ScrollRect` · `EventSystem` · `IBeginDragHandler` · `PointerEventData` 가 전부 `UnityEngine.UI` 어셈블리에 있다. 씬은 이미 `CanvasScaler` 를 쓰고 있지만 **코드가 그 타입을 부른 적이 없어** 참조 없이도 컴파일이 됐다. M6 은 버튼과 드래그를 코드로 다룬다.

`autoReferenced` 는 **`false` 그대로 둔다** (RULE-01). 참조 추가는 Domain Reload 를 유발하지 않는다.

> 승인 전에는 이 단계를 진행하지 않는다. 참조 없이 짜면 컴파일이 통째로 막힌다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/UI/CardView.cs` — 생성
- `Assets/Code/Scripts/Presentation/UI/CardCaption.cs` — 생성
- `Assets/Code/Scripts/Presentation/UI/Card.prefab` — 생성 (**기능 폴더 안에** — [`parallel-work.md`](../../.claude/rules/parallel-work.md) §2)
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (`NameOf` 를 접는다)
- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 수정 (`DisplayNameOf` 를 접는다)
- `Assets/Tests/EditMode/UI/CardCaptionTests.cs` — 생성
- `Assets/Tests/PlayMode/UI/CardViewTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>
    /// 카드 한 장의 화면 표현. <b>판정하지 않는다</b> — 무엇을 보여줄지는 부르는 쪽이 정한다.
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        public void Show(SushiData sushi);
        public void Show(CustomerData customer);
        public void Clear();                       // 빈 자리로 되돌린다

        public string NameText { get; }            // 검증용
        public string DetailText { get; }          // 검증용
        public Sprite ShownSprite { get; }         // 검증용
        public bool IsShowing { get; }

        public event Action<CardView> Clicked;     // 누가 눌렀는지 부르는 쪽이 안다
    }

    /// <summary>카드에 쓸 문구를 만든다. 순수 계산이라 EditMode 로 검증된다.</summary>
    internal static class CardCaption
    {
        public static string NameOf(SushiData sushi);
        public static string NameOf(CustomerData customer);
        public static string DetailOf(SushiData sushi);      // 예: "300 · 포화 3"
        public static string DetailOf(CustomerData customer);// 예: "100~300 · 영입 20"
    }
}
```

### `bool` 로 분기하지 않는다 — `Show` 를 둘로 나눈 이유

`Show(object card, bool isSushi)` 로 두면 뷰 안에 분기가 생기고, 그 분기가 곧 판정이 된다. `IStageTransitionView` 가 클리어와 런 종료를 **다른 메서드**로 나눈 것과 같은 판단이다 (그 인터페이스의 주석에 이유가 적혀 있다).

`RewardOffer` 는 이미 `Kind` 를 들고 있으므로, 보상 화면(step-08)은 그것을 보고 어느 `Show` 를 부를지 정한다 — **뷰가 아니라 부르는 쪽이 안다.**

### 이름 폴백을 여기로 접는다 (README D7)

*"표시 이름이 비면 애셋 이름"* 이 지금 세 곳에 있다:

| 위치 | 처분 |
|---|---|
| `CustomerView.NameOf` | **`CardCaption.NameOf` 로 접는다** (같은 어셈블리) |
| `StageHudView.DisplayNameOf` | **`CardCaption.NameOf` 로 접는다** (같은 어셈블리) |
| `RewardOffer.DisplayName` | **남긴다** — `Runtime` 이고 영입 비용까지 붙인다. 처분은 step-08 |

두 곳을 접을 때 **표시 문구가 바뀌면 안 된다.** `CustomerViewTests` · `StageHudViewTests` 가 그대로 통과해야 한다 — 통과하지 않으면 접는 과정에서 동작을 바꾼 것이다.

### 프리팹 구성

`Card.prefab` — uGUI, `Canvas` 아래에 놓이는 것을 전제한다.

```
Card (RectTransform, Image=틀, Button, CardView)
├── Icon      (Image)
├── NameLabel (TextMeshProUGUI)
└── DetailLabel (TextMeshProUGUI)
```

- **`Button` 을 쓴다.** 클릭 판정을 직접 짜지 않는다 — `EventSystem` 이 이미 하는 일이다
- `CardView` 는 인스펙터가 비면 **자기 하위에서 이름으로** 자식을 찾는다 (`HudLabel.Resolve` 와 같은 방식, §4.3 의 전역 탐색이 아니다)
- 틀·아이콘 스프라이트는 step-11 이 채운다. **지금은 비어 있어도 동작해야 한다**
- 카드 크기·간격은 프리팹의 직렬화 값이다 (연출 수치 — M5 D7)

### 선행 산출물 의존성

- `SushiDefense.Data.SushiData` · `CustomerData` (기존)
- `SushiDefense.UI.HudLabel` (기존) — 라벨 쓰기·자식 찾기를 재사용한다

### 밸런스 수치

**없다.**

### 제약

- `Instantiate`/`Destroy` 를 부르지 않는다 (§3.4). **카드는 각 화면이 미리 놓아 둔 것을 켜고 끈다** — `TableSlotView._seatVisual` 이 이미 쓰는 방식이다. 필요한 최대 장수는 화면마다 정해져 있다 (손패 = 명부 크기 상한, 보상 = `OfferCount`)
- `Update` 경로에서 문자열을 만들지 않는다 (§4.3). 문구는 `Show` 에서 한 번만 만든다
- `FindObjectOfType` 금지 (§4.3)
- 이벤트 구독은 `OnDestroy` 에서 해제한다 ([`scripts.md`](../../.claude/rules/scripts.md) §6). `Button.onClick` 도 마찬가지다
- **한글 문자열 리터럴을 넣으면 폰트 서브셋에 들어가야 한다** — step-11 이 재추출한다. `DetailOf` 의 구분자·단위(`포화`·`영입`)가 여기 해당한다

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
EditMode  NameOf_EmptyDisplayName_FallsBackToAssetName
          NameOf_WhitespaceDisplayName_FallsBackToAssetName
          DetailOf_Sushi_ContainsPriceAndSaturation
          DetailOf_Customer_ContainsBandAndRecruitCost
PlayMode  Show_Sushi_WritesNameAndIcon
          Show_Customer_ThenSushi_ReplacesBoth      ← 이전 카드가 남지 않는가
          Clear_AfterShow_EmptiesLabels
          Click_WhenShowing_RaisesClicked
          Click_AfterClear_DoesNotRaise             ← 빈 카드는 눌려도 아무 일 없다
```

`Show_Customer_ThenSushi_ReplacesBoth` 는 **재사용에서 직전 내용이 남는 사고**를 본다 — M5 에서 풀 반납 시 스프라이트를 되돌리지 않아 재사용 첫 프레임에 직전 초밥이 번쩍인 것과 같은 형태다.

> **`CardCaptionTests` 를 뷰 없이 쓴다.** 문구 생성이 `MonoBehaviour` 밖에 있는 이유가 그것이고, 뷰를 세워야만 확인되는 문구라면 `CardCaption` 이 제 역할을 못 하는 것이다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| `NameOf` 의 `IsNullOrWhiteSpace` → `== null` | 1건 | 예측대로 **1건** |
| **프리팹의 자식 이름을 바꾼다** (`NameLabel` → `TitleLabel`) | — | **1건** — `CardPrefabTests` 만 |
| `Show` 에서 아이콘 대입 제거 | 1건 | **2건** — 재사용 테스트가 함께 |
| `CustomerView` 의 이름 폴백을 접으면서 문구를 바꾼다 | 기존 테스트 | **기존 `CustomerViewTests`·`StageHudViewTests` 수정 없이 통과** |

### 코드 하네스가 프리팹을 못 밟는다 — `CardPrefabTests` 를 추가했다

작업서에는 없던 파일이다. `CardViewTests` 는 오브젝트를 **코드로** 세우므로 프리팹이 어떻게
생겼든 초록이고, 자식을 이름으로 찾는 폴백이 있어 **프리팹의 자식 이름이 하나만 틀려도
화면이 비는데 예외는 나지 않는다.**

프리팹의 `NameLabel` 을 `TitleLabel` 로 바꿔 확인했다 — **`CardPrefabTests` 한 건만** 죽고
나머지 619건은 전부 초록이었다. 이것이 [`tests.md`](../../.claude/rules/tests.md) §1 의
«애셋 등록·설정» 사각지대이며, M5 가 Build Settings 로 같은 값을 치렀다.

봐야 할 것 중 **루트의 레이캐스트 그래픽**을 잊지 않는다. 없으면 `Button` 이 있어도 눌리지
않는데 화면에는 카드가 멀쩡히 보인다.

### 공허해서 지운 테스트 하나

`Prefab_IconAndLabels_AreDistinctObjects`(셋이 서로 다른 오브젝트인가)를 넣었다가 **지웠다.**
`Transform.Find` 는 이름이 다르면 같은 오브젝트를 돌려줄 수 없고, 셋 다 없는 경우는
`Prefab_Has*Child` 셋이 이미 각각 잡는다 — 독립적으로 잡는 것이 없다 (step-01 · step-02 에서
지운 것과 같은 이유).

### 완료 판정

- [x] `Presentation.asmdef` 에 `UnityEngine.UI` 가 있고 `autoReferenced` 는 여전히 `false`
- [x] `grep -rn "IsNullOrWhiteSpace" Assets/Code/Scripts/Presentation/` 이 **`CardCaption` 한 파일**에서만 나온다
- [x] `Card.prefab` 이 `Assets/Code/Scripts/Presentation/UI/` 에 있다
- [x] EditMode **619/619** · PlayMode **167/167**
- [x] **기존** `CustomerViewTests` · `StageHudViewTests` 가 수정 없이 통과한다
- [x] `./tests/preflight.sh` 전 항목 PASS

### 계약 두 곳이 작업서와 다르다

| 작업서 | 실제 | 이유 |
|---|---|---|
| `internal static class CardCaption` | **`public`** | `Presentation` 에 `InternalsVisibleTo` 가 없다. 새로 만드는 것보다 `SlotPicker`(이미 `public static`)의 선례를 따르는 편이 작다 |
| `Clear()` 는 내용만 지운다 | **오브젝트도 끈다** | `TableSlotView.Vacate` 와 같은 형태다. 부르는 쪽이 `Clear` 뒤에 `SetActive(false)` 를 또 부르게 두면 한쪽을 잊는 날이 온다 |

`Show`/`Clear` 가 활성 상태까지 다루므로 뒤 단계의 패널들은 **켜고 끄는 코드를 따로 쓰지
않는다.**

### 프리팹은 브리지로 만들었다

`Asset.CreatePrefab` 까지 17개 op 이 전부 `ok`. uGUI 프리팹 YAML 을 손으로 쓰면 `fileID`·
`m_Component` 목록·앵커가 얽혀 조용히 어긋난다 — 브리지가 만들면 그 부분이 정답으로 나온다.

### 예상 커밋 메시지

```
feat(ui): add reusable card view
```

---

## 금지 사항

- 등급별 프리팹 배리언트를 만들지 않는다 (README D3)
- `Show` 를 하나로 합쳐 `bool`·`enum` 으로 분기하지 않는다
- 카드를 `Instantiate` 하지 않는다
- `CustomerView`·`StageHudView` 의 **public 계약**을 바꾸지 않는다 — 안쪽 폴백만 접는다
- 프리팹을 심링크 폴더에 만들지 않는다 (RULE-02)
