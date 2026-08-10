using System;
using System.Collections.Generic;
using SushiDefense.Run;

namespace SushiDefense.UI
{
    /// <summary>
    /// 보상 선택의 로직. <b>Unity API 를 모른다</b> — 뷰는 인터페이스로만 본다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 이 프로젝트에서 MVP 패턴을 처음 쓰는 지점이다. 화면 로직을 <c>MonoBehaviour</c> 안에
    /// 두면 EditMode 로 검증할 수 없고, M6 에서 UI 를 갈아 끼울 때 로직까지 다시 짜게 된다.
    /// </para>
    /// </summary>
    public sealed class RewardSelectionPresenter
    {
        private readonly IRewardSelectionView _view;
        private readonly RewardGenerator _generator;
        private readonly StageWindowArbiter _windows;

        /// <summary>지금 제시 중인 후보. 틱마다 도는 경로가 아니라 재사용은 편의다.</summary>
        private readonly List<RewardOffer> _offers = new();

        /// <summary>지금 화면이 물고 있는 런. 닫히면 놓는다 — 죽은 참조를 들고 있지 않는다.</summary>
        private RunState _current;

        /// <summary>지금 제시 중인 후보 수. 0 이면 고를 것이 없다.</summary>
        public int OfferCount => _offers.Count;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 보상을 골랐다. 인자는 <b>런에 반영된</b> 보상이며, 건너뛴 경우 발생하지 않는다.
        /// </summary>
        public event Action<RewardOffer> RewardChosen;

        /// <summary>
        /// 화면이 닫혔다 — 골랐든 건너뛰었든 발생한다.
        ///
        /// <para>
        /// <b>지금은 아무도 구독하지 않아도 된다.</b> 다음 스테이지로 넘어가는 것
        /// (<c>RunState.AdvanceStage</c>)은 M4 이고, 이 이벤트가 그때 붙일 자리다.
        /// </para>
        /// </summary>
        public event Action Closed;

        public RewardSelectionPresenter(IRewardSelectionView view, RewardGenerator generator,
                                       StageWindowArbiter windows)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        }

        /// <summary>
        /// 이 런의 보상 후보를 뽑아 화면을 연다.
        ///
        /// <para>
        /// <b>후보가 0개여도 연다.</b> 조용히 건너뛰면 클리어했는데 아무 화면도 안 뜨는 상태가
        /// 되어, 보상 시스템이 고장 난 것과 구분되지 않는다.
        /// </para>
        /// </summary>
        public void Open(RunState run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            // 조정자는 보상을 거절하지 않는다 — 닫으면 다음 판이라 나중에 다시 열 방법이
            // 없다. 위에 창이 떠 있으면 그 밑으로 깔린다.
            _windows.TryOpen(StageWindow.Reward);

            _generator.Generate(run, _offers);
            _current = run;
            IsOpen = true;

            _view.ShowOffers(_offers);
        }

        /// <summary>
        /// 후보 하나를 고른다. 범위 밖 인덱스이거나 화면이 닫혀 있으면 <c>false</c> 를
        /// 돌려주고 <b>화면을 그대로 둔다</b>.
        /// </summary>
        public bool Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= _offers.Count)
            {
                return false;
            }

            var offer = _offers[index];
            RewardGenerator.Apply(_current, offer);

            Close();
            RewardChosen?.Invoke(offer);
            return true;
        }

        /// <summary>
        /// 아무것도 고르지 않고 닫는다. 후보가 0개일 때의 <b>유일한 출구</b>이기도 하다.
        /// </summary>
        public void Skip()
        {
            if (!IsOpen)
            {
                return;
            }

            Close();
        }

        /// <summary>
        /// 닫기의 한 경로. 고르기와 건너뛰기가 각자 닫으면 <see cref="Closed"/> 발행이
        /// 두 곳에 살게 되고, 한쪽만 고쳐지는 사고가 난다.
        /// </summary>
        private void Close()
        {
            IsOpen = false;
            _current = null;
            _windows.Close(StageWindow.Reward);
            _view.Hide();
            Closed?.Invoke();
        }
    }
}
