# M6.5 — 인스테이지 UX 확장

## 한 줄 요약

스테이지 안에서 **손님이 자기 정보를 말하게** 한다 — 자리를 얼마나 차지하는지(인구수), 언제 다시 먹는지(소화 잔여), 무엇을 노리는지(정보 창), 그리고 그 전부를 담을 **커진 카드**와 평소엔 접혀 있는 손패.

## 원문

[`docs/plan/M6.5-in-stage-ux.md`](../../docs/plan/M6.5-in-stage-ux.md) — M6 step-13 완료 후 WebGL 실플레이 리포트(2026-08-09)에서 **오류가 아니라 신규 기능**으로 갈라낸 넷.

| # | 요구 | 이 작업서에서 |
|---|---|---|
| F1 | 먹보는 손님 수 2를 차지한다 | step-01 · 02 · 03 |
| F2 | 소화 중 남은 시간을 배지에 표시 | step-04 |
| F3 | 배치된 손님을 클릭하면 정보 창 | step-05 · 06 |
| F4 | 카드 128×192 규격 + 접히는 손패 | step-07 · 08 |

## 아키텍트 결정 (3문 3답 — 착수 시 확인)

| 물음 | 답 |
|---|---|
| 인구수 필드 이름 | **`Population`** — 자리를 둘 차지한다는 오해를 만들지 않는 이름 |
| 소화 잔여 표시 | **배지 안 숫자 한 자리** — 새 스프라이트가 붙지 않는다 |
| 128×192 로 커지는 카드 프레임 | **9-slice 테두리를 넣는다** — 원본을 다시 그리지 않는다 |

## 착수 전 실측 — 지금 저장소의 상태

추측이 아니라 `grep`·파일 조회·PNG 헤더·폰트 커버리지 계산 결과다.

| 확인 대상 | 상태 | 영향 |
|---|---|---|
| **선행 조건 ①** 창 우선순위 조정자 | `StageWindowArbiter` 존재. `StageWindow.CustomerInfo = 0` 이 **이미 등재돼 있고** 「가장 아래 창은 남의 위에 겹치지 않는다」 규칙까지 들어 있다 | **F3 는 항목을 새로 만들지 않는다.** 프레젠터가 `TryOpen(CustomerInfo)` 를 부르기만 하면 된다 → step-05 |
| **선행 조건 ②** 손패 갱신 경로 | `CustomerHandView.Watch(RecruitWallet)` 이 M6 `673b0e5` 에서 들어감 | 잔액 변화 → `Refresh()` 경로가 이미 있다. F1 은 **그 경로를 다시 만들지 않고** 판정만 바꾼다 |
| `CustomerPlacementService.PlacedCount` | `_bySlot.Count` (머릿수) | **`AudioDirector` 가 이 값이 «늘었나» 로 배치음을 낸다** (`AudioDirector.cs:233~240`). 의미를 인구수로 바꾸면 먹보 하나에 값이 2 뛰는데, 소리는 그대로 한 번 나서 **테스트가 초록인 채 의미만 어긋난다** → D2 |
| `StageConfig._maxPlacedCustomers` | 직렬화 필드 이름 | **이름을 바꾸면 `.asset` 3장의 값이 조용히 기본값으로 날아간다.** M3 에서 직렬화 필드 이름을 바꿔 씬의 손님 참조가 통째로 사라진 사고와 같은 형태다 ([`tests.md`](../../.claude/rules/tests.md) §1) → D3 |
| 자리 수 ↔ 배치 한도 | Stage01 **3/3** · Stage02 **4/4** · Stage03 **4/4** | **지금은 한도가 자리 수와 같아 아무것도 막지 않는다.** 인구수가 들어오면 «자리는 비었는데 못 앉힌다» 가 처음 생긴다 — 의도한 긴장이지만 수치는 사람이 다시 봐야 한다 (§7) |
| 한글 폰트 | M6 `9230d73` 에서 **폰트 전체 커버리지**로 전환 (3,248자) | **원문의 «한글 서브셋 재굽기가 따라온다» 는 낡은 전제다.** F2·F3·F4 가 쓸 문구 전부를 실측해 **누락 0자** 확인 — 재굽기 없음 |
| 손님 아이콘 | `customer-*.png` **32×32** | F4 의 «32×32 이미지 공간» 과 **1:1**. 원문의 «4배 확대하는가» 질문은 실측으로 사라진다 — 정수배 확대 결정이 필요 없다 |
| `card-frame.png` | **48×64**, `spriteBorder: 0,0,0,0` | 128×192 로 늘리면 픽셀이 뭉개진다. `panel.png` 는 이미 `(8,8,8,8)` 로 그 문제를 푼 상태 → D8 |
| `Card.prefab` | 루트 **96×128**, `Icon` **64×64** | 128×192 / 40×40 으로. `CustomerCard.prefab` 이 96×128 을 **오버라이드로** 들고 있어 같이 고쳐야 한다 |
| 손님 클릭 히트박스 | 손님은 월드 `SpriteRenderer`, **콜라이더 0건** | `SlotPicker` 의 거리 비교를 재사용한다 → D6 |
| 입력 백엔드 | `ENABLE_LEGACY_INPUT_MANAGER` **미정의** | `UnityEngine.Input` 은 런타임 예외. `UnityEngine.InputSystem` 만 쓴다 (`InputBackendTests` 가 고정) |
| 워크트리 | primary — `Assets/Art` 가 심링크가 **아니다** | 아트 편집은 가능하지만 §9 커밋 승인은 그대로다 |
| `CardCaption` | `DetailOf` 가 한 줄 문자열 | F4 의 스탯 «행» 은 여기서 만든다 → D10 |

