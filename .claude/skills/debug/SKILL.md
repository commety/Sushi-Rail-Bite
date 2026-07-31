---
name: debug
description: 버그 리포트·예외·오작동을 만났을 때 에이전트가 직접 소스코드를 읽고 추적·재현해 원인을 짚는 툴 사용 지침. 사용자에게 "브레이크포인트 걸어 주세요 / 플레이해 보세요"를 시키지 않는다.
---

# Skill: /debug (Agent-driven Debugging)

**전제:** 디버깅의 책임은 에이전트에게 있다. 사용자에게 Unity Editor 에서 수동으로 브레이크포인트를 걸거나, Play 해서 눈으로 확인하라고 요청하지 않는다. 에이전트가 갖고 있는 툴로 직접 정적 추적(소스 평가) → 필요하면 Bridge 로 재현 → 런타임 값 검증 한다.

이 문서는 **어떤 툴을 어느 순간 쓸지**만 다룬다. 작업 착수·종료 절차는 `/task-start`, `/task-done` 참고.

---

## 1. 툴 매트릭스 — 증상 종류별 1순위 툴

| 증상 유형 | 1순위 (소스 평가) | 2순위 (재현·런타임) | 금지 |
|---|---|---|---|
| `NullReferenceException` / 미할당 참조 | `LSP.findReferences` + `LSP.goToDefinition` 으로 대입 지점 전수 조회 | Bridge `Component.GetField` 로 실제 필드값 확인 | 사용자에게 "인스펙터 열어 확인해 달라" |
| 의도치 않은 값 전이 / 잘못된 상태 | `LSP.incomingCalls` / `outgoingCalls` 로 호출 그래프 추적 | Bridge `Component.GetField` + `/run bridge` 배치 검증 | 사용자에게 "Play 해서 값 보세요" |
| 이벤트 중복 호출 / 구독 누수 | `Grep` 으로 `+=`·`-=` 쌍 전수 매칭 + `LSP.findReferences` | Bridge 로 씬 로드 후 `Component.GetField` 로 델리게이트 체인 조회 | "리스너 한 번 지워 보세요" |
| 물리 버그 / 벨트 이동 이상 | `Grep` 으로 `AddForce`/`MovePosition`/`velocity` 호출 위치 + `RULES.md` RULE-04 확인 | Bridge `EnterPlaymode` + `Component.GetField` 로 벨트 위치·속도 폴링 | "직접 돌려 보세요" |
| 밸런스가 의도와 다름 (너무 빨리 배부름 등) | `Grep` 으로 해당 수치가 **코드 상수인지 SO 필드인지** 먼저 판정 (`CLAUDE.md` §3.1) | Bridge `Component.GetField` 로 런타임에 실제 로드된 SO 값 확인 | 값을 임의로 바꿔 "고쳤다" 고 보고 |
| **손님이 눈앞 초밥을 안 집고 구경함** | 집기 경로에 **가격 비교가 섞였는지** 먼저 본다 — 타겟팅을 게이트로 오용한 것이 1순위 용의자 (`CLAUDE.md` §1.1-3a). 그다음 범위·포화도·상태 순 | Bridge `EnterPlaymode` + 해당 손님의 상태·포화도 폴링 | "밸런스 문제인 것 같다" 로 넘기기 — 이건 기획 위반 버그다 |
| 경합 시 엉뚱한 손님이 가져감 | `SushiClaimResolver` 의 우선순위 산정 + 동률 tie-break 를 `Read` (`CLAUDE.md` §1.1-3a) | 고정 시드로 EditMode 재현이 우선. Bridge 는 최후 | 한 번 관찰하고 "랜덤이라 어쩔 수 없다" 로 종결 |
| 손님 상태가 멈춤 (Eating/Digesting 에서 안 빠져나옴) | 상태 머신 전이 조건을 순수 클래스에서 `Read` + 대응 EditMode 테스트 확인 | Bridge 로 상태 필드 폴링 | "한참 기다려 보세요" |
| 도메인 리로드 / static 상태 오염 | `Grep` 으로 `static` 필드 + `[RuntimeInitializeOnLoadMethod]` 쌍 확인 (RULE-01 / RULES.md RULE-01) | Bridge `EnterPlaymode` → `ExitPlaymode` → `EnterPlaymode` 재진입 시 필드값 비교 | "Play 두 번 눌러 보세요" |
| UI 레이아웃 깨짐 | `LSP.workspaceSymbol` 로 관련 Layout 컴포넌트 심볼 + `Grep` anchor/pivot 설정 | Bridge `Component.GetField` 로 RectTransform 수치 조회 | "씬 열어서 위치 보세요" |
| 컴파일 에러 | `Read` + `LSP.hover` 로 타입 확인 | `/run bridge` 헤드리스 컴파일 (콘솔 로그 그대로 수집) | "콘솔 오류 복붙해 주세요" |

