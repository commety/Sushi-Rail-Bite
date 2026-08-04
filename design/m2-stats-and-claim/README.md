# M2 — 스탯 · 배정 · 경제

## 한 줄 요약

초밥에 **가격**이, 손님에 **타겟팅·먹는 시간·포화도·소화**가 생기고, 그 위에 **쌍 랭킹 배정** · **가격에서 유도되는 결정적 스폰 수열** · **매출/영입 재화 경제**를 얹는다.

## 원문

[`docs/plan/M2-stats-and-claim.md`](../../docs/plan/M2-stats-and-claim.md) (원본 2차 목표)

> *"초밥은 가격과 포화도를 가진다 / 타워는 초밥을 스캔해서 자신의 우선순위에 따른 초밥을 우선적으로 처리한다 / 타워는 집기 범위, 먹는 시간, 소화 시간을 가진다"*

M1 이 **자격 판정**과 **이벤트 기반 인식**을 세웠다. M2 는 그 위에 **배정**을 얹는다. 자격은 M1 그대로 두고 손대지 않는다.

---

## 착수 시 확정한 열린 질문

플랜의 열린 질문 5건 중 1건은 M1 에서 이미 해소됐고, 나머지 4건을 착수 시 사람에게 물어 확정했다.

| # | 질문 | 결정 | 비고 |
|---|---|---|---|
| 1 | `점수/10` 계산 규칙 | **정수 누적** — 초밥 1개마다 `가격 / 10` 을 정수 나눗셈해 더한다 | 사람 판단: *"초밥은 실제 엔 단위로 100 이상이다. 100 미만은 오류다"* → **`SushiData.Price` 하한 100 을 구조 불변조건으로 넣는다** (step-01). 플랜의 제안(실수 누적)은 **채택하지 않았다** |
| 2 | 순차번호 범위 | **스테이지마다 리셋** | 기본안. 현재 `StageBootstrap.Build()` 가 이미 스테이지마다 새 발급기를 만든다 — step-09 에서 회귀 테스트로 고정한다 |
| 3 | 인식 래치 상한 | **M1 에서 해소됨** | `StageConfig.RecognitionLatchSeconds`, `0 = 상한 없음`. M2 는 손대지 않는다 |
| 4 | 한 손님이 동시에 여러 초밥 | **아니오 — 한 번에 하나** | 기본안. `Eating` 중에는 자격이 없고, `SushiClaimResolver` 의 손님당 1건 확정 구조가 그대로 유지된다 |
| 5 | 영입 재화 스테이지 간 이월 | **이월 안 함 — 스테이지마다 초기화** | 기본안. 지갑의 수명이 스테이지 안에 닫히므로 M2 가 M3 로 번지지 않는다 |

> 1번의 파생 결정(가격 하한 100)은 **밸런스 수치가 아니라 구조 불변조건**으로 다룬다 — `StageConfig.MinimumTimeLimitSeconds` 와 같은 성격이다. 값 자체가 아니라 "이 아래는 성립하지 않는다"는 경계다.

---

## 아키텍처 결정

### D1 — 자격은 M1 그대로 둔다. `CustomerLogic.cs` 에 가격이 들어오면 실패다

이 마일스톤 **최대 위험**은 타겟팅이 자격 게이트로 새어 들어가는 것이다 (`CLAUDE.md` §1.1-3a). M1 이 만든 방어선을 그대로 유지한다:

- `CustomerLogic.cs` 는 `using SushiDefense.Data` 가 없다 — `State.Data.Reach` 처럼 멤버 경유로만 정적 값을 읽는다
- **M2 에서 이 파일에 `Price` · `Targeting` 문자열이 등장하면 안 된다.** 매 단계 완료 판정에 grep 을 넣었다

가격을 읽는 곳은 **정확히 세 곳**뿐이다:

