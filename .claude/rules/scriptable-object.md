---
description: ScriptableObject 정의(Runtime.Data)와 밸런스 애셋 편집 규칙
globs: ["Assets/Code/Scripts/Runtime.Data/**/*.cs", "Assets/**/*.asset"]
---

# ScriptableObject / 밸런스 데이터 규칙

이 게임의 밸런스는 **전부 데이터 주도**다 (`CLAUDE.md` §3.1). 코드를 고치지 않고 기획을 바꿀 수 있어야 한다.

## 1. SO 로 정의해야 하는 것

| 타입 | 담는 것 |
|---|---|
| `SushiData` | 초밥 가격, 손님이 느끼는 포화도, 스프라이트, 태그(연어·알 등 시너지 키) |
| `CustomerData` | 집기 범위(reach), **타겟팅 가격대(선호 대역 — 제약이 아님, §7)**, 먹는 속도, 최대 포화도, 소화 시간 |
| `StageConfig` | 목표 매출, 제한 시간, 테이블 배치, 보너스 목표, 등장 기믹 |
| `~EventChannelSO` | 시스템 간 통신 채널 (`SushiEatenEventChannelSO` 등) |

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

새 SO 타입에는 데이터 유효성 EditMode 테스트를 붙인다 (`CLAUDE.md` §5.2-4). 예: 음수 가격 거부, 타겟팅 대역의 `min <= max`, 소화 시간 > 0.

## 7. 타겟팅(Targeting) 필드는 "필터"가 아니다 — 자주 틀리는 지점

`CustomerData` 의 타겟팅 가격대는 **먹을 수 있는 것을 제한하는 값이 아니다** (`CLAUDE.md` §1.1-3a).

- 손님은 집기 범위 안에 들어온 초밥을 **가격과 무관하게 FIFO 로** 집는다.
- 타겟팅은 **한 초밥을 두 명 이상이 동시에 집을 수 있을 때** 누가 가져갈지 정하는 **우선순위 산정용**이다.
- 따라서 이 필드를 `if (price < min || price > max) return false;` 같은 **게이트로 쓰면 기획 위반**이다. 손님이 눈앞의 초밥을 두고 구경만 하는 상황은 버그로 취급한다.

SO 필드 이름을 `MinEatablePrice` / `MaxEatablePrice` 처럼 "먹을 수 있는" 뉘앙스로 짓지 않는다. `TargetingRange`(또는 `TargetingMin`/`TargetingMax`) 처럼 **우선순위용**임이 드러나는 이름을 쓴다. 이름이 잘못되면 다음 사람이 반드시 게이트로 쓴다.
