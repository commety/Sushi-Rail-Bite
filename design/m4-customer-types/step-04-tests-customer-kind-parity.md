# Step 04: 유형 동일 코드 경로 방어선

- **영역:** `tests` — 어셈블리 `Tests.EditMode`
- **선행 단계:** 없음 (다른 모든 단계와 독립. 가장 먼저 돌려도 된다)
- **후행 단계:** 없음 — 이 단계는 다른 단계에 산출물을 넘기지 않는다. **방어선**이다

---

## 목적

**이 단계의 기대 산출물은 "프로덕션 코드 변경 0" 이다.**

M2 의 자격 판정과 M2.5 의 대역 배정은 손님 유형을 모르도록 설계돼 있다. 유형은 `CustomerData` **값의 차이**일 뿐이다. 그 설계가 실제로 옳았는지는 **세 유형이 하나도 안 고치고 다르게 동작하는지**로만 증명된다.

동시에, 이 마일스톤에서 가장 들어오기 쉬운 오해를 막는다:

> *"소식 손님인데 싼 걸 먹네? 버그 아냐?"*

**버그가 아니다.** 대역은 *무엇을 · 언제* 를 정할 뿐 *먹을 수 있는가* 를 정하지 않는다 (`CLAUDE.md` §1.1-3a). 이 오해가 코드로 들어가면 손님이 눈앞의 초밥을 두고 굶는다. 아래 자격 테스트 2개가 유일한 방어선이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

- `Assets/Tests/EditMode/Customers/CustomerKindParityTests.cs` — 생성

**기존 테스트 파일을 고치지 않는다.** 유형별 검증을 한 파일에 모아 두면, 나중에 누가 대역을 만졌을 때 어디를 봐야 하는지가 분명해진다.

### 쓰는 값 (README 의 3분할 배치)

| 유형 | 대역 | 폭 | 포화도 |
|---|---|---|---|
| 먹보 | 100~150 | 50 | 8 |
| 기본 | 150~250 | 100 | 5 |
| 소식 | 250~320 | 70 | 3 |

**애셋을 로드하지 않는다.** `ScriptableObject.CreateInstance<CustomerData>()` + 기존 `Assets/Tests/EditMode/Data/SerializedFieldSetter.cs` 로 만든다 ([`rules/tests.md`](../../.claude/rules/tests.md) §4). 디스크 애셋을 읽으면 step-10 에서 밸런스를 조정하는 순간 이 방어선이 깨진다.

> 값이 애셋과 같은 것은 **읽는 사람을 위한 것**이지 결합이 아니다. 이 테스트가 지키는 것은 "폭이 좁은 쪽이 이긴다" 이지 "먹보는 100~150 이다" 가 아니다.

### 기존 API (직접 확인한 시그니처)

```csharp
CustomerLogic(CustomerRuntimeState state, float beltPosition)
bool CustomerLogic.CanTake(SushiItem sushi)
void SushiClaimResolver.Resolve(IReadOnlyList<CustomerLogic> customers, ...)
ClaimTiming ClaimDeadline.Evaluate(CustomerLogic customer, CandidateSet candidates, float nowSeconds)
```

호출 형태는 기존 `CustomerLogicTests` · `SushiClaimResolverTests` · `ClaimDeadlineTests` 를 그대로 따른다. **새 헬퍼를 만들지 말고 그 파일들이 쓰는 방식을 재사용한다.**

### 테스트 목록

```
자격 (핵심 방어선 — 대역이 게이트가 아님을 고정)
  CanTake_SmallEaterOnlyCheapSushi_ReturnsTrue        ← 소식(250~320)에게 120 짜리 하나. true
  CanTake_BigEaterOnlyExpensiveSushi_ReturnsTrue      ← 먹보(100~150)에게 400 짜리 하나. true

배정 — 한 손님 / 여러 초밥 (선호가 실제로 체감되나)
  Resolve_SmallEaterMixedPrices_TakesExpensive        ← 120 vs 300 → 300
  Resolve_BigEaterMixedPrices_TakesCheap              ← 120 vs 300 → 120

배정 — 여러 손님 / 한 초밥 (누가 이기나)
  Resolve_ExpensiveSushiSmallEaterVsBigEater_SmallEaterWins
  Resolve_CheapSushiSmallEaterVsBigEater_BigEaterWins
  Resolve_BoundaryPricedSushiNormalVsBigEater_BigEaterWins   ← 150 은 양쪽 다 거리 0. 폭이 깬다

타이밍 (기다림 ≠ 굶기)
  Claim_SmallEaterOnlyCheapSushi_TakesItBeforeExit    ← 대역 밖뿐이어도 유한 시간 안에 집는다

포화도 (유형 차이가 값에서 나온다)
  Eat_BigEaterVsSmallEater_BigEaterEatsMoreBeforeFull
```