## 아키텍처 결정

- **D1. 인구수는 `Population` 이고, 자리는 하나만 차지한다.**
  요구가 적은 것은 *"손님 수 2"* 이지 *"자리 2"* 가 아니다. 자리 점유까지 늘리면 `_bySlot` 구조·`SlotPicker`·드롭 판정·씬 자리 배치가 통째로 딸려 오고, **화면에 «반쯤 앉은 손님»** 을 그려야 한다. 이름을 `SeatCost` 로 하지 않는 것도 같은 이유다 — [`scriptable-object.md`](../../.claude/rules/scriptable-object.md) §7 이 *"이름이 잘못되면 다음 사람이 반드시 그렇게 쓴다"* 고 적어 둔 그대로다.

- **D2. `PlacedCount` 는 머릿수로 남기고, `PlacedPopulation` 을 새로 만든다.**
  `AudioDirector` 가 `PlacedCount` 의 증가로 배치음을 낸다. 의미를 갈아끼우면 **소리는 여전히 한 번 나므로 아무 테스트도 죽지 않는데**, «한 명 앉았다» 를 세는 자리가 «인구 2 늘었다» 로 바뀐다. 한도 판정만 새 값을 쓰고, 세는 자리는 세던 것을 계속 센다.

- **D3. `StageConfig._maxPlacedCustomers` 의 이름을 바꾸지 않는다. 의미만 바뀐다.**
  직렬화 키라서 이름을 바꾸면 `.asset` 3장의 값이 **경고 없이** 기본값으로 돌아간다. XML 문서 주석과 [`data-model.md`](../../.claude/domain/data-model.md) 가 의미 변경을 진다.

- **D4. 소화 잔여는 배지 «안» 의 한 자리 숫자다.**
  `DigestingBadgeView` 의 주석은 *"12px 폰트라 «소화중» 세 글자가 16×16 에 안 들어간다"* 고 적었는데, 그것은 **세 글자**에 대한 판단이다. 소화 시간은 실측 3~3.5초라 한 자리이고 숫자는 폰트에 이미 있다. 배지 크기·씬 레이아웃·스프라이트가 하나도 안 움직인다.

