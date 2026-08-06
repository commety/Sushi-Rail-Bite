# Claude Code Index — 지식·지침·스킬 인덱스

에이전트는 `/task-start`에서 이 파일을 **먼저** 읽고, 작업 주제와 매칭되는 파일만 선별 로드한다.  
모든 지침을 매 세션 통째로 로드하면 토큰 낭비다.

## 읽기 전략

1. **이 파일(`INDEX.md`)만 먼저 읽는다** (수백 토큰).
2. 작업 프롬프트의 키워드와 각 항목의 `keywords` 필드를 매칭한다.
3. 매칭된 파일만 `Read`로 펼친다. 매칭 안 되면 읽지 않는다.
4. `RULES.md`는 **항상 스캔** (불변 제약).
5. `CLAUDE.md`는 **항상 이미 로드되어 있다** (세션 시작 시 주입).

---

## Level 1 — Language & Engine (범용, 최상단)

다른 Unity/C# 프로젝트에도 그대로 적용되는 지식. 프로젝트 도메인보다 상위.

### [knowledge/RULES.md](knowledge/RULES.md) — 항상 스캔 (범용 코딩 강제 규약)
Chris Zimmerman의 21개 프로그래밍 규약 (R1~R21).
- **keywords:** simplicity, abstraction, generalization, optimization, premature optimization, code review, dead code, collapsible code, localize complexity, big-O, convention, naming, root cause, refactor, weed, parallel rework, comment
- **when to read:** **항상** 스캔 (짧고 모든 작업에 적용). 이 파일은 1계층이며, 루트 [`../RULES.md`](../RULES.md)(프로젝트 불변 제약, 3계층)와 **다르다**.

### [knowledge/unity-webgl-performance.md](knowledge/unity-webgl-performance.md)
Unity 성능 최적화. **§0 에 WebGL 타깃 보정표** (DXT 압축·싱글 스레드·초기 로드 크기·IL2CPP 고정)가 있고, 나머지는 원문(모바일 기준)을 그대로 적용.
- **keywords:** profiling, GC, GC.Collect, draw call, batching, dynamic batching, SRP batcher, texture, compression, DXT, ASTC, sprite atlas, pixel art, point filter, mesh, UGUI, canvas, GraphicRaycaster, raycast target, layout group, physics, FixedUpdate, collider, rigidbody, shader, lighting, LOD, audio, animation, animator, coroutine, WaitForSeconds, frame budget, fps, targetFrameRate, vsync, object pool, ScriptableObject, Addressables, overdraw, post-processing, WebGL, 로드 시간, 빌드 크기, 싱글 스레드
- **when to read:** 성능·메모리·프레임 이슈, WebGL 빌드 크기·초기 로드 시간, 스프라이트 임포트 설정, 렌더·UI·오디오 튜닝

### [knowledge/unity-scripting-gotchas.md](knowledge/unity-scripting-gotchas.md)
모델이 자주 틀리는 Unity 스크립팅 함정 3선: 직렬화 (depth=7·null 부활·인라인 복제·SerializeReference·ISerializationCallbackReceiver), 코루틴 중단 조건 (enabled=false 안 멈춤·WaitForSecondsRealtime), IL2CPP Managed Code Stripping (link.xml·[Preserve]). **WebGL 은 IL2CPP 고정이라 §3 이 항상 해당된다.**
- **keywords:** serialization depth, serialization null, ISerializationCallbackReceiver, SerializeReference, Dictionary serialize, inline serialization, coroutine stop, enabled false coroutine, WaitForSecondsRealtime, timeScale pause, IL2CPP, Managed Code Stripping, link.xml, Preserve, MissingMethodException, JsonUtility strip, Activator.CreateInstance, MakeGenericMethod, Reflection.Emit
- **when to read:** `[Serializable]` 필드·ScriptableObject·MonoBehaviour 직렬화 필드 설계, 코루틴으로 타이머/일시정지 구현, iOS·콘솔·WebGL 빌드 실패 (MissingMethod/TypeLoad), 리플렉션·JsonUtility 쓰는 코드 빌드 대비

