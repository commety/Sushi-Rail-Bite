namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 상태를 몸통 동작으로 옮긴다. <b>판정하지 않는다</b> — 상태는 식욕 상태 머신이
    /// 이미 정했고, 여기서는 «집기» 라는 짧은 구간을 그 위에 얹기만 한다.
    ///
    /// <para>
    /// <c>MonoBehaviour</c> 밖에 있는 이유: 집기가 먹기를 얼마나 가리는지는 프레임 없이
    /// 검증할 수 있는 순수 계산이다 (<c>CLAUDE.md</c> §3.2). <c>Animator</c> 안에 넣으면
    /// 전이 조건이 애셋으로 들어가 <b>애니메이션 창을 열기 전에는 확인할 수 없다.</b>
    /// </para>
    /// </summary>
    public sealed class CustomerMotionSelector
    {
        private float _pickingRemaining;

        /// <summary>지금 나가야 할 동작. <see cref="Advance"/> 가 갱신한다.</summary>
        public CustomerMotion Current { get; private set; } = CustomerMotion.None;

        /// <summary>
        /// 초밥을 집었다. <paramref name="pickingSeconds"/> 동안 집기 동작이 먹기를 가린다.
        ///
        /// <para>
        /// <b>먹기를 대체하지 않고 앞에 붙는다.</b> 집기는 배정이 확정된 순간의 한 동작이고,
        /// 그 뒤 남은 시간은 먹는 모습이어야 한다 — 집기로 먹는 시간을 통째로 채우면
        /// 먹보와 소식가가 화면에서 구분되지 않는다.
        /// </para>
        /// </summary>
        public void NotifyPicked(float pickingSeconds)
        {
            _pickingRemaining = pickingSeconds > 0f ? pickingSeconds : 0f;
        }

        /// <summary>손님이 바뀌었다. 앞 손님의 집기가 새 손님에게 이어지지 않게 비운다.</summary>
        public void Reset()
        {
            _pickingRemaining = 0f;
            Current = CustomerMotion.None;
        }

        /// <summary>
        /// 시간을 흘리고 지금 나갈 동작을 정한다.
        ///
        /// <para>
        /// <b>집기는 먹는 중일 때만 살아 있다.</b> 먹다 만 초밥이 끝점에서 반납되면
        /// (<c>ClaimCoordinator.OnSushiRemoved</c>) 손님은 <c>Idle</c> 로 돌아가는데, 그때
        /// 집기가 남아 있으면 <b>아무것도 안 든 손님이 계속 집는 시늉을 한다.</b>
        /// </para>
        /// </summary>
        public CustomerMotion Advance(CustomerState state, float deltaSeconds)
        {
            if (state != CustomerState.Eating)
            {
                _pickingRemaining = 0f;
            }
            else if (_pickingRemaining > 0f)
            {
                _pickingRemaining -= deltaSeconds;
            }

            Current = _pickingRemaining > 0f
                ? CustomerMotion.Picking
                : state switch
                {
                    CustomerState.Eating => CustomerMotion.Eating,
                    CustomerState.Digesting => CustomerMotion.Digesting,
                    _ => CustomerMotion.None
                };

            return Current;
        }
    }
}
