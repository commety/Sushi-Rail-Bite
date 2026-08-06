using System;
using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Run
{
    /// <summary>
    /// 런이 스테이지 목록의 어디쯤 와 있는가 — 지금 몇 번째인가 · 다음이 있는가 ·
    /// 런이 끝났는가.
    ///
    /// <para>
    /// <b>런 종료를 플래그로 들지 않는다.</b> "끝났다" 는 <c>bool</c> 필드를 두면
    /// <see cref="RunState.StageNumber"/> 와 어긋날 수 있고, 어긋난 상태는 "3판 깼는데
    /// 안 끝난다" 또는 그 반대로 나타난다. 유도값이면 어긋날 수가 없다 — M3 에서
    /// <c>StageController.Tick</c> 의 조기 반환 하나가 "정지" 와 "판정 1회" 를 동시에
    /// 지킨 것과 같은 구조다.
    /// </para>
    /// <para>
    /// <b><see cref="RunState"/> 는 스테이지 총수를 모른다.</b> 총수는 스테이지 목록의
    /// 지식이고, 런 상태가 알게 되는 순간 둘이 결합된다. 아는 곳은 여기 하나다.
    /// </para>
    /// <para>
    /// <b><c>RunConfig</c> 가 아니라 목록을 받는다.</b> SO 를 통해서만 세울 수 있으면
    /// 테스트가 매번 직렬화 계층으로 목록을 밀어 넣어야 하고, <c>Runtime</c> 이
    /// <c>Runtime.Data</c> 의 런 구성까지 알게 된다. 목록을 그대로 받으면 스테이지가
    /// 1개인 런도 배열 하나로 표현된다.
    /// </para>
    /// </summary>
    public sealed class RunProgression
    {
        /// <summary>
        /// 도전 순서대로의 스테이지. <b>생성자에서 복사한다</b> — 바깥 목록이 나중에
        /// 바뀌어도 진행 중인 런의 발밑이 흔들리지 않아야 한다.
        /// </summary>
        private readonly List<StageConfig> _stages = new();

        private readonly RunState _run;

        /// <summary>
        /// 진행할 스테이지를 순서대로 받는다. <c>null</c> 항목은 <b>여기서 걸러낸다</b> —
        /// 인스펙터에서 목록을 늘리면 자연히 생기는 상태이고, 거르는 지점이 둘이면
        /// 어느 쪽이 지켰는지 알 수 없게 된다.
        /// </summary>
        /// <param name="stages">
        /// <c>null</c> 이면 빈 목록으로 본다. 씬을 조금씩 조립하는 동안 터지는 것보다
        /// 빈 런으로 도는 편이 진단하기 쉽다.
        /// </param>
        /// <param name="run">
        /// 진행 상태를 읽고 쓸 런. <c>null</c> 이면 예외다 — 덱과 달리 대신할 기본값이
        /// 없고, 빈 것으로 감싸면 원인이 한참 뒤에 드러난다.
        /// </param>
        public RunProgression(IReadOnlyList<StageConfig> stages, RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));

            if (stages == null)
            {
                return;
            }

            for (var i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null)
                {
                    _stages.Add(stages[i]);
                }
            }
        }

        /// <summary>실제로 도전할 스테이지 수. <c>null</c> 항목은 세지 않는다.</summary>
        public int StageCount => _stages.Count;

        /// <summary>
        /// 마지막 스테이지까지 깼나. <b>유도값이다</b> — 클리어할 때마다 스테이지 번호가
        /// 오르므로, 번호가 총수를 넘은 상태가 곧 런의 끝이다.
        ///
        /// <para>
        /// 목록이 비어 있으면 시작하자마자 참이 된다 (<c>1 &gt; 0</c>). 별도 분기가
        /// 없는 것은 우연이 아니라 <b>의도</b>다 — 도전할 스테이지가 없는 런은 이미
        /// 끝난 런과 같다.
        /// </para>
        /// </summary>
        public bool IsRunComplete => _run.StageNumber > StageCount;

        /// <summary>지금 스테이지를 깨면 갈 곳이 남아 있나.</summary>
        public bool HasNextStage => _run.StageNumber < StageCount;

        /// <summary>
        /// 지금 도전 중인 스테이지의 <b>진행 순서 번호</b>. 1 부터 시작한다.
        ///
        /// <para>
        /// 화면이 "스테이지 2 클리어" 를 쓰는 데 필요하다. <c>StageConfig.StageNumber</c> 를
        /// 쓰지 않는 이유는 그것이 <b>표시용</b>이고 목록 순서와 어긋나도 오류가 아니라고
        /// 정했기 때문이다 — 순서의 진실은 목록이므로 번호도 목록에서 나와야 한다.
        /// </para>
        /// <para>
        /// 런이 끝나면 <see cref="StageCount"/> 를 넘는다. 그 상태가 곧
        /// <see cref="IsRunComplete"/> 이므로, <b>진행 표시에 그대로 쓰면 안 된다.</b>
        /// </para>
        /// </summary>
        public int CurrentStageNumber => _run.StageNumber;

        /// <summary>
        /// 지금 도전 중인 스테이지. 런이 끝났으면 <c>null</c> 이다.
        ///
        /// <para>
        /// 스테이지 번호는 1 부터 시작하므로 인덱스는 하나 뺀 값이다. 번호가
        /// <see cref="StageCount"/> 이하임을 <see cref="IsRunComplete"/> 가 이미
        /// 보장하므로 범위 검사를 따로 두지 않는다.
        /// </para>
        /// </summary>
        public StageConfig CurrentStage => IsRunComplete ? null : _stages[_run.StageNumber - 1];

        /// <summary>
        /// 지금 스테이지를 클리어했다. 다음이 있으면 그리로 옮기고 <c>true</c> 를 돌려준다.
        ///
        /// <para>
        /// <b>마지막 스테이지에서도 번호는 오른다.</b> 갈 곳이 없다고 번호를 그대로 두면
        /// <see cref="IsRunComplete"/> 의 유도식이 런의 끝을 알아보지 못해, 3판을 깨고도
        /// 마지막 스테이지에 머문 상태가 된다.
        /// </para>
        /// <para>
        /// 첫 줄의 조기 반환 하나가 <b>멱등성</b>을 만든다 — 이미 끝난 런에서 다시 불러도
        /// 번호가 계속 오르지 않는다. 별도의 가드 플래그를 두지 않는 이유다.
        /// </para>
        /// </summary>
        /// <returns>다음 스테이지가 있으면 <c>true</c>. 런이 끝났으면 <c>false</c>.</returns>
        public bool AdvanceAfterClear()
        {
            if (IsRunComplete)
            {
                return false;
            }

            _run.AdvanceStage();
            return !IsRunComplete;
        }
    }
}
