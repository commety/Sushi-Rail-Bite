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

        private TableSlotView Slot(Vector2 position)
        {
            var go = new GameObject("TableSlot");
            go.transform.position = position;
            _created.Add(go);
            return go.AddComponent<TableSlotView>();
        }
    }
}
