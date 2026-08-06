namespace SushiDefense.Audio
{
    /// <summary>
    /// 첫 사용자 입력이 있기 전까지 재생을 막는다.
    ///
    /// <para>
    /// 배포 타깃(WebGL)에서 브라우저는 사용자 제스처가 있기 전까지 오디오를 잠근다.
    /// 그 상태에서 낸 소리는 <b>밀리지 않고 그냥 사라진다</b> — 배경음을 씬 시작에 재생하면
    /// 무음으로 흘러가 영영 들리지 않는다. 증상이 "소리가 안 난다" 라서 볼륨·클립·믹서를
    /// 뒤지게 되는데, 원인은 재생 시점이다.
    /// </para>
    /// <para>
    /// <see cref="SoundBudget"/> 과 합치지 않는다. 예산은 <i>너무 많다·너무 잦다</i> 를,
    /// 이쪽은 <i>아직 아무것도 안 된다</i> 를 본다 — 합치면 "소리가 안 났다" 의 원인이
    /// 둘 중 어느 쪽인지 테스트에서 구분되지 않는다.
    /// </para>
    /// </summary>
    public sealed class AudioUnlockGate
    {
        private bool _momentPending;

        /// <summary>재생이 허용된 상태인가.</summary>
        public bool IsUnlocked { get; private set; }

        /// <summary>
        /// 첫 입력에서 연다. 두 번째 호출부터는 아무 일도 하지 않는다 — 입력은 계속
        /// 들어오는데 그때마다 <see cref="TryConsumeUnlockMoment"/> 가 되살아나면
        /// 배경음이 처음부터 다시 재생된다.
        /// </summary>
        public void Unlock()
        {
            if (IsUnlocked)
            {
                return;
            }

            IsUnlocked = true;
            _momentPending = true;
        }

        /// <summary>
        /// 열린 뒤 <b>처음 한 번만</b> <c>true</c>. 배경음을 시작할 시점을 여기서 잡는다.
        /// </summary>
        public bool TryConsumeUnlockMoment()
        {
            if (!_momentPending)
            {
                return false;
            }

            _momentPending = false;
            return true;
        }
    }
}
