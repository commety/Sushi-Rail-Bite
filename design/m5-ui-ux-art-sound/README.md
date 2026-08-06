# M5 — UI/UX · 아트 · 사운드

## 한 줄 요약

데모 3스테이지의 **보이는 것과 들리는 것**을 채운다 — 초밥·손님이 종류별로 구분되고, 먹힘·클리어가 소리와 이펙트로 되돌아오고, 인스테이지 UI 가 브라우저 창 크기와 무관하게 읽히는 화면이 된다.

## 원문

[`docs/plan/M5-M9-later.md`](../../docs/plan/M5-M9-later.md) §M5 — *배경, 초밥 라인, 초밥·손님 디자인, UI, BGM, 효과음, VFX.*

원문이 "지금 알아둘 것" 으로 못박은 다섯 가지가 그대로 이 작업서의 뼈대다:

| 원문의 요구 | 이 작업서에서 |
|---|---|
| 아트 톤 — 일본풍 2D 픽셀 아트, placeholder 는 원본을 덮지 않는다 | step-04 (플레이스홀더), step-03 (임포트 규칙이 원본에도 그대로 적용) |
| 픽셀 아트 임포트 설정 — Point, Mip 끄기, 압축 신중히 | **step-03 이 코드로 강제한다** — 인스펙터 수작업으로 두면 새 스프라이트마다 샌다 |
| Sprite Atlas 가 특히 중요 (벨트에 여러 종류가 동시에 돈다) | step-04 후반 |
| 원본 에셋 보호 (`CLAUDE.md` §9) | step-04 · step-06 · step-09 의 §7 승인 항목 |
| **효과음 겹침** — 동시 재생 수 제한·쿨다운·덕킹 | **step-02 가 순수 C# 로직으로 뽑는다** — EditMode 로 검증되는 유일한 형태 |

## 착수 전 실측 — 지금 저장소의 상태

작업서를 쓰기 전에 확인한 사실이다. 추측이 아니라 `grep` 결과다.

