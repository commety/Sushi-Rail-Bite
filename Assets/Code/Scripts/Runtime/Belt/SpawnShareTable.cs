using System.Collections.Generic;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 덱 + α → 유형별 등장 비율. <b>가격을 읽는 세 지점 중 하나다.</b>
    ///
    /// <para>
    /// <c>share_i ∝ (덱 내 최저가 / 가격_i) ^ α</c> 로 계산해 총합이 1 이 되도록 정규화한다.
    /// 목표는 "비쌀수록 적게, 쌀수록 많이" 이며, 다만 비싼 카드도 제 주기로는 반드시 나온다.
    /// </para>
    /// <para>
    /// <b>가격의 비율만 쓴다.</b> <c>가격 / 10</c> 같은 절대 스케일 연산이 없어서 100엔 덱이든
    /// 1000엔 덱이든 동일하게 동작하고, 별도 정규화 결정이 필요 없다.
    /// </para>
    /// <para>
    /// <b>최저가는 덱 안에서 구한다.</b> 스테이지 전체나 전역 최저가가 아니다. 덱이 바뀌면 남은
    /// 카드의 등장률도 바뀌는데, 이건 버그가 아니라 설계 목표다 — 비싼 카드를 넣으면 나머지가
    /// 상대적으로 흔해지는 것이 덱빌딩의 의미다.
    /// </para>
    /// <para>
    /// 이 클래스는 <b>"얼마나 자주"</b> 만 답한다. 배출 순서는 <see cref="SpawnSequence"/> 다.
    /// </para>
    /// </summary>
    public sealed class SpawnShareTable
    {
        private readonly SushiData[] _sushi;
        private readonly float[] _shares;

        /// <summary>덱에 실제로 오른 유형 수. 초밥이 비어 있는 항목은 빠진다.</summary>
        public int Count => _sushi.Length;

        /// <summary>덱 안 최저가. 모든 share 의 기준점이다. 덱이 비었으면 0.</summary>
        public int MinimumPrice { get; }

        /// <summary>
        /// 덱과 희소성 지수로 비율표를 만든다.
        ///
        /// <para>
        /// 계산을 전부 여기서 끝내 배열에 담는다. <see cref="ShareOf"/> 가 호출마다
        /// <c>Mathf.Pow</c> 를 돌면 스폰 경로에 연산이 쌓인다 — 배포 타깃이 WebGL 이다.
        /// </para>
        /// </summary>
        public SpawnShareTable(IReadOnlyList<SushiData> deck, float sparsityExponent)
        {
            var included = Collect(deck);
            _sushi = included.ToArray();
            _shares = new float[_sushi.Length];

            if (_sushi.Length == 0)
            {
                return;
            }

            MinimumPrice = FindMinimumPrice(_sushi);
            FillShares(sparsityExponent);
        }

        /// <summary>이 인덱스의 초밥 종류. 덱 순서를 유지한다.</summary>
        public SushiData SushiAt(int index) => _sushi[index];

        /// <summary>이 인덱스의 등장 비율. 전체 합은 1 이다.</summary>
        public float ShareOf(int index) => _shares[index];

        private static List<SushiData> Collect(IReadOnlyList<SushiData> deck)
        {
            var included = new List<SushiData>();
            if (deck == null)
            {
                return included;
            }

            for (var i = 0; i < deck.Count; i++)
            {
                if (deck[i] == null)
                {
                    continue;
                }

                included.Add(deck[i]);
            }

            return included;
        }

        private static int FindMinimumPrice(SushiData[] sushi)
        {
            var minimum = sushi[0].Price;
            for (var i = 1; i < sushi.Length; i++)
            {
                if (sushi[i].Price < minimum)
                {
                    minimum = sushi[i].Price;
                }
            }

            return minimum;
        }

        /// <summary>
        /// 정규화까지 끝낸다. <c>(최저가/가격)^α</c> 의 합은 1 이 아니므로 그대로 두면
        /// credit 누적(<see cref="SpawnSequence"/>)이 서 있는 "한 번 스폰에 credit 총합이
        /// 1 만큼 는다" 는 전제가 무너진다.
        /// </summary>
        private void FillShares(float sparsityExponent)
        {
            var total = 0f;
            for (var i = 0; i < _sushi.Length; i++)
            {
                var ratio = (float)MinimumPrice / _sushi[i].Price;
                _shares[i] = Mathf.Pow(ratio, sparsityExponent);
                total += _shares[i];
            }

            for (var i = 0; i < _shares.Length; i++)
            {
                _shares[i] /= total;
            }
        }
    }
}
