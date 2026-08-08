# Step 05: 포화도 칸 바 · 소화중 배지

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-02 (`SaturationGauge`)
- **후행 단계:** step-11(배지 스프라이트) · step-12(씬 확인)

---

## 목적

손님이 **얼마나 찼는지**와 **왜 지금 안 먹는지**가 화면에 없다. `CustomerView` 가 상태를 색 틴트로만 알리는데, 회색빛(소화 중)과 파란빛(대기 중)은 나란히 놓고 봐야 구분되고, 포화도는 아예 보이지 않는다.

칸 바 하나와 16×16 배지 하나면 둘 다 해결된다. **판정은 이미 있다** — 이 단계는 그것을 옮기기만 한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Customers/SaturationBarView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Customers/DigestingBadgeView.cs` — 생성
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (둘을 물리고 갱신을 넘긴다)
- `Assets/Tests/PlayMode/Customers/SaturationBarViewTests.cs` — 생성
- `Assets/Tests/PlayMode/Customers/DigestingBadgeViewTests.cs` — 생성

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    /// <summary>포화도를 칸으로 그린다. 몇 칸인지는 <see cref="SaturationGauge"/> 가 정한다.</summary>
    public sealed class SaturationBarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _cells;   // 프리팹에 미리 놓은 칸 (기본 8개)
        [SerializeField] private Color _emptyColor;         // 회색
        [SerializeField] private Color _filledColor;        // 노란색

        public void Bind(CustomerRuntimeState state);       // null 이면 바를 끈다
        public void Refresh();                              // 값이 바뀐 프레임에만 색을 쓴다

        public int ShownFilledCells { get; }                // 검증용
        public int ShownVisibleCells { get; }               // 검증용
    }

    /// <summary>소화 중임을 손님 위에 작게 알린다.</summary>
    public sealed class DigestingBadgeView : MonoBehaviour
    {
        public void Show();
        public void Hide();
        public bool IsShowing { get; }                      // 검증용
    }
}
```

### 칸 수는 계산에서 온다 (README D6)

```
표시 칸 = SaturationGauge.VisibleCells(state.Data.MaxSaturation, _cells.Length)
찬 칸   = SaturationGauge.FilledCells(state.CurrentSaturation, state.Data.MaxSaturation, _cells.Length)
```

- 칸은 프리팹에 **8개 미리 놓는다.** 런타임에 만들지 않는다 (§3.4)
- 표시 칸을 넘는 칸은 **끈다** (`SetActive(false)`) — 회색으로 두면 "아직 못 채운 칸" 으로 읽힌다
- **`SaturationAmount` 가 1 이 아니다.** 장어 3 · 성게 4 · 참치 2 — 한 입에 여러 칸이 찬다. 이것이 정상이며, "5칸 = 5개" 로 읽는 구현은 틀렸다

### 값이 바뀐 프레임에만 쓴다

`CustomerView` 가 이미 그 방식이다 — 상태·대기가 바뀐 프레임에만 색을 대입한다. 칸 바도 같다. 매 프레임 8개 `SpriteRenderer` 에 색을 대입하면 렌더러가 머티리얼 프로퍼티를 계속 갱신해 배포 타깃(WebGL)에서 손해다 (§4.3).

**갱신 주체는 `CustomerView.LateUpdate` 하나로 유지한다.** 바와 배지가 각자 `LateUpdate` 를 돌면 손님 4명 × 3개 컴포넌트가 매 프레임 도는 구조가 된다.

### 배지의 크기 — 16×16 은 애셋 크기다

요구는 *"인터페이스 UI는 16x16px 크기"* 다. 이 프로젝트는 PPU 32 로 고정돼 있으므로 (`PixelArtImportSettings.PixelsPerUnit`), **16×16 px 스프라이트는 월드에서 0.5 유닛**이다.

- 스프라이트 애셋을 **정확히 16×16 px 로 만든다** (step-11). 크기는 텍스처를 읽어 테스트로 고정할 수 있다
- 화면에 실제로 몇 픽셀로 보이는지는 **카메라 줌이 정한다.** 1 유닛 = 32 화면픽셀이면 16px 로 보이고, 그렇지 않으면 다르게 보인다 — step-12 에서 실측한다

