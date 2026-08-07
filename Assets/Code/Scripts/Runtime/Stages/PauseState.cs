using System;

namespace SushiDefense.Stages
{
    /// <summary>
    /// 이 판이 멈춰 있나. <b>시간을 흘릴지 말지의 유일한 진실</b>이다.
    ///
    /// <para>
    /// 이 게임의 시간은 전부 씬 진입점의 <c>Tick</c> 한 줄을 지난다. 그 줄을 건너뛰면
    /// 벨트·손님·시계·판정이 함께 선다 — 멈춤을 위해 새 경로를 만들 필요가 없다.
    /// </para>
    /// <para>
    /// <b>전역 시간 배율을 쓰지 않는 이유</b>가 셋이다. 전역이라 누가 멈췄는지 추적할 수
    /// 없고, UI 애니메이션과 실시간 대기까지 함께 얼리며, 무엇보다 "멈췄다" 를 확인하려면
    /// 프레임을 세야 한다. 여기 있는 <c>bool</c> 하나는 프레임 없이 검증된다.
    /// </para>
    /// <para>
    /// <b>스테이지가 아니라 런의 것이다.</b> 판이 바뀔 때마다 새로 만들면 멈춘 상태가
    /// 조용히 풀린다 — 다만 새 판은 재개 상태로 열려야 하므로, 판을 세우는 쪽이
    /// <see cref="Resume"/> 를 부른다.
    /// </para>
    /// </summary>
    public sealed class PauseState
    {
        /// <summary>지금 멈춰 있나.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 멈춤 여부가 바뀌었다. <b>값이 실제로 달라질 때만</b> 발생한다 —
        /// <c>RecruitWallet.BalanceChanged</c> 와 같은 규칙이다.
        ///
        /// <para>
        /// 억제를 "이미 알렸다" 플래그로 하지 않는다. 그러면 두 번째 일시정지가 조용히
        /// 사라진다 — 비교 대상은 발행 이력이 아니라 <b>지금 값</b>이다.
        /// </para>
        /// </summary>
        public event Action<bool> Changed;

        /// <summary>멈춘다. 이미 멈춰 있으면 아무 일도 하지 않는다.</summary>
        public void Pause()
        {
            Set(true);
        }

        /// <summary>다시 흐르게 한다. 이미 흐르고 있으면 아무 일도 하지 않는다.</summary>
        public void Resume()
        {
            Set(false);
        }

        /// <summary>
        /// 바뀌는 유일한 경로. 두 메서드가 각자 알리면 나중에 한쪽만 고쳐진다 —
        /// <c>RewardSelectionPresenter.Close</c> 와 같은 판단이다.
        /// </summary>
        private void Set(bool paused)
        {
            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Changed?.Invoke(paused);
        }
    }
}
