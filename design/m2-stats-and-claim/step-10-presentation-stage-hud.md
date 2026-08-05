# Step 10: 화면 반영 — 손님 상태 · 매출/잔액 HUD · 밸런스 값

- **영역:** `presentation` — 어셈블리 `Presentation` (+ 밸런스 애셋, **사람 승인 필요**)
- **선행 단계:** step-09 완료 필요 (조율자의 `SushiClaimed` · `SushiEaten` 이벤트)
- **후행 단계:** 없음. M2 의 마지막 단계

---

## 목적

M2 의 기능은 전부 `Runtime` 안에서 끝났고 **EditMode 로 검증돼 있다.** 이 단계가 하는 일은 둘이다.

1. 그 상태를 **화면에서 볼 수 있게** 한다 — 먹는 중/소화 중, 매출, 잔액, 배치 수
2. **밸런스 값을 채운다** — 현재 placeholder 는 `eatSeconds: 0` · `digestSeconds: 0` · `recruitCost: 0` · `initialRecruitBudget: 0` · 초밥 1종이라, 값이 없으면 M2 가 만든 것이 화면에서 아무것도 달라 보이지 않는다

**2번은 `CLAUDE.md` §7 사람 판단 영역이다.** 아래 "밸런스 값 — 멈추고 물어본다" 를 반드시 먼저 읽는다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 밸런스 값 — 멈추고 물어본다

**에이전트가 값을 임의로 채우지 않는다.** 아래 표를 제시하고 사람에게 값을 받는다.

| 애셋 | 필드 | 현재 | 필요한 이유 |
|---|---|---|---|
| `Customer.Placeholder` | `_eatSeconds` | 0 | 0 이면 먹는 시간이 보이지 않는다 |
| `Customer.Placeholder` | `_digestSeconds` | 0 | 0 이면 소화 상태가 한 틱도 안 보인다 |
| `Customer.Placeholder` | `_maxSaturation` | 99 | 사실상 무한이라 포화가 안 온다 |
| `Customer.Placeholder` | `_recruitCost` | 0 | 0 이면 잔액 검사가 작동하지 않는다 |
| `Stage01.Placeholder` | `_initialRecruitBudget` | 0 | 0 + 비용 0 이면 경제가 무의미하다 |
| `Stage01.Placeholder` | `_sparsityExponent` | 1.0 (기본) | 확정값. 그대로 둘지 확인만 |
| **신규 `SushiData` 애셋** | 가격이 다른 초밥 3종 이상 | 1종뿐 | 초밥 1종이면 share·타겟팅 거리가 전부 자명해진다 |
| **신규 `CustomerData` 애셋** | 타겟팅이 다른 손님 2종 이상 | 1종뿐 | 손님 1종이면 N:1 경합이 화면에 안 나온다 |

> 신규 애셋을 만들 때는 `.claude/rules/parallel-work.md` §2 를 따른다 — 새 밸런스 애셋은 `Assets/Level/Balance/` 에 둔다 (심링크 폴더가 아니다). 스프라이트가 필요하면 기존 `Assets/Level/Placeholder/*.png` 를 재사용하고 **`Assets/Art/` 에 새로 만들지 않는다.**

값을 받기 전까지 이 단계의 **애셋 부분은 시작하지 않는다.** View 코드 부분은 값과 무관하므로 먼저 진행해도 된다.

### 생성/수정 파일

**코드**
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정 (먹는 중/소화 중 표시)
- `Assets/Code/Scripts/Presentation/UI/StageHudView.cs` — **생성** (매출 · 잔액 · 배치 수)
- `Assets/Code/Scripts/Presentation/StageBootstrap.cs` — 수정 (HUD 배선)
- `Assets/Tests/PlayMode/UI/StageHudViewTests.cs` — **생성**
- `Assets/Tests/PlayMode/StageIntegrationTests.cs` — 수정 (M2 통합 시나리오)

**애셋 (값 확인 후)**
- `Assets/Level/Balance/*.asset` — 수정·추가
- `Assets/Level/Scenes/Stage01.unity` — 수정 (HUD 오브젝트 추가)

### 핵심 심볼

```csharp
namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 진행 표시. <b>계산하지 않는다</b> — 원장·지갑·배치 서비스가 준 값을 그리기만 한다
    /// (<c>CLAUDE.md</c> §3.2).
    /// </summary>
    public sealed class StageHudView : MonoBehaviour
    {
        public void Bind(RevenueLedger revenue, RecruitWallet wallet,
                         CustomerPlacementService placement, StageConfig config);

        /// <summary>구독을 끊는다. <c>OnDestroy</c> 에서 반드시 부른다.</summary>
        public void Unbind();
    }
}
```

`CustomerView` 는 로직을 갖지 않는다 — `Logic.State.State` 를 읽어 색이나 라벨만 바꾼다.

### 선행 산출물 의존성

- `SushiDefense.Scoring.RevenueLedger.TotalChanged` · `RecruitWallet.BalanceChanged` — step-07
- `SushiDefense.Customers.ClaimCoordinator.SushiClaimed` · `SushiEaten` — step-09
- `SushiDefense.Customers.CustomerPlacementService.PlacedCount` · `MaxPlacedCustomers` — M1 · step-08

### 제약

