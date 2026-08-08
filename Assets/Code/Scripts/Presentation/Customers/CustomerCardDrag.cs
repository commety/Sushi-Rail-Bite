using SushiDefense.Data;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 카드 하나의 끌기. <b>판정하지 않는다</b> — 좌표를 손패에 넘기기만 한다.
    ///
    /// <para>
    /// 어느 자리에 떨어졌는지는 <c>SlotPicker</c> 가, 거기 놓을 수 있는지는
    /// <c>CustomerPlacementService</c> 가 답한다. 여기서 되물으면 두 판정이 어긋날 수 있고
    /// EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// <para>
    /// <b>놓아도 카드는 남는다.</b> 명부는 "앉힐 수 있는 종류" 이고 같은 유형을 여러 자리에
    /// 앉힐 수 있으므로, 카드는 소모품이 아니라 항상 제자리로 돌아온다.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public sealed class CustomerCardDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>놓을 수 없는 카드의 투명도. 연출 수치라 여기 있다 (M5 D7).</summary>
        [SerializeField, Range(0f, 1f)] private float _unavailableAlpha = 0.4f;

        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>참조를 이미 챙겼나.</summary>
        private bool _resolved;

        private CustomerHandView _hand;
        private Camera _worldCamera;
        private RectTransform _rect;
        private Vector2 _home;

        /// <summary>이 카드가 들고 있는 손님. 비어 있으면 <c>null</c>.</summary>
        public CustomerData Customer { get; private set; }

        /// <summary>이 카드의 그림·문구를 그리는 쪽.</summary>
        public CardView Card { get; private set; }

        /// <summary>지금 놓을 수 있는 카드인가. 표시 상태이지 판정이 아니다.</summary>
        public bool IsAvailable { get; private set; } = true;

        /// <summary>지금 끌리고 있는가. 검증용이다.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>
        /// 손패가 카드에 손님을 물린다. <paramref name="customer"/> 가 <c>null</c> 이면
        /// 빈 카드가 된다.
        /// </summary>
        public void Bind(CustomerHandView hand, CustomerData customer, Camera worldCamera)
        {
            Resolve();

            _hand = hand;
            _worldCamera = worldCamera;
            Customer = customer;

            if (customer != null)
            {
                Card.Show(customer);
            }
            else
            {
                Card.Clear();
            }

            SetAvailable(true);
        }

        /// <summary>
        /// 놓을 수 있는지를 화면에 반영한다. <b>판단은 손패가 한다</b> — 카드가 잔액을
        /// 되물으면 배치 규칙이 두 곳에 살게 된다.
        /// </summary>
        public void SetAvailable(bool available)
        {
            IsAvailable = available;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = available ? 1f : _unavailableAlpha;
            }
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
            {
                return;
            }

            IsDragging = true;
            _home = _rect.anchoredPosition;
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            // 카드는 Canvas 좌표계에서 움직인다 — 여기에 월드 변환이 끼면 손가락과
            // 카드가 어긋난다. 변환은 드롭 판정에서 한 번만 한다.
            _rect.anchoredPosition += eventData.delta;
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            IsDragging = false;

            // 놓였든 아니든 카드는 제자리로 돌아온다. 실패한 드롭에 이유를 붙이지 않는 것은
            // 잔액 부족·자리 점유·한도 초과가 각각 다른 문구를 요구하는 별도 기획이기 때문이다.
            _rect.anchoredPosition = _home;

            if (_hand == null || _worldCamera == null)
            {
                return;
            }

            _hand.DropAt(Customer, _worldCamera.ScreenToWorldPoint(eventData.position));
        }

        private void Awake()
        {
            Resolve();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라 <see cref="Bind"/>
        /// 에서도 부르므로 <b>실행 순서에 기대지 않는다.</b>
        ///
        /// <para>
        /// 씬 진입점은 <c>Awake</c> 안에서 판을 통째로 세우고 <b>마지막에</b> 손패를 물린다.
        /// 그때 이 카드의 <see cref="Awake"/> 가 아직 안 돌았으면 <see cref="Card"/> 가
        /// <c>null</c> 이라 <see cref="Bind"/> 가 그 자리에서 터진다 — WebGL 에서 실제로 났고,
        /// 손패 배선이 <c>Build</c> 의 마지막 줄이라 <b>다른 것은 전부 멀쩡한 채 손패만</b>
        /// 빈 카드로 남았다. 예외 하나가 콘솔에 조용히 찍힐 뿐이다.
        /// </para>
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            Card = GetComponent<CardView>();
            _rect = GetComponent<RectTransform>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _home = _rect != null ? _rect.anchoredPosition : Vector2.zero;
        }

        private bool CanDrag()
        {
            return Customer != null && IsAvailable && _rect != null;
        }
    }
}
