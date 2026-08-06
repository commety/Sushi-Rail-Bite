# Step 09: 한글 서브셋 TMP 폰트 + 두부 회귀 테스트

- **영역:** `assets` — 어셈블리 없음 (애셋 제작) + `Tests.EditMode`
- **선행 단계:** 없음 (step-01·02·03 과 병렬 가능)
- **후행 단계:** step-10 의 uGUI 전환이 이 폰트를 쓴다. **폰트 없이 전환하면 화면이 전부 두부가 된다**

---

## 목적

**지금 상태로 WebGL 빌드를 하면 HUD 의 한글이 두부(□)로 나올 가능성이 높다.**

`PlaceholderLabel.EnsureFont` 는 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 를 쓴다. 이 폰트에는 한글 글리프가 없고, **WebGL 은 OS 폰트에 접근할 수 없어 폴백도 없다.** 다른 플랫폼에서는 시스템 폰트가 메워 주기 때문에 에디터에서는 멀쩡히 보인다 — 빌드해야만 드러나는 실패다.

화면에 나가는 문구가 전부 한글이다: `매출 4990/5000`, `남은 시간 47`, `영입 재화 20`, `대기 2`, `클리어`, `스테이지 1 클리어 — 다음: 스테이지 2 (Enter)`, 손님 이름 `기본`·`먹보`·`소식`, 초밥 이름 `참치`·`성게` …

**정적 서브셋으로 간다** (아키텍트 결정) — 실제 쓰는 글자만 구우면 수십 KB 이고, WebGL 초기 로드에 사실상 영향이 없다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 생성 파일

```
Assets/Art/Fonts/
  <원본>.ttf                        ← 라이선스 확인 필요 (§7)
  SushiRailBite-KR.asset            ← TMP_FontAsset (정적 서브셋)
  charset-ko.txt                    ← 구울 글자 목록 (근거를 남긴다)

Assets/Tests/EditMode/UI/KoreanFontCoverageTests.cs
```

> **`Assets/Art/` 는 워크트리에서 심링크다.** 메인 프로젝트에서만 실행한다.

### 폰트 선택 — §7 승인 필요

**폰트는 서드파티 자산이다.** 에이전트가 단독으로 고르지 않는다 (`CLAUDE.md` §7).

제안 기준을 보고하고 승인을 받는다:

| 기준 | 왜 |
|---|---|
| **오픈 라이선스** (OFL 등) — 웹 배포에 재배포 조항 확인 | 데모를 공개 호스팅한다 (M8) |
| **픽셀/비트맵 계열 또는 굵고 각진 고딕** | 일본풍 픽셀 아트와 얇은 명조는 안 어울린다 |
| **한글 완성형 커버리지** | 서브셋을 뽑으려면 원본에 글자가 있어야 한다 |

승인 뒤 파일을 받는다. **에이전트가 폰트를 임의로 다운로드하지 않는다** — 라이선스 판단은 사람의 몫이다.

### 서브셋 문자 목록을 어디서 뽑나

**하드코딩하지 않는다.** 목록의 출처가 없으면 문구를 추가할 때마다 어긋난다.

수집 대상:

1. **코드의 표시 문자열** — `Assets/Code/Scripts/Presentation/**` 의 보간 문자열 리터럴
   (`매출 {total}/{target}`, `남은 시간 {seconds}`, `클리어`, `실패`, `배치 예정`, `대기`, `손님`, `영입 재화`, `스테이지 {N} 클리어 — 다음: 스테이지 {N} (Enter)`, `런 완료!`, `영입` …)
2. **밸런스 애셋의 `_displayName`** — `Assets/Level/Balance/*.asset`
   (초밥 9종: 달걀 한치 광어 연어 방어 참치 장어 성게 / 손님 3종: 기본 먹보 소식)
3. **숫자·영문·기호** — `0-9`, `A-Za-z`, `/ ~ ( ) — ! : + -`

**추출 절차를 `charset-ko.txt` 옆에 스크립트나 주석으로 남긴다.** 다음 사람이 문구를 추가한 뒤 같은 절차를 다시 돌릴 수 있어야 한다.

여유분을 조금 더 굽는다 — M6 이 쓸 흔한 단어(`설정`·`시작`·`덱`·`저장`·`나가기`)를 미리 넣어 두면 다음 마일스톤에서 서브셋을 다시 뽑지 않아도 된다. **다만 여유분이 커지면 서브셋의 의미가 사라진다.** 수십 글자 선에서 끊는다.

### TMP_FontAsset 생성

Window > TextMeshPro > Font Asset Creator:

