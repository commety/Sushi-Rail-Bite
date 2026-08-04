# M1 — 코어 루프 검증 (원본 1차)

> 원본 목표: *"게임이 시작하면 초밥이 라인 시작점에서 나오고 타워를 지정된 위치에 배치할 수 있다 / 타워는 초밥을 인식하고 처리한다"*

## 목표

**재미의 최소 단위를 눈으로 확인한다.** 초밥이 벨트를 따라 흐르고, 테이블에 앉은 손님이 자기 범위에 들어온 초밥을 집는다.

이 마일스톤에서는 **스탯이 없다.** 가격·포화도·소화 시간은 M2 다. 여기서는 "집는다/안 집는다"만 된다.

## 선행 조건

- M0 완료 (어셈블리·SO 스키마·`SushiPool`·이벤트 채널)

---

## 완료 판정

- [x] 씬을 재생하면 초밥이 벨트 시작점에서 나와 라인을 따라 이동한다 — `Stage01SceneTests`
- [x] 초밥이 라인 끝에 도달하면 **풀로 반납**된다 (`Destroy` 호출 0) — 프로덕션의 `Destroy` 는 `SushiPoolBehaviour` 한 곳뿐
- [x] 손님을 `TableSlot` 에 배치할 수 있다 — **진행 중 배치도 가능** (Q4)
- [x] 손님이 **자기 집기 범위에 들어온 초밥을 집는다**
- [x] 범위 안에 초밥이 여러 개면 **순차번호가 낮은 것부터** 집는다
- [x] 범위 안에 초밥이 있는데 집지 않고 지나보내는 프레임이 **0** 이다 — EditMode·PlayMode 양쪽에 짝으로
- [x] **탐색이 이벤트 기반이다** — 진입 시각을 스폰·배치 때 한 번 계산해 예약하고, 틱마다 기한이 된 것만 꺼낸다
- [x] EditMode 전량 Green + `/run webgl` 빌드 성공 유지 — EditMode 181 / PlayMode 37

> 작업서는 [`design/m1-core-loop/`](../../design/m1-core-loop/README.md). 실행 중 내린 판단(D1~D6)은 그 README 에 있다.
>
> **검증 씬**: [`Assets/Level/Scenes/Stage01.unity`](../../Assets/Level/Scenes/Stage01.unity). Build Settings 에는 **등록하지 않았다** — `ProjectSettings` 수정이라 사람 승인 사항이다 (RULE-06). 등록 없이도 에디터에서 열어 재생할 수 있고, PlayMode 테스트는 경로로 연다.
>
> **밸런스 값은 아직 사람이 정하지 않았다.** `Assets/Level/Balance/*.Placeholder.asset` 3종은 씬이 돌아가는 것을 보기 위한 임시값이다 (§7 — 확인 필요).

---

## 핵심 규칙 — 여기서 결정되는 것

이 마일스톤이 `CLAUDE.md` §1.1-3a~3c 의 **뼈대**를 코드로 굳히는 지점이다. **잘못 만들면 M2·M4 가 전부 그 위에 쌓인다.**

### (1) 자격 판정은 세 조건뿐

```
범위 안에 있는가  ∧  포화도에 여유가 있는가  ∧  집을 수 있는 상태인가
```

**가격은 이 조건에 들어가지 않는다.** M1 에는 가격 자체가 없으니 자연히 지켜지지만, M2 에서 가격이 생길 때 여기에 슬쩍 끼어드는 것이 전형적인 실패다. M1 에서 **자격 / 배정** 을 별도 함수로 분리해 두면 M2 가 안전해진다.

> 손님이 눈앞의 초밥을 두고 구경하는 상태는 **버그**로 취급한다. 성능 문제도, 밸런스 문제도 아니다.

### (2) 배정 구조와 순차번호를 미리 세운다

M1 의 배정 규칙은 **순차번호 순** 하나뿐이다. M2 에서 **타겟팅 우선순위**가 정렬 1순위로 앞에 붙으므로, 지금부터 "쌍 랭킹 → 그리디 확정" 구조를 만들어 두고 정렬 키만 나중에 추가한다. 초밥 순차번호는 **M1 부터 발급한다** — 나중에 넣으면 배정 로직을 다시 짜게 된다.

### (3) 탐색은 이벤트 기반, 인식은 래치 (§1.1-3c)

매 프레임 전체 초밥을 스캔하지 않는다. 범위 **진입 시점**에 후보로 등록한다. 구현은 **결정적 구간 계산 채택** — 벨트가 1차원·등속이라 진입 시각이 계산으로 나오고, 물리 엔진 없이 EditMode 로 전부 검증된다 ([`sushi-claim-flow.md`](../../.claude/domain/sushi-claim-flow.md) §4).

