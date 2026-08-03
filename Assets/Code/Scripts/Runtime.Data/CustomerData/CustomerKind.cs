namespace SushiDefense.Data
{
    /// <summary>
    /// 손님 유형. <b>로직 분기용이 아니다</b> — 세 유형은 같은 코드 경로를 타고 값만 다르다.
    /// 표시·정렬·필터에만 쓴다 (<c>.claude/domain/data-model.md</c> §2).
    /// </summary>
    public enum CustomerKind
    {
        /// <summary>기본 손님.</summary>
        Normal = 0,

        /// <summary>소식 — 적게 먹는 대신 비싼 쪽을 노리는 값 구성.</summary>
        SmallEater = 1,

        /// <summary>먹보 — 많이 먹는 대신 저가를 노리는 값 구성.</summary>
        BigEater = 2
    }
}
