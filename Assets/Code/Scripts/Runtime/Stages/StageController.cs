using System;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;

namespace SushiDefense.Stages
{
    /// <summary>
    /// 스테이지 한 판의 진행을 굴린다 — 시간을 흘리고, 결과가 나면 <b>멈춘다</b>.
    ///
    /// <para>
    /// <b>배정 규칙을 모른다.</b> 조율자에게 시간을 넘겨줄 뿐이며, 누가 무엇을 먹는지는
    /// <see cref="ClaimCoordinator"/> 의 몫이다.
    /// </para>
    /// <para>
    /// <b>조율자를 소유하지 않는다.</b> 여기서 <c>Dispose</c> 하지 않는다 — 조율자의 수명은
    /// 씬 진입점이 쥐고 있고, 소유권이 둘이 되면 어느 쪽이 먼저 버렸는지에 따라 동작이
    /// 달라진다.
    /// </para>
    /// </summary>
    public sealed class StageController
    {
        private readonly ClaimCoordinator _coordinator;
        private readonly RevenueLedger _revenue;
        private readonly StageConfig _config;
        private readonly StageClock _clock;

        /// <summary>지금까지의 판정. 아직 안 끝났으면 <see cref="StageOutcome.InProgress"/>.</summary>
        public StageOutcome Outcome { get; private set; } = StageOutcome.InProgress;

        /// <summary>남은 시간(초). 0 아래로 내려가지 않는다.</summary>
        public float RemainingSeconds => _clock.RemainingSeconds;

        /// <summary>
        /// 이 판의 목표 매출. 진행도를 그리는 쪽이 <c>StageConfig</c> 를 따로 들지 않도록
        /// 여기서 내보낸다 — 판정하는 쪽에 물어보는 것이 맞다.
        /// </summary>
        public int TargetRevenue => _config.TargetRevenue;

        /// <summary>아직 진행 중인가.</summary>
        public bool IsRunning => Outcome == StageOutcome.InProgress;

        /// <summary>
        /// 결과가 확정됐다. <b>판당 정확히 한 번</b> 발생한다 — <see cref="Tick"/> 의 조기
        /// 반환이 그것을 보장하므로 별도의 발행 플래그를 두지 않는다. 두 장치가 같은 것을
        /// 지키면 나중에 한쪽만 고쳐진다.
        /// </summary>
        public event Action<StageOutcome> OutcomeDecided;

        public StageController(ClaimCoordinator coordinator, RevenueLedger revenue, StageConfig config)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _revenue = revenue ?? throw new ArgumentNullException(nameof(revenue));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _clock = new StageClock(_config.TimeLimitSeconds);
        }

        /// <summary>
        /// 시간을 흘린다. <b>판정이 조율자 틱보다 뒤에 온다는 것이 계약이다.</b>
        ///
        /// <para>
        /// 매출 원장을 구독하지 않고 틱마다 읽는다. 구독하면 해제 경로가 늘고, 매출이 바뀌지
        /// 않은 프레임에 만료로 실패하는 경우를 따로 처리해야 한다 — <c>int</c> 하나를 읽는
        /// 편이 싸고 정확하다.
        /// </para>
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            // 음수는 조율자에 닿기 **전에** 막는다. 시계가 어차피 던지지만, 그때는 이미
            // 벨트와 조율자의 경과 시간이 되감긴 뒤라 판이 오염된 상태로 예외가 난다.
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds,
                                                      "시간은 되감을 수 없습니다.");
            }

            // 0) 이미 끝났으면 아무것도 하지 않는다. 결과가 난 뒤에도 벨트가 흐르면
            //    보상 화면 뒤에서 매출이 계속 오른다.
            if (!IsRunning)
            {
                return;
            }

            // 1) 조율자를 굴린다.
            _coordinator.Tick(deltaSeconds);

            // 2) 시간을 흘린다.
            //
            //    1 과 2 의 **상대 순서는 관측되지 않는다.** 조율자는 시계를 읽지 않고 시계는
            //    매출을 읽지 않으므로, 판정이 3 에서 두 최종값을 읽는 한 어느 쪽을 먼저
            //    굴려도 같은 결과가 나온다.
            _clock.Advance(deltaSeconds);

            // 3) 판정. **반드시 1 뒤에 온다** — 이것이 실제 계약이다. 조율자보다 앞서 판정하면
            //    이번 프레임에 입에 들어간 초밥의 매출이 빠진 채로 만료가 걸려
            //    "먹었는데 실패" 가 난다. InProgress 가 아니게 된 그 틱에만 알린다.
            var outcome = StageEvaluator.Evaluate(_revenue.Total, _config.TargetRevenue, _clock.IsExpired);
            if (outcome == StageOutcome.InProgress)
            {
                return;
            }

            Outcome = outcome;
            OutcomeDecided?.Invoke(outcome);
        }
    }
}