| 파일 | 무엇을 위해 |
|---|---|
| `TargetingPriority` | `\|가격 − 타겟팅\|` 거리 계산 (step-04) |
| `SpawnShareTable` | share = `(덱 내 최저가 / 가격) ^ α` (step-02) |
| `RevenueLedger` / `RecruitWallet` | 매출 누적, `가격/10` (step-07) |

`ClaimPairComparer` 는 `TargetingPriority` 를 경유해 가격을 읽는다(step-05) — 네 번째 지점이지만 계산식을 직접 갖지 않는다.

### D2 — 상태 머신을 `CustomerLogic` 에 넣지 않고 분리한다

`CLAUDE.md` §3.5 는 전이 로직이 테스트 가능한 순수 클래스에 있기를 요구한다. `CustomerLogic` 도 순수 클래스지만, 여기에 먹는 시간·포화도를 넣으면 그 파일이 초밥의 정적 데이터를 만지기 시작하고 **D1 의 grep 방어가 무의미해진다.**

→ 별도 파일 `CustomerAppetiteMachine` (step-06). 이 클래스는 **`SushiItem` 을 받지 않는다** — `BeginEating(int saturationAmount, float eatSeconds)` 처럼 **원시 값만** 받는다. 그래서 가격을 볼 수 있는 경로가 타입 수준에서 없다.

`CustomerLogic` 은 M2 이후에도 **자격 판정 3줄짜리 클래스**로 남는다.

### D3 — 정렬 키는 `ClaimPairComparer` 한 곳에만 추가한다

M1 이 이 파일을 "M2 가 정렬 키를 추가하는 유일한 지점"으로 명시해 두었다. `SushiClaimResolver` 의 그리디 루프·중복 방지(`TryClaim`)는 **한 줄도 바뀌지 않는다.**

케이스별 분기(1:N / N:1 / N:M)를 새로 만들지 않는다. 정렬 키 2개를 앞에 붙이는 것이 M2 배정 구현의 전부다.

### D4 — 스폰은 `share 계산`과 `배출 수열`을 두 클래스로 나눈다

`SpawnShareTable`(순수 계산, 상태 없음) / `SpawnSequence`(credit 누적, 상태 있음). 나누는 이유:

- share 검증(스케일 무관·매출 균등·덱 기준 최저가)과 배출 검증(고른 분산·결정성·리셋)은 **깨지는 방식이 다르다**. 한 클래스면 어느 쪽이 틀렸는지 테스트가 말해주지 못한다
- share 는 덱이 바뀔 때만 다시 계산되고, credit 은 스폰마다 갱신된다 — 수명이 다르다

`SpawnSequence` 는 M1 의 가중치 반복 방식에서 **credit 누적 방식으로 갈아엎는다.** 파일은 재사용하되 내부는 전부 바뀐다.

### D5 — 매출과 영입 재화를 한 클래스에 합치지 않는다

[`data-model.md`](../../.claude/domain/data-model.md) §4 가 "두 개의 다른 자원이다. 섞지 않는다" 고 못박았다. `RevenueLedger`(클리어 판정용 매출) / `RecruitWallet`(배치용 재화)로 나눈다.

지갑은 매출을 **구독하지 않는다** — 조율자가 소비 시점에 양쪽 모두에 알린다 (step-09). 지갑이 원장을 구독하면 "초기 예산"과 "매출 유래분"이 한 스트림에 섞여 잔액 추적이 어려워진다.

### D6 — 소비 시점이 M1 과 달라진다. 조율자의 틱 계약을 갱신한다

M1 은 "배정 확정 = 즉시 소비"였다. M2 는 그 사이에 **먹는 시간**이 들어간다.

```
배정 확정  →  Eating 진입, 초밥은 Claimed 로 벨트에 남아 있음
   ↓ eatSeconds 경과
소비 확정  →  포화도 증가 · 매출/재화 누적 · 벨트에서 제거 · 포화면 Digesting
```

