namespace SushiDefense.Run
{
    /// <summary>
    /// 런 하나의 기억 — 스테이지가 끝나도 남는 것들.
    ///
    /// <para>
    /// <b>Unity API 를 모른다.</b> ScriptableObject 참조를 담을 뿐이라, 나중에 저장이
    /// 필요해지면 <c>SushiData.Id</c>·<c>CustomerData.Id</c> 로 매핑하는 계층만 얹으면
    /// 된다. 지금 Id 기반으로 짜지 않는 이유는 쓰지 않을 간접층이기 때문이다.
    /// </para>
    /// <para>
    /// <b><c>StageBootstrap.Build()</c> 바깥에 산다.</b> 재시도는 <c>Build()</c> 재호출이고,
    /// 원장·지갑·벨트·순차번호 발급기는 그때 새로 열리지만 이 객체는 살아남는다. 덕분에
    /// 재시도에 새 코드가 거의 없다.
    /// </para>
    /// <para>
    /// <b>매출·영입 재화를 담지 않는다.</b> 둘은 스테이지마다 리셋되므로
    /// (<c>.claude/domain/data-model.md</c> §4), 여기 넣으면 이월이 생긴 것처럼 읽힌다.
    /// </para>
    /// </summary>
    public sealed class RunState
    {
        /// <summary>이 런의 초밥 덱. 벨트가 여기서 스폰 구성을 읽는다.</summary>
        public SushiDeck Sushi { get; }

        /// <summary>이 런의 손님 명부. 배치할 수 있는 손님 종류다.</summary>
        public CustomerDeck Customers { get; }

        /// <summary>지금 도전 중인 스테이지 번호. 1 부터 시작한다.</summary>
        public int StageNumber { get; private set; } = 1;

        /// <summary>지금 스테이지를 몇 번째 시도하고 있나. 처음이 1 이다.</summary>
        public int AttemptNumber { get; private set; } = 1;

        /// <summary>
        /// 보상 추첨에 쓰는 난수원. <b>런 내내 같은 인스턴스</b>라, 시드가 같으면 런 전체가
        /// 같은 보상을 낸다.
        /// </summary>
        public IRandomSource Random { get; }

        /// <summary>
        /// 런을 연다. 덱이 <c>null</c> 이면 빈 것으로 시작한다 — 씬이 아직 덜 조립된
        /// 상태에서 터지는 것보다 빈 덱으로 도는 편이 진단하기 쉽다.
        /// </summary>
        public RunState(SushiDeck sushi, CustomerDeck customers, int seed)
        {
            Sushi = sushi ?? new SushiDeck();
            Customers = customers ?? new CustomerDeck();
            Random = new XorShiftRandomSource(seed);
        }

        /// <summary>클리어했다. 다음 스테이지로 넘어가고 시도 횟수를 1 로 되돌린다.</summary>
        public void AdvanceStage()
        {
            StageNumber++;
            AttemptNumber = 1;
        }

        /// <summary>
        /// 실패했다. <b>같은 스테이지에 머물고</b> 시도 횟수만 올린다 (착수 시 확정).
        /// 덱과 명부는 그대로다 — 실패가 런을 끝내지 않는다.
        /// </summary>
        public void RecordFailedAttempt()
        {
            AttemptNumber++;
        }
    }
}