### [knowledge/csharp-dotnet.md](knowledge/csharp-dotnet.md)
C#/.NET 언어 핵심.
- **keywords:** value type, reference type, struct, class, stack, heap, boxing, unboxing, string, StringBuilder, event, delegate, subscribe, unsubscribe, generic, constraint, Nullable, nullable, null coalescing, equality, Equals, GetHashCode, virtual, override, sealed, async, await, CancellationToken, UniTask, property, field, exception, LINQ, IEnumerable, foreach, interface, List<T>, Dictionary, HashSet, Queue, Stack, ArrayList, Hashtable
- **when to read:** 새 타입 설계, async·이벤트 구현, 자료구조 선택, 박싱/할당 판단, C# 스펙 관련 결정

### [knowledge/qa/](knowledge/qa/) — Global App Testing QA 10원칙 (per-file, 게임 적용 필터 적용)
*The Ultimate QA Testing Handbook* 9장 중 게임 테스트에 유효한 10 규칙만 Do/Don't 로. Crowdtesting·Usability 챕터는 에이전트 QA 와 부적합으로 폐기.
- **keywords:** QA, test case, narrow focus, measurable, test environment, test early, test often, traceability, RTM, requirement, regression, regression script, exploratory, learn design execute, session-based, unit test, integration test, system test, E2E, functional, non-functional, performance, load, reliability, bug report, severity, repro, reproduction, localization, i18n, RTL, TextMeshPro, CultureInfo
- **when to read:** QA 전략 수립·케이스 설계·리포트 작성 시. `/qa` 스킬 §7 인덱스에서 해당 규칙만 펼쳐 본다.

### [knowledge/debugging/](knowledge/debugging/) — Adragna 디버깅 10원칙 (per-file)
P. Adragna, *Software debugging techniques*, CERN School of Computing 2007. 방법론 원칙 10개를 Do/Don't 형식으로.
- **keywords:** debugging, assumption, classify, Bohrbug, Heisenbug, symptom, cause, root cause, fix, bug journal, static analysis, compiler warning, Debug.Log, print debugging, assertion, Assert, rubber duck, ACI, binary split, bisect, stuck, reframe
- **when to read:** 버그·예외·오작동 조우 시. `/debug` 스킬 §7 인덱스에서 해당 규칙만 펼쳐 본다.

