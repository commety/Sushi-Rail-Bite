using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class CustomerLogicTests
    {
        private const float TablePosition = 50f;
        private const float Reach = 10f;

        private CustomerData _customerData;
        private SushiData _sushiData;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(_customerData, "_reach", Reach);
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", 3);

            _sushiData = ScriptableObject.CreateInstance<SushiData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_customerData);
            Object.DestroyImmediate(_sushiData);
        }

        // ── 범위 ────────────────────────────────────────────────

        [Test]
        public void IsInReach_SushiAtCenter_ReturnsTrue()
        {
            var logic = NewLogic();

            Assert.IsTrue(logic.IsInReach(TablePosition));
        }

        [Test]
        public void IsInReach_SushiAtReachEdge_ReturnsTrue()
        {
            // 폐구간이라는 결정을 여기서 고정한다. 안 정하면 다음 사람이 반대로 구현한다.
            var logic = NewLogic();

            Assert.IsTrue(logic.IsInReach(TablePosition + Reach));
            Assert.IsTrue(logic.IsInReach(TablePosition - Reach));
        }

        [Test]
        public void IsInReach_SushiBeyondReach_ReturnsFalse()
        {
            var logic = NewLogic();

            Assert.IsFalse(logic.IsInReach(TablePosition + Reach + 0.01f));
        }

        [Test]
        public void IsInReach_SushiBehindCustomer_ReturnsTrue()
        {
            // 범위는 앞뒤 대칭이다 — 지나간 쪽도 집을 수 있다.
            var logic = NewLogic();

            Assert.IsTrue(logic.IsInReach(TablePosition - Reach * 0.5f));
        }

        // ── 손님 쪽 상태 ────────────────────────────────────────

        [Test]
        public void CanAcceptSushi_Idle_ReturnsTrue()
        {
            var logic = NewLogic();

            Assert.IsTrue(logic.CanAcceptSushi);
        }

        [Test]
        public void CanAcceptSushi_Eating_ReturnsFalse()
        {
            var logic = NewLogic();
            logic.State.State = CustomerState.Eating;

            Assert.IsFalse(logic.CanAcceptSushi);
        }

        [Test]
        public void CanAcceptSushi_Digesting_ReturnsFalse()
        {
            var logic = NewLogic();
            logic.State.State = CustomerState.Digesting;

            Assert.IsFalse(logic.CanAcceptSushi);
        }

        [Test]
        public void CanAcceptSushi_Full_ReturnsFalse()
        {
            var logic = NewLogic();
            logic.State.CurrentSaturation = _customerData.MaxSaturation;

            Assert.IsFalse(logic.CanAcceptSushi);
        }

        // ── 종합 자격 ───────────────────────────────────────────

        [Test]
        public void CanTake_SushiInReach_ReturnsTrue()
        {
            var logic = NewLogic();

            Assert.IsTrue(logic.CanTake(NewSushi(TablePosition)));
        }

        [Test]
        public void CanTake_SushiOutOfReach_ReturnsFalse()
        {
            var logic = NewLogic();

            Assert.IsFalse(logic.CanTake(NewSushi(TablePosition + Reach * 3f)));
        }

        [Test]
        public void CanTake_Full_ReturnsFalse()
        {
            var logic = NewLogic();
            logic.State.CurrentSaturation = _customerData.MaxSaturation;

            Assert.IsFalse(logic.CanTake(NewSushi(TablePosition)));
        }

        [Test]
        public void CanTake_Eating_ReturnsFalse()
        {
            var logic = NewLogic();
            logic.State.State = CustomerState.Eating;

            Assert.IsFalse(logic.CanTake(NewSushi(TablePosition)));
        }

        [Test]
        public void CanTake_AlreadyClaimedSushi_ReturnsFalse()
        {
            var logic = NewLogic();
            var sushi = NewSushi(TablePosition);
            sushi.TryClaim(99);

            Assert.IsFalse(logic.CanTake(sushi));
        }

        [Test]
        public void CanTake_NullSushi_ReturnsFalse()
        {
            var logic = NewLogic();

            Assert.IsFalse(logic.CanTake(null));
        }

        // ── 가격은 자격에 영향을 주지 않는다 (M2 회귀 방지) ─────

        [Test]
        public void CanTake_ExpensiveSushiFarFromTargeting_ReturnsTrue()
        {
            // 이 두 테스트는 M1 에서는 당연히 통과한다. 누군가 자격에 가격을 끼우는 순간
            // 가장 먼저 깨지라고 두는 장치다 (CLAUDE.md §1.1-3a). 지우지 않는다.
            SerializedFieldSetter.SetInt(_customerData, "_targetingPrice", 100);
            SerializedFieldSetter.SetInt(_sushiData, "_price", 99999);
            var logic = NewLogic();

            Assert.IsTrue(logic.CanTake(NewSushi(TablePosition)));
        }

        [Test]
        public void CanTake_CheapSushi_ReturnsTrue()
        {
            SerializedFieldSetter.SetInt(_customerData, "_targetingPrice", 100);
            SerializedFieldSetter.SetInt(_sushiData, "_price", 0);
            var logic = NewLogic();

            Assert.IsTrue(logic.CanTake(NewSushi(TablePosition)));
        }

        private CustomerLogic NewLogic()
        {
            return new CustomerLogic(new CustomerRuntimeState(_customerData, 0), TablePosition);
        }

        private SushiItem NewSushi(float beltPosition)
        {
            return new SushiItem(_sushiData, 0) { BeltPosition = beltPosition };
        }
    }
}
