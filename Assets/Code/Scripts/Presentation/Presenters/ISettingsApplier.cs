using SushiDefense.Settings;

namespace SushiDefense.UI
{
    /// <summary>
    /// 고른 설정을 실제 엔진에 먹인다. <b>구현만 Unity 를 안다.</b>
    ///
    /// <para>
    /// 설정은 세 층이다 — 값(<see cref="GameSettings"/>) · 저장(<see cref="ISettingsStore"/>) ·
    /// 적용(여기). 합치면 <i>"소리가 안 줄었다"</i> 의 원인이 셋 중 어느 쪽인지 테스트에서
    /// 구분되지 않는다 (<c>SoundBudget</c> 과 <c>AudioUnlockGate</c> 를 합치지 않은 것과
    /// 같은 판단이다).
    /// </para>
    /// </summary>
    public interface ISettingsApplier
    {
        /// <summary>
        /// 엔진이 <b>실제로</b> 전체화면인가. 고른 값이 아니다.
        ///
        /// <para>
        /// 둘을 나누는 이유는 <b>적용이 거부될 수 있기 때문</b>이다. 브라우저는 사용자
        /// 제스처 밖에서 전체화면 요청을 무시하므로, 저장된 선호와 지금 화면의 상태가
        /// 갈릴 수 있다 — 화면에는 후자를 보여 줘야 무엇이 참인지 알 수 있다.
        /// </para>
        /// </summary>
        bool IsFullscreen { get; }

        /// <summary>
        /// 지금 값을 엔진에 먹인다. <b>설정을 고쳐 쓰지 않는다</b> — 거부된 결과는
        /// <see cref="IsFullscreen"/> 으로 되읽는다. 여기서 모델을 덮으면 페이지를 새로 연
        /// 것만으로 저장된 선호가 지워진다.
        /// </summary>
        void Apply(GameSettings settings);
    }
}
