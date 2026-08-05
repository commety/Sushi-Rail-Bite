# Step 06: 화면 — 대역과 "기다리는 중" 을 보이게 한다

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-05 (`ClaimCoordinator.IsWaiting`)
- **후행 단계:** step-07 이 문서를, step-08 이 밸런스 값을 마무리한다

---

## 목적

**가시성은 이 마일스톤에서 선택이 아니다.** 바꾸는 이유 자체가 *"플레이어가 규칙을 배우지 못한다"* 였으므로, 화면에 안 보이면 고친 것이 아니다.

계획서 완료 판정 2건:

- HUD 가 손님의 **대역**을 보여 준다
- HUD 가 **기다리는 중** 상태를 구분해 보여 준다

## 계획서와 다른 지점

계획서 산출물 표는 둘 다 `StageHudView` 에 넣으라고 한다. **대역은 손님별 값이라 전역 HUD 에 넣으면 손님 셋을 구분해 보여 줄 수 없다.** 이렇게 나눈다:

| 무엇 | 어디 | 왜 |
|---|---|---|
| 대역 `100~300` | **`CustomerView`** (손님 옆) | 손님별 값. 자리마다 달라야 읽힌다 |
| 대기 중 색 | **`CustomerView`** | 누가 기다리는지가 정보다 |
| `대기 2` (인원 수) | `StageHudView` | 전역 요약. "왜 아무도 안 먹지" 에 대한 답 |

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

**코드**
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (대역 라벨 · 대기 색)
- `Assets/Code/Scripts/Presentation/Customers/TableSlotView.cs` — 수정 (`Occupy` 가 대기 프로브를 함께 넘긴다)
- `Assets/Code/Scripts/Presentation/Customers/CustomerPlacementController.cs` — 수정 (프로브 배선)
- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 수정 (대기 인원)
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (조율자를 배치 컨트롤러·HUD 에 물린다)
- `Assets/Tests/PlayMode/Customers/CustomerViewTests.cs` — 수정
- `Assets/Tests/PlayMode/UI/StageHudViewTests.cs` — 수정
- `Assets/Tests/PlayMode/Stage01SceneTests.cs` — 수정 (**애셋 대역 검증**)

**씬**
- `Assets/Level/Scenes/Stage01.unity` — 수정 (자리별 대역 라벨 오브젝트, HUD 대기 라벨)

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    public sealed class CustomerView : MonoBehaviour
    {
        /// <summary>로직과 대기 프로브를 함께 물린다. 프로브가 <c>null</c> 이면 대기 표시만 꺼진다.</summary>
        public void Bind(CustomerLogic logic, ClaimCoordinator coordinator);

        /// <summary>지금 화면에 반영돼 있는 대역 문구. 검증용이다.</summary>
        public string BandText { get; }

        /// <summary>지금 대기 상태로 그려져 있는가. 검증용이다.</summary>
        public bool ShownWaiting { get; }
    }
}
```

```csharp
namespace SushiDefense.UI
{
    public sealed class StageHudView : MonoBehaviour
    {
        public void Bind(RevenueLedger revenue, RecruitWallet wallet,
                         CustomerPlacementService placement, StageConfig config,
                         ClaimCoordinator coordinator);   // ← 인자 하나 증가

        /// <summary>지금 표시 중인 대기 인원 문구.</summary>
        public string WaitingText { get; }
    }
}
```

### 배선 경로

`ClaimCoordinator` 는 `StageBootstrap.Build()` 가 만든다. 거기서 두 갈래로 흘린다.

```
StageBootstrap.Build()
  ├─ _hud.Bind(…, Coordinator)
  └─ _placementController.Bind(Placement, Coordinator)
         └─ TryPlaceAt → slot.Occupy(logic, Coordinator)
                            └─ _seatVisual.Bind(logic, Coordinator)
