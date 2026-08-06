using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Stages
{
    /// <summary>
    /// 컨트롤러는 <b>틱 순서와 정지</b>를 책임진다. 판정 규칙 자체는
    /// <c>StageEvaluatorTests</c> 가 이미 고정했으므로 여기서 되풀이하지 않는다.
    ///
    /// <para>
    /// 기본 하네스는 <b>덱이 비어 있다</b> — 초밥이 스폰되지 않으므로 매출은 테스트가
    /// 원장에 직접 넣을 때만 움직인다. 판정을 볼 때 벨트의 타이밍이 끼어들지 않게 하기
    /// 위해서다. 실제 소비가 필요한 두 테스트만 <see cref="RebuildWithLiveDeck"/> 를 쓴다.
    /// </para>
    /// </summary>
    public sealed class StageControllerTests
    {
        private const int Target = 200;
        private const float Speed = 10f;
        private const float Length = 100f;
        private const float Interval = 1f;

        /// <summary>스폰 지점(0)이 이미 집기 범위 안인 자리. 스폰 즉시 판정이 돈다.</summary>
        private const float NearTable = 2f;

        private const float Reach = 12f;
        private const float Tolerance = 0.0001f;

        private readonly List<Object> _disposables = new();

        private StageConfig _config;
        private SushiBelt _belt;
        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private ClaimCoordinator _coordinator;
        private StageController _controller;
        private List<StageOutcome> _decided;

        [SetUp]
        public void SetUp()
        {
            Rebuild(timeLimitSeconds: 10f, liveDeck: false);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
            Object.DestroyImmediate(_config);

            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 판정 ────────────────────────────────────────────────

        [Test]
        public void Outcome_BeforeAnyTick_IsInProgress()
        {
            Assert.AreEqual(StageOutcome.InProgress, _controller.Outcome);
            Assert.IsTrue(_controller.IsRunning);
        }

        [Test]
        public void Tick_BelowTargetWithinTime_StaysInProgress()
        {
            _revenue.Add(Target - 1);

            _controller.Tick(1f);

            Assert.AreEqual(StageOutcome.InProgress, _controller.Outcome);
            Assert.IsTrue(_controller.IsRunning);
        }

        [Test]
        public void Tick_TargetReached_OutcomeIsCleared()
        {
            _revenue.Add(Target);

            _controller.Tick(1f);

            Assert.AreEqual(StageOutcome.Cleared, _controller.Outcome);
            Assert.IsFalse(_controller.IsRunning);
        }

        [Test]
        public void Tick_TimeoutWithoutTarget_OutcomeIsFailed()
        {
            _controller.Tick(10f);

            Assert.AreEqual(StageOutcome.Failed, _controller.Outcome);
            Assert.IsFalse(_controller.IsRunning);
        }

        [Test]
        public void Tick_TargetReached_RaisesOutcomeDecidedOnce()
        {
            _revenue.Add(Target);

            _controller.Tick(1f);

            Assert.AreEqual(1, _decided.Count);
            Assert.AreEqual(StageOutcome.Cleared, _decided[0]);
        }

        [Test]
        public void Tick_AfterDecided_RaisesNothingMore()
        {
            _revenue.Add(Target);
            _controller.Tick(1f);

            for (var i = 0; i < 20; i++)
            {
                _controller.Tick(1f);
            }

            Assert.AreEqual(1, _decided.Count);
        }

        [Test]
        public void Tick_AfterDecided_KeepsTheFirstOutcome()
        {
            _controller.Tick(10f);
            Assert.AreEqual(StageOutcome.Failed, _controller.Outcome);

            // 판정 뒤에 목표를 채워도 결과는 뒤집히지 않는다.
            _revenue.Add(Target * 10);
            _controller.Tick(1f);

            Assert.AreEqual(StageOutcome.Failed, _controller.Outcome);
        }

        // ── 시간 ────────────────────────────────────────────────

        [Test]
        public void RemainingSeconds_Midway_ReportsRemainder()
        {
            _controller.Tick(2.5f);

            Assert.AreEqual(7.5f, _controller.RemainingSeconds, Tolerance);
        }

        [Test]
        public void RemainingSeconds_AfterDecided_StopsCountingDown()
        {
            _revenue.Add(Target);
            _controller.Tick(2.5f);

            _controller.Tick(5f);

            Assert.AreEqual(7.5f, _controller.RemainingSeconds, Tolerance);
        }

        /// <summary>
        /// 음수 델타는 조율자에 닿기 <b>전에</b> 막혀야 한다. 조율자를 먼저 굴리면 벨트와
        /// 경과 시간이 되감긴 상태로 예외가 나 판이 오염된다.
        /// </summary>
        [Test]
        public void Tick_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _controller.Tick(-1f));
        }

        [Test]
        public void Tick_Zero_IsAllowed()
        {
            _controller.Tick(0f);

            Assert.AreEqual(StageOutcome.InProgress, _controller.Outcome);
        }

        // ── 정지 (작업서 D3) ────────────────────────────────────

        /// <summary>
        /// <b>아래 정지 테스트의 대조군이다.</b> 이것이 없으면 초밥이 한 개도 안 나오는
        /// 하네스에서 "정지했다" 가 공허하게 통과한다 (<c>.claude/rules/tests.md</c> §3).
        /// </summary>
        [Test]
        public void Tick_WhileRunning_RevenueGrows()
        {
            // 목표를 높여 둔다. 기본 목표(200)는 초밥 한 개 값이라 두 번째 틱에 클리어돼
            // "진행 중" 을 관측할 구간이 없다.
            RebuildWithLiveDeck(timeLimitSeconds: 100f, targetRevenue: Target * 10);

            for (var i = 0; i < 5; i++)
            {
                _controller.Tick(Interval);
            }

            Assert.IsTrue(_controller.IsRunning, "제한 시간 100초 안에서 아직 진행 중이어야 한다");
            Assert.That(_revenue.Total, Is.GreaterThan(0), "살아 있는 덱에서 매출이 늘어야 한다");
        }

        [Test]
        public void Tick_AfterDecided_RevenueStopsGrowing()
        {
            RebuildWithLiveDeck(timeLimitSeconds: 100f);

            while (_controller.IsRunning)
            {
                _controller.Tick(Interval);
            }

            var frozen = _revenue.Total;
            for (var i = 0; i < 20; i++)
            {
                _controller.Tick(Interval);
            }

            Assert.AreEqual(StageOutcome.Cleared, _controller.Outcome);
            Assert.AreEqual(frozen, _revenue.Total, "판정 뒤에도 벨트가 돌면 매출이 계속 오른다");
        }

        /// <summary>
        /// <b>D2 와 D3 가 함께 걸리는 지점이다.</b>
        ///
        /// <para>
        /// 하네스가 이렇게 움직인다 — 틱 1 에서 초밥이 스폰·인식·배정되고, 먹는 시간이 0 이라
        /// <b>틱 2 의 식욕 처리에서 소비가 확정</b>돼 매출 200 이 붙는다. 제한 시간을 정확히
        /// 2초로 두면 그 틱에 시계도 만료된다.
        /// </para>
        /// <para>
        /// <b>이 테스트가 잡는 것은 "판정이 조율자 틱 뒤에 온다" 하나다.</b> 시계를 먼저 흘리고
        /// 판정한 뒤 조율자를 굴리는 구현을 넣어 확인했다 — 매출이 0 인 채로 만료가 걸려
        /// <c>Failed</c> 가 났다.
        /// </para>
        /// <para>
        /// 반대로 <b>조율자와 시계의 상대 순서는 이 테스트가 잡지 못하며, 잡을 것도 없다</b> —
        /// 둘을 바꿔 넣어도 357개가 전부 통과했다. 서로의 값을 읽지 않으므로 판정이 마지막에
        /// 오는 한 최종 상태가 같다. 정각 달성이 클리어인 것(D2)은 <c>StageEvaluatorTests</c>
        /// 가 따로 고정한다.
        /// </para>
        /// </summary>
        [Test]
        public void Tick_TargetReachedOnTheExpiringFrame_Clears()
        {
            RebuildWithLiveDeck(timeLimitSeconds: 2f);

            _controller.Tick(Interval);
            Assert.AreEqual(0, _revenue.Total, "첫 틱은 배정까지만 간다 — 전제가 깨졌다");
            Assert.AreEqual(StageOutcome.InProgress, _controller.Outcome);

            _controller.Tick(Interval);

            Assert.AreEqual(Target, _revenue.Total, "두 번째 틱에 소비가 확정돼야 한다 — 전제가 깨졌다");
            Assert.AreEqual(0f, _controller.RemainingSeconds, Tolerance);
            Assert.AreEqual(StageOutcome.Cleared, _controller.Outcome);
        }

        // ── 헬퍼 ────────────────────────────────────────────────

        private void RebuildWithLiveDeck(float timeLimitSeconds, int targetRevenue = Target)
        {
            Rebuild(timeLimitSeconds, liveDeck: true, targetRevenue);

            var customerData = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customerData);
            SerializedFieldSetter.SetFloat(customerData, "_reach", Reach);
            SerializedFieldSetter.SetInt(customerData, "_maxSaturation", 100);

            // 덱 가격(200)을 품는 대역. 대역 밖으로 두면 배정이 이탈 직전까지 밀려
            // 소비 시각이 예측 불가능해진다 (M2.5 에서 두 테스트 파일이 빠졌던 함정).
            SerializedFieldSetter.SetTargetingBand(customerData, 100, 300);

            var customer = new CustomerLogic(
                new CustomerRuntimeState(customerData, 0), NearTable);
            _coordinator.PlaceCustomer(customer);
        }

        private void Rebuild(float timeLimitSeconds, bool liveDeck, int targetRevenue = Target)
        {
            _coordinator?.Dispose();
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }

            var builder = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(Length)
                .WithRecognitionLatch(0f)
                .WithTargetRevenue(targetRevenue)
                .WithTimeLimit(timeLimitSeconds);

            if (liveDeck)
            {
                builder.WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables, Target));
            }

            _config = builder.Build();

            _belt = new SushiBelt(_config, new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
            _revenue = new RevenueLedger();
            _wallet = new RecruitWallet(0);
            _coordinator = new ClaimCoordinator(_belt, _config, _revenue, _wallet);

            _decided = new List<StageOutcome>();
            _controller = new StageController(_coordinator, _revenue, _config);
            _controller.OutcomeDecided += outcome => _decided.Add(outcome);
        }
    }
}
