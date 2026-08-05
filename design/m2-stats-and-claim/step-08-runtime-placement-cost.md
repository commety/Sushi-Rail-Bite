# Step 08: 배치에 영입 비용 · 잔액 검사를 붙인다

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-07 (`RecruitWallet`)
- **후행 단계:** step-09 가 조립부에서 지갑을 주입하고, step-10 이 잔액·배치 수를 화면에 띄운다

---

## 목적

M1 의 배치는 **무료였다.** `CustomerPlacementService` 가 자리 점유와 `MaxPlacedCustomers` 만 봤고, `CustomerData.RecruitCost` 를 읽지 않는다고 문서에 명시해 뒀다.

M2 는 그 문장을 지운다. 배치 판정에 **잔액**이 세 번째 축으로 들어온다.

```
배치 가능  ⇔  자리가 비어 있다  ∧  배치 한도 미만  ∧  잔액 ≥ 영입 비용
```

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs` — 수정
- `Assets/Tests/EditMode/Customers/CustomerPlacementServiceTests.cs` — 수정

### 핵심 심볼

생성자에 지갑이 하나 늘고, `CanPlace` 가 손님 데이터를 받게 된다.

```csharp
public CustomerPlacementService(ClaimCoordinator coordinator,
                                StageConfig config,
                                SequenceNumberIssuer customerSequenceNumbers,
                                RecruitWallet wallet);

/// <summary>이 손님을 이 자리에 놓을 수 있는가 — 점유 · 배치 한도 · 잔액.</summary>
public bool CanPlace(CustomerData data, int slotIndex);

public CustomerLogic Place(CustomerData data, int slotIndex, float slotBeltPosition);
public CustomerLogic TryPlace(CustomerData data, int slotIndex, float slotBeltPosition);

/// <summary>배치를 취소한다. <b>영입 비용은 환불하지 않는다.</b></summary>
public bool Remove(int slotIndex);
```

**`CanPlace(int slotIndex)` 시그니처는 사라진다.** 비용을 보려면 어떤 손님인지 알아야 하기 때문이다.

`CustomerPlacementController` 는 `TryPlace` · `Remove` 만 부르므로 `CanPlace` 변경에 영향받지 않는다. 다만 **생성자에 지갑이 하나 늘기 때문에 `StageBootstrap` 은 반드시 바뀐다** — 이 작업서 초판이 `CanPlace` 만 grep 하고 생성자 호출부를 놓쳐 "`Presentation` 변경 0줄" 이라고 잘못 적었다.

| 파일 | 무엇 |
|---|---|
| `Runtime/Customers/CustomerPlacementService.cs` | `CanPlace` 내부 호출 2곳 |
| `Presentation/StageBootstrap.cs` | **생성자에 지갑 주입** — `new RecruitWallet(config.InitialRecruitBudget)` |
| `Tests/EditMode/Customers/CustomerPlacementServiceTests.cs` | `CanPlace` 4곳 + 생성자 |
| `Tests/PlayMode/Customers/CustomerPlacementControllerTests.cs` | 생성자 |
| `Tests/PlayMode/Stage01SceneTests.cs` · `StageIntegrationTests.cs` | `CanPlace` 각 1곳 |

`StageBootstrap` 의 지갑은 step-09 에서 조율자와 공유하도록 옮긴다. 여기서는 배치 서비스만 쓴다.

### 선행 산출물 의존성

- `SushiDefense.Scoring.RecruitWallet` — step-07
- `SushiDefense.Data.CustomerData.RecruitCost` — M0 부터 존재

### 밸런스 수치

- 영입 비용은 `CustomerData.RecruitCost` 에서, 배치 한도는 `StageConfig.MaxPlacedCustomers` 에서 읽는다. **코드 상수 0건**
- `RecruitCost == 0` 은 유효한 값이다 (무료 손님). 현재 placeholder 가 그렇다

### 제약

- **차감은 배치 확정 시점에 한 번만.** `CanPlace` 는 잔액을 **읽기만** 한다. 검사와 차감이 둘 다 부수효과를 내면 `TryPlace` 가 두 번 차감한다
- **차감 실패 시 배치도 실패한다.** `TrySpend` 가 `false` 면 순차번호를 발급하지 않고 조율자에도 등록하지 않는다. 순서는 **잔액 차감 → 번호 발급 → 조율자 등록**이다 — 번호를 먼저 발급하면 실패한 배치가 순차번호를 소모해 배정 결과가 달라진다
- **환불하지 않는다.** `Remove` 는 자리만 비운다. 배치·해제를 반복해 재화를 되찾는 경로를 만들지 않는다. 이 판단을 코드 주석에 남긴다
- **스테이지 진행 중 배치는 M1 그대로 허용된다** (M1 Q4 확정 사항). 이 단계가 그 성질을 없애지 않는다
- `Random` 을 쓰지 않는다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 완료 판정

- [ ] `grep -n "RecruitCost" Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs` — 비용을 읽는다
- [ ] `grep -n "TrySpend" Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs` — **`Place` 안에서 1회만** 호출된다
- [ ] `grep -rn "CanPlace(" Assets/Code/Scripts/ Assets/Tests/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — 위 표의 8개 호출부가 전부 새 시그니처다 (인자 2개)
- [ ] `git diff --stat Assets/Code/Scripts/Presentation/` — **`StageBootstrap.cs` 만** (지갑 주입)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] PlayMode Green — `./tests/run-tests.sh all` (`CustomerPlacementControllerTests` 가 호출부 변경의 영향을 받는다)
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름 (플랜 §테스트 항목 그대로)

```
Place_AtMaxCustomers_Rejected                    ← 플랜 명시
Place_InsufficientCurrency_Rejected              ← 플랜 명시
Place_SufficientCurrency_DeductsRecruitCost
Place_ZeroCost_SucceedsWithEmptyWallet
CanPlace_InsufficientCurrency_DoesNotDeduct      ← 검사에 부수효과가 없다
TryPlace_InsufficientCurrency_ReturnsNullAndKeepsBalance
TryPlace_InsufficientCurrency_DoesNotConsumeSequenceNumber   ← 실패가 번호를 태우지 않는다
Remove_PlacedCustomer_DoesNotRefund
Place_OccupiedSlot_Rejected                      ← M1 회귀
Place_MidStage_JoinsNextResolution                ← M1 회귀
```

`TryPlace_InsufficientCurrency_DoesNotConsumeSequenceNumber` 가 **순서 계약을 고정하는 회귀선**이다. 번호를 먼저 발급하는 구현이면 이 테스트만 깨진다.

### 예상 커밋 메시지

```
feat(customer): gate placement on recruit cost and wallet balance
```

---

## 금지 사항

- `RecruitWallet` 을 수정하지 않는다. 지갑이 부족해 보이면 멈추고 step-07 로 보고한다
- `ClaimCoordinator` 를 건드리지 않는다. 지갑 주입 배선은 step-09 다
- `Presentation` 은 **`StageBootstrap` 의 지갑 주입만** 고친다. `CustomerPlacementController` 가 깨진다면 시그니처를 잘못 바꾼 것이다. UI 표시는 step-10 이다
- 환불·부분 환불을 만들지 않는다