```

`TableSlotView.Vacate()` 는 `Bind(null, null)` 을 부른다.

> **`CustomerView` 가 조율자를 직접 찾지 않는다.** `FindObjectOfType` 은 금지이며(§4.3), 뷰가 로직을 스스로 조달하기 시작하면 M1·M2 가 지킨 "뷰는 물려받기만 한다" 가 무너진다.

### 왜 상태 머신이 아니라 프로브인가

`CustomerState` 에 `Waiting` 을 넣으면 `CanAcceptSushi` 가 `State == Idle` 을 보므로 **대기 중인 손님이 자격을 잃는다** — 대기가 곧 굶기가 된다 (D5). 대기는 손님의 상태가 아니라 **조율자가 이번 틱에 내린 판정**이라, 판정한 쪽에 물어보는 것이 맞다.

### 선행 산출물 의존성

- `SushiDefense.Customers.ClaimCoordinator.IsWaiting(CustomerLogic)` — step-05
- `SushiDefense.Data.CustomerData.TargetingMin` · `TargetingMax` — step-01

### 밸런스 수치

**없다.** 색과 문구 형식은 화면 표현이지 밸런스가 아니다. 다만 **값을 코드에 박지 않고** 기존 `CustomerView` 의 색 필드 패턴대로 `[SerializeField] private Color _waitingColor` 로 노출한다.

### 제약

- **`Update`/`LateUpdate` 에서 문자열을 만들지 않는다.** 대역 문구는 `Bind` 시점에 **한 번만** 만든다 (손님 데이터는 런타임에 안 바뀐다). 대기 인원 문구는 **인원 수가 바뀐 프레임에만** 만든다 — 기존 `RefreshPlacement` 의 변화 감지 패턴을 그대로 쓴다 (§4.3)
- **`IsWaiting` 폴링은 `LateUpdate` 에서, 색은 바뀐 프레임에만 쓴다.** 기존 `_shownState` 패턴 그대로. 매 프레임 `_body.color = …` 를 대입하면 렌더러가 머티리얼 프로퍼티를 계속 갱신한다
- **뷰에 판정을 두지 않는다.** "기다리는 중" 인지는 조율자가 답한다. `CustomerView` 안에 `BandDistance` 나 이탈 시각 비교가 나타나면 위반이다
- **이벤트 구독을 새로 만들지 않는다.** `IsWaiting` 은 폴링으로 읽는다 — 조율자에 대기 이벤트를 추가하면 매 틱 손님 수만큼 델리게이트가 튄다. 구독을 늘리면 `OnDestroy` 해제 경로도 함께 늘어난다 (`.claude/rules/scripts.md` §6)
- `Find` / `FindObjectOfType` 금지. 참조는 인스펙터 주입 또는 **자기 하위 계층 탐색**으로만
- `Instantiate` / `Destroy` 를 새로 부르지 않는다. 라벨 오브젝트는 씬에 미리 둔다 (§3.4)
- **Build Settings 를 수정하지 않는다** (§7). `Stage01.unity` 미등록 상태를 유지한다
- `Assets/Art/` · `Assets/Audio/` · `Assets/Settings/` 에 파일을 만들지 않는다 (`.claude/rules/parallel-work.md` §1)

### 씬 조립

`.claude/skills` 의 브리지를 쓴다. **`Assets/Settings/` 가 변경되지 않았는지 커밋 전에 반드시 확인한다** — M2 에서 WebGL 빌드가 이 폴더를 두 번 건드렸고, preflight 의 에셋 누출 검사는 빌드 **이전**에 돌아 잡지 못한다.

```bash
git status --short Assets/Settings/
```

### 완료 판정

- [ ] `grep -rn "FindObjectOfType\|GameObject.Find" Assets/Code/Scripts/Presentation/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `grep -rnE "Instantiate\(|(^|[^A-Za-z])Destroy\(" Assets/Code/Scripts/Presentation/ | grep -v "SushiPoolBehaviour.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `grep -rn "BandDistance\|ExitAtOf\|TargetingMin" Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — **`TargetingMin`/`Max` 만** (표시용). `BandDistance`·`ExitAtOf` 는 0건
- [ ] `grep -rnE '\$"' Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 문자열 조립이 **`Bind` 안에만** 있다
- [ ] `git status --short Assets/Settings/ ProjectSettings/ Packages/` — **변경 0건**
- [ ] 씬을 재생하면 **손님 옆에 대역이 보이고**, 대기 중인 손님의 색이 다르다
- [ ] 씬을 재생하면 **대역 밖 초밥만 있을 때 손님이 잠시 기다렸다가 집는다** (이 마일스톤을 눈으로 확인하는 지점)
- [ ] EditMode + PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `./scripts/run.sh webgl` 빌드 성공, 로그 에러 0건 — **빌드 후 `Assets/Settings/` 를 다시 확인한다**

### 테스트 이름

```
CustomerViewTests
  Bind_Customer_ShowsTargetingBand
  Bind_Null_ClearsBandText
  LateUpdate_WaitingCustomer_ShowsWaitingColor
  LateUpdate_EatingCustomer_ShowsEatingColorNotWaiting     ← 상태 색이 대기 색을 이긴다
  LateUpdate_WaitingUnchanged_DoesNotRewriteColor          ← 변화 감지가 실제로 작동한다
  Bind_NullCoordinator_DoesNotThrow

