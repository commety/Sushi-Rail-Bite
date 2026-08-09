# Step 04: 소화 배지가 남은 초를 말한다

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** 없음 (다른 세 갈래와 독립)
- **후행 단계:** step-09 가 씬에서 배지 라벨을 확인한다

---

## 목적

지금 배지는 **켜고 끄기만** 한다. 손님이 쉬는 중인 것은 알아도 «언제 다시 먹나» 를 알 수 없어, 플레이어가 그 자리를 포기해야 하는지 기다려야 하는지 판단할 수 없다.

남은 시간은 `CustomerRuntimeState.RemainingDigestSeconds` 가 **이미 들고 있다** — 새 계산이 없고 표시만 는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/Customers/DigestingBadgeView.cs` — 수정
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (`Apply` · `LateUpdate`)
- `Assets/Code/Scripts/Presentation/Customers/Customer.prefab` — 수정 (배지 아래 라벨 하나)
- `Assets/Tests/PlayMode/Customers/DigestingBadgeViewTests.cs` — 수정
- `Assets/Tests/PlayMode/Customers/CustomerViewTests.cs` — 수정
- `Assets/Tests/EditMode/Customers/CustomerPrefabTests.cs` — 수정
- `Assets/Tests/EditMode/UI/KoreanFontCoverageTests.cs` — 수정 (숫자만 — 이미 있는지 확인)

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    public sealed class DigestingBadgeView : MonoBehaviour
    {
        /// <summary>
        /// 배지를 띄우고 남은 초를 적는다. <b>초 단위로 잘린 값을 받는다</b> —
        /// 자르는 것은 부르는 쪽의 일이다. 여기서 자르면 «값이 바뀐 프레임에만» 을
        /// 판단할 곳이 둘로 갈린다.
        /// </summary>
        public void Show(int remainingSeconds);

        /// <summary>지금 배지에 적혀 있는 글자. 검증용이다.</summary>
        public string RemainingText { get; }
    }
}
```

**기존 인자 없는 `Show()` 를 남기지 않는다.** 남기면 «숫자 없이 뜨는 배지» 경로가 그대로 살아, 어느 쪽이 불렸는지 화면에서만 드러난다.

`CustomerView` 쪽:

```csharp
private int _shownDigestSeconds = -1;   // -1 = 아직 안 그림

// LateUpdate 안, 상태·대기 감지와 나란히
if (_digestingBadge != null && Logic.State.State == CustomerState.Digesting)
{
    var seconds = Mathf.CeilToInt(Logic.State.RemainingDigestSeconds);
    if (seconds != _shownDigestSeconds)
    {
        _shownDigestSeconds = seconds;
        _digestingBadge.Show(seconds);
    }
}
```

`CeilToInt` 를 쓰는 이유는 `StageHudView.RefreshTime` 과 같다 — 0.3초 남았을 때 `0` 보다 `1` 이 낫고, 실제로 0 이 되는 순간은 소화가 끝나는 시점뿐이다.

상태가 `Digesting` 이 아니게 되면 `Hide()` 와 함께 `_shownDigestSeconds = -1` 로 되돌린다. **되돌리지 않으면 다음 소화가 같은 초에서 시작할 때 숫자가 안 그려진다** — 배지는 뜨는데 글자가 직전 값이거나 비는 형태다.

### 프리팹 변경

`Customer.prefab` 의 `DigestingBadge` 아래에 `RemainingLabel` (TMP) 을 둔다.

- 배지는 **16×16 그대로**다. 크기·씬 레이아웃·스프라이트를 건드리지 않는다
- 폰트 12px, 한 자리 숫자 — 실측 소화 시간 3~3.5초 (`Customer.*.asset`)
- 자식 이름 `RemainingLabel` 은 `DigestingBadgeView` 의 상수와 맺는 약속이다. `HudLabel.Resolve` 방식으로 인스펙터가 비면 자기 하위에서만 찾는다

> **두 자리가 되면 안 들어간다.** 소화 시간을 10초 이상으로 올리는 밸런스 변경이 오면 이 표시가 먼저 깨진다 — 그 사실을 `DigestingBadgeView` 클래스 주석에 남긴다. 지금 막지는 않는다 (밸런스는 오류가 아니다).

