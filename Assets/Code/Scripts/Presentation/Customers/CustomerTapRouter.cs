using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 화면의 클릭을 «어느 자리의 손님» 으로 바꿔 넘긴다. <b>판정하지 않는다</b> — 어느
    /// 자리인지는 <see cref="SlotPicker"/> 가, 거기 누가 앉아 있는지는 자리가 답한다
    /// (<c>CLAUDE.md</c> §3.2).
    ///
    /// <para>
    /// <b>콜라이더를 도입하지 않는다.</b> 손님은 월드 스페이스라 uGUI 레이캐스트가 닿지 않지만,
    /// 자리는 서넛뿐이고 좌표가 이미 알려져 있어 거리 비교로 끝난다 — 물리를 들이면 씬에
    /// 관리할 것이 늘고 호출 시점 제약이 따라온다 (RULE-04).
    /// </para>
    /// <para>
    /// <b>드롭과 클릭을 «누르기 시작한 지점» 으로 가른다.</b> 카드를 자리에 떨어뜨리는 손짓과
    /// 자리를 눌러 보는 손짓은 <b>끝나는 좌표가 같다</b> — 뗀 시점만 보면 카드를 앉히는 순간
    /// 정보 창이 함께 뜬다. 카드 드래그는 반드시 카드(uGUI) 위에서 시작하므로, 누른 곳이
    /// uGUI 였는지 하나만 기억하면 둘이 갈린다. HUD·패널 위의 클릭도 같은 검사로 걸러진다.
    /// </para>
    /// </summary>
    public sealed class CustomerTapRouter : MonoBehaviour
    {
        /// <summary>
        /// 클릭이 자리로 인정되는 거리. 자리 간격이 2~8 유닛이라 이보다 크면 옆자리를
        /// 집어간다. 연출 수치이므로 SO 가 아니라 여기 있다 (M5 D7).
        /// </summary>
        [SerializeField, Min(0f)] private float _tapRadius = 1.2f;

        [SerializeField] private Camera _worldCamera;
        [SerializeField] private TableSlotView[] _slots;

        private Action<CustomerLogic, Vector2> _onTapped;
        private Action _onMissed;

        /// <summary>이번 누름이 uGUI 위에서 시작했나. 시작했으면 클릭으로 치지 않는다.</summary>
        private bool _pressBeganOverUi;

        /// <summary>uGUI 레이캐스트 결과를 담을 버퍼. 매번 새로 만들지 않는다.</summary>
        private readonly List<RaycastResult> _uiHits = new();

        /// <summary>지금까지 정보 창을 연 횟수. 검증용이다.</summary>
        public int TapCount { get; private set; }

        /// <summary>클릭 좌표를 변환할 카메라. 검증용이다.</summary>
        public Camera WorldCamera => _worldCamera;

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.
        /// </summary>
        /// <param name="worldCamera">
        /// <c>null</c> 이면 <b>이미 찾아 둔 것을 지우지 않는다.</b> 씬 진입점은 카메라를 들고
        /// 있지 않아 <c>null</c> 을 넘기는데, 그대로 대입하면 <see cref="Awake"/> 가 잡아 둔
        /// 참조가 날아가 클릭이 통째로 죽는다 — M5 에서 손패에 실제로 났던 사고다.
        /// </param>
        /// <param name="onTapped">
        /// 앉아 있는 손님과 <b>그 자리의 월드 좌표</b>를 받는다. 좌표가 함께 가지 않으면
        /// 정보 창이 «어느 손님인지» 는 알아도 «어디에 떠야 하는지» 를 알 수 없다.
        /// </param>
        /// <param name="onMissed">
        /// 손님이 아닌 곳을 눌렀을 때. <b>이것이 없으면 정보 창을 닫을 길이 없다</b> — 창에
        /// 닫기 버튼이 없고, 다른 창처럼 아이콘으로 토글되지도 않기 때문이다.
        /// </param>
        public void Bind(TableSlotView[] slots, Camera worldCamera,
                         Action<CustomerLogic, Vector2> onTapped, Action onMissed = null)
        {
            _slots = slots;
            _onTapped = onTapped;
            _onMissed = onMissed;

            if (worldCamera != null)
            {
                _worldCamera = worldCamera;
            }
        }

        /// <summary>
        /// 한 점을 눌렀을 때의 처리. <b>포인터를 거치지 않는 진입점</b>이라 테스트가 직접
        /// 부를 수 있다.
        /// </summary>
        /// <returns>앉아 있는 손님을 찾아 넘겼으면 <c>true</c>.</returns>
        public bool TapAt(Vector2 worldPoint)
        {
            if (_slots == null || _onTapped == null)
            {
                return false;
            }

            var slot = SlotPicker.Pick(worldPoint, _slots, _tapRadius);
            var occupant = slot != null ? slot.Occupant : null;
            if (occupant == null || occupant.Logic == null)
            {
                // 빈 자리도 «다른 곳» 이다. 열려 있던 창은 여기서 닫힌다.
                _onMissed?.Invoke();
                return false;
            }

            TapCount++;

            // 누른 좌표가 아니라 <b>자리의 좌표</b>를 넘긴다. 손끝을 기준으로 삼으면 같은
            // 손님을 두 번 눌러도 창이 조금씩 다른 데 뜬다.
            _onTapped(occupant.Logic, slot.transform.position);
            return true;
        }

        private void Awake()
        {
            ResolveCamera();
        }

        /// <summary>
        /// 누름의 시작과 끝을 함께 본다. <b>레거시 <c>Input</c> 을 쓰지 않는다</b> —
        /// 이 프로젝트는 <c>ENABLE_LEGACY_INPUT_MANAGER</c> 가 정의돼 있지 않아 그쪽은
        /// 런타임에 예외를 던진다 (<c>.claude/domain/presentation-and-audio.md</c> §6).
        /// </summary>
        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                _pressBeganOverUi = IsOverUi(pointer.position.ReadValue());
            }

            if (!pointer.press.wasReleasedThisFrame || _pressBeganOverUi)
            {
                return;
            }

            if (_worldCamera == null)
            {
                return;
            }

            TapAt(_worldCamera.ScreenToWorldPoint(pointer.position.ReadValue()));
        }

        /// <summary>
        /// 이 화면 좌표가 uGUI 위인가.
        ///
        /// <para>
        /// <b><c>IsPointerOverGameObject()</c> 를 쓰지 않는다.</b> 그것은 입력 모듈이 그 프레임에
        /// 이미 돌았는지에 달려 있어 <b>스크립트 실행 순서에 의존</b>한다 — 우리 <c>Update</c> 가
        /// 먼저 돌면 «uGUI 위가 아니다» 가 나오고, 그 순서는 씬마다 다르다. 직접 레이캐스트하면
        /// 그 의존이 사라진다.
        /// </para>
        /// <para>
        /// <c>EventSystem</c> 이 없는 판에서는 «위가 아니다» 로 본다 — 없다는 이유로 클릭을
        /// 통째로 막으면 원인을 찾기 어렵다.
        /// </para>
        /// </summary>
        private bool IsOverUi(Vector2 screenPoint)
        {
            var events = EventSystem.current;
            if (events == null)
            {
                return false;
            }

            // 목록을 재사용한다. 누를 때만 도는 경로라 매 프레임은 아니지만, 새로 만들면
            // 클릭할 때마다 쓰레기가 는다 (§4.3).
            _uiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = screenPoint }, _uiHits);
            return _uiHits.Count > 0;
        }

        /// <summary>
        /// 카메라가 없으면 주 카메라로 대신한다. <c>Awake</c> 순서에 기대지 않도록
        /// <see cref="Bind"/> 뒤에도 한 번 더 볼 수 있게 열어 둔다.
        /// </summary>
        private void ResolveCamera()
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }
    }
}
