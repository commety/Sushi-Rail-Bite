# Step 06: 보상 생성 — `RewardOffer` · `RewardGenerator`

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 (`RewardCatalog`) · step-05 (`RunState` · `IRandomSource`). **둘 다 필요하다**
- **후행 단계:** step-08 의 Presenter 가 이 후보 목록을 화면에 올리고, 고른 것을 되돌려 준다

---

## 목적

"클리어했다" 와 "덱이 자랐다" 사이를 잇는다. 카탈로그에서 **아직 안 가진 카드**만 추려 몇 개를 제시하고, 고른 것을 런에 반영한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Run/RewardKind.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Run/RewardOffer.cs` — **생성**
- `Assets/Code/Scripts/Runtime/Run/RewardGenerator.cs` — **생성**
- `Assets/Tests/EditMode/Run/RewardGeneratorTests.cs` — **생성**
- `Assets/Tests/EditMode/Run/RewardOfferTests.cs` — **생성**

### 핵심 심볼

```csharp
namespace SushiDefense.Run
{
    /// <summary>보상 한 장이 무엇인가.</summary>
    public enum RewardKind
    {
        SushiCard = 0,
        Customer = 1
    }

    /// <summary>
    /// 제시된 보상 한 장. <b>둘 중 하나만 채워진다</b> — 종류가 그것을 말한다.
    /// </summary>
    public readonly struct RewardOffer
    {
        public static RewardOffer OfSushi(SushiData sushi);
        public static RewardOffer OfCustomer(CustomerData customer);

        public RewardKind Kind { get; }
        public SushiData Sushi { get; }
        public CustomerData Customer { get; }

        /// <summary>화면에 띄울 이름. 카드의 <c>DisplayName</c> 이며 비어 있으면 애셋 이름이다.</summary>
        public string DisplayName { get; }
    }

    /// <summary>
    /// 클리어 시 제시할 후보를 뽑고, 고른 것을 런에 반영한다.
    ///
    /// <para>
    /// <b>이미 가진 카드는 제시하지 않는다</b> (작업서 D9). 스폰 비중이 가격에서만 유도되므로
    /// 중복 카드는 아무 효과가 없다 — 손으로 적는 가중치 필드가 없다는 것이 곧 그 뜻이다.
    /// </para>
    /// </summary>
    public sealed class RewardGenerator
    {
        public RewardGenerator(RewardCatalog catalog);

        /// <summary>
        /// 이 런에 제시할 후보. <b>최대 <c>catalog.OfferCount</c> 개</b>이며, 미보유 카드가
        /// 그보다 적으면 있는 만큼만 담긴다. 하나도 없으면 빈 목록이다.
        /// </summary>
        public void Generate(RunState run, List<RewardOffer> results);

        /// <summary>
        /// 고른 보상을 런에 반영한다. 이미 가진 카드였다면 <c>false</c> 를 돌려주고
        /// <b>런을 그대로 둔다</b> — 부분 적용이 없다.
        /// </summary>
        public static bool Apply(RunState run, in RewardOffer offer);
    }
}
```

### 후보를 뽑는 규칙

```
1. 카탈로그의 초밥 풀 + 손님 풀을 순회하며 null 이 아니고 런이 아직 갖지 않은 것만 모은다
2. 모인 목록을 IRandomSource 로 부분 셔플한다 (Fisher-Yates, 앞 OfferCount 개만)
3. 앞에서 OfferCount 개를 잘라 results 에 담는다
```

**부분 셔플인 이유**: 전체를 섞으면 카탈로그가 커질수록 낭비가 커지고, "인덱스를 무작위로 골라 중복이면 다시" 방식은 루프 횟수가 난수에 따라 달라져 **같은 시드로도 소비되는 난수 개수가 흔들린다.** Fisher-Yates 는 정확히 `min(OfferCount, N)` 번만 뽑는다.

**초밥과 손님을 한 풀에 섞는다.** 종류별 쿼터(초밥 2 + 손님 1 같은)를 두지 않는다 — 쿼터를 두면 한쪽 풀이 마를 때 "몇 개를 제시하나" 가 애매해지고, 그 규칙이 곧 밸런스 손잡이가 되어 SO 필드를 또 부른다.

### 할당 주의

`Generate` 는 `List<RewardOffer>` 를 **인자로 받아 채운다.** 돌려주지 않는다 — 클리어는 판당 한 번이라 성능이 문제는 아니지만, 이 저장소의 다른 배출 경로(`SushiClaimResolver.Resolve`)와 형태를 맞춘다. 내부 후보 버퍼도 **필드로 잡아 재사용**한다.

### 선행 산출물 의존성

- `RewardCatalog` — step-01
- `RunState` · `SushiDeck` · `CustomerDeck` · `IRandomSource` — step-04 · step-05

### 밸런스 수치

**없다.** 제시 개수는 `RewardCatalog.OfferCount` 에서 읽는다. 이 파일에 숫자 상수가 없어야 한다.

### 제약

- 로직은 `MonoBehaviour` 밖 순수 C#
- **`UnityEngine.Random` · `System.Random` 을 쓰지 않는다.** 난수는 `run.Random` 뿐이다 (step-05 의 가드가 잡는다)
- `Runtime.Data` 에 계산을 추가하지 않는다 — 미보유 판정은 여기 있다
- `RewardOffer` 는 `readonly struct` 다. 화면이 들고 다니는 값이라 변경 가능하면 어디서 바뀌었는지 추적할 수 없다
- `Apply` 가 **부분 적용을 하지 않는다.** 실패하면 런은 호출 전과 완전히 같다 (`RecruitWallet.TrySpend` 와 같은 계약)
- `Runtime` → `Presentation` 참조 금지
- `StageController` 에서 이 생성기를 부르지 않는다. 컨트롤러는 "끝났다" 만 알린다 (step-03 의 금지 사항)

### 완료 판정

- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/Run/RewardGenerator.cs` — `IRandomSource`·`run.Random` 경유만, 전역 난수 0건
- [ ] `grep -rn "new List<" Assets/Code/Scripts/Runtime/Run/RewardGenerator.cs` — 전부 **필드 초기화**
- [ ] `grep -rn "[0-9]" Assets/Code/Scripts/Runtime/Run/RewardGenerator.cs | grep -vE '(///|//|\*)'` — 숫자 리터럴이 배열 인덱스 외에 없다
- [ ] `git diff --stat Assets/Code/Scripts/Runtime/Stages/ Assets/Code/Scripts/Presentation/` — **0줄**
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름