### 선행 산출물 의존성

- `SushiDefense.Customers.CustomerRuntimeState.RemainingDigestSeconds` — 이미 있음

### 밸런스 수치

없다. 소화 시간은 이미 `CustomerData.DigestSeconds` 다.

### 제약

- **`Update`/`LateUpdate` 에서 매 프레임 문자열을 만들지 않는다** (`CLAUDE.md` §4.3). 초가 바뀐 프레임에만 쓴다
- **배지가 상태를 되묻지 않는다.** 언제 뜰지는 `CustomerView` 가 정한다 — 지금의 계약 그대로다 (§3.2·§3.5)
- 배지가 없어도 손님은 정상 동작해야 한다 (`_digestingBadge == null` 경로 유지)
- 폰트: 숫자는 이미 커버돼 있다 (`Font_CoversDigitsAndSeparators`). **새 한글 문구를 넣지 않는다** — 넣으면 커버리지 테스트에 줄을 더해야 한다

### 테스트 계획

**억제 장치가 주입을 가릴 수 있다** ([`tests.md`](../../.claude/rules/tests.md) §3). 여기서 그 장치는 «초 단위 변화 감지» 다 — 초가 안 바뀌는 구간을 고르면 잘못된 구현도 통과한다.

```
DigestingBadgeViewTests
  Show_WithSeconds_WritesTheNumber            ← "3" 구체값
  Show_WithSeconds_TurnsTheBadgeOn            ← 숫자와 표시를 분리해 본다
  Hide_AfterShow_ClearsTheNumber              ← 남은 글자가 다음 소화에 번쩍이지 않는다

CustomerViewTests
  LateUpdate_Digesting_ShowsRemainingSeconds
  LateUpdate_DigestSecondsUnchanged_DoesNotRewrite   ← 갱신 횟수를 세어 «바뀐 프레임에만» 을 고정
  LateUpdate_SecondCrossed_Rewrites                  ← 위와 짝. 하나만 있으면
                                                        «아예 안 쓰는» 구현도 통과한다
  LateUpdate_LeftDigesting_HidesAndResetsCache       ← 두 번째 소화에서 숫자가 다시 그려지는지
```

- **`LateUpdate_LeftDigesting_HidesAndResetsCache` 는 소화를 두 번 돌린다.** 한 번만 돌리면 `_shownDigestSeconds` 를 안 되돌리는 구현이 통과한다 — 실제 증상은 «두 번째 소화부터 숫자가 안 뜬다» 이고, 이것이 이 프로젝트가 반복해 겪은 «두 번째부터/가끔» 형태다
- 갱신 횟수는 `DigestingBadgeView` 에 쓰기 횟수를 세는 검증용 프로퍼티를 두거나, `RemainingText` 를 비교해 확인한다

### 완료 판정

- [ ] `grep -n "Show(int" Assets/Code/Scripts/Presentation/Customers/DigestingBadgeView.cs` — 정의 1건
- [ ] `grep -rn "_digestingBadge.Show()" Assets/Code/Scripts/` — **0건** (인자 없는 옛 경로가 남아 있지 않다)
- [ ] `CustomerPrefabTests` 가 `DigestingBadge/RemainingLabel` 존재를 본다
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] 깨뜨려 보기: `_shownDigestSeconds` 리셋을 지우면 `LateUpdate_LeftDigesting_HidesAndResetsCache` 가 죽는지 확인하고 되돌린다

### 예상 커밋 메시지

```
feat(customer): count the digest badge down in seconds
```

---

## 금지 사항

- 배지를 키우거나 옆으로 옮기지 않는다. 크기를 바꾸면 씬 레이아웃과 자리 간격이 따라온다.
- 원형 게이지·새 스프라이트를 만들지 않는다 (README D4).
- `CustomerLogic`·`CustomerAppetiteMachine` 등 `Runtime` 을 건드리지 않는다. 남은 시간은 이미 나와 있다.
