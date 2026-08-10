---
description: 손님 몸통 동작(M9) — idle 시트가 대기 동작이 아닌 이유, Animator 소유권, 밸런스에서 유도하는 배속, 기다림은 말풍선
globs: ["Assets/Code/Scripts/Presentation/Customers/CustomerMotion*.cs", "Assets/Code/Scripts/Editor/Animations/*.cs", "Assets/Level/Animations/**"]
---

# 손님 동작 — 클립 두 장으로 네 모습을 만든다

손님이 화면에서 움직이는 방식. 게임 규칙은 담지 않는다 — 무엇을 집을지는
[`sushi-claim-flow.md`](sushi-claim-flow.md) 가, 여기 있는 것은 전부 그 결과를 몸으로
옮기는 이야기다.

---

## 1. `idle` 시트는 대기 동작이 **아니다**

이름이 정확히 반대로 읽히는 지점이라 먼저 못 박는다. 유형별 시트는 둘뿐이고
(`customer-{kind}-idle`, `customer-{kind}-picking`), 화면에 나가는 모습은 넷이다.

| 손님 상태 | 무엇이 나가나 |
|---|---|
| Idle | 클립이 아니라 **낱장** `CustomerData.Icon` |
| Idle + 대역 밖 초밥을 두고 유예 중 | 낱장 + 머리 위 `thinking-interface` |
| 집는 순간 | `picking` 클립 1회 |
| 먹는 중 | `idle` 클립을 **빠르게** |
| 소화 중 | `idle` 클립을 **느리게** |

**「먹기」와 「소화」는 같은 클립이고 갈리는 것은 재생 속도뿐이다.** 그래서 시트를
따로 그리지 않았다. 반대로 **아무 동작도 없을 때는 클립이 없다** — 낱장 한 장이 선다.

> `-idle` 이라는 이름을 「가만히 있는 모습」으로 읽고 Idle 상태에 물리면, 앉아 있는
> 손님이 전부 상시로 들썩인다. 시트 이름은 원본 그림의 이름이지 쓰임이 아니다.

## 2. 아무 동작도 없을 때 `Animator` 를 끄는 이유는 **소유권**이다

성능 때문이 아니다. 켜진 `Animator` 는 자기 클립이 묶은 `m_Sprite` 를 매 프레임 되쓰므로,
`CustomerData.Icon` 을 대입해도 **다음 프레임에 사라진다.** 예외도 경고도 없이 화면만
틀린다 → [`../knowledge/unity-scripting-gotchas.md`](../knowledge/unity-scripting-gotchas.md) §7.

- `CustomerMotion.None` 에서 `Animator.enabled = false` 로 내리고 낱장을 되돌린다
- **몸통 색은 충돌하지 않는다.** 클립이 스프라이트만 묶고 색은 안 묶어서, `CustomerView`
  가 상태 틴트를 계속 쥐고 있어도 된다
- `Animator` 는 **몸통 렌더러와 같은 오브젝트**여야 한다. 클립의 곡선이 빈 경로
  (=「컨트롤러가 달린 오브젝트 자신」)에 묶여 있어, 자식에 달면 아무것도 안 움직이는데
  **예외도 경고도 없다**

## 3. 배속은 밸런스에서 유도한다

`speed = cycles × clipLen / window`. 「초밥 하나를 먹는 동안 클립이 `cycles` 번 돈다」가
정의이고, 그 결과 **유형별 성격이 밸런스 수치 하나에서 따라 나온다.**

| | `EatSeconds` | 배속 |
|---|---|---|
| 먹보 | 1.0 | **1.5×** |
| 기본 | 1.5 | 1.0× |
| 소식 | 2.0 | **0.75×** |

- 산술은 `CustomerMotionSpeed` **한 곳**이다. `Runtime.Data` 는 계약이지 계산이 아니다
- **「몇 번 씹나」(`_chewCycles`·`_restCycles`)는 밸런스가 아니라 연출**이라 프리팹의
  `[SerializeField]` 에 산다 → [`presentation-and-audio.md`](presentation-and-audio.md) §3
- 클립 길이는 컨트롤러에서 **읽는다.** 시트 칸 수나 프레임 속도를 코드에 박으면 칸을
  하나 늘렸을 때 **배속만 조용히 어긋난다**
- **0 을 나누지 않는다.** `EatSeconds` 가 0 인 손님(placeholder)이 실제로 있고, 그대로
  나누면 배속이 무한대가 되어 `Animator` 가 조용히 멈춘다 — 첫 프레임에 굳은 손님만 남는다

집기는 이 유도 밖이다. 밸런스와 무관하게 늘 같은 속도라 배속이 `1×` 이고, 길이도 클립이
아니라 프리팹의 `_pickingSeconds` 가 정한다 — 클립에서 읽게 하면 `-picking` 이라는
이름 규칙이 계약으로 하나 더 는다.

## 4. 기다림은 색이 아니라 말풍선이 알린다

대역 밖 초밥을 두고 유예 중인 손님은 한때 **몸통이 파랬다.** 지금은 머리 위
`thinking-interface` 만 뜬다.

