namespace SushiDefense.UI
{
    /// <summary>
    /// 설정 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b>Unity 타입이 하나도 없다.</b> 프레젠터를 EditMode 로 검증하기 위해서다
    /// (<c>CLAUDE.md</c> §3.6).
    /// </para>
    /// <para>
    /// <b>값을 인자로 받는다.</b> 화면이 슬라이더·토글의 현재 값을 진실로 삼으면 모델의
    /// 것과 어긋날 수 있고, 그때 어느 쪽이 참인지 알 수 없다 (<see cref="IStageMenuView"/>
    /// 가 «지금 멈춰 있나» 를 인자로 받는 것과 같은 이유다).
    /// </para>
    /// </summary>
    public interface ISettingsView
    {
        /// <summary>
        /// 지금 값을 화면에 반영한다. 슬라이더·토글이 여기서 맞춰진다.
        ///
        /// <para>
        /// <paramref name="fullscreen"/> 은 <b>고른 값이 아니라 실제로 그렇게 됐는지</b>다 —
        /// 브라우저가 전체화면을 거부할 수 있고, 그때 토글만 켜져 있으면 플레이어가 무엇이
        /// 참인지 알 수 없다 (<c>SettingsApplier</c> 의 클래스 주석).
        /// </para>
        /// </summary>
        void ShowSettings(float masterVolume, bool fullscreen);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
