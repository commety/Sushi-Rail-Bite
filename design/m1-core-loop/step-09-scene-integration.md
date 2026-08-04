# Step 09: 씬 조립 · 통합 검증 · 문서 갱신

- **영역:** 통합 — 씬/프리팹 + `Presentation` 부트스트랩 (+ `Tests.PlayMode`)
- **선행 단계:** step-01 ~ step-08 전부 완료·머지
- **후행 단계:** M2 착수

---

## 목적

지금까지 만든 것을 **실제로 눈에 보이게** 조립하고, M1 완료 판정 8줄을 전부 통과시킨다. 그리고 이번 마일스톤에서 **확정된 열린 질문 4건을 문서에 반영**한다 — 미결로 남겨 두면 M2 에서 같은 질문을 다시 하게 된다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 수행한다.

### 1) placeholder 프리팹

아트는 M5 다. **색 블록으로 간다.** [`/make-asset`](../../.claude/skills/make-asset) 로 만들거나 프리미티브를 조합한다.

```
Assets/Code/Scripts/Presentation/Belt/SushiItem.prefab        — 생성 (색 블록 + SushiItemView)
Assets/Code/Scripts/Presentation/Customers/Customer.prefab    — 생성 (색 블록 + CustomerView)
```

> **`Assets/Art/` 에 만들지 않는다.** 심링크 폴더라 워크트리 git 이 새 파일을 못 본다 (RULE-02 · [`parallel-work.md`](../../.claude/rules/parallel-work.md) §2). 기능 폴더에 두는 것이 이 프로젝트의 규칙이다.

### 2) 씬 조립

```
Assets/Level/Scenes/Stage01.unity          — 생성 {TODO: verify — 이름을 M1 검증 전용으로 할지 확인}
Assets/Code/Scripts/Presentation/StageBootstrap.cs   — 생성
```

`StageBootstrap : MonoBehaviour` 가 **로직 객체를 만들어 뷰에 물려 주는 유일한 지점**이다:

```csharp
namespace SushiDefense
{
    /// <summary>
    /// 씬 진입점. 순수 로직 객체를 조립해 뷰에 Bind 한다.
    /// 뷰가 로직을 스스로 만들지 않게 하려고 조립을 한곳에 모은다.
    /// </summary>
    public sealed class StageBootstrap : MonoBehaviour
    {
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private SushiBeltView _beltView;
        [SerializeField] private CustomerPlacementController _placement;
        [SerializeField] private TableSlotView[] _slots;

        private void Awake();   // 풀·발급기·벨트·조율자·배치 서비스 생성 → Bind
    }
}
```

- 씬 편집은 **한 번에 한 워크트리만** 한다 (`parallel-work.md` §3). 씬은 텍스트여도 사실상 merge 불가다
- `Assets/Level/Scenes/SampleScene.unity`(M0 빌드 관문용)는 **건드리지 않는다**. 빌드 설정 등록이 필요하면 `ProjectSettings/EditorBuildSettings.asset` 수정이므로 **RULE-06 — 직접 고치지 말고 아키텍트에게 보고**한다

### 3) 통합 검증 (`Tests.PlayMode`)

```
Assets/Tests/PlayMode/StageIntegrationTests.cs   — 생성
```

```
StageIntegrationTests
  Play_SushiSpawnsAndMovesAlongBelt
  Play_SushiReachingEnd_ReturnsToPoolNotDestroyed     ← Destroy 0
  Play_CustomerPlacedOnSlot_ClaimsSushiInReach
  Play_MultipleSushiInReach_ClaimsLowestSequenceFirst
  Play_CustomerPlacedMidStage_StartsClaiming          ← Q4
  Play_NoFrameWithClaimableSushiUnclaimed             ← "구경하지 않는다"
```

마지막 항목은 여러 프레임을 돌리며 *"범위 안에 `OnBelt` 초밥이 있고 손님이 `Idle` 인데 배정이 없는 프레임"* 을 세어 **0** 인지 본다. 플랜이 "테스트로 만들기 어렵다"고 한 항목이라, EditMode 의 `Resolve_CandidatesExist_AlwaysProducesAtLeastOne`(step-05)과 **짝으로** 둔다.

