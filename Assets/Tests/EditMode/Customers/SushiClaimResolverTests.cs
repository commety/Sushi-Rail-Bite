using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class SushiClaimResolverTests
    {
        private const float Latch = 0f;

        private CustomerData _customerData;
        private SushiClaimResolver _resolver;
        private List<CustomerLogic> _customers;
        private Dictionary<CustomerLogic, CandidateSet> _candidates;
        private List<ClaimCandidatePair> _results;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(_customerData, "_reach", 100f);
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", 5);

            _resolver = new SushiClaimResolver();
            _customers = new List<CustomerLogic>();
            _candidates = new Dictionary<CustomerLogic, CandidateSet>();
            _results = new List<ClaimCandidatePair>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_customerData);
        }

        [Test]
        public void Resolve_OneCustomerOneSushi_AssignsIt()
        {
            var customer = AddCustomer(0);
            var sushi = Recognize(customer, 0);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(customer, _results[0].Customer);
            Assert.AreSame(sushi, _results[0].Sushi);
        }

        [Test]
        public void Resolve_Assigned_MarksSushiClaimed()
        {
            var customer = AddCustomer(3);
            var sushi = Recognize(customer, 0);

            Resolve();

            Assert.AreEqual(SushiState.Claimed, sushi.State);
            Assert.AreEqual(3, sushi.ClaimedByCustomerSequenceNumber);
        }

        [Test]
        public void Resolve_MultipleSushiInReach_TakesLowestSequence()
        {
            var customer = AddCustomer(0);
            var later = Recognize(customer, 5);
            var earlier = Recognize(customer, 1);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(earlier, _results[0].Sushi, "순차번호가 낮은 초밥이 먼저다");
            Assert.AreEqual(SushiState.OnBelt, later.State);
        }

        [Test]
        public void Resolve_ManyCustomersOneSushi_LowerCustomerSequenceWins()
        {
            var first = AddCustomer(0);
            var second = AddCustomer(1);
            var sushi = new SushiItem(null, 0);
            Recognize(first, sushi);
            Recognize(second, sushi);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(first, _results[0].Customer);
        }

        [Test]
        public void Resolve_ManyCustomersManySushi_AssignsWithoutDuplicates()
        {
            var first = AddCustomer(0);
            var second = AddCustomer(1);
            var sushiA = new SushiItem(null, 0);
            var sushiB = new SushiItem(null, 1);
            Recognize(first, sushiA);
            Recognize(first, sushiB);
            Recognize(second, sushiA);
            Recognize(second, sushiB);

            Resolve();

            Assert.AreEqual(2, _results.Count);
            Assert.AreNotSame(_results[0].Customer, _results[1].Customer);
            Assert.AreNotSame(_results[0].Sushi, _results[1].Sushi);
        }

        [Test]
        public void Resolve_AssignedSushi_NotAssignedAgain()
        {
            var customer = AddCustomer(0);
            var sushi = Recognize(customer, 0);
            sushi.TryClaim(99);

            Resolve();

            Assert.IsEmpty(_results, "이미 배정된 초밥은 다시 배정되지 않는다");
        }

        [Test]
        public void Resolve_AssignedCustomer_DoesNotTakeSecondSushi()
        {
            // 한 번에 하나 (sushi-claim-flow §7 기본안).
            var customer = AddCustomer(0);
            Recognize(customer, 0);
            Recognize(customer, 1);

            Resolve();

            Assert.AreEqual(1, _results.Count);
        }

        [Test]
        public void Resolve_CandidatesExist_AlwaysProducesAtLeastOne()
        {
            // "구경하지 않는다" 의 EditMode 대응물 — 후보가 있으면 아무도 못 집는 결과는
            // 나올 수 없다 (CLAUDE.md §1.1-3a).
            var customer = AddCustomer(0);
            Recognize(customer, 42);

            Resolve();

            Assert.IsNotEmpty(_results);
        }

        [Test]
        public void Resolve_EmptyCandidates_ProducesNothing()
        {
            AddCustomer(0);

            Resolve();

            Assert.IsEmpty(_results);
        }

        [Test]
        public void Resolve_NoCustomers_ProducesNothing()
        {
            Resolve();

            Assert.IsEmpty(_results);
        }

        [Test]
        public void Resolve_SameBoardTwice_ProducesIdenticalResult()
        {
            // 결정성 회귀 방지 — 배정 경로에 난수가 들어오면 이 테스트가 먼저 깨진다.
            var firstRun = RunScenario();
            var secondRun = RunScenario();

            Assert.AreEqual(firstRun.Count, secondRun.Count);
            for (var i = 0; i < firstRun.Count; i++)
            {
                Assert.AreEqual(firstRun[i].customer, secondRun[i].customer);
                Assert.AreEqual(firstRun[i].sushi, secondRun[i].sushi);
            }
        }

        [Test]
        public void Resolve_ReusedResultBuffer_DoesNotAccumulate()
        {
            var customer = AddCustomer(0);
            Recognize(customer, 0);
            Resolve();

            Resolve();

            Assert.IsEmpty(_results, "두 번째 호출은 버퍼를 비우고 시작한다");
        }

        private List<(int customer, int sushi)> RunScenario()
        {
            _customers.Clear();
            _candidates.Clear();

            var first = AddCustomer(0);
            var second = AddCustomer(1);
            var third = AddCustomer(2);
            var sushiA = new SushiItem(null, 7);
            var sushiB = new SushiItem(null, 3);

            Recognize(first, sushiA);
            Recognize(second, sushiA);
            Recognize(second, sushiB);
            Recognize(third, sushiB);

            Resolve();

            var snapshot = new List<(int, int)>();
            foreach (var pair in _results)
            {
                snapshot.Add((pair.Customer.State.SequenceNumber, pair.Sushi.SequenceNumber));
            }

            return snapshot;
        }

        private void Resolve() => _resolver.Resolve(_customers, _candidates, _results);

        private CustomerLogic AddCustomer(int sequenceNumber)
        {
            var customer = new CustomerLogic(new CustomerRuntimeState(_customerData, sequenceNumber), 0f);
            _customers.Add(customer);
            _candidates[customer] = new CandidateSet(Latch);
            return customer;
        }

        private SushiItem Recognize(CustomerLogic customer, int sushiSequenceNumber)
        {
            var sushi = new SushiItem(null, sushiSequenceNumber);
            _candidates[customer].Recognize(sushi, 0f);
            return sushi;
        }

        private void Recognize(CustomerLogic customer, SushiItem sushi)
        {
            _candidates[customer].Recognize(sushi, 0f);
        }
    }
}
