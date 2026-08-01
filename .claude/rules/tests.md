---
description: Assets/Tests/ 하위 Unity Test Framework 테스트 작성 규칙 (TDD 기본값)
globs: ["Assets/Tests/**/*.cs"]
---

# 테스트 작성 규칙

**이 프로젝트의 기본 개발 방식은 TDD 다** (`CLAUDE.md` §5). 실패하는 테스트 작성 → 최소 구현 → 리팩터. 새 로직을 테스트 없이 커밋하지 않는다.

## 1. 어디에 무엇을 쓰나

| 위치 | 어셈블리 | 대상 | 비중 |
|---|---|---|---|
| `Assets/Tests/EditMode/` | `Tests.EditMode` | 순수 로직 — `CustomerLogic`, 집기·경합 해소, 점수 계산, 상태 머신, Presenter | **대부분** |
| `Assets/Tests/PlayMode/` | `Tests.PlayMode` | 씬 통합, 코루틴·타이밍 의존 — 벨트 이동, 오브젝트 풀 재사용, 씬 전환 | 최소한 |

PlayMode 는 느리다. **프레임 단위 정확성이 실제로 필요한 경우에만** 쓴다. "MonoBehaviour 라서 EditMode 로 못 짜겠다"면 그건 테스트 문제가 아니라 로직이 `MonoBehaviour` 안에 갇혀 있다는 신호다 — 로직을 plain C# 으로 빼는 게 먼저다.

## 2. 우선순위 (`CLAUDE.md` §5.2)

1. 순수 게임 로직 — 자격 판정 / 배정(거리→고가→SeqNo) / 포화도 / 소화 타이머 / 점수 누적
2. 상태 머신 전이 조건 — `Idle → Eating → Full/Digesting → Idle`
3. Presenter (View 는 스텁으로 대체)
4. SO 데이터 검증 (예: 음수 가격 거부)
5. PlayMode — 벨트 스폰·이동, 풀 재사용, 씬 간 플로우

## 3. 네이밍 · 구조

- 메서드명: `MethodName_StateUnderTest_ExpectedBehavior`
- **자격과 배정을 테스트에서도 분리**한다. 한 테스트가 둘을 동시에 검증하면 어디가 깨졌는지 알 수 없다.

```
자격  CanTake_SushiFarFromTargeting_ReturnsTrue          ← 타겟팅에서 멀어도 자격은 있다
      CanTake_Full_ReturnsFalse
      CanTake_SushiLatchedPastReach_ReturnsTrue          ← 인식 래치 (§1.1-3c)
배정  Resolve_OneCustomerManySushi_TakesNearestToTargeting
      Resolve_EqualDistance_TakesHigherPrice             ← 190 vs 210 → 210
      Resolve_SamePrice_TakesLowerSushiSequence
      Resolve_ManyCustomersOneSushi_HigherPriorityWins
      Resolve_EqualPrioritySameSushi_LowerCustomerSequenceWins
      Resolve_SameBoardTwice_ProducesIdenticalResult     ← 결정성 회귀 방지
      Resolve_SushiAlreadyClaimed_NotAssignedAgain
기타  Digest_AfterCooldown_ResetsSaturation
      AddScore_SushiEaten_IncreasesByPrice
      AddRecruitCurrency_ScoreGained_AccruesOneTenth
```
- Arrange–Act–Assert 를 주석 없이도 **빈 줄로** 구분한다.
- 테스트 더블은 손으로 쓴 스텁/페이크 우선. NSubstitute 같은 패키지 추가는 팀 합의 사항이다 (`CLAUDE.md` §7 — 에이전트가 단독으로 패키지를 추가하지 않는다).

## 4. SO 를 쓰는 테스트

`ScriptableObject` 는 테스트 안에서 `ScriptableObject.CreateInstance<SushiData>()` 로 만든다. 디스크의 실제 밸런스 애셋을 로드해서 검증하지 않는다 — 밸런스 수치가 바뀔 때마다 테스트가 깨진다.

밸런스 애셋 자체의 유효성(음수 가격 등)은 **별도의 데이터 검증 테스트**로 분리한다.

## 5. 배정은 결정적이다 — 난수를 쓰지 않는다

타겟팅 우선순위 동률은 **순차번호**로 끝난다 (`CLAUDE.md` §1.1-3b). 배정 경로에 난수가 없다.

- **프로덕션 배정 코드에 `Random` 이 등장하면 규칙 위반이다.** 난수원 주입도, 시드 고정도 필요 없다
- 덕분에 테스트가 단순하다 — 같은 판 상태 → 항상 같은 결과. 반복 실행·통계 검증이 불필요하다
- 검증할 것:
  - 거리가 다르면 → 가까운 쪽이 이긴다
  - 동거리면 → **비싼 초밥** (`타겟팅±d` 두 지점 중 고가)
  - 동거리 + 같은 가격이면 → **초밥 SeqNo** 낮은 쪽
  - 같은 초밥을 두고 동거리면 → **손님 SeqNo** 낮은 쪽
  - 같은 입력을 두 번 넣으면 → **완전히 같은 배정 결과** (결정성 자체를 테스트로 고정)
  - 후보가 있으면 → 아무도 못 집는 결과가 **나오지 않는다**

```csharp
// 결정성 회귀 방지 — 이 테스트 하나가 난수 재도입을 막는다
Assert.AreEqual(resolver.Resolve(board), resolver.Resolve(board));
```

> 난수가 필요해 보이는 상황이 생기면 그건 **순차번호 설계가 빠진 신호**다. 난수를 넣지 말고 순서 키를 먼저 정한다.

## 6. 실행

```
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml -quit
```

커밋 전 체크리스트(`CLAUDE.md` §8)는 EditMode 전량 + 관련 PlayMode 통과를 요구한다. 실패하면 커밋하지 말고 실패를 보고한다.

## 7. 금지

- 프로덕션 어셈블리가 `Tests.*` 를 참조하는 것. (asmdef 규칙 §3)
- 검증 없는 테스트 (`Assert` 없이 실행만 하고 통과하는 테스트).
- 테스트를 통과시키려고 프로덕션 코드의 접근 제한자를 넓히는 것. 필요하면 `InternalsVisibleTo` 또는 설계 재검토.
- 테스트가 깨졌을 때 테스트를 지우거나 `[Ignore]` 로 덮는 것. 원인을 보고한다.
