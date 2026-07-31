# domain/

이 프로젝트만의 **기획·시스템·유기적 관계** 지식이 여기에 쌓인다. 지식 계층의 **2번째 계층(도메인)**.

## 이 폴더에 들어가는 것

- **기획 의도**: 왜 이 시스템이 이런 형태로 설계됐는가 (예: "왜 초밥이 라인 끝으로 흘러가도 실패가 아닌가")
- **시스템 간 관계**: A가 바뀌면 B도 바뀐다, A는 B를 읽지만 B는 A를 모른다 등
- **비즈니스 규칙**: 매출 산정 공식, 초밥 집기 순서(FIFO)와 경합 시 타겟팅 우선순위, 시너지(연어·알 등) 계산, 스테이지 클리어 판정
- **데이터 흐름 / 이벤트 체인**: 한 액션이 어떤 순서로 어떤 시스템을 거치는가 (초밥 스폰 → 손님 스캔 → 집기 → 포화 → 소화 → 매출 반영)
- **용어 정의**: 이 프로젝트에서만 쓰는 단어의 뜻

### 이미 확정된 용어는 여기에 다시 쓰지 않는다

Sushi / Customer / Targeting / Claim / Table / Belt / Deck / Stage / Run 의 정의와 대응 코드 심볼은 `CLAUDE.md` §2 (Ubiquitous Language) 에 있다. 여기엔 **그 용어들이 실제로 어떻게 맞물려 도는지**를 쓴다.

특히 **타겟팅이 우선순위용이지 제약이 아니라는 점**(`CLAUDE.md` §1.1-3a)은 반복해서 잘못 구현되는 지점이다. 관련 판단이 쌓이면 여기 문서로 남긴다.

## 이 폴더에 **들어가지 않는** 것

- Unity/C# 일반 규칙 → `.claude/knowledge/`
- 불변 제약 → `RULES.md`
- 코드 스타일·네임스페이스 규칙 → `CLAUDE.md`
- 코드 전문 붙여넣기 (코드는 소스에, 여긴 **원리**만)

## 파일 구조

시스템 또는 기획 단위로 파일을 분리한다.

```
domain/
├── README.md                  ← 이 파일
├── {system-name}.md           ← 예: belt-spawn-system.md, customer-satiety.md
├── {feature-flow}.md          ← 예: stage-clear-flow.md, deckbuilding-flow.md
└── ...
```

## 각 파일 템플릿

```markdown
# {시스템/기획 이름}

## 한 줄 요약
{한 문장}

## 핵심 타입 / 진입점
- `SushiDefense.{Domain}.{ClassName}` — `Assets/Code/Scripts/{Assembly}/{Domain}/{File}.cs`
- `SushiDefense.{Domain}.{Interface}` — {역할}
- 관련 SO — `SushiData` / `CustomerData` / `StageConfig` 중 해당하는 것

## 유기적 관계
{이 시스템이 어떤 시스템과 연결되는가. 단방향인가 양방향인가}

## 기획 의도
{왜 이 형태로 설계했는가. 어떤 대안을 버렸는가}

## 알려진 제약 / 주의
{수정 시 깨기 쉬운 지점}

## 관련 RULES.md 규칙
{해당되는 RULE-NN이 있다면 명시}

## 밸런스 수치의 근거
{이 시스템의 SO 필드 값이 왜 그 값인지. `.asset` 파일은 이유를 기록하지 못한다}
```

## 갱신 주체

- **에이전트가 `/task-done`에서 자동 제안** → 아키텍트 승인 후 커밋.
- 아키텍트/기획자가 직접 편집도 가능.

새 파일을 추가하면 [`.claude/INDEX.md`](../INDEX.md)의 Level 2 섹션에도 이 파일의 `keywords`를 넣는다. 그래야 다음 세션의 `/task-start`가 이 도메인 지식을 선별 로드할 수 있다.