### 공허하게 통과하는 배치를 피한다

[`rules/tests.md`](../../.claude/rules/tests.md) §3 의 함정을 이 단계에 그대로 적용한다.

| 테스트 | 반드시 엇갈리게 둘 것 |
|---|---|
| `Resolve_SmallEaterMixedPrices_TakesExpensive` | **싼 초밥에 낮은 SeqNo** 를 준다. 나란히 두면 "먼저 올라온 것" 을 고르는 구현도 통과한다 |
| `Resolve_BigEaterMixedPrices_TakesCheap` | **비싼 초밥에 낮은 SeqNo** 를 준다. 이 테스트는 위 테스트의 거울이라 **"고가 우선(키 2)이 최우선 키가 아님"** 을 함께 증명한다 — 둘 중 하나만 있으면 반쪽이다 |
| `..._SmallEaterWins` | **소식에게 더 높은 손님 SeqNo** 를 준다 (먹보 먼저 배치). 손님 SeqNo 만 보는 구현이 걸린다 |
| `..._BigEaterWins` | **먹보에게 더 높은 손님 SeqNo** 를 준다. 위와 방향이 반대여야 쌍이 의미를 갖는다 |
| `..._BoundaryPricedSushi...` | **기본 손님에게 낮은 SeqNo** 를 준다. 폭(키 4)이 손님 SeqNo(키 5)보다 앞선다는 것이 이 테스트의 전부다 |
| `Eat_BigEaterVsSmallEater_...` | **"둘이 다르다" 로 끝내지 않는다.** 먹보 8 · 소식 3 이라는 **구체값**을 함께 박는다 |

두 손님을 겨루게 할 때는 **같은 초밥 하나**를 두고 겨루게 한다. 초밥이 둘이면 키 1~3 에서 갈려 손님 키가 아예 불리지 않는다.

### 제약

- **프로덕션 코드를 수정하지 않는다.** 한 줄도.
- `Tests.EditMode` 는 `Runtime` · `Runtime.Data` 를 참조한다. 역방향 금지
- 새 패키지 금지 (§7)
- 밸런스 애셋(`.asset`)을 건드리지 않는다 — step-10 이다

### 완료 판정

- [ ] `./tests/run-tests.sh` EditMode 전량 Green
- [ ] **`git diff --stat Assets/Code/` 가 비어 있다** — 이것이 이 단계의 진짜 결과다
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 통과하지 않으면

**테스트를 고치지 말고 멈추고 보고한다.** 이 단계에서의 실패는 두 가지 중 하나다.

1. **테스트가 규칙을 잘못 읽었다** — `CLAUDE.md` §1.1-3a 와 [`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) 를 다시 대조한다
2. **M2·M2.5 구현에 실제 결함이 있다** — 그렇다면 그것이 이 마일스톤의 가장 값진 발견이다. 고치기 전에 먼저 보고한다

자격 테스트 2개 중 하나라도 실패하면 **2번일 가능성이 높다.** 가격이 자격 경로로 샜다는 뜻이고, `./tests/preflight.sh` 의 "자격 경로 청정" 항목이 놓친 형태를 찾은 것이다.

### 예상 커밋 메시지

```
test(customer): lock three customer kinds onto one code path
```

---

## 금지 사항

- 유형별 분기를 만들지 않는다. **테스트가 그런 코드를 요구하는 것처럼 보이면 테스트가 틀린 것이다.**
- `CustomerLogic` · `SushiClaimResolver` · `ClaimPairComparer` · `ClaimDeadline` 을 수정하지 않는다.
- 밸런스 애셋을 만들거나 고치지 않는다.
