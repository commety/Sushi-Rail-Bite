using System;
using SushiDefense.Belt;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님의 <b>자격 판정</b>. "이 손님이 뭐라도 집을 수 있나"만 답한다.
    ///
    /// <para>
    /// 판정 기준은 셋뿐이다 — 범위 ∧ 포화도 여유 ∧ 상태. <b>가격은 들어가지 않는다.</b>
    /// 가격이 자격에 끼면 손님이 눈앞의 초밥을 두고 구경하게 되고, 그건 밸런스 문제가 아니라
    /// 버그로 취급한다 (<c>CLAUDE.md</c> §1.1-3a).
    /// </para>
    /// <para>
    /// 누가 무엇을 가져가는지는 <c>SushiClaimResolver</c> 의 몫이다. 둘을 한 클래스에 두면
    /// M2 에서 타겟팅 검사가 자격 쪽으로 새어 들어온다 — 이 파일이 가격 타입을 아예 참조하지
    /// 않게 두는 것이 그 방어다 (작업서 D6).
    /// </para>
    /// </summary>
    public sealed class CustomerLogic
    {
        /// <summary>이 손님의 런타임 상태. 전이는 조율자가, 타이머는 M2 가 다룬다.</summary>
        public CustomerRuntimeState State { get; }

        /// <summary>앉은 자리의 1차원 벨트 좌표. 집기 범위의 중심이다.</summary>
        public float BeltPosition { get; }

        public CustomerLogic(CustomerRuntimeState state, float beltPosition)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            BeltPosition = beltPosition;
        }

        /// <summary>
        /// 지금 새 초밥을 받을 수 있는 상태인가. 초밥과 무관한 손님 쪽 조건만 본다.
        /// </summary>
        public bool CanAcceptSushi =>
            State.State == CustomerState.Idle && State.HasSaturationHeadroom;

        /// <summary>
        /// 초밥이 집기 범위 안에 있는가. <c>[BeltPosition − Reach, BeltPosition + Reach]</c>
        /// <b>폐구간</b>이며, 앞뒤 대칭이다.
        /// </summary>
        public bool IsInReach(float sushiBeltPosition)
        {
            var reach = State.Data.Reach;
            var distance = sushiBeltPosition - BeltPosition;
            return distance >= -reach && distance <= reach;
        }

        /// <summary>
        /// 이 초밥을 집을 자격이 있는가. <b>가격을 보지 않는다.</b>
        /// </summary>
        public bool CanTake(SushiItem sushi)
        {
            return sushi != null
                   && sushi.State == SushiState.OnBelt
                   && CanAcceptSushi
                   && IsInReach(sushi.BeltPosition);
        }
    }
}
