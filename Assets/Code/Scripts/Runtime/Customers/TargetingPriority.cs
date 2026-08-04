using System;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 우선순위의 1순위 키. <b>가격을 읽는 세 지점 중 하나다.</b>
    ///
    /// <para>
    /// <b>자격 판정에서는 절대 호출되지 않는다.</b> 가격이 자격에 끼면 손님이 눈앞의 초밥을
    /// 두고 구경하게 되고, 그건 밸런스 문제가 아니라 버그로 취급한다 (<c>CLAUDE.md</c> §1.1-3a).
    /// 호출자는 <see cref="ClaimPairComparer"/> 하나뿐이다.
    /// </para>
    /// <para>
    /// 계산이 세 줄인데도 별도 타입인 이유는 <b>가격을 읽는 지점을 셀 수 있게</b> 만들기
    /// 위해서다. 비교자 안에 산술을 인라인하면, 다음 사람이 같은 식을 자격 판정 쪽에 복사해
    /// 넣어도 grep 이 잡지 못한다.
    /// </para>
    /// </summary>
    public static class TargetingPriority
    {
        /// <summary>
        /// 가격과 타겟팅 사이의 거리. <b>작을수록 우선</b>이다.
        ///
        /// <para>
        /// 도메인 문서의 표기는 <c>−|가격 − 타겟팅|</c>(클수록 우선)이지만, 부호를 뒤집어
        /// 오름차순으로 다루는 편이 비교자에서 헷갈릴 자리가 없다. 두 표현은 정렬 결과가 같다.
        /// </para>
        /// <para>
        /// <b>거리는 대칭이다 — 의도된 것이다.</b> 타겟팅 200 인 손님은 199 짜리를 250 짜리보다
        /// 먼저 집는다. 점수만 보면 손해지만, 모든 손님이 무조건 비싼 걸 먹으면 타겟팅도 배치도
        /// 의미가 없어진다. 손님이 탐욕적이지 않은 선호를 갖기 때문에 플레이어의 배열이 전략이 된다.
        /// </para>
        /// <para>
        /// 타겟팅이 <b>단일 값</b>이라 정규화가 필요 없다 — 모든 손님의 거리가 같은 단위(엔)다.
        /// 대역(<c>min~max</c>)이었다면 손님마다 폭이 달라 "거리 10" 의 의미가 서로 달라진다
        /// (<c>.claude/domain/sushi-claim-flow.md</c> §6).
        /// </para>
        /// </summary>
        public static int Distance(int sushiPrice, int targetingPrice)
        {
            return Math.Abs(sushiPrice - targetingPrice);
        }
    }
}
