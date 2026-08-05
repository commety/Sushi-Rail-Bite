using System;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 우선순위의 대역 산술. <b>가격을 읽는 다섯 지점 중 하나다.</b>
    ///
    /// <para>
    /// <b>자격 판정에서는 절대 호출되지 않는다.</b> 대역이 자격에 끼면 손님이 눈앞의 초밥을
    /// 두고 구경하다 굶고, 그건 밸런스 문제가 아니라 버그로 취급한다 (<c>CLAUDE.md</c> §1.1-3a).
    /// 호출자는 <see cref="ClaimPairComparer"/> 와 <c>ClaimDeadline</c> 둘뿐이다.
    /// </para>
    /// <para>
    /// 계산이 몇 줄인데도 별도 타입인 이유는 <b>대역 산술이 사는 곳을 셀 수 있게</b> 만들기
    /// 위해서다. 비교자 안에 인라인하면, 다음 사람이 같은 식을 자격 판정 쪽에 복사해 넣어도
    /// grep 이 잡지 못한다.
    /// </para>
    /// </summary>
    public static class TargetingPriority
    {
        /// <summary>
        /// 가격이 선호 대역에서 얼마나 벗어났는가. <b>대역 안이면 0</b>, 작을수록 우선이다.
        ///
        /// <para>
        /// <c>max(0, min − 가격, 가격 − max)</c>. 대역 밖에서는 <b>가까운 쪽 경계까지의 거리</b>다.
        /// </para>
        /// <para>
        /// <b>"대역 안이 전부 0 이면 정보를 버리는 것 아닌가"</b> — 아니다. 대역 안 동점은
        /// 정렬 키 2(가격 높은 순)가 깬다. 205 와 250 은 여기서 동점이지만 배정에서는 250 이
        /// 이긴다. 이 분업 덕분에 대역이 "선호 구간" 이라는 뜻을 그대로 유지하면서도
        /// 점수 최대화 정보를 잃지 않는다.
        /// </para>
        /// <para>
        /// <b>폭으로 나누지 않는다.</b> 손님마다 대역 폭이 달라 "거리 10" 의 의미가 서로
        /// 다르다는 것이 대역 방식의 고전적 반론인데, 정규화 대신 <b>폭 자체를 이산
        /// 타이브레이커</b>(정렬 키 4, <see cref="BandWidth"/>)로 쓰면 그 문제가 발생하지 않는다.
        /// 여기에 <c>/ 폭</c> 이 등장하면 설계 위반이다
        /// (<c>.claude/domain/sushi-claim-flow.md</c> §6).
        /// </para>
        /// </summary>
        public static int BandDistance(int sushiPrice, int targetingMin, int targetingMax)
        {
            // 셋 중 최대. 대역 안에서는 뒤 둘이 모두 음수라 0 이 남는다.
            return Math.Max(0, Math.Max(targetingMin - sushiPrice, sushiPrice - targetingMax));
        }

        /// <summary>
        /// 선호 대역의 폭. <b>좁을수록 우선</b>이다 — 전문가가 범용가를 이긴다.
        ///
        /// <para>
        /// 정렬 키 4이며, 손님 순차번호(키 5)보다 <b>위</b>다. 없으면 대역이 겹치는 구간에서
        /// <b>배치 순서</b>가 승자를 정해, 소식좌가 자기 전문 분야를 먼저 앉은 범용 손님에게
        /// 빼앗긴다.
        /// </para>
        /// <para>
        /// <see cref="Data.CustomerData"/> 의 프로퍼티로 두지 않은 이유: <c>Runtime.Data</c> 는
        /// 계약이지 계산이 아니고, 대역 산술이 흩어지면 위 grep 방어가 절반만 지킨다.
        /// </para>
        /// </summary>
        public static int BandWidth(int targetingMin, int targetingMax)
        {
            return targetingMax - targetingMin;
        }
    }
}
