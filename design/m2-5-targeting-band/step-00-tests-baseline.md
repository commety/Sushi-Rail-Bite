# Step 00: (선택) 현재 동작의 기준선 측정

- **영역:** `tests` — 어셈블리 `Tests.EditMode` *(산출물은 코드가 아니라 숫자다)*
- **선행 단계:** 없음
- **후행 단계:** 없음. **건너뛰어도 M2.5 는 완성된다**

---

## 목적

계획서가 *"0단계 측정 권장"* 으로 남긴 항목이다. 바꾸기 **전** 의 손님별 집기 분포를 찍어 둔다.

두 가지를 얻는다.

1. *"소식좌가 싼 걸 먼저 집는다"* 는 체감이 실제로 얼마나 심한지 — 60초에 몇 번인지
2. 바꾼 뒤 비교할 기준선 — step-05 이후 같은 측정을 돌려 **개선을 숫자로 보여 준다**

측정 없이 진행해도 기능은 완성되지만, 이 변경의 목적이 **가독성/체감**이라 "고쳤다"를 주장할 근거가 없어진다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 생성/수정 파일

- `Assets/Tests/EditMode/Customers/ClaimDistributionProbe.cs` — **생성 후 삭제** (커밋하지 않는다)
- `design/m2-5-targeting-band/baseline.md` — **생성** (커밋한다)

**하네스는 버리고 숫자만 남긴다.** 이 프로브는 검증이 아니라 관측이라 `Assert` 가 없고, 그대로 두면 `.claude/rules/tests.md` §7 의 *"검증 없는 테스트"* 금지에 걸린다.

### 측정 방법

`StageBootstrap` 을 쓰지 않는다 — `MonoBehaviour` 가 필요 없다. `ClaimCoordinator` 를 직접 세워 고정 스텝으로 돌린다.

```csharp
// 스텝은 고정한다. Time.deltaTime 을 쓰면 측정이 재현되지 않는다.
const float Step = 1f / 60f;
for (var t = 0f; t < 60f; t += Step) coordinator.Tick(Step);
```

- `SushiEaten` 을 구독해 `(손님 SeqNo, 초밥 가격)` 을 누적한다
- 손님 배치는 stage01 자리 좌표(`5` · `10` · `15`)를 그대로 쓴다
- `StageConfig` 는 `StageConfigTestFactory` 로 코드에서 만든다 — **디스크의 밸런스 애셋을 로드하지 않는다** (`.claude/rules/tests.md` §4). 대신 stage01 과 같은 수치를 코드에 적고, 어느 값을 베꼈는지 `baseline.md` 에 남긴다

### `baseline.md` 에 남길 것

| 항목 | 왜 |
|---|---|
| 측정에 쓴 stage01 수치 전부 (벨트 속도·스폰 간격·래치·α·덱 가격 5종·손님 스탯) | 나중에 밸런스가 바뀌면 비교가 무의미해진다. 조건을 못 박는다 |
| 손님별 집은 초밥 **가격 히스토그램** | 본론 |
| 손님별 **총 집은 수 / 총 매출** | 대역 전환이 가동률을 떨어뜨리는지 보는 축 |
| **`Customer.SmallEater`(타겟팅 300)가 300 미만을 집은 비율** | 이 변경이 없애려는 바로 그 증상 |
| 60초 동안 **한 번도 못 집은 손님이 있는지** | 불변식의 기준선 |

### 제약

- **프로덕션 코드를 한 줄도 고치지 않는다.** 관측만 한다
- 프로브 파일을 커밋에 포함시키지 않는다. `git status` 로 확인한다
- `Random` 을 쓰지 않는다 — 배정도 스폰도 결정적이라 **1회 실행이면 충분하다.** 반복·평균이 필요하다고 느껴지면 어딘가에 난수가 들어간 것이므로 멈추고 보고한다

### 완료 판정

- [ ] `design/m2-5-targeting-band/baseline.md` 에 위 5개 항목이 채워져 있다
- [ ] `git status` 에 `ClaimDistributionProbe.cs` 가 없다 (파일·`.meta` 둘 다)
- [ ] `./tests/run-tests.sh` — 삭제 후에도 EditMode 전량 Green

### 예상 커밋 메시지

```
docs(claim): record pre-M2.5 claim distribution baseline
```

---

## 금지 사항

- 프로덕션 코드 수정 금지. 측정하다 버그를 발견하면 **고치지 말고 `baseline.md` 에 적는다**
- 프로브를 "회귀 테스트" 로 승격시키지 않는다. `Assert` 를 붙이고 싶어지면 그건 step-05 의 테스트로 가야 할 내용이다
- 이 단계가 어렵거나 오래 걸리면 **건너뛴다.** 선택 단계다
