using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;
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
    public sealed class CustomerTapRouterTests
    {
        private readonly List<Object> _garbage = new();

        private CustomerTapRouter _router;
        private TableSlotView[] _slots;
        private readonly List<CustomerLogic> _tapped = new();

        [SetUp]
        public void SetUp()
        {
            _tapped.Clear();

            _slots = new[]
            {
                NewSlot(0, new Vector2(-4f, -2f), occupied: true),
                NewSlot(1, new Vector2(4f, -2f), occupied: false)
            };

            _router = NewObject("TapRouter").AddComponent<CustomerTapRouter>();
            _router.Bind(_slots, NewCamera(), _tapped.Add);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        [Test]
        public void TapAt_OccupiedSlot_OpensTheInspector()
        {
            var opened = _router.TapAt(_slots[0].transform.position);

            Assert.IsTrue(opened);
            Assert.AreEqual(1, _tapped.Count);
            Assert.AreSame(_slots[0].Occupant.Logic, _tapped[0]);
            Assert.AreEqual(1, _router.TapCount);
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

        /// <summary>
        /// 자리 간격이 8 유닛이라, 반경 안에 있어도 <b>가장 가까운</b> 자리가 이겨야 한다.
        /// 목록 순서가 이기는 구현이면 왼쪽 자리가 항상 열린다.
        /// </summary>
        [Test]
        public void TapAt_NearerToTheSecondSlot_PicksThatOne()
        {
            var occupiedSecond = NewSlot(1, new Vector2(4f, -2f), occupied: true);
            _router.Bind(new[] { _slots[0], occupiedSecond }, NewCamera(), _tapped.Add);

            _router.TapAt(new Vector2(3.8f, -2f));

            Assert.AreEqual(1, _tapped.Count);
            Assert.AreSame(occupiedSecond.Occupant.Logic, _tapped[0]);
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
            router.Bind(_slots, null, _tapped.Add);

            Assert.IsNotNull(router.WorldCamera, "씬 진입점이 넘긴 null 이 카메라를 지웠다");
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

        private Camera NewCamera()
        {
            return NewObject("Tap Camera").AddComponent<Camera>();
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
