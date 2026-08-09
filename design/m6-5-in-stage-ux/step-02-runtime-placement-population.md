# Step 02: 배치 한도가 머릿수가 아니라 합계를 센다

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 (`CustomerData.Population`)
- **후행 단계:** step-03 이 `PlacedPopulation` 을 화면에 옮긴다

---

## 목적

`CanPlace` 가 보는 값을 «앉아 있는 사람 수» 에서 «앉아 있는 인구수 합계» 로 바꾼다. **세는 자리는 하나만 늘린다** — 기존 `PlacedCount` 는 그대로 둔다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs` — 수정
- `Assets/Tests/EditMode/Customers/CustomerPlacementServiceTests.cs` — 수정

### 핵심 심볼

```csharp
namespace SushiDefense.Customers
{
    public sealed class CustomerPlacementService
    {
        /// <summary>지금 배치돼 있는 손님 <b>머릿수</b>. 한도 판정에 쓰이지 않는다.</summary>
        public int PlacedCount { get; }          // 기존 그대로 — _bySlot.Count

        /// <summary>
        /// 배치된 손님들의 인구수 합계. <b>한도가 세는 값은 이것이다.</b>
        /// </summary>
        public int PlacedPopulation { get; }

        /// <summary>이 스테이지에 앉힐 수 있는 <b>인구수</b>의 상한.</summary>
        public int MaxPlacedCustomers { get; }   // 이름은 그대로, 의미가 바뀐다
    }
}
```

`CanPlace` 의 한도 항을 바꾼다:

```csharp
// 이전:  PlacedCount < MaxPlacedCustomers
// 이후:  PlacedPopulation + data.Population <= MaxPlacedCustomers
```

**부등호가 `<` 에서 `<=` 로 바뀌는 것에 주의한다.** 이전 식은 «자리 하나를 미리 뺀» 형태였고, 새 식은 «넣고 나서도 넘지 않는가» 를 직접 묻는다. 인구수가 전부 1이면 두 식은 정확히 같다 — 그래서 **기존 테스트가 전부 통과한 채로 새 규칙이 들어온다.** 그 사실이 곧 «새 테스트가 없으면 아무것도 검증되지 않는다» 는 뜻이다.

`PlacedPopulation` 은 **합계를 캐시하지 말고 `_bySlot` 을 순회해 더한다.** 자리는 셋~넷이라 비용이 없고, 캐시하면 `Remove` 에서 빼는 것을 잊는 경로가 생긴다. 순회는 `CanPlace` 안에서 도는데, `CanPlace` 는 카드 흐리기 때문에 **프레임마다 카드 수만큼** 불릴 수 있다 — `foreach` 대신 인덱스 순회나 `Dictionary.ValueCollection` 열거자를 쓰고 LINQ 는 쓰지 않는다 (`Update` 경로 할당 금지, `CLAUDE.md` §4.3).

### 선행 산출물 의존성

- `SushiDefense.Data.CustomerData.Population` — step-01

### 밸런스 수치

없다. `MaxPlacedCustomers` 는 이미 `StageConfig` 에서 온다.

### 제약

- **`PlacedCount` 의 의미를 바꾸지 않는다.** `AudioDirector.cs:233~240` 이 이 값의 증가로 배치음을 낸다 — 인구수로 갈아끼우면 먹보 하나에 값이 2 뛰는데 소리는 그대로 한 번 나서 **테스트가 전부 초록인 채 의미만 어긋난다** (README D2)
- **`StageConfig._maxPlacedCustomers` 의 이름을 바꾸지 않는다** (README D3). XML 문서 주석만 «인구수 상한» 으로 고친다
- `Runtime` 은 `Presentation` 을 모른다
- 이 클래스에 난수가 없다 (배정 결정성)

### 테스트 계획 (TDD — 먼저 실패시킨다)

**공허하게 통과하는 테스트를 조심한다** ([`tests.md`](../../.claude/rules/tests.md) §3). 인구수가 전부 1이면 새 식과 옛 식이 같으므로, **인구수 2 를 실제로 쓰는 테스트가 없으면 아무것도 검증되지 않는다.**

```
CanPlace_PopulationTwoWithTwoHeadroom_ReturnsTrue
CanPlace_PopulationTwoWithOneHeadroom_ReturnsFalse   ← 잔액은 충분하게 준다.
                                                        안 그러면 잔액 항으로도 통과한다
CanPlace_PopulationTwoWithOneHeadroom_AndEmptySlot_ReturnsFalse
                                                     ← 자리는 비어 있다. 자리 항과 한도 항을 가른다
PlacedPopulation_TwoOnesAndOneTwo_ReturnsFour        ← 상수 3 을 돌려주는 구현을 배제한다
PlacedCount_PopulationTwoPlaced_StillCountsHeads     ← D2 의 회귀 그물
Remove_PopulationTwo_FreesTwo                        ← 뺄 때도 합계다
```

- **한도 항만 남기고 다른 항을 비활성화한 판을 만든다.** 잔액을 넉넉히, 자리를 비워 두어야 «한도가 막았다» 임이 확정된다
- `PlacedPopulation_TwoOnesAndOneTwo_ReturnsFour` 는 «둘이 같다» 가 아니라 **구체값 4** 를 박는다

기존 `StageConfigBuilder` (`Assets/Tests/EditMode/Data/StageConfigBuilder.cs`) 와 `SerializedFieldSetter` 로 인구수를 넣는다 — 디스크 애셋을 로드하지 않는다 (`tests.md` §4).

### 완료 판정

- [ ] `grep -n "PlacedPopulation" Assets/Code/Scripts/Runtime/Customers/CustomerPlacementService.cs` — 프로퍼티 + `CanPlace` 2곳
- [ ] `grep -rn "PlacedCount" Assets/Code/Scripts/Presentation/Audio/AudioDirector.cs` — **그대로 남아 있는지** 확인 (고치지 않았다는 증거)
- [ ] Red 확인: 위 6개 중 인구수 2를 쓰는 것들이 **정확히** 실패하고 나머지 전량은 통과
- [ ] Green 뒤 `./tests/run-tests.sh` EditMode 전량
- [ ] 깨뜨려 보기: `<=` 를 `<` 로 되돌리면 `CanPlace_PopulationTwoWithTwoHeadroom_ReturnsTrue` 가 죽는지 확인하고 되돌린다

### 예상 커밋 메시지

```
feat(customer): count the placement limit in population, not heads
```

---

## 금지 사항

- 화면(`StageHudView`·`CustomerHandView`)을 이 단계에서 고치지 않는다 (step-03).
- `PlacedCount` 를 지우거나 의미를 바꾸지 않는다.
- 합계를 필드에 캐시하지 않는다.
