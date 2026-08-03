using NUnit.Framework;
using SushiDefense;

namespace SushiDefense.Tests.EditMode
{
    public sealed class SequenceNumberIssuerTests
    {
        [Test]
        public void Next_CalledRepeatedly_IncrementsByOne()
        {
            var issuer = new SequenceNumberIssuer();

            var first = issuer.Next();
            var second = issuer.Next();
            var third = issuer.Next();

            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(2, third);
        }

        [Test]
        public void Next_TwoIssuers_AreIndependent()
        {
            var sushiIssuer = new SequenceNumberIssuer();
            var customerIssuer = new SequenceNumberIssuer();

            sushiIssuer.Next();
            sushiIssuer.Next();

            Assert.AreEqual(0, customerIssuer.Next());
        }

        [Test]
        public void Reset_AfterIssuing_RestartsFromZero()
        {
            var issuer = new SequenceNumberIssuer();
            issuer.Next();
            issuer.Next();

            issuer.Reset();

            Assert.AreEqual(0, issuer.Next());
        }

        [Test]
        public void Next_SameCallSequenceTwice_ProducesIdenticalNumbers()
        {
            var first = new SequenceNumberIssuer();
            var second = new SequenceNumberIssuer();

            var firstRun = new[] { first.Next(), first.Next(), first.Next() };
            var secondRun = new[] { second.Next(), second.Next(), second.Next() };

            Assert.AreEqual(firstRun, secondRun);
        }
    }
}
