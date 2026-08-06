# Step 10: 밸런스 애셋과 씬 조립

- **영역:** `data` (`Runtime.Data` 애셋) + 씬
- **선행 단계:** step-01 (`RunConfig` 타입) · step-07 (자리 바인딩) · step-08 (진행 배선) 모두 완료 필요
- **후행 단계:** 없음 — M4 의 마지막 단계

---

## 목적

앞의 아홉 단계는 **값이 하나도 없는 구조**를 만들었다. 여기서 실제 손님·스테이지·런을 채우고 씬에 물려, 3판이 이어서 도는 것을 눈으로 확인한다.

**이 단계는 씬을 편집한다.** [`rules/parallel-work.md`](../../.claude/rules/parallel-work.md) §3 에 따라 **다른 워크트리와 병행하지 않는다.**

---

## §7 승인 상태

밸런스 값은 **착수 시 사람이 확정했다** (README "착수 전에 확정한 것"). 이 단계는 확정된 값을 입력하는 것이므로 추가 승인이 필요 없다.

**표에 없는 값을 임의로 정하지 않는다.** 채워야 하는데 없는 필드가 나오면 **멈추고 묻는다** — M3 에서 초밥 포화도를 에이전트가 유추해 채운 전례가 있고, 그건 PR 에 별도로 표시해야 했다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### A. 기존 애셋 수정 (1건)

**`Assets/Level/Balance/Customer.Standard.asset`**

| 필드 | 현재 | 바꿈 |
|---|---|---|
| `_targetingMin` | 100 | **150** |
| `_targetingMax` | 300 | **250** |

다른 필드는 건드리지 않는다. 기본 손님의 대역을 좁히는 이유는 README "대역 재배치" 절에 있다 — 넓은 대역이 다른 두 유형과 겹치면 배정 키 4(폭)에 밀려 **겹친 구간 전부에서 항상 양보**하게 된다.

### B. 새 애셋 (4건)

**`Assets/Level/Balance/Customer.BigEater.asset`**

```
_id: customer-big-eater
_displayName: 먹보
_description: 빠르게 많이 먹지만 싼 초밥만 노린다.
_kind: 2                  (BigEater)
_reach: 3
_targetingMin: 100
_targetingMax: 150
_eatSeconds: 1
_maxSaturation: 8
_digestSeconds: 3
_recruitCost: 60
_icon: {fileID: 0}
```

**`Assets/Level/Balance/Stage02.asset`**

```
_stageNumber: 2
_displayName: Stage 02
_targetRevenue: 6200
_timeLimitSeconds: 60
_maxPlacedCustomers: 4
_initialRecruitBudget: 80
_beltSpeed: 2
_spawnIntervalSeconds: 1.3
_beltLength: 20
_recognitionLatchSeconds: 1
_sparsityExponent: 1
_tableSlots:   자리 4개 — 아래 표
_spawnTable:   []          ← 비운다 (README D6)
_bonusObjectives: []
```

**`Assets/Level/Balance/Stage03.asset`** — Stage02 와 같되:

```
_stageNumber: 3
_displayName: Stage 03
_targetRevenue: 7500
_initialRecruitBudget: 100
_spawnIntervalSeconds: 1.1
```

자리 4개 (Stage02 · Stage03 공통):

| `_slotIndex` | `_beltPosition` | `_position` |
|---|---|---|
| 0 | 4 | (-6, -2) |
| 1 | 8 | (-2, -2) |
| 2 | 12 | (2, -2) |
| 3 | 16 | (6, -2) |

> **`16 + Reach 3 = 19 < 벨트 길이 20`** 을 반드시 확인한다. 넘으면 초밥이 범위를 벗어나기 전에 끝점에서 사라져 **마감시한이 영영 오지 않는다** ([`rules/scripts.md`](../../.claude/rules/scripts.md) §2).

**`Assets/Level/Balance/Run.Demo.asset`** (`RunConfig`)

```
_displayName: 데모 런
_stages: [Stage01.Placeholder, Stage02, Stage03]     ← 이 순서
```

### C. 보상 풀 갱신

**`Assets/Level/Balance/RewardCatalog.asset`** 의 `_customerPool` 에 `Customer.BigEater` 를 더한다.

결과: 초밥 3장(참치·성게·달걀) + 손님 2명(소식·먹보) = **후보 5장 중 3장 제시**.

### D. 씬 조립 (`Assets/Level/Scenes/Stage01.unity`)

