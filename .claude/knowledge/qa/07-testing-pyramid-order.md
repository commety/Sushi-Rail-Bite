# 7. Unit → Integration → System — 순서는 있지만 전부 해야 하는 건 아니다

> Unit tests must always be executed by the developer and should be built into the code itself. (...) run unit tests in parallel to save time, but only move onto integration tests once you've ensured individual components work as they should.
> — Global App Testing, Ch 1 · 2 · 7

## 인디 톤
이 원칙의 핵심은 **"하위 단계 실패 상태에서 상위 단계 돌리지 말라"** 이다. Unity Test Framework 셋업해서 Unit → Integration → System 3계층 전부 유지하라는 뜻 아님. 인디는 대부분 **Bridge 로 돌리는 시스템 스모크** 만으로 시작해도 괜찮다.

## Do (여력 순서)
1. **최소한: 시스템 스모크 (E2E)** — Bridge `EnterPlaymode` + 핵심 필드 스냅샷. 이거 하나만 있어도 "게임이 돌아가는가" 는 증명 가능.
2. **여력 있으면: Integration** — 서브시스템 조합 (Inventory + Economy). 버그 재현 씬으로 격리.
3. **진짜 여력 있으면: Unit** — Unity Test Framework EditMode (순수 C# 로직). 셋업 시간 투자 필요.

**순서 규칙** (이건 지킨다):
- Unit 깨진 상태에서 E2E 돌리면 디버깅 불가 → 하위 실패는 상위 중단 사유.
- Unit 통과는 상위 단계의 **선행 조건**이다. 이 프로젝트에서 EditMode 전량 통과는 커밋 게이트다 (`CLAUDE.md` §8).

## Don't
- Unit 실패를 무시하고 "어차피 Bridge 스모크에서 다 볼 것" 으로 스킵 금지.
- 역피라미드 (Unit 없고 System 수동 검증만) 를 기본값으로 삼지 않는다.

## 강제하지 않는 것 (기본 동작 ❌)
- Unit 테스트 커버리지 수치 (예: 80%) — 무의미한 목표. 커버리지가 아니라 **§5.2 우선순위 순서**가 기준이다.
- PlayMode 테스트 확대 — 느리다. 프레임 정확성이 필요한 경우로 제한한다.

## Unity / Agent (이 프로젝트)
- **TDD 가 기본값이다** (`CLAUDE.md` §5). `Tests.EditMode` 는 optional 이 아니라 **표준 경로**다: 순수 로직 → 상태 머신 → Presenter → SO 검증.
- `Assets/Tests/EditMode` / `Assets/Tests/PlayMode` 로 분리. 어셈블리는 `Tests.EditMode` / `Tests.PlayMode`.
- MonoBehaviour 라서 Unit 으로 못 짜겠다면 → 로직을 순수 C# 클래스로 빼는 것이 수정의 일부다 (`CLAUDE.md` §3.2).
- Bridge 스모크는 **System 단계**다. Unit 을 대체하지 않는다.
- 실패 단계 요약: `U ok · I fail at X · S skipped` 식.
