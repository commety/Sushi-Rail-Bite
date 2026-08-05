# Step 07: 문서 9종 일괄 갱신 + preflight 가드 재조준

- **영역:** 문서 · 하네스 (어셈블리 없음)
- **선행 단계:** step-06 까지 (코드가 확정돼야 문서가 사실을 적을 수 있다)
- **후행 단계:** step-08 (밸런스)

---

## 목적

**이 단계의 분량이 코드보다 크다.** *"타겟팅은 단일 값"* 이 9개 문서에 박혀 있고, 그중 하나는 **대역을 반대하는 논증**이라 그대로 두면 다음 사람을 정확히 반대 방향으로 이끈다.

> 규칙 문서와 구현이 어긋나면 다음 에이전트가 잘못된 쪽을 따른다. 그래서 **문서 갱신은 코드와 같은 브랜치·같은 PR** 이다 (계획서 리스크 표).

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤, 아래를 수행한다.

### 수정 파일 — grep 으로 확인한 실제 위치

`grep -rn "타겟팅\|Targeting"` 기준 **총 73건**이다. 전부 고칠 필요는 없고(마일스톤 이력 서술은 그대로 둔다), 아래가 **뜻이 틀리게 되는** 지점이다.

| # | 문서 | 줄 | 무엇 |
|---|---|---|---|
| 1 | `CLAUDE.md` | 19, 24, 30, 37, 40, 73 | §1.1-3a 정의·정렬 키 표·대칭성 문단·§2 용어표. **§1.1-3a 에 마감시한 항목 신설** |
| 2 | `AGENTS.md` | 19, 24, 30, 37, 40, 73 | **`CLAUDE.md` 와 완전 동일하게 유지** (§10 — 두 파일은 동기화 대상) |
| 3 | `.claude/domain/sushi-claim-flow.md` | 한 줄 요약, §1, §2 정렬 키, §5, **§6 통째로**, §7 타입 표·"가격을 읽는 곳" 표 | 본체 |
| 4 | `.claude/domain/data-model.md` | 21, 48 | `CustomerData` 타겟팅 행 |
| 5 | `.claude/rules/scriptable-object.md` | 15, 46, 48~67 (§7 전체) | 표·§7 금지 패턴 |
| 6 | `.claude/rules/scripts.md` | 42~58 | 배정 표·금지 패턴 예시 |
| 7 | `.claude/rules/tests.md` | 33, 36, 58, 64 | 테스트 이름 예시 |
| 8 | `docs/plan/README.md` | 38, 77~88, 107~119, 158, 161 | §1 표 M2.5 ☑, §3-(2) 갱신, §5 Q10 |
| 9 | `docs/on-boarding/README.md` | 101~119 | 규칙 요약 |
| 10 | `docs/plan/M2.5-targeting-band.md` | 완료 판정 체크박스 | 채운다 |

`docs/plan/M2-stats-and-claim.md` · `M4-customer-types.md` 는 **이력 서술**이라 "M2 시점에는 단일 값이었다" 로 읽히면 그대로 둔다. 현재형으로 규칙을 서술한 문장만 고친다.

### 반드시 뒤집어야 할 것 — `sushi-claim-flow.md` §6

제목이 **"왜 타겟팅이 단일 값인가"** 이고 내용이 대역 반대 논증이다. 현재는 상단에 ⚠️ 경고만 붙어 있는데, **M2.5 가 끝나면 경고가 아니라 대체가 필요하다.**

새 §6 은 **"왜 대역인가"** 로 다시 쓰고, 옛 반대 근거 둘이 어떻게 해소됐는지를 남긴다:

| 옛 반대 근거 | 해소 |
|---|---|
| "대역 안은 전부 동점(205 와 250 이 같다)" | **정렬 키 2(고가 우선)가 깬다** |
| "폭이 달라 거리 단위가 손님마다 다르다 → 정규화 필요" | **폭으로 나누지 않는다.** 폭 자체를 이산 타이브레이커(키 4)로 쓴다 |

**옛 논증을 삭제하지 말고 "이래서 틀렸다" 로 남긴다.** 지우면 다음 사람이 같은 반대를 처음부터 다시 발명한다.

### 새로 적어야 할 것 (문서에 아직 없는 사실)

M2.5 가 만든 것 중 **어느 문서에도 없는** 내용이다. 빠뜨리기 쉽다.

- **불변식이 바뀌었다.** *"가격은 자격에 절대 들어가지 않는다"* → **"모든 손님은 유한 시간 안에 반드시 집는다"**. 문자 그대로의 옛 불변식은 마감시한 때문에 유지되지 않는다 — 이 사실을 얼버무리지 않는다
- **래치의 정의가 바뀌었다** (step-04 D4). "인식하고 N초" → **"범위를 벗어나고 N초"**. `sushi-claim-flow.md` §3 과 `StageConfig.RecognitionLatchSeconds` 주석 양쪽. **왜 바꿨는지**(인식 기준은 `latch ≥ 통과시간` 일 때만 맞는 근사였고, stage01 은 3.0초 통과 / 1.0초 래치라 그 조건을 어긴다)를 함께 남긴다 — 이유가 없으면 다음 사람이 "단순하니까" 되돌린다
- **마감시한과 래치의 역할 분담** — 마감시한은 *언제 결정하나*, 래치는 *결정이 한 틱 늦어도 유효하게*
- **대기에는 비용이 있다** — 기다리는 동안 다른 손님이 그 초밥을 가져갈 수 있다 (step-05)
- **"가격을 읽는 곳" 표가 넷에서 다섯으로 는다** — `ClaimDeadline` 추가
- **타입 지도에 `ClaimDeadline` 행 추가**, `TargetingPriority` 행을 대역으로 갱신
- **유형 차이는 대역 폭에서 나온다** — `PatienceRatio` 같은 "까다로움" 손잡이를 두지 않은 이유

