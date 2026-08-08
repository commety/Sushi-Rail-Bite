using SushiDefense.Data;
using SushiDefense.UI;
using TMPro;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 하나의 화면 표현. <see cref="CustomerLogic"/> 에 위임만 한다 (<c>CLAUDE.md</c> §3.2).
    /// 자격·배정 판단을 여기에 두지 않는다.
    ///
    /// <para>
    /// 상태에 따라 색을, 손님 데이터에 따라 대역 문구를 바꾼다. <b>판정하지 않는다</b> —
    /// 상태는 식욕 상태 머신이, "기다리는 중" 인지는 조율자가 정하고 이 뷰는 결과를 읽기만
    /// 한다. 색 블록은 M5(아트)까지의 placeholder 다.
    /// </para>
    /// <para>
    /// <b>대역과 대기를 손님 옆에 둔 이유</b>: 대역은 손님마다 다른 값이라 전역 HUD 로는
    /// 자리 셋을 구분해 보여 줄 수 없다. M2.5 를 하는 이유 자체가 "플레이어가 규칙을
    /// 배우지 못한다" 이므로 어느 손님이 무엇을 노리는지가 자리 옆에 있어야 한다.
    /// </para>
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string BandLabelName = "BandLabel";

        private const string SaturationBarName = "SaturationBar";
        private const string DigestingBadgeName = "DigestingBadge";

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private TMP_Text _bandLabel;
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

        /// <summary>이 뷰가 그리고 있는 손님 로직.</summary>
        public CustomerLogic Logic { get; private set; }

        /// <summary>지금 화면에 반영돼 있는 상태. 검증용이다.</summary>
        public CustomerState ShownState => _shownState;

        /// <summary>지금 대기 상태로 그려져 있는가. 검증용이다.</summary>
        public bool ShownWaiting => _shownWaiting;

        /// <summary>지금 표시 중인 대역 문구. 검증용이다.</summary>
        public string BandText { get; private set; }

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

            if (logic == null)
            {
                BandText = string.Empty;
                HudLabel.Write(_bandLabel, BandText);
                return;
            }

            // 손님의 정적 데이터는 런타임에 바뀌지 않으므로 여기서 한 번만 만든다.
            // LateUpdate 에서 매 프레임 만들면 WebGL 에서 GC 스파이크가 그대로 히칭이 된다.
            var data = logic.State.Data;
            BandText = $"{CardCaption.NameOf(data)}\n{data.TargetingMin}~{data.TargetingMax}";
            HudLabel.Write(_bandLabel, BandText);

            ShowIcon(data);
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

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }

            _bandLabel = HudLabel.Resolve(transform, _bandLabel, BandLabelName);

            // 인스펙터가 비면 자기 하위에서 이름으로 찾는다. 씬 전역 탐색이 아니다 (§4.3).
            _saturationBar = Resolve(_saturationBar, SaturationBarName);
            _digestingBadge = Resolve(_digestingBadge, DigestingBadgeName);
        }

        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>자기 하위 계층에서만</b> 이름으로 찾는다.
        /// <see cref="HudLabel.Resolve"/> 와 같은 방식이며, 타입 둘 때문에 그 조각을
        /// 일반화하지는 않았다.
        /// </summary>
        private T Resolve<T>(T assigned, string childName) where T : Component
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

            // 포화도는 상태·대기와 다른 축이라 변화 감지에 걸리지 않는다. 바가 스스로
            // 값이 바뀐 프레임에만 색을 쓰므로 매 프레임 물어도 할당이 없다 (§4.3).
            if (_saturationBar != null)
            {
                _saturationBar.Refresh();
            }
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
            if (_digestingBadge != null)
            {
                if (state == CustomerState.Digesting)
                {
                    _digestingBadge.Show();
                }
                else
                {
                    _digestingBadge.Hide();
                }
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
