using System;
using SushiDefense.Data;
using SushiDefense.Run;

namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지를 깬 뒤 다음 판으로 넘어가기 전의 한 박자. <b>Unity API 를 모른다</b> —
    /// 뷰는 인터페이스로만 본다 (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 이 박자가 없으면 보상 화면이 닫히자마자 다음 스테이지가 시작되어, 플레이어는 자기가
    /// 몇 판째인지도 런이 끝났는지도 알 수 없다.
    /// </para>
    /// <para>
    /// <b>화면을 여는 것과 상태를 바꾸는 것은 다른 일이다.</b> <see cref="Open"/> 이 스테이지
    /// 번호를 올리면 플레이어가 확인 입력을 하기도 전에 HUD 의 번호가 바뀐다. 상태는
    /// <see cref="Proceed"/> 에서만 바뀐다.
    /// </para>
    /// </summary>
    public sealed class StageTransitionPresenter
    {
        private readonly IStageTransitionView _view;
        private readonly RunProgression _progression;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 지금 떠 있는 화면이 "런 종료" 인가.
        ///
        /// <para>
        /// <b>저장하지 않고 유도한다.</b> 필드로 들면 닫을 때 되돌리는 것을 잊어 닫힌
        /// 화면이 종료 화면인 척하게 된다 — <c>RunProgression.IsRunComplete</c> 와 같은
        /// 판단이다. 화면이 떠 있는 동안 <c>HasNextStage</c> 는 바뀌지 않으므로 값도 흔들리지
        /// 않는다.
        /// </para>
        /// </summary>
        public bool IsRunFinale => IsOpen && !_progression.HasNextStage;

        /// <summary>
        /// 다음 스테이지로 넘어간다. 인자는 <b>넘어갈</b> 스테이지이며, 런이 끝났으면
        /// 발생하지 않는다.
        /// </summary>
        public event Action<StageConfig> StageAdvanced;

        /// <summary>마지막 스테이지였다. 넘어갈 곳이 없다.</summary>
        public event Action RunCompleted;

        public StageTransitionPresenter(IStageTransitionView view, RunProgression progression)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
        }

        /// <summary>
        /// 클리어 직후 화면을 연다. <b>런 상태를 바꾸지 않는다.</b>
        ///
        /// <para>
        /// 두 번 불러도 화면만 다시 그려진다 — 상태를 건드리지 않으므로 중복 호출이
        /// 사고가 되지 않는다.
        /// </para>
        /// </summary>
        public void Open()
        {
            IsOpen = true;

            var cleared = _progression.CurrentStageNumber;
            if (IsRunFinale)
            {
                _view.ShowRunComplete(cleared);
                return;
            }

            _view.ShowStageCleared(cleared, cleared + 1);
        }

        /// <summary>
        /// 확인 입력. <b>여기서 런이 움직인다.</b>
        ///
        /// <para>
        /// 화면을 <b>먼저 내리고</b> 이벤트를 발행한다. 구독자가 스테이지를 새로 세우는데
        /// (<c>StageBootstrap</c>) 그때 화면이 아직 떠 있으면 새 판 위에 전환 화면이 겹쳐
        /// 남는다.
        /// </para>
        /// <para>
        /// 화면이 닫혀 있으면 아무 일도 하지 않는다. 이 가드 하나가 <b>중복 확인 입력</b>을
        /// 막으며, 별도의 발행 플래그를 두지 않는 이유다.
        /// </para>
        /// </summary>
        /// <returns>다음 스테이지로 넘어갔으면 <c>true</c>. 런이 끝났으면 <c>false</c>.</returns>
        public bool Proceed()
        {
            if (!IsOpen)
            {
                return false;
            }

            var advanced = _progression.AdvanceAfterClear();
            Close();

            if (advanced)
            {
                StageAdvanced?.Invoke(_progression.CurrentStage);
            }
            else
            {
                RunCompleted?.Invoke();
            }

            return advanced;
        }

        /// <summary>
        /// 닫기의 유일한 경로. 여러 곳에서 닫으면 나중에 한쪽만 고쳐진다 —
        /// <c>RewardSelectionPresenter</c> 와 같은 판단이다.
        /// </summary>
        private void Close()
        {
            IsOpen = false;
            _view.Hide();
        }
    }
}
