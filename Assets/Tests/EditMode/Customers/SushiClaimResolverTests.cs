using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 배정만 검증한다 — <b>자격은 여기서 보지 않는다.</b> 두 가지를 한 테스트에서 섞으면
    /// 먼 초밥을 안 먹었을 때 자격이 막은 건지 배정이 다른 걸 고른 건지 구분되지 않는다
    /// (<c>.claude/rules/tests.md</c> §3).
    ///
    /// <para>
    /// 리졸버는 넘겨받은 손님이 <b>이미 자격을 통과했다</b>고 전제한다. 그래서 여기 손님들은
    /// 전부 넉넉한 범위·포화도를 갖는다.
    /// </para>
    /// </summary>
    public sealed class SushiClaimResolverTests
    {
        private const float Latch = 0f;

        /// <summary>가격·타겟팅을 맞춰 두면 앞 두 키가 동률이 되어 SeqNo 키가 드러난다.</summary>
        private const int NeutralPrice = 100;

        private readonly List<Object> _disposables = new();
        private SushiClaimResolver _resolver;
        private List<CustomerLogic> _customers;
        private Dictionary<CustomerLogic, CandidateSet> _candidates;
        private List<ClaimCandidatePair> _results;

        [SetUp]
        public void SetUp()
        {
            _resolver = new SushiClaimResolver();
            _customers = new List<CustomerLogic>();
            _candidates = new Dictionary<CustomerLogic, CandidateSet>();
            _results = new List<ClaimCandidatePair>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 배정 1:N ──────────────────────────────────────────

        [Test]
        public void Resolve_OneCustomerManySushi_TakesNearestToTargeting()
        {
            var customer = AddCustomer(0, targetingPrice: 200);
            var far = Recognize(customer, NewSushi(0, price: 800));
            var near = Recognize(customer, NewSushi(9, price: 210));

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(near, _results[0].Sushi, "타겟팅에 가까운 초밥이 먼저다");
            Assert.AreEqual(SushiState.OnBelt, far.State);
        }

        [Test]
        public void Resolve_EqualDistance_TakesHigherPrice()
        {
            // 타겟팅 200 → 190 과 210 은 동거리. 비싼 쪽을 집는다.
            var customer = AddCustomer(0, targetingPrice: 200);
            var cheap = Recognize(customer, NewSushi(0, price: 190));
            var expensive = Recognize(customer, NewSushi(9, price: 210));

            Resolve();

            Assert.AreSame(expensive, _results[0].Sushi);
            Assert.AreEqual(SushiState.OnBelt, cheap.State);
        }

        [Test]
        public void Resolve_SamePrice_TakesLowerSushiSequence()
        {
            var customer = AddCustomer(0);
            var later = Recognize(customer, NewSushi(5));
            var earlier = Recognize(customer, NewSushi(1));

            Resolve();

            Assert.AreSame(earlier, _results[0].Sushi, "동거리·동가격이면 순차번호가 낮은 쪽");
            Assert.AreEqual(SushiState.OnBelt, later.State);
        }

        [Test]
        public void Resolve_NoGoodMatch_StillTakesFarSushi()
        {
            // 타겟팅에서 아무리 멀어도 더 맞는 대안이 없으면 먹는다.
            // 손님이 눈앞의 초밥을 두고 구경하는 것은 버그다 (CLAUDE.md §1.1-3a).
            var customer = AddCustomer(0, targetingPrice: 200);
            var farAway = Recognize(customer, NewSushi(0, price: 5000));

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(farAway, _results[0].Sushi);
        }

        // ── 배정 N:1 ──────────────────────────────────────────

        [Test]
        public void Resolve_ManyCustomersOneSushi_NearestTargetingWins()
        {
            var offTarget = AddCustomer(0, targetingPrice: 900);
            var onTarget = AddCustomer(1, targetingPrice: 200);
            var sushi = NewSushi(0, price: 200);
            Recognize(offTarget, sushi);
            Recognize(onTarget, sushi);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(onTarget, _results[0].Customer, "손님 SeqNo 가 뒤여도 타겟팅이 위다");
        }

        [Test]
        public void Resolve_EqualDistanceSameSushi_LowerCustomerSequenceWins()
        {
            var first = AddCustomer(0);
            var second = AddCustomer(1);
            var sushi = NewSushi(0);
            Recognize(first, sushi);
            Recognize(second, sushi);

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(first, _results[0].Customer);
        }

        // ── 배정 N:M ──────────────────────────────────────────

        [Test]
        public void Resolve_ManyToMany_EachTakesItsNearest()
        {
            // 순차번호와 선호를 **엇갈리게** 배치한다. SeqNo 만 보는 정렬이면
            //   (luxuryLover, cheapSushi) → (cheapLover, luxurySushi)
            // 로 둘 다 반대 것을 집는다. 이 엇갈림이 없으면 잘못된 정렬로도
            // 우연히 정답이 나와 테스트가 아무것도 잡지 못한다.
            var luxuryLover = AddCustomer(0, targetingPrice: 800);
            var cheapLover = AddCustomer(1, targetingPrice: 120);
            var cheapSushi = NewSushi(0, price: 120);
            var luxurySushi = NewSushi(1, price: 800);

            Recognize(cheapLover, cheapSushi);
            Recognize(cheapLover, luxurySushi);
            Recognize(luxuryLover, cheapSushi);
            Recognize(luxuryLover, luxurySushi);

            Resolve();

            Assert.AreEqual(2, _results.Count);
            Assert.AreSame(cheapSushi, SushiTakenBy(cheapLover));
            Assert.AreSame(luxurySushi, SushiTakenBy(luxuryLover));
        }

        [Test]
        public void Resolve_ManyCustomersManySushi_AssignsWithoutDuplicates()
        {
            var first = AddCustomer(0);
            var second = AddCustomer(1);
            var sushiA = NewSushi(0);
            var sushiB = NewSushi(1);
            Recognize(first, sushiA);
            Recognize(first, sushiB);
            Recognize(second, sushiA);
            Recognize(second, sushiB);

            Resolve();

            Assert.AreEqual(2, _results.Count);
            Assert.AreNotSame(_results[0].Customer, _results[1].Customer);
            Assert.AreNotSame(_results[0].Sushi, _results[1].Sushi);
        }

        // ── 불변식 ────────────────────────────────────────────

        [Test]
        public void Resolve_OneCustomerOneSushi_AssignsIt()
        {
            var customer = AddCustomer(0);
            var sushi = Recognize(customer, NewSushi(0));

            Resolve();

            Assert.AreEqual(1, _results.Count);
            Assert.AreSame(customer, _results[0].Customer);
            Assert.AreSame(sushi, _results[0].Sushi);
        }

        [Test]
        public void Resolve_Assigned_MarksSushiClaimed()
        {
            var customer = AddCustomer(3);
            var sushi = Recognize(customer, NewSushi(0));

            Resolve();

            Assert.AreEqual(SushiState.Claimed, sushi.State);
            Assert.AreEqual(3, sushi.ClaimedByCustomerSequenceNumber);
        }

        [Test]
        public void Resolve_SushiAlreadyClaimed_NotAssignedAgain()
        {
            var customer = AddCustomer(0);
            var sushi = Recognize(customer, NewSushi(0));
            sushi.TryClaim(99);

            Resolve();

            Assert.IsEmpty(_results, "이미 배정된 초밥은 다시 배정되지 않는다");
        }

        [Test]
        public void Resolve_AssignedCustomer_DoesNotTakeSecondSushi()
        {
            // 한 번에 하나 (착수 시 확정).
            var customer = AddCustomer(0);
            Recognize(customer, NewSushi(0));
            Recognize(customer, NewSushi(1));

            Resolve();

            Assert.AreEqual(1, _results.Count);
        }

        [Test]
        public void Resolve_CandidatesExist_AlwaysProducesAtLeastOne()
        {
            // "구경하지 않는다" 의 EditMode 대응물 — 후보가 있으면 아무도 못 집는 결과는
            // 나올 수 없다 (CLAUDE.md §1.1-3a).
            var customer = AddCustomer(0);
            Recognize(customer, NewSushi(42));

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
            Recognize(customer, NewSushi(0));
            Resolve();

            Resolve();

            Assert.IsEmpty(_results, "두 번째 호출은 버퍼를 비우고 시작한다");
        }

        private List<(int customer, int sushi)> RunScenario()
        {
            _customers.Clear();
            _candidates.Clear();

            // 타겟팅·가격을 서로 다르게 둬 네 키가 모두 관여하게 만든다.
            var first = AddCustomer(0, targetingPrice: 150);
            var second = AddCustomer(1, targetingPrice: 300);
            var third = AddCustomer(2, targetingPrice: 150);
            var sushiA = NewSushi(7, price: 300);
            var sushiB = NewSushi(3, price: 150);

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

        private SushiItem SushiTakenBy(CustomerLogic customer)
        {
            foreach (var pair in _results)
            {
                if (ReferenceEquals(pair.Customer, customer))
                {
                    return pair.Sushi;
                }
            }

            return null;
        }

        private void Resolve() => _resolver.Resolve(_customers, _candidates, _results);

        private CustomerLogic AddCustomer(int sequenceNumber, int targetingPrice = NeutralPrice)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);
            SerializedFieldSetter.SetFloat(data, "_reach", 100f);
            SerializedFieldSetter.SetInt(data, "_targetingPrice", targetingPrice);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", 5);

            var customer = new CustomerLogic(new CustomerRuntimeState(data, sequenceNumber), 0f);
            _customers.Add(customer);
            _candidates[customer] = new CandidateSet(Latch);
            return customer;
        }

        private SushiItem NewSushi(int sequenceNumber, int price = NeutralPrice)
        {
            return new SushiItem(StageConfigBuilder.CreateSushi(_disposables, price), sequenceNumber);
        }

        private SushiItem Recognize(CustomerLogic customer, SushiItem sushi)
        {
            _candidates[customer].Recognize(sushi, 0f);
            return sushi;
        }
    }
}