| 확인 대상 | 상태 | 영향 |
|---|---|---|
| `SushiData.Icon` · `CustomerData.Icon` | 필드는 있는데 **읽는 코드가 0건** (`grep -rn "\.Icon" Assets/ --include="*.cs"`) | **초밥 9종이 전부 같은 블록으로 그려진다.** 가격 대역이 이 게임의 핵심 규칙(M2.5)인데 화면에서 가격을 구분할 수 없다 → step-05 |
| `SushiEatenEventChannelSO` | 정의·EditMode 테스트 완비, **프로덕션 사용처 0건**. 실제 경로는 `ClaimCoordinator.SushiEaten` (C# 이벤트) | 오디오를 채널에 붙이면 배선이 두 벌이 된다 → D5 |
| 오디오 | `AudioSource`·`AudioClip` 코드 0건, `Assets/Audio/` 는 `.gitkeep` 뿐. `AudioListener` 는 Main Camera 에 있다 | 바닥부터 → step-01·02·06·07 |
| UI | 전부 월드스페이스 `TextMesh` (HUD 7라벨 + 보상 + 전환). Canvas 없음 | 창 크기가 바뀌면 무너진다 → step-10 |
| `Presentation.asmdef` | `["Runtime", "Runtime.Data"]` 만 참조 | TMP 참조 추가 필요 (§7 인접) → step-10 |
| 한글 폰트 | `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` | **WebGL 에는 OS 폰트 폴백이 없다.** 이 폰트에 한글 글리프가 없으므로 빌드에서 HUD 가 전부 두부(□)로 나올 가능성이 높다 → step-09 |
| Sprite Atlas | 없음 | step-04 |
| `Assets/Code/Scripts/Editor/` | `Editor.asmdef` 만 있고 스크립트 0개 | step-03 이 첫 입주자 |

## 아키텍트 결정 (4문 4답)

| 물음 | 답 |
|---|---|
| 인스테이지 HUD 를 uGUI 로 전환? | **uGUI + TMP 로 전환** — 인스테이지에 뜨는 것(HUD·보상·전환)까지 |
| 한글 폰트 | **정적 서브셋** — 실제 쓰는 글자만 |
| 아트 조달 | **플레이스홀더 파이프라인 우선** — 원본이 나오면 파일만 교체 |
| 사운드 | **BGM + SFX 전부 `/synth`** |

## 아키텍처 결정

- **D1. 인스테이지 UI 는 M5, 화면·메뉴는 M6.**
  `PlaceholderLabel` · `StageHudView` · `RewardSelectionView` · `StageTransitionView` 네 파일의 주석이 *"M6 에서 제대로 된 UI 로 교체된다"* 고 적혀 있는데, 원문 계획은 UI 를 M5 에 두었다. **경계를 여기서 확정한다** — 스테이지 안에서 뜨는 것은 M5, 메인화면·덱빌딩·설정·백과사전은 M6. step-10 에서 그 주석 네 곳을 함께 정정한다.

- **D2. 뷰의 public 계약을 바꾸지 않는다.**
  `RevenueText` · `BandText` · `OutcomeText` 같은 검증용 프로퍼티와 `Bind`/`Unbind` 시그니처는 그대로 두고, **내부 렌더 대상만** `TextMesh` → `TMP_Text` 로 바꾼다. 그래야 `StageHudViewTests` · `RewardSelectionViewTests` · `StageTransitionViewTests` · `CustomerViewTests` 가 전환 작업의 **회귀 그물로 살아남는다.** 계약까지 같이 바꾸면 테스트를 고치면서 하는 전환이 되고, 그 순간 그물이 사라진다.

- **D3. 아이콘을 실제로 그린다 (step-05).**
  `SushiData.Icon` 이 2년째 아무도 읽지 않는 필드로 남아 있는 것이 M5 의 가장 큰 구멍이다. 벨트 위에서 100원과 400원이 같아 보이면 플레이어는 대역 규칙을 배울 수 없다 — M2.5 를 한 이유가 그것이었다.

- **D4. 효과음 겹침 제어는 순수 C# 다 (step-02).**
  `SoundBudget` 은 `AudioSource` 를 모른다. 쿨다운과 동시 재생 상한만 판정한다. `MonoBehaviour` 안에 넣으면 "0.05초 안에 초밥 셋이 먹혔을 때" 를 재현하려고 PlayMode 프레임을 세게 되고, 그 테스트는 느리고 불안정하다 (`.claude/rules/tests.md` §1).

- **D5. `SushiEatenEventChannelSO` 를 지금 살리지 않는다.**
  채널은 어셈블리를 넘는 통신용인데, M5 의 오디오·VFX 는 **`StageBootstrap` 이 이미 붙잡고 있는 `ClaimCoordinator`** 에서 같은 정보를 그대로 얻는다. 지금 채널을 끼우면 같은 사실을 알리는 경로가 두 벌이 되고, 어느 쪽이 진실인지 다음 사람이 알 수 없다. **채널의 처분은 M6 으로 미룬다** — 씬이 여럿이 되어 씬을 넘는 통신이 실제로 필요해지는 시점이다. 그때 살릴지 지울지 정한다.

- **D6. VFX 도 풀을 경유한다. 인터페이스 이름은 바꾸지 않는다.**
  `CLAUDE.md` §3.4 는 프로덕션 코드의 `Instantiate`/`Destroy` 직접 호출을 금지한다 — 초밥에만 걸린 규칙이 아니라 WebGL GC 때문이다. `SushiPool<T>` 는 이미 제네릭이므로 그대로 쓴다. `ISushiInstanceFactory<T>` 라는 이름이 초밥 전용처럼 보이지만 **M5 에서 리네임하지 않는다** — 4파일짜리 개명의 이득보다, 아트·사운드가 걸린 마일스톤에 리팩터 커밋을 섞는 비용이 크다. step-08 의 주석에 이유를 남긴다.

- **D7. 연출 수치는 SO 로 가지 않는다. 오디오 수치는 간다.**
  구분선은 *"사람이 밸런싱하며 만질 값인가"* 다.
  - **`AudioBankSO` 로 (step-01)**: 쿨다운, 동시 재생 상한, 큐별 볼륨 — 겹침이 거슬리는지 아닌지는 **플레이하며 조정하는 값**이다
  - **프리팹의 `[SerializeField]` 로 (step-08)**: 이펙트 수명·크기·색 — 그 이펙트 프리팹 하나에만 의미가 있고, 다른 곳에서 참조되지 않는다. SO 로 빼면 애셋만 늘고 찾기 어려워진다
  - **코드 상수로 (step-03)**: PPU·필터 모드 — 밸런스가 아니라 파이프라인 설정이며, 값이 갈리면 그게 버그다. 단 **한 파일 한 곳**에만 둔다

- **D8. 폰트 서브셋은 회귀 테스트를 동반한다 (step-09).**
  정적 서브셋의 유일한 실패 모드는 *"나중에 추가한 문구의 글자가 폰트에 없다"* 이고, 그 증상은 **두부(□)** 다. `TMP_FontAsset.HasCharacters(string, out missing)` 로 EditMode 에서 확인할 수 있으므로, 화면에 나가는 문구의 글자 집합을 테스트가 지킨다. 이게 없으면 서브셋은 시한폭탄이다.

- **D9. 임포트 설정을 사람 손에 맡기지 않는다 (step-03).**
  Filter `Point` 하나만 빠져도 픽셀 아트가 뭉개진다. 스프라이트를 추가할 때마다 인스펙터에서 맞추는 방식은 반드시 샌다. `AssetPostprocessor` 로 경로 기반 자동 적용하고, **이 규칙은 M5 가 끝나도 M9 밸런싱까지 계속 쓰인다** — 이 마일스톤이 남기는 가장 오래 가는 자산이다.

- **D10. 브라우저 자동재생 정책을 설계에 넣는다 (step-02·07).**
  WebGL 은 사용자 제스처가 있기 전 오디오 컨텍스트가 잠긴다. `Start()` 에서 BGM 을 재생하면 **무음으로 시작해 영영 들리지 않는다** — 소리가 안 나는 게 아니라 재생이 이미 지나가 버린다. 첫 입력 시점까지 재생을 보류하는 게이트를 두고, 그 판정도 순수 로직으로 뺀다.

## 원문 정정 — M5 는 워크트리로 병렬화할 수 없다

[`docs/plan/M5-M9-later.md`](../../docs/plan/M5-M9-later.md) §M5 는 *"파일이 겹치지 않으므로 워크트리를 나눠 동시에 진행할 수 있다"* 고 적었다. **애셋 단계에는 해당되지 않는다.**

`scripts/ensure-worktree-setup.sh` 의 `SYMLINK_ASSET_DIRS` 에 `Assets/Art` 와 `Assets/Audio` 가 들어 있다. 워크트리에서 이 폴더에 새 파일을 만들면 파일은 심링크를 관통해 **메인 프로젝트에** 생기고, 워크트리의 `git status` 는 그것을 보지 못한다 ([`parallel-work.md`](../../.claude/rules/parallel-work.md) §2). **M5 의 결과물 대부분이 정확히 그 폴더로 간다.**

따라서:

| 단계 | 워크트리 |
|---|---|
| step-04 · 06 · 09 (스프라이트 · 오디오 · 폰트) | **금지** — 메인 프로젝트에서만 |
| step-11 (씬 편집) | **금지** — 씬은 한 번에 한 워크트리 (§3) |
| 나머지 (코드) | 가능하지만 이득이 적다 |

**M5 는 메인 프로젝트에서 직렬로 간다.** 원문의 §M5 문단은 이 사실을 반영해 고쳐야 한다 (step-11 에서 함께).

## 터치 영역

| 영역 | 어셈블리 | 역할 |
|---|---|---|
| data | `Runtime.Data` | `AudioBankSO` · `AudioCue` — 큐별 클립·볼륨·쿨다운, 동시 재생 상한 |
| runtime | `Runtime` | `SoundBudget` · `AudioUnlockGate` — 겹침 제어와 자동재생 게이트의 **판정** |
| presentation | `Presentation` | 아이콘 바인딩, `AudioDirector`, VFX 풀·뷰, uGUI/TMP 전환 |
| editor | `Editor` | `PixelArtTextureImportRules` — 임포트 설정 자동 적용 |
| tests | `Tests.EditMode` / `Tests.PlayMode` | 위 전부 |
| — | (애셋) | 스프라이트 · 아틀라스 · WAV · TMP 폰트 · 씬 |

## 의존성 그래프

```
SushiDefense.Data.AudioBankSO ──► SushiDefense.Audio.SoundBudget ──► SushiDefense.Audio.AudioDirector
SushiDefense.Data.SushiData.Icon ─────────────────────────────────► SushiDefense.Belt.SushiItemView
SushiDefense.Data.CustomerData.Icon ──────────────────────────────► SushiDefense.Customers.CustomerView
SushiDefense.Belt.SushiPool<GameObject> ──────────────────────────► SushiDefense.UI.EffectPoolBehaviour
```

`Runtime` 은 `Presentation` 을 모른다. 오디오·VFX 는 **`StageBootstrap` 이 구독을 걸어** 방향을 뒤집는다 — `ClaimCoordinator` 가 뷰를 직접 부르지 않는다.

## 새 밸런스 수치

| 수치 | 들어갈 SO | 초기값 제안 | 근거 |
|---|---|---|---|
| 큐별 쿨다운(초) | `AudioBankSO` | 먹힘 `0.06` / 나머지 `0` | 먹힘만 연달아 터진다. 60fps 기준 3~4프레임이면 개별 타격감을 잃지 않으면서 뭉개짐이 사라지는 구간 |
| 동시 SFX 상한 | `AudioBankSO` | `6` | 손님 4명 × 먹힘 + 배치/보상이 겹치는 최악 순간을 덮는 값 |
| 큐별 볼륨 | `AudioBankSO` | 먹힘 `0.5` / 결과음 `0.8` | 먹힘은 가장 자주 난다 — 가장 작아야 한다 |
| BGM 볼륨 | `AudioBankSO` | `0.35` | SFX 를 덮지 않는 선. 덕킹 대신 고정 비율로 시작하고 실측 후 조정 |
| 픽셀 PPU | (코드 상수, step-03) | `32` | 벨트 길이 20 유닛에 초밥 9종이 도는 스케일. **밸런스가 아니다** — D7 |

> **`.asset` 파일에 실제로 값을 쓰는 것은 사람의 판단 영역이다** (`CLAUDE.md` §7). 위는 제안값이며, step-06 이 diff 를 제시하고 승인을 기다린다.

## §7 승인이 필요한 항목

각 단계가 해당 지점에서 멈추고 요청한다. 에이전트가 단독으로 진행하지 않는다.

| # | 항목 | 단계 |
|---|---|---|
| 1 | `Tests.EditMode.asmdef` 에 `"Editor"` 참조 추가 | step-03 |
| 2 | `Presentation.asmdef` 에 `"Unity.TextMeshPro"` 참조 추가 | step-10 |
| 3 | 밸런스 `.asset` 12장에 `_icon` 물리기 | step-05 |
| 4 | Sprite Atlas V2 설정 (`ProjectSettings/EditorSettings.asset`) | step-04 |
| 5 | 새 오디오 파일 커밋 (§9 원본 보호) | step-06 |
| 6 | 폰트 라이선스 확인 및 커밋 (§9) | step-09 |
| 7 | `AudioBank.asset` 수치 확정 | step-06 |

## 단계

| # | 파일 | 영역 | 무엇 |
|---|---|---|---|
| 01 | [step-01-data-audio-bank.md](step-01-data-audio-bank.md) | data | `AudioBankSO` · `AudioCue` 스키마 |
| 02 | [step-02-runtime-sound-budget.md](step-02-runtime-sound-budget.md) | runtime | `SoundBudget` · `AudioUnlockGate` — 겹침 제어 |
| 03 | [step-03-editor-pixel-import-rules.md](step-03-editor-pixel-import-rules.md) | editor | 픽셀 아트 임포트 설정 자동 적용 |
| 04 | [step-04-assets-placeholder-sprites.md](step-04-assets-placeholder-sprites.md) | assets | 플레이스홀더 스프라이트 13종 + Sprite Atlas |
| 05 | [step-05-presentation-icon-binding.md](step-05-presentation-icon-binding.md) | presentation | 초밥·손님 아이콘을 실제로 그린다 |
| 06 | [step-06-assets-chiptune-audio.md](step-06-assets-chiptune-audio.md) | assets | `/synth` BGM 1곡 + SFX 6종 + `AudioBank.asset` |
| 07 | [step-07-presentation-audio-director.md](step-07-presentation-audio-director.md) | presentation | `AudioDirector` 배선 + 자동재생 게이트 |
| 08 | [step-08-presentation-effects.md](step-08-presentation-effects.md) | presentation | VFX — 먹힘 팝 · 클리어/실패, 풀 경유 |
| 09 | [step-09-assets-korean-font.md](step-09-assets-korean-font.md) | assets | 한글 서브셋 TMP 폰트 + 두부 회귀 테스트 |
| 10 | [step-10-presentation-ugui-canvas.md](step-10-presentation-ugui-canvas.md) | presentation | HUD·보상·전환을 uGUI Canvas 로 |
| 11 | [step-11-scene-dressing-and-webgl.md](step-11-scene-dressing-and-webgl.md) | scene | 배경·벨트 비주얼·레이아웃 + WebGL 실측 |

## 병렬 실행 가능성

**결론부터: 실행은 직렬로 한다** (위 «원문 정정» 참조). 아래는 *논리적* 독립성이며, 한 단계가 막혔을 때 무엇을 먼저 돌릴 수 있는지 판단하는 데 쓴다.

```
01 ─┬─────────────► 06 ──┐
02 ─┘                    ├──► 07
03 ──► 04 ─┬──► 05       │
           └──► 08 ◄─────┘
09 ──► 10 ──┐
            ├──► 11
전부 ───────┘
```

- **step-01 · 02 · 03 · 09 는 서로 독립이다.** `SoundBudget` 은 쿨다운을 인자로 받으므로 `AudioBankSO` 타입을 컴파일 의존하지 않는다 — 순서는 논리적 순서일 뿐이다
- **step-04 는 step-03 뒤에 와야 한다.** 임포트 규칙이 먼저 있어야 새로 만든 텍스처가 자동으로 맞춰진다. 순서가 뒤집히면 13장을 손으로 다시 임포트해야 한다
- **step-05 와 step-08 은 step-04 뒤에서 병렬 가능** — 서로 다른 파일을 만든다
- **step-11 은 전부 끝난 뒤.** 씬은 모든 단계의 결과가 모이는 곳이다

## 완료 판정 (M5 전체)

- [ ] 벨트 위에서 **초밥 종류가 눈으로 구분된다** — 가격대가 다르면 다르게 보인다
- [ ] 손님 3유형이 서로 다르게 보인다
- [ ] 초밥이 먹히면 **소리와 이펙트가 난다**. 넷이 동시에 먹혀도 뭉개지지 않는다
- [ ] BGM 이 첫 입력 이후 재생된다 (자동재생 정책 대응 확인)
- [ ] HUD 가 **브라우저 창 크기를 바꿔도 유지된다**
- [ ] **WebGL 빌드에서 한글이 두부로 나오지 않는다**
- [ ] 벨트 위 초밥이 몇 종류든 **드로우콜이 종류 수만큼 늘지 않는다** (아틀라스)
- [ ] `./tests/preflight.sh` 전 항목 PASS
- [ ] 원본 픽셀 아트가 들어올 때 **코드 변경 0** 으로 교체된다 (파일만 갈아끼우면 된다)

## 이 마일스톤이 M6 에 넘기는 것

- Canvas · TMP · 한글 폰트 — M6 의 메인화면·덱빌딩이 그대로 쓴다
- `AudioBankSO` — 메뉴 SE 를 큐로 추가만 하면 된다
- 임포트 규칙 — M6·M9 에 들어올 모든 스프라이트에 자동 적용된다
- **`SushiEatenEventChannelSO` 의 처분** (D5) — 씬이 여럿이 되는 시점의 숙제

## 이 마일스톤이 손대지 않는 것

- 메인화면 · 덱빌딩 · 설정 · 백과사전 (M6)
- 씬 전환 구조 (M6) — M5 는 `Stage01` 한 씬 안에서 끝난다
- 밸런스 수치 (M9) — 성게 400 이 모든 대역 밖이라는 문제는 M9 로 넘어가 있다
- 실제 픽셀 아트 원본 (사람 조달, 시점 미정) — step-04 는 교체 가능한 자리를 만들어 둘 뿐이다
