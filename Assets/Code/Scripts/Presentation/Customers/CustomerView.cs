using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 하나의 화면 표현. <see cref="CustomerLogic"/> 에 위임만 한다 (<c>CLAUDE.md</c> §3.2).
    /// 자격·배정 판단을 여기에 두지 않는다.
    ///
    /// <para>
    /// 상태에 따라 색을 바꾼다. <b>판정하지 않는다</b> — 상태는 식욕 상태 머신이,
    /// "기다리는 중" 인지는 조율자가 정하고 이 뷰는 결과를 읽기만 한다.
    /// </para>
    /// <para>
    /// <b>머리 위에 상시 글자를 두지 않는다.</b> 한때 이름과 선호 가격대를 자리 옆에 적어
    /// 두었는데(M2.5), 그때는 그것 말고 손님을 구별할 방법이 없었다. 지금은 유형이 실루엣으로
    /// 갈리고(M5) 자세한 값은 눌러서 여는 창이 진다(M6.5) — 셋을 다 띄우면 자리마다 글자
    /// 뭉치가 앉아 정작 포화도·소화가 안 읽힌다. 여기 남는 표시는 <b>상태 색 · 포화도 칸 ·
    /// 소화 배지</b> 셋뿐이다.
    /// </para>
    /// <para>
    /// 잃은 것이 하나 있다: <b>자리 셋을 나란히 놓고 비교할 수 없다.</b> 창은 한 번에 한
    /// 손님만 연다 — 실플레이에서 그것이 문제로 드러나면 되돌릴 자리가 여기다.
    /// </para>
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string SaturationBarName = "SaturationBar";

        private const string DigestingBadgeName = "DigestingBadge";

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _eatingColor = new(1f, 0.85f, 0.3f);
        [SerializeField] private Color _digestingColor = new(0.5f, 0.55f, 0.65f);

        /// <summary>대역 밖 초밥을 두고 더 좋은 것을 기다리는 중임을 나타내는 색.</summary>
        [SerializeField] private Color _waitingColor = new(0.45f, 0.75f, 1f);

        /// <summary>포화도 칸. 없으면 표시만 빠진다 — 표시는 로직의 전제 조건이 아니다.</summary>
        [SerializeField] private SaturationBarView _saturationBar;

        /// <summary>소화중 배지. 없으면 표시만 빠진다.</summary>
        [SerializeField] private DigestingBadgeView _digestingBadge;

        private ClaimCoordinator _coordinator;
        private CustomerState _shownState = CustomerState.Idle;
        private bool _shownWaiting;

        /// <summary>
        /// 배지에 마지막으로 쓴 초. <c>-1</c> 은 «아직 안 썼다» 이며, 소화가 끝날 때 여기로
        /// 되돌린다 — 되돌리지 않으면 <b>다음 소화가 같은 초에서 시작할 때 숫자가 안 그려진다.</b>
        /// </summary>
        private int _shownDigestSeconds = -1;

        /// <summary>참조를 이미 챙겼나.</summary>
        private bool _resolved;

        /// <summary>
        /// 프리팹이 들고 있던 그림. 자리를 비울 때 여기로 되돌린다 — 되돌리지 않으면
        /// 직전 손님의 모습이 그대로 남는다.
        /// </summary>
        private Sprite _defaultSprite;

        /// <summary>이 뷰가 그리고 있는 손님 로직.</summary>
        public CustomerLogic Logic { get; private set; }

        /// <summary>지금 화면에 반영돼 있는 상태. 검증용이다.</summary>
        public CustomerState ShownState => _shownState;

        /// <summary>지금 대기 상태로 그려져 있는가. 검증용이다.</summary>
        public bool ShownWaiting => _shownWaiting;

        /// <summary>지금 화면에 나가 있는 그림. 검증용이다.</summary>
        public Sprite ShownSprite => _body != null ? _body.sprite : null;

        /// <summary>
        /// 배치 시점에 로직과 대기 프로브를 물린다. 뷰가 로직을 스스로 만들거나 찾지 않는다 —
        /// <c>FindObjectOfType</c> 은 금지이고(§4.3), 뷰가 의존을 조달하기 시작하면
        /// "뷰는 물려받기만 한다" 가 무너진다.
        /// </summary>
        /// <param name="coordinator">
        /// 대기 여부를 물어볼 곳. <c>null</c> 이면 대기 표시만 꺼진다 — 상태 색과 대역
        /// 문구는 그대로 나온다.
        /// </param>
        public void Bind(CustomerLogic logic, ClaimCoordinator coordinator)
        {
            Resolve();

            Logic = logic;
            _coordinator = coordinator;
            _shownWaiting = false;

            // 자리를 갈아탈 때 직전 손님의 칸·배지가 남으면, 방금 앉은 손님이 이미 찼거나
            // 소화 중인 것처럼 보인다. 로직이 null 인 경우에도 지나가야 하므로 앞에 둔다.
            if (_saturationBar != null)
            {
                _saturationBar.Bind(logic?.State);
            }

            if (_digestingBadge != null)
            {
                _digestingBadge.Hide();
            }

            _shownDigestSeconds = -1;

            if (logic == null)
            {
                RestoreDefaultIcon();
                return;
            }

            ShowIcon(logic.State.Data);
            Apply(logic.State.State, false);
        }

        /// <summary>
        /// 유형별 그림을 물린다. <b>아이콘이 비면 프리팹의 그림을 그대로 둔다</b> —
        /// 손님이 화면에서 사라지면 자리가 비어 보인다 (<c>CardCaption.NameOf</c> 와 같은 처리다).
        ///
        /// <para>
        /// 그림과 상태 색은 다른 채널이다. 유형을 색으로 구분하면 상태 색과 싸우므로
        /// <b>유형은 실루엣이 맡고 색은 상태가 그대로 쓴다.</b>
        /// </para>
        /// </summary>
        private void ShowIcon(CustomerData data)
        {
            if (_body != null && data.Icon != null)
            {
                _body.sprite = data.Icon;
            }
        }

        /// <summary>
        /// 자리가 비었을 때 프리팹의 그림으로 되돌린다. 이것이 없으면 <b>다시 시작한 판의
        /// 빈 자리에 직전 손님이 그대로 남는다</b> — 자리를 끄더라도, 그 자리에 다른 유형이
        /// 앉고 그 손님의 아이콘이 비어 있으면 옛 그림이 이어진다.
        /// </summary>
        private void RestoreDefaultIcon()
        {
            if (_body != null)
            {
                _body.sprite = _defaultSprite;
            }
        }

        private void Awake()
        {
            Resolve();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라
        /// <see cref="Bind"/> 에서도 부르므로 <b>실행 순서에 기대지 않는다.</b>
        ///
        /// <para>
        /// 자리는 손님이 앉을 때까지 <b>꺼져 있고</b>, <c>TableSlotView.Occupy</c> 는
        /// <see cref="Bind"/> 를 부른 <b>뒤에</b> 자리를 켠다. 꺼진 오브젝트의
        /// <see cref="Awake"/> 는 켜질 때까지 돌지 않으므로, 챙기는 일을 거기에만 두면
        /// <b>첫 배치에서 <c>_body</c>·포화도 칸·배지가 전부 <c>null</c></b> 이다 —
        /// 아이콘이 안 바뀌어 placeholder 블록이 그대로 남고 포화도는 통째로 빠진다.
        /// 두 번째 판부터는 <see cref="Awake"/> 가 이미 돌아 정상으로 보이는데,
        /// <b>«가끔 된다» 가 정확히 그 증상</b>이다 (<c>CustomerCardDrag.Resolve</c> 와
        /// 같은 사고이며 그쪽은 이미 이 방식으로 막아 두었다).
        /// </para>
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }

            _defaultSprite = _body != null ? _body.sprite : null;

            // 인스펙터가 비면 자기 하위에서 이름으로 찾는다. 씬 전역 탐색이 아니다 (§4.3).
            _saturationBar = ResolveChild(_saturationBar, SaturationBarName);
            _digestingBadge = ResolveChild(_digestingBadge, DigestingBadgeName);
        }

        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>자기 하위 계층에서만</b> 이름으로 찾는다.
        /// <c>HudLabel.Resolve</c> 와 같은 방식이며, 타입 둘 때문에 그 조각을 일반화하지는
        /// 않았다.
        /// </summary>
        private T ResolveChild<T>(T assigned, string childName) where T : Component
        {
            if (assigned != null)
            {
                return assigned;
            }

            var child = transform.Find(childName);
            return child != null && child.TryGetComponent<T>(out var found) ? found : null;
        }

        /// <summary>
        /// 상태·대기가 <b>바뀐 프레임에만</b> 색을 쓴다. 매 프레임 대입하면 렌더러가
        /// 머티리얼 프로퍼티를 계속 갱신해 배포 타깃(WebGL)에서 손해다.
        /// </summary>
        private void LateUpdate()
        {
            if (Logic == null)
            {
                return;
            }

            var state = Logic.State.State;
            var waiting = _coordinator != null && _coordinator.IsWaiting(Logic);

            if (state != _shownState || waiting != _shownWaiting)
            {
                Apply(state, waiting);
            }

            RefreshDigestCountdown(state);

            // 포화도는 상태·대기와 다른 축이라 변화 감지에 걸리지 않는다. 바가 스스로
            // 값이 바뀐 프레임에만 색을 쓰므로 매 프레임 물어도 할당이 없다 (§4.3).
            if (_saturationBar != null)
            {
                _saturationBar.Refresh();
            }
        }

        /// <summary>
        /// 소화 중이면 남은 초를 배지에 흘려보낸다.
        ///
        /// <para>
        /// <b>초 단위로 잘라 바뀐 프레임에만 쓴다.</b> 매 프레임 문자열을 만들면 WebGL 에서
        /// GC 스파이크가 그대로 히칭이 된다 (<c>CLAUDE.md</c> §4.3) —
        /// <c>StageHudView.RefreshTime</c> 과 같은 형태다.
        /// </para>
        /// <para>
        /// 올림인 이유도 같다: 0.3초 남았을 때 <c>0</c> 보다 <c>1</c> 이 낫고, 실제로 0 이
        /// 되는 순간은 소화가 끝나는 시점뿐이다.
        /// </para>
        /// </summary>
        private void RefreshDigestCountdown(CustomerState state)
        {
            if (_digestingBadge == null || state != CustomerState.Digesting)
            {
                return;
            }

            var seconds = Mathf.CeilToInt(Logic.State.RemainingDigestSeconds);
            if (seconds == _shownDigestSeconds)
            {
                return;
            }

            _shownDigestSeconds = seconds;
            _digestingBadge.Show(seconds);
        }

        /// <summary>
        /// 대기 색은 <b>Idle 일 때만</b> 쓴다. 먹는 중·소화 중이 대기보다 정보가 크고,
        /// 조율자도 그 상태에서는 대기로 판정하지 않는다 — 두 겹으로 막아 둔다.
        /// </summary>
        private void Apply(CustomerState state, bool waiting)
        {
            _shownState = state;
            _shownWaiting = waiting;

            // 배지는 소화에만 뜬다. 대기와 소화는 둘 다 "지금 안 먹는 상태" 라 뭉뚱그리기
            // 쉬운데, 그러면 배지가 거의 항상 떠 있어 아무것도 알려 주지 않는다.
            //
            // 여기서는 «내리는 것» 만 한다. 띄우는 것은 남은 초를 함께 넘겨야 하므로
            // RefreshDigestCountdown 의 몫이다 — 인자 없는 Show 를 남기면 «숫자 없이 뜨는
            // 배지» 경로가 살아 어느 쪽이 불렸는지 화면에서만 드러난다.
            if (_digestingBadge != null && state != CustomerState.Digesting)
            {
                _digestingBadge.Hide();
                _shownDigestSeconds = -1;
            }

            if (_body == null)
            {
                return;
            }

            _body.color = state switch
            {
                CustomerState.Eating => _eatingColor,
                CustomerState.Digesting => _digestingColor,
                _ => waiting ? _waitingColor : _idleColor
            };
        }
    }
}
