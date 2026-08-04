using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 초밥은 풀에서 재사용되므로 <b>인스턴스 참조로 "그 초밥"을 추적하면 안 된다</b> —
    /// 반납된 객체가 다음 스폰에 그대로 다시 나온다. 여기서는 순차번호로 식별한다.
    /// </summary>
    public sealed class ClaimCoordinatorTests
    {
        private const float Interval = 1f;
        private const float Speed = 10f;
        private const float Length = 100f;
        private const float Reach = 12f;

        /// <summary>스폰 지점(0)이 이미 집기 범위 안인 자리. 스폰 즉시 판정이 돈다.</summary>
        private const float NearTable = 2f;

        /// <summary>스폰 지점이 범위 밖인 자리. 초밥이 흘러와야 인식된다.</summary>
        private const float FarTable = 60f;

        /// <summary>먹는 시간이 관측 가능해야 하는 테스트용. 기본 하네스는 0 이다.</summary>
        private const float EatSeconds = 2f;

        private const float DigestSeconds = 3f;

        /// <summary>덱에 든 초밥의 가격. 매출·재화 기대값을 여기서 끌어온다.</summary>
        private const int SushiPrice = 200;

        private readonly List<Object> _disposables = new();

        private StageConfig _config;
        private CustomerData _customerData;
        private SushiBelt _belt;
        private ClaimCoordinator _coordinator;
        private SequenceNumberIssuer _customerSequence;
        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private List<Claim> _claims;
        private List<Claim> _eaten;

        private readonly struct Claim
        {
            public int CustomerSequence { get; }
            public int SushiSequence { get; }
            public float BeltPositionAtClaim { get; }

            public Claim(CustomerLogic customer, SushiItem sushi)
            {
                CustomerSequence = customer.State.SequenceNumber;
                SushiSequence = sushi.SequenceNumber;
                BeltPositionAtClaim = sushi.BeltPosition;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(_customerData, "_reach", Reach);
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", 5);

            _customerSequence = new SequenceNumberIssuer();
            Rebuild(latchSeconds: 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
            Object.DestroyImmediate(_customerData);
            Object.DestroyImmediate(_config);

            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 인식 ────────────────────────────────────────────────

        [Test]
        public void Tick_SushiOutOfReach_NotRecognized()
        {
            var customer = Place(FarTable);

            _coordinator.Tick(Interval);

            Assert.IsEmpty(_coordinator.CandidatesOf(customer));
            Assert.IsEmpty(_claims);
        }

        [Test]
        public void Tick_SushiEntersReach_IsRecognizedAndClaimed()
        {
            Place(FarTable);
            _coordinator.Tick(Interval);

            // 스폰 지점(0)에서 범위 시작점(FarTable − Reach = 48)까지는 4.8초가 걸린다.
            // 넉넉히 흘려보낸다.
            for (var i = 0; i < 20; i++)
            {
                _coordinator.Tick(0.5f);
            }

            Assert.IsNotEmpty(_claims);
        }

        [Test]
        public void Tick_TwoSushiInReach_RecognizesBothButClaimsOne()
        {
            // 한 틱에 두 개가 스폰되고 둘 다 범위 안이다. 한 손님은 한 번에 하나만 집으므로
            // 나머지 하나가 후보로 남아 "인식됐다" 를 밖에서 확인할 수 있다.
            var customer = Place(NearTable);

            _coordinator.Tick(Interval * 2f);

            Assert.AreEqual(1, _claims.Count, "한 번에 하나만 집는다");
            Assert.AreEqual(2, _coordinator.CandidatesOf(customer).Count, "둘 다 인식은 됐다");
        }

        // ── 배정 ────────────────────────────────────────────────

        [Test]
        public void Tick_MultipleSushiInReach_TakesLowestSequence()
        {
            Place(NearTable);

            _coordinator.Tick(Interval * 2f);

            Assert.AreEqual(0, _claims[0].SushiSequence);
        }

        [Test]
        public void Tick_ManyCustomersOneSushi_LowerCustomerSequenceWins()
        {
            Place(NearTable);
            Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.AreEqual(1, _claims.Count);
            Assert.AreEqual(0, _claims[0].CustomerSequence);
        }

        // ── 먹는 시간 (M2 에서 M1 계약이 바뀐 지점) ──────────────

        [Test]
        public void Tick_ClaimAssigned_SushiStaysOnBeltWhileEating()
        {
            // M1 은 배정이 곧 소비였다. M2 는 그 사이에 먹는 시간이 있고,
            // 그동안 초밥은 Claimed 상태로 벨트에 남는다.
            SetEatSeconds(EatSeconds);
            var customer = Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.AreEqual(CustomerState.Eating, customer.State.State);
            Assert.AreEqual(1, _belt.ActiveSushi.Count);
            Assert.AreEqual(SushiState.Claimed, _belt.ActiveSushi[0].State);
            Assert.IsEmpty(_eaten, "아직 먹는 중이다");
        }

        [Test]
        public void Tick_EatSecondsElapsed_SushiRemovedFromBelt()
        {
            SetEatSeconds(EatSeconds);
            Place(NearTable);
            _coordinator.Tick(Interval);
            var claimedSequence = _claims[0].SushiSequence;

            _coordinator.Tick(EatSeconds);

            Assert.IsNotEmpty(_eaten);
            Assert.AreEqual(claimedSequence, _eaten[0].SushiSequence);
            foreach (var onBelt in _belt.ActiveSushi)
            {
                Assert.AreNotEqual(claimedSequence, onBelt.SequenceNumber, "먹은 초밥은 내려간다");
            }
        }

        [Test]
        public void Tick_EatingCustomer_NotAssignedAnotherSushi()
        {
            // 한 번에 하나 (착수 시 확정). 먹는 동안에는 자격이 없다.
            SetEatSeconds(EatSeconds);
            Place(NearTable);
            _coordinator.Tick(Interval);

            _coordinator.Tick(Interval);

            Assert.AreEqual(1, _claims.Count);
        }

        [Test]
        public void Tick_SushiEaten_ForgottenByClaimer()
        {
            var customer = Place(NearTable);
            _coordinator.Tick(Interval);

            _coordinator.Tick(Interval);

            foreach (var candidate in _coordinator.CandidatesOf(customer))
            {
                Assert.AreNotEqual(_eaten[0].SushiSequence, candidate.SequenceNumber);
            }
        }

        [Test]
        public void Tick_SushiClaimedByOne_NotClaimableByOthers()
        {
            // 먹는 동안 초밥이 벨트에 남지만 Claimed 라 두 번째 손님이 가져갈 수 없다.
            SetEatSeconds(EatSeconds);
            Place(NearTable);
            Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.AreEqual(1, _claims.Count, "같은 초밥을 둘이 나눠 갖지 않는다");
        }

        [Test]
        public void Tick_ClaimedSushiReachesBeltEnd_IsLostNotEaten()
        {
            // 먹다 만 초밥이 끝점을 지나면 놓친다 — 의도된 동작이다.
            // 손님은 Eating 에 갇히지 않고 Idle 로 돌아와야 한다.
            SetEatSeconds(EatSeconds * 100f);
            Place(Length - 5f);

            for (var i = 0; i < 40; i++)
            {
                _coordinator.Tick(0.5f);
            }

            Assert.IsEmpty(_eaten, "먹는 시간이 끝나기 전에 끝점을 지났다");
            Assert.AreEqual(0, _revenue.Total, "놓친 초밥은 매출이 아니다");

            // 두 번 이상 집었다는 것이 곧 Eating 에서 풀려났다는 증거다.
            // 갇혀 있었다면 첫 배정 이후로 영영 한 건에 머문다.
            Assert.Greater(_claims.Count, 1,
                           "먹던 초밥이 사라졌으면 다시 집을 수 있어야 한다");
        }

        // ── 경제 반영 ───────────────────────────────────────────

        [Test]
        public void Tick_SushiEaten_AddsPriceToRevenue()
        {
            Place(NearTable);

            _coordinator.Tick(Interval);
            _coordinator.Tick(Interval);

            Assert.IsNotEmpty(_eaten);
            Assert.AreEqual(SushiPrice, _revenue.Total);
        }

        [Test]
        public void Tick_SushiEaten_AccruesRecruitCurrency()
        {
            Place(NearTable);

            _coordinator.Tick(Interval);
            _coordinator.Tick(Interval);

            Assert.AreEqual(SushiPrice / 10, _wallet.Balance);
        }

        [Test]
        public void Tick_SushiClaimedNotYetEaten_RevenueUnchanged()
        {
            // 매출은 배정이 아니라 소비에 붙는다.
            SetEatSeconds(EatSeconds);
            Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.IsNotEmpty(_claims);
            Assert.AreEqual(0, _revenue.Total);
            Assert.AreEqual(0, _wallet.Balance);
        }

        // ── 틱 순서 계약 ────────────────────────────────────────

        [Test]
        public void Tick_RemovedBeforeRecognize_NoGhostClaim()
        {
            // 끝점에서 내려간 초밥이 같은 틱에 인식되면 유령 배정이 된다 (계약 1 → 2).
            Place(Length - 1f);

            for (var i = 0; i < 60; i++)
            {
                _coordinator.Tick(0.5f);
            }

            foreach (var claim in _claims)
            {
                Assert.LessOrEqual(claim.BeltPositionAtClaim, Length,
                                   "벨트를 벗어난 초밥이 배정됐다");
            }
        }

        [Test]
        public void Tick_LatchExpiredBeforeResolve_NotClaimed()
        {
            // 만료가 배정보다 뒤에 오면 상한이 무의미해진다 (계약 3 → 5).
            Rebuild(latchSeconds: 0.5f);
            Place(NearTable);

            // 두 개가 인식되고 하나만 배정된다. 남은 하나(순차번호 1)가 만료 대상이다.
            _coordinator.Tick(Interval * 2f);
            Assert.AreEqual(1, _claims.Count);

            // 래치(0.5초)보다 길게 흘린다.
            _coordinator.Tick(Interval);

            foreach (var claim in _claims)
            {
                Assert.AreNotEqual(1, claim.SushiSequence, "만료된 후보가 배정됐다");
            }
        }

        // ── 자격 ────────────────────────────────────────────────

        [Test]
        public void Tick_CustomerLosesEligibility_CandidatesCleared()
        {
            // 포화도로 자격을 없앤다. 상태 필드를 직접 건드리지 않는 이유는 이제
            // 상태가 식욕 머신의 소유이기 때문이다 — 밖에서 쓰면 머신의 타이머와
            // 어긋난 상태가 만들어져 테스트가 실제 동작을 검증하지 않게 된다.
            var customer = Place(NearTable);
            customer.State.CurrentSaturation = _customerData.MaxSaturation;

            _coordinator.Tick(Interval * 2f);

            Assert.IsEmpty(_coordinator.CandidatesOf(customer));
            Assert.IsEmpty(_claims);
        }

        [Test]
        public void Tick_CustomerFull_DoesNotClaim()
        {
            var customer = Place(NearTable);
            customer.State.CurrentSaturation = _customerData.MaxSaturation;

            _coordinator.Tick(Interval);

            Assert.IsEmpty(_claims);
        }

        [Test]
        public void Tick_CustomerRegainsEligibility_ClaimsAgain()
        {
            var customer = Place(NearTable);
            customer.State.CurrentSaturation = _customerData.MaxSaturation;
            _coordinator.Tick(Interval);
            Assert.IsEmpty(_claims, "자격이 없는 동안에는 집지 않는다");

            customer.State.CurrentSaturation = 0;
            _coordinator.Tick(Interval);

            Assert.IsNotEmpty(_claims, "자격을 되찾으면 이미 벨트에 있는 초밥도 다시 본다");
        }

        [Test]
        public void Tick_DigestionCompletes_ResumesClaimingSameTick()
        {
            // 틱 순서 계약 — 식욕 진행(2)이 자격 정리(5)·배정(6)보다 앞이라
            // 소화가 끝난 그 틱에 바로 다시 집는다. 뒤에 두면 한 틱씩 굶는다.
            Rebuild(latchSeconds: 0f, saturationPerSushi: 1);
            SetSaturationBudget(1);
            SetDigestSeconds(DigestSeconds);
            Place(NearTable);

            _coordinator.Tick(Interval);
            _coordinator.Tick(Interval);
            Assert.AreEqual(1, _claims.Count, "포화되어 소화 중이다");

            _coordinator.Tick(DigestSeconds);

            Assert.AreEqual(2, _claims.Count, "소화가 끝난 틱에 바로 다시 집는다");
        }

        [Test]
        public void Tick_EatingFinishesThisTick_ClaimsAgainSameTick()
        {
            // 같은 계약의 다른 면 — 먹기를 마친 손님이 같은 틱에 새 배정을 받는다.
            Place(NearTable);
            _coordinator.Tick(Interval);
            Assert.AreEqual(1, _claims.Count);

            _coordinator.Tick(Interval);

            Assert.AreEqual(2, _claims.Count, "먹기 완료와 새 배정이 같은 틱에 일어난다");
        }

        // ── 수명 ────────────────────────────────────────────────

        [Test]
        public void Dispose_AfterPlacements_StopsReactingToBelt()
        {
            Place(NearTable);
            _coordinator.Dispose();
            var claimsBefore = _claims.Count;

            _belt.Tick(Interval);

            Assert.AreEqual(claimsBefore, _claims.Count);
        }

        [Test]
        public void RemoveCustomer_WhileEating_ReleasesSushiWithoutRevenue()
        {
            SetEatSeconds(EatSeconds);
            var customer = Place(NearTable);
            _coordinator.Tick(Interval);
            Assert.IsNotNull(_coordinator.EatingOf(customer));

            _coordinator.RemoveCustomer(customer);

            Assert.IsEmpty(_belt.ActiveSushi, "먹다 만 초밥이 벨트에 남아 자리를 차지하면 안 된다");
            Assert.AreEqual(0, _revenue.Total, "소비가 끝나지 않았으므로 매출이 아니다");
        }

        // ── 진행 중 배치 (Q4) ───────────────────────────────────

        [Test]
        public void PlaceCustomer_MidStage_ParticipatesInNextResolve()
        {
            _coordinator.Tick(Interval);
            _coordinator.Tick(Interval);
            Assert.IsEmpty(_claims, "아직 손님이 없다");

            Place(NearTable);
            _coordinator.Tick(Interval);

            Assert.IsNotEmpty(_claims);
        }

        [Test]
        public void PlaceCustomer_MidStage_SeesSushiAlreadyInReach()
        {
            // 이미 흐르고 있는 초밥의 위치에 맞춰 앉힌다.
            _coordinator.Tick(Interval);
            var inFlight = _belt.ActiveSushi[0];

            Place(inFlight.BeltPosition);
            _coordinator.Tick(0.01f);

            Assert.IsNotEmpty(_claims, "배치 시점에 이미 범위 안인 초밥도 인식해야 한다");
        }

        [Test]
        public void PlaceCustomer_MidStage_DoesNotDisturbExistingClaims()
        {
            var veteran = Place(NearTable);
            _coordinator.Tick(Interval);
            var claimsBefore = _claims.Count;

            Place(NearTable);
            _coordinator.Tick(Interval);

            Assert.AreEqual(veteran.State.SequenceNumber, _claims[claimsBefore - 1].CustomerSequence,
                            "기존 배정 결과가 바뀌면 안 된다");
        }

        [Test]
        public void RemoveCustomer_ClearsCandidatesAndStopsClaiming()
        {
            var customer = Place(FarTable);
            _coordinator.Tick(Interval);

            _coordinator.RemoveCustomer(customer);
            for (var i = 0; i < 8; i++)
            {
                _coordinator.Tick(0.5f);
            }

            Assert.IsEmpty(_coordinator.CandidatesOf(customer));
            Assert.IsEmpty(_claims);
            Assert.IsEmpty(_coordinator.Customers);
        }

        [Test]
        public void PlaceCustomer_Twice_Throws()
        {
            var customer = Place(NearTable);

            Assert.Throws<System.ArgumentException>(() => _coordinator.PlaceCustomer(customer));
        }

        // ── 결정성과 "구경하지 않는다" ──────────────────────────

        [Test]
        public void Tick_SameScenarioTwice_ProducesIdenticalClaims()
        {
            var first = RunScenario();
            var second = RunScenario();

            Assert.AreEqual(first.Count, second.Count);
            for (var i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i], second[i]);
            }
        }

        [Test]
        public void Tick_SushiInReachAndCustomerIdle_NeverSkipsAFrame()
        {
            // 집을 수 있는데 집지 않고 지나보내는 프레임이 있으면 안 된다 (CLAUDE.md §1.1-3a).
            var customer = Place(NearTable);

            for (var i = 0; i < 60; i++)
            {
                _coordinator.Tick(0.1f);

                if (!customer.CanAcceptSushi)
                {
                    continue;
                }

                foreach (var sushi in _coordinator.CandidatesOf(customer))
                {
                    Assert.AreNotEqual(SushiState.OnBelt, sushi.State,
                                       $"{i} 번째 틱에서 집을 수 있는 초밥을 그냥 두었다");
                }
            }
        }

        // ── 헬퍼 ────────────────────────────────────────────────

        private void Rebuild(float latchSeconds, int saturationPerSushi = 0)
        {
            _coordinator?.Dispose();
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }

            _config = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(Length)
                .WithRecognitionLatch(latchSeconds)
                .WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables, SushiPrice, saturationPerSushi))
                .Build();

            _belt = new SushiBelt(_config, new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
            _revenue = new RevenueLedger();
            _wallet = new RecruitWallet(0);
            _coordinator = new ClaimCoordinator(_belt, _config, _revenue, _wallet);

            _claims = new List<Claim>();
            _eaten = new List<Claim>();
            _coordinator.SushiClaimed += (customer, sushi) => _claims.Add(new Claim(customer, sushi));
            _coordinator.SushiEaten += (customer, sushi) => _eaten.Add(new Claim(customer, sushi));
        }

        private void SetEatSeconds(float seconds) =>
            SerializedFieldSetter.SetFloat(_customerData, "_eatSeconds", seconds);

        private void SetDigestSeconds(float seconds) =>
            SerializedFieldSetter.SetFloat(_customerData, "_digestSeconds", seconds);

        /// <summary>이만큼 먹으면 포화된다. 소화 전이를 관측할 때 쓴다.</summary>
        private void SetSaturationBudget(int maxSaturation) =>
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", maxSaturation);

        private CustomerLogic Place(float beltPosition)
        {
            var customer = new CustomerLogic(
                new CustomerRuntimeState(_customerData, _customerSequence.Next()), beltPosition);
            _coordinator.PlaceCustomer(customer);
            return customer;
        }

        private List<string> RunScenario()
        {
            Rebuild(latchSeconds: 0f);
            _customerSequence.Reset();

            Place(NearTable);
            Place(FarTable);

            for (var i = 0; i < 50; i++)
            {
                _coordinator.Tick(0.2f);
            }

            var snapshot = new List<string>();
            foreach (var claim in _claims)
            {
                snapshot.Add($"{claim.CustomerSequence}:{claim.SushiSequence}");
            }

            return snapshot;
        }
    }
}
