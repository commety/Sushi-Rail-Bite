using System.Collections.Generic;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 명부를 카드로 늘어놓는 손패. 카드를 끌어다 자리에 떨어뜨리면 손님이 앉는다.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 어느 자리에 떨어졌는지는 <see cref="SlotPicker"/> 가,
    /// 놓을 수 있는지는 <c>CustomerPlacementService</c> 가 답한다. 여기서 되물으면 두 판정이
    /// 어긋날 수 있고 EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// <para>
    /// <b>카드를 만들지 않는다.</b> 필요한 만큼 미리 놓아 두고 켜고 끈다 — 프로덕션에서
    /// 오브젝트를 새로 만드는 지점은 풀 하나로 유지한다 (§3.4). 명부가 카드보다 길면
    /// <b>있는 만큼만 그리고 예외를 내지 않는다</b>: 씬을 조금씩 조립하는 동안 흔한
    /// 상태이고, 여기서 터지면 나머지를 아무것도 확인할 수 없다.
    /// </para>
    /// </summary>
    public sealed class CustomerHandView : MonoBehaviour
    {
        [SerializeField] private CustomerPlacementController _controller;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private TableSlotView[] _slots;
        [SerializeField] private CustomerCardDrag[] _cards;

        /// <summary>
        /// 드롭이 자리로 인정되는 거리. 자리 간격이 2~4 유닛이라 이보다 크면 옆자리를
        /// 집어간다. 연출 수치이므로 SO 가 아니라 여기 있다 (M5 D7).
        /// </summary>
        [SerializeField, Min(0f)] private float _dropRadius = 1.2f;

        /// <summary>지금 손님이 물려 있는 카드 수. 검증용이다.</summary>
        public int ShownCardCount { get; private set; }

        /// <summary>실제로 앉힌 횟수. 검증용이다.</summary>
        public int PlacedCount { get; private set; }

        /// <summary>드롭 좌표를 변환할 카메라. 검증용이다.</summary>
        public Camera WorldCamera => _worldCamera;

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.
        /// </summary>
        /// <param name="worldCamera">
        /// <c>null</c> 이면 <b>이미 찾아 둔 것을 지우지 않는다.</b> 씬 진입점은 카메라를
        /// 들고 있지 않아 <c>null</c> 을 넘기는데, 그대로 대입하면 <see cref="Awake"/> 가
        /// 잡아 둔 참조가 날아가 드래그가 통째로 죽는다 — M5 에서 실제로 났던 사고이며,
        /// 테스트는 <see cref="DropAt"/> 을 직접 불러 이 경로를 지나친다.
        /// </param>
        public void Initialize(CustomerPlacementController controller, Camera worldCamera,
                               TableSlotView[] slots)
        {
            _controller = controller;
            _slots = slots;

            if (worldCamera != null)
            {
                _worldCamera = worldCamera;
            }
        }

        /// <summary>인스펙터 없이 카드 목록을 물린다. 테스트용 진입점이다.</summary>
        public void InitializeCards(CustomerCardDrag[] cards)
        {
            _cards = cards;
        }

        /// <summary>
        /// 명부를 카드에 늘어놓는다. <b>여러 번 불러도 겹치지 않는다</b> — 남는 카드는
        /// 비워지므로 명부가 줄어도 직전 손님이 남지 않는다.
        /// </summary>
        public void Bind(IReadOnlyList<CustomerData> roster)
        {
            ShownCardCount = 0;

            if (_cards == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                var member = roster != null && i < roster.Count ? roster[i] : null;
                card.Bind(this, member, _worldCamera);

                if (member != null)
                {
                    ShownCardCount++;
                }
            }

            Refresh();
        }

        /// <summary>
        /// 지금 놓을 수 있는 카드를 다시 표시한다. 잔액·한도가 바뀌면 부르는 쪽이 부른다.
        ///
        /// <para>
        /// <b>자리를 지목하지 않고 묻는다.</b> "어느 자리에도 못 놓는가" 를 보는 것이라
        /// 빈 자리 하나만 찾으면 되며, 그 판단도 서비스가 한다.
        /// </para>
        /// </summary>
        public void Refresh()
        {
            if (_cards == null)
            {
                return;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card != null)
                {
                    card.SetAvailable(CanPlaceAnywhere(card.Customer));
                }
            }
        }

        /// <summary>
        /// 한 점에 카드를 떨어뜨렸을 때의 처리. <b>포인터를 거치지 않는 진입점</b>이라
        /// 테스트가 직접 부를 수 있다.
        /// </summary>
        public bool DropAt(CustomerData customer, Vector2 worldPoint)
        {
            if (customer == null || _controller == null)
            {
                return false;
            }

            var slot = SlotPicker.Pick(worldPoint, _slots, _dropRadius);
            if (slot == null || !_controller.TryPlace(customer, slot))
            {
                return false;
            }

            PlacedCount++;
            Refresh();
            return true;
        }

        /// <summary>이 자리의 카드가 들고 있는 손님. 비어 있으면 <c>null</c>. 검증용이다.</summary>
        public CustomerData CustomerAt(int index)
        {
            return _cards != null && index >= 0 && index < _cards.Length && _cards[index] != null
                ? _cards[index].Customer
                : null;
        }

        /// <summary>이 자리의 카드가 지금 놓을 수 있는 상태인가. 검증용이다.</summary>
        public bool IsAvailableAt(int index)
        {
            return _cards != null && index >= 0 && index < _cards.Length && _cards[index] != null
                   && _cards[index].IsAvailable;
        }

        private void Awake()
        {
            ResolveCamera();

            if (_cards == null || _cards.Length == 0)
            {
                _cards = GetComponentsInChildren<CustomerCardDrag>(true);
            }
        }

        /// <summary>
        /// 카메라가 없으면 주 카메라로 대신한다. <c>Awake</c> 순서에 기대지 않도록
        /// <see cref="Initialize"/> 뒤에도 한 번 더 볼 수 있게 열어 둔다.
        /// </summary>
        private void ResolveCamera()
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }

        /// <summary>
        /// 이 손님을 <b>어느 자리에든</b> 놓을 수 있는가. 잔액·한도가 막으면 어느 자리든
        /// 거부되고, 자리가 전부 찼으면 빈 자리가 없다.
        /// </summary>
        private bool CanPlaceAnywhere(CustomerData customer)
        {
            if (customer == null || _controller == null || _slots == null)
            {
                return false;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot != null && _controller.CanPlace(customer, slot))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
