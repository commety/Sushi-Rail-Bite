using System;
using NUnit.Framework;
using SushiDefense.Scoring;

namespace SushiDefense.Tests.EditMode.Scoring
{
    /// <summary>
    /// 매출은 클리어 판정(M3)의 입력이다. <b>영입 재화와 섞지 않는다</b> — 두 개의 다른
    /// 자원이고, 하나로 합치면 "초기 예산" 과 "매출 유래분" 이 한 스트림에 섞여 잔액
    /// 추적이 어려워진다 (<c>.claude/domain/data-model.md</c> §4).
    /// </summary>
    public sealed class RevenueLedgerTests
    {
        private RevenueLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            _ledger = new RevenueLedger();
        }

        [Test]
        public void Total_NewLedger_IsZero()
        {
            Assert.AreEqual(0, _ledger.Total);
        }

        [Test]
        public void Add_SushiEaten_IncreasesByPrice()
        {
            _ledger.Add(150);

            Assert.AreEqual(150, _ledger.Total);
        }

        [Test]
        public void Add_MultipleSushi_AccumulatesTotal()
        {
            _ledger.Add(150);
            _ledger.Add(300);
            _ledger.Add(100);

            Assert.AreEqual(550, _ledger.Total);
        }

        [Test]
        public void Add_NegativePrice_Throws()
        {
            // 조용히 통과시키면 매출이 줄어드는 경로가 생긴다.
            Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.Add(-1));
        }

        [Test]
        public void Add_NegativePrice_LeavesTotalUntouched()
        {
            _ledger.Add(200);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ledger.Add(-50));
            Assert.AreEqual(200, _ledger.Total);
        }

        [Test]
        public void Add_Value_RaisesTotalChangedOnce()
        {
            var raised = 0;
            var lastValue = 0;
            _ledger.TotalChanged += total =>
            {
                raised++;
                lastValue = total;
            };

            _ledger.Add(120);

            Assert.AreEqual(1, raised);
            Assert.AreEqual(120, lastValue);
        }

        [Test]
        public void Add_Zero_DoesNotRaise()
        {
            // 값이 실제로 바뀔 때만 알린다. 매 프레임 0 을 더하는 호출자가 생겨도
            // UI 가 헛돌지 않는다.
            var raised = 0;
            _ledger.TotalChanged += _ => raised++;

            _ledger.Add(0);

            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Reset_AfterAdds_ReturnsToZero()
        {
            _ledger.Add(500);

            _ledger.Reset();

            Assert.AreEqual(0, _ledger.Total);
        }

        [Test]
        public void Reset_AlreadyZero_DoesNotRaise()
        {
            var raised = 0;
            _ledger.TotalChanged += _ => raised++;

            _ledger.Reset();

            Assert.AreEqual(0, raised);
        }
    }
}
