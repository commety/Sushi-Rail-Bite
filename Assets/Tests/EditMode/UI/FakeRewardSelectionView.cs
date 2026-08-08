using System.Collections.Generic;
using SushiDefense.Run;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 손으로 쓴 스텁. 프레젠터가 뷰에 <b>무엇을 몇 번 시켰는지</b>만 기록한다.
    ///
    /// <para>
    /// NSubstitute 같은 패키지를 쓰지 않는다 — 패키지 추가는 팀 합의 사항이고
    /// (<c>CLAUDE.md</c> §7), 이 정도 크기에서는 손으로 쓴 편이 무엇이 검증되는지 더 잘 보인다.
    /// </para>
    /// </summary>
    internal sealed class FakeRewardSelectionView : IRewardSelectionView
    {
        /// <summary>마지막으로 화면에 올라간 후보들.</summary>
        public List<RewardOffer> ShownOffers { get; } = new();

        /// <summary><see cref="IRewardSelectionView.ShowOffers"/> 가 불린 횟수.</summary>
        public int ShowCount { get; private set; }

        /// <summary><see cref="IRewardSelectionView.Hide"/> 가 불린 횟수.</summary>
        public int HideCount { get; private set; }

        public void ShowOffers(IReadOnlyList<RewardOffer> offers)
        {
            ShowCount++;

            // 목록을 복사해 둔다 — 프레젠터가 버퍼를 재사용하므로, 참조만 들면 나중에
            // 읽었을 때 내용이 이미 바뀌어 있다.
            ShownOffers.Clear();
            for (var i = 0; i < offers.Count; i++)
            {
                ShownOffers.Add(offers[i]);
            }
        }

        public void Hide()
        {
            HideCount++;
        }
    }
}
