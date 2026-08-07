using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 클릭 한 점이 <b>어느 자리를 가리키는지</b>만 본다. 앉힐 수 있는지는 배치 서비스가
    /// 정하고 그쪽 테스트가 이미 덮는다.
    ///
    /// <para>
    /// 순수 계산이지만 <c>TableSlotView</c> 가 <c>MonoBehaviour</c> 라 PlayMode 에 둔다 —
    /// 자리의 좌표가 <c>Transform</c> 에 있기 때문이다.
    /// </para>
    /// </summary>
    public sealed class SlotPickerTests
    {
        private const float Radius = 1.2f;

        private readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        [Test]
        public void Pick_PointOnSlot_ReturnsThatSlot()
        {
            var near = Slot(new Vector2(-4f, -2f));
            var far = Slot(new Vector2(4f, -2f));

            var picked = SlotPicker.Pick(new Vector2(-4f, -2f), new[] { near, far }, Radius);

            Assert.AreSame(near, picked);
        }

        [Test]
        public void Pick_BetweenTwoSlots_ReturnsTheNearer()
        {
            // "가까운 쪽" 을 보려면 둘 다 반경 안에 있어야 한다. 하나만 닿으면
            // 거리 비교 없이 그것만 남는 구현으로도 통과한다.
            var left = Slot(new Vector2(0f, 0f));
            var right = Slot(new Vector2(1.5f, 0f));

            var picked = SlotPicker.Pick(new Vector2(1.0f, 0f), new[] { left, right }, 2f);

            Assert.AreSame(right, picked);
        }

        [Test]
        public void Pick_PointBeyondRadius_ReturnsNull()
        {
            // 빈 곳을 눌렀는데 옆자리가 잡히면 플레이어가 의도하지 않은 자리에 앉는다.
            var slot = Slot(new Vector2(0f, 0f));

            Assert.IsNull(SlotPicker.Pick(new Vector2(5f, 0f), new[] { slot }, Radius));
        }

        [Test]
        public void Pick_JustInsideRadius_ReturnsSlot()
        {
            var slot = Slot(new Vector2(0f, 0f));

            Assert.AreSame(slot, SlotPicker.Pick(new Vector2(Radius * 0.99f, 0f),
                                                 new[] { slot }, Radius));
        }

        [Test]
        public void Pick_NullEntryInList_IsSkipped()
        {
            // 자리를 지운 씬에서 배열에 구멍이 남을 수 있다.
            var slot = Slot(new Vector2(0f, 0f));

            Assert.AreSame(slot, SlotPicker.Pick(Vector2.zero, new[] { null, slot }, Radius));
        }

        [Test]
        public void Pick_NoSlots_ReturnsNull()
        {
            Assert.IsNull(SlotPicker.Pick(Vector2.zero, new TableSlotView[0], Radius));
            Assert.IsNull(SlotPicker.Pick(Vector2.zero, null, Radius));
        }

        /// <summary>
        /// <c>Initialize</c> 가 <b>이미 찾아 둔 카메라를 지우지 않는지</b> 본다.
        ///
        /// <para>
        /// 씬 진입점은 카메라를 들고 있지 않아 <c>null</c> 을 넘긴다. 그대로 대입하면
        /// <c>Awake</c> 가 잡아 둔 참조가 날아가 <b>클릭이 통째로 죽는데</b>, 다른 테스트는
        /// <c>ClickAt</c> 을 직접 불러 카메라 경로를 지나치므로 브라우저에서야 드러났다.
        /// </para>
        /// </summary>
        [Test]
        public void Initialize_WithNullCamera_KeepsResolvedCamera()
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.AddComponent<Camera>();
            _created.Add(cameraGo);

            var inputGo = new GameObject("PlacementInput");
            _created.Add(inputGo);
            var input = inputGo.AddComponent<CustomerPlacementInput>();

            input.Initialize(null, null, new TableSlotView[0]);

            Assert.IsNotNull(CameraOf(input), "씬 진입점이 넘긴 null 이 카메라를 지웠다");
        }

        private static Camera CameraOf(CustomerPlacementInput input)
        {
            var field = typeof(CustomerPlacementInput).GetField(
                "_camera", System.Reflection.BindingFlags.Instance
                           | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "직렬화 필드 '_camera' 를 찾지 못했습니다.");
            return (Camera)field.GetValue(input);
        }

        private TableSlotView Slot(Vector2 position)
        {
            var go = new GameObject("TableSlot");
            go.transform.position = position;
            _created.Add(go);
            return go.AddComponent<TableSlotView>();
        }
    }
}
