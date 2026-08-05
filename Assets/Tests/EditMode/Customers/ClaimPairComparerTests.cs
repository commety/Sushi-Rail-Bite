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
    /// 정렬 키 4단 — 타겟팅 거리 → 가격(높은 순) → 초밥 SeqNo → 손님 SeqNo.
    ///
    /// <para>
    /// 뒤쪽 두 키(M1)를 검증할 때는 <b>가격과 타겟팅을 일부러 같게</b> 둔다. 앞 두 키가
    /// 동률이어야 뒤 키가 작동하는 것을 볼 수 있고, 그래야 어느 키가 깨졌는지 테스트가 말해준다.
    /// </para>
    /// </summary>
    public sealed class ClaimPairComparerTests
    {
        /// <summary>기본 가격·타겟팅을 맞춰 두면 앞 두 키가 동률이 되어 SeqNo 키가 드러난다.</summary>
        private const int NeutralPrice = 100;

        private readonly List<Object> _disposables = new();
        private ClaimPairComparer _comparer;

        [SetUp]
        public void SetUp()
        {
            _comparer = new ClaimPairComparer();
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
        public void Compare_NearerToTargeting_ComesFirst()
        {
            var customer = NewCustomer(0, targetingPrice: 200);
            var near = new ClaimCandidatePair(customer, NewSushi(9, price: 220));
            var far = new ClaimCandidatePair(customer, NewSushi(0, price: 500));

            // 초밥 SeqNo 는 far 쪽이 낮은데도 거리가 이긴다 — 1순위이기 때문이다.
            Assert.Less(_comparer.Compare(near, far), 0);
            Assert.Greater(_comparer.Compare(far, near), 0);
        }

        [Test]
        public void Compare_EqualDistance_HigherPriceComesFirst()
        {
            // 타겟팅 200 에서 190 과 210 은 똑같이 가깝다. 이때 비싼 쪽을 집는다 —
            // 목적이 점수 최대화이기 때문이다. 부호를 뒤집어 쓰면 여기가 먼저 깨진다.
            var customer = NewCustomer(0, targetingPrice: 200);
            var expensive = new ClaimCandidatePair(customer, NewSushi(9, price: 210));
            var cheap = new ClaimCandidatePair(customer, NewSushi(0, price: 190));

            Assert.Less(_comparer.Compare(expensive, cheap), 0);
            Assert.Greater(_comparer.Compare(cheap, expensive), 0);
        }

        [Test]
        public void Compare_LowerSushiSequence_ComesFirst()
        {
            var customer = NewCustomer(0);
            var earlier = new ClaimCandidatePair(customer, NewSushi(0));
            var later = new ClaimCandidatePair(customer, NewSushi(1));

            Assert.Less(_comparer.Compare(earlier, later), 0);
            Assert.Greater(_comparer.Compare(later, earlier), 0);
        }

        [Test]
        public void Compare_SameSushi_LowerCustomerSequenceComesFirst()
        {
            var sushi = NewSushi(0);
            var first = new ClaimCandidatePair(NewCustomer(0), sushi);
            var second = new ClaimCandidatePair(NewCustomer(1), sushi);

            Assert.Less(_comparer.Compare(first, second), 0);
            Assert.Greater(_comparer.Compare(second, first), 0);
        }

        [Test]
        public void Compare_SushiKeyOutranksCustomerKey()
        {
            // TD 로 보면 적(초밥)이 최선, 타워(손님)가 차선이다 (sushi-claim-flow §2).
            // 손님 순차번호가 한참 뒤여도 초밥이 앞서면 이긴다.
            var lateCustomerEarlySushi = new ClaimCandidatePair(NewCustomer(99), NewSushi(0));
            var earlyCustomerLateSushi = new ClaimCandidatePair(NewCustomer(0), NewSushi(1));

            Assert.Less(_comparer.Compare(lateCustomerEarlySushi, earlyCustomerLateSushi), 0);
        }

        [Test]
        public void Compare_TargetingOutranksCustomerSequence()
        {
            // 순차번호는 어그로 시스템이 아니다 — 타겟팅이 항상 위다. 정렬을 뒤집어
            // "먼저 배치하면 항상 먼저 먹는" 방식은 검토 후 미채택됐다 (§2).
            var sushi = NewSushi(0, price: 200);
            var lateButOnTarget = new ClaimCandidatePair(NewCustomer(99, targetingPrice: 200), sushi);
            var earlyButOffTarget = new ClaimCandidatePair(NewCustomer(0, targetingPrice: 900), sushi);

            Assert.Less(_comparer.Compare(lateButOnTarget, earlyButOffTarget), 0);
        }

        [Test]
        public void Compare_SamePair_ReturnsZero()
        {
            var pair = new ClaimCandidatePair(NewCustomer(0), NewSushi(0));

            Assert.AreEqual(0, _comparer.Compare(pair, pair));
        }

        [Test]
        public void Compare_DifferentPairs_NeverReturnsZero()
        {
            // 완전순서 — 순차번호가 유일하므로 서로 다른 쌍은 항상 순서가 갈린다.
            // 0 이 나오면 승자가 정렬 구현에 좌우되어 결정성이 깨진다.
            // 가격·타겟팅을 모두 같게 둬 앞 두 키를 동률로 만든 최악의 경우다.
            var customers = new[] { NewCustomer(0), NewCustomer(1) };
            var sushi = new[] { NewSushi(0), NewSushi(1) };

            foreach (var leftCustomer in customers)
            {
                foreach (var leftSushi in sushi)
                {
                    foreach (var rightCustomer in customers)
                    {
                        foreach (var rightSushi in sushi)
                        {
                            if (leftCustomer == rightCustomer && leftSushi == rightSushi)
                            {
                                continue;
                            }

                            var left = new ClaimCandidatePair(leftCustomer, leftSushi);
                            var right = new ClaimCandidatePair(rightCustomer, rightSushi);

                            Assert.AreNotEqual(0, _comparer.Compare(left, right));
                        }
                    }
                }
            }
        }

        private CustomerLogic NewCustomer(int sequenceNumber, int targetingPrice = NeutralPrice)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);
            SerializedFieldSetter.SetFloat(data, "_reach", 10f);
            SerializedFieldSetter.SetInt(data, "_targetingPrice", targetingPrice);

            return new CustomerLogic(new CustomerRuntimeState(data, sequenceNumber), 0f);
        }

        private SushiItem NewSushi(int sequenceNumber, int price = NeutralPrice)
        {
            return new SushiItem(StageConfigBuilder.CreateSushi(_disposables, price), sequenceNumber);
        }
    }
}
