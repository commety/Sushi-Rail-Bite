# 인스테이지 창과 판 흐름 (M6)

## 한 줄 요약

스테이지 위에 뜨는 창은 **한 번에 하나**이고, 그 창을 여닫는 것이 곧 판을 멈추고 잇는 일이라
— 어떤 창이 이기는지와 **어떤 창이 닫히면 안 되는지**가 같은 규칙 안에 있다.

## 핵심 타입 / 진입점

- `SushiDefense.UI.StageWindowArbiter` — `Assets/Code/Scripts/Presentation/Presenters/StageWindowArbiter.cs`
- `SushiDefense.UI.StageWindow` — 우선순위를 값으로 가진 열거형 (`CustomerInfo` 0 → `Menu` 3)
- `SushiDefense.UI.StageMenuPresenter` — 메뉴이자 **실패 창**
- `SushiDefense.UI.DeckPanelPresenter` · `RewardSelectionPresenter` — 조정자를 거치는 나머지 둘
- `SushiDefense.Stages.PauseState` — 멈춤의 유일한 진실 (→ [`stage-and-run.md`](stage-and-run.md))
- 조립 지점 — `StageBootstrap.EnsureRunScope` 가 조정자를 만들고 `CloseRequested` 를 받는다

## 유기적 관계

```
StageController.OutcomeDecided ─┬─ Cleared ─► RewardSelectionPresenter.Open
                                └─ Failed  ─► StageMenuPresenter.OpenAfterFailure

StageWindowArbiter ──CloseRequested──► StageBootstrap ──► Menu.Close / Deck.Close
       ▲                                                   (Reward 는 없다 — 아래 §2)
       └── TryOpen / Close ── 각 프레젠터
```

**조정자는 창을 모른다.** 열거형 값 하나만 받고, 내릴 창은 이벤트로 알린다 — 그래서
`Presentation` 안에서도 프레젠터끼리 서로를 참조하지 않는다.

## 1. 우선순위는 «밀어내기» 가 아니라 «거절» 이다

`메뉴 > 덱 > 보상 > 손님 정보`. 더 높은 창이 떠 있으면 낮은 창은 **열리지 않는다.**

낮은 창이 높은 창을 밀어내는 구현이 먼저 나왔는데, 그러면 실패 창 위에서 덱 버튼을 누른
순간 유일한 출구가 사라진다. **창의 우선순위는 «누가 위에 그려지나» 가 아니라 «누가 살아
남나» 를 정한다.**

## 2. 보상만 예외다 — 아래로 깔린다

`Reward` 는 양방향 예외다: **거절당하지 않고, 남을 막지도 않는다.**

보상 창을 **닫는 것이 곧 다음 스테이지로 넘어가는 동작**이라, 다른 창이 밀어내면 런이
진행되지 않는 상태로 멈춘다 — 다시 열 경로가 없다. 그래서 위에 창이 하나 겹칠 수 있는
유일한 창으로 두고, 겹친 창이 닫히면 그대로 다시 드러난다.

`StageBootstrap.OnWindowCloseRequested` 의 `switch` 에 **`Reward` 가 일부러 없다.** 조정자가
보상을 내리라고 말하는 경로 자체가 없어야 한다.

## 3. 실패 창은 메뉴 창이다 — 그래서 토글이 죽는다

실패했을 때 필요한 것은 «다시 시작» 과 «나가기» 뿐이고 둘 다 이미 메뉴에 있다. 전용 화면을
만들면 같은 버튼이 두 곳에 살고 조정자가 다룰 창이 하나 는다. 다른 점은 **재개할 수 없다**
는 것 하나라, 그것만 인자로 넘긴다 (`ShowMenu(bool canResume)`).

**대신 메뉴 아이콘이 실패 창 위에서는 아무 일도 하지 않는다.** 토글로 두면:

1. 누르는 순간 창이 닫히고 — **실패한 판에서 유일한 두 출구가 함께 사라진다**
2. 다시 누르면 실패를 모르는 `Open()` 을 지나 «일시정지 + 재개» 로 돌아온다 —
   이미 끝난 판으로 돌아가는 버튼이 생긴다

> **불변식**: *재개할 수 없는 창은 스스로 닫히지 않는다.* 닫는 것은 그 창의 다시 시작·나가기
> 뿐이고, 그 둘은 `Toggle()` 이 아니라 `Close()` 를 지난다.

## 4. 클리어는 멈추지 않고 흐른다

보상을 고르고 나면 **곧바로 다음 스테이지가 열린다.** 중간에 «계속하기» 를 두지 않는 이유는
누를 것이 하나뿐인 화면이기 때문이다.

**단 런의 마지막은 예외다** — `Transition.IsRunFinale` 이면 화면을 열어 둔다. 3스테이지를
끝냈다는 사실을 알릴 자리가 그것 말고 없다.

## 5. 창과 멈춤의 대응은 1:1 이 아니다

| 창 | 판이 멈추나 | 왜 |
|---|---|---|
| 메뉴 | **멈춘다** | 여는 것이 곧 멈춤이다. 열린 채 시간이 흐르는 네 번째 상태를 만들지 않는다 |
| 덱 | 안 멈춘다 | 덱 확인은 정보 조회다. 멈추면 «덱을 열어 시간을 번다» 가 생긴다 (M6 D8) |
| 보상 | — | 판정이 이미 났다. 멈출 판이 없다 |

## 알려진 제약 / 주의

- **조정자는 «지금 떠 있는 목록» 만 안다.** 실패했다는 사실은 `StageMenuPresenter.CanResume`
  이 들고 있고, 그 값은 **열 때만** 쓴다 — 닫힌 창의 값은 아무도 읽지 않는다
- **`Evict()` 는 목록에서 먼저 지우고 이벤트를 낸다.** 순서를 바꾸면 이벤트 핸들러가 부르는
  `Close()` 가 목록을 다시 건드려 순회 중 변경이 된다
- **창을 늘리면 우선순위 값을 다시 봐야 한다.** M6.5 의 손님 정보 창은 가장 아래(`CustomerInfo`)
  이고 «다른 창이 하나라도 있으면 안 열린다» 규칙을 이미 타고 있다 → [`../../docs/plan/M6.5-in-stage-ux.md`](../../docs/plan/M6.5-in-stage-ux.md)

## 관련 RULES.md 규칙

- 이벤트 구독 해제 — `StageBootstrap` 이 `CloseRequested` 를 `ReleaseRunScope` 에서 푼다

## 밸런스 수치의 근거

이 시스템에는 밸런스 수치가 없다. 우선순위는 **정책**이고 열거형 값 자체가 그 정책이다 —
SO 로 빼면 «메뉴보다 높은 정보 창» 같은 설정이 가능해지는데, 그것은 조정할 값이 아니라
버그다.
