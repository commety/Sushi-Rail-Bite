using System;
using System.Collections.Generic;
using SushiDefense.Belt;
using SushiDefense.Data;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 벨트·자격·후보·배정을 잇는 순수 오케스트레이터.
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
    /// <b>M1 에는 먹는 시간이 없다.</b> 배정이 확정되면 그 자리에서 소비로 처리하고 초밥을
    /// 벨트에서 내린다. <c>Eating</c> 상태 체류와 타이머는 M2 다.
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

        private readonly SushiBelt _belt;
        private readonly StageConfig _config;
        private readonly SushiClaimResolver _resolver = new();

        private readonly List<CustomerLogic> _customers = new();
        private readonly Dictionary<CustomerLogic, CandidateSet> _candidates = new();
        private readonly Dictionary<CustomerLogic, bool> _wasEligible = new();
        private readonly List<PendingEntry> _pending = new();
        private readonly List<ClaimCandidatePair> _claims = new();

        private float _elapsedSeconds;

        /// <summary>배치된 손님. 배치 순서를 유지한다.</summary>
        public IReadOnlyList<CustomerLogic> Customers => _customers;

        /// <summary>배정이 확정됐다. 뷰가 먹는 연출을 시작할 지점이다.</summary>
        public event Action<CustomerLogic, SushiItem> SushiClaimed;

        public ClaimCoordinator(SushiBelt belt, StageConfig config)
        {
            _belt = belt ?? throw new ArgumentNullException(nameof(belt));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));

            _belt.SushiSpawned += OnSushiSpawned;
            _belt.SushiRemoved += OnSushiRemoved;
        }

        /// <summary>구독을 끊는다. 조율자를 버릴 때 부르지 않으면 벨트가 죽은 참조를 붙든다.</summary>
        public void Dispose()
        {
            _belt.SushiSpawned -= OnSushiSpawned;
            _belt.SushiRemoved -= OnSushiRemoved;
        }

        /// <summary>이 손님이 인식 중인 초밥. 진단·검증용이다.</summary>
        public IReadOnlyList<SushiItem> CandidatesOf(CustomerLogic customer)
        {
            return _candidates.TryGetValue(customer, out var set)
                ? set.Items
                : Array.Empty<SushiItem>();
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
            ScheduleAgainstActiveSushi(customer);
        }

        /// <summary>배치를 취소한다. 후보와 예약을 비우고 배정 대상에서 뺀다.</summary>
        public void RemoveCustomer(CustomerLogic customer)
        {
            if (!_candidates.TryGetValue(customer, out var set))
            {
                return;
            }

            set.Clear();
            _candidates.Remove(customer);
            _wasEligible.Remove(customer);
            _customers.Remove(customer);
            PurgePendingFor(customer);
        }

        /// <summary>
        /// 시간을 흘린다. 벨트 → 진입 인식 → 래치 만료 → 자격 정리 → 배정 순으로 돈다.
        /// 이 순서가 계약이다 (작업서 step-06 참고).
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;

            // 1) 벨트를 굴린다. 스폰은 진입 예약을 만들고, 끝점 반납은 후보·예약을 지운다
            //    (OnSushiSpawned / OnSushiRemoved). 제거가 인식보다 먼저 끝나야
            //    이미 내려간 초밥이 같은 틱에 인식되는 유령 배정이 생기지 않는다.
            _belt.Tick(deltaSeconds);

            // 2) 기한이 된 진입 예약을 인식으로 바꾼다.
            RecognizeDueEntries();

            // 3) 래치 만료. 배정보다 먼저 와야 상한이 의미를 갖는다.
            ExpireCandidates();

            // 4) 자격을 잃은 손님의 후보를 비우고, 되찾은 손님은 다시 예약한다.
            SyncEligibility();

            // 5) 배정 확정 → 소비 처리.
            ResolveClaims();
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
                SushiClaimed?.Invoke(claim.Customer, claim.Sushi);

                // M1 은 먹는 시간이 없다 — 확정이 곧 소비다. 벨트에서 내리면
                // OnSushiRemoved 가 모든 손님의 후보에서 지워 준다.
                _belt.Remove(claim.Sushi);
            }
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
                _candidates[_customers[i]].Forget(sushi);
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