StageHudViewTests
  Bind_Immediately_ShowsWaitingCount
  WaitingCountChanged_UpdatesTextOnNextFrame
  Destroyed_ThenValueChanges_NoLongerReceivesThem          ← 기존 회귀 유지

Stage01SceneTests
  Play_Scene_CustomersHaveMeaningfulBands                  ← ★ 애셋 0~0 을 잡는다
  Play_Scene_WaitingCustomerEventuallyEats                 ← ★ 불변식을 실제 씬에서
```

> **★ `Play_Scene_CustomersHaveMeaningfulBands`** — step-01 이 애셋의 타겟팅을 조용히 `0~0` 으로 만든다. 이 테스트가 step-08 전까지 **일부러 빨간 상태로 남아** 밸런스 작업이 빠지는 것을 막는다. 검증 내용: 배치 가능한 손님의 `TargetingMax >= 100`(가격 하한) 그리고 **손님끼리 대역이 서로 다르다**.
>
> **★ `Play_Scene_WaitingCustomerEventuallyEats`** — 실제 씬 수치(래치 1초 · 통과 3초)에서 불변식을 확인한다. step-04 의 D4 가 되돌아가면 여기서 잡힌다.
>
> `LateUpdate_WaitingUnchanged_DoesNotRewriteColor` 는 **공허하게 통과할 수 있다** — 색이 같으면 다시 써도 결과가 같다. `ShownWaiting` 플래그가 아니라 **쓰기 횟수**를 관측하도록 짠다 (예: 대기 판정을 바꾸지 않은 채 두 프레임 돌리고, 중간에 `_body.color` 를 밖에서 오염시킨 뒤 복구되지 않음을 확인).

### 예상 커밋 메시지

```
feat(ui): show targeting band and waiting state on customers
```

씬 커밋은 분리한다 (§9 — 사람이 공개용 여부를 판단한 뒤):

```
chore(scene): add band and waiting labels to stage01
```

---

## 금지 사항

- **`Runtime` 어셈블리를 수정하지 않는다.** 화면에 필요한 값이 없으면 멈추고 어느 단계로 되돌아가야 하는지 보고한다
- **뷰에 판정을 두지 않는다.** 대역 안/밖, 이탈 시각 비교가 `Presentation` 에 나타나면 위반이다
- `CustomerState` 에 `Waiting` 을 추가하지 않는다 (D5)
- `ProjectSettings/*` · Build Settings · `Packages/manifest.json` 을 수정하지 않는다 (§7)
- 밸런스 애셋(`Assets/Level/Balance/*.asset`)을 수정하지 않는다. **step-08 이다**
- 아트를 시작하지 않는다. **아트는 M5** — 색 블록과 `TextMesh` 로 충분하다
- HUD 를 제대로 된 UI 로 바꾸지 않는다. **`StageHudView` 교체는 M6** 다
