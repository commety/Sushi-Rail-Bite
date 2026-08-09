# Step 04: 패널을 화면 안으로 밀어 넣는 계산 (P1-a)

- **영역:** `presentation` — 어셈블리 `Presentation` + `Tests.EditMode`
- **선행 단계:** 없음 (step-01·02·03 과 병렬 가능)
- **후행 단계:** step-05 가 `PanelAnchorMath.ClampInside` 를 부른다

---

## 목적

정보 창을 손님 옆에 띄우면 **가장자리 자리에서 화면 밖으로 밀려난다.** 씬의 자리는 월드
`x = -4, 0, 4, 6`, 패널은 200×280, 캔버스 기준 해상도는 960×540 이다 — 오른쪽 끝 자리(`x=6`)는
화면비에 따라 잘린다.

**뒤집지 않고 밀어 넣는다** (README D5). 뒤집기는 «어느 쪽으로» 라는 두 번째 규칙이 필요하고
그 규칙이 자리마다 다르게 보인다. 밀어 넣기는 규칙이 하나라 자리 수가 늘어도 그대로 성립한다.

**계산만 여기서 한다.** 월드→화면→캔버스 로컬 변환은 Unity 가 하고(step-05), 이 단계는
**«주어진 사각형을 주어진 경계 안으로»** 만 푼다. 경계값을 넣기 쉬워 EditMode 로 다 덮인다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

**TDD.** 실패하는 테스트를 먼저 쓰고 최소 구현으로 통과시킨다. Red 와 Green 이 작으므로
**한 커밋으로 묶는다** (`CLAUDE.md` §8 — 나누는 것은 실패 테스트 자체가 리뷰 대상일 때다).

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/UI/PanelAnchorMath.cs` — **생성**
- `Assets/Tests/EditMode/UI/PanelAnchorMathTests.cs` — **생성**

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>주어진 사각형을 경계 안으로 최소한만 민다. 순수 계산이다.</summary>
    public static class PanelAnchorMath
    {
        /// <param name="anchoredPosition">부모 로컬 좌표에서의 위치(피벗 기준점).</param>
        /// <param name="size">패널 크기.</param>
        /// <param name="pivot">패널 피벗 (0~1).</param>
        /// <param name="bounds">부모의 <c>RectTransform.rect</c> — 이미 피벗이 반영된 로컬 사각형이다.</param>
        /// <param name="margin">경계에서 띄울 여백.</param>
        public static Vector2 ClampInside(Vector2 anchoredPosition, Vector2 size,
                                          Vector2 pivot, Rect bounds, float margin);
    }
}
```

- **`Vector2` · `Rect` 는 값 구조체다.** 엔진 호출이 아니므로 `MonoBehaviour` 없이 EditMode 에서
  그대로 돈다 — `Tests.EditMode` 는 이미 `Presentation` 을 참조한다
- **`RectTransform` 을 인자로 받지 않는다.** 받는 순간 오브젝트를 세워야 검증할 수 있다
- **`Camera` 도, 화면 좌표도 모른다.** 변환은 step-05 의 몫이다

### 계약 (테스트로 못박을 것)

| # | 계약 |
|---|---|
| 1 | 이미 안에 들어 있으면 **그대로 돌려준다** |
| 2 | 오른쪽으로 넘치면 **왼쪽으로만** 민다 — `y` 는 안 바뀐다 (축 독립) |
| 3 | 왼쪽·위·아래도 각각 같다 |
| 4 | 두 축이 동시에 넘치면 **둘 다** 민다 |
| 5 | `margin` 이 결과에 들어간다 — `0` 과 `8` 이 **다른 값**을 낸다 |
| 6 | 피벗이 `(0.5, 0)` 이 아니어도 성립한다 |
| 7 | 패널이 허용 영역보다 **크면** 결정적으로 한쪽(최소 모서리)에 붙는다 — 예외를 던지지 않는다 |

### 공허하게 통과하지 않게 하는 조건 ([`rules/tests.md`](../../.claude/rules/tests.md) §3)

- **정사각형 패널·중앙 정렬 경계를 쓰지 않는다.** `size` 를 `200×280` 처럼 **가로세로가 다르게**,
  `bounds` 를 중심이 원점이 아니게 잡는다 — 그래야 `x`/`y` 를 바꿔 쓰거나 `min`/`max` 를 뒤집은
  구현이 걸린다
- **피벗을 볼 때는 «피벗만 다른 두 입력»을 나란히 놓는다.** 나머지가 같아야 피벗이 결과를
  가른다는 것이 드러난다. 피벗을 `(0.5,0.5)` 로만 테스트하면 피벗을 아예 안 쓰는 구현도 통과한다
- **«같다» 만 확인하지 않는다.** 계약 1(그대로)은 **상수를 돌려주는 구현**에서도 통과하므로,
  같은 테스트 안에 «넘치는 입력은 달라진다» 는 반례를 함께 박는다
- **여백을 볼 때는 여백이 0 이 아닌 경계를 고른다.** 다른 억제 장치가 결과를 같게 만들 수 있다

### 이름 규칙

`MethodName_StateUnderTest_ExpectedBehavior` — 예:

```
ClampInside_AlreadyInside_ReturnsUnchanged
ClampInside_OverflowsRight_ShiftsLeftOnly
ClampInside_OverflowsTwoAxes_ShiftsBoth
ClampInside_DifferentPivot_LandsAtDifferentPosition
ClampInside_LargerThanBounds_PinsToMinimumCorner
ClampInside_WithMargin_KeepsThatGap
```

### 밸런스 수치

없다. `margin` 은 인자로 들어오며, 그 값이 사는 곳은 step-05 의 직렬화 필드다.

### 제약

- **`Runtime` · `Runtime.Data` 를 참조하지 않는다** — 이 계산은 게임 규칙이 아니다
- **`MonoBehaviour` 를 만들지 않는다** (`CLAUDE.md` §3.2)
- `Update` 경로에서 불릴 수 있으니 **할당을 만들지 않는다** — `Vector2` 는 값 타입이므로
  그대로 두면 되고, LINQ·`params`·문자열 조립을 넣지 않는다 (§4.3)
- public API 에 `///` XML 문서 주석 (`CLAUDE.md` §4.4)

### 완료 판정

- [ ] `grep -rn "PanelAnchorMath" Assets/Code/Scripts/Presentation/` 로 정의 확인
- [ ] `./tests/preflight.sh` 전부 `[ok]`
- [ ] **공허 확인**: 구현에서 `margin` 무시 / `pivot` 무시 / `min`↔`max` 뒤집기를 **하나씩**
      주입하고, 지목한 테스트가 실제로 죽는지 본 뒤 되돌린다. **전량 통과가 나오면 테스트를
      먼저 고친다.** 결과는 예측이 아니라 실측으로 보고한다

### 예상 커밋 메시지

```
feat(ui): clamp a floating panel inside its parent rect
```

---

## 금지 사항

- 좌표 변환(`WorldToScreenPoint` · `ScreenPointToLocalPointInRectangle`)을 여기 넣지 않는다
- «화면 밖이면 반대편으로 뒤집기» 를 구현하지 않는다 (README D5)
- 시그니처를 임의로 바꾸지 않는다 — step-05 가 이대로 부른다. 바꿔야 하면 멈추고 보고한다
