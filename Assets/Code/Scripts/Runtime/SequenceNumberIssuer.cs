namespace SushiDefense
{
    /// <summary>
    /// 순차번호 발급기. 초밥은 스폰 순서로, 손님은 배치 순서로 번호를 받는다.
    ///
    /// <para>
    /// 이 프로젝트의 배정에는 난수가 없다 — 타겟팅 우선순위가 같을 때 승자를 정하는 것은
    /// 오직 이 번호다 (<c>CLAUDE.md</c> §1.1-3b). 그래서 같은 판 상태는 항상 같은 결과를 낸다.
    /// </para>
    /// <para>
    /// 정적 카운터가 아니라 인스턴스인 이유: 번호의 범위를 스테이지마다 리셋할지 런 전체로
    /// 이어갈지가 아직 미결이라(<c>docs/plan/README.md</c> Q8), 소유자가 수명을 정하게 남겨 둔다.
    /// 어느 쪽으로 정해지든 이 클래스는 그대로다.
    /// </para>
    /// </summary>
    public sealed class SequenceNumberIssuer
    {
        private int _next;

        /// <summary>다음 번호를 발급한다. 0 부터 시작해 1씩 오른다.</summary>
        public int Next()
        {
            return _next++;
        }

        /// <summary>발급을 처음으로 되돌린다.</summary>
        public void Reset()
        {
            _next = 0;
        }
    }
}