**한 번 인식된 초밥은 범위를 조금 벗어나도 후보에서 빼지 않는다.** 계산 지연(200ms 내외 허용) 때문에 눈앞에서 놓치는 그림이 나오면 안 된다.

---

## 산출물

### `Runtime` — 순수 로직 (테스트 대상)

| 타입 | 책임 |
|---|---|
| `SushiBelt` 로직 | 스폰 타이밍, 위치 진행, 끝점 도달 판정 |
| `CustomerLogic` | **자격 판정**과 **배정**을 별도 메서드로. **`MonoBehaviour` 아님** |
| 후보 집합 | 인식된 초밥을 **래치**하며 증분 갱신하는 자료구조 |
| 범위 진입 이벤트원 | 구간 계산 기반. 매 프레임 스캔 아님 (§1.1-3c) |
| 순차번호 발급기 | 초밥 스폰 시 단조 증가 번호 |

### `Presentation` — 얇은 껍데기

| 타입 | 책임 |
|---|---|
| `Customer : MonoBehaviour` | `CustomerLogic` 에 위임만. 판단 로직 금지 |
| `SushiItem : MonoBehaviour` | 벨트 위 초밥의 시각 표현. 풀이 관리 |
| `TableSlot` | 손님을 놓는 고정 자리 |

### 씬·프리팹

`Assets/Level/Scenes/` 에 M1 검증 씬. 프리팹은 [`/make-asset`](../on-boarding/skills.md#make-asset) 으로 색 블록 placeholder 를 만들어 진행한다. **아트는 M5 다.**

---

## 작업 순서

`/design` 분해 권장. TDD 순서를 지킨다.

1. **자격 판정 + 배정 로직** (`Runtime`) — 실패 테스트 → 구현. 범위·포화 여유·SeqNo 순
2. **벨트 진행 로직** (`Runtime`) — 스폰·이동·끝점 판정
3. **View 연결** (`Presentation`) — `Customer`/`SushiItem` 이 로직에 위임
4. **씬 조립** — placeholder 프리팹 + 테이블 배치 (`/make-asset`, `/run bridge`)
5. **손님 배치 조작** — 지정 위치에 놓기

1~2 는 순수 로직이라 병렬 가능하다. 3 이후는 순차.

## 테스트 항목

| 대상 | 종류 | 테스트 이름 예시 |
|---|---|---|
| 자격 — 범위 안 | EditMode | `CanTake_SushiInReach_ReturnsTrue` |
| 자격 — 범위 밖 | EditMode | `CanTake_SushiOutOfReach_ReturnsFalse` |
| 자격 — 포화 | EditMode | `CanTake_Full_ReturnsFalse` |
| 배정 — SeqNo 순 | EditMode | `Resolve_MultipleSushiInReach_TakesLowestSequence` |
| 배정 — 결정성 | EditMode | `Resolve_SameBoardTwice_ProducesIdenticalResult` |
| 이벤트 — 진입/이탈 | EditMode | `OnEnter_SushiEntersReach_AddsToCandidates` |
| 이벤트 — 먹힘 반영 | EditMode | `OnTaken_SushiEatenByOther_RemovedFromCandidates` |
| 벨트 이동·풀 반납 | PlayMode | 끝점 도달 시 반납, 재사용 확인 |

**"구경하지 않는다"는 테스트로 만들기 어렵다.** 범위 안에 초밥이 있고 여유가 있는데 집기가 실패하는 케이스를 EditMode 로 잡고, 런타임 확인은 `/qa` 의 Bridge 폴링에 맡긴다.

## 열린 질문

이 마일스톤을 착수하기 전에 답이 필요하다.

| # | 질문 | 없으면 |
|---|---|---|
| Q2 | 초밥 스폰 — 시작 시 일괄? 시간에 걸쳐 계속? | 원본 문구대로 "계속 나온다"로 진행 |
| Q3 | 라인 끝 초밥 — 사라지나 순환하나? | 사라지고 풀 반납 |
| Q4 | 손님 배치 — 시작 전에만? 진행 중에도? | 시작 전에만 |

## 리스크

| 리스크 | 대응 |
|---|---|
| 집기 판정이 `MonoBehaviour` 안으로 들어감 | EditMode 테스트를 먼저 쓰면 구조적으로 막힌다 |
| 벨트 이동을 `Update` 에서 매 프레임 할당하며 계산 | WebGL 은 GC 스파이크가 곧 히칭. `Update` 내 `new`/LINQ 금지 |
| 초밥을 `Instantiate`/`Destroy` 로 처리 | §3.4 위반. `SushiPool` 경유. `/qa` 가 잡는다 |
