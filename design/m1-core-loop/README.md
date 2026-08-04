# M1 — 코어 루프 검증 작업서

## 한 줄 요약

초밥이 벨트 시작점에서 계속 나와 라인을 따라 흐르고, 테이블에 앉은 손님이 자기 범위에 들어온 초밥을 집는다 — **스탯 없이, 집는다/안 집는다만.**

## 원문

[`docs/plan/M1-core-loop.md`](../../docs/plan/M1-core-loop.md) (원본 1차 목표). 집기 규칙의 근거는 [`.claude/domain/sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md).

> **M1 에는 가격이 없다.** 포화도·소화·타겟팅·점수는 전부 M2 다. 여기서 만드는 것은 그 위에 얹힐 **뼈대**다.

---

## 착수 전 결정된 열린 질문

플랜이 착수 전 답을 요구한 항목이다. 아래는 **확정된 답**이며, 이 작업서 전체가 이 답을 전제로 짜였다.

| # | 질문 | **결정** | 작업서에 미치는 영향 |
|---|---|---|---|
| Q2 | 초밥 스폰 | **시간에 걸쳐 계속** | `StageConfig.SpawnIntervalSeconds` 신설. 벨트에 스폰 타이머가 들어간다 (step-02) |
| Q3 | 라인 끝 초밥 | **사라지고 풀 반납** | 끝점 도달이 곧 풀 반납 지점. 순환 없음 (step-02) |
| Q4 | 손님 배치 시점 | **진행 중에도 가능** ⚠️ | 기본안과 다르다. 배치가 재배정 트리거가 되고, 배치 조작이 별도 단계로 커진다 (step-08) |
| Q9 | 인식 래치 상한 | **시간 상한 — 인식 후 N초** ⚠️ | 기본안과 다르다. `StageConfig.RecognitionLatchSeconds` 신설. 후보 집합이 인식 시각을 들고 있어야 한다 (step-04) |

⚠️ 표시 둘은 플랜의 기본안과 다른 선택이다. 관련 문서를 M1 완료 시 갱신해야 한다 — [`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §3·§7 의 "미결" 표기와 [`docs/plan/README.md`](../../docs/plan/README.md) §5 의 Q2·Q3·Q4·Q9 행. **step-09 의 완료 판정에 포함돼 있다.**

---

## 아키텍처 결정

### D1. `SushiItem` 이름은 이미 `Runtime` 이 쓴다 — 뷰는 `SushiItemView`

플랜 §산출물 표는 `SushiItem : MonoBehaviour` 를 `Presentation` 에 두라고 적었지만, **M0 이 `SushiDefense.Belt.SushiItem` 을 순수 C# 런타임 상태로 이미 만들었다** (확인함). `CLAUDE.md` §2 용어표도 `SushiItem` 을 "벨트 위 초밥"의 런타임 타입으로 정의한다.

같은 이름을 두 어셈블리에 두면 `using` 하나로 어느 쪽인지 알 수 없게 된다. **뷰는 `SushiItemView`, 손님 뷰는 `CustomerView`, 자리 뷰는 `TableSlotView`** 로 간다. 순수 로직과 껍데기의 이름이 눈으로 구분되는 편이 이 프로젝트의 §3.2 를 지키기에도 낫다.

### D2. 초밥은 풀을 **두 겹**으로 쓴다 — 로직 풀과 뷰 풀

- `Runtime` — `SushiPool<SushiItem>` 이 순수 상태 객체를 재사용한다. 스폰마다 `new SushiItem` 을 하면 WebGL 에서 GC 가 쌓인다
- `Presentation` — 기존 `SushiPoolBehaviour`(`ISushiInstanceFactory<GameObject>`) 가 화면 오브젝트를 재사용한다

둘을 한 겹으로 합치려면 `Runtime` 이 `GameObject` 를 알아야 하고, 그 순간 벨트 로직이 EditMode 밖으로 나간다. **`Destroy` 호출 0** 이라는 완료 판정은 뷰 풀이 지키고, 로직 풀은 할당 0 을 지킨다.

### D3. 벨트 → 뷰는 C# 이벤트로 뒤집는다

`SushiBelt`(Runtime)가 `SushiSpawned` · `SushiRemoved` 이벤트를 쏘고, `SushiBeltView`(Presentation)가 구독해 뷰를 빌리고 돌려준다. `Runtime` 은 `Presentation` 을 모른 채로 남는다 (`CLAUDE.md` §3.3).

SO 이벤트 채널(`~EventChannelSO`)을 쓰지 않는 이유: 채널은 **씬·시스템을 가로지르는** 통신용이다. 벨트와 그 벨트의 뷰는 같은 오브젝트의 두 겹이라 C# 이벤트가 맞고, SO 를 하나 더 만들면 씬에 물릴 애셋만 는다.

### D4. 스폰 선택은 결정적이다 — 난수를 쓰지 않는다

`StageConfig.SpawnTable` 의 가중치를 **결정적 라운드로빈**으로 소화한다. 같은 스테이지를 두 번 돌리면 같은 순서로 나온다.

배정 경로의 난수 금지(`CLAUDE.md` §1.1-3b)가 스폰까지 강제하지는 않지만, **M1 의 완료 판정이 "순차번호 낮은 것부터 집는다"** 라서 스폰이 흔들리면 그 테스트가 흔들린다. 스폰에 변주가 필요해지면 M3 에서 별도 결정으로 다룬다.

### D5. 범위 진입은 계산으로 얻는다 — 물리 없음

[`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §4 에서 **이미 채택된 결정**이다. 벨트는 1차원·등속이라 초밥이 손님 범위 `[table − reach, table + reach]` 에 들어오는 시각이 위치·속도만으로 나온다. `OnTriggerEnter2D` 를 쓰면 판정이 `MonoBehaviour` 콜백 안으로 끌려들어가 EditMode 테스트가 막힌다.

그래서 M1 은 **`Rigidbody2D`·`Collider2D` 를 쓰지 않는다.** 물리 컴포넌트가 등장하면 D5 위반이다.

### D6. 자격과 배정은 **처음부터 다른 파일**에 둔다

M1 에는 가격이 없어 자격 판정이 저절로 지켜진다. 문제는 M2 에서 가격이 생길 때다 — 두 판정이 한 메서드에 있으면 `TargetingPrice` 검사가 자격 쪽에 슬쩍 끼어드는 것이 이 프로젝트의 전형적 실패다 (`CLAUDE.md` §1.1-3a).

- `CustomerLogic` — 자격만. **가격 타입을 아예 참조하지 않는다**
- `SushiClaimResolver` — 배정만. M2 에서 정렬 키가 추가되는 유일한 곳

M1 의 정렬 키는 `초밥 SeqNo → 손님 SeqNo` 둘뿐이지만, **쌍 랭킹 → 그리디 확정** 구조를 지금 만들어 둔다. M2 는 키 두 개를 앞에 끼우기만 하면 된다.

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `StageConfig` 3필드 · `TableSlotDefinition` 1필드 추가 |
| runtime | `Runtime` | `SushiBelt` · `CustomerLogic` · `ReachWindow` · `CandidateSet` · `SushiClaimResolver` · `ClaimCoordinator` |
| presentation | `Presentation` | `SushiBeltView` · `SushiItemView` · `CustomerView` · `TableSlotView` · `CustomerPlacementController` |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 자격·배정·래치·벨트 (EditMode 대부분) / 씬 통합 (PlayMode 최소) |

## 의존성 그래프

```
SushiDefense.Data.StageConfig ─┬→ SushiDefense.Belt.SushiBelt ──────┐
                               │      (스폰·이동·끝점, SushiPool<SushiItem>)
                               │                                     ├→ SushiDefense.Belt.ClaimCoordinator
SushiDefense.Data.CustomerData ┴→ SushiDefense.Customers.CustomerLogic  (오케스트레이션)
                                       (자격만)                      │
                                  SushiDefense.Customers.ReachWindow ┤  (진입 시각 계산)
                                  SushiDefense.Customers.CandidateSet ┤  (래치 + 시간 상한)
                                  SushiDefense.Customers.SushiClaimResolver ┘  (쌍 랭킹 → 그리디)
                                                    │
                                          (C# event, D3)
                                                    ↓
SushiDefense.Belt.SushiBeltView · SushiItemView · CustomerView · TableSlotView (Presentation)
```

## 새 밸런스 수치

**값은 정하지 않는다. 필드만 만든다** (`CLAUDE.md` §3.1 · §7).

| 필드 | 들어갈 SO | 제약 | 근거 |
|---|---|---|---|
| `SpawnIntervalSeconds` | `StageConfig` | `[Min]` 양수 | Q2 = 시간에 걸쳐 계속 |
| `BeltLength` | `StageConfig` | `[Min]` 양수 | 끝점 판정 기준. 순수 로직이 씬 없이 알아야 한다 |
| `RecognitionLatchSeconds` | `StageConfig` | `[Min(0)]` | Q9 = 시간 상한. 0 이면 상한 없음으로 읽는다 |
| `BeltPosition` | `TableSlotDefinition` | — | 자리의 1차원 벨트 좌표. 기존 `Position`(Vector2)은 화면 배치용이라 별개다 |

기존 필드 중 M1 이 처음 실제로 쓰는 것: `StageConfig.BeltSpeed` · `StageConfig.SpawnTable` · `StageConfig.TableSlots` · `CustomerData.Reach`.

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 01 | [step-01-data-belt-fields.md](step-01-data-belt-fields.md) | data | SO 필드 4개 추가 + 검증 테스트 |
| 02 | [step-02-runtime-belt.md](step-02-runtime-belt.md) | runtime | `SushiBelt` — 스폰·이동·끝점·풀 반납 |
| 03 | [step-03-runtime-eligibility.md](step-03-runtime-eligibility.md) | runtime | `CustomerLogic` — **자격만** |
| 04 | [step-04-runtime-candidates.md](step-04-runtime-candidates.md) | runtime | `ReachWindow` + `CandidateSet` — 진입 계산·래치·시간 상한 |
| 05 | [step-05-runtime-claim-resolver.md](step-05-runtime-claim-resolver.md) | runtime | `SushiClaimResolver` — 쌍 랭킹 → 그리디 |
| 06 | [step-06-runtime-coordinator.md](step-06-runtime-coordinator.md) | runtime | `ClaimCoordinator` — 재배정 트리거 6종을 묶는다 |
| 07 | [step-07-presentation-views.md](step-07-presentation-views.md) | presentation | 뷰 4종 — 로직에 위임만 |
| 08 | [step-08-presentation-placement.md](step-08-presentation-placement.md) | presentation | 손님 배치 조작 (**진행 중 배치 포함**) |
| 09 | [step-09-scene-integration.md](step-09-scene-integration.md) | 통합 | 씬·프리팹 조립, PlayMode 검증, 문서 갱신 |

**총 9단계** — data 1 · runtime 5 · presentation 2 · 통합 1.

## 병렬 실행 가능성

```
step-01 (data)
   ├─→ step-02 (벨트) ─────────────────┐
   └─→ step-03 (자격) → step-04 (후보) → step-05 (배정) → step-06 (조립)
                                                                  ↓
                                                        step-07 (뷰)
                                                                  ↓
                                                        step-08 (배치 조작)
                                                                  ↓
                                                        step-09 (씬 통합)
```

- **step-02 와 step-03 은 병렬 가능** — 파일이 겹치지 않는다 (플랜 §작업 순서와 일치)
- step-04 는 step-03 의 `CustomerLogic` 시그니처가 필요하다 (자격 상실 시 후보 제거)
- step-06 은 step-02 와 step-05 **둘 다** 끝나야 시작
- step-07 이후는 순차. 씬 파일은 한 번에 한 워크트리만 만진다 (`.claude/rules/parallel-work.md` §3)

## M1 이 만들지 않는 것

경계를 흐리면 M2 가 갈 곳이 없어진다.

- **가격·점수** — `SushiEatenEventChannelSO` 는 M0 에 있지만 M1 은 **발행하지 않는다**. 점수 집계는 M2
- **포화도 증가·소화 타이머** — `CustomerRuntimeState` 에 필드는 있으나 M1 은 값을 올리지 않는다. 자격 판정은 `HasSaturationHeadroom` 을 읽기만 한다
- **타겟팅** — `SushiClaimResolver` 가 `CustomerData.TargetingPrice` 를 **읽지 않는다**. M2 에서 정렬 키로 추가된다
- **영입 재화·배치 비용** — 배치는 M1 에서 무료다. `RecruitCost` 는 M2
- **아트** — placeholder 색 블록으로 간다 (M5)
