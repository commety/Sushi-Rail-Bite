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
    /// <b>타이밍만 검증한다</b> — 누가 무엇을 가져가는지는 <c>SushiClaimResolverTests</c> 의
    /// 몫이다. 둘을 한 테스트에서 섞으면 안 먹은 이유가 랭킹인지 대기인지 구분되지 않는다.
    ///
    /// <para>
    /// 자격도 섞지 않는다. 여기 손님은 전부 자격을 통과한 상태이며, 조율자가 틱 5단계에서
    /// 자격 없는 손님의 후보를 이미 비운 뒤에 이 판정이 돈다.
    /// </para>
    /// </summary>
    public sealed class ClaimDeadlineTests
    {
        private const float NoLatch = 0f;

        /// <summary>소식좌 대역. 덱 최고가(300)만 겨우 들어오는 좁은 고가대다.</summary>
        private const int BandMin = 300;

        private const int BandMax = 550;

        private readonly List<Object> _disposables = new();
        private ClaimDeadline _deadline;
        private CandidateSet _candidates;
        private CustomerLogic _customer;

        [SetUp]
        public void SetUp()
        {
            _deadline = new ClaimDeadline(new ClaimPairComparer());
            _candidates = new CandidateSet(NoLatch);
            _customer = NewCustomer(0, BandMin, BandMax);
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

        [Test]
        public void Evaluate_NoCandidates_ReturnsNothing()
        {
            Assert.AreEqual(ClaimTiming.Nothing, _deadline.Evaluate(_customer, _candidates, 0f));
        }

        [Test]
        public void Evaluate_OnlyClaimedCandidate_ReturnsNothing()
        {
            // 다른 손님이 이미 가져간 초밥은 최선 후보가 될 수 없다. 되면 영영 오지 않는
            // 마감시한에 걸려 이 손님이 굶는다.
            var taken = NewSushi(0, price: 400);
            taken.TryClaim(99);
            _candidates.Recognize(taken, 10f);

            Assert.AreEqual(ClaimTiming.Nothing, _deadline.Evaluate(_customer, _candidates, 0f));
        }

        [Test]
        public void Evaluate_InBandCandidate_ReturnsDueImmediately()
        {
            // 이탈까지 한참 남았는데도 기다리지 않는다 — 규칙 1.
            _candidates.Recognize(NewSushi(0, price: 400), 999f);

            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 0f));
        }

        [Test]
        public void Evaluate_OnlyOutOfBandBeforeExit_ReturnsWaiting()
        {
            _candidates.Recognize(NewSushi(0, price: 120), 5f);

            Assert.AreEqual(ClaimTiming.Waiting, _deadline.Evaluate(_customer, _candidates, 4.99f));
        }

        [Test]
        public void Evaluate_OnlyOutOfBandAtExit_ReturnsDue()
        {
            // 경계는 폐구간이다. `>` 로 쓰면 마감시한이 한 틱 늦어 초밥을 놓친다.
            _candidates.Recognize(NewSushi(0, price: 120), 5f);

            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 5f));
        }

        [Test]
        public void Evaluate_InBandArrivesWhileWaiting_ReturnsDueImmediately()
        {
            // 규칙 2 → 규칙 1 전환. 기다리던 손님도 대역 안이 들어오면 즉시 확정한다.
            _candidates.Recognize(NewSushi(0, price: 120), 5f);
            Assert.AreEqual(ClaimTiming.Waiting, _deadline.Evaluate(_customer, _candidates, 1f));

            _candidates.Recognize(NewSushi(1, price: 400), 999f);

            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 1f));
        }

        [Test]
        public void Evaluate_BetterCandidateRecognized_PushesDeadlineLater()
        {
            // 마감시한 갱신. 갱신 코드가 따로 없고 최선을 매번 다시 고르는 것으로 나온다.
            _candidates.Recognize(NewSushi(0, price: 120), 3f);
            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 3f));

            // 190 이 대역(300~550)에 더 가깝다 → 최선이 바뀌고 마감도 그쪽(8초)으로 간다.
            _candidates.Recognize(NewSushi(1, price: 190), 8f);

            Assert.AreEqual(ClaimTiming.Waiting, _deadline.Evaluate(_customer, _candidates, 3f));
            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 8f));
        }

        [Test]
        public void Evaluate_WorseCandidateExitsFirst_StillWaits()
        {
            // 못한 후보가 먼저 사라지는 것은 손해가 아니다. **이탈 순서를 선호 순서와
            // 엇갈리게** 둔 것이 핵심이다 — 나란히 두면 "가장 먼저 나가는 것" 을 보는
            // 잘못된 구현으로도 통과한다.
            _candidates.Recognize(NewSushi(0, price: 190), 9f);   // 더 좋은데 늦게 나간다
            _candidates.Recognize(NewSushi(1, price: 120), 2f);   // 못한데 먼저 나간다

            Assert.AreEqual(ClaimTiming.Waiting, _deadline.Evaluate(_customer, _candidates, 2f),
                            "못한 후보의 이탈 시각에 확정하면 안 된다");
            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 9f));
        }

        [Test]
        public void Evaluate_TwoInBandCandidates_ReturnsDueRegardlessOfExitTimes()
        {
            _candidates.Recognize(NewSushi(0, price: 310), 1f);
            _candidates.Recognize(NewSushi(1, price: 540), 999f);

            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(_customer, _candidates, 0f));
        }

        [Test]
        public void Evaluate_ZeroWidthBandOnExactPrice_ReturnsDueImmediately()
        {
            // 폭 0 손님도 자기 가격을 만나면 즉시 먹는다. 대역이 좁다고 굶지 않는다.
            var picky = NewCustomer(1, 200, 200);
            _candidates.Recognize(NewSushi(0, price: 200), 999f);

            Assert.AreEqual(ClaimTiming.Due, _deadline.Evaluate(picky, _candidates, 0f));
        }

        [Test]
        public void Evaluate_SameInputTwice_ReturnsSameTiming()
        {
            // 결정성 — 판정 경로에 난수가 들어오면 여기가 먼저 깨진다.
            _candidates.Recognize(NewSushi(0, price: 120), 5f);
            _candidates.Recognize(NewSushi(1, price: 190), 7f);

            Assert.AreEqual(_deadline.Evaluate(_customer, _candidates, 3f),
                            _deadline.Evaluate(_customer, _candidates, 3f));
        }

        private CustomerLogic NewCustomer(int sequenceNumber, int targetingMin, int targetingMax)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);
            SerializedFieldSetter.SetFloat(data, "_reach", 100f);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", 5);
            SerializedFieldSetter.SetTargetingBand(data, targetingMin, targetingMax);

            return new CustomerLogic(new CustomerRuntimeState(data, sequenceNumber), 0f);
        }

        private SushiItem NewSushi(int sequenceNumber, int price)
        {
            return new SushiItem(StageConfigBuilder.CreateSushi(_disposables, price), sequenceNumber);
        }
    }
}
