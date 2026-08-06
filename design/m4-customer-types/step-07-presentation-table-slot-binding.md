# Step 07: 자리 정의를 `StageConfig` 에서 바인딩

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** 없음 (독립. 다만 step-08 과 **같은 `StageBootstrap.cs`** 를 고치므로 반드시 직렬)
- **후행 단계:** step-08 · step-10 이 이 동작에 의존한다

---

## 목적

**`StageConfig.TableSlots` 는 지금 프로덕션 코드에서 아무도 읽지 않는다.**

```bash
grep -rn "TableSlots" Assets/Code
# → StageConfig.cs 의 프로퍼티 선언 1건. 소비자 0건
```

`TableSlotView.Bind(TableSlotDefinition)` 은 **정의돼 있지만 호출되지 않는다.** 자리 좌표는 씬의 각 `TableSlotView` 인스펙터에 손으로 박혀 있고, 스테이지 1 에서는 우연히 값이 같아 아무 문제가 없었다.

스테이지 2·3 이 자리 4개를 쓰는 순간 이것이 사고가 된다 — **설정을 고쳐도 화면이 안 바뀐다.** M3 에서 씬이 조용히 죽었던 것과 같은 계열이고, 테스트가 전부 초록인 채로 진행된다는 점까지 같다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (`Build()` 에 자리 바인딩 추가)
- `Assets/Tests/PlayMode/StageIntegrationTests.cs` — 수정 (테스트 추가)

**수정하지 않는 파일**

- `Assets/Code/Scripts/Presentation/Customers/TableSlotView.cs` — `Bind` 가 이미 있다. 부르기만 하면 된다
- `Assets/Code/Scripts/Runtime.Data/StageConfig/StageConfig.cs` — 스키마 변경 없음
- 씬 파일 — step-10

### 동작 계약

`Build()` 안에서 `_placementController.Initialize(...)` **앞에** 자리를 맞춘다.

```
정의 목록이 비어 있으면 → 아무것도 하지 않는다 (씬에 박힌 값을 그대로 쓴다)

i < min(정의 수, 자리 수)  → _slots[i].Bind(정의[i]); 활성화
i >= 정의 수               → _slots[i] 를 비활성화
정의가 자리보다 많으면      → 남는 정의는 무시한다
```

**빈 목록이 no-op 인 것이 핵심이다.** `StageConfigBuilder` · `StageConfigTestFactory` 로 세운 기존 테스트 하네스는 자리 정의를 채우지 않는다. 빈 목록에서 자리를 전부 꺼 버리면 **기존 PlayMode 테스트가 통째로 죽는다.**

**정의가 자리보다 많은 경우를 예외로 만들지 않는다.** 씬을 조금씩 조립하는 동안 흔한 상태이고, 여기서 터지면 나머지를 아무것도 확인할 수 없다. 대신 그 상황이 실제로 문제라면 step-10 의 씬 조립에서 드러난다.

### 왜 `Build()` 인가

자리 수는 스테이지마다 다르고, 스테이지 교체는 `Build()` 재호출이다 (README D1). `Awake` 에 두면 스테이지 2 에서 자리가 안 바뀐다.

### 테스트 목록 (`StageIntegrationTests` 에 추가)

```
Build_ConfigWithThreeSlotDefinitions_BindsBeltPositions
    ← 각 자리의 BeltPosition 이 정의값과 같은지. **세 개를 각각** 확인한다
Build_ConfigWithFewerSlotsThanScene_DeactivatesExtras
    ← 자리 3개 씬 + 정의 2개 → 세 번째가 비활성
Build_ConfigWithNoSlotDefinitions_LeavesSceneSlotsUntouched
    ← 빈 목록 no-op. 기존 하네스가 사는 이유
Build_Rebuilt_WithDifferentSlotCount_UpdatesActiveSlots
    ← 자리 2개 설정으로 한 번, 4개 설정으로 다시 Build. **스테이지 교체의 핵심**
```

