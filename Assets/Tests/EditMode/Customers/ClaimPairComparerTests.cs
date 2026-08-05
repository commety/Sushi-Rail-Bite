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
    /// 정렬 키 5단 — 대역 거리 → 가격(높은 순) → 초밥 SeqNo → <b>대역 폭(좁은 순)</b> → 손님 SeqNo.
    ///
    /// <para>
    /// 뒤쪽 키를 검증할 때는 <b>앞 키를 일부러 동률로</b> 만든다. 앞이 갈리면 뒤 키가 아예
    /// 불리지 않아, 잘못된 구현으로도 우연히 정답이 나온다.
    /// </para>
    /// <para>
    /// <b>키 4를 검증할 때는 손님 SeqNo 를 반대로 준다.</b> 좁은 대역 손님에게 낮은 번호를
    /// 주면 키 5 만으로도 통과해 아무것도 증명하지 못한다.
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
        public void Compare_NearerToBand_ComesFirst()
        {
            var customer = NewBandCustomer(0, 100, 300);
            var near = new ClaimCandidatePair(customer, NewSushi(9, price: 320));
            var far = new ClaimCandidatePair(customer, NewSushi(0, price: 900));

            // 초밥 SeqNo 는 far 쪽이 낮은데도 거리가 이긴다 — 1순위이기 때문이다.
            Assert.Less(_comparer.Compare(near, far), 0);
            Assert.Greater(_comparer.Compare(far, near), 0);
        }

        [Test]
        public void Compare_BothInsideBand_HigherPriceComesFirst()
        {
            // 대역 안은 거리가 전부 0 이라 키 1이 아무것도 못 가른다. 그 동점을 키 2가
            // 깨는 것이 대역 방식의 핵심이다 — 여기가 깨지면 "대역 안에서 FIFO" 라는,
            // 이 마일스톤이 없애려는 바로 그 버그가 한 층 아래에서 재발한다.
            var customer = NewBandCustomer(0, 100, 300);
            var expensive = new ClaimCandidatePair(customer, NewSushi(9, price: 290));
            var cheap = new ClaimCandidatePair(customer, NewSushi(0, price: 110));

            Assert.Less(_comparer.Compare(expensive, cheap), 0);
            Assert.Greater(_comparer.Compare(cheap, expensive), 0);
        }

        [Test]
        public void Compare_EqualDistance_HigherPriceComesFirst()
        {
            // 타겟팅 200 에서 190 과 210 은 똑같이 가깝다. 이때 비싼 쪽을 집는다 —
            // 목적이 점수 최대화이기 때문이다. 부호를 뒤집어 쓰면 여기가 먼저 깨진다.
            var customer = NewCustomer(0, targetingPoint: 200);
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
        public void Compare_SameSushiSameWidth_LowerCustomerSequenceComesFirst()
        {
            var sushi = NewSushi(0);
            var first = new ClaimCandidatePair(NewCustomer(0), sushi);
            var second = new ClaimCandidatePair(NewCustomer(1), sushi);

            Assert.Less(_comparer.Compare(first, second), 0);
            Assert.Greater(_comparer.Compare(second, first), 0);
        }

        [Test]
        public void Compare_SameSushiNarrowerBand_ComesFirst()
        {
            // 전문가가 범용가를 이긴다. 손님 SeqNo 를 **반대로** 준 것이 핵심이다 —
            // 좁은 쪽에 낮은 번호를 주면 키 5 만으로도 통과해 키 4를 증명하지 못한다.
            var sushi = NewSushi(0, price: 200);
            var narrow = new ClaimCandidatePair(NewBandCustomer(9, 190, 210), sushi);
            var wide = new ClaimCandidatePair(NewBandCustomer(0, 100, 300), sushi);

            Assert.Less(_comparer.Compare(narrow, wide), 0);
            Assert.Greater(_comparer.Compare(wide, narrow), 0);
        }

        [Test]
        public void Compare_SushiSequenceOutranksBandWidth()
        {
            // 초밥 키(1~3)가 손님 키(4~5)보다 위라는 원칙은 대역 폭이 끼어들어도 유지된다.
            // 키 4를 키 3 위로 올리면 여기가 먼저 깨진다.
            var earlySushiWideCustomer =
                new ClaimCandidatePair(NewBandCustomer(0, 100, 300), NewSushi(0, price: 200));
            var lateSushiNarrowCustomer =
                new ClaimCandidatePair(NewBandCustomer(1, 190, 210), NewSushi(1, price: 200));

            Assert.Less(_comparer.Compare(earlySushiWideCustomer, lateSushiNarrowCustomer), 0);
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
        public void Compare_BandOutranksCustomerSequence()
        {
            // 순차번호는 어그로 시스템이 아니다 — 대역이 항상 위다. 정렬을 뒤집어
            // "먼저 배치하면 항상 먼저 먹는" 방식은 검토 후 미채택됐다 (§2).
            var sushi = NewSushi(0, price: 200);
            var lateButInBand = new ClaimCandidatePair(NewBandCustomer(99, 100, 300), sushi);
            var earlyButOutOfBand = new ClaimCandidatePair(NewBandCustomer(0, 800, 900), sushi);

            Assert.Less(_comparer.Compare(lateButInBand, earlyButOutOfBand), 0);
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

        /// <summary>
        /// 대역 폭 0 인 손님. 폭이 관여하지 않아야 하는 테스트가 쓴다 — 모든 손님의 폭이
        /// 같으면 키 4가 동률이 되어 그 아래 키가 드러난다.
        /// </summary>
        private CustomerLogic NewCustomer(int sequenceNumber, int targetingPoint = NeutralPrice) =>
            NewBandCustomer(sequenceNumber, targetingPoint, targetingPoint);

        private CustomerLogic NewBandCustomer(int sequenceNumber, int targetingMin, int targetingMax)
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(data);
            SerializedFieldSetter.SetFloat(data, "_reach", 10f);
            SerializedFieldSetter.SetTargetingBand(data, targetingMin, targetingMax);

            return new CustomerLogic(new CustomerRuntimeState(data, sequenceNumber), 0f);
        }

        private SushiItem NewSushi(int sequenceNumber, int price = NeutralPrice)
        {
            return new SushiItem(StageConfigBuilder.CreateSushi(_disposables, price), sequenceNumber);
        }
    }
}