### preflight 가드 재조준

`tests/preflight.sh` **5번 검사**(69~81행)를 바꾼다. 현재 패턴:

```bash
'Targeting.*(>|<|>=|<=).*(threshold|Threshold)|Abs\(.*Targeting.*\)\s*(>|>=)'
```

**오탐**: `ClaimDeadline` 은 정당하게 대역을 보고 `false` 를 돌려준다.
**미탐**: 새 금지형 `if (BandDistance(...) > 0) return false;` 를 잡지 못한다.

→ **허용 목록 방식으로 바꾼다** (D6). 대역 산술이 정해진 파일 밖에 등장하면 FAIL.

```
허용: Runtime/Customers/TargetingPriority.cs
      Runtime/Customers/ClaimPairComparer.cs
      Runtime/Customers/ClaimDeadline.cs
검사: BandDistance | BandWidth | TargetingMin | TargetingMax
대상: Assets/Code/Scripts/Runtime/   (주석 제외 필터 적용)
```

같은 형태의 검사를 하나 더 붙인다 — **자격 경로 청정성**:

```
CustomerLogic.cs · CustomerAppetiteMachine.cs 에 Price|Targeting  →  0건이어야 한다
```

`record` 헬퍼·SKIP 분기·`--fast` 동작은 기존 항목들과 **같은 형태를 유지**한다. 검사 이름은 `"대역 산술 격리"` · `"자격 경로 청정"` 처럼 무엇을 보는지가 드러나게 짓는다.

### 제약

- **`tests/preflight.sh` 는 CI 가 아니라 로컬 하네스다.** 수정해도 §7 의 "CI 설정 변경" 에 해당하지 않지만, **검사를 약하게 만드는 방향의 수정은 하지 않는다.** 오탐이 난다고 검사를 지우지 않는다
- 문서에 **아직 사실이 아닌 것을 적지 않는다.** step-08 이 끝나기 전이므로 밸런스 값은 "사람이 정한다" 로 남긴다
- `CLAUDE.md` 와 `AGENTS.md` 는 **바이트 단위로 같은 내용**이어야 한다 (§10). 한쪽만 고치는 사고가 이 단계 최대 위험이다
- 새 문서를 만들지 않는다. M2.5 는 규칙의 **교체**라 기존 문서 안에서 끝난다

### 완료 판정

- [ ] `diff <(sed -n '1,200p' CLAUDE.md) <(sed -n '1,200p' AGENTS.md)` — **차이 0줄**
- [ ] `grep -rn "단일 가격\|단일 값" CLAUDE.md AGENTS.md .claude/ docs/ | grep -v "M2 " | grep -v "이력"` — 남은 건이 **전부 "옛날에는 그랬다" 서술**임을 눈으로 확인
- [ ] `grep -rn "−|가격−타겟팅|\|−\\\\|가격 − 타겟팅\\\\|" CLAUDE.md AGENTS.md .claude/ docs/` — 옛 산정식이 **현재형으로** 남아 있지 않다
- [ ] `grep -n "ClaimDeadline" .claude/domain/sushi-claim-flow.md` — 타입 지도·가격 읽는 곳 표에 등장한다
- [ ] `grep -n "유한 시간" CLAUDE.md .claude/domain/sushi-claim-flow.md` — 새 불변식이 두 곳에 있다
- [ ] `grep -n "BandDistance" tests/preflight.sh` — 가드가 재조준됐다
- [ ] `./tests/preflight.sh` **전 항목 PASS** — 새 5번 검사 포함. 이 단계에서 처음으로 5번이 정상 PASS 해야 한다
- [ ] 가드가 실제로 작동하는지 **한 번 확인한다**: `CustomerLogic.cs` 에 `// BandDistance` 가 아니라 진짜 코드 한 줄을 임시로 넣고 preflight 가 FAIL 하는지 본 뒤 되돌린다

### 예상 커밋 메시지

```
docs(claim): rewrite targeting rules for the band and deadline model
```

하네스는 분리한다:

```
chore(tests): guard band arithmetic by allowlist instead of regex
```

---

## 금지 사항

- **`CLAUDE.md` 만 고치고 `AGENTS.md` 를 두지 않는다.** 둘은 동기화 대상이다 (§10)
- `sushi-claim-flow.md` §6 의 옛 논증을 **삭제하지 않는다.** "이래서 틀렸다" 로 남긴다
- preflight 검사를 **약화시키지 않는다.** 오탐이 나면 허용 목록을 정확히 하고, 검사를 지우지 않는다
- 코드를 수정하지 않는다. 문서를 쓰다 코드가 틀린 것을 발견하면 **문서를 코드에 맞추지 말고** 멈추고 어느 단계로 되돌아가야 하는지 보고한다
- 밸런스 값을 문서에 확정으로 적지 않는다. step-08 전이다