- **View 에 판정을 두지 않는다.** 잔액이 모자라 배치가 거부되는 판단은 `CustomerPlacementService` 가 이미 한다. View 는 `TryPlace` 가 `null` 을 돌려준 사실만 표시한다
- **이벤트 구독은 `OnDestroy`/`OnDisable` 에서 반드시 해제한다** (`.claude/rules/scripts.md` §6). M1 의 `SushiBeltView.Unbind()` 가 파괴된 뷰를 만져 `MissingReferenceException` 을 낸 전례가 있다 — 해제 경로에 `if (x == null) continue;` 방어를 넣는다
- **`Update` 안에서 문자열을 만들지 않는다.** 매출·잔액 텍스트는 **값이 바뀔 때만** 갱신한다. 매 프레임 `$"매출 {n}"` 을 만들면 WebGL 에서 GC 스파이크가 그대로 히칭이 된다 (`CLAUDE.md` §4.3)
- **`Find` / `FindObjectOfType` 금지.** 참조는 인스펙터 주입 또는 `StageBootstrap.ResolveMissingReferences()` 의 **자기 하위 계층 탐색**으로만 채운다
- **Build Settings 를 수정하지 않는다** (§7). `Stage01.unity` 등록은 M1 에서 사람이 보류한 상태를 유지한다
- `Instantiate`/`Destroy` 를 새로 부르지 않는다. HUD 오브젝트는 씬에 미리 둔다 (§3.4)

### 완료 판정

**M2 완료 판정 전체를 여기서 확인한다** (`docs/plan/M2-stats-and-claim.md` §완료 판정).

- [ ] `grep -rn "FindObjectOfType\|GameObject.Find" Assets/Code/Scripts/Presentation/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] `grep -rnE "Instantiate\(|(^|[^A-Za-z])Destroy\(" Assets/Code/Scripts/Presentation/ | grep -v "SushiPoolBehaviour.cs" | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
      (`[^A-Za-z]Destroy\(` 형태여야 한다. `Destroy(` 만 쓰면 정상적인 `OnDestroy()` 선언이 걸려 영원히 0 이 되지 않는다)
- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/` — **0건**
- [ ] `grep -rn "Targeting\|Price" Assets/Code/Scripts/Runtime/Customers/CustomerLogic.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — **0건**
- [ ] 씬을 재생하면 **손님이 먹는 동안 상태 표시가 바뀌고**, 포화되면 소화 표시로 넘어간다
- [ ] 씬을 재생하면 **매출·잔액·배치 수가 갱신된다**
- [ ] 잔액이 모자라면 배치가 거부된다
- [ ] EditMode 전량 Green + PlayMode Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `./scripts/run.sh webgl` 빌드 성공, 로그 에러 0건

### 문서 갱신 (`/task-done` STEP 5)

M2 에서 확정된 것을 도메인 문서로 승격한다. **작업서가 아니라 도메인 문서가 "왜 이렇게 돌아가는가" 를 담는다.**

- [`.claude/domain/data-model.md`](../../.claude/domain/data-model.md) §4 — **"미결 — `점수/10` 의 계산 규칙" 절을 결정 사항으로 다시 쓴다.** 정수 누적으로 확정, 근거는 가격 하한 100
- [`.claude/domain/data-model.md`](../../.claude/domain/data-model.md) §3 — 순차번호 범위 미결 표기 삭제 (스테이지 리셋으로 확정)
- [`.claude/domain/sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §7 — 미결 3건 중 "동시에 여러 초밥" 을 확정으로 옮긴다. **M2 가 실제로 만든 타입 지도**를 M1 절 옆에 추가
- **스폰 구성**은 새 도메인 문서 `.claude/domain/spawn-composition.md` 로 뺀다 — share 계산과 credit 배출은 집기 플로우와 별개의 시스템이고, `sushi-claim-flow.md` 에 넣으면 문서 하나가 두 주제를 담는다
- [`docs/plan/README.md`](../../docs/plan/README.md) §1 표 — M2 상태 ☑, §5 열린 질문에서 Q5(일부)·Q7·Q8 해소 표기
- [`docs/plan/M2-stats-and-claim.md`](../../docs/plan/M2-stats-and-claim.md) — 완료 판정 체크박스 채우기

### 예상 커밋 메시지

```
feat(ui): show customer appetite state and stage economy on the hud
```

애셋 값 커밋은 **분리한다** (§9 — 사람이 공개용 여부를 판단한 뒤):

```
chore(balance): fill placeholder stats for M2 verification
```

---

## 금지 사항

- **밸런스 값을 사람 확인 없이 채우지 않는다** (`CLAUDE.md` §7). 이 단계 최대 위반 지점이다
- `Runtime` 어셈블리를 수정하지 않는다. 화면에 필요한 값이 없으면 멈추고 어느 단계로 되돌아가야 하는지 보고한다
- `ProjectSettings/*` · Build Settings · `Packages/manifest.json` 을 수정하지 않는다 (§7)
- `Assets/Art/` · `Assets/Audio/` · `Assets/Settings/` 에 파일을 만들지 않는다 (`.claude/rules/parallel-work.md` §1)
- 아트 작업을 시작하지 않는다. **아트는 M5 다** — 여기서는 색 블록 placeholder 로 충분하다