### [knowledge/unity-editor-automation.md](knowledge/unity-editor-automation.md)
ClaudeBridge 스택 (C# op + Python MCP + /run + /make-asset 연동) 운용 지침.
- **keywords:** ClaudeBridge, unity_call, unity_batch_flush, bridge-run, editor automation, headless, batchmode, prefab stage, prefab variant, nested prefab, inbox, outbox, Component.SetRectTransform, Prefab.Open, Prefab.CreateVariant, InstantiatePrefab, make-asset, run editor, run bridge
- **when to read:** Unity 씬/프리팹/컴포넌트 조작이 필요할 때, 에이전트가 Editor 작업을 자동 실행하려 할 때, ClaudeBridge op 추가·확장 작업

---

## Level 2 — Project Domain (이 프로젝트 전용)

프로젝트만의 기획·시스템·유기적 관계. `/task-done`이 새 도메인 지식을 여기에 쌓는다.

### [../CLAUDE.md](../CLAUDE.md) — 세션 시작 시 자동 로드됨
게임 컨셉·코어 루프, 용어 사전(Ubiquitous Language), 아키텍처 원칙(SO 데이터 주도·얇은 MonoBehaviour·이벤트 채널·오브젝트 풀·상태 머신·MVP), 어셈블리 분리, 코딩 컨벤션, TDD 전략, Git Flow 3-way handshake, 인간 판단 영역, 커밋 전 체크리스트, 에셋 보호, 디렉터리 구조.
`AGENTS.md` 는 같은 내용의 미러다.
- **keywords:** Sushi, SushiData, SushiItem, Customer, CustomerData, Targeting, TargetingMin, TargetingMax, TargetingPriority, 대역, band, 마감시한, ClaimDeadline, 거리, Claim, SushiClaimResolver, FIFO, FCFS, 경합, tie-break, TableSlot, SushiBelt, SushiDeck, CustomerDeck, StageConfig, StageController, RunState, SushiPool, ISushiConsumer, IScorable, EventChannelSO, SushiDefense, Runtime, Runtime.Data, Presentation, Tests.EditMode, Tests.PlayMode, assembly, asmdef, autoReferenced, namespace, TDD, Unity Test Framework, EditMode, PlayMode, Git Flow, 3-way handshake, feature branch, no-ff, Conventional Commits, 커밋 전 체크리스트, 에셋 유출, Git LFS, WebGL, URP, Unity 6.5
- **when to read:** **항상** (자동 주입)

### [../README.md](../README.md) — 기획 톤·차별점의 1차 출처
게임 컨셉 소개, 핵심 차별점(Leak 없는 디펜스·덱빌딩 시너지·손님 타워 유형·로그라이트 빌드업), 아트/사운드 톤, 개발 로드맵(Phase 1~9).
- **keywords:** 회전초밥, 매출, 덱빌딩, 시너지, 먹보 손님, 소식가, 로그라이트, 픽셀 아트, 데이브 더 다이버, BGM, 로드맵, Phase, MVP
- **when to read:** 기획 의도·톤이 관련될 때 (`/design`, `/make-asset`, `/synth`, UX 판단)

### [domain/](domain/) — 시스템·기획 단위 파일 누적
`/task-done`이 새 지식을 파일로 추가한다. 용어 정의 자체는 `CLAUDE.md` §2 에 있고, 여기엔 **용어들이 실제로 어떻게 맞물려 도는지**를 쌓는다.
- **keywords (동적):** 파일이 생길 때마다 이 인덱스에 `keywords` 추가.
- **when to read:** 해당 시스템/기획의 내부 동작·연관 관계 파악이 필요할 때

#### [domain/sushi-claim-flow.md](domain/sushi-claim-flow.md)
자격 / 배정 / **타이밍** 3갈래. 쌍 기반 그리디 배정(대역 밖 거리 → 고가 → 초밥 SeqNo → **대역 폭** → 손님 SeqNo), **마감시한**(대역 안이면 즉시, 아니면 이탈 직전), 이탈 기준 래치, 이벤트 기반 탐색. **`CLAUDE.md` §1.1-3a·3b·3c 의 구현 지침.**
- **keywords:** 집기, claim, 자격, 배정, 타이밍, eligibility, assignment, timing, FIFO, FCFS, 타겟팅, Targeting, 대역, band, BandDistance, BandWidth, 대역 폭, 마감시한, deadline, ClaimDeadline, ClaimTiming, 유예, 대기, waiting, 굶음, 우선순위, 순차번호, SequenceNumber, SeqNo, tie-break, 동률, 결정적, deterministic, 난수 금지, SushiClaimResolver, ClaimPairComparer, CustomerLogic, 래치, latch, 이탈 시각, 인식, 이벤트 기반, event-driven, OnTriggerEnter2D, 구간 계산, 스캔, 폴링, 구경, reach, 범위
- **when to read:** 손님·벨트·집기·배정 관련 코드를 만지기 직전 (**필수**), "손님이 안 집는다" 증상, 타겟팅·순차번호 관련 판단

#### [domain/data-model.md](domain/data-model.md)
초밥·손님·스테이지·점수/경제·시너지 버프의 보유 데이터. 정적(SO) ↔ 런타임 상태 분리. 🟢 기획 명시 / 🔵 추가 제안 구분.
- **keywords:** 데이터 모델, data model, SushiData, SushiItem, CustomerData, CustomerRuntimeState, StageConfig, 필드, 스탯, 가격, 포화도, 특성, Trait, 집기 범위, 먹는 시간, 소화 시간, 영입 비용, RecruitCost, 최대 배치, 초기 예산, 영입 재화, 점수/10, 경제, 시너지, 버프, buff, 순차번호 카운터
- **when to read:** SO 스키마 설계·수정, 새 필드 추가 판단, 경제·보상 구현, 시너지 설계

#### [domain/stage-and-run.md](domain/stage-and-run.md)
클리어/실패 판정(매출 먼저 → 정각 달성은 클리어), 틱 계약(판정은 조율자 뒤), 판정 후 정지, 런의 수명(`Build()` 바깥 — 재시도에 덱이 남는다), 덱의 진실이 SO 에서 런으로 옮겨 간 것, **난수의 경계**(배정엔 없음/보상만 주입), 스테이지 1 밸런스 근거.
- **keywords:** 스테이지, stage, 클리어, clear, 실패, fail, 판정, outcome, StageOutcome, StageEvaluator, StageClock, StageController, 제한 시간, 남은 시간, 목표 매출, targetRevenue, 경계, 정각, 틱 순서, 정지, 재시도, retry, 런, run, RunState, SushiDeck, CustomerDeck, 덱, 명부, roster, 보상, reward, RewardCatalog, RewardOffer, RewardGenerator, 추첨, 난수, Random, IRandomSource, xorshift, 시드, seed, Fisher-Yates, 영속성, 저장, persistence, MVP, Presenter, RewardSelectionPresenter
- **when to read:** 클리어·실패·재시도·보상 관련 코드를 만지기 직전, 난수를 도입하려 할 때, 스테이지 진행(M4)을 붙일 때

#### [domain/spawn-composition.md](domain/spawn-composition.md)
어떤 초밥이 얼마나 자주 벨트에 오르는가. **share 를 가격에서 유도**(`(덱 내 최저가/가격)^α`)하고 **credit 누적**으로 배출 순서를 만든다. 난수 없음, 가방(인스턴스 재고) 방식 아님.
- **keywords:** 스폰, spawn, 등장 빈도, 출현율, share, 비중, 가중치, weight, 희소성, sparsity, 알파, α, SparsityExponent, 최저가, minPrice, credit, 배출, 수열, 덱, deck, SpawnTable, SushiSpawnEntry, SpawnShareTable, SpawnSequence, 뭉침, 분산, 결정적, 풀 크기, 프리웜, prewarm
- **when to read:** 스폰 빈도·덱 구성을 만지기 직전, "비싼 게 너무 자주/드물게 나온다" 증상, α 튜닝, 새 초밥 종류 추가

---

## Level 3 — Immutable Constraints

### [../RULES.md](../RULES.md) — 항상 스캔
위반 시 시스템이 실제로 망가지는 규칙만. 현재 6개.
- **keywords:** RULE-01, RULE-02, RULE-03, RULE-04, RULE-05, RULE-06, Domain Reload, InitializeOnLoad, symlink, .meta, GUID, FixedUpdate, physics, CancellationToken, ProjectSettings
- **when to read:** 모든 작업 (짧으니 매번 재확인)

> **RULES.md 와 CLAUDE.md §7 의 차이**: RULES.md 는 "어기면 시스템이 망가지는 것"(에디터 정지, GUID 파손). CLAUDE.md §7 은 "사람이 판단해야 하는 것"(머지, 패키지 추가, 밸런스 변경). 둘 다 에이전트를 멈추게 하지만 이유가 다르다 — 전자는 기술적 파손, 후자는 권한.

---

## Level 4 — Path-scoped Rules

특정 경로/파일 타입에 한정된 규칙. 해당 경로 작업 시에만 로드.

### [rules/scripts.md](rules/scripts.md)
`Assets/Code/Scripts/**/*.cs` 작성 규칙 — 폴더↔어셈블리↔네임스페이스 매핑, 로직/MonoBehaviour 분리, 매직 넘버 금지, 풀링, 이벤트 해제, 직렬화·문서화.
- **keywords:** namespace, SushiDefense, static, event, OnDestroy, GetComponent, TryGetComponent, FindObjectOfType, async, cache, SerializeField, XML doc, 매직 넘버, SushiPool, Instantiate, MVP, Presenter, View, 집기, Targeting, 타겟팅, FIFO, 경합, claim
- **when to read:** .cs 파일 생성/수정

### [rules/scriptable-object.md](rules/scriptable-object.md)
`Runtime.Data` SO 정의와 밸런스 `.asset` 편집 규칙 — 정적 데이터/런타임 상태 분리, 범위 제약, 밸런스 수정은 사람 승인.
- **keywords:** ScriptableObject, SushiData, CustomerData, StageConfig, EventChannelSO, CreateAssetMenu, 밸런스, balance, 가격, Targeting, 타겟팅, 포화도, 소화 시간, 목표 매출, .asset, 데이터 주도
- **when to read:** SO 타입 신설·수정, 밸런스 수치를 어디에 둘지 판단할 때

### [rules/tests.md](rules/tests.md)
`Assets/Tests/**` 작성 규칙 — TDD 기본값, EditMode/PlayMode 분담, 네이밍, SO 테스트 처리, 커밋 게이트.
- **keywords:** TDD, test, Unity Test Framework, UTF, NUnit, EditMode, PlayMode, Assert, Arrange Act Assert, stub, fake, NSubstitute, 커밋 게이트, runTests, batchmode
- **when to read:** 테스트 작성·수정, 새 로직 착수 직전(테스트 먼저)

### [rules/asmdef.md](rules/asmdef.md)
`.asmdef` 파일 규칙 — 6개 어셈블리 구성, 단방향 의존, 테스트 어셈블리 설정.
- **keywords:** asmdef, assembly definition, autoReferenced, defineConstraints, dependency, Runtime, Runtime.Data, Presentation, Tests.EditMode, Tests.PlayMode, UNITY_INCLUDE_TESTS, nunit
- **when to read:** .asmdef 생성/수정, 어셈블리 간 참조를 추가하려 할 때

### [rules/parallel-work.md](rules/parallel-work.md)
워크트리 병렬 작업 중 에셋·프리팹·파일 배치 규칙.
- **keywords:** worktree, parallel, prefab placement, feature folder, symlink, add vs modify, multi-editor, 씬 충돌
- **when to read:** 워크트리에서 작업 중, 새 프리팹·머티리얼·텍스처 생성 직전

---

## Level 5 — Skills (on-demand)

에이전트가 `/name`으로 호출하는 공정. **사용자 명령을 기다리지 말고 적절한 타이밍에 에이전트가 선제적으로 호출해도 되는 스킬은 "자동 호출 가능" 표시.**

| Skill | 역할 | 호출 타이밍 | 자동 호출 |
|---|---|---|---|
| [setup](skills/setup/SKILL.md) | 프로젝트 최초 세팅 — OS 감지 → `uv` 확인 → `.mcp.json` 확인 → Unity Editor 감지 → Bridge MCP 연결 테스트. 맥·윈도우·리눅스 모두 지원 | 저장소 첫 클론 시, 다른 컴퓨터로 옮겼을 때 | — 사용자가 `/setup` 으로 호출 |
| [task-start](skills/task-start/SKILL.md) | 작업 착수 브리핑 (이 인덱스 활용) | 모든 작업 시작 | ✓ |
| [task-done](skills/task-done/SKILL.md) | 작업 마무리 + 도메인 지식 승격 | 모든 작업 완료 | ✓ |
| [self-update](skills/self-update/SKILL.md) | 세션 지식을 5개 계층에 승격 제안 | 새 패턴 발견 시 | — |
| [design](skills/design/SKILL.md) | 기획 → 단계별 에이전트 프롬프트 (TDD 순서로 분해: SO → 실패 테스트 → 로직 → View) | 새 기획 받았을 때 | — |
| [run](skills/run/SKILL.md) | Unity 빌드(**타깃은 `webgl`**) / Editor 실행(`editor`) / ClaudeBridge 헤드리스(`bridge`) | 검증·실행·Editor 구동 필요 시 | **✓** — 빌드 확인, Editor 띄워야 할 때, inbox 커맨드 flush가 필요할 때 에이전트 판단으로 호출 |
| [make-asset](skills/make-asset/SKILL.md) | Unity 어셋 제작: UGUI 프리팹 / 파티클 / 프리미티브 모델 / **SVG 직접 그려 PNG 래스터화한 아이콘 스프라이트** / 사용자 제공 이미지 임포트 | 참조할 어셋이 Assets/ 아래 없는데 필요할 때 | **✓** — 씬 조립 중 missing prefab 발견, 프로토타이핑 첫 어셋 필요 시, 심볼/아이콘(초밥 접시·동전·별 등)이 요구될 때 SVG로 즉석 생성 |
| [debug](skills/debug/SKILL.md) | 에이전트 주도 디버깅 툴 사용 지침: LSP 로 소스 평가 → Bridge 로 런타임 재현·검증 (DAP 대체 포함) | 버그·예외·오작동 증상을 받은 직후 | — 사용자가 `/debug` 로 호출 (자동 호출 아님; 워크플로우는 별도 규약) |
| [qa](skills/qa/SKILL.md) | 에이전트 주도 QA 툴 사용 지침: 기준 문서화 → LSP/Grep 정적 평가 → Bridge 런타임 확인 → 기준별 Pass/Fail 보고 | 기능·규칙 준수 여부 평가가 필요할 때 | — 사용자가 `/qa` 로 호출 (자동 호출 아님; 워크플로우는 별도 규약) |
| [lsp-setup](skills/lsp-setup/SKILL.md) | Claude Code 의 LSP 툴이 `.cs` 를 인식하도록 OmniSharp 자동 설치·설정. 프로그래밍 모르는 사용자 대상 에이전트 주도 세팅. | `No LSP server available for file type: .cs` 응답 받은 직후 | **✓** — `/debug`·`/qa` 가 LSP 미연결 탐지하면 자동 호출 |
| [synth](skills/synth/SKILL.md) | sin/cos 가산합성 신디사이저. 피아노·패드·리드·베이스·앰비언트 프리셋 + ADSR·비브라토·디튠·bitcrush 로 NES 풍 WAV 생성. Python stdlib 만. | 효과음·BGM·placeholder 오디오가 필요한데 `AudioClip` 이 비어 있을 때 | **✓** — UI SE 공란, 초밥 집기/포화/매출 획득 피드백 부재, 타이틀 BGM 필요 시 에이전트 판단으로 호출 |

**자동 호출의 의미**: `/run editor`·`/run bridge`·`/make-asset`은 사용자가 입력한 적 없어도 에이전트가 판단해서 호출 가능. 호출 전에 한 줄로 사용자에게 알리되, 무응답 시 합리적 기본값으로 진행하고 나중에 되돌릴 수 있게 기록을 남긴다.

---

## Level 6 — Sub-agents (작업 분업)

`/task-start` ~ `/task-done` 파이프라인을 **리서치 ↔ 구현**으로 분리한 서브 에이전트. Agent 툴의 `subagent_type` 으로 호출한다.

| Sub-agent | 역할 | 호출 타이밍 |
|---|---|---|
| [agents/task-researcher.md](agents/task-researcher.md) | `/task-start` 착수 직후 CLAUDE.md·INDEX.md·RULES.md 스캔 + grep/LSP 로 증거 수집 → **구현 설계서** 산출. 코드 수정 금지. | 모든 작업 착수 시 선행 |
| [agents/task-engineer.md](agents/task-engineer.md) | 리서처의 설계서를 **임의 판단 없이** 그대로 실행. 범위 밖 변경 금지. 위반 신호 시 즉시 멈춤. | 설계서 완성 후 구현·검증 |

설계서가 불완전하면 엔지니어를 부르지 말고 리서처로 되돌린다. 엔지니어는 스스로 설계서를 재해석하지 않는다.

---

## 인덱스 갱신 규칙

- 새 지식/규칙/스킬/도메인 파일을 추가하면 **이 인덱스도 함께 갱신**한다.
- `keywords`는 작업 프롬프트에 실제로 나올 단어를 고른다. 너무 일반적(`game`, `code`)이면 매칭 품질이 떨어져 결국 전부 로드하게 된다.
- 각 항목 설명은 한 줄. 상세는 링크로.
- 이 파일은 짧게 유지한다. 200줄 넘어가면 섹션 분리를 고려한다.