- **D5. 정보 창은 판을 멈추지 않는다.**
  M6 D8 이 «덱 보기는 멈추지 않는다» 를 이미 정했고, 정보 조회라는 성격이 같다. 멈추면 «정보 창을 열어 시간을 번다» 가 생긴다.

- **D6. 클릭 판정에 콜라이더를 도입하지 않는다.**
  자리는 셋~넷뿐이고 좌표가 알려져 있어 `SlotPicker.Pick` 이 그대로 답한다. 콜라이더를 붙이면 씬에 관리할 것이 늘고 물리 질의에 호출 시점 제약(RULE-04)이 따라온다.

- **D7. 「드롭」과 「클릭」을 나눈다.**
  카드를 자리에 떨어뜨리는 동작과 자리를 눌러 보는 동작이 **같은 좌표에서 끝난다.** 구분선은 둘이다 — ⑴ 드래그가 진행 중이었으면 클릭이 아니다, ⑵ 포인터가 uGUI 위에 있으면(`EventSystem.current.IsPointerOverGameObject()`) 클릭이 아니다. 손패 카드는 항상 uGUI 위에 있으므로 ⑵ 하나로도 대부분 갈리지만, **드래그 끝점은 자리 위(uGUI 밖)** 라 ⑴ 이 반드시 필요하다.

- **D8. 카드 프레임은 9-slice 로 늘린다.**
  48×64 원본을 128×192 로 스트레치하면 픽셀 아트의 각이 죽는다. 테두리를 지정하면 모서리는 원본 픽셀 그대로, 가운데만 늘어난다. **`.meta` 편집이라 사람이 인스펙터에서 한다** (RULE-03 — 에이전트는 `.meta` 를 직접 고치지 않는다).

- **D9. 접히는 손패는 «컨테이너» 를 움직인다. 카드를 움직이지 않는다.**
  `CustomerCardDrag._home` 은 `Resolve()` 시점의 `anchoredPosition` 이고, 드래그가 끝나면 거기로 돌아간다. 카드 자체를 접힘 애니메이션으로 옮기면 **카드가 올라온 상태에서 끌었을 때 `_home` 이 접힌 위치를 가리켜** 카드가 화면 밖으로 돌아간다. 부모 컨테이너를 움직이면 카드의 로컬 좌표가 안 변해 이 문제가 아예 생기지 않는다.

- **D10. 스탯 «행» 은 `CardCaption` 이 만든다. 뷰가 조립하지 않는다.**
  M6 D7 이 이름 폴백을 한 벌로 접은 자리다. 행을 뷰에서 이어 붙이면 초밥 카드와 손님 카드가 각자 조립하게 되고, 그 순간 «타입으로 분기하는 뷰» 가 다시 생긴다.

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `CustomerData.Population` — 인구수 |
| runtime | `Runtime` | `CustomerPlacementService` — 한도가 세는 단위 |
| presentation | `Presentation` | 배지 숫자 · 정보 창(프레젠터+뷰+입력) · 카드 레이아웃 · 접히는 손패 |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 위 전부 + 프리팹 애셋 검증 |
| — | (애셋) | `Customer.*.asset` 4장 · 카드 프리팹 2장 · `card-frame` 9-slice · `Stage01.unity` |

**새 어셈블리는 없다.** 새 패키지도 없다.

## 의존성 그래프

```
SushiDefense.Data.CustomerData.Population
        └─► SushiDefense.Customers.CustomerPlacementService.PlacedPopulation
                    ├─► SushiDefense.UI.StageHudView            (손님 n/m)
                    ├─► SushiDefense.Customers.CustomerHandView (카드 흐리기)
                    └─► SushiDefense.UI.CustomerInspectorPresenter

SushiDefense.Customers.CustomerRuntimeState.RemainingDigestSeconds
        ├─► SushiDefense.Customers.DigestingBadgeView   (배지 숫자)
        └─► SushiDefense.UI.CustomerInspectorPresenter  (정보 창)

SushiDefense.UI.StageWindowArbiter ──► CustomerInspectorPresenter ──► ICustomerInspectorView
SushiDefense.Customers.SlotPicker  ──► CustomerTapRouter ──────────┘
SushiDefense.UI.CardCaption ───────► SushiDefense.UI.CardView (4구획)
```