### 공허하게 통과하지 않게

- **`..._BindsBeltPositions` 는 정의값을 씬에 박힌 값과 다르게 준다.** 같게 주면 바인딩을 아예 안 하는 구현도 통과한다. 씬 값이 5·10·15 라면 정의는 4·8·12 처럼 확실히 다른 값을 쓴다.
- **`..._UpdatesActiveSlots` 는 늘리는 방향과 줄이는 방향을 모두 본다.** 한 방향만 보면 비활성화만 하고 다시 켜지 않는 구현이 통과한다.
- **`SlotIndex` 도 함께 확인한다.** `BeltPosition` 만 보면 인덱스가 어긋난 채로 배정이 도는 상태가 숨는다.

### 선행 산출물 의존성

- `SushiDefense.Data.TableSlotDefinition` — 이미 있다
- `SushiDefense.Customers.TableSlotView.Bind` — 이미 있다 (**호출되지 않고 있을 뿐**)

### 밸런스 수치

없음. 실제 자리 좌표는 step-10 의 애셋에 들어간다.

### 제약

- `Find` / `FindObjectOfType` 금지 (§4.3). `_slots` 는 이미 인스펙터 또는 `GetComponentsInChildren` 로 채워진다
- `Instantiate` 금지 (§3.4) — 자리는 **미리 놓여 있고** 켜고 끄기만 한다. 자리가 모자라면 새로 만들지 말고 무시한다
- `Update()` 에서 도는 경로가 아니므로 할당 최적화는 불필요
- `null` 체크는 `!= null` 로 명시적으로 (`UnityEngine.Object` 의 `==` 오버로드)
- 씬 파일을 수정하지 않는다

### 완료 판정

- [ ] `grep -rn "TableSlots" Assets/Code/Scripts/Presentation/` 가 **1건 이상** (드디어 소비자가 생겼다)
- [ ] `./tests/run-tests.sh all` 전량 Green — **기존 PlayMode 테스트가 하나도 안 죽어야 한다**
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 주입 검증 (구현 후 반드시)

| 주입 | 실패해야 하는 테스트 (실측) |
|---|---|
| 빈 목록에서도 전부 비활성화하도록 | `Build_ConfigWithNoSlotDefinitions_LeavesSceneSlotsUntouched` **하나뿐** |
| 비활성화만 하고 재활성화는 안 하도록 | `Build_Rebuilt_WithDifferentSlotCount_UpdatesActiveSlots` |
| `Bind` 호출을 통째로 제거 | `Build_ConfigWithThreeSlotDefinitions_BindsBeltPositions` |

> **첫 줄은 원래 "+ 기존 PlayMode 다수" 라고 적혀 있었다. 틀렸다.** 자리를 전부 꺼도
> 기존 테스트는 **하나도 죽지 않는다** — 그 테스트들이 `Placement.Place(...)` 를 직접 부르고
> 자리 뷰에서는 `SlotIndex`·`BeltPosition` 만 읽기 때문이다. `GameObject` 가 꺼져도 로직은
> 그대로 돈다.
>
> **M3 에서 씬이 조용히 죽었던 것과 정확히 같은 사각지대다.** 이 하네스는 뷰 계층을
> 우회하므로, **자리·좌석이 화면에서 사라지는 종류의 사고를 구조적으로 못 잡는다.**
> 그런 회귀를 막고 싶으면 `Play_Scene_*` 계열처럼 실제 씬을 여는 테스트가 필요하다.

### 예상 커밋 메시지

```
fix(stage): bind table slots from stage config
```

`fix` 인 이유: 새 기능이 아니라 **설정이 무시되던 구멍을 닫는 것**이다.

---

## 금지 사항

- 스테이지 진행 배선을 여기서 하지 않는다 (step-08). 이 단계는 `Build()` 한 곳에만 손댄다.
- `TableSlotView` 의 시그니처를 바꾸지 않는다.
- 자리가 모자랄 때 새 `GameObject` 를 만들지 않는다.
