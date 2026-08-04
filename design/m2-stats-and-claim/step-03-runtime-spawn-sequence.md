# Step 03: `SpawnSequence` — credit 누적 배출로 교체

- **영역:** `runtime` — 어셈블리 `Runtime`
- **선행 단계:** step-01 (`Weight` 제거) · step-02 (`SpawnShareTable`)
- **후행 단계:** step-09 가 스테이지 시작 리셋을 회귀 테스트로 고정한다

---

## 목적

share 를 **순서**로 바꾼다. 요구사항은 두 가지이고 서로 긴장 관계다:

1. 유형별 등장 비율이 share 에 정확히 비례할 것
2. 비싼 초밥이 **앞에 뭉치지 않을 것** — 초반이 자연히 싼 쪽으로 기울어야 한다

credit 누적이 둘을 동시에 만족한다.

```
각 유형에 credit_i += share_i
credit 이 가장 큰 유형을 배출        ← 동률이면 덱 순서 (결정적)
그 유형의 credit -= 1
```

share 가 0.5 / 0.25 / 0.25 면 `A B C A | A B C A | …` 가 나온다. 비율은 정확히 2:1:1 이고 **고르게 흩어진다.** share 가 낮은 유형은 credit 이 차는 데 시간이 걸려 제 주기대로만 나오므로, **초반 억제 장치를 따로 넣을 이유가 없다.**

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs` — **내부 전면 교체** (파일·타입 이름은 유지)
- `Assets/Code/Scripts/Runtime/Belt/SushiBelt.cs` — 수정 (생성자에서 α 를 넘겨 수열을 만든다)
- `Assets/Tests/EditMode/Belt/SpawnSequenceTests.cs` — 수정 (가중치 반복 전제의 기존 테스트를 credit 전제로 다시 씀)

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    /// <summary>share 를 결정적 배출 순서로 바꾼다. 난수를 쓰지 않는다.</summary>
    public sealed class SpawnSequence
    {
        /// <summary>배출할 유형이 하나도 없다. 벨트는 아무것도 올리지 않는다.</summary>
        public bool IsEmpty { get; }

        /// <summary>다음에 올릴 초밥. 비었으면 null.</summary>
        public SushiData Next();

        /// <summary>credit 을 전부 0 으로 되돌린다. 스테이지 시작 시 부른다.</summary>
        public void Reset();

        public SpawnSequence(IReadOnlyList<SushiSpawnEntry> deck, float sparsityExponent);
    }
}
```

`SushiBelt` 쪽 변경은 한 줄이다:

```csharp
_spawnSequence = new SpawnSequence(config.SpawnTable, config.SparsityExponent);
```

### 선행 산출물 의존성

- `SushiDefense.Belt.SpawnShareTable` — step-02. `SpawnSequence` 가 **소유**한다 (생성자에서 만들어 필드로 든다)
- `SushiDefense.Data.StageConfig.SparsityExponent` — step-01

### 밸런스 수치

- α 만 인자로 받는다. 코드 상수 0건

### 제약

- **`credit` 배열은 생성자에서 한 번 잡고 `Next()` 마다 재사용한다.** `Next()` 안에서 `new` / LINQ / 문자열 결합을 만들지 않는다 — 스폰은 `Update` 경로다 (`CLAUDE.md` §4.3, 배포 타깃 WebGL)
- **동률은 덱 순서로 끝낸다.** `credit` 최대값이 같으면 **인덱스가 낮은 쪽**을 배출한다. 이 한 줄이 결정성을 만든다
- **`Random` 을 쓰지 않는다.** preflight 의 "배정 난수 금지" 검사가 `Runtime` 전역을 훑는다
- **정수 나눗셈을 도입하지 않는다.** credit 은 `float` 누적이다. 가중합과 인스턴스 개수가 어긋날 자리가 애초에 없어야 한다
- 로직은 `MonoBehaviour` 밖 순수 C#

### 완료 판정

- [ ] `grep -rn "Random" Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs` — **0건**
- [ ] `grep -n "new \|Linq" Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` — 생성자 밖에 할당이 없는지 눈으로 확인
- [ ] `grep -rn "SushiPool\|Prewarm" Assets/Code/Scripts/Runtime/Belt/SpawnSequence.cs Assets/Code/Scripts/Runtime/Belt/SpawnShareTable.cs` — **0건** (풀 크기가 구성에 관여하지 않는다)
- [ ] EditMode 전량 Green — `./tests/run-tests.sh`
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 테스트 이름 (플랜 §테스트 항목 그대로)

```
Emit_LowShareType_NotClusteredInFirstWindow     ← 고른 분산. 이 단계의 핵심
Emit_SameDeckTwice_ProducesIdenticalSequence    ← 결정성 회귀선
Emit_TypeNotInDeck_NeverAppears
Reset_AtStageStart_RestartsFromBeginning
Emit_TwoToOneShares_AlternatesEvenly            ← 0.5/0.25/0.25 → A B C A
Emit_OverLongWindow_RatioMatchesShare
Next_EmptyDeck_ReturnsNull
```

`Emit_LowShareType_NotClusteredInFirstWindow` 를 어떻게 쓸지가 이 단계에서 가장 애매한 지점이다. **"앞 구간의 등장 횟수가 전체 비율에서 크게 벗어나지 않는다"** 로 쓴다 — 예: 24개 구간에서 share 8.7% 인 유형이 첫 8개 안에 2개 이상 나오지 않는다. 임계값은 테스트 안의 지역 상수로 두고 근거를 주석에 남긴다.

`Emit_SameDeckTwice_ProducesIdenticalSequence` 는 **난수 재도입을 막는 회귀선**이다 (`.claude/rules/tests.md` §5). 같은 덱으로 수열 2개를 만들어 N회 배출을 통째로 비교한다.

### 예상 커밋 메시지

```
feat(belt): emit spawn order from credit accumulation without randomness
```

---

## 금지 사항

- **가방(인스턴스 재고) 방식으로 되돌리지 않는다.** 풀에 인스턴스를 몇 개씩 담아 두는 구성은 가중합과 개수가 정수에서 어긋나고, 남는 칸을 채우는 휴리스틱이 결국 난수를 부른다
- **초반 억제 장치(nice value 류)를 덧붙이지 않는다.** 고른 분산이 이미 해결한다. 덧붙이면 α 와 손잡이가 겹쳐 조절만 어려워진다
- `SpawnShareTable` 을 수정하지 않는다. share 계산이 틀렸다면 step-02 로 되돌아가 보고한다
- `ClaimCoordinator` · `CustomerLogic` 을 건드리지 않는다
