using System;

namespace SushiDefense.Run
{
    /// <summary>
    /// xorshift32. 외부 의존이 없고 시드가 같으면 수열이 같다.
    ///
    /// <para>
    /// BCL·Unity 의 난수를 쓰지 않는 이유는 성능이 아니라 <b>재현성</b>이다 — 구현이
    /// 런타임 버전에 따라 달라질 수 있고, 시드를 우리가 들고 있지 않으면 같은 런을
    /// 다시 돌려볼 수 없다.
    /// </para>
    /// </summary>
    public sealed class XorShiftRandomSource : IRandomSource
    {
        /// <summary>
        /// 시드 0 을 대신할 값. xorshift 에서 0 은 <b>고정점</b>이라 방치하면 영원히 0 만
        /// 나온다. 값 자체는 임의이며(황금비 상수), 0 이 아니기만 하면 된다.
        /// </summary>
        private const uint ZeroSeedReplacement = 0x9E3779B9u;

        private uint _state;

        public XorShiftRandomSource(int seed)
        {
            _state = seed != 0 ? (uint)seed : ZeroSeedReplacement;
        }

        /// <summary>
        /// <c>[0, exclusiveMax)</c> 범위의 정수.
        ///
        /// <para>
        /// 모듈로 편향이 있지만 <b>고치지 않는다.</b> 이 난수원이 쓰이는 곳은 보상 후보
        /// 추첨이고 후보는 많아야 수십 개라 편향의 크기가 무시할 수 있다. 반면 편향 제거
        /// 루프를 넣으면 뽑기 하나에 소비되는 난수 개수가 값에 따라 달라져, 같은 시드로도
        /// 이후 수열이 어긋날 수 있는 경로가 생긴다 — 재현성이 이 클래스의 존재 이유다.
        /// </para>
        /// </summary>
        public int Next(int exclusiveMax)
        {
            if (exclusiveMax < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax), exclusiveMax,
                                                      "뽑기 범위는 1 이상이어야 합니다.");
            }

            return (int)(NextState() % (uint)exclusiveMax);
        }

        private uint NextState()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
