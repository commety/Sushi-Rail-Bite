using System;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 1명의 상태 전이와 타이머. <c>Idle → Eating → (포화) Digesting → Idle</c>.
    ///
    /// <para>
    /// <b>초밥을 받지 않는다.</b> <see cref="BeginEating"/> 이 포화도 기여량만 <c>int</c> 로
    /// 받으므로 가격을 볼 수 있는 경로가 타입 수준에서 없다. 이게 이 클래스를
    /// <see cref="CustomerLogic"/> 과 분리한 이유다 — 먹는 로직을 자격 판정 옆에 두면
    /// 초밥의 정적 데이터를 만지기 시작하고, "타겟팅이 자격 게이트로 새는" 이 마일스톤
    /// 최대 위험이 열린다 (<c>CLAUDE.md</c> §1.1-3a).
    /// </para>
    /// <para>
    /// 전이 조건이 <c>MonoBehaviour</c> 밖 순수 클래스에 있어야 한다는 요구
    /// (<c>CLAUDE.md</c> §3.5)를 만족한다. 시간도 스스로 읽지 않고 <see cref="Tick"/> 인자로
    /// 받으므로 "2.5초가 흘렀을 때" 를 프레임 대기 없이 검증할 수 있다.
    /// </para>
    /// </summary>
    public sealed class CustomerAppetiteMachine
    {
        private int _pendingSaturation;

        /// <summary>이 손님의 런타임 상태. 전이 결과가 여기에 쓰인다.</summary>
        public CustomerRuntimeState State { get; }

        /// <summary>
        /// 먹기가 끝났다 — <b>소비 확정 시점</b>이다. 조율자가 매출·재화 누적과 벨트 제거를
        /// 여기서 처리한다.
        ///
        /// <para>
        /// 발행 시점에는 포화도 증가와 상태 전이가 <b>이미 끝나 있다</b>. 구독자가 중간
        /// 상태를 보지 않게 하기 위해서다.
        /// </para>
        /// </summary>
        public event Action EatingFinished;

        public CustomerAppetiteMachine(CustomerRuntimeState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// 먹기 시작한다. <see cref="CustomerState.Idle"/> 이 아니면 <c>false</c> 를 돌려주고
        /// 아무것도 바꾸지 않는다 — <b>한 번에 하나</b>이고 예약 큐를 두지 않는다.
        ///
        /// <para>
        /// 포화도 여유를 따로 검사하지 않는다. 포화에 닿는 순간 <see cref="CustomerState.Digesting"/>
        /// 으로 넘어가므로 <c>Idle</c> 이면 여유가 있다는 것이 이 클래스가 유지하는 불변식이다.
        /// </para>
        /// </summary>
        public bool BeginEating(int saturationAmount)
        {
            if (State.State != CustomerState.Idle)
            {
                return false;
            }

            _pendingSaturation = saturationAmount;
            State.State = CustomerState.Eating;
            State.RemainingEatSeconds = State.Data.EatSeconds;
            return true;
        }

        /// <summary>
        /// 시간을 흘린다. 한 틱에 전이는 최대 하나다 — 먹기 완료와 소화 완료가 같은 틱에
        /// 겹치지 않아서 상태 변화를 쫓기 쉽다.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            switch (State.State)
            {
                case CustomerState.Eating:
                    TickEating(deltaSeconds);
                    break;

                case CustomerState.Digesting:
                    TickDigesting(deltaSeconds);
                    break;
            }
        }

        private void TickEating(float deltaSeconds)
        {
            State.RemainingEatSeconds -= deltaSeconds;
            if (State.RemainingEatSeconds > 0f)
            {
                return;
            }

            FinishEating();
        }

        /// <summary>
        /// 순서가 계약이다 — <b>먹기 완료 → 포화도 증가 → 포화 판정</b>. 뒤집으면 마지막
        /// 한 입이 포화 판정에 반영되지 않아 손님이 최대치를 넘겨 먹는다.
        /// </summary>
        private void FinishEating()
        {
            State.RemainingEatSeconds = 0f;

            var filled = State.CurrentSaturation + _pendingSaturation;
            State.CurrentSaturation = filled > State.Data.MaxSaturation
                ? State.Data.MaxSaturation
                : filled;
            _pendingSaturation = 0;

            if (State.HasSaturationHeadroom)
            {
                State.State = CustomerState.Idle;
            }
            else
            {
                State.State = CustomerState.Digesting;
                State.RemainingDigestSeconds = State.Data.DigestSeconds;
            }

            EatingFinished?.Invoke();
        }

        private void TickDigesting(float deltaSeconds)
        {
            State.RemainingDigestSeconds -= deltaSeconds;
            if (State.RemainingDigestSeconds > 0f)
            {
                return;
            }

            State.RemainingDigestSeconds = 0f;
            State.CurrentSaturation = 0;
            State.State = CustomerState.Idle;
        }
    }
}
