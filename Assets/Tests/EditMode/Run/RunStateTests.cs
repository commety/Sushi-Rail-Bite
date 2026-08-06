using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 런은 스테이지가 끝나도 남는 것을 담는다 — 덱 · 명부 · 진행 스테이지.
    ///
    /// <para>
    /// <b>매출·영입 재화는 여기 없다.</b> 둘은 스테이지마다 리셋되므로 런에 넣으면 이월이
    /// 생긴 것처럼 읽힌다 (<c>.claude/domain/data-model.md</c> §4).
    /// </para>
    /// </summary>
    public sealed class RunStateTests
    {
        private const int Seed = 4242;

        private readonly List<Object> _disposables = new();

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
        public void StageNumber_NewRun_StartsAtOne()
        {
            var run = NewRun();

            Assert.AreEqual(1, run.StageNumber);
        }

        [Test]
        public void AttemptNumber_NewRun_StartsAtOne()
        {
            var run = NewRun();

            Assert.AreEqual(1, run.AttemptNumber);
        }

        [Test]
        public void AdvanceStage_AfterClear_IncrementsStageAndResetsAttempt()
        {
            var run = NewRun();
            run.RecordFailedAttempt();
            run.RecordFailedAttempt();

            run.AdvanceStage();

            Assert.AreEqual(2, run.StageNumber);
            Assert.AreEqual(1, run.AttemptNumber);
        }

        /// <summary>
        /// <b>착수 시 확정한 규칙이다</b> — 실패하면 런을 끝내지 않고 같은 스테이지를 다시
        /// 한다. 스테이지 번호가 그대로임을 함께 확인한다: 시도 횟수만 보면 런을 리셋하는
        /// 구현도 통과한다.
        /// </summary>
        [Test]
        public void RecordFailedAttempt_AfterFailure_KeepsStageAndIncrementsAttempt()
        {
            var run = NewRun();

            run.RecordFailedAttempt();

            Assert.AreEqual(1, run.StageNumber);
            Assert.AreEqual(2, run.AttemptNumber);
        }

        [Test]
        public void RecordFailedAttempt_Twice_KeepsCounting()
        {
            var run = NewRun();

            run.RecordFailedAttempt();
            run.RecordFailedAttempt();

            Assert.AreEqual(3, run.AttemptNumber);
        }

        /// <summary>실패해도 덱이 남는다는 것이 "런은 유지된다" 의 실체다.</summary>
        [Test]
        public void RecordFailedAttempt_AfterFailure_KeepsDeckAndRoster()
        {
            var run = NewRun();
            run.Sushi.TryAdd(CreateSushi());
            run.Customers.TryAdd(CreateCustomer());

            run.RecordFailedAttempt();

            Assert.AreEqual(1, run.Sushi.Count);
            Assert.AreEqual(1, run.Customers.Count);
        }

        [Test]
        public void Sushi_RewardAdded_IsVisibleInDeck()
        {
            var run = NewRun();
            var card = CreateSushi();

            Assert.IsTrue(run.Sushi.TryAdd(card));

            Assert.IsTrue(run.Sushi.Contains(card));
        }

        [Test]
        public void Customers_RewardAdded_IsVisibleInRoster()
        {
            var run = NewRun();
            var member = CreateCustomer();

            Assert.IsTrue(run.Customers.TryAdd(member));

            Assert.IsTrue(run.Customers.Contains(member));
        }

        [Test]
        public void Constructor_NullDecks_StartsEmptyInsteadOfThrowing()
        {
            var run = new RunState(null, null, Seed);

            Assert.AreEqual(0, run.Sushi.Count);
            Assert.AreEqual(0, run.Customers.Count);
        }

        /// <summary>
        /// 난수를 들이면서도 결정성을 잃지 않았다는 것을 이 테스트가 지킨다 —
        /// 같은 시드의 두 런은 같은 수열을 낸다.
        /// </summary>
        [Test]
        public void Random_SameSeed_ProducesIdenticalDraws()
        {
            var left = NewRun();
            var right = NewRun();

            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual(left.Random.Next(100), right.Random.Next(100),
                                $"{i} 번째 뽑기가 어긋났습니다");
            }
        }

        [Test]
        public void Random_DifferentSeeds_ProduceDifferentDraws()
        {
            var left = new RunState(new SushiDeck(), new CustomerDeck(), Seed);
            var right = new RunState(new SushiDeck(), new CustomerDeck(), Seed + 1);

            var differs = false;
            for (var i = 0; i < 20; i++)
            {
                if (left.Random.Next(1000) != right.Random.Next(1000))
                {
                    differs = true;
                }
            }

            Assert.IsTrue(differs);
        }

        private RunState NewRun()
        {
            return new RunState(new SushiDeck(), new CustomerDeck(), Seed);
        }

        private SushiData CreateSushi()
        {
            return StageConfigBuilder.CreateSushi(_disposables);
        }

        private CustomerData CreateCustomer()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            return customer;
        }
    }
}