| 설정 | 값 | 이유 |
|---|---|---|
| Sampling Point Size | 폰트에 맞춰 고정 | 픽셀 폰트는 **정수 배수**여야 뭉개지지 않는다 |
| Padding | `2~4` | 너무 작으면 글자끼리 번진다 |
| Render Mode | `RASTER` 또는 `SMOOTH` | 픽셀 폰트면 `RASTER` — 안티에일리어싱이 픽셀 아트를 흐린다 |
| Atlas Resolution | 서브셋이 들어가는 최소 크기 | 512×512 로 시작해 남으면 줄인다 |
| Character Set | `Characters from File` → `charset-ko.txt` | |

**생성된 아틀라스 텍스처에 step-03 의 픽셀 규칙이 걸리면 안 된다.** `Assets/Art/Fonts/` 는 `Assets/Art/Sprites/` 하위가 아니므로 대상 밖이다 — step-03 의 `AppliesTo` 가 이 경로에 `false` 를 돌려주는지 확인한다. 걸리면 폰트가 깨진다.

### 선행 산출물 의존성

없음.

### 밸런스 수치

없음.

### 제약

- **`CLAUDE.md` §9 — 원본 자산 보호.** 폰트 원본을 커밋하기 전에 라이선스와 재배포 가능 여부를 사람이 확인한다
- 폰트 파일은 크다. **Git LFS 대상인지 `.gitattributes` 를 확인한다** (§9)
- `.meta` 를 직접 편집하지 않는다 (RULE-03)
- **동적 폰트로 도망가지 않는다.** Dynamic 모드는 없는 글자를 런타임에 굽지만, **원본 폰트 전체가 빌드에 들어간다** — 서브셋을 택한 이유가 사라진다

### 테스트 계획 — 두부 회귀를 잡는다 (README D8)

정적 서브셋의 유일한 실패 모드는 *"나중에 추가한 문구의 글자가 폰트에 없다"* 이고, 증상은 두부다. **EditMode 로 잡을 수 있다.**

```csharp
// TMP_FontAsset.HasCharacters(string, out List<char> missing)
```

```
KoreanFontCoverageTests
  HasCharacters_HudLabels_NoneMissing            ← 매출/남은 시간/영입 재화/대기/손님/배치 예정
  HasCharacters_OutcomeLabels_NoneMissing        ← 클리어/실패
  HasCharacters_TransitionLabels_NoneMissing     ← 스테이지 N 클리어 — 다음: …
  HasCharacters_SushiDisplayNames_NoneMissing    ← 밸런스 애셋 9종
  HasCharacters_CustomerDisplayNames_NoneMissing ← 밸런스 애셋 3종
  HasCharacters_Digits_NoneMissing
```

**이 테스트는 예외적으로 디스크의 실제 애셋을 로드한다.** `tests.md` §4 는 밸런스 애셋 로드를 금지하지만, 그 이유는 *"수치가 바뀔 때마다 테스트가 깨진다"* 이다. 여기서 보는 것은 수치가 아니라 **글자 커버리지**이고, 이름이 바뀌면 폰트를 다시 구워야 하는 것이 맞다 — **깨져야 정상인 테스트다.** 이 예외를 테스트 파일 주석에 남긴다.

폰트 애셋이 아직 없으면 테스트를 `[Ignore]` 로 덮지 않는다 (`tests.md` §7). **애셋을 먼저 만든 뒤 테스트를 쓴다** — 이 단계는 TDD 의 Red-first 가 적용되지 않는 드문 경우다 (검증 대상이 코드가 아니라 애셋이다).

### 완료 판정

- [ ] `Assets/Art/Fonts/SushiRailBite-KR.asset` 이 존재하고 인스펙터에서 글리프가 보인다
- [ ] `charset-ko.txt` 와 그 추출 절차가 저장소에 있다
- [ ] `./tests/run-tests.sh` Green — 커버리지 테스트 전부 통과
- [ ] **일부러 없는 글자**(예: `벽`)를 넣은 임시 테스트가 **실패하는지** 확인한 뒤 되돌린다 — 통과하면 `HasCharacters` 를 잘못 쓰고 있는 것이다
- [ ] 폰트 애셋 + 아틀라스 텍스처의 총 용량을 보고한다 (step-11 실측 기준선)
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(ui): add korean subset font asset with coverage tests
```

---

## 금지 사항

- 승인 없이 폰트를 고르거나 다운로드하지 않는다 (§7·§9)
- 전체 한글을 굽지 않는다 — 아키텍트가 서브셋을 택했다
- Dynamic 폰트로 대체하지 않는다
- `.meta` 를 직접 편집하지 않는다
- 커버리지 테스트를 `[Ignore]` 로 덮지 않는다
- 코드 파일을 수정하지 않는다 (테스트 제외)
