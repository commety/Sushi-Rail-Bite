# Step 06: chiptune BGM · SFX 제작 + `AudioBank.asset`

- **영역:** `assets` — 어셈블리 없음 (애셋 제작)
- **선행 단계:** step-01 (`AudioBankSO` 타입이 있어야 `.asset` 을 만들 수 있다)
- **후행 단계:** step-07 의 `AudioDirector` 가 이 뱅크를 읽는다

---

## 목적

저장소에 오디오 파일이 **하나도 없다.** `Assets/Audio/Music/` 과 `Assets/Audio/Sound/` 는 `.gitkeep` 뿐이다.

`/synth` 스킬로 BGM 1곡과 효과음 6종을 만들고, `AudioBank.asset` 에 물려 step-07 이 바로 쓸 수 있게 한다. 외부 의존성 없이(Python 3 stdlib) M5 안에서 끝난다.

**임시물이다.** 원본 음원이 들어오면 같은 경로에 같은 이름으로 덮으면 되고, 코드 변경은 0 이어야 한다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출한 뒤 `/synth` 로 진행한다.

### 생성 파일

```
Assets/Audio/Music/
  stage-bgm.wav              스테이지 BGM (루프)

Assets/Audio/Sound/
  sfx-sushi-eaten.wav        초밥이 먹혔다      ← 가장 자주 난다
  sfx-customer-placed.wav    손님을 앉혔다
  sfx-reward-picked.wav      보상을 골랐다
  sfx-stage-advanced.wav     다음 스테이지로
  sfx-stage-cleared.wav      클리어
  sfx-stage-failed.wav       실패

Assets/Level/Balance/AudioBank.asset
```

> **`Assets/Audio/` 는 워크트리에서 심링크다.** 이 단계는 **메인 프로젝트에서만** 실행한다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2).
>
> `AudioBank.asset` 은 `Assets/Level/Balance/` 로 간다 — 다른 SO 애셋(`Stage01`, `RewardCatalog`, `Run.Demo`)이 전부 거기 있고, 그 폴더는 심링크가 아니다.

### 제작 지침

`/synth` 의 프리셋과 레시피는 [SKILL.md](../../.claude/skills/synth/SKILL.md) §1·§2 참조.

| 파일 | 성격 | 출발점 |
|---|---|---|
| `sfx-sushi-eaten` | **짧고 작게.** 초당 여러 번 난다 — 여기서 화려하면 귀가 지친다 | `lead`, 0.08s 내외, 단음 |
| `sfx-customer-placed` | 확인감 있는 두 음 | `piano`, 짧은 상승 |
| `sfx-reward-picked` | 보상감 — 조금 밝게 | `lead` 아르페지오 상승 |
| `sfx-stage-advanced` | 전환 | `piano` 상승 3음 |
| `sfx-stage-cleared` | 가장 화려하게. 스테이지에 한 번뿐이다 | `lead` + `bass` 레이어 |
| `sfx-stage-failed` | 하강 | `bass` 하강 2음 |
| `stage-bgm` | 캐주얼·개그 톤. **루프 이음매가 튀지 않아야 한다** | `lead` + `bass` 레이어, `--bitcrush 5` |

**길이 예산을 지킨다.** BGM 이 길수록 WebGL 초기 로드가 늘어난다. 30~40초 루프면 데모에 충분하다 — 3분짜리 곡은 이 마일스톤의 문제가 아니다.

### WebGL 임포트 설정 — 놓치기 쉬운 지점

Unity WebGL 의 오디오는 다른 플랫폼과 다르게 동작한다:

| 항목 | SFX | BGM | 이유 |
|---|---|---|---|
| Load Type | `Decompress On Load` | `Compressed In Memory` | **WebGL 은 `Streaming` 을 지원하지 않는다.** 지정해도 조용히 다른 모드로 떨어진다 |
| Compression Format | `PCM` 또는 `Vorbis` | `Vorbis` | 짧은 SFX 는 압축 이득이 작고 디코딩 지연만 는다 |
| Force To Mono | 켠다 | 켠다 | 2D 게임이고 스테레오 정보가 없다. 용량 절반 |
| Preload Audio Data | 켠다 | 켠다 | 첫 재생에서 끊기지 않게 |