**같은 사실을 두 채널로 내보내면 상태 색이 통째로 대기에 묶여 먹기·소화가 묻힌다.**
몸통 색은 상태 셋(Idle/Eating/Digesting)만 나눠 갖는다.

- 말풍선은 **꺼진 채로** 프리팹에 저장돼 있어야 한다. 켜 두면 앉는 손님이 전부 고민하는
  것으로 보이고, 뷰는 **바뀐 프레임에만** 쓰므로 첫 프레임의 오해가 그대로 남는다
- 「기다리는 중」은 손님의 상태가 아니라 **조율자가 이번 틱에 내린 판정**이다
  (`ClaimCoordinator.IsWaiting`). `CustomerState` 에 넣으면 대기가 곧 굶기가 된다

## 5. 집기는 먹기를 **대체하지 않고 앞에 붙는다**

배정이 확정된 순간(`SushiClaimed`) 집기 동작이 `_pickingSeconds` 동안 먹기를 가리고,
남은 시간은 씹는 모습이다. 집기로 먹는 시간을 통째로 채우면 먹보와 소식가가 화면에서
구분되지 않는다.

- **집기는 먹는 중일 때만 살아 있다.** 먹다 만 초밥이 끝점에서 반납되면 손님은 `Idle` 로
  돌아가는데, 그때 집기가 남아 있으면 **아무것도 안 든 손님이 계속 집는 시늉을 한다**
- **조율자의 사건은 자리 넷에 모두 온다.** 뷰가 자기 손님인지 거르지 않으면 한 명이
  집을 때 넷이 함께 집는다
- 판정은 `MonoBehaviour` 밖이다 (`CustomerMotionSelector`). 전이 조건을 `Animator` 안에
  넣으면 **애니메이션 창을 열기 전에는 확인할 수 없다**

## 6. 시트를 개명하면 참조가 시한부가 된다

`f7b8202` 에서 `eating-interface.png` → `thinking-interface.png` 로 바꿨는데, `.meta` 안의
칸 이름은 `eating-interface_0…5` 로 남아 있었다. 당장은 멀쩡했다 — 클립은 이름이 아니라
`internalID` 로 물기 때문이다.

**다음 재임포트에 터진다.** 슬라이서가 파일 이름으로 칸 이름을 다시 짓는 순간 새 ID 가
발급되고 클립 참조가 `null` 이 된다. `PixelArtTextureImporter.GetVersion()` 을 올리는
것만으로도 충분하다.

→ `CustomerAnimationBuilder` 가 **굽기 직전에 시트를 강제 재임포트**한다. 끊긴 상태로
저장되는 일이 없게 하려는 것이다 →
[`../knowledge/unity-scripting-gotchas.md`](../knowledge/unity-scripting-gotchas.md) §4-8.

## 7. 테스트가 두 번 빗나갔다

「남의 배정을 거른다」를 검증하려다 **잘못된 배치를 두 번** 골랐다. 둘 다 거르는 코드를
통째로 지워도 초록이었다.

| 배치 | 왜 통과했나 |
|---|---|
| 이 손님을 `Idle` 로 두고 남의 사건을 받음 | `CustomerMotionSelector` 가 집기를 어차피 버린다 |
| 집기 시간을 아주 짧게 잡아 결정적으로 만듦 | 그 창이 한 프레임 안에 소진된다 |

「집기 없이 먹는 중」에 남의 사건이 도착하게 바꾸고서야 잡혔다 →
[`../rules/tests.md`](../rules/tests.md) §3.

**애셋과 씬은 각각 다른 그물이 지킨다:**

| 무엇이 끊기면 | 잡는 곳 |
|---|---|
| 프리팹에 `Animator`·말풍선이 없다 | `CustomerPrefabTests` |
| 손님 데이터에 컨트롤러가 없다 / 남의 것을 쓴다 | `CardCatalogAssetTests` |
| **둘을 잇는 `Bind` 가 빠졌다** | `Stage01SceneTests` |

셋째가 핵심이다 — 프리팹과 밸런스 애셋이 각각 멀쩡해도 **둘이 만나는 곳은 씬**이라,
앞의 둘만으로는 「먹어도 쉬어도 낱장 그림으로 서 있는」 상태를 못 잡는다.

## 8. 알려진 제약

- **`picking` 시트는 뒷모습이고 몸통은 앞모습이다.** 방향 처리가 없어 집는 순간만 뒤를
  보는데, **의도된 상태로 두기로 했다** (M9)
- 클립 길이를 읽을 때 `-idle` 접미가 계약이다. 런타임에는 상태 기계 안을 볼 수 없어
  (`AnimatorController` 는 에디터 전용) 이름 말고 가를 방법이 없다
- 컨트롤러를 굽는 것은 `CustomerAnimationBuilder`, 데이터·프리팹에 잇는 것은
  `CustomerMotionWiring` 으로 **나눠 두었다.** 합치면 클립을 다시 굽고 싶을 때마다
  사람이 손으로 잡아 둔 프리팹 배치까지 건드리게 된다
