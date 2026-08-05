# Step 08: 대역 값 — **사람이 정한다**

- **영역:** 밸런스 애셋 (`CLAUDE.md` §7 — 사람 판단 영역)
- **선행 단계:** step-07 까지 전부
- **후행 단계:** 없음. M2.5 의 마지막 단계

---

## 목적

step-01 이 `_targetingPrice` 를 지운 순간부터 **stage01 의 손님 셋은 대역 `0~0` 이다.** Unity 가 사라진 필드를 버리고 새 필드를 기본값으로 채웠기 때문이다.

`0~0` 은 단순히 "값이 없는" 상태가 아니라 **적극적으로 이상한** 상태다:

- 모든 초밥이 대역 **밖**이다 → 아무도 즉시 집지 않고 전부 이탈 직전까지 기다린다
- 폭이 **0** 이다 → 정렬 키 4에서 최강 전문가라 손님끼리의 경합이 손님 순차번호로만 갈린다

**이 단계가 끝나야 M2.5 가 완성된다.** `Stage01SceneTests.Play_Scene_CustomersHaveMeaningfulBands`(step-06)가 그때까지 빨간 상태로 남아 이 단계를 빠뜨릴 수 없게 한다.

---

## 에이전트 실행 지침

**에이전트가 값을 임의로 채우지 않는다** (`CLAUDE.md` §7). 아래 표를 제시하고 사람에게 값을 받는다.

### 물어볼 것 (1) — 손님 대역

| 애셋 | M2 의 단일 값 | 계획서 예시안 | 확정 |
|---|---|---|---|
| `Customer.Standard` (기본) | 120 | 100 ~ 300 | ? |
| `Customer.SmallEater` (소식) | 300 | 300 ~ 550 | ? |
| `Customer.Placeholder` | 100 | *(계획서에 없음)* | ? |

### 물어볼 것 (2) — 덱과 맞물리는 두 가지

**덱의 가격은 5종이다**: 120(한치) · 150(연어) · 150(광어) · 190(방어) · **300(장어, 최고가)**.

| 확인할 것 | 왜 |
|---|---|
| **소식 대역 `300~550` 은 대역 안 후보가 정확히 1종뿐이다** | 상한 550 은 더 비싼 초밥이 생기기 전까지 아무 일도 하지 않는다. 의도된 희소성인가, 아니면 상한을 낮출까 |
| **기본 `100~300` 과 소식 `300~550` 이 300 에서 겹친다** | 계획서가 키 4(대역 폭)를 넣은 근거가 이 겹침이다. 겹침을 유지할지, 애셋 설계에서 없앨지 |

> **α = 1.0 스폰 규칙에서 비싼 초밥은 의도적으로 희소하다** ([`spawn-composition.md`](../../.claude/domain/spawn-composition.md)). 소식좌의 가동률이 자기 스탯이 아니라 **공급률**에 묶이는 구조라, 대역 값과 α 를 함께 봐야 한다 (계획서 리스크 표).

### 래치는 물어보지 않는다

step-04 에서 **D4(이탈 기준 래치)가 확정**됐다. `_recognitionLatchSeconds: 1` 은 **그대로 둔다** — 마감시한이 만료보다 항상 먼저 오므로 값이 불변식에 영향을 주지 않는다.

이 필드는 이제 순수한 연출 손잡이다: *"범위를 벗어난 초밥을 몇 초까지 붙들어 주나."* 밸런스 조정 대상이 되면 그때 다룬다.

### 물어볼 것 (3) — 목표 매출 (M2 에서 넘어온 미결)

M2 PR #10 에 ⚠️ 로 남긴 항목이다. **이번에 같이 볼지만 확인한다.**

`Stage01._targetRevenue: 2500` 은 평균가 130 가정으로 뽑았는데 확정 가격의 평균은 **165** 다. M2.5 는 손님이 **기다렸다가 더 비싼 것을 집게** 만들므로 실효 매출이 한 번 더 오른다 — 두 요인이 같은 방향이라 목표가 헐거워질 수 있다.

**M2.5 의 범위 밖이다.** 사람이 "지금 조정" 이라고 하면 하고, 아니면 그대로 둔다.

### 수정 파일 (값을 받은 뒤)

- `Assets/Level/Balance/Customer.Standard.asset`
- `Assets/Level/Balance/Customer.SmallEater.asset`
- `Assets/Level/Balance/Customer.Placeholder.asset`
- `Assets/Level/Balance/Stage01.Placeholder.asset` — *(3) 목표 매출을 조정하기로 한 경우에만*

### 제약

- **값을 받기 전까지 이 단계를 시작하지 않는다.** M2.5 최대 위반 지점이다
- `.asset` 파일은 **`_targetingMin` · `_targetingMax` 두 줄만** 바뀐다. `git diff` 로 확인한다 — Unity 가 다른 필드를 재직렬화하면 리뷰가 어려워진다
- **값을 바꾼 이유를 `.claude/domain/` 에 남긴다** (`.claude/rules/scriptable-object.md` §5). `.asset` 파일은 이유를 기록하지 못한다
- 밸런스 커밋을 **기능 커밋과 분리한다** (§9 — 사람이 공개용 여부를 판단한 뒤)
- `Assets/Art/` 에 새 스프라이트를 만들지 않는다. 새 손님 유형이 필요해 보이면 **M4** 다

### 완료 판정

- [ ] `grep -n "_targeting" Assets/Level/Balance/Customer.*.asset` — 세 애셋 모두 `Min`/`Max` 가 채워져 있고 `min ≤ max`
- [ ] `git diff Assets/Level/Balance/` — **타겟팅 줄 외에 바뀐 것이 없다**
- [ ] `Stage01SceneTests.Play_Scene_CustomersHaveMeaningfulBands` — **Green** (step-06 부터 빨갛던 것이 여기서 녹색이 된다)
- [ ] `Stage01SceneTests.Play_Scene_WaitingCustomerEventuallyEats` — Green
- [ ] 씬을 재생하면 **소식좌가 싼 초밥을 흘려보내고 장어를 기다린다** — 이 마일스톤을 눈으로 확인하는 지점
- [ ] EditMode + PlayMode 전량 Green — `./tests/run-tests.sh all`
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] `./scripts/run.sh webgl` 빌드 성공 — **빌드 후 `git status --short Assets/Settings/` 로 누출 재확인**
- [ ] `docs/plan/M2.5-targeting-band.md` 완료 판정 체크박스 **전부** 채워짐
- [ ] `docs/plan/README.md` §1 표에서 M2.5 ☑, §5 Q10 해소 표기

### 예상 커밋 메시지

```
chore(balance): set targeting bands for stage 01 customers
```

---

## 금지 사항

- **사람 확인 없이 값을 채우지 않는다** (§7). 이 단계의 존재 이유다
- 값이 "명백해 보인다" 는 이유로 건너뛰지 않는다. 계획서의 예시안은 **예시**이지 결정이 아니다
- 사람이 지정하지 않은 필드를 함께 손보지 않는다. M2 때의 원칙 그대로 — **지정한 부분만 고치고, 지정하지 않은 부분은 그대로 둔다**
- 새 `CustomerData` · `SushiData` 애셋을 만들지 않는다. 손님 유형 확장은 **M4**, 초밥 추가는 **M3** 다
- 밸런스가 이상해 보인다고 코드를 고치지 않는다. 값이 잘못된 것과 규칙이 잘못된 것은 다르다 — 후자라고 판단되면 멈추고 보고한다
