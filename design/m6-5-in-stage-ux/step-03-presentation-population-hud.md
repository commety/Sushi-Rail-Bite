# Step 03: `손님 n/m` 과 카드 흐리기가 인구수를 본다

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-02 (`CustomerPlacementService.PlacedPopulation`)
- **후행 단계:** step-08 이 같은 `CustomerHandView.cs` 를 고친다 — **이 단계가 먼저 머지돼야 한다**

---

## 목적

인구수가 판정에 들어갔으니 **화면이 그 사실을 말해야 한다.** 지금은 먹보를 앉혀도 `손님 1/3` 이라 왜 다음 손님이 안 앉는지 알 수 없다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 수정 (`RefreshPlacement`)
- `Assets/Tests/PlayMode/UI/StageHudViewTests.cs` — 수정
- `Assets/Tests/PlayMode/Customers/CustomerHandViewTests.cs` — 수정
- `Assets/Level/Balance/Stage0*.asset` — **§7 승인 후** (아래 §7-2)

### 핵심 변경

```csharp
// StageHudView.RefreshPlacement
var placed = _placement.PlacedPopulation;   // 이전: PlacedCount
...
PlacementText = $"손님 {placed}/{_placement.MaxPlacedCustomers}";
```

`_shownPlacedCount` 필드 이름도 함께 바꾼다 (`_shownPopulation`). **직렬화 필드가 아니라 `private` 캐시라 안전하다** — 반대로 `StageConfig` 쪽은 직렬화 키라 못 바꾼다 (README D3). 그 차이를 주석 한 줄로 남긴다.

**`CustomerHandView` 는 코드를 고치지 않는다.** `CanPlaceAnywhere` → `_controller.CanPlace` → `_service.CanPlace` 를 이미 지나므로 step-02 만으로 카드가 자동으로 흐려진다. **그래서 테스트가 필요하다** — 고친 데가 없다는 것은 곧 회귀를 잡을 그물도 없다는 뜻이다.

### 선행 산출물 의존성

- `SushiDefense.Customers.CustomerPlacementService.PlacedPopulation` — step-02

### 밸런스 수치 (§7-2 — 여기서 실측하고 사람에게 넘긴다)

지금 **한도 = 자리 수**다 (Stage01 3/3, Stage02·03 4/4). 인구수가 들어오면 처음으로 «자리는 비었는데 못 앉힌다» 가 생긴다.

에이전트는 **값을 바꾸지 않고** 아래를 채워 보고한다:

| 스테이지 | 자리 | 현재 한도 | 먹보 1 + 기본 n 조합 | 남는 자리 |
|---|---|---|---|---|
| Stage01 | 3 | 3 | ? | ? |
| Stage02 | 4 | 4 | ? | ? |
| Stage03 | 4 | 4 | ? | ? |

> **M4 의 전례를 기억한다.** 손님 대역을 좁혔다가 스테이지 1 이 클리어 불가가 됐다 (소비율 96% → 78%, [`customer-kinds.md`](../../.claude/domain/customer-kinds.md) §3). 한도를 그대로 두면 이번에도 실질 배치력이 줄어든다. **얼마로 올릴지는 사람이 정한다.**

### 제약

- `StageHudView` 는 **계산하지 않는다.** 합계는 서비스가 이미 냈다
- **값이 바뀐 프레임에만 문자열을 만든다.** `LateUpdate` 경로라 매 프레임 `$"..."` 를 만들면 WebGL 에서 GC 히칭이 된다 (`CLAUDE.md` §4.3)
- `AudioDirector` 를 건드리지 않는다 — 그쪽은 머릿수를 계속 센다 (README D2)

### 테스트 계획

**하네스가 뷰 계층을 우회한다는 것을 기억한다** ([`tests.md`](../../.claude/rules/tests.md) §1). 아래는 전부 실제 뷰/서비스를 세워 돌린다.

```
StageHudViewTests
  RefreshPlacement_PopulationTwoPlaced_ShowsTwo        ← "손님 2/3" 구체 문자열을 박는다
  RefreshPlacement_TwoOnesPlaced_ShowsTwo              ← 머릿수 구현으로도 통과하는 대조군.
                                                          위와 짝으로 둬야 «2가 나온다» 가 공허하지 않다

CustomerHandViewTests
  Refresh_PopulationTwoWithOneHeadroom_DimsTheCard     ← 잔액은 넉넉히, 자리는 비워 둔다
  Refresh_PopulationOneWithOneHeadroom_KeepsCardBright ← 반례를 같은 스위트에
```

- `RefreshPlacement_PopulationTwoPlaced_ShowsTwo` 는 **손님 하나만** 앉힌다. 둘을 앉히면 머릿수 구현으로도 2가 나와 공허해진다 (§3 «검증하려는 키가 다른 키와 나란히 놓이면»)
- 흐리기 테스트는 **잔액과 자리를 일부러 풀어 둔다.** 안 그러면 한도가 아닌 항이 결과를 낼 수 있다

### 완료 판정

- [ ] `grep -n "PlacedPopulation" Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — 1건
- [ ] `grep -n "PlacedCount" Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — **0건**
- [ ] `grep -n "PlacedCount" Assets/Code/Scripts/Presentation/Audio/AudioDirector.cs` — **남아 있음** (안 건드렸다는 증거)
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] 깨뜨려 보기: `PlacedPopulation` 을 `PlacedCount` 로 되돌리면 `RefreshPlacement_PopulationTwoPlaced_ShowsTwo` 와 흐리기 테스트가 **둘 다** 죽는지 확인하고 되돌린다
- [ ] §7-2 표를 채워 아키텍트에게 보고하고 **멈춘다** (한도 값 자체는 바꾸지 않는다)

### 예상 커밋 메시지

```
feat(ui): show the placement limit in population
```

---

## 금지 사항

- **`CustomerHandView` 에 접힘·호버 관련 코드를 넣지 않는다** (step-08). 같은 파일을 두 단계가 고치므로 범위를 엄격히 지킨다.
- 승인 없이 `Stage0*.asset` 의 한도를 바꾸지 않는다.
