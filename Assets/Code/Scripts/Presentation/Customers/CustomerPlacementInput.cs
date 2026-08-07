using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 클릭을 자리 배치로 옮기는 얇은 껍데기. <b>판정하지 않는다</b> — 어느 자리를 눌렀는지는
    /// <see cref="SlotPicker"/>, 앉힐 수 있는지는 <see cref="CustomerPlacementService"/> 가 정한다.
    ///
    /// <para>
    /// <b>M5 의 최소 입력이다.</b> 명부에서 손님을 고르는 화면은 M6 의 몫이고, 여기서는
    /// 배치 껍데기가 이미 들고 있는 <c>PendingCustomer</c> 를 빈 자리에 앉히기만 한다.
    /// 이것이 없으면 M5 가 만든 소리·이펙트를 브라우저에서 관측할 방법이 없다.
    /// </para>
    /// <para>
    /// <c>UnityEngine.Input</c> 이 아니라 Input System 을 쓴다. 이 프로젝트는
    /// <c>ENABLE_LEGACY_INPUT_MANAGER</c> 가 정의되어 있지 않아 레거시 API 가 런타임에
    /// 예외를 던진다 — <c>InputBackendTests</c> 가 그 전제를 고정한다.
    /// </para>
    /// </summary>
    public sealed class CustomerPlacementInput : MonoBehaviour
    {
        [SerializeField] private CustomerPlacementController _controller;
        [SerializeField] private Camera _camera;
        [SerializeField] private TableSlotView[] _slots;

        /// <summary>
        /// 클릭이 자리로 인정되는 거리. 자리 간격이 2~4 유닛이라 이보다 크면 옆자리를
        /// 집어간다. 연출 수치이므로 SO 가 아니라 여기 있다 (README D7).
        /// </summary>
        [SerializeField, Min(0f)] private float _pickRadius = 1.2f;

        /// <summary>실제로 앉힌 횟수. 검증용이다.</summary>
        public int PlacedCount { get; private set; }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(CustomerPlacementController controller, Camera camera,
                               TableSlotView[] slots)
        {
            _controller = controller;
            _slots = slots;

            // 넘어온 카메라가 없으면 이미 찾아 둔 것을 지우지 않는다. 씬 진입점은 카메라를
            // 들고 있지 않아 null 을 넘기는데, 그대로 대입하면 Awake 가 잡아 둔 참조가
            // 날아가 클릭이 통째로 죽는다 — 테스트는 ClickAt 을 직접 불러 이 경로를
            // 지나치므로 브라우저에서야 드러났다.
            if (camera != null)
            {
                _camera = camera;
            }
        }

        /// <summary>
        /// 한 점을 눌렀을 때의 처리. <b>마우스를 거치지 않는 진입점</b>이라 테스트가 직접
        /// 부를 수 있다.
        /// </summary>
        public bool ClickAt(Vector2 worldPoint)
        {
            if (_controller == null)
            {
                return false;
            }

            var slot = SlotPicker.Pick(worldPoint, _slots, _pickRadius);
            if (slot == null || !_controller.TryPlaceAt(slot))
            {
                return false;
            }

            PlacedCount++;
            return true;
        }

        private void Awake()
        {
            ResolveCamera();
        }

        /// <summary>
        /// 카메라가 없으면 주 카메라로 대신한다. <c>Awake</c> 순서에 기대지 않도록
        /// 클릭 시점에도 한 번 더 본다.
        /// </summary>
        private void ResolveCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            ResolveCamera();
            if (_camera == null)
            {
                return;
            }

            ClickAt(_camera.ScreenToWorldPoint(mouse.position.ReadValue()));
        }
    }
}
