using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Run
{
    /// <summary>
    /// 이 런이 들고 다니는 초밥 덱. <b>런 중에 자란다</b> — 클리어 보상이 카드를 더한다.
    ///
    /// <para>
    /// <c>StageConfig.SpawnTable</c> 은 이 덱의 <b>시작 상태</b>일 뿐이다. 벨트가 SO 를 직접
    /// 읽으면 보상으로 얻은 초밥이 영영 벨트에 나오지 않는다.
    /// </para>
    /// <para>
    /// <b>추가 순서를 유지한다.</b> <c>SpawnSequence</c> 가 credit 동률을 덱 순서로 끊으므로
    /// (<c>CLAUDE.md</c> §1.1-3b 와 같은 결정성), 순서가 흔들리면 스폰 결과가 흔들린다.
    /// <c>HashSet</c> 으로 갈아타지 않는 이유가 이것이며, 덱은 수십 장 규모라
    /// <c>List.Contains</c> 로 충분하다.
    /// </para>
    /// <para>
    /// <b>카드를 빼는 경로가 없다.</b> 덱에서 카드가 사라지는 기획이 없다.
    /// </para>
    /// </summary>
    public sealed class SushiDeck
    {
        private readonly List<SushiData> _cards = new();

        /// <summary>덱에 든 카드. 추가 순서를 유지한다.</summary>
        public IReadOnlyList<SushiData> Cards => _cards;

        /// <summary>덱에 든 카드 수.</summary>
        public int Count => _cards.Count;

        /// <summary>빈 덱으로 시작한다.</summary>
        public SushiDeck()
        {
        }

        /// <summary>
        /// 주어진 카드로 덱을 연다. <c>null</c> 과 중복은 빠지므로
        /// <see cref="TryAdd"/> 를 반복한 것과 같은 결과가 나온다.
        /// </summary>
        public SushiDeck(IEnumerable<SushiData> cards)
        {
            if (cards == null)
            {
                return;
            }

            foreach (var card in cards)
            {
                TryAdd(card);
            }
        }

        /// <summary>이 카드가 덱에 있나.</summary>
        public bool Contains(SushiData card)
        {
            return card != null && _cards.Contains(card);
        }

        /// <summary>
        /// 카드를 더한다. 이미 있거나 <c>null</c> 이면 <c>false</c> 를 돌려주고
        /// <b>덱을 그대로 둔다</b>.
        /// </summary>
        public bool TryAdd(SushiData card)
        {
            if (card == null || _cards.Contains(card))
            {
                return false;
            }

            _cards.Add(card);
            return true;
        }

        /// <summary>
        /// 스테이지 설정의 시작 덱에서 만든다. 빈 슬롯(<c>null</c>)은 빠진다 — 인스펙터에서
        /// 목록을 늘리면 자연히 생기는 상태다.
        ///
        /// <para>
        /// <b>사본이다.</b> 이후 덱이 자라도 SO 는 그대로다 — 런타임이 SO 를 수정하면 플레이
        /// 종료 후에도 디스크에 남는다 (<c>.claude/rules/scriptable-object.md</c> §2).
        /// </para>
        /// </summary>
        public static SushiDeck FromSpawnTable(StageConfig config)
        {
            var deck = new SushiDeck();
            if (config == null)
            {
                return deck;
            }

            var table = config.SpawnTable;
            for (var i = 0; i < table.Count; i++)
            {
                deck.TryAdd(table[i]?.Sushi);
            }

            return deck;
        }
    }
}
