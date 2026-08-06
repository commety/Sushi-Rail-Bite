# Step 05: 초밥·손님 아이콘을 실제로 그린다

- **영역:** `presentation` — 어셈블리 `Presentation`
- **선행 단계:** step-04 (스프라이트 13종)
- **후행 단계:** step-11 의 씬 검증이 이 결과를 본다

---

## 목적

`SushiData.Icon` 과 `CustomerData.Icon` 은 **정의된 이후 한 번도 읽힌 적이 없다.**

```
$ grep -rn "\.Icon" Assets/ --include="*.cs"
(0건)
```

`SushiItem.prefab` 은 `SpriteRenderer` 에 `SushiBlock.png` 를 고정으로 물고 있고, `SushiItemView` 에는 `SpriteRenderer` 필드조차 없다. 결과적으로 **가격 100원부터 400원까지 9종이 벨트 위에서 전부 똑같이 보인다.**

이 마일스톤의 가장 큰 구멍이고, 고치는 데 필요한 코드는 몇 줄이다.

---

## 에이전트 실행 지침

`/task-start` 를 먼저 호출해 범위를 확정한 뒤 아래를 수행한다.

### 수정 파일

- `Assets/Code/Scripts/Presentation/Belt/SushiItemView.cs` — 수정
- `Assets/Code/Scripts/Presentation/Customers/CustomerView.cs` — 수정
- `Assets/Tests/PlayMode/Belt/SushiBeltViewTests.cs` — 추가
- `Assets/Tests/PlayMode/Customers/CustomerViewTests.cs` — 추가
- 밸런스 `.asset` 12장 — **수정 (§7 승인 필요)**

### 핵심 심볼

```csharp
namespace SushiDefense.Belt
{
    public sealed class SushiItemView : MonoBehaviour
    {
        public SushiItem Model { get; private set; }

        /// <summary>지금 그리고 있는 스프라이트. 검증용이다.</summary>
        public Sprite ShownSprite { get; }

        public void Bind(SushiItem model);   // 시그니처 유지
        public void Release();               // 시그니처 유지
    }
}
```

`Bind` 안에서 `model.Data.Icon` 을 `SpriteRenderer.sprite` 에 대입한다. `Release` 는 스프라이트를 되돌린다 — **풀에 누운 뷰가 직전 초밥의 그림을 붙들고 있으면, 재사용 첫 프레임에 엉뚱한 초밥이 번쩍인다.**

`CustomerView` 는 이미 `_body` (`SpriteRenderer`) 를 갖고 있다. `Bind` 에서 `data.Icon` 을 대입하고, **상태 색은 `color` 틴트로 그대로 남긴다** — 스프라이트와 틴트는 다른 채널이므로 충돌하지 않는다.

### 아이콘이 비어 있을 때

**절대 빈 화면을 만들지 않는다.** `Icon` 이 `null` 이면 **프리팹에 물려 있는 기본 스프라이트를 유지**한다. `_icon` 을 아직 안 채운 애셋에서 초밥이 사라지면 "벨트가 고장 났다" 로 보인다.

`CustomerView.NameOf` 가 이미 같은 판단을 하고 있다 — 이름이 비면 애셋 이름으로 대신한다. **같은 처리를 그림에도 적용한다.**

### 선행 산출물 의존성

- step-04 의 13개 스프라이트

### 밸런스 수치

없음. 다만 **밸런스 `.asset` 12장의 `_icon` 필드를 물려야** 한다 (§7 — 아래).

### §7 승인 요청 — `.asset` 수정

`_icon` 은 밸런스 수치가 아니라 표현이지만, 수정 대상이 밸런스 애셋 파일이므로 §7 의 *"밸런스 SO 데이터 일괄 수정"* 에 걸리는 회색지대다. **diff 를 제시하고 승인을 받는다.**

| 애셋 | 물릴 스프라이트 |
|---|---|
| `Sushi.Egg` `Squid` `Flounder` `Salmon` `Yellowtail` `Tuna` `Eel` `SeaUrchin` `Placeholder` | 대응하는 `Assets/Art/Sprites/Sushi/*.png` |
| `Customer.Standard` `BigEater` `SmallEater` | 대응하는 `Assets/Art/Sprites/Customers/*.png` |
| `Customer.Placeholder` | 기존 `CustomerBlock.png` 유지 |