---

## 2. 정적 평가 — LSP 중심

소스를 직접 평가해 원인을 짚는다. **추측 금지**, 호출·정의·참조는 LSP 가 진리다.

### 2-0. LSP 가용성 사전 점검 (필수)

세션 시작 시 LSP 가 실제로 `.cs` 를 처리하는지 먼저 1회 검증. 환경에 따라 Roslyn LSP 가 연결돼 있지 않을 수 있다 (`No LSP server available for file type: .cs` 응답). 연결 안 된 상태로 LSP 경로를 전제하면 진단이 전부 공회전한다.

```
# 아무 .cs 파일로 probe
LSP.documentSymbol filePath=<프로젝트 내 아무 .cs> line=1 character=1
```

- **연결됨** → 아래 LSP 조합을 1순위로.
- **연결 안 됨** → [`/lsp-setup`](lsp-setup.md) 스킬 자동 호출해 세팅 시도. 세팅 중엔 Grep 폴백으로 진행. 사용자가 재시작을 미루면 이번 세션은 Grep 유지.
- 모든 LSP 조합은 Grep 대체 쿼리가 있다 (본 섹션 하단 "Grep 으로 빠르게 훑을 때").

### 자주 쓰는 LSP 조합

```
증상 라인 발견
  → LSP.hover        (그 심볼의 타입·선언 확인)
  → LSP.goToDefinition (정의처 본체 읽기)
  → LSP.findReferences (모든 사용처 = 범인 후보 전수 조회)
  → LSP.incomingCalls  (함수가 버그라면 누가 부르는지)
  → LSP.outgoingCalls  (함수가 의심이라면 내부에서 뭘 부르는지)
```

### 사용 예

- **NRE 위치에서 역추적**: 스택트레이스의 파일·라인 확인 → `LSP.hover` 로 대상 심볼 → `LSP.findReferences` 로 대입처 전수 → 초기화 누락 후보 짚기
- **호출 순서 의심**: `LSP.prepareCallHierarchy` → `incomingCalls` 재귀로 진입 경로 트리
- **인터페이스 구현 확인**: `LSP.goToImplementation` 으로 실제 구현체 식별 (다형성 버그 고정)

### Grep 으로 빠르게 훑을 때

LSP 가 무거울 만한 광범위 패턴은 Grep. **파일 목록만** 뽑아 LSP 로 상세 점검.

```
Grep "static\s+\w+\s+\w+\s*=" --type cs --glob "Assets/Code/Scripts/**"   # static 필드 전수
Grep "\+=\s*.*(?:\.)" --type cs                                       # 이벤트 구독
Grep "AddForce|MovePosition|\.velocity\s*=" --type cs                 # 물리 호출 지점
```

---

## 3. 재현·런타임 검증 — Bridge 중심

정적 평가로 원인 후보가 좁혀졌으면 Bridge 로 **실제 실행해서 값 본다**. 사용자에게 실행을 넘기지 않는다.

### 모드 선택