**초밥은 먹는 동안 벨트 위에 남는다.** `Claimed` 상태라 다른 손님이 가져갈 수 없고(`TryClaim` 이 막는다), 끝점에 닿으면 M1 그대로 반납된다 — 즉 **먹다 만 초밥이 끝점을 지나면 놓친다.** 이건 의도된 동작이며, step-09 에서 테스트로 고정한다.

### D7 — 완료 판정 grep 은 주석을 제외한다

M0 에서 확정한 필터를 그대로 쓴다. 문서 주석에 규칙 이름이 등장하는 것은 위반이 아니다.

```
| grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'
```

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | α 필드 추가, `SushiSpawnEntry.Weight` 제거, 가격 하한 |
| runtime | `Runtime` | share·배출 수열, 타겟팅 거리, 정렬 키, 식욕 상태 머신, 경제, 조율자 배선 |
| presentation | `Presentation` | 먹는 중·소화 중 표시, 매출·잔액·배치 수 HUD |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 위 전부 |

**새 어셈블리는 만들지 않는다.** `SushiDefense.Scoring` 은 네임스페이스일 뿐 `Runtime` 어셈블리 안에 들어간다 (`.claude/rules/scripts.md` §1).

## 의존성 그래프

```
SushiDefense.Data.SushiData ─┬─→ SushiDefense.Belt.SpawnShareTable ──→ SushiDefense.Belt.SpawnSequence
                             ├─→ SushiDefense.Customers.TargetingPriority ──→ ClaimPairComparer ──→ SushiClaimResolver
                             └─→ SushiDefense.Scoring.RevenueLedger / RecruitWallet
                                                                                    ↓
SushiDefense.Data.CustomerData ──→ CustomerRuntimeState ──→ CustomerAppetiteMachine ─┴─→ ClaimCoordinator
                                                                                              ↓
                                                                     SushiDefense.UI.StageHudView (Presentation)
```

`CustomerLogic` 은 이 그래프에서 **가격 쪽 어느 노드와도 연결되지 않는다** — D1 의 방어가 그림으로 보이는 지점이다.

## 새 밸런스 수치

**값은 사람이 정한다** (`CLAUDE.md` §7). 에이전트는 필드와 검증만 만든다.

| 수치 | 들어갈 SO | 초기값 제안 | 근거 |
|---|---|---|---|
| 희소성 지수 `α` (`SparsityExponent`) | `StageConfig` | 1.0 | 플랜 확정값. α=1 은 유형별 매출 기여가 균등한 지점 |
| 가격 하한 100 | `SushiData` (코드 상수) | 100 | 사람 판단 — 엔 단위, 100 미만은 오류. **밸런스가 아니라 불변조건** |

기존 필드에 실제 값을 채우는 것(먹는 시간·소화 시간·영입 비용·초기 예산·초밥 종류별 가격)은 **step-10 에서 사람에게 확인을 받고 진행한다.** 현재 placeholder 는 `eatSeconds: 0` · `digestSeconds: 0` · `recruitCost: 0` · `initialRecruitBudget: 0` · 초밥 1종이라, 값을 채우지 않으면 M2 기능이 화면에서 아무것도 달라 보이지 않는다.

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 01 | [step-01-data-spawn-schema.md](step-01-data-spawn-schema.md) | data | α 추가, `Weight` 제거, 가격 하한 100 |
| 02 | [step-02-runtime-spawn-share.md](step-02-runtime-spawn-share.md) | runtime | `SpawnShareTable` — 가격 → share |
| 03 | [step-03-runtime-spawn-sequence.md](step-03-runtime-spawn-sequence.md) | runtime | `SpawnSequence` credit 배출로 교체 |
| 04 | [step-04-runtime-targeting-priority.md](step-04-runtime-targeting-priority.md) | runtime | `TargetingPriority` — 거리 계산 |
| 05 | [step-05-runtime-claim-ranking.md](step-05-runtime-claim-ranking.md) | runtime | `ClaimPairComparer` 에 정렬 키 2개 추가 |
| 06 | [step-06-runtime-appetite-machine.md](step-06-runtime-appetite-machine.md) | runtime | `CustomerAppetiteMachine` — 먹는 시간·포화·소화 |
| 07 | [step-07-runtime-economy-ledger.md](step-07-runtime-economy-ledger.md) | runtime | `RevenueLedger` · `RecruitWallet` |
| 08 | [step-08-runtime-placement-cost.md](step-08-runtime-placement-cost.md) | runtime | 배치에 영입 비용·잔액 검사 추가 |
| 09 | [step-09-runtime-coordinator-eating.md](step-09-runtime-coordinator-eating.md) | runtime | 조율자 배선 — 먹는 시간·소비·경제 |
| 10 | [step-10-presentation-stage-hud.md](step-10-presentation-stage-hud.md) | presentation | 손님 상태 표시, 매출·잔액 HUD, 씬 검증 |

