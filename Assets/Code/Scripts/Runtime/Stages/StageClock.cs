using System;

namespace SushiDefense.Stages
{
    /// <summary>
    /// 제한 시간 대비 경과.
    ///
    /// <para>
    /// <b>스스로 시간을 읽지 않는다.</b> <c>Time.deltaTime</c> 대신 <see cref="Advance"/>
    /// 인자로 받기 때문에 "59.9초가 흘렀을 때" 를 프레임 대기 없이 EditMode 로 검증할 수
    /// 있다 (<c>SushiBelt</c>·<c>ClaimCoordinator</c> 와 같은 방식).
    /// </para>
    /// <para>
    /// 클리어 여부를 판단하지 않는다 — 그건 <see cref="StageEvaluator"/> 다.
    /// </para>
    /// </summary>
    public sealed class StageClock
    {
        /// <summary>이 스테이지의 제한 시간(초). <see cref="Reset"/> 이 건드리지 않는다.</summary>
        public float LimitSeconds { get; }

        /// <summary>
        /// 시작 이후 흐른 시간. <b>제한 시간을 넘겨도 계속 자란다</b> — 얼마나 넘겼는지는
        /// 진단에 쓸 수 있어야 한다. 잘리는 것은 <see cref="RemainingSeconds"/> 쪽이다.
        /// </summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>남은 시간. <b>0 아래로 내려가지 않는다</b> — 표시에 음수가 새면 안 된다.</summary>
        public float RemainingSeconds => Math.Max(0f, LimitSeconds - ElapsedSeconds);

        /// <summary>제한 시간에 <b>도달했거나</b> 넘었다. 정각이 만료다.</summary>
        public bool IsExpired => ElapsedSeconds >= LimitSeconds;

        /// <summary>
        /// 제한 시간을 정해 시계를 연다. 0 이하는 예외다 — <c>StageConfig.OnValidate</c> 가
        /// 이미 1초 하한을 걸지만, 이 클래스는 SO 없이도 만들어질 수 있다.
        /// </summary>
        public StageClock(float limitSeconds)
        {
            if (limitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(limitSeconds), limitSeconds,
                                                      "제한 시간은 0보다 커야 합니다.");
            }

            LimitSeconds = limitSeconds;
        }

        /// <summary>
        /// 시간을 흘린다. 음수는 예외다 — 조용히 통과시키면 시간이 되감기는 경로가 생긴다
        /// (<c>RevenueLedger.Add</c> 가 음수 가격을 예외로 두는 것과 같은 판단).
        /// </summary>
        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds,
                                                      "시간은 되감을 수 없습니다.");
            }

            ElapsedSeconds += deltaSeconds;
        }

        /// <summary>경과를 0 으로 되돌린다. <b>제한 시간은 그대로다.</b></summary>
        public void Reset()
        {
            ElapsedSeconds = 0f;
        }
    }
}
