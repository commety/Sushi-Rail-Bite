namespace SushiDefense.UI
{
    /// <summary>
    /// 화면이 뜬 직후 <b>잠깐 입력을 받지 않는다.</b> <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 웹에서 Unity 준비 화면(로딩 바)이 떠 있는 동안 버튼이 놓일 자리를 누르면, 게임이
    /// 뜨자마자 <b>그 클릭이 그대로 배달되어</b> 누른 적 없는 화면으로 넘어간다. 로딩 중의
    /// 입력은 브라우저가 물고 있다가 첫 프레임에 한꺼번에 흘려보내기 때문이다.
    /// </para>
    /// <para>
    /// <b>시간과 프레임을 함께 센다.</b> 첫 프레임의 <c>deltaTime</c> 은 로딩 시간만큼
    /// 부풀어 있을 수 있어, 시간만 세면 <b>단 한 프레임 만에 조건을 넘긴다</b> — 정작
    /// 막아야 할 그 프레임이다. 반대로 프레임만 세면 프레임률이 높은 기기에서 너무 빨리
    /// 열린다. 둘 다 지나야 열린다.
    /// </para>
    /// <para>
    /// <b>«아직 안 눌렀다» 를 확인하지 않는다.</b> 그러려면 입력 장치를 알아야 하고, 그러면
    /// 이 판단이 EditMode 밖으로 나간다. 짧은 무시 구간 하나가 같은 일을 하며, 사람이
    /// 의도적으로 누른 클릭은 이 구간 뒤에 온다.
    /// </para>
    /// </summary>
    public sealed class InputArmGate
    {
        private readonly float _delaySeconds;
        private readonly int _delayFrames;

        private float _elapsed;
        private int _frames;

        /// <summary>지금 입력을 받아도 되나.</summary>
        public bool IsArmed { get; private set; }

        /// <param name="delaySeconds">열리기까지의 최소 시간. 0 이하면 시간 조건이 없다.</param>
        /// <param name="delayFrames">열리기까지의 최소 프레임 수. 0 이하면 프레임 조건이 없다.</param>
        public InputArmGate(float delaySeconds, int delayFrames)
        {
            _delaySeconds = delaySeconds;
            _delayFrames = delayFrames;
            IsArmed = delaySeconds <= 0f && delayFrames <= 0;
        }

        /// <summary>
        /// 한 프레임 흘린다. <b>한 번 열리면 다시 닫히지 않는다</b> — 닫힐 수 있으면
        /// 판이 도는 중에 입력이 사라지는 구간이 생긴다.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (IsArmed)
            {
                return;
            }

            _frames++;

            if (deltaSeconds > 0f)
            {
                _elapsed += deltaSeconds;
            }

            IsArmed = _elapsed >= _delaySeconds && _frames >= _delayFrames;
        }
    }
}
