using System.Collections.Generic;
using SushiDefense.Data;
using SushiDefense.Scoring;
using UnityEngine;
using UnityEngine.EventSystems;

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
    /// <remarks>
    /// <b>평소에는 접혀 있다.</b> 카드가 128×192 가 되면서 손패가 화면을 먹으므로, 손이
    /// 오기 전에는 손잡이만 남기고 내려 둔다.
    ///
    /// <para>
    /// <b>움직이는 것은 컨테이너이고 카드는 제자리에 있는다.</b> 카드를 직접 옮기면 드래그가
    /// 돌아갈 자리(<c>CustomerCardDrag</c> 가 <c>Resolve</c> 시점에 붙잡는 좌표)가 접힌
    /// 위치를 가리켜, <b>펼친 채 끌어 놓았을 때 카드가 화면 밖으로 돌아간다.</b> 부모를
    /// 움직이면 카드의 로컬 좌표가 변하지 않아 이 문제가 아예 생기지 않는다.
    /// </para>
    /// </remarks>
    public sealed class CustomerHandView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

        /// <summary>
        /// 접힘·펼침으로 움직일 대상. <b>카드가 아니라 그 부모다.</b>
        /// 비어 있으면 자기 자신을 움직인다 — 씬에 컨테이너를 아직 넣지 않은 동안에도
        /// 동작하게 두되, 그 상태에서는 카드가 함께 움직이므로 조립이 끝나면 물려야 한다.
        /// </summary>
        [SerializeField] private RectTransform _tray;

        /// <summary>
        /// 접혔을 때 컨테이너가 내려가는 자리. 펼치면 0 이다. 손잡이로 남길 높이만큼만
        /// 화면에 남도록 카드 높이에서 빼서 정한다 — 연출 수치라 여기 있다 (M5 D7).
        /// </summary>
        [SerializeField] private float _collapsedOffsetY = -144f;

        /// <summary>펼쳐지는 데 걸리는 시간(초). 0 이면 즉시.</summary>
        [SerializeField, Min(0f)] private float _slideSeconds = 0.12f;

        /// <summary>
        /// 지금 보고 있는 지갑. 잔액이 바뀌면 카드를 다시 평가한다.
        ///
        /// <para>
        /// 이것이 없으면 <b>영입 재화가 쌓여도 카드가 계속 흐린 채로 남는다.</b> 카드가
        /// 다시 평가되는 시점이 «명부를 물릴 때» 와 «배치에 성공했을 때» 둘뿐이라, 비싼
        /// 손님은 <i>다른 손님을 먼저 앉혀야</i> 풀렸다 — 리포트의 «기본 손님 배치 후 다시
        /// 배치 가능해진다» 가 정확히 그 증상이다.
        /// </para>
        /// </summary>
        private RecruitWallet _wallet;

        /// <summary>지금 손님이 물려 있는 카드 수. 검증용이다.</summary>
        public int ShownCardCount { get; private set; }

        /// <summary>실제로 앉힌 횟수. 검증용이다.</summary>
        public int PlacedCount { get; private set; }

        /// <summary>드롭 좌표를 변환할 카메라. 검증용이다.</summary>
        public Camera WorldCamera => _worldCamera;

        /// <summary>지금 펼쳐져 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsExpanded { get; private set; }

        /// <summary>
        /// 접거나 펼친다. <b>포인터를 거치지 않는 진입점</b>이라 테스트·터치 폴백이 쓴다.
        /// </summary>
        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            SetExpanded(true);
        }

        /// <summary>
        /// 손이 나가면 접는다. <b>단 끌고 있는 중에는 접지 않는다</b> — 카드를 자리로
        /// 가져가려면 손패 밖으로 나가야 하는데, 거기서 접히면 카드가 손가락 아래에서
        /// 함께 내려간다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsDraggingAnyCard())
            {
                return;
            }

            SetExpanded(false);
        }

        /// <summary>
        /// <b>호버는 터치에 없다.</b> 배포 타깃이 웹이라 모바일 브라우저가 사정권이고, 탭으로
        /// 여는 길이 없으면 그쪽에서는 손패가 영영 접힌 채로 남는다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            SetExpanded(!IsExpanded);
        }

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
        /// 잔액을 지켜볼 지갑을 물린다. <b>이미 보고 있으면 먼저 끊는다</b> — 판이 바뀌면
        /// 지갑도 새로 열리므로, 끊지 않으면 죽은 지갑의 구독이 그대로 쌓인다
        /// (<c>.claude/rules/scripts.md</c> §6).
        /// </summary>
        /// <param name="wallet"><c>null</c> 이면 보던 것을 놓기만 한다.</param>
        public void Watch(RecruitWallet wallet)
        {
            Unwatch();

            _wallet = wallet;

            if (_wallet != null)
            {
                _wallet.BalanceChanged += OnBalanceChanged;
            }
        }

        /// <summary>지갑 구독을 끊는다.</summary>
        public void Unwatch()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
                _wallet = null;
            }
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

        private void OnDestroy()
        {
            Unwatch();
        }

        /// <summary>
        /// 목표 위치로 컨테이너를 옮긴다.
        ///
        /// <para>
        /// <b>코루틴이 아니라 <c>Update</c> 안의 보간이다.</b> 코루틴의 <c>WaitForSeconds</c> 는
        /// <c>Time.timeScale</c> 에 묶이는데, 이 프로젝트는 그것을 건드리지 않는 대신 멈춤을
        /// 따로 둔다 (M6 D4) — 손패는 멈춘 동안에도 움직여야 하므로 그 게이트 밖이다.
        /// </para>
        /// <para>
        /// <b>목표에 닿으면 멈춘다.</b> 매 프레임 같은 값을 계속 대입하면 레이아웃이 매번
        /// 더럽혀진다. <c>Vector2</c> 는 구조체라 이 경로에 할당이 없다 (§4.3).
        /// </para>
        /// </summary>
        private void Update()
        {
            var tray = _tray != null ? _tray : transform as RectTransform;
            if (tray == null)
            {
                return;
            }

            var target = IsExpanded ? 0f : _collapsedOffsetY;
            var current = tray.anchoredPosition;
            if (Mathf.Approximately(current.y, target))
            {
                return;
            }

            var next = _slideSeconds <= 0f
                ? target
                : Mathf.MoveTowards(current.y, target,
                                    Mathf.Abs(_collapsedOffsetY) / _slideSeconds * Time.deltaTime);

            tray.anchoredPosition = new Vector2(current.x, next);
        }

        /// <summary>
        /// 지금 끌리고 있는 카드가 있나. <b>카드에 묻는다</b> — 손패가 끌기 상태를 따로 들면
        /// 진실이 둘이 되고, 드래그를 시작·종료하는 곳은 카드다.
        /// </summary>
        private bool IsDraggingAnyCard()
        {
            if (_cards == null)
            {
                return false;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null && _cards[i].IsDragging)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 잔액이 <b>바뀐 순간에만</b> 돈다 — 지갑이 값이 실제로 달라질 때만 알린다.
        /// 매 프레임 다시 평가하면 손님 수 × 자리 수 검사가 프레임 예산을 먹는다 (§4.3).
        /// </summary>
        private void OnBalanceChanged(int balance)
        {
            Refresh();
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
