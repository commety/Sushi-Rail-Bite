# M0 — 기반 구조 작업서

## 한 줄 요약

게임플레이 코드를 놓을 자리(어셈블리 6개 · SO 스키마 3종 · 이벤트 채널 · 런타임 상태 컨테이너 · 초밥 풀)를 세우고, 빈 씬 WebGL 빌드가 통과하는 것까지 확인한다.

## 원문

[`docs/plan/M0-foundation.md`](../../docs/plan/M0-foundation.md) — 원본에 없던 신설 마일스톤. 근거는 [`docs/plan/README.md` §3-(1)](../../docs/plan/README.md).

> **게임플레이는 아직 없다.** 벨트가 돌지 않고 손님이 먹지 않는다. 그건 M1 이다.

---

## 아키텍처 결정

### D1. "실패하는 테스트"의 정의를 Unity 에 맞게 조정한다

Unity 는 어셈블리 단위 컴파일이라 **존재하지 않는 타입을 참조하는 테스트는 컴파일 자체가 실패**하고, 그러면 스위트 전체가 안 돈다 (`./tests/run-tests.sh` 종료 코드 2). 그래서 이 작업서의 TDD 는 **단계를 쪼개는 방식이 아니라 한 단계 안의 순서**로 지킨다:

1. 타입·시그니처만 만든다 (본문은 `throw new NotImplementedException()` 또는 빈 구현)
2. 테스트를 쓰고 **Red 를 눈으로 확인**한다
3. 최소 구현으로 Green

각 step 문서의 "실행 순서" 절이 이 순서를 명시한다. 컴파일 에러를 Red 로 세지 않는다.

### D2. 이벤트 채널의 페이로드는 `Runtime.Data` 안에서 닫는다

`SushiEatenEventChannelSO` 가 `SushiItem`(`Runtime`) 을 인자로 받으면 `Runtime.Data → Runtime` 역방향 의존이 생겨 어셈블리 그래프가 깨진다. 페이로드는 **`Runtime.Data` 에 정의한 값 타입 `SushiEatenPayload`** (가격·포화도·초밥 SeqNo·손님 SeqNo) 로 닫는다. 참조 타입을 실어 나르지 않는다.

### D3. `SushiPool` 을 순수 로직과 Unity 생성으로 쪼갠다

M0 원문은 `SushiPool` 을 `Runtime` 에 둔다. 그런데 실제 인스턴스 생성은 `Object.Instantiate` 이고, 이건 `MonoBehaviour`/Unity 오브젝트 영역이다. 한 클래스에 두면 EditMode 테스트가 불가능해진다 (`CLAUDE.md` §3.2).

- `Runtime` — `SushiPool<T>` 순수 대여/반납 로직. 생성은 주입된 `ISushiInstanceFactory` 에 위임. EditMode 로 "반납분 재사용 · 생성 호출 횟수" 검증
- `Presentation` — `SushiPoolBehaviour : MonoBehaviour` 가 `ISushiInstanceFactory` 를 구현해 실제 `Instantiate`. PlayMode 로 실물 재사용 검증

프로덕션 코드에서 `Instantiate`/`Destroy` 를 직접 부르는 곳은 **이 팩토리 한 곳뿐**이 된다 (§3.4).

### D4. `.asmdef` 6개는 사람 승인 후에만 만든다

`CLAUDE.md` §7 · RULE-01. step-01 은 **승인 게이트**이고, 승인 전에는 step-02 이후를 시작할 수 없다.

### D5. `ProjectSettings` 는 건드리지 않는다

`Assets/Level/Scenes/SampleScene.unity` 가 이미 `EditorBuildSettings.asset` 에 등록돼 있다 (확인함). M0 의 빌드 관문은 **새 씬을 만들지 않고 이 씬을 그대로 쓴다.** RULE-06 회피이자, 필요 없는 씬을 늘리지 않기 위함.

---

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| — | *(신규 6개 `.asmdef`)* | 골격. step-01, 승인 사항 |
| data | `Runtime.Data` | `SushiData` · `CustomerData` · `StageConfig` · `SushiEatenEventChannelSO` · `SushiEatenPayload` |
| runtime | `Runtime` | `SushiItem` · `CustomerRuntimeState` · `SequenceNumberIssuer` · `SushiPool<T>` · `ISushiInstanceFactory` |
| presentation | `Presentation` | `SushiPoolBehaviour` (유일한 `Instantiate` 지점) |
| tests | `Tests.EditMode` / `Tests.PlayMode` | SO 검증 · 채널 발행/구독 · 상태 컨테이너 · 풀 재사용 |
| editor | `Editor` | M0 에서는 **비어 있다.** `.asmdef` 만 세운다 |

## 의존성 그래프

