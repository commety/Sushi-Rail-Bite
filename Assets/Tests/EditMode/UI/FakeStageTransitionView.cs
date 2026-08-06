using System.Collections.Generic;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 손으로 쓴 스텁. 프레젠터가 뷰에 <b>무엇을 어떤 인자로 몇 번 시켰는지</b>만 기록한다.
    ///
    /// <para>
    /// <see cref="HideCount"/> 를 노출하는 이유는 순서 검증 때문이다 — 이벤트 핸들러 안에서
    /// 이 값을 읽으면 "닫기가 발행보다 먼저였나" 를 알 수 있다. "둘 다 일어났다" 만 보면
    /// 순서가 뒤집힌 구현도 통과한다.
    /// </para>
    /// </summary>
    internal sealed class FakeStageTransitionView : IStageTransitionView
    {
        /// <summary>클리어 화면이 받은 <c>(깬 번호, 다음 번호)</c> 기록.</summary>
        public List<(int cleared, int next)> ClearedCalls { get; } = new();

        /// <summary>런 종료 화면이 받은 <c>깬 번호</c> 기록.</summary>
        public List<int> RunCompleteCalls { get; } = new();

        /// <summary><see cref="IStageTransitionView.Hide"/> 가 불린 횟수.</summary>
        public int HideCount { get; private set; }

        public void ShowStageCleared(int clearedStageNumber, int nextStageNumber)
        {
            ClearedCalls.Add((clearedStageNumber, nextStageNumber));
        }

        public void ShowRunComplete(int clearedStageNumber)
        {
            RunCompleteCalls.Add(clearedStageNumber);
        }

        public void Hide()
        {
            HideCount++;
        }
    }
}
