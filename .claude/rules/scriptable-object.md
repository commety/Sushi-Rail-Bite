---
description: ScriptableObject 정의(Runtime.Data)와 밸런스 애셋 편집 규칙
globs: ["Assets/Code/Scripts/Runtime.Data/**/*.cs", "Assets/**/*.asset"]
---

# ScriptableObject / 밸런스 데이터 규칙

이 게임의 밸런스는 **전부 데이터 주도**다 (`CLAUDE.md` §3.1). 코드를 고치지 않고 기획을 바꿀 수 있어야 한다.

## 1. SO 로 정의해야 하는 것

| 타입 | 담는 것 |
|---|---|
| `SushiData` | 가격, 포화도 기여량, 특성(Trait — 시너지 키), 스프라이트 |
| `CustomerData` | 집기 범위(reach), **타겟팅 대역(`min`~`max` — 선호이지 제약이 아님, §7)**, 먹는 시간, 최대 포화도, 소화 시간, **영입 비용** |
| `StageConfig` | 목표 매출, 제한 시간, **최대 배치 손님 수**, **초기 영입 예산**, 테이블 배치, 벨트 속도, 초밥 스폰 구성, 보너스 목표 |
| `~EventChannelSO` | 시스템 간 통신 채널 (`SushiEatenEventChannelSO` 등) |

**순차번호(SequenceNumber)는 SO 에 넣지 않는다.** 초밥은 스폰 시, 손님은 배치 시 발급되는 **런타임 값**이다. 필드 전체 목록과 근거는 [`../domain/data-model.md`](../domain/data-model.md).

## 2. 정적 데이터 ↔ 런타임 상태 분리

- SO 는 **읽기 전용 템플릿**이다. 런타임에 SO 필드를 쓰지 않는다.
- 현재 포화도, 남은 소화 시간, 지금 먹고 있는 초밥 같은 가변 값은 별도 클래스/구조체(`CustomerRuntimeState` 등)에 둔다.
- 이유: 에디터에서 SO 를 런타임에 수정하면 **플레이 종료 후에도 디스크에 남는다**. 밸런스 애셋이 조용히 오염된다.

## 3. 필드 작성

- `[SerializeField] private` + public 읽기 전용 프로퍼티. `public` 필드 금지.
- 수치 필드에는 범위 제약을 건다 (`[Min(0)]`, `[Range(a, b)]`). 가격·포화도·시간은 음수가 될 수 없다.
- public API 에 `///` XML 문서 주석. `Runtime.Data` 는 다른 모든 어셈블리가 읽는 계약이다.
- `[CreateAssetMenu(menuName = "SushiRailBite/...")]` 로 생성 메뉴를 붙인다.

## 4. 의존 방향

`Runtime.Data` 는 가장 아래 어셈블리다. `Runtime` · `Presentation` 을 참조하지 않는다. (asmdef 규칙 §3)

## 5. 밸런스 애셋(`.asset`) 편집 — 사람의 판단 영역

- **기획 의도 확인 없이 밸런스 SO 데이터를 일괄 수정하지 않는다** (`CLAUDE.md` §7).
- 에이전트는 변경안과 diff 를 제시하고 승인을 기다린다.
- 수치를 바꾼 이유(어떤 기획 결정에서 나왔는지)는 `.claude/domain/` 에 남긴다. `.asset` 파일 자체는 이유를 기록하지 못한다.

## 6. 검증

새 SO 타입에는 데이터 유효성 EditMode 테스트를 붙인다 (`CLAUDE.md` §5.2-4). 예: 음수 가격 거부, 타겟팅 `min ≤ max`, 소화 시간 > 0.

**밸런스를 검증에 섞지 않는다.** 대역이 좁다·넓다, 덱 가격대와 맞다·아니다는 오류가 아니라 기획이다. `OnValidate` 는 구조 불변식만 본다.

## 7. 타겟팅 대역은 "필터"가 아니다 — 자주 틀리는 지점

`CustomerData` 의 타겟팅은 **선호 가격 대역**(`_targetingMin` ~ `_targetingMax`)이고,
**먹을 수 있는 것을 제한하는 값이 아니다** (`CLAUDE.md` §1.1-3a).

이름이 `Min`/`Max` 라 "이 사이만 먹는다" 로 읽히기 쉽지만 정확히 반대다. 대역이 쓰이는 곳은 둘이다:

| 쓰이는 곳 | 무엇을 정하나 |
|---|---|
| **배정** | 누가 무엇을 가져가나 — 대역 밖 거리(키 1), 대역 폭(키 4) |
| **타이밍** | 지금 확정하나 기다리나 — 대역 안이면 즉시, 아니면 이탈 직전 |

**자격 판정(먹을 수 있나)에는 절대 쓰이지 않는다.** 다음 코드는 기획 위반이다:

```csharp
// ❌ 대역을 자격 게이트로 — 이러면 손님이 영원히 굶는다
if (TargetingPriority.BandDistance(price, min, max) > 0) return false;

// ❌ 같은 위반의 옛날 형태 (단일 값 시절)
if (Mathf.Abs(price - _data.Targeting) > threshold) return false;
```

더 맞는 대안이 없으면 손님은 **대역에서 아무리 먼 초밥도 먹는다.** 다만 즉시 먹지는 않고
**범위를 벗어나기 직전까지 기다린다** — 기다림과 굶기는 다르다.

> **불변식**: *모든 손님은 유한 시간 안에 반드시 집는다.* 대역은 *무엇을 · 언제* 를 정할 뿐
> **영구 배제는 금지**다. `ClaimDeadline` 이 `false`(=아직 아니다)를 돌려주는 것은 위반이 아니며,
> 유한 시간 뒤 반드시 확정으로 바뀌어야 한다.

SO 필드 이름을 `MaxEatablePrice` 처럼 "먹을 수 있는" 뉘앙스로 짓지 않는다. 이름이 잘못되면
다음 사람이 반드시 게이트로 쓴다.

**대역 산술을 `Runtime.Data` 에 두지 않는다.** `max − min` 한 줄이라 `CustomerData` 프로퍼티로
넣고 싶어지지만, `Runtime.Data` 는 계약이지 계산이 아니고 — 무엇보다 산술이 흩어지면
`tests/preflight.sh` 의 허용 목록 가드가 절반만 지킨다. 계산은 `Runtime` 의 `TargetingPriority`
한 곳이다.

> 산정식은 **확정됐다**: 대역 밖 거리 `max(0, min−가격, 가격−max)`, 대역 안 동점은 고가 우선,
> 손님끼리 겹치면 좁은 대역 우선. 상세는 [`../domain/sushi-claim-flow.md`](../domain/sushi-claim-flow.md) §2.

**대역을 좁히면 처리량이 떨어진다.** 대역 밖 초밥은 이탈 직전까지 유예되므로, **경합 상대가
없어도** 손님이 그만큼 논다. M4 에서 기본 손님의 대역을 좁혔다가 스테이지 1 이 클리어
불가가 됐다 (소비율 96% → 78%). 범용가의 대역은 **덱의 가격 전 구간을 덮어야** 한다 —
[`../domain/customer-kinds.md`](../domain/customer-kinds.md) §3.