```
Runtime.Data  ←  Runtime  ←  Presentation
      ↑             ↑             ↑
      └── Tests.EditMode / Tests.PlayMode ──┘

SushiDefense.Data.SushiData
   → SushiDefense.Belt.SushiItem            (런타임 상태, SO 참조 보유)
   → SushiDefense.Belt.SushiPool<T>         (ISushiInstanceFactory 주입)
   → SushiDefense.Belt.SushiPoolBehaviour   (Presentation, Instantiate 담당)

SushiDefense.Data.SushiEatenPayload
   → SushiDefense.Data.SushiEatenEventChannelSO   (Runtime.Data 안에서 닫힘, D2)
```

## 새 밸런스 수치

**M0 은 값을 하나도 정하지 않는다.** 필드 이름과 제약(`[Min(0)]` 등)만 세운다 — 값은 M2·M3 에서 사람이 정한다 (`CLAUDE.md` §3.1 · §7).

| 필드 | 들어갈 SO | 값 |
|---|---|---|
| 가격 · 포화도 기여량 · Trait · Id · 표시 이름 · 스프라이트 | `SushiData` | **미정 (필드만)** |
| 집기 범위 · **타겟팅 가격** · 먹는 시간 · 최대 포화도 · 소화 시간 · 영입 비용 · 유형 · Id · 이름 · 스프라이트 | `CustomerData` | **미정 (필드만)** |
| 목표 매출 · 제한 시간 · 최대 배치 손님 수 · 초기 영입 예산 · 테이블 배치 · 벨트 속도 · 스폰 구성 · 보너스 목표 · 번호 · 이름 | `StageConfig` | **미정 (필드만)** |

> 타겟팅 필드는 `TargetingPrice`. `MaxEatablePrice` 류의 "먹을 수 있는" 뉘앙스 금지 — 다음 사람이 반드시 자격 게이트로 쓴다 (`.claude/rules/scriptable-object.md` §7).

## 단계

| # | 파일 | 영역 | 내용 |
|---|---|---|---|
| 01 | [step-01-editor-assembly-skeleton.md](step-01-editor-assembly-skeleton.md) | 골격 | `.asmdef` 6개 **(사람 승인 필수)** |
| 02 | [step-02-data-so-schemas.md](step-02-data-so-schemas.md) | data | SO 3종 스키마 + 데이터 검증 EditMode 테스트 |
| 03 | [step-03-data-event-channel.md](step-03-data-event-channel.md) | data | `SushiEatenEventChannelSO` + 발행/구독 테스트 |
| 04 | [step-04-runtime-state-containers.md](step-04-runtime-state-containers.md) | runtime | `SushiItem` · `CustomerRuntimeState` · `SequenceNumberIssuer` + 테스트 |
| 05 | [step-05-runtime-sushi-pool.md](step-05-runtime-sushi-pool.md) | runtime | `SushiPool<T>` · `ISushiInstanceFactory` + 재사용 EditMode 테스트 |
| 06 | [step-06-presentation-pool-behaviour.md](step-06-presentation-pool-behaviour.md) | presentation | `SushiPoolBehaviour` + PlayMode 재사용 테스트 |
| 07 | [step-07-build-gate-webgl.md](step-07-build-gate-webgl.md) | 관문 | `preflight` + `/run webgl` 성공 확인 |

## 병렬 실행 가능성

```
step-01 (승인 게이트)
   ├─→ step-02 ─┐
   ├─→ step-03 ─┤        (셋 다 서로 독립 — 다른 파일)
   └─→ step-04 ─┤
       step-05 ─┘→ step-06 → step-07
```

- **step-01 이 끝나기 전에는 아무것도 병렬로 못 간다.** 어셈블리가 없으면 파일을 놓을 자리가 없다
- step-02 · step-03 · step-04 · step-05 는 **동시 실행 가능**. 생성 파일이 겹치지 않는다
- step-06 은 step-05 의 `ISushiInstanceFactory` 시그니처가 확정돼야 시작
- step-07 은 전부 머지된 뒤 마지막

워크트리를 나눠 병렬로 돌린다면 `.claude/rules/parallel-work.md` §2 를 지킨다 — **새 파일은 전부 기능 폴더 안**이라 심링크 문제는 없다.

## 이 작업서가 답을 미루는 것 (열린 질문)

| # | 질문 | M0 영향 | 이 작업서의 처리 |
|---|---|---|---|
| Q8 | 순차번호 범위 — 스테이지 리셋 / 런 연속 | step-04 `SequenceNumberIssuer` | **리셋 가능한 인스턴스**로 만든다. 어느 시점에 리셋할지는 M2 에서 결정 — 설계를 양쪽 다 수용하게 열어 둔다 |
| Q2 · Q3 | 스폰 타이밍 · 라인 끝 처리 | step-05 풀 반납 시점 | 풀은 "언제 반납하나"를 모른다. 호출자가 정한다 (M1) |
| Q1 | 저장/영속 | 없음 | M3 에서 답해도 된다 (M0 원문 §열린 질문) |

`StageConfig` 의 필드는 지금 다 세워도 값이 비어 있어 무해하다. **필드 추가는 싸고, 값을 코드에 박는 것이 비싸다** (M0 §리스크).