## 병렬 실행 가능성

```
01 ──┬──→ 02 ──→ 03
     ├──→ 04 ──→ 05 ──┐
     ├──→ 06 ──────────┼──→ 09 ──→ 10
     └──→ 07 ──→ 08 ───┘
```

- **step-01 이 전부의 선행이다.** 스키마가 바뀌면 나머지가 전부 다시 컴파일된다
- 01 이후 **02·04·06·07 네 갈래가 서로 독립**이다. 파일이 겹치지 않는다:
  - 02·03 → `Belt/SpawnShareTable.cs`, `Belt/SpawnSequence.cs`
  - 04·05 → `Customers/TargetingPriority.cs`, `Customers/ClaimPairComparer.cs`
  - 06 → `Customers/CustomerAppetiteMachine.cs` (신규)
  - 07·08 → `Scoring/*.cs` (신규), `Customers/CustomerPlacementService.cs`
- **step-09 는 03·05·06·08 이 모두 끝나야 시작한다.** `ClaimCoordinator.cs` 하나에 네 갈래가 모인다
- step-10 은 09 이후

> 병렬로 돌릴 때 `.claude/rules/parallel-work.md` §3 을 따른다 — 신규 파일 추가는 안전하고, `ClaimCoordinator.cs` 처럼 **한 파일을 두 워크트리가 고치는 조합은 만들지 않는다.**

## 검증 명령

각 단계 끝에서 돌린다.

```bash
./tests/preflight.sh
```

`preflight` 는 §8 체크리스트 전체를 돌리며, 그중 **"배정 난수 금지"** 항목이 `Assets/Code/Scripts/Runtime` 전역에서 `Random.` 을 잡는다 ([`tests/preflight.sh:59`](../../tests/preflight.sh)). M2 는 배정과 스폰 **둘 다** 난수가 없어야 하므로 이 검사가 두 경로를 동시에 지킨다.

## 리스크와 대응

| 리스크 | 대응 | 어느 단계 |
|---|---|---|
| **타겟팅이 자격 게이트로 샌다** | `CustomerLogic.cs` 에 `Price`/`Targeting` grep 0건을 매 단계 완료 판정에 넣었다 | 04·05·06·09 |
| 자격과 배정이 한 함수로 합쳐진다 | D2 — 상태 머신을 별도 파일로 분리, 원시 값만 받는 시그니처 | 06 |
| 케이스별 분기(1:N/N:1/N:M) 코드가 생긴다 | D3 — 정렬 키만 추가. `SushiClaimResolver` 는 변경 금지 | 05 |
| 난수가 다시 들어온다 | 결정성 테스트 2건 + preflight 의 `Random.` 검사 | 03·05 |
| 스폰이 가방(인스턴스 재고) 방식으로 되돌아간다 | 풀 크기가 구성에 관여하지 않는지 grep 으로 확인 | 03 |
| 밸런스 값을 에이전트가 임의로 채운다 | step-10 에서 **멈추고 사람에게 값을 받는다** | 10 |
| 먹는 동안 초밥을 두 손님이 노린다 | `SushiItem.TryClaim` 이 M1 부터 막고 있다. 회귀 테스트로 고정 | 09 |