```
RewardOfferTests
  OfSushi_Built_KindIsSushiCardAndCustomerIsNull
  OfCustomer_Built_KindIsCustomerAndSushiIsNull
  DisplayName_CardWithName_UsesIt
  DisplayName_CardWithBlankName_FallsBackToAssetName

RewardGeneratorTests
  Generate_EmptyCatalog_ProducesNothing
  Generate_AllCardsOwned_ProducesNothing               ← ★ 착수 시점의 실제 상태다
  Generate_PoolLargerThanOfferCount_ProducesExactlyOfferCount
  Generate_PoolSmallerThanOfferCount_ProducesWholePool
  Generate_OwnedCard_IsNeverOffered                    ← ★ D9
  Generate_MixedPools_CanOfferBothKinds
  Generate_SameSeedTwice_ProducesIdenticalOffers       ← ★ 결정성 회귀 방지
  Generate_DifferentSeeds_ProduceDifferentOffers
  Generate_CatalogWithNullEntry_SkipsIt
  Generate_CalledTwice_DoesNotAccumulateIntoResults    ← 인자 리스트를 먼저 비운다

  Apply_SushiOffer_AddsToDeck
  Apply_CustomerOffer_AddsToRoster
  Apply_AlreadyOwned_ReturnsFalseAndLeavesRunUnchanged
```

> **★ `Generate_AllCardsOwned_ProducesNothing`** 은 가정적인 케이스가 아니다. **지금 저장소의 실제 상태**다 — 초밥 5종이 전부 stage01 시작 덱에 있어 보상 풀이 빈다. step-09 에서 사람이 애셋을 정할 때까지 이것이 정상 동작이다.
>
> **★ `Generate_OwnedCard_IsNeverOffered`** 는 **공허하게 통과하기 쉽다.** 풀에 미보유 카드 1장 + 보유 카드 1장을 넣고 `OfferCount = 2` 로 두면, 중복 제거를 안 하는 구현도 "2장 중 하나는 미보유" 로 통과할 수 있다. **보유 카드 3장 + 미보유 1장, `OfferCount = 3`** 으로 짜서 결과가 정확히 1장이고 그것이 그 미보유 카드임을 확인한다.
>
> **★ `Generate_SameSeedTwice_ProducesIdenticalOffers`** 하나가 난수를 들이면서도 결정성을 잃지 않았음을 지킨다 (`.claude/rules/tests.md` §5 의 `Assert.AreEqual(resolver.Resolve(board), resolver.Resolve(board))` 와 같은 역할).
>
> `Generate_DifferentSeeds_ProduceDifferentOffers` 는 **풀을 충분히 크게**(10장 이상) 잡는다. 3장 풀에서 3장을 제시하면 시드와 무관하게 같은 집합이 나와 늘 실패한다.

### 예상 커밋 메시지

```
feat(reward): offer unowned cards on clear and apply the chosen one
```

---

## 금지 사항

- 전역 난수(`UnityEngine.Random` · `System.Random`)를 쓰지 않는다
- 이미 가진 카드를 제시하지 않는다 (D9). "중복이면 비중 증가" 같은 규칙을 만들지 않는다 — 스폰 비중은 가격에서만 나온다
- 종류별 쿼터를 만들지 않는다
- 보상 확률·희귀도를 만들지 않는다. 원본 기획에 없고, 넣는 순간 SO 필드와 밸런스 결정이 딸려 온다 — 필요해 보이면 멈추고 보고한다
- `StageController` 를 수정하지 않는다
- `Presentation` 을 수정하지 않는다. 화면은 step-08 이다
- 시너지·`SushiTrait` 을 건드리지 않는다 (D8)
