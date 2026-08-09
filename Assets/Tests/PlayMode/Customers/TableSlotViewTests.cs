using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 자리가 손님 시각 표현을 챙기는 경로. <b>이 경로가 조용히 비면 «배치했는데 안 보인다»</b>
    /// 가 된다 — 배치 서비스에는 손님이 등록돼 «손님 n/3» 은 오르는데 화면에는 아무것도
    /// 나타나지 않고, 예외도 로그도 없다.
    ///
    /// <para>
    /// 씬의 자리 넷은 인스펙터 참조가 모두 비어 있어 <b>자식에서 찾는 폴백이 유일한
    /// 연결</b>이다. 다른 테스트는 대부분 <c>Initialize</c> 로 참조를 물려 주므로 그 폴백을
    /// 지나친다 (<c>.claude/rules/tests.md</c> §1 «직접 주입으로 우회되는 폴백»).
    /// </para>
    /// </summary>
    public sealed class TableSlotViewTests
    {
        private readonly List<Object> _garbage = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        /// <summary>
        /// <b><c>Awake</c> 가 아직 안 돈 자리도 손님을 앉힌다.</b> 챙기는 일을 <c>Awake</c> 에만
        /// 두면 실행 순서에 따라 <c>Occupy</c> 가 먼저 올 수 있고, 그때 자리는 조용히 아무
        /// 일도 하지 않는다 — 증상이 «아주 가끔 안 보인다» 라 원인을 짚기 어렵다.
        /// </summary>
        [Test]
        public void Occupy_BeforeAwakeRuns_StillShowsTheCustomer()
        {
            var root = new GameObject("TableSlot");
            _garbage.Add(root);

            // 꺼진 채로 세우면 Awake 가 미뤄진다 — 씬에서 실행 순서가 어긋난 상황과 같다.
            root.SetActive(false);

            var seat = new GameObject("SeatVisual");
            seat.transform.SetParent(root.transform, false);
            seat.AddComponent<SpriteRenderer>();
            var view = seat.AddComponent<CustomerView>();

            var slot = root.AddComponent<TableSlotView>();
            slot.Occupy(NewLogic(), null);

            Assert.AreSame(view, slot.Occupant,
                           "Awake 전에 앉히면 자리가 시각 표현을 못 찾는다 — 손님이 안 보인다");
            Assert.IsTrue(seat.activeSelf, "시각 표현이 꺼진 채로 남았다");
        }

        /// <summary>
        /// <b>명시 주입이 폴백을 이긴다.</b> <c>null</c> 을 넘겨 «시각 표현 없음» 을 만들려던
        /// 쪽이, 자식에서 찾아낸 것을 도로 물면 «없음» 을 표현할 방법이 사라진다.
        /// </summary>
        [Test]
        public void Initialize_WithNullSeat_DoesNotFallBackToTheChild()
        {
            var root = new GameObject("TableSlot");
            _garbage.Add(root);
            root.SetActive(false);

            var seat = new GameObject("SeatVisual");
            seat.transform.SetParent(root.transform, false);
            seat.AddComponent<SpriteRenderer>();
            seat.AddComponent<CustomerView>();

            var slot = root.AddComponent<TableSlotView>();
            slot.Initialize(slotIndex: 0, beltPosition: 0f, seatVisual: null);

            slot.Occupy(NewLogic(), null);

            Assert.IsNull(slot.Occupant, "없다고 넘겼는데 자식을 찾아 물었다");
        }

        private CustomerLogic NewLogic()
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _garbage.Add(data);

            return new CustomerLogic(new CustomerRuntimeState(data, 0), 0f);
        }
    }
}
