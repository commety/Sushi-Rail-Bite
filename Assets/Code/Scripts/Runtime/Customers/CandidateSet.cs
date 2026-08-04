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
    /// 다만 상한이 없으면 한참 지나간 초밥까지 집게 되므로 인식 후 일정 시간이 지나면
    /// 만료된다. <b>상한이 0 이면 만료가 없다</b> — "즉시 만료" 가 아니다.
    /// </para>
    /// <para>
    /// 후보에서 빠지는 경우는 넷뿐이다 — 소비됨 / 벨트에서 제거됨 / 손님 자격 상실 / 래치 만료.
    /// </para>
    /// </summary>
    public sealed class CandidateSet
    {
        private readonly float _latchSeconds;
        private readonly List<SushiItem> _items = new();
        private readonly List<float> _recognizedAt = new();

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
        public bool Recognize(SushiItem sushi, float nowSeconds)
        {
            if (sushi == null || _items.Contains(sushi))
            {
                return false;
            }

            _items.Add(sushi);
            _recognizedAt.Add(nowSeconds);
            return true;
        }

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
            _recognizedAt.Clear();
        }

        /// <summary>래치 시간이 지난 후보를 정리한다. 상한이 0 이면 아무것도 만료되지 않는다.</summary>
        public void ExpireOlderThan(float nowSeconds)
        {
            if (_latchSeconds <= 0f)
            {
                return;
            }

            // 제거가 뒤 원소를 당기므로 뒤에서부터 훑는다. 순서를 바꾸는 swap-remove 는
            // 쓰지 않는다 — 배정 입력의 순서가 결정적이어야 한다.
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                if (nowSeconds - _recognizedAt[i] > _latchSeconds)
                {
                    RemoveAt(i);
                }
            }
        }

        private void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            _recognizedAt.RemoveAt(index);
        }
    }
}