1. **`TableSlot3` 오브젝트 추가** — 기존 `TableSlot0~2` 와 같은 구성(자식 `Seat` 에 `CustomerView`). 스테이지 1 은 자리 3개이므로 step-07 의 바인딩이 **이 자리를 꺼 둔다** — 그것이 정상 동작이다.
2. **`StageTransition` 오브젝트 추가** — `StageTransitionView` + 자식 `StageTransitionLabel`(`TextMesh`). `RewardView` 구성을 그대로 따른다.
3. **`StageBootstrap` 에 물리기** — `_runConfig` = `Run.Demo`, `_transitionView` = 위 오브젝트, `_slots` 에 `TableSlot3` 포함.

`_stageConfig` 는 **그대로 둔다.** 목록이 있으면 쓰이지 않지만, 목록을 지웠을 때의 폴백이다 (README D5).

> **씬 편집은 ClaudeBridge 로 한다** (`./scripts/bridge-run.sh`). 씬 YAML 을 손으로 쓰면 `fileID` 를 맞출 수 없다. `.meta` 파일도 손으로 만들지 않는다 (RULE-03).

### E. 실측 (이 단계의 진짜 산출물)

애셋을 넣은 것만으로는 밸런스가 맞는지 알 수 없다. **재생해서 숫자를 본다.**

| 측정 | 확인할 것 |
|---|---|
| 스테이지 1 클리어 | 목표 5000 이 여전히 닿는가 (기본 대역이 좁아진 영향) |
| 스테이지 2 벨트 총액 | 예측 7599 와 얼마나 다른가 |
| 스테이지 2·3 클리어 가능성 | 자리 4개 실측 소비율이 M3 의 91%(3자리) 대비 어떤가 |
| 3판 연속 진행 | 클리어 → 보상 → 전환 → 다음 판이 실제로 이어지는가 |
| 런 종료 | 3판째 클리어 후 종료 화면이 뜨고 **더 진행되지 않는가** |

**목표에 못 닿으면 목표를 내린다.** 벨트 총액과 스폰 간격은 구조에 가깝고, 목표 매출이 조정용 손잡이다. 조정했으면 **조정한 값과 실측치를 함께 보고**한다.

> **기본 대역을 좁힌 것이 스테이지 1 을 어렵게 만들 수 있다.** 기본 손님이 120·130 짜리에서 먹보에게, 250·300 짜리에서 소식에게 밀리기 때문인데 — 스테이지 1 은 명부에 기본밖에 없으므로 **경합 상대가 없다.** 영향은 이론상 0 이어야 하고, 실측이 다르면 그게 발견이다.

### F. 문서

**`.claude/domain/` 에 값의 근거를 남긴다.** `.asset` 파일은 이유를 기록하지 못한다 ([`rules/scriptable-object.md`](../../.claude/rules/scriptable-object.md) §5).

- **새 파일 `customer-kinds.md`** — 세 대역의 배치와 그 이유, 경계값 150·250 에서 폭이 승자를 정한다는 것, "유형은 데이터 차이" 라는 불변식, step-04 가 그 방어선이라는 것
- **`stage-and-run.md` 갱신** — §5(덱의 진실)에 "스테이지 2·3 의 `_spawnTable` 은 비어 있다" 를 명시, §8 에 스테이지 2·3 밸런스 근거와 **실측치**를 추가, "아직 안 정한 것" 에서 해소된 항목(성게 400 · 소식 영입 비용)을 갱신

### 완료 판정

- [ ] `Assets/Level/Balance/` 에 애셋 4개 신규 + 1개 수정
- [ ] `git status` 에 원본 아트·오디오 애셋이 **하나도 없다** (§9 · preflight "에셋 누출")
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 PASS — 특히 "공유 폴더(RULE-02)" 와 ".meta 편집(RULE-03)"
- [ ] `./scripts/run.sh webgl` (또는 프로젝트의 WebGL 빌드 경로) 성공, 빌드 후 **워킹트리가 깨끗**
- [ ] `docs/plan/M4-customer-types.md` 의 완료 판정 10개를 **하나씩 대조**해 결과를 보고

### 예상 커밋 메시지

값과 씬을 나눈다. 밸런스 조정은 되돌릴 일이 잦다.

```
chore(balance): add big eater and stages 02-03
feat(stage): wire the three-stage demo run into the scene
docs(domain): record customer kind band placement
```

---

## 금지 사항

- **표에 없는 밸런스 값을 스스로 정하지 않는다.** 필요한데 없으면 멈추고 묻는다.
- `Assets/Art/` · `Assets/Audio/` · `Assets/Settings/` · `ProjectSettings/` · `Packages/` 에 파일을 만들지 않는다 (RULE-02). 새 애셋은 `Assets/Level/Balance/` 에 둔다
- `.meta` 파일을 손으로 만들거나 고치지 않는다 (RULE-03)
- 스테이지 2·3 의 `_spawnTable` 을 채우지 않는다 (README D6)
- `git merge` · 브랜치 삭제 · PR 머지를 하지 않는다 (§7). PR 생성까지가 에이전트의 범위다
