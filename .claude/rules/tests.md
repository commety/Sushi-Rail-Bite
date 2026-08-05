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

1. 순수 게임 로직 — 자격 판정 / 배정(대역 거리→고가→초밥 SeqNo→대역 폭→손님 SeqNo) / 확정 타이밍 / 포화도 / 소화 타이머 / 점수 누적
2. 상태 머신 전이 조건 — `Idle → Eating → Full/Digesting → Idle`
3. Presenter (View 는 스텁으로 대체)
4. SO 데이터 검증 (예: 음수 가격 거부)
5. PlayMode — 벨트 스폰·이동, 풀 재사용, 씬 간 플로우

## 3. 네이밍 · 구조

- 메서드명: `MethodName_StateUnderTest_ExpectedBehavior`
- **자격 · 배정 · 타이밍을 테스트에서도 분리**한다. 한 테스트가 둘 이상을 동시에 검증하면 어디가 깨졌는지 알 수 없다. 특히 **"안 먹었다" 의 이유가 자격인지 대기인지 구분되어야 한다.**

```
자격    CanTake_SushiFarFromBand_ReturnsTrue             ← 대역 밖이어도 자격은 있다
        CanTake_Full_ReturnsFalse
        CanTake_SushiLatchedPastReach_ReturnsTrue        ← 인식 래치 (§1.1-3c)
배정    Resolve_TwoInBandSushi_TakesHigherPrice          ← 대역 안 동점은 고가가 깬다
        Resolve_NarrowBandPlacedLater_StillWins          ← 전문가가 배치순을 이긴다
        Resolve_SamePrice_TakesLowerSushiSequence
        Resolve_SameBoardTwice_ProducesIdenticalResult   ← 결정성 회귀 방지
        Resolve_SushiAlreadyClaimed_NotAssignedAgain
타이밍  Claim_InBandSushiRecognized_ClaimsImmediately
        Claim_OnlyOutOfBandSushi_DefersUntilExit
        Claim_BetterCandidateRecognized_ExtendsDeadline
        Claim_AtDeadline_TakesNearestToBand              ← SeqNo 순이 아니다
불변식  Claim_OnlyOutOfBandSushiInReach_TakesItBeforeExit ← 손님은 반드시 집는다
기타    Digest_AfterCooldown_ResetsSaturation
        AddScore_SushiEaten_IncreasesByPrice
        AddRecruitCurrency_ScoreGained_AccruesOneTenth
```

### 공허하게 통과하는 테스트를 조심한다

검증하려는 키가 **다른 키와 나란히** 놓이면 잘못된 구현으로도 정답이 나온다. M2·M2.5 에서 반복해 겪은 함정이다.

- **대역 폭(키 4)을 볼 때는 손님 SeqNo 를 반대로 준다.** 좁은 대역 손님에게 낮은 번호를 주면 키 5 만으로도 통과한다
- **손님 키를 볼 때는 두 손님이 같은 초밥을 겨루게 한다.** 초밥이 다르면 키 1~3 에서 갈려 손님 키가 아예 불리지 않는다
- **마감시한을 볼 때는 이탈 순서와 선호 순서를 엇갈리게 둔다.** 나란히 두면 "가장 먼저 나가는 것" 을 보는 구현으로도 통과한다
- **"둘이 같다" 만 확인하지 않는다.** 상수를 돌려주는 구현에서도 통과한다 — 구체값이나 반례를 같은 테스트에 함께 박는다
- Arrange–Act–Assert 를 주석 없이도 **빈 줄로** 구분한다.
- 테스트 더블은 손으로 쓴 스텁/페이크 우선. NSubstitute 같은 패키지 추가는 팀 합의 사항이다 (`CLAUDE.md` §7 — 에이전트가 단독으로 패키지를 추가하지 않는다).

## 4. SO 를 쓰는 테스트

`ScriptableObject` 는 테스트 안에서 `ScriptableObject.CreateInstance<SushiData>()` 로 만든다. 디스크의 실제 밸런스 애셋을 로드해서 검증하지 않는다 — 밸런스 수치가 바뀔 때마다 테스트가 깨진다.

밸런스 애셋 자체의 유효성(음수 가격 등)은 **별도의 데이터 검증 테스트**로 분리한다.

## 5. 배정은 결정적이다 — 난수를 쓰지 않는다

대역 우선순위 동률은 **대역 폭 → 순차번호**로 끝난다 (`CLAUDE.md` §1.1-3b). 배정 경로에 난수가 없다.

- **프로덕션 배정 코드에 `Random` 이 등장하면 규칙 위반이다.** 난수원 주입도, 시드 고정도 필요 없다
- 덕분에 테스트가 단순하다 — 같은 판 상태 → 항상 같은 결과. 반복 실행·통계 검증이 불필요하다
- 검증할 것:
  - 거리가 다르면 → 가까운 쪽이 이긴다
  - 대역 안이거나 동거리면 → **비싼 초밥**
  - 같은 초밥을 두고 겨루면 → **대역이 좁은 손님**
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

```bash
./tests/run-tests.sh
```

```bash
./tests/run-tests.sh all
```

**`Unity -batchmode -runTests` 를 손으로 치지 않는다.** `Unity` 는 PATH 에 없고(Hub 경로 해석은 `scripts/lib/unity-path.sh` 에 있다), 손으로 조립하면 `-quit` 을 붙이게 되는데 그러면 **테스트가 끝나기 전에 에디터가 내려간다.**

출력은 요약이다 — `통과: 42/42 (1.2s)`, 실패 시 실패 케이스만. 원본 NUnit XML 은 `tests/.results/` 에 남는다.

종료 코드에서 **컴파일 실패(2)와 테스트 실패(1)가 구분된다.** 배치모드 로그만 봐서는 둘이 비슷해 보이므로, 코드로 먼저 확인한다.

커밋 전 체크리스트(`CLAUDE.md` §8) 전체는 `./tests/preflight.sh` 하나로 돈다. 실패하면 커밋하지 말고 실패를 보고한다. → [`tests/README.md`](../../tests/README.md)

## 7. 금지

- 프로덕션 어셈블리가 `Tests.*` 를 참조하는 것. (asmdef 규칙 §3)
- 검증 없는 테스트 (`Assert` 없이 실행만 하고 통과하는 테스트).
- 테스트를 통과시키려고 프로덕션 코드의 접근 제한자를 넓히는 것. 필요하면 `InternalsVisibleTo` 또는 설계 재검토.
- 테스트가 깨졌을 때 테스트를 지우거나 `[Ignore]` 로 덮는 것. 원인을 보고한다.