> **문구가 아니라 아이콘이다.** *"소화중..."* 을 12px 픽셀 폰트로 쓰면 폭이 60px 을 넘어 16×16 안에 들어가지 않는다. 배지는 **아이콘 하나**로 그 상태를 말하고, 글자가 필요하다는 판단이 서면 말풍선 폭을 다시 정하는 별도 작업이다. **이 축소를 작업서에 남겨 두어야** 다음 사람이 "요구를 못 읽었나" 로 멈추지 않는다.

### 배지는 상태를 되묻지 않는다

`CustomerState.Digesting` 인지는 `CustomerView` 가 이미 읽고 있다. 배지는 `Show()`/`Hide()` 만 받는다 — **뷰가 판정하면 상태 머신의 진실이 둘이 된다** (§3.2·§3.5).

### 선행 산출물 의존성

- step-02 의 `SaturationGauge`
- 기존 `CustomerRuntimeState` · `CustomerState` · `CustomerView`

### 밸런스 수치

**없다.** 칸 수(8)·색·배지 위치는 프리팹의 직렬화 값이다 (연출 수치 — M5 D7).

### 제약

- `Instantiate`/`Destroy` 금지 (§3.4)
- `Update`/`LateUpdate` 경로 할당 금지 (§4.3)
- `FindObjectOfType` 금지 — 비어 있으면 **자기 하위**에서 찾는다 (§4.3, `HudLabel.Resolve` 와 같은 방식)
- **`CustomerView` 의 public 계약을 바꾸지 않는다.** `ShownState` · `ShownWaiting` · `BandText` · `ShownSprite` · `Bind` 시그니처를 그대로 두어 `CustomerViewTests` 가 회귀 그물로 남는다
- 바·배지가 `null` 이어도 죽지 않는다 — **표시는 로직의 전제 조건이 아니다**

### 테스트 계획 (TDD — 먼저 실패시킬 것)

```
칸 바   Refresh_NoneEaten_FillsZeroCells
        Refresh_PartiallyEaten_FillsExactCells        ← MaxSaturation 5, 현재 3 → 3칸
        Refresh_Full_FillsAllVisibleCells
        Bind_MaxSaturationBelowCapacity_DisablesSpareCells
        Bind_MaxSaturationAboveCapacity_ShowsCapacityCells   ← Placeholder 99
        Bind_Null_HidesBar
배지    Digesting_Enters_ShowsBadge
        Digesting_Ends_HidesBadge
        Waiting_NotDigesting_DoesNotShowBadge         ← 대기와 소화를 섞지 않는다
통합    CustomerView_Bind_ResetsBarAndBadge           ← 자리를 갈아탈 때 직전 손님이 남지 않는가
```

> **`Refresh_PartiallyEaten_FillsExactCells` 에 구체값을 박는다.** `SaturationGauge` 를 그대로 부르는 구현이 맞지만, *"게이지와 같다"* 로 단언하면 뷰가 게이지를 안 부르고 아무 값이나 써도 통과할 수 있다 — 기대값을 숫자로 쓴다.

> **`Waiting_NotDigesting_DoesNotShowBadge` 가 필요한 이유:** 대기(파란 틴트)와 소화(회색 틴트)는 둘 다 "지금 안 먹는 상태" 라 구현에서 뭉뚱그리기 쉽다. 배지는 **소화에만** 뜬다.

### 주입 실측

| 주입 | 예측 | 실제 |
|---|---|---|
| 표시 칸을 넘는 칸을 끄지 않는다 | 1건 | **3건** |
| 배지를 소화가 아닌 상태에서도 띄운다 | 1건 | 예측대로 **1건** (`CustomerView_Eating_DoesNotShowBadge`) |
| `Bind` 가 재계산을 강제하지 않는다 | 1건 | **0건** — 아래 |

### 0건이 나온 주입 하나 — 테스트가 아니라 **코드가** 불필요했다