`.asset` 은 YAML 이므로 직접 편집한다. 참조는 GUID 이고, **GUID 는 step-04 가 임포트한 뒤 생성된 `.meta` 에서 읽는다** — 2-pass 절차다 (`.claude/knowledge/unity-editor-automation.md` «한계/함정»). `.meta` 를 **읽는 것**은 RULE-03 위반이 아니다. 쓰는 것이 금지다.

### 제약

- `SushiItemView` 는 **판정하지 않는다.** 지금도 좌표만 따라가고 있고, 이 단계 이후에도 그림만 추가된다
- `Bind`/`Release` 시그니처를 바꾸지 않는다 — `SushiBeltView` 가 호출한다
- `LateUpdate` 에서 스프라이트를 대입하지 않는다. `Bind` 시점 한 번이면 충분하고, 매 프레임 대입하면 렌더러가 계속 갱신된다 (`CustomerView` 가 상태 색에 이미 같은 최적화를 하고 있다 — 바뀐 프레임에만 쓴다)
- `Update`/`LateUpdate` 경로에 할당을 만들지 않는다 (`scripts.md` §4)
- `GetComponent` 는 `Awake` 에서 캐싱한다 (§4)
- `FindObjectOfType` 금지. 참조는 인스펙터 또는 `Awake` 의 자기 하위 탐색으로 (§4.3)

### 테스트 계획

**PlayMode 다** — `SpriteRenderer` 와 프리팹 인스턴스가 필요하다. 다만 **최소한으로** 쓴다 (`tests.md` §1).

```
초밥  Bind_DifferentSushiData_ShowsDifferentSprites   ← 두 종류가 서로 다르게 그려진다
      Bind_DataWithoutIcon_KeepsPrefabSprite          ← 빈 화면 금지
      Release_ThenBindAgain_ShowsNewSprite            ← 풀 재사용 시 잔상 없음
손님  Bind_CustomerWithIcon_ShowsThatSprite
      Bind_CustomerWithIcon_StillAppliesStateTint     ← 스프라이트와 틴트가 싸우지 않는다
```

**"둘이 같다" 만 확인하지 않는다** (`tests.md` §3). `Bind_DifferentSushiData_ShowsDifferentSprites` 는 *"서로 다르다"* 뿐 아니라 **어느 쪽이 어느 스프라이트인지**를 박는다 — 스프라이트를 뒤바꿔 대입하는 구현에서도 "다르다" 는 통과한다.

### 주입으로 확인한다

| 주입 | 잡혀야 할 테스트 (가설) |
|---|---|
| `Release` 에서 스프라이트 복원 제거 | `Release_ThenBindAgain_ShowsNewSprite` |
| `Icon` 이 null 일 때 `sprite = null` 대입 | `Bind_DataWithoutIcon_KeepsPrefabSprite` |
| `Bind` 에서 스프라이트 대입 자체를 제거 | 초밥 2건 모두 |

**예측은 가설이다.** 실제로 넣어 보고 잡힌 이름으로 이 표를 정정한다 (`tests.md` §3 — M4 에서 세 번 빗나갔다).

### 완료 판정

- [ ] `grep -rn "\.Icon" Assets/Code/Scripts/Presentation/` 가 **2건 이상** (0건이 아니라는 것이 이 단계의 핵심이다)
- [ ] `./tests/run-tests.sh all` Green
- [ ] 씬을 재생해 벨트 위에 **서로 다른 초밥이 흐르는** 스크린샷 확인 (저해상도, §9)
- [ ] 주입 3건 실측 후 표 정정
- [ ] `./tests/preflight.sh` 전 항목 PASS

### 예상 커밋 메시지

```
feat(belt): render sushi icons from sushi data
```

`.asset` 수정은 별도 커밋으로 나눈다:

```
chore(balance): wire placeholder icons into sushi and customer data
```

---

## 금지 사항

- 유형·가격으로 코드에서 분기해 스프라이트를 고르지 않는다. **어느 그림을 쓸지는 데이터가 정한다** — 그렇지 않으면 초밥을 추가할 때마다 뷰를 고쳐야 한다
- 승인 없이 `.asset` 을 수정하지 않는다
- `.meta` 를 쓰지 않는다 (읽기만) — RULE-03
- 다른 어셈블리 파일을 수정하지 않는다