### 4) 런타임 확인

`/qa` 로 Bridge 를 통해 실제 플레이를 관찰한다. 사용자에게 "플레이해 보고 알려 달라"고 요청하지 않는다 — 에이전트가 직접 확인한다.

### 5) 문서 갱신 — **이 단계의 필수 산출물**

| 문서 | 갱신 내용 |
|---|---|
| [`docs/plan/README.md`](../../docs/plan/README.md) §1 | M1 상태 `☐` → `☑` |
| [`docs/plan/README.md`](../../docs/plan/README.md) §5 | Q2·Q3·Q4 를 **해소됨**으로. 결정된 답을 적는다 |
| [`docs/plan/M1-core-loop.md`](../../docs/plan/M1-core-loop.md) | 완료 판정 체크박스 8개 |
| [`.claude/domain/sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §3·§7 | **래치 상한이 결정됐다** (Q9 = 시간 상한). "미결" 표기를 지우고 `StageConfig.RecognitionLatchSeconds` 를 가리킨다 |
| [`.claude/domain/data-model.md`](../../.claude/domain/data-model.md) §3 | `StageConfig` 신규 필드 3개 추가 |

> 특히 `sushi-claim-flow.md` §7 의 래치 상한 미결은 **반드시 지운다.** 남겨 두면 M2 에서 같은 질문을 다시 하게 되고, 그때 다른 답이 나오면 두 구현이 충돌한다.

### 완료 판정 (= M1 완료 판정)

- [ ] 씬을 재생하면 초밥이 시작점에서 나와 라인을 따라 이동한다
- [ ] 끝점 도달 시 **풀 반납** — `grep -rn "Destroy(" Assets/Code/Scripts/ | grep -vE '^[^:]*:[0-9]+:[[:space:]]*(///|//|\*)'` → `SushiPoolBehaviour.cs` 외 0건
- [ ] 손님을 `TableSlot` 에 배치할 수 있다 (진행 중 포함)
- [ ] 손님이 자기 범위의 초밥을 집는다
- [ ] 여러 개면 **순차번호 낮은 것부터** 집는다
- [ ] 범위 안 초밥을 지나보내는 프레임 **0** (PlayMode + EditMode 짝)
- [ ] **탐색이 이벤트 기반** — `grep -rn "foreach.*Customers.*foreach\|for.*customers.*for.*sushi" Assets/Code/Scripts/Presentation/` 로 `Update` 내 이중 순회 없음 확인
- [ ] `./tests/run-tests.sh all` 전량 Green
- [ ] `./tests/preflight.sh` 전 항목 통과
- [ ] **`./scripts/run.sh webgl` 빌드 성공 유지** (M0 관문 회귀 확인)
- [ ] 위 §5 문서 5건 갱신 완료
- [ ] `git status --short ProjectSettings/` → 변경 없음

### 빌드 부산물 주의

WebGL 빌드·PlayMode 실행은 아래를 건드린다. **커밋에 섞지 않는다** (M0 에서 확립):

- `Assets/Settings/*.asset` — URP 셰이더 프리필터 캐시. 되돌린다
- `ProjectSettings/SceneTemplateSettings.json` · 루트 `Data/` — 이미 `.gitignore` 처리됨

### 예상 커밋 메시지

```
feat(stage): assemble M1 verification scene with placeholder prefabs
chore(plan): mark M1 core loop complete
```

씬·프리팹 커밋과 문서 커밋을 나눈다. 씬은 diff 를 읽기 어려워 다른 변경과 섞이면 리뷰가 막힌다.

---

## 금지 사항

- `ProjectSettings/` 를 수정하지 않는다 (RULE-06). 빌드 설정 등록이 필요하면 보고하고 승인을 기다린다.
- `Assets/Art/` 에 프리팹·스프라이트를 만들지 않는다 (RULE-02).
- 아트를 다듬지 않는다. placeholder 색 블록이면 충분하다 — 아트는 M5 다.
- 테스트가 깨졌을 때 `[Ignore]` 로 덮지 않는다.
- `main`/`dev` 에 직접 커밋하지 않는다. `feat/*` → PR → 사람 승인 (§6).
