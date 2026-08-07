using System;

namespace SushiDefense.Settings
{
    /// <summary>
    /// 플레이어가 고른 설정. <b>무엇을 골랐나만 답한다</b> — 저장도 적용도 하지 않는다.
    ///
    /// <para>
    /// 설정은 세 층이다: 값(여기) · 저장(<see cref="ISettingsStore"/>) ·
    /// 적용(<c>Presentation</c>). 합치면 "소리가 안 줄었다" 의 원인이 셋 중 어느 쪽인지
    /// 테스트에서 구분되지 않는다 — <c>SoundBudget</c> 과 <c>AudioUnlockGate</c> 를
    /// 합치지 않은 것과 같은 판단이다.
    /// </para>
    /// <para>
    /// <b>범위 밖 값에 예외를 던지지 않는다.</b> 슬라이더가 만드는 값이고 저장소가 손상된
    /// 값을 돌려줄 수도 있다 — 그때마다 화면이 죽으면 설정을 열 수 없게 된다. 자르는 곳은
    /// 여기 한 곳이며, 프로퍼티에 공개 세터가 없으므로 <b>자르지 않고 값이 들어올 길이
    /// 없다.</b>
    /// </para>
    /// </summary>
    public sealed class GameSettings
    {
        /// <summary>
        /// 설정을 만진 적 없을 때의 볼륨. 밸런스가 아니라 <i>"아직 고르지 않았다"</i> 의
        /// 값이라 코드에 둔다 — 0 이면 소리가 안 나는 것을 버그로 신고하게 된다.
        /// </summary>
        public const float DefaultMasterVolume = 1f;

        /// <summary>전체 볼륨. <c>0</c>~<c>1</c> 이다.</summary>
        public float MasterVolume { get; private set; } = DefaultMasterVolume;

        /// <summary>전체화면인가.</summary>
        public bool Fullscreen { get; private set; }

        /// <summary>
        /// 값 하나라도 바뀌었다. <b>무엇이 바뀌었는지 구분하지 않는다</b> — 구독자가 둘 다
        /// 다시 적용하는 편이 싸고, 나누면 한쪽만 구독하는 사고가 난다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 볼륨을 정한다. 범위 밖은 잘린다. <b>자른 뒤의 값</b>으로 변화를 판단하므로,
        /// 슬라이더를 상한 밖에서 흔드는 동안 알림이 계속 나가지 않는다.
        /// </summary>
        public void SetMasterVolume(float value)
        {
            // 숫자가 아닌 값은 자르기가 통하지 않는다 — 비교가 전부 false 라 그대로 들어가고,
            // 그 뒤로는 볼륨이 영영 복구되지 않는다. 저장소가 손상된 값을 돌려줄 수 있으므로
            // 실제로 도달 가능한 경로다. 무시하는 편이 임의의 값으로 덮는 것보다 낫다.
            if (float.IsNaN(value))
            {
                return;
            }

            var clamped = Math.Min(1f, Math.Max(0f, value));
            if (MasterVolume == clamped)
            {
                return;
            }

            MasterVolume = clamped;
            Changed?.Invoke();
        }

        /// <summary>전체화면 여부를 정한다.</summary>
        public void SetFullscreen(bool value)
        {
            if (Fullscreen == value)
            {
                return;
            }

            Fullscreen = value;
            Changed?.Invoke();
        }
    }
}