이 설정은 임포터가 파일별로 들고 있다. **`.meta` 를 직접 쓰지 않는다** (RULE-03) — Editor 를 통해 설정하거나, `AudioImporter` 를 만지는 것이 필요하면 별도 승인 항목으로 올린다.

### `AudioBank.asset` 만들기 — 2-pass

`ScriptableObject` 애셋을 만드는 ClaudeBridge op 은 **없다** (`unity-editor-automation.md` «한계/함정»). 절차:

1. WAV 7개를 먼저 디스크에 쓰고 Unity 를 한 번 띄워 임포트 → `.meta` 가 생성된다
2. 생성된 `.meta` 에서 각 클립의 GUID 를 **읽는다**
3. `AudioBank.asset` YAML 을 손으로 쓴다. `m_Script` 의 GUID 는 `AudioBankSO.cs.meta` 에서 베낀다
4. 다시 임포트해 `.meta` 가 생성되게 한다

### 선행 산출물 의존성

- step-01 의 `AudioBankSO` · `AudioCue` 타입

### 밸런스 수치 — §7 승인 요청

**값을 확정하는 것은 사람의 판단 영역이다** (`CLAUDE.md` §7). 아래를 제안으로 제시하고 승인을 기다린다.

| 필드 | 제안값 | 근거 |
|---|---|---|
| `SushiEaten.CooldownSeconds` | `0.06` | 60fps 기준 3~4프레임. 개별 타격감은 남기고 뭉개짐은 없앤다 |
| 나머지 SFX 쿨다운 | `0` | 연달아 날 일이 없다 |
| `SushiEaten.Volume` | `0.5` | 가장 자주 난다 — 가장 작아야 한다 |
| 결과음 `Volume` | `0.8` | 스테이지에 한 번뿐이다 |
| 나머지 `Volume` | `0.7` | |
| `Bgm.Volume` | `0.35` | SFX 를 덮지 않는 선. **덕킹 대신 고정 비율로 시작한다** — 덕킹은 실측 후에 필요하면 |
| `MaxConcurrentSfx` | `6` | 손님 4명 × 먹힘 + 배치/보상이 겹치는 최악 순간 |

**이 값들은 M9 밸런싱에서 다시 만진다.** 지금은 "말이 되는 출발점" 이면 된다. 바꾼 이유는 `.claude/domain/` 에 남긴다 — `.asset` 파일은 이유를 기록하지 못한다.

### §9 원본 자산 보호

- 이 단계가 만드는 WAV 는 전부 프로그램 생성물이라 유출 위험이 없다
- **사람이 만든 음원이 이미 같은 경로에 있으면 덮지 않는다.** 확인 후 진행
- 외부에 오디오를 올릴 일이 생기면 사람 확인을 먼저 받는다

### 완료 판정

- [ ] WAV 7개가 위 경로에 존재하고 **메인 프로젝트의 `git status` 에 untracked 로 보인다**
- [ ] 각 파일 재생 확인 (길이·볼륨이 의도대로인지)
- [ ] `stage-bgm.wav` 를 이어 붙였을 때 이음매가 튀지 않는다
- [ ] `AudioBank.asset` 이 임포트되고 인스펙터에서 7개 클립이 전부 물려 있다
- [ ] 오디오 파일 총합 용량을 보고한다 (step-11 의 WebGL 실측 기준선)
- [ ] `./tests/preflight.sh --fast` PASS

### 예상 커밋 메시지

WAV 와 SO 를 나눈다 — 애셋 커밋과 데이터 커밋은 되돌리는 이유가 다르다:

```
feat(audio): add chiptune bgm and sound effects
chore(balance): add audio bank asset
```

---

## 금지 사항

- **워크트리에서 실행하지 않는다.** `Assets/Audio/` 는 심링크다
- 승인 없이 밸런스 값을 확정하지 않는다 (`CLAUDE.md` §7)
- `.meta` 를 직접 편집하지 않는다 (RULE-03) — 읽기만
- 사람이 만든 음원을 덮지 않는다 (§9)
- 코드 파일을 수정하지 않는다
