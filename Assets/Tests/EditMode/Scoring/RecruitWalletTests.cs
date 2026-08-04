using System;
using NUnit.Framework;
using SushiDefense.Scoring;

namespace SushiDefense.Tests.EditMode.Scoring
{
    /// <summary>
    /// 영입 재화는 <c>가격 / 10</c> 을 <b>정수 나눗셈</b>으로 누적한다 (착수 시 확정).
    ///
    /// <para>
    /// 가격 하한이 100 이라 초밥 1개당 최소 10 이 들어온다. 그래서 실수 누적·잔여분
    /// 이월 같은 장치 없이도 재화가 조용히 0 에 머무는 상황이 생기지 않는다.
    /// </para>
    /// </summary>
    public sealed class RecruitWalletTests
    {
        private const int InitialBudget = 50;

        private RecruitWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = new RecruitWallet(InitialBudget);
        }

        [Test]
        public void Balance_NewWallet_EqualsInitialBudget()
        {
            Assert.AreEqual(InitialBudget, _wallet.Balance);
        }

        [Test]
        public void Constructor_NegativeBudget_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitWallet(-1));
        }

        // ── 적립 ──────────────────────────────────────────────

        [Test]
        public void AddRecruitCurrency_ScoreGained_AccruesOneTenth()
        {
            _wallet.AccrueFrom(300);

            Assert.AreEqual(InitialBudget + 30, _wallet.Balance);
        }

        [Test]
        public void AccrueFrom_PriceNotMultipleOfTen_TruncatesDown()
        {
            // 정수 규칙을 고정하는 회귀선 — 실수 누적으로 되돌리면 여기가 먼저 깨진다.
            _wallet.AccrueFrom(155);

            Assert.AreEqual(InitialBudget + 15, _wallet.Balance);
        }

        [Test]
        public void AccrueFrom_RepeatedPrices_TruncatesEachTime()
        {
            // 실수 누적이면 155 를 두 번 먹었을 때 31 이 된다. 정수 누적은 30 이다.
            _wallet.AccrueFrom(155);
            _wallet.AccrueFrom(155);

            Assert.AreEqual(InitialBudget + 30, _wallet.Balance);
        }

        [Test]
        public void AccrueFrom_NegativePrice_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.AccrueFrom(-100));
        }

        [Test]
        public void AccrueFrom_Value_RaisesBalanceChangedOnce()
        {
            var raised = 0;
            var lastValue = 0;
            _wallet.BalanceChanged += balance =>
            {
                raised++;
                lastValue = balance;
            };

            _wallet.AccrueFrom(100);

            Assert.AreEqual(1, raised);
            Assert.AreEqual(InitialBudget + 10, lastValue);
        }

        // ── 지불 ──────────────────────────────────────────────

        [Test]
        public void CanAfford_ExactBalance_ReturnsTrue()
        {
            Assert.IsTrue(_wallet.CanAfford(InitialBudget));
        }

        [Test]
        public void CanAfford_AboveBalance_ReturnsFalse()
        {
            Assert.IsFalse(_wallet.CanAfford(InitialBudget + 1));
        }

        [Test]
        public void CanAfford_InsufficientBalance_DoesNotDeduct()
        {
            // 검사에 부수효과가 없다. 있으면 TryPlace 가 두 번 차감한다.
            _wallet.CanAfford(InitialBudget + 1000);

            Assert.AreEqual(InitialBudget, _wallet.Balance);
        }

        [Test]
        public void TrySpend_SufficientBalance_DeductsAndReturnsTrue()
        {
            Assert.IsTrue(_wallet.TrySpend(20));
            Assert.AreEqual(InitialBudget - 20, _wallet.Balance);
        }

        [Test]
        public void TrySpend_InsufficientBalance_ReturnsFalseAndKeepsBalance()
        {
            // 부분 차감이 없다.
            Assert.IsFalse(_wallet.TrySpend(InitialBudget + 1));
            Assert.AreEqual(InitialBudget, _wallet.Balance);
        }

        [Test]
        public void TrySpend_InsufficientBalance_DoesNotRaise()
        {
            var raised = 0;
            _wallet.BalanceChanged += _ => raised++;

            _wallet.TrySpend(InitialBudget + 1);

            Assert.AreEqual(0, raised);
        }

        [Test]
        public void TrySpend_ZeroCost_SucceedsWithoutChange()
        {
            Assert.IsTrue(_wallet.TrySpend(0));
            Assert.AreEqual(InitialBudget, _wallet.Balance);
        }

        [Test]
        public void TrySpend_NegativeCost_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend(-10));
        }

        // ── 리셋 ──────────────────────────────────────────────

        [Test]
        public void Reset_AfterSpending_ReturnsToInitialBudget()
        {
            // 스테이지 간 이월이 없다 (착수 시 확정). 각 스테이지가 초기 예산으로 시작한다.
            _wallet.TrySpend(30);
            _wallet.AccrueFrom(1000);

            _wallet.Reset();

            Assert.AreEqual(InitialBudget, _wallet.Balance);
        }
    }
}
