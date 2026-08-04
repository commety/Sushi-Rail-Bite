using System;
using System.Collections.Generic;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Scoring;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 벨트·자격·후보·배정·식욕·경제를 잇는 순수 오케스트레이터.
    ///
    /// <para>
    /// 재배정이 필요한 순간을 한곳에 모은다 — 진입 / 소비 / 벨트 제거 / 자격 회복 /
    /// 자격 상실 / 손님 배치 (<c>.claude/domain/sushi-claim-flow.md</c> §4). 흩어 두면
    /// 트리거가 늘 때마다 빠뜨리는 곳이 생긴다.
    /// </para>
    /// <para>
    /// <b>매 프레임 손님 × 초밥을 훑지 않는다.</b> 진입 시각은 초밥이 스폰될 때(또는 손님이
    /// 배치될 때) 한 번 계산해 예약해 두고, 틱마다 기한이 된 예약만 꺼낸다.
    /// </para>
    /// <para>
    /// <b>배정 확정과 소비 사이에 먹는 시간이 있다</b> (M1 과 달라진 지점). 그동안 초밥은
    /// <c>Claimed</c> 상태로 벨트에 남는다 — 다른 손님은 가져갈 수 없고(<c>TryClaim</c> 이
    /// 막는다), 끝점에 닿으면 그대로 반납된다. 즉 <b>먹다 만 초밥이 끝점을 지나면 놓친다.</b>
    /// 의도된 동작이다.
    /// </para>
    /// </summary>
    public sealed class ClaimCoordinator
    {
        /// <summary>범위 진입 예약. 스폰·배치 시점에 계산해 넣고 기한이 되면 인식으로 바뀐다.</summary>
        private readonly struct PendingEntry
        {
            public CustomerLogic Customer { get; }
            public SushiItem Sushi { get; }
            public float DueSeconds { get; }

            public PendingEntry(CustomerLogic customer, SushiItem sushi, float dueSeconds)
            {
                Customer = customer;
                Sushi = sushi;
                DueSeconds = dueSeconds;
            }
        }

        /// <summary>
        /// 손님 1명의 식욕 상태 머신과 그에 딸린 것들.
        ///
        /// <para>
        /// 구독 해제용 델리게이트를 함께 들고 있어야 한다 — 지역 함수·람다는 매번 새
        /// 인스턴스라 보관하지 않으면 <c>-=</c> 로 떼어낼 수 없다
        /// (<c>.claude/rules/scripts.md</c> §6).
        /// </para>
        /// </summary>
        private sealed class Appetite
        {
            public CustomerAppetiteMachine Machine { get; }
            public Action Handler { get; }

            /// <summary>지금 먹고 있는 초밥. 먹는 중이 아니면 <c>null</c>.</summary>
            public SushiItem Eating { get; set; }

            public Appetite(CustomerAppetiteMachine machine, Action handler)
            {
                Machine = machine;
                Handler = handler;
            }
        }

        private readonly SushiBelt _belt;
        private readonly StageConfig _config;
        private readonly RevenueLedger _revenue;
        private readonly RecruitWallet _wallet;
        private readonly SushiClaimResolver _resolver = new();

        private readonly List<CustomerLogic> _customers = new();
        private readonly Dictionary<CustomerLogic, CandidateSet> _candidates = new();
        private readonly Dictionary<CustomerLogic, bool> _wasEligible = new();
        private readonly Dictionary<CustomerLogic, Appetite> _appetites = new();
        private readonly List<PendingEntry> _pending = new();
        private readonly List<ClaimCandidatePair> _claims = new();

        /// <summary>이번 틱에 먹기를 마친 손님. 필드로 잡아 틱마다 재사용한다.</summary>
        private readonly List<CustomerLogic> _finishedEating = new();

        private float _elapsedSeconds;

        /// <summary>배치된 손님. 배치 순서를 유지한다.</summary>
        public IReadOnlyList<CustomerLogic> Customers => _customers;

        /// <summary>배정이 확정됐다 — <b>먹기 시작</b> 시점이다. 뷰가 먹는 연출을 켠다.</summary>
        public event Action<CustomerLogic, SushiItem> SushiClaimed;

        /// <summary>
        /// 소비가 끝났다. 발행 시점에는 <b>매출·영입 재화가 이미 반영</b>돼 있고 초밥은
        /// 벨트에서 내려가 있다.
        /// </summary>
        public event Action<CustomerLogic, SushiItem> SushiEaten;

        public ClaimCoordinator(SushiBelt belt, StageConfig config,
                                RevenueLedger revenue, RecruitWallet wallet)
        {
            _belt = belt ?? throw new ArgumentNullException(nameof(belt));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _revenue = revenue ?? throw new ArgumentNullException(nameof(revenue));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

            _belt.SushiSpawned += OnSushiSpawned;
            _belt.SushiRemoved += OnSushiRemoved;
        }

        /// <summary>
        /// 구독을 끊는다. 벨트뿐 아니라 <b>손님마다 붙은 식욕 머신 구독까지</b> 전부 뗀다 —
        /// 조율자를 버릴 때 부르지 않으면 죽은 참조가 남는다.
        /// </summary>
        public void Dispose()
        {
            _belt.SushiSpawned -= OnSushiSpawned;
            _belt.SushiRemoved -= OnSushiRemoved;

            foreach (var appetite in _appetites.Values)
            {
                appetite.Machine.EatingFinished -= appetite.Handler;
            }

            _appetites.Clear();
        }

        /// <summary>이 손님이 인식 중인 초밥. 진단·검증용이다.</summary>
        public IReadOnlyList<SushiItem> CandidatesOf(CustomerLogic customer)
        {
            return _candidates.TryGetValue(customer, out var set)
                ? set.Items
                : Array.Empty<SushiItem>();
        }

        /// <summary>이 손님이 지금 먹고 있는 초밥. 먹는 중이 아니면 <c>null</c>. 진단용이다.</summary>
        public SushiItem EatingOf(CustomerLogic customer)
        {
            return _appetites.TryGetValue(customer, out var appetite) ? appetite.Eating : null;
        }

        /// <summary>
        /// 손님을 배치한다. <b>스테이지 진행 중에도 부를 수 있다</b> — 배치 즉시 벨트 위
        /// 초밥에 대한 진입 예약이 잡히고, 다음 배정부터 참여한다.
        /// </summary>
        public void PlaceCustomer(CustomerLogic customer)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (_candidates.ContainsKey(customer))
            {
                throw new ArgumentException("이미 배치된 손님입니다.", nameof(customer));
            }

            _customers.Add(customer);
            _candidates[customer] = new CandidateSet(_config.RecognitionLatchSeconds);
            _wasEligible[customer] = customer.CanAcceptSushi;

            var machine = new CustomerAppetiteMachine(customer.State);
            Action handler = () => _finishedEating.Add(customer);
            machine.EatingFinished += handler;
            _appetites[customer] = new Appetite(machine, handler);

            ScheduleAgainstActiveSushi(customer);
        }

        /// <summary>
        /// 배치를 취소한다. 후보와 예약을 비우고 배정 대상에서 뺀다.
        ///
        /// <para>
        /// 먹는 중이었다면 <b>그 초밥은 벨트에서 내려간다.</b> 매출은 붙지 않는다 — 소비가
        /// 끝나지 않았기 때문이다. 그냥 두면 아무도 먹지 않는 <c>Claimed</c> 초밥이 끝점까지
        /// 흘러가며 자리만 차지한다.
        /// </para>
        /// </summary>
        public void RemoveCustomer(CustomerLogic customer)
        {
            if (!_candidates.TryGetValue(customer, out var set))
            {
                return;
            }

            if (_appetites.TryGetValue(customer, out var appetite))
            {
                appetite.Machine.EatingFinished -= appetite.Handler;
                _appetites.Remove(customer);

                if (appetite.Eating != null)
                {
                    var abandoned = appetite.Eating;
                    appetite.Eating = null;
                    _belt.Remove(abandoned);
                }
            }

            set.Clear();
            _candidates.Remove(customer);
            _wasEligible.Remove(customer);
            _customers.Remove(customer);
            _finishedEating.Remove(customer);
            PurgePendingFor(customer);
        }

        /// <summary>
        /// 시간을 흘린다. <b>이 순서가 계약이다.</b>
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;

            // 1) 벨트를 굴린다. 스폰은 진입 예약을 만들고, 끝점 반납은 후보·예약을 지운다.
            //    제거가 인식보다 먼저 끝나야 이미 내려간 초밥이 같은 틱에 인식되는
            //    유령 배정이 생기지 않는다.
            _belt.Tick(deltaSeconds);

            // 2) 먹기·소화를 진행하고, 끝난 것을 그 자리에서 소비 처리한다.
            //    이게 5·6 보다 앞이어야 먹기를 마친 손님이 **같은 틱에** 자격을 되찾고
            //    새 배정에 참여한다. 뒤에 두면 손님이 매번 한 틱씩 굶는다.
            TickAppetites(deltaSeconds);

            // 3) 기한이 된 진입 예약을 인식으로 바꾼다.
            RecognizeDueEntries();

            // 4) 래치 만료. 배정보다 먼저 와야 상한이 의미를 갖는다.
            ExpireCandidates();

            // 5) 자격을 잃은 손님의 후보를 비우고, 되찾은 손님은 다시 예약한다.
            SyncEligibility();

            // 6) 배정 확정 → 먹기 시작.
            ResolveClaims();
        }

        private void TickAppetites(float deltaSeconds)
        {
            _finishedEating.Clear();

            for (var i = 0; i < _customers.Count; i++)
            {
                _appetites[_customers[i]].Machine.Tick(deltaSeconds);
            }

            ConsumeFinished();
        }

        /// <summary>
        /// 소비를 확정한다. 순서는 <b>상태 전이 → 매출 → 재화 → 벨트 제거 → 알림</b>.
        ///
        /// <para>
        /// 식욕 머신의 <c>Tick</c> 루프 <b>바깥</b>에서 돈다. 벨트에서 초밥을 내리면
        /// <see cref="CandidateSet"/> 과 예약 목록이 함께 흔들리는데, 그걸 순회 도중
        /// 재진입으로 건드리지 않기 위해서다.
        /// </para>
        /// </summary>
        private void ConsumeFinished()
        {
            for (var i = 0; i < _finishedEating.Count; i++)
            {
                var customer = _finishedEating[i];
                if (!_appetites.TryGetValue(customer, out var appetite))
                {
                    continue;
                }

                var sushi = appetite.Eating;
                appetite.Eating = null;
                if (sushi == null || !sushi.TryConsume())
                {
                    continue;
                }

                var price = sushi.Data.Price;
                _revenue.Add(price);
                _wallet.AccrueFrom(price);

                _belt.Remove(sushi);
                SushiEaten?.Invoke(customer, sushi);
            }

            _finishedEating.Clear();
        }

        private void RecognizeDueEntries()
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var entry = _pending[i];
                if (entry.DueSeconds > _elapsedSeconds)
                {
                    continue;
                }

                _pending.RemoveAt(i);

                if (_candidates.TryGetValue(entry.Customer, out var set))
                {
                    set.Recognize(entry.Sushi, _elapsedSeconds);
                }
            }
        }

        private void ExpireCandidates()
        {
            for (var i = 0; i < _customers.Count; i++)
            {
                _candidates[_customers[i]].ExpireOlderThan(_elapsedSeconds);
            }
        }

        private void SyncEligibility()
        {
            for (var i = 0; i < _customers.Count; i++)
            {
                var customer = _customers[i];
                var eligible = customer.CanAcceptSushi;

                if (!eligible)
                {
                    _candidates[customer].Clear();
                }
                else if (!_wasEligible[customer])
                {
                    // 자격을 잃은 동안 후보와 예약이 비워졌으므로, 되찾는 시점에
                    // 벨트 위 초밥을 다시 예약해 준다. 안 하면 이미 범위 안에 있는
                    // 초밥을 영영 못 본다.
                    ScheduleAgainstActiveSushi(customer);
                }

                _wasEligible[customer] = eligible;
            }
        }

        private void ResolveClaims()
        {
            _resolver.Resolve(_customers, _candidates, _claims);

            for (var i = 0; i < _claims.Count; i++)
            {
                var claim = _claims[i];
                BeginEating(claim.Customer, claim.Sushi);
                SushiClaimed?.Invoke(claim.Customer, claim.Sushi);
            }
        }

        /// <summary>
        /// 먹기를 시작시킨다. 초밥은 <c>Claimed</c> 상태로 벨트에 남는다.
        ///
        /// <para>
        /// 리졸버가 자격 있는(=<c>Idle</c>) 손님에게만 배정하므로 실패할 수 없다. 그럼에도
        /// 실패했다면 초밥이 <c>Claimed</c> 인데 먹을 사람이 없는 상태라 조용히 넘기면 안 된다 —
        /// 불변식이 깨진 것이므로 소리 나게 터뜨린다.
        /// </para>
        /// </summary>
        private void BeginEating(CustomerLogic customer, SushiItem sushi)
        {
            var appetite = _appetites[customer];
            if (!appetite.Machine.BeginEating(sushi.Data.SaturationAmount))
            {
                throw new InvalidOperationException(
                    $"손님 {customer.State.SequenceNumber} 이 배정을 받고도 먹기 시작하지 못했습니다 " +
                    $"(상태 {customer.State.State}). 자격 판정과 배정이 어긋났습니다.");
            }

            appetite.Eating = sushi;
        }

        private void OnSushiSpawned(SushiItem sushi)
        {
            for (var i = 0; i < _customers.Count; i++)
            {
                Schedule(_customers[i], sushi);
            }
        }

        private void OnSushiRemoved(SushiItem sushi)
        {
            for (var i = 0; i < _customers.Count; i++)
            {
                var customer = _customers[i];
                _candidates[customer].Forget(sushi);

                // 먹다 만 초밥이 끝점에 닿아 반납된 경우다. 손님을 Idle 로 되돌려
                // 다음 배정에 참여시킨다 — 아니면 영영 Eating 에 갇힌다.
                if (!_appetites.TryGetValue(customer, out var appetite)
                    || !ReferenceEquals(appetite.Eating, sushi))
                {
                    continue;
                }

                appetite.Eating = null;
                appetite.Machine.AbortEating();
            }

            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_pending[i].Sushi, sushi))
                {
                    _pending.RemoveAt(i);
                }
            }
        }

        private void ScheduleAgainstActiveSushi(CustomerLogic customer)
        {
            var active = _belt.ActiveSushi;
            for (var i = 0; i < active.Count; i++)
            {
                Schedule(customer, active[i]);
            }
        }

        /// <summary>
        /// 진입 시각을 계산해 예약한다. 매 프레임 위치를 비교하는 대신 스폰·배치 시점에
        /// 한 번만 푼다 (작업서 D5).
        /// </summary>
        private void Schedule(CustomerLogic customer, SushiItem sushi)
        {
            var reach = customer.State.Data.Reach;
            var window = ReachWindow.Solve(sushi.BeltPosition, _config.BeltSpeed,
                                           customer.BeltPosition - reach,
                                           customer.BeltPosition + reach);

            if (!window.WillEverEnter)
            {
                return;
            }

            _pending.Add(new PendingEntry(customer, sushi, _elapsedSeconds + window.EnterSeconds));
        }

        private void PurgePendingFor(CustomerLogic customer)
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Customer == customer)
                {
                    _pending.RemoveAt(i);
                }
            }
        }
    }
}
