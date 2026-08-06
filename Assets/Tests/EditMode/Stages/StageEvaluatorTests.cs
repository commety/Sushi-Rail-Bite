using NUnit.Framework;
using SushiDefense.Stages;

namespace SushiDefense.Tests.EditMode.Stages
{
    /// <summary>
    /// 클리어/실패 규칙만 본다. <b>시간을 모른다</b> — 만료 여부가 <c>bool</c> 로 들어오므로
    /// 경계 규칙을 시계 없이 고정할 수 있다 (작업서 D1).
    /// </summary>
    public sealed class StageEvaluatorTests
    {
        private const int Target = 1000;

        [Test]
        public void Evaluate_TargetReachedBeforeTimeout_Clears()
        {
            var outcome = StageEvaluator.Evaluate(Target, Target, expired: false);

            Assert.AreEqual(StageOutcome.Cleared, outcome);
        }

        [Test]
        public void Evaluate_TimeoutBeforeTarget_Fails()
        {
            var outcome = StageEvaluator.Evaluate(Target - 1, Target, expired: true);

            Assert.AreEqual(StageOutcome.Failed, outcome);
        }

        /// <summary>
        /// 같은 만료 상태에서 목표만 1 낮추면 클리어가 된다. 이 반례가 없으면 늘
        /// <c>Failed</c> 를 돌려주는 구현도 위 테스트를 통과한다
        /// (<c>.claude/rules/tests.md</c> §3 — 공허하게 통과하는 테스트).
        /// </summary>
        [Test]
        public void Evaluate_TimeoutWithTargetOneLower_ClearsInstead()
        {
            var outcome = StageEvaluator.Evaluate(Target - 1, Target - 1, expired: true);

            Assert.AreEqual(StageOutcome.Cleared, outcome);
        }

        /// <summary>
        /// <b>이 마일스톤의 경계 결정이다</b> (작업서 D2). 판정이 매출을 먼저 보므로 제한
        /// 시간 정각에 목표를 채우면 클리어다. 순서를 뒤집으면 같은 프레임에 목표를 채운
        /// 플레이어가 실패한다.
        /// </summary>
        [Test]
        public void Evaluate_TargetReachedExactlyAtTimeout_Clears()
        {
            var outcome = StageEvaluator.Evaluate(Target, Target, expired: true);

            Assert.AreEqual(StageOutcome.Cleared, outcome);
        }

        [Test]
        public void Evaluate_BelowTargetNotExpired_ReturnsInProgress()
        {
            var outcome = StageEvaluator.Evaluate(Target - 1, Target, expired: false);

            Assert.AreEqual(StageOutcome.InProgress, outcome);
        }

        [Test]
        public void Evaluate_ZeroRevenueNotExpired_ReturnsInProgress()
        {
            var outcome = StageEvaluator.Evaluate(0, Target, expired: false);

            Assert.AreEqual(StageOutcome.InProgress, outcome);
        }

        [Test]
        public void Evaluate_RevenueOverTarget_Clears()
        {
            var outcome = StageEvaluator.Evaluate(Target * 3, Target, expired: false);

            Assert.AreEqual(StageOutcome.Cleared, outcome);
        }
    }
}