| 상황 | 사용 |
|---|---|
| Editor 가 이미 열려 있고 사용자가 보고 있음 | `unity_call` 로 계속 쏨 (GUI 상주 모드) |
| Editor 닫혀 있음 / 사용자 자리 비움 | `unity_call` 로 inbox 에 쌓은 뒤 `/run bridge` 헤드리스 배치 |
| 단순 1~2 호출 | `mcp__claude-bridge__unity_call` 직접 |
| 10+ 호출 / 씬 세팅 필요 | inbox 에 쌓고 `unity_batch_flush()` 또는 `/run bridge` |

Bridge 가 준비됐는지 먼저 `mcp__claude-bridge__unity_bridge_status` 로 확인. `editor_running: false` 면 `/run editor` 또는 `/run bridge` (헤드리스).

### 런타임 값 검증 패턴

**필드값 스냅샷**:
```
Component.GetField path="/GameRoot" type="SushiDefense.Belt.SushiBelt" field="_isRotating"
Component.GetField path="/Tables/Table01" type="SushiDefense.Customers.Customer" field="_saturation"
```

**Play 모드 진입 후 폴링**: `EnterPlaymode` 는 void 즉시 반환 — `get_isPlaying` 을 1~2초 간격으로 `True` 될 때까지 폴링 (상한 20초). CLAUDE.md §8 참고.

**도메인 리로드 상태 오염 재현**: `EnterPlaymode` → 값 확인 → `ExitPlaymode` → `EnterPlaymode` 재진입 → 같은 필드 재조회. 두 번째 값이 전 세션 잔재면 `[RuntimeInitializeOnLoadMethod]` 누락 확정.

**재현 최소 케이스 구성**: `Scene.New` → 필요한 GameObject 만 `GameObject.Create` → `Component.Add` → `Component.SetField` 로 재현 조건 세팅 → `EnterPlaymode`. 기존 복잡 씬에서 원인 찾지 말고 격리.

### Reflection.Invoke 탈출구

전용 op 없는 메서드 호출 필요 시 `Reflection.Invoke` 로 런타임 평가. 예: 임의 static 메서드 호출해 상태 덤프.

---

## 4. 재현 테스트로 고정 (TDD)

원인을 특정했으면 **고치기 전에 실패하는 EditMode 테스트를 먼저 쓴다** (`CLAUDE.md` §5). 버그 재현이 순수 로직 수준에서 불가능하면, 그건 로직이 `MonoBehaviour` 안에 갇혀 있다는 신호다 — 로직 분리가 수정의 일부다.

```
1) 버그를 드러내는 EditMode 테스트 작성  → 실패 확인
2) 최소 수정                              → 테스트 Green
3) 리팩터                                 → 전량 여전히 Green
```

이 테스트는 회귀 방지 자산으로 남는다. 수정만 하고 테스트 없이 닫지 않는다.

---

## 5. DAP 런타임 (향후·설정됐을 때)

DAP 툴이 MCP 로 붙어 있으면 **에이전트가 직접** 다음을 수행:

- 브레이크포인트 설정·해제 — 사용자에게 맡기지 않음
- Step in/over/out — 에이전트 판단
- `evaluate` 로 런타임 표현식 평가 (지역변수 포함)
- 브레이크 후 반드시 `continue` 호출 — 미호출 시 에디터가 멈춘 채로 남는다

DAP 가 세팅되지 않은 환경에서는 **Bridge `Component.GetField` + Play 모드 폴링** 이 DAP 대체 경로다. 정적(LSP) → 런타임(Bridge) 로 커버되는 증상은 DAP 없이도 원인 특정 가능. DAP 는 "지역변수 스냅샷 / 스텝 단위 흐름 확인" 이 필요한 소수 케이스에 쓴다.

DAP 사용 후에는 반드시 Code Optimization 을 Release 로 복구한다.

---

## 6. 금지 사항

