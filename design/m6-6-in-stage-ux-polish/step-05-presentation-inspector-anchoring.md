# Step 05: 정보 창을 누른 손님 옆에 띄운다 — 배선 (P1-b)

- **영역:** `presentation` — 어셈블리 `Presentation` + `Tests.PlayMode`
- **선행 단계:** **step-04 필수** — `PanelAnchorMath.ClampInside` 를 쓴다
- **후행 단계:** step-06 이 `Stage01.unity` 를 이 시그니처에 맞춰 조립한다

---

## 목적

> 원했던 동작은 해당 손님 위에 오버레이되는 것이지만 우하단에 고정되어 나타난다.

M6.5 는 화면 우하단 고정으로 만들었다 — 작업서가 그렇게 적었고 그대로 나왔다.

**월드 좌표가 창까지 닿는 길이 없다는 것이 문제의 전부다.** 손님은 월드 스페이스, 창은 Canvas
이고, 지금 라우터는 «어느 손님인가» 만 넘기고 «어디였나» 는 버린다. 그 값을 흘려보내면 나머지는
step-04 의 계산과 Unity 의 변환으로 끝난다.

**열 때 위치를 잡고 고정이다** (README D4). 따라다니지 않는다 — 손님은 자리에 앉아 있고,
매 프레임 `WorldToScreenPoint` 는 그대로 `Update` 경로 비용이다 (§4.3).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Views/CustomerInspectorView.cs` — 수정 (`AnchorTo` 추가)
- `Assets/Code/Scripts/Presentation/Customers/CustomerTapRouter.cs` — 수정 (콜백에 좌표 추가)
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (`BuildInspector` 배선)
- `Assets/Tests/PlayMode/UI/CustomerInspectorViewTests.cs` — 수정 (앵커 검증)
- `Assets/Tests/PlayMode/Customers/CustomerTapRouterTests.cs` — 수정 (콜백 시그니처)

### 1) `CustomerInspectorView.AnchorTo`

```csharp
/// <summary>이 월드 좌표 위에 창을 세운다. 화면 밖으로 나가면 안으로 민다.</summary>
public void AnchorTo(Vector3 worldPoint);
```

동작:

1. `Resolve()` — 꺼진 오브젝트의 `Awake` 는 안 돌았을 수 있다 (M6.5 에서 실제로 났던 사고)
2. 월드 → 화면: `_worldCamera.WorldToScreenPoint(worldPoint)`
3. 화면 → 부모 로컬:
   `RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, uiCamera, out local)`
4. `local + (0, _gap)` 을 원하는 위치로 삼고
   `PanelAnchorMath.ClampInside(desired, rect.rect.size, rect.pivot, parentRect.rect, _edgeMargin)`
5. 결과를 `anchoredPosition` 에 대입

**좌표계를 맞추는 두 가지 못**:

- **`uiCamera` 는 `ScreenSpaceOverlay` 에서 `null` 이다.** 씬의 Canvas 는 `renderMode 0`
  (Overlay) 이므로 `canvas.worldCamera` 를 그대로 넘기면 되고 그 값이 `null` 이다.
  `Camera.main` 을 넘기면 좌표가 통째로 어긋난다 —
  [`knowledge/unity-scripting-gotchas.md`](../../.claude/knowledge/unity-scripting-gotchas.md) §6
- **패널의 `anchorMin`·`anchorMax` 를 부모의 `pivot` 과 같게 맞춘다.** 그래야 3번이 준 로컬
  좌표가 곧 `anchoredPosition` 이 된다. 이것을 씬에 맡기지 않고 `Resolve()` 에서 **코드가
  못박는다** — 배치가 아니라 이 창의 **동작 계약**이고, 씬에서 어긋나면 «조금 빗나간 위치» 라
  눈으로 원인을 못 찾는다. `pivot` 은 `(0.5, 0)` — 손님 위로 자란다

새 직렬화 필드 (연출 수치, M5 D7):

| 필드 | 뜻 | 제안 |
|---|---|---|
| `_worldCamera` | 좌표 변환용. 비면 `Camera.main` 폴백 | — |
| `_gap` | 손님과 창 아래변 사이 (캔버스 단위) | `40` |
| `_edgeMargin` | 화면 가장자리 여백 | `8` |

> 손님 몸통은 1 월드 유닛이고 카메라 `orthographicSize 5` · 기준 해상도 540 이므로
> **1 유닛 ≈ 54 캔버스 단위**다. 머리 위로 나오려면 `_gap` 이 27 보다 커야 한다 —
> 40 은 그 근거에서 나온 출발점이고 **사람이 확정한다**.

### 2) `CustomerTapRouter` — 좌표를 흘려보낸다

```csharp
private Action<CustomerLogic, Vector2> _onTapped;   // 였다: Action<CustomerLogic>

public void Bind(TableSlotView[] slots, Camera worldCamera,
                 Action<CustomerLogic, Vector2> onTapped, Action onMissed = null);