`Bind` 에 *"다음 `Refresh` 가 반드시 색을 쓰게"* 하는 한 줄(`ShownFilledCells = -1`)을 넣어
두었다가, 그것을 지워도 아무도 죽지 않았다. [`tests.md`](../../.claude/rules/tests.md) §3 의
3분기를 순서대로 배제했다:

1. **검사가 헛돌았나** — 아니다. 같은 편집에서 함께 넣은 배지 주입은 잡혔으므로 컴파일은 됐다
2. **관측 범위 밖인가** — 아니다. 칸 색은 **오직 찬 칸 수로만** 정해진다. 찬 칸 수가 같으면
   손님이 바뀌어도 그려야 할 색이 정확히 같고, 표시 칸을 넘는 칸은 이미 꺼져 있다
3. **테스트가 공허한가** — 아니다. **지킬 계약이 애초에 없었다**

그래서 테스트를 보강하는 대신 **그 줄을 지웠다.** 지워도 관측 결과가 같은 코드는 방어가
아니라 군더더기다 (R8). 주입이 0건일 때 반사적으로 테스트를 늘리면 검증은 안 늘고 코드만 는다.

### 애셋 테스트를 또 붙였다 — `CustomerPrefabTests`

step-03·step-04 와 같은 사각지대다. 두 뷰 테스트는 오브젝트를 **코드로** 세우므로
`Customer.prefab` 이 비어 있어도 초록이고, 뷰가 자식을 이름으로 찾는 폴백이 있어 프리팹에
자식이 없으면 표시가 통째로 빠지는데 예외는 나지 않는다.

**칸 수는 단언한다.** 위치·색은 연출이지만 개수는 계약이다 — 먹보의 포화도가 8 이라 칸이
그보다 적으면 그 손님만 비례로 접혀 한 입이 한 칸이 아니게 된다.

### 완료 판정

- [x] `DigestingBadgeView` 에 `CustomerState` **0건** (배지는 상태를 모른다)
- [x] `LateUpdate` 가 `CustomerView` 에만 있다 — 바·배지에는 **0건**
- [x] EditMode **630/630** · PlayMode **193/193**
- [x] **기존** `CustomerViewTests` 가 수정 없이 통과한다
- [x] `./tests/preflight.sh` 전 항목 PASS

### `InternalsVisibleTo` 를 하나 넓혔다

`CustomerRuntimeState.CurrentSaturation` 의 세터가 `internal` 이라 PlayMode 에서 *"세 칸 찬
손님"* 을 꾸밀 수 없었다. **프로덕션에 세터를 여는 대신** `Runtime/AssemblyInfo.cs` 에
`Tests.PlayMode` 를 더했다 — [`tests.md`](../../.claude/rules/tests.md) §7 이 지정한 탈출구이고,
`Tests.EditMode` 에 대해 이미 내린 결정을 한 줄 확장한 것이다.

실제 로직으로 초밥을 먹여 만들 수도 있었지만, 그러면 **표시 테스트가 집기 규칙까지 끌고 온다**
— 바가 깨졌을 때 원인이 표시인지 집기인지 구분되지 않는다.

### 배지는 아이콘이다 — 요구를 한 번 좁혔다

요구는 *"'소화중...' 인터페이스가 16x16px"* 인데 이 프로젝트의 픽셀 폰트는 12 px 이라
«소화중» 세 글자가 60 px 을 넘는다. 16×16 안에 글자를 넣을 방법이 없어 **아이콘 하나**로
그 상태를 말하게 했다.

말풍선 폭을 다시 정하면 문구를 넣을 수 있지만 그건 별도 기획이다. 스프라이트는 step-11 이
만들며, **애셋이 정확히 16×16 px 인지**는 그 단계가 테스트로 고정한다.

### 예상 커밋 메시지

```
feat(customer): show saturation cells and digesting badge
```

---

## 금지 사항

- 칸을 런타임에 생성하지 않는다
- 배지에 상태 판정을 넣지 않는다
- 포화도 칸 수를 SO 로 빼지 않는다 (연출 수치다)
- `CustomerView` 의 public 계약을 바꾸지 않는다
- 다른 어셈블리 파일을 수정하지 않는다
