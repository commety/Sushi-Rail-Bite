# M0 — 기반 구조

> 원본에 없던 신설 마일스톤. 근거는 [README §3-(1)](README.md#3-원본-대비-변경한-것--반드시-확인-필요).

## 목표

게임 로직을 놓을 자리를 만든다. **게임플레이는 아직 없다.** 어셈블리·데이터 계약·풀·이벤트 채널·빌드 경로가 서면 끝이다.

## 왜 먼저인가

현재 `Assets/Code/Scripts/` 는 비어 있고 `.asmdef` 가 하나도 없다. 이 상태에서 M1 을 시작하면 코드가 어셈블리 밖에 흩어지고, 나중에 옮기면서 네임스페이스·참조·테스트를 전부 다시 손대야 한다.

또 **어셈블리 생성은 `CLAUDE.md` §7 승인 사항**이라 에이전트가 임의로 못 한다. 여기서 한 번에 승인받고 넘어간다.

## 선행 조건

- `/setup` 완료 (Bridge MCP 응답 확인)
- **아키텍트 승인**: 아래 6개 `.asmdef` 생성 (§7)

---

## 완료 판정

- [ ] 6개 어셈블리가 생성되고 의존 방향이 단방향이다
- [ ] `Runtime.Data` 에 SO 3종 스키마가 있고, 각각 데이터 검증 EditMode 테스트가 붙어 있다
- [ ] `SushiPool` 이 있고 재사용을 검증하는 PlayMode 테스트가 통과한다
- [ ] 이벤트 채널 SO 1종이 있고 발행→구독이 EditMode 로 검증된다
- [ ] `Unity -batchmode -runTests -testPlatform EditMode` 전량 Green
- [ ] **`/run webgl` 로 빈 씬 빌드가 성공한다** (M0 의 핵심 관문)
- [ ] `./scripts/bridge-run.sh` 로 컴파일 에러 0

---

## 산출물

### 어셈블리 (§7 승인 필요)

```
Assets/Code/Scripts/Runtime/          → Runtime.asmdef
Assets/Code/Scripts/Runtime.Data/     → Runtime.Data.asmdef
Assets/Code/Scripts/Presentation/     → Presentation.asmdef
Assets/Code/Scripts/Editor/           → Editor.asmdef
Assets/Tests/EditMode/                → Tests.EditMode.asmdef
Assets/Tests/PlayMode/                → Tests.PlayMode.asmdef
```

의존 방향과 테스트 어셈블리 필수 설정은 [`.claude/rules/asmdef.md`](../../.claude/rules/asmdef.md) 를 따른다. `autoReferenced: false` 는 RULE-01 이라 반드시 유지.

### SO 스키마 (`Runtime.Data`)

**필드 이름만 정한다. 값은 M2·M3 에서 사람이 정한다.**

| 타입 | 필드 |
|---|---|
| `SushiData` | 가격, 포화도 기여량, 특성(Trait), 스프라이트 |
| `CustomerData` | 집기 범위, **타겟팅(단일 가격 값)**, 먹는 시간, 최대 포화도, 소화 시간, 영입 비용 |
| `StageConfig` | 목표 매출, 제한 시간, 최대 배치 손님 수, 초기 영입 예산, 테이블 배치, 보너스 목표 |

> 타겟팅 필드 이름을 `MaxEatablePrice` 처럼 "먹을 수 있는" 뉘앙스로 짓지 않는다. 다음 사람이 반드시 게이트로 쓴다. → [`.claude/rules/scriptable-object.md`](../../.claude/rules/scriptable-object.md) §7

### 런타임 기반 (`Runtime`)

- `SushiPool` — 초밥 인스턴스 생성/반납 (§3.4). M1 의 벨트가 이걸 쓴다
- `SushiEatenEventChannelSO` — 벨트/손님 ↔ 점수/UI 결합을 끊는 첫 채널 (§3.3)
- 런타임 상태 컨테이너 — `CustomerRuntimeState` 등. **SO 와 분리** (§3.1)

### 검증 씬

`Assets/Level/Scenes/` 에 WebGL 빌드 확인용 최소 씬 1개. Build Settings 에 등록한다.

---

## 작업 순서

`/design` 으로 쪼갤 때의 권장 분해. 한 단계 = 한 어셈블리.

1. **어셈블리 골격** — `.asmdef` 6개 + 의존 설정 *(승인 후)*
2. **SO 스키마** — `Runtime.Data` 3종 + 데이터 검증 테스트
3. **이벤트 채널** — `~EventChannelSO` 1종 + 발행/구독 테스트
4. **오브젝트 풀** — `SushiPool` + 재사용 PlayMode 테스트
5. **빌드 관문** — 최소 씬 + `/run webgl` 성공 확인

1번이 끝나야 2~4번이 병렬 가능하다. 5번은 마지막.

## 테스트 항목

| 대상 | 종류 | 확인 |
|---|---|---|
| SO 데이터 검증 | EditMode | 음수 가격 거부, 타겟팅 > 0, 소화 시간 > 0 |
| 이벤트 채널 | EditMode | 발행 시 구독자 호출, 구독 해제 후 미호출 |
| `SushiPool` | PlayMode | 반납한 인스턴스가 재사용됨, 새로 `Instantiate` 되지 않음 |

SO 는 테스트 안에서 `ScriptableObject.CreateInstance<T>()` 로 만든다. 디스크의 밸런스 애셋을 로드하지 않는다 → [`.claude/rules/tests.md`](../../.claude/rules/tests.md) §4

## 리스크

| 리스크 | 대응 |
|---|---|
| WebGL 빌드가 여기서 실패 | **오히려 이게 M0 의 목적이다.** 로직 쌓기 전에 발견하는 게 훨씬 싸다 |
| 어셈블리 구성을 나중에 바꾸고 싶어짐 | 지금 `.claude/rules/asmdef.md` 대로 가고, 바꿔야 하면 §7 로 다시 승인받는다 |
| SO 필드를 나중에 추가하게 됨 | 정상이다. 필드 추가는 싸다. 값을 코드에 박는 것이 비싸다 |

## 열린 질문

- Q1(저장/영속) 이 M0 의 SO 설계에 영향을 주지는 않는다. M3 에서 답해도 된다.
