using System.Collections.Generic;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 런 하나가 지나갈 스테이지의 <b>순서</b>. 데모는 3스테이지 한 벌이다.
    ///
    /// <para>
    /// <b>진행 판정을 담지 않는다.</b> 지금 몇 번째인가 · 다음이 있는가 · 런이 끝났는가는
    /// <c>Runtime</c> 의 <c>RunProgression</c> 이 답한다 — <c>Runtime.Data</c> 는 계약이지
    /// 계산이 아니다 (<c>.claude/rules/scriptable-object.md</c> §7 과 같은 이유).
    /// </para>
    /// <para>
    /// <b>스테이지 목록을 코드나 씬 배열에 박지 않기 위해 존재한다.</b> 순서를 바꾸거나
    /// 스테이지를 늘리는 데 코드 수정이 필요해지면 데이터 주도가 깨진 것이다
    /// (<c>CLAUDE.md</c> §3.1).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private List<StageConfig> _stages = new();

        /// <summary>UI 표시용 런 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 도전할 스테이지를 <b>도전할 순서대로</b>. 이 목록이 순서의 진실이다.
        ///
        /// <para>
        /// <c>StageConfig.StageNumber</c> 와 인덱스가 맞는지 <b>대조하지 않는다.</b> 맞아야
        /// 한다는 규칙을 만들면 순서를 바꿀 때마다 애셋을 두 곳 고쳐야 하고, 한쪽만 고친
        /// 상태가 조용히 남는다. 번호는 표시용이다.
        /// </para>
        /// <para>
        /// <c>null</c> 항목도 그대로 담는다. 인스펙터에서 목록을 늘리면 자연히 생기는
        /// 상태이며, 거르는 일은 진행 판정 <b>한 곳</b>이 맡는다.
        /// </para>
        /// </summary>
        public IReadOnlyList<StageConfig> Stages => _stages;

        /// <summary>
        /// 목록에 든 스테이지 수. 진행 판정이 자주 묻는 값이라, 호출부가 그때마다
        /// <see cref="Stages"/> 를 거치지 않게 따로 낸다.
        /// </summary>
        public int StageCount => _stages.Count;
    }
}
