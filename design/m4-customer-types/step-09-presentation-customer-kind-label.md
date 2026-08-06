# Step 09: 손님 유형 표시

- **영역:** `presentation` — 어셈블리 `Presentation` (+ `Tests.PlayMode`)
- **선행 단계:** 없음 (독립. 다른 모든 단계와 병렬 가능)
- **후행 단계:** 없음

---

## 목적

세 유형이 자리에 앉았을 때 **어느 손님인지 화면에서 구분되지 않는다.** 지금 자리 옆 라벨은 대역만 보여 준다 (`100~300`).

M2.5 를 한 이유 자체가 "플레이어가 규칙을 배우지 못한다" 였다. 유형이 셋으로 늘면 대역 숫자만으로는 "이게 소식인가 먹보인가" 를 매번 역산해야 한다. **이름을 같이 띄운다.**

**색으로 구분하지 않는다.** 몸통 색은 이미 상태(대기·먹는 중·소화 중)를 나타내고 있어서, 유형까지 색에 실으면 두 정보가 충돌한다. 아트는 M5 다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정
- `Assets/Tests/PlayMode/Customers/CustomerViewTests.cs` — 수정

### 변경 내용

`Bind()` 안에서 만드는 라벨 문구에 손님 이름을 더한다.

```
지금:  "100~300"
바꿈:  "기본\n150~250"
```

`CustomerData.DisplayName` 을 쓴다. **비어 있으면 애셋 이름으로 대신한다** — `RewardOffer.DisplayName` 이 같은 이유로 같은 처리를 하고 있으므로 그 형태를 따른다. 이름을 안 채운 애셋에서 빈 줄이 나오면 라벨이 고장 난 것처럼 보인다.

`CustomerKind` **enum 을 화면 문구로 쓰지 않는다.** `BigEater` 가 그대로 뜨면 안 되고, `switch` 로 한글을 붙이면 **표시 문자열이 코드로 들어가** §3.1 위반이다. 이름은 애셋의 `_displayName` 에 있다.

### 문구를 만드는 시점

**`Bind()` 에서 한 번만** 만든다 (지금과 같다). 손님의 정적 데이터는 런타임에 바뀌지 않는다. `LateUpdate` 에서 매 프레임 문자열을 만들면 WebGL 에서 GC 스파이크가 그대로 프레임 히칭이 된다 (§4.3).

### 검증용 프로퍼티

`BandText` 는 이름을 유지하되 내용이 두 줄이 된다. 기존 이름을 그대로 두는 이유: 이 프로퍼티를 읽는 곳이 테스트뿐이고, 이름을 바꾸면 **바꾼 이유가 아니라 이름 변경 자체가 diff 의 대부분**이 된다.

> 이름과 내용이 어긋난다고 판단되면 **바꿔도 좋다.** 다만 그때는 커밋을 나눈다 — 이름 변경 커밋과 내용 변경 커밋을 섞지 않는다.

### 테스트

기존 `CustomerViewTests` 의 대역 문구 단언은 **내용이 바뀌었으므로 갱신 대상**이다. 이것은 "테스트를 고쳐서 통과시키는 것" 이 아니다 — 기대 동작이 바뀐 것이고, 갱신하지 않으면 새 동작이 검증되지 않는다.

추가할 테스트:

```
Bind_BigEaterData_ShowsKindNameAndBand      ← "먹보" 와 "100~150" 이 모두 있다
Bind_DataWithoutDisplayName_FallsBackToAssetName
Bind_Null_ClearsLabel                       ← 기존 동작 유지 확인
```

### 공허하게 통과하지 않게

- **`..._ShowsKindNameAndBand` 는 이름과 대역을 둘 다 확인한다.** 이름만 보면 대역이 사라진 구현이 통과하고, 대역만 보면 이 단계가 아무것도 안 한 것과 같다.
- **두 개의 서로 다른 유형으로 각각 확인한다.** 하나만 보면 상수를 돌려주는 구현이 통과한다.

### 선행 산출물 의존성

- `SushiDefense.Data.CustomerData.DisplayName` — 이미 있다

### 밸런스 수치

없음. 표시 이름은 애셋 필드에서 온다.

### 제약

- **판정하지 않는다.** 뷰는 상태·대역을 읽기만 한다 (§3.2)
- `Update`/`LateUpdate` 에서 문자열 결합·할당 금지 (§4.3)
- 씬을 편집하지 않는다 — 라벨 오브젝트는 이미 있다
- `CustomerData` 스키마를 바꾸지 않는다

### 완료 판정

- [ ] `grep -rn "CustomerKind" Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` 가 **0건** (enum 을 문구로 쓰지 않았다)
- [ ] `grep -n "\$\"" Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` 의 결과가 **`Bind()` 안에만** 있다
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(customer): show customer name alongside targeting band
```

---

## 금지 사항

- 유형별 색·스프라이트를 넣지 않는다 (M5).
- `CustomerKind` 로 분기하지 않는다. 유형은 **데이터 차이**다 (README D9).
- 다른 뷰·프레젠터를 건드리지 않는다.
