using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Belt
{
    /// <summary>
    /// share 를 배출 순서로 바꾼다. <b>난수를 쓰지 않는다.</b>
    ///
    /// <para>
    /// 지켜야 할 성질이 둘이고 서로 긴장 관계다 — 유형별 등장 비율이 share 에 정확히 비례할 것,
    /// 그리고 share 가 낮은 유형이 <b>앞에 뭉치지 않을 것</b>. credit 누적이 둘을 동시에 만족한다.
    /// </para>
    /// <code>
    /// 각 유형에 credit_i += share_i
    /// credit 이 가장 큰 유형을 배출        ← 동률이면 덱 순서
    /// 그 유형의 credit -= 1
    /// </code>
    /// <para>
    /// share 가 0.5 / 0.25 / 0.25 면 <c>A B C A | A B C A | …</c> 가 나온다. 비율은 정확히
    /// 2:1:1 이고 고르게 흩어진다. share 가 낮은 유형은 credit 이 차는 데 시간이 걸려 제
    /// 주기대로만 나오므로, <b>초반 억제 장치를 따로 넣을 이유가 없다</b> — 넣으면 α 와
    /// 손잡이가 겹쳐 조절만 어려워진다.
    /// </para>
    /// <para>
    /// 인스턴스를 몇 개씩 담아 두는 <b>가방 방식은 쓰지 않는다.</b> 가중합과 인스턴스 개수가
    /// 정수에서 어긋나고, 남는 칸을 채우는 규칙이 휴리스틱이 되며, 그 휴리스틱이 결국 난수를
    /// 부른다. 여기서는 credit 이 실수라 어긋날 자리가 없다.
    /// </para>
    /// </summary>
    public sealed class SpawnSequence
    {
        private readonly SpawnShareTable _shares;
        private readonly float[] _credits;

        /// <summary>배출할 유형이 하나도 없다. 벨트는 아무것도 올리지 않는다.</summary>
        public bool IsEmpty => _shares.Count == 0;

        /// <summary>
        /// 덱과 희소성 지수로 배출기를 만든다. 초밥이 비어 있는 항목은 빠진다.
        /// </summary>
        public SpawnSequence(IReadOnlyList<SushiData> deck, float sparsityExponent)
        {
            _shares = new SpawnShareTable(deck, sparsityExponent);

            // credit 배열은 여기서 한 번만 잡는다. Next() 가 매번 배열을 만들면
            // 스폰 경로에 할당이 쌓이고, 배포 타깃이 WebGL 이라 그대로 히칭이 된다.
            _credits = new float[_shares.Count];
        }

        /// <summary>다음에 올릴 초밥. 배출할 유형이 없으면 <c>null</c>.</summary>
        public SushiData Next()
        {
            if (IsEmpty)
            {
                return null;
            }

            var winner = AccrueAndPick();
            _credits[winner] -= 1f;
            return _shares.SushiAt(winner);
        }

        /// <summary>credit 을 전부 0 으로 되돌린다. 스테이지 시작 시 부른다.</summary>
        public void Reset()
        {
            for (var i = 0; i < _credits.Length; i++)
            {
                _credits[i] = 0f;
            }
        }

        /// <summary>
        /// credit 을 채우고 최대값을 고른다.
        ///
        /// <para>
        /// <b>동률은 인덱스가 낮은 쪽 — 덱 순서다.</b> 이 한 줄이 결정성을 만든다.
        /// 여기에 난수가 들어가면 같은 덱이 매번 다른 수열을 내고, 배정 쪽 결정성과
        /// 기준이 갈라진다 (<c>CLAUDE.md</c> §1.1-3b).
        /// </para>
        /// </summary>
        private int AccrueAndPick()
        {
            var winner = 0;
            for (var i = 0; i < _credits.Length; i++)
            {
                _credits[i] += _shares.ShareOf(i);

                if (_credits[i] > _credits[winner])
                {
                    winner = i;
                }
            }

            return winner;
        }
    }
}
