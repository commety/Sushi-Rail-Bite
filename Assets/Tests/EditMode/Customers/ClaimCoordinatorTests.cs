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

            // 덱 가격(200)을 품는 대역. 이 파일의 테스트 대부분은 **배정**을 보는 것이라
            // 즉시 확정이 전제다. 대역 밖으로 두면 모든 배정이 이탈 직전까지 밀려
            // 무엇이 깨졌는지 구분되지 않는다 — 마감시한은 아래 전용 절에서 본다.
            SerializedFieldSetter.SetTargetingBand(_customerData, 100, 300);

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

        // ── 마감시한 (M2.5) ─────────────────────────────────────

        [Test]
        public void Tick_InBandSushiRecognized_ClaimsImmediately()
        {
            // 대역 안이면 이탈까지 한참 남았어도 기다리지 않는다.
            _customerSequence.Reset();
            Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.AreEqual(1, _claims.Count);
        }

        [Test]
        public void Tick_OnlyOutOfBandSushi_DefersUntilExit()
        {
            // 덱은 200 뿐인데 손님 대역은 400~500 이다. 즉시 확정이 남아 있으면
            // 첫 틱에 집어 버린다.
            SetTargetingBand(400, 500);
            _customerSequence.Reset();
            var customer = Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.IsEmpty(_claims, "대역 밖이면 이탈 직전까지 기다린다");
            Assert.IsTrue(_coordinator.IsWaiting(customer));
        }

        [Test]
        public void Tick_OnlyOutOfBandSushiInReach_TakesItBeforeExit()
        {
            // ★ 이 마일스톤의 불변식 — **모든 손님은 유한 시간 안에 반드시 집는다.**
            //
            // 손님 1명 · 대역 밖 초밥으로만 짠다. 경합이 끼면 못 집은 이유가 대기인지
            // 남이 가져가서인지 구분되지 않는다.
            //
            // 대역이 자격 게이트로 굳는 사고를 이 테스트 하나가 막는다.
            SetTargetingBand(400, 500);
            _customerSequence.Reset();
            Place(NearTable);

            for (var i = 0; i < 40; i++)
            {
                _coordinator.Tick(0.1f);
            }

            Assert.IsNotEmpty(_claims, "대역 밖뿐이어도 결국 집는다");
        }

        [Test]
        public void Tick_ShortLatchAndOutOfBand_ClaimsBeforeExpiry()
        {
            // 마감시한(now ≥ 이탈)이 만료(now > 이탈 + 래치)보다 **구조적으로** 먼저 온다.
            // 래치를 짧게 줘도 대기하던 손님이 초밥을 잃지 않는다는 것이 D4 의 결론이고,
            // 이 마일스톤의 불변식이 밸런스 값과 무관해지는 근거다.
            Rebuild(latchSeconds: 0.5f);
            SetTargetingBand(400, 500);
            _customerSequence.Reset();
            Place(NearTable);

            for (var i = 0; i < 40; i++)
            {
                _coordinator.Tick(0.1f);
            }

            Assert.IsNotEmpty(_claims, "래치가 짧아도 마감시한이 먼저 와서 집는다");
        }

        [Test]
        public void Tick_EatingCustomer_IsNotWaiting()
        {
            // 먹는 중은 대기가 아니다. 둘을 구분하지 않으면 화면에서 모든 손님이
            // 계속 "기다리는 중" 으로 보인다.
            SetEatSeconds(5f);
            _customerSequence.Reset();
            var customer = Place(NearTable);

            _coordinator.Tick(Interval);

            Assert.IsFalse(_coordinator.IsWaiting(customer));
        }

        [Test]
        public void IsWaiting_RemovedCustomer_ReturnsFalse()
        {
            SetTargetingBand(400, 500);
            _customerSequence.Reset();
            var customer = Place(NearTable);
            _coordinator.Tick(Interval);
            Assert.IsTrue(_coordinator.IsWaiting(customer), "전제: 기다리는 중이다");

            _coordinator.RemoveCustomer(customer);

            Assert.IsFalse(_coordinator.IsWaiting(customer), "배치 취소된 손님이 남아 있다");
        }

        // ── 래치는 이탈 기준이다 (M2.5 D4) ──────────────────────

        [Test]
        public void Tick_LatchShorterThanReachTraversal_KeepsCandidateUntilExit()
        {
            // stage01 과 같은 관계 — 범위 통과(2×12/10 = 2.4초)가 래치(0.5초)보다 길다.
            //
            // 이미 배정된 초밥을 후보로 들고 있는 손님을 만든다. 그 초밥은 Claimed 라
            // 가져갈 수 없어 후보로 남고, 손님은 Idle 이라 자격 정리에도 걸리지 않는다 —
            // 래치 만료를 관측할 수 있는 유일한 구도다.
            Rebuild(latchSeconds: 0.5f);
            SetEatSeconds(5f);
            _customerSequence.Reset();
            Place(NearTable);
            var waiting = Place(NearTable);

            _coordinator.Tick(Interval);
            Assert.AreEqual(1, _coordinator.CandidatesOf(waiting).Count, "전제: 후보를 들고 있다");

            _coordinator.Tick(0.8f);

            Assert.AreEqual(1, _coordinator.CandidatesOf(waiting).Count,
                            "인식 0.8초 뒤 — 래치(0.5)는 넘었지만 아직 범위 안이다");
        }

        [Test]
        public void Tick_PastExitPlusLatch_ExpiresCandidate()
        {
            // 반대편 — 래치가 죽은 코드가 되지 않았음을 확인한다.
            Rebuild(latchSeconds: 0.5f);
            SetEatSeconds(9f);
            _customerSequence.Reset();
            Place(NearTable);
            var waiting = Place(NearTable);

            _coordinator.Tick(Interval);
            _coordinator.Tick(3f);

            // 개수로 세지 않는다 — 3초를 흘리는 동안 새 초밥이 계속 스폰돼 후보가 다시
            // 찬다. 처음 그 초밥(순차번호 0)이 빠졌는지를 본다.
            foreach (var candidate in _coordinator.CandidatesOf(waiting))
            {
                Assert.AreNotEqual(0, candidate.SequenceNumber,
                                   "이탈(≤2.4) + 래치(0.5) 를 지난 후보가 남아 있다");
            }
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
        public void Tick_StillInReachAfterLatch_StillClaimed()
        {
            // **M1 에서 뒤집힌 테스트다.** 원래 이름은 Tick_LatchExpiredBeforeResolve_NotClaimed
            // 이었고, 인식 0.5초 뒤 후보에서 빠지므로 순차번호 1은 영영 배정되지 않는다고
            // 단언했다. M2.5 의 D4 가 그 전제를 없앤다 — 래치는 이제 이탈 기준이라, 범위
            // 안(통과 2.4초)에 있는 초밥은 래치 0.5초를 넘겨도 후보로 남는다.
            //
            // 옛 동작이 곧 "눈앞의 초밥을 두고 구경하는 손님" 이었다. 그건 밸런스가 아니라
            // 플레이 경험 버그로 취급된다 (CLAUDE.md §1.1-3a).
            Rebuild(latchSeconds: 0.5f);
            Place(NearTable);

            _coordinator.Tick(Interval * 2f);
            Assert.AreEqual(1, _claims.Count, "전제: 둘이 인식되고 하나만 배정된다");

            _coordinator.Tick(Interval);

            var claimedSecond = false;
            foreach (var claim in _claims)
            {
                claimedSecond |= claim.SushiSequence == 1;
            }

            Assert.IsTrue(claimedSecond, "범위 안에 남아 있는 초밥은 래치를 넘겨도 집는다");
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

        private void SetTargetingBand(int min, int max) =>
            SerializedFieldSetter.SetTargetingBand(_customerData, min, max);

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
