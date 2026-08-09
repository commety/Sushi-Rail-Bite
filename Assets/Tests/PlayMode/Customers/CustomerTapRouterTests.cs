using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 클릭을 «어느 자리의 손님» 으로 옮기는 껍데기. <b>판정하지 않는다</b> — 어느 자리인지는
    /// <c>SlotPicker</c> 가, 거기 누가 앉아 있는지는 자리가 답한다.
    ///
    /// <para>
    /// <b>콜라이더를 쓰지 않는다.</b> 자리는 서넛뿐이고 좌표가 알려져 있어 거리 비교로
    /// 끝난다 — 물리를 들이면 호출 시점 제약(RULE-04)과 씬에 관리할 것이 함께 따라온다.
    /// </para>
    /// </summary>
    public sealed class CustomerTapRouterTests : InputTestFixture
    {
        private readonly List<Object> _garbage = new();

        private CustomerTapRouter _router;
        private TableSlotView[] _slots;
        /// <summary>
        /// 넘어온 (손님, 좌표) 쌍. <b>좌표까지 모은다</b> — 손님만 보면 창이 어디에
        /// 떠야 하는지가 검증에서 빠진다.
        /// </summary>
        private readonly List<(CustomerLogic Logic, Vector2 Point)> _tapped = new();

        /// <summary>
        /// <b>픽스처를 상속한다.</b> 그것 없이 <c>QueueStateEvent</c> 로 가상 마우스를 흔들면
        /// 이벤트가 장치에 <b>아예 반영되지 않는다</b> — 실측했다(누름 0회, 레벨 <c>000</c>).
        /// 네이티브 백엔드가 상태를 소유한 채라, 입력 시스템을 테스트 상태로 바꿔 주는 이
        /// 픽스처를 지나야 큐가 살아난다.
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            _tapped.Clear();

            _slots = new[]
            {
                NewSlot(0, new Vector2(-4f, -2f), occupied: true),
                NewSlot(1, new Vector2(4f, -2f), occupied: false)
            };

            _router = NewObject("TapRouter").AddComponent<CustomerTapRouter>();
            _router.Bind(_slots, NewCamera(), Record);
        }

        public override void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();

            // 장치 해제는 픽스처가 한다 — 여기서 먼저 지우면 그쪽이 두 번 지운다.
            base.TearDown();
        }

        [Test]
        public void TapAt_OccupiedSlot_OpensTheInspector()
        {
            var opened = _router.TapAt(_slots[0].transform.position);

            Assert.IsTrue(opened);
            Assert.AreEqual(1, _tapped.Count);
            Assert.AreSame(_slots[0].Occupant.Logic, _tapped[0].Logic);
            Assert.AreEqual(1, _router.TapCount);
        }

        /// <summary>
        /// <b>자리의 좌표가 함께 간다.</b> 손님만 넘기면 정보 창이 «어느 손님인지» 는 알아도
        /// «어디에 떠야 하는지» 를 모른다.
        ///
        /// <para>
        /// <b>누른 좌표가 아니라 자리의 좌표</b>다. 그래서 자리에서 조금 빗나가게 눌러도 같은
        /// 값이 나온다 — 손끝을 기준으로 삼으면 같은 손님을 두 번 눌러도 창이 조금씩 다른
        /// 데 뜬다. 두 좌표가 <b>다른</b> 지점을 골라야 이 구분이 드러난다.
        /// </para>
        /// </summary>
        [Test]
        public void TapAt_OffCenterOfTheSlot_ReportsTheSlotPosition()
        {
            var seat = (Vector2)_slots[0].transform.position;

            _router.TapAt(seat + new Vector2(0.5f, 0.3f));

            Assert.AreEqual(seat, _tapped[0].Point,
                            "누른 좌표를 그대로 넘겼다 — 창이 손끝을 따라 흔들린다");
        }

        /// <summary>
        /// 빈 자리는 열 것이 없다. <b>여기서 «자리를 눌렀다» 만 보고 열면</b> 빈 창이 뜬다.
        /// </summary>
        [Test]
        public void TapAt_EmptySlot_DoesNothing()
        {
            Assert.IsFalse(_router.TapAt(_slots[1].transform.position));
            Assert.IsEmpty(_tapped);
        }

        [Test]
        public void TapAt_FarFromEverySlot_DoesNothing()
        {
            Assert.IsFalse(_router.TapAt(new Vector2(100f, 100f)));
            Assert.IsEmpty(_tapped);
        }

        // ── 다른 곳을 누르면 닫힌다 ────────────────────────────────────────
        //
        // 정보 창에는 닫기 버튼이 없고 아이콘으로 토글되지도 않는다. 이 경로가 없으면
        // 창이 **영영 안 닫혀서** 보상 화면 위에도, 다음 판에도 그대로 남는다.

        [Test]
        public void TapAt_EmptySlot_ReportsAMiss()
        {
            var missed = 0;
            _router.Bind(_slots, null, Record, () => missed++);

            _router.TapAt(_slots[1].transform.position);

            Assert.AreEqual(1, missed, "빈 자리를 눌렀는데 «다른 곳» 으로 안 쳤다");
        }

        [Test]
        public void TapAt_FarFromEverySlot_ReportsAMiss()
        {
            var missed = 0;
            _router.Bind(_slots, null, Record, () => missed++);

            _router.TapAt(new Vector2(100f, 100f));

            Assert.AreEqual(1, missed);
        }

        /// <summary>
        /// 반례. <b>손님을 눌렀을 때는 «다른 곳» 이 아니다</b> — 이것이 없으면 «항상 닫는»
        /// 구현이 통과하고, 열자마자 닫히는 창이 된다.
        /// </summary>
        [Test]
        public void TapAt_OccupiedSlot_DoesNotReportAMiss()
        {
            var missed = 0;
            _router.Bind(_slots, null, Record, () => missed++);

            _router.TapAt(_slots[0].transform.position);

            Assert.AreEqual(0, missed, "손님을 눌렀는데 창을 닫았다");
        }

        /// <summary>
        /// 자리 간격이 8 유닛이라, 반경 안에 있어도 <b>가장 가까운</b> 자리가 이겨야 한다.
        /// 목록 순서가 이기는 구현이면 왼쪽 자리가 항상 열린다.
        /// </summary>
        [Test]
        public void TapAt_NearerToTheSecondSlot_PicksThatOne()
        {
            var occupiedSecond = NewSlot(1, new Vector2(4f, -2f), occupied: true);
            _router.Bind(new[] { _slots[0], occupiedSecond }, NewCamera(), Record);

            _router.TapAt(new Vector2(3.8f, -2f));

            Assert.AreEqual(1, _tapped.Count);
            Assert.AreSame(occupiedSecond.Occupant.Logic, _tapped[0].Logic);
        }

        [Test]
        public void TapAt_WithoutBinding_DoesNotThrow()
        {
            var bare = NewObject("Bare").AddComponent<CustomerTapRouter>();

            Assert.IsFalse(bare.TapAt(Vector2.zero));
        }

        /// <summary>
        /// 씬 진입점은 카메라를 들고 있지 않아 <c>null</c> 을 넘긴다. 그대로 대입하면
        /// <c>Awake</c> 가 잡아 둔 참조가 날아가 <b>클릭이 통째로 죽는다</b> — M5 에서 손패에
        /// 실제로 났던 사고이며, <c>TapAt</c> 을 직접 부르는 테스트는 좌표 변환을 지나쳐
        /// 그것을 못 잡는다.
        /// </summary>
        [Test]
        public void Bind_WithNullCamera_KeepsResolvedCamera()
        {
            var mainCamera = NewObject("Main Camera");
            mainCamera.tag = "MainCamera";
            mainCamera.AddComponent<Camera>();

            var router = NewObject("TapRouter2").AddComponent<CustomerTapRouter>();
            router.Bind(_slots, null, Record);

            Assert.IsNotNull(router.WorldCamera, "씬 진입점이 넘긴 null 이 카메라를 지웠다");
        }

        // ── 바깥 껍데기 (실제 포인터를 흘려보낸다) ─────────────────────────
        //
        // 위 테스트들은 TapAt 을 직접 부르므로 입력 읽기와 좌표 변환을 지나친다. 그 우회로가
        // 곧 사각지대다 — M5 에서 Enter·Esc 가 전멸했을 때 «코드도 테스트도 멀쩡한데 재생만
        // 죽는» 형태였고, 아무도 못 잡은 이유가 정확히 이것이다 (tests.md §1).

        /// <summary>
        /// 눌렀다 떼면 <b>Update 경로를 지나</b> 정보 창이 열린다.
        /// </summary>
        [UnityTest]
        public IEnumerator Update_PressedAndReleasedOverSlot_Taps()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Set(mouse.position, ScreenPointOf(_slots[0]));

            Press(mouse.leftButton);
            yield return null;

            Release(mouse.leftButton);
            yield return null;

            Assert.AreEqual(1, _router.TapCount, "포인터 경로가 TapAt 까지 닿지 않았다");
            Assert.AreSame(_slots[0].Occupant.Logic, _tapped[0].Logic);
        }

        /// <summary>
        /// <b>카드를 자리에 떨어뜨리는 손짓과 자리를 눌러 보는 손짓은 끝나는 좌표가 같다.</b>
        /// 뗀 시점만 보면 카드를 앉히는 순간 정보 창이 함께 뜬다 — 누른 지점이 uGUI 위였는지로
        /// 가른다.
        ///
        /// <para>
        /// 그래서 <b>누를 때만 uGUI 위</b>에 두고 뗄 때는 자리 위로 옮긴다. 둘 다 uGUI 위에
        /// 두면 «뗀 시점을 보는» 잘못된 구현으로도 통과한다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator Update_PressBeganOverUi_DoesNotTap()
        {
            var ui = NewFullScreenUi();
            var mouse = InputSystem.AddDevice<Mouse>();

            Set(mouse.position, ui.center);
            Press(mouse.leftButton);
            yield return null;

            // 카드는 놓이는 순간 포인터 아래에서 사라진다. 뗄 때는 자리 위이므로,
            // 뗀 시점만 보는 구현이면 여기서 정보 창이 열린다.
            ui.panel.SetActive(false);
            Set(mouse.position, ScreenPointOf(_slots[0]));
            Release(mouse.leftButton);
            yield return null;

            Assert.AreEqual(0, _router.TapCount, "카드를 앉히는 손짓이 정보 창을 열었다");
        }

        // ── 입력 조립 ──────────────────────────────────────────────────────

        private Vector2 ScreenPointOf(TableSlotView slot)
        {
            return _router.WorldCamera.WorldToScreenPoint(slot.transform.position);
        }

        /// <summary>
        /// 화면을 덮는 uGUI 하나. <c>IsPointerOverGameObject</c> 가 참이 되게 하려면
        /// <c>EventSystem</c> 과 레이캐스트를 받는 그래픽이 둘 다 필요하다.
        /// </summary>
        private (GameObject panel, Vector2 center) NewFullScreenUi()
        {
            var eventSystem = NewObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var canvasGo = NewObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Blocker", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            panel.AddComponent<Image>();

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return (panel, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private TableSlotView NewSlot(int index, Vector2 position, bool occupied)
        {
            var go = NewObject($"TableSlot{index}");
            go.transform.position = position;

            var seatGo = new GameObject("Customer");
            seatGo.transform.SetParent(go.transform, false);
            seatGo.AddComponent<SpriteRenderer>();
            var seat = seatGo.AddComponent<CustomerView>();

            var slot = go.AddComponent<TableSlotView>();
            slot.Initialize(index, index * 2f, seat);

            if (occupied)
            {
                slot.Occupy(NewLogic(), null);
            }

            return slot;
        }

        private CustomerLogic NewLogic()
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _garbage.Add(data);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(data);
            serialized.FindProperty("_maxSaturation").intValue = 5;
            serialized.FindProperty("_reach").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return new CustomerLogic(new CustomerRuntimeState(data, 0), 0f);
        }

        /// <summary>
        /// 2D 카메라. <b>기본값 그대로 쓰면 좌표 왕복이 깨진다</b> — 새 <c>Camera</c> 는
        /// 원근이고 위치가 원점이라, 같은 원점 평면에 있는 자리를 투영하면 거리가 0 이라
        /// 화면 좌표가 의미를 잃는다. 실제 씬처럼 직교로 두고 뒤로 물린다.
        /// </summary>
        private Camera NewCamera()
        {
            var camera = NewObject("Tap Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            return camera;
        }

        private void Record(CustomerLogic logic, Vector2 worldPoint)
        {
            _tapped.Add((logic, worldPoint));
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