`Runtime` 은 `Presentation` 을 여전히 모른다. 정보 창은 **프레젠터가 `Runtime` 타입을 읽고 뷰에는 문자열만 넘긴다.**

## 새 밸런스 수치

| 수치 | 들어갈 곳 | 초기값 제안 | 근거 |
|---|---|---|---|
| `Population` — 기본 | `Customer.Standard.asset` | `1` | 기준값 |
| `Population` — 소식 | `Customer.SmallEater.asset` | `1` | 정수 유지. 0.5 를 도입하면 한도 표시가 `2.5/4` 가 되어 읽기 나쁘다 |
| `Population` — 먹보 | `Customer.BigEater.asset` | **`2`** | 요구 그대로 |
| `Population` — Placeholder | `Customer.Placeholder.asset` | `1` | 테스트용, 판정에 끼지 않게 |
| `MaxPlacedCustomers` 재조정 | `Stage01~03.asset` | **미정 — 사람이 정한다** | 지금 한도 = 자리 수라 인구수가 들어오면 실질 한도가 준다. Stage01 은 3/3 이라 «먹보+기본» 이면 세 번째 자리가 영영 빈다 |
| 카드 크기 · 여백 | 프리팹 `[SerializeField]` / `RectTransform` | 128×192, 아이콘 40×40(패딩 8), 이름 12, 비용 16 | 요구가 픽셀로 못박았다. 연출 수치 (M5 D7 구분선) |
| 손패 접힘 노출 높이 · 전개 시간 | `CustomerHandView` 의 `[SerializeField]` | 노출 `48`, 전개 `0.12초` | 연출 수치 — 그 프리팹 하나에만 의미가 있다 |

> **`.asset` 에 값을 쓰는 것은 사람의 판단 영역이다** (`CLAUDE.md` §7). step-01 이 diff 를 제시하고 멈춘다.

## §7 승인이 필요한 항목

각 단계가 그 지점에서 **멈추고** 요청한다.

| # | 항목 | 단계 |
|---|---|---|
| 1 | `Customer.*.asset` 4장에 `_population` 값 기입 | step-01 |
| 2 | `Stage01~03.asset` 의 `_maxPlacedCustomers` 재조정 | step-03 (실측 뒤) |
| 3 | `card-frame.png` · `card-frame-disabled.png` 의 `spriteBorder` 지정 — **`.meta` 편집이라 사람이 인스펙터에서** (RULE-03) | step-07 |
| 4 | `CLAUDE.md` §2 용어 사전에 **인구수(`Population`)** 등재 | step-01 |
| 5 | `Stage01.unity` 편집 (정보 창 패널 · 손패 컨테이너) | step-09 |

## 단계

