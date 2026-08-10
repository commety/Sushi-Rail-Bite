using System.Collections.Generic;
using SushiDefense.Belt;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 한 손님이 인식한 초밥들.
    ///
    /// <para>
    /// <b>인식은 래치된다.</b> 한 번 범위에 들어온 초밥은 물리적으로 범위를 조금 벗어나도
    /// 후보에서 빠지지 않는다 — 계산 지연 때문에 눈앞에서 놓치는 그림보다, 인식된 것은
    /// 처리되는 편이 플레이 경험이 낫다는 기획 판단이다 (<c>CLAUDE.md</c> §1.1-3c).
    /// </para>
    /// <para>
    /// 다만 상한이 없으면 한참 지나간 초밥까지 집게 되므로, <b>범위를 벗어나고</b> 일정
    /// 시간이 지나면 만료된다. <b>상한이 0 이면 만료가 없다</b> — "즉시 만료" 가 아니다.
    /// </para>
    /// <para>
    /// <b>기준은 인식 시각이 아니라 이탈 시각이다</b> (M2.5). 인식 기준으로 재면
    /// <c>래치 ≥ 범위 통과 시간</c> 일 때만 의도와 일치하는데, stage01 은 통과 3.0초에
    /// 래치 1.0초라 그 조건을 어긴다 — 범위 안에 멀쩡히 있는 초밥이 후보에서 빠졌다.
    /// 이탈 기준이면 마감시한(<c>now ≥ 이탈</c>)이 만료(<c>now &gt; 이탈 + 래치</c>)보다
    /// 항상 먼저 와서, <b>"손님은 유한 시간 안에 반드시 집는다" 가 밸런스 값과 무관하게
    /// 성립한다.</b>
    /// </para>
    /// <para>
    /// 부수 효과로 이 클래스는 <b>시계를 갖지 않아도 된다</b> — 인식 시점의 "지금" 을
    /// 받아 둘 필요가 없고, 기하로 정해진 이탈 시각 하나면 충분하다.
    /// </para>
    /// <para>
    /// 후보에서 빠지는 경우는 넷뿐이다 — 소비됨 / 벨트에서 제거됨 / 손님 자격 상실 / 래치 만료.
    /// </para>
    /// </summary>
    public sealed class CandidateSet
    {
        private readonly float _latchSeconds;
        private readonly List<SushiItem> _items = new();
        private readonly List<float> _exitAt = new();

        /// <summary>인식 중인 초밥 수.</summary>
        public int Count => _items.Count;

        /// <summary>인식 중인 초밥. <b>인식 순서를 유지한다</b> — 배정 입력이 결정적이어야 한다.</summary>
        public IReadOnlyList<SushiItem> Items => _items;

        /// <param name="latchSeconds">인식을 붙들어 두는 시간. 0 이면 상한 없음.</param>
        public CandidateSet(float latchSeconds)
        {
            _latchSeconds = latchSeconds;
        }

        /// <summary>
        /// 범위 진입 시 후보로 등록한다. 이미 있으면 <c>false</c> 를 돌려주고
        /// <b>인식 시각을 갱신하지 않는다</b> — 갱신하면 범위 안에 머무는 동안 래치가 계속
        /// 연장되어 상한이 무의미해진다.
        /// </summary>
        public bool Recognize(SushiItem sushi, float exitAtSeconds)
        {
            if (sushi == null || _items.Contains(sushi))
            {
                return false;
            }

            _items.Add(sushi);
            _exitAt.Add(exitAtSeconds);
            return true;
        }

        /// <summary>
        /// 인식 순서의 <paramref name="index"/> 번째 후보가 범위를 벗어나는 <b>절대 시각</b>.
        /// <see cref="Items"/> 와 인덱스가 나란하다.
        /// </summary>
        public float ExitAtOf(int index) => _exitAt[index];

        /// <summary>소비·벨트 제거 시 후보에서 뺀다.</summary>
        public bool Forget(SushiItem sushi)
        {
            var index = _items.IndexOf(sushi);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        /// <summary>손님이 자격을 잃었을 때 전부 비운다.</summary>
        public void Clear()
        {
            _items.Clear();
            _exitAt.Clear();
        }

        /// <summary>
        /// <b>범위를 벗어나고</b> 래치 시간이 더 지난 후보를 정리한다.
        /// 상한이 0 이면 아무것도 만료되지 않는다 — "이탈 즉시 만료" 가 아니다.
        /// </summary>
        public void ExpirePastLatch(float nowSeconds)
        {
            if (_latchSeconds <= 0f)
            {
                return;
            }

            // 제거가 뒤 원소를 당기므로 뒤에서부터 훑는다. 순서를 바꾸는 swap-remove 는
            // 쓰지 않는다 — 배정 입력의 순서가 결정적이어야 한다.
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                if (nowSeconds - _exitAt[i] > _latchSeconds)
                {
                    RemoveAt(i);
                }
            }
        }

        private void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            _exitAt.RemoveAt(index);
        }
    }
}