```

- `TapAt(Vector2 worldPoint)` 이 **받은 그 좌표**를 넘긴다
- **자리 좌표가 아니라 «누른 좌표» 를 넘길지**를 결정한다. `SlotPicker` 가 고른 자리의
  `transform.position` 을 넘기는 쪽이 낫다 — 손님 몸통 가운데를 기준으로 창이 서므로
  **같은 손님을 두 번 눌러도 창이 같은 자리에 뜬다.** 누른 좌표를 쓰면 창이 손끝을 따라 흔들린다
- `_onMissed`·`TapCount`·uGUI 판별은 **그대로 둔다**

### 3) `StageBootstrap.BuildInspector`

```csharp
_tapRouter.Bind(_slots, null, OnCustomerTapped, Inspector.Close);
…
private void OnCustomerTapped(CustomerLogic customer, Vector2 worldPoint)
{
    _inspectorView.AnchorTo(worldPoint);   // 먼저 세우고
    Inspector.Open(customer);              // 그 다음 띄운다
}
```

- **순서가 있다.** 먼저 띄우면 한 프레임 동안 직전 손님 자리에 창이 보인다
- `Bind` 의 두 번째 인자 `null` 은 **«이미 찾아 둔 카메라를 지우지 않는다»** 라는 기존 계약이다.
  건드리지 않는다 (M5 에서 손패에 실제로 났던 사고)
- 람다를 쓰지 않고 **메서드 그룹**으로 넘긴다 — 구독 해제 대칭이 필요해지면 그 편이 안전하고,
  매 프레임이 아니어도 할당을 남길 이유가 없다

### 4) 테스트

**PlayMode 인 이유**: `RectTransform` 과 `Camera` 를 실제로 세워야 좌표가 돈다. 계산 자체는
step-04 가 EditMode 로 이미 덮었으므로 **여기서는 «배선이 이어지는가» 만** 본다.

| 볼 것 | 어디 |
|---|---|
| `AnchorTo` 뒤 `anchoredPosition` 이 **바뀐다** (기본값에 머물지 않는다) | `CustomerInspectorViewTests` |
| 서로 다른 월드 좌표 둘이 **서로 다른** 위치를 낸다 | 〃 |
| 화면 밖 월드 좌표를 주면 패널이 **부모 사각형 안에 들어 있다** | 〃 |
| `Resolve` 전(=`Awake` 미실행)에 불러도 던지지 않는다 | 〃 |
| 라우터가 손님 **과 좌표**를 함께 넘긴다 | `CustomerTapRouterTests` |
| 넘어온 좌표가 **자리 좌표와 같다** | 〃 |

**공허하게 통과하지 않게**:

- 카메라를 세울 때 **직교 · `z = -10`** 으로 둔다. 기본 원근 카메라를 원점에 두면 좌표 왕복이
  깨진다 (M6.5 에서 실제로 걸렸다)
- **«바뀌었다» 만 보지 않는다.** 상수를 대입하는 구현도 통과한다 — 서로 다른 두 입력이 서로
  다른 결과를 낸다는 반례를 같은 테스트에 함께 박는다
- **클램프를 볼 때는 여백이 0 이 아닌 값**으로 잡는다. 0 이면 «클램프 안 함» 과 결과가 같아진다
- 라우터 테스트의 기존 `_tapped.Add` 는 시그니처가 달라져 **컴파일이 깨진다.** 지우지 말고
  `(logic, point)` 를 받는 수집기로 바꾼다 — 좌표가 새 검증 대상이다

### 밸런스 수치

없다. `_gap`·`_edgeMargin` 은 연출 수치라 뷰의 직렬화 필드에 산다 (M5 D7).

### 제약

- **`CustomerInspectorPresenter` 와 `ICustomerInspectorView` 를 건드리지 않는다.**
  인터페이스는 *"글자만 오간다"* 라고 스스로 적어 두었고, 좌표는 화면 계층의 일이다.
  `AnchorTo` 는 **구현 클래스에만** 둔다
- `Update` 에 좌표 변환을 넣지 않는다 (README D4 — 열 때 한 번)
- `Find` / `FindObjectOfType` 금지. 카메라는 직렬화 필드 + `Camera.main` 폴백 (§4.3)
- **씬 파일을 열지 않는다** — step-06 의 몫이다
- 이벤트 구독을 새로 만들지 않는다. 만들면 `OnDestroy` 해제 쌍이 필요하다 (RULE)

### 완료 판정

- [ ] `grep -rn "Action<CustomerLogic>" Assets/` → **0건** (전부 좌표를 함께 받는다)
- [ ] `grep -rn "AnchorTo" Assets/Code/Scripts/Presentation/` 로 정의·호출 확인
- [ ] `./tests/preflight.sh` 전부 `[ok]` (PlayMode 포함: `./tests/run-tests.sh all`)
- [ ] **공허 확인**: `AnchorTo` 안에서 `ClampInside` 호출을 빼고 / `_gap` 을 무시하고 /
      `uiCamera` 대신 `Camera.main` 을 넘겨 **하나씩** 돌린다. 어느 테스트가 죽는지 **실측으로**
      보고한다 — «잡힐 것이다» 는 M4 에서 세 번 빗나갔다
- [ ] 기존 정보 창 테스트 5건(열림·닫힘·클리어 시 닫힘·라벨·시작 시 꺼짐)이 **여전히 통과**

### 예상 커밋 메시지

```
feat(ui): open the customer inspector beside the customer
```

---

## 금지 사항

- `ICustomerInspectorView` 에 좌표를 추가하지 않는다
- 창이 손님을 **따라 움직이게** 만들지 않는다 (README D4)
- 화면 밖일 때 **반대편으로 뒤집지** 않는다 (README D5)
- `Stage01.unity` 를 열지 않는다
