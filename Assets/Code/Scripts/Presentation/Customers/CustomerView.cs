using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 하나의 화면 표현. <see cref="CustomerLogic"/> 에 위임만 한다 (<c>CLAUDE.md</c> §3.2).
    /// 자격·배정 판단을 여기에 두지 않는다.
    ///
    /// <para>
    /// 상태에 따라 색만 바꾼다. <b>판정하지 않는다</b> — 상태는 식욕 상태 머신이 정하고
    /// 이 뷰는 결과를 읽기만 한다. 색 블록은 M5(아트)까지의 placeholder 다.
    /// </para>
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _eatingColor = new(1f, 0.85f, 0.3f);
        [SerializeField] private Color _digestingColor = new(0.5f, 0.55f, 0.65f);

        private CustomerState _shownState = CustomerState.Idle;

        /// <summary>이 뷰가 그리고 있는 손님 로직.</summary>
        public CustomerLogic Logic { get; private set; }

        /// <summary>지금 화면에 반영돼 있는 상태. 검증용이다.</summary>
        public CustomerState ShownState => _shownState;

        /// <summary>배치 시점에 로직을 물린다. 뷰가 로직을 스스로 만들지 않는다.</summary>
        public void Bind(CustomerLogic logic)
        {
            Logic = logic;

            if (logic != null)
            {
                Apply(logic.State.State);
            }
        }

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 상태가 <b>바뀐 프레임에만</b> 색을 쓴다. 매 프레임 대입하면 렌더러가 머티리얼
        /// 프로퍼티를 계속 갱신해 배포 타깃(WebGL)에서 손해다.
        /// </summary>
        private void LateUpdate()
        {
            if (Logic == null)
            {
                return;
            }

            var state = Logic.State.State;
            if (state != _shownState)
            {
                Apply(state);
            }
        }

        private void Apply(CustomerState state)
        {
            _shownState = state;

            if (_body == null)
            {
                return;
            }

            _body.color = state switch
            {
                CustomerState.Eating => _eatingColor,
                CustomerState.Digesting => _digestingColor,
                _ => _idleColor
            };
        }
    }
}