| # | 파일 | 영역 | 무엇 |
|---|---|---|---|
| 01 | [step-01-data-customer-population.md](step-01-data-customer-population.md) | data | `CustomerData.Population` + 애셋 값 (§7) |
| 02 | [step-02-runtime-placement-population.md](step-02-runtime-placement-population.md) | runtime | 한도가 머릿수가 아니라 합계를 센다 |
| 03 | [step-03-presentation-population-hud.md](step-03-presentation-population-hud.md) | presentation | `손님 n/m` 과 카드 흐리기가 인구수를 본다 |
| 04 | [step-04-presentation-digest-countdown.md](step-04-presentation-digest-countdown.md) | presentation | 소화 배지에 남은 초 |
| 05 | [step-05-presentation-inspector-presenter.md](step-05-presentation-inspector-presenter.md) | presentation | `CustomerInspectorPresenter` (순수 C#) |
| 06 | [step-06-presentation-inspector-view-and-tap.md](step-06-presentation-inspector-view-and-tap.md) | presentation | 정보 창 뷰 + 손님 클릭 라우팅 |
| 07 | [step-07-presentation-card-layout.md](step-07-presentation-card-layout.md) | presentation | 카드 128×192 4구획 |
| 08 | [step-08-presentation-collapsible-hand.md](step-08-presentation-collapsible-hand.md) | presentation | 접히는 손패 |
| 09 | [step-09-scene-stage01-assembly.md](step-09-scene-stage01-assembly.md) | scene | `Stage01` 조립 · WebGL 확인 |

## 병렬 실행 가능성

```
01 ──► 02 ──► 03 ──┐
04 ────────────────┤
05 ──► 06 ─────────┼──► 09
07 ──► 08 ─────────┘
```

- **04 · (01→02→03) · (05→06) · (07→08) 네 갈래는 서로 독립이다.** 파일이 겹치지 않는다
- **단 03 과 08 은 둘 다 `CustomerHandView.cs` 를 고친다** — 병렬로 가면 충돌이 거의 확실하다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §3). **03 을 먼저 머지하고 08 은 rebase 뒤에 시작한다**
- **06 과 09 는 둘 다 씬을 건드린다** → 직렬. 씬 편집은 한 번에 한 워크트리 (§3)
- **05 는 01~04 를 컴파일 의존하지 않는다.** 다만 정보 창이 인구수를 표시하므로, 01 뒤라면 한 줄이 더 붙는다 — 순서가 뒤바뀌면 06 에서 채운다
- **09 가 마지막.** 씬은 모든 결과가 모이는 곳이다

## 완료 판정 (M6.5 전체)

- [ ] 인구수 2인 손님을 앉히면 `손님 n/m` 이 **2 늘어난다**
- [ ] 남은 인구수보다 큰 손님은 **잔액이 충분해도** 카드가 흐려진다
- [ ] 소화 중인 손님 배지에 남은 초가 보이고, 0 에 닿으면 배지가 사라진다. **값이 바뀐 프레임에만** 갱신된다
- [ ] 앉아 있는 손님을 클릭하면 유형·대역·범위·먹는 시간·소화 시간·영입 비용·인구수가 보인다
- [ ] `현재/최대` 포화도와 현재 상태가 **실시간으로** 갱신된다
- [ ] 우선순위가 더 높은 창이 떠 있으면 정보 창이 열리지 않는다. **정보 창은 판을 멈추지 않는다**
- [ ] 카드가 128×192 이고 비용·이름·아이콘·스탯 4구획이 규격대로 배치된다
- [ ] 덱빌딩·보상·손패 **세 화면이 같은 프리팹**을 쓴다
- [ ] 인스테이지 손패가 평소에 접혀 있고 호버(또는 탭)로 올라온다. **올라온 상태에서 끌어도 카드가 제자리로 돌아온다**
- [ ] WebGL 빌드에서 **한글 두부 0** · 예외 0
- [ ] `./tests/preflight.sh` 전 항목 PASS

## 이 작업서가 하지 않는 것

- **분수 인구수** (소식가 0.5). 정수만 쓴다 — D1
- **자리 점유 확장** (인구수 2 = 자리 2). D1 이 뺐다
- **밸런스 재조정 자체.** `MaxPlacedCustomers` 를 얼마로 할지는 사람이 정한다 (§7-2). M4 에서 대역을 좁혔다가 스테이지 1 이 클리어 불가가 된 전례가 있다 ([`customer-kinds.md`](../../.claude/domain/customer-kinds.md) §3)
- **한글 폰트 재굽기.** 실측으로 불필요함을 확인했다
- **덱 편집 화면** (M6 D9 그대로)
- **드로우콜·빌드 크기** (M8)