- **사용자에게 Play·브레이크포인트·인스펙터 확인을 시키지 않는다.** "화면에서 어떻게 보이나요?" 금지. 에이전트가 직접 Bridge 로 조회해 보고한다.
- **컴퓨터 유즈(`mcp__computer-use__*`) 금지.** GUI 좌표 클릭으로 우회하지 말고 Bridge op 로 표현한다. 표현 안 되면 op 추가 제안 (`.claude/knowledge/unity-editor-automation.md`).
- **밸런스 SO 값을 "디버깅" 명목으로 바꾸지 않는다.** 재현을 위해 임시로 바꿨다면 반드시 원복하고, 원복했음을 보고한다 (`CLAUDE.md` §7).
- **추측으로 원인을 단정하지 않는다.** LSP·Grep 으로 전수 확인한 증거가 있어야 원인 보고.
- **정적 평가 없이 Bridge 로 직행하지 않는다.** LSP 로 호출 그래프부터 좁히고, 좁혀진 범위에서만 재현한다. Bridge 왕복은 느리다.
- **한 번의 재현으로 단정하지 않는다.** 간헐 버그는 최소 케이스 재현 + Play 재진입 2회 이상 일치해야 확정.
- **Unity 로그 전문을 대화창에 붙이지 않는다.** `error CS`·stack trace 요약만. `/run` §4 와 동일.

---

## 7. Adragna 디버깅 원칙 (Do/Don't)

소스: P. Adragna, *Software debugging techniques*, CERN School of Computing 2007, pp. 71–86. [원문 PDF](https://cds.cern.ch/record/1100526/files/p71.pdf)

방법론 원칙 10개. **각 규칙은 별도 파일** — 작업 중 해당 상황을 만나면 펼쳐 읽는다. 여기선 인덱스만.

| # | 원칙 | 출처 | 언제 펼칠 것인가 | 파일 |
|---|---|---|---|---|
| 1 | 가정을 먼저 의심하라 | §1.1 | 디버깅 착수 직후 | [01-doubt-assumptions.md](../../knowledge/debugging/01-doubt-assumptions.md) |
| 2 | 버그를 먼저 분류하라 | §1.2 | 증상을 처음 받았을 때 | [02-classify-first.md](../../knowledge/debugging/02-classify-first.md) |
| 3 | 증상과 원인을 혼동하지 마라 | §1.3 | NRE·예외 라인을 원인으로 단정하고 싶을 때 | [03-symptom-vs-cause.md](../../knowledge/debugging/03-symptom-vs-cause.md) |
| 4 | 완전히 이해한 뒤 고쳐라 | §1.3–1.4 | 수정 PR 올리기 직전 | [04-understand-before-fix.md](../../knowledge/debugging/04-understand-before-fix.md) |
| 5 | 버그 저널을 남겨라 | §1.4 | `/task-done` STEP 5 에서 (`.claude/domain/` 승격 판단) | [05-bug-journal.md](../../knowledge/debugging/05-bug-journal.md) |
| 6 | 정적 분석·경고를 최대한 살려라 | §2.1 | 경고 억제가 유혹될 때 | [06-static-analysis.md](../../knowledge/debugging/06-static-analysis.md) |
| 7 | print/Debug.Log 남용 금지 | §2.3 | `Debug.Log` 박으려는 순간 | [07-no-print-debug-abuse.md](../../knowledge/debugging/07-no-print-debug-abuse.md) |
| 8 | 어서션으로 가정을 코드에 박아라 | §2.5 | Awake/진입점·상태 전이 직전 | [08-assertions.md](../../knowledge/debugging/08-assertions.md) |
| 9 | 막히면 관점을 바꿔라 (ACI / 다시 읽기) | §2.6–2.7 | 30분 이상 진전 없을 때 | [09-reframe-when-stuck.md](../../knowledge/debugging/09-reframe-when-stuck.md) |
| 10 | 이진 탐색으로 범위를 쪼개라 | §2.8 | 의심 범위 100줄 이상일 때 | [10-binary-split.md](../../knowledge/debugging/10-binary-split.md) |

> **원저 요지 (Abstract / Introduction):** 디버깅은 "옳다고 믿었던 전제가 실은 틀렸다" 를 확인하는 과정이다. 은총알은 없다. 경험·창의성과 함께 **규율 잡힌 도구 사용** 이 전부다. 이 10 원칙은 그 규율의 최소 집합이다.
