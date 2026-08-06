using System.Collections.Generic;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 클리어 보상으로 제시할 수 있는 카드의 전체 목록.
    ///
    /// <para>
    /// <b>런의 진행 상태를 담지 않는다.</b> 무엇을 이미 가졌는지는 런타임의 런 상태가 알고,
    /// 미보유 카드를 추리는 계산도 <c>Runtime</c> 쪽 생성기의 몫이다 — 이 SO 는 읽기 전용
    /// 템플릿이다 (<c>CLAUDE.md</c> §3.1).
    /// </para>
    /// <para>
    /// <b>풀이 비어 있는 것은 오류가 아니다.</b> 보상 없는 구성은 유효하며, 제시 개수가 풀보다
    /// 큰 것도 마찬가지다 — 있는 만큼 제시하는 것이 정상 동작이다. 이런 것을
    /// <see cref="OnValidate"/> 에서 막으면 밸런스를 구조 검증에 섞게 된다
    /// (<c>.claude/rules/scriptable-object.md</c> §6).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Reward Catalog", fileName = "RewardCatalog")]
    public sealed class RewardCatalog : ScriptableObject
    {
        /// <summary>
        /// 제시 개수의 구조적 하한. 밸런스 값이 아니라 "0장을 제시하는 보상 화면은 성립하지
        /// 않는다"는 불변 조건이라 코드에 둔다 (<c>StageConfig</c> 의 제한 시간 하한과 같은 성격).
        /// </summary>
        private const int MinimumOfferCount = 1;

        [SerializeField] private List<SushiData> _sushiPool = new();
        [SerializeField] private List<CustomerData> _customerPool = new();

        /// <summary>
        /// 한 번에 제시할 후보 수. <b>기본값 3 은 결정이 아니라 출발점이다</b> — 확정은
        /// 밸런스 애셋을 채우는 시점에 사람이 한다 (<c>CLAUDE.md</c> §7).
        /// </summary>
        [SerializeField, Min(MinimumOfferCount)] private int _offerCount = 3;

        /// <summary>
        /// 보상으로 줄 수 있는 초밥 카드 전체. <b>순서를 유지한다</b> — 후보 추첨이 결정적이려면
        /// 입력 순서가 흔들리지 않아야 한다.
        /// </summary>
        public IReadOnlyList<SushiData> SushiPool => _sushiPool;

        /// <summary>보상으로 영입할 수 있는 손님 전체. 순서를 유지한다.</summary>
        public IReadOnlyList<CustomerData> CustomerPool => _customerPool;

        /// <summary>
        /// 한 번에 제시할 후보 수. 미보유 카드가 이보다 적으면 있는 만큼만 제시된다.
        /// </summary>
        public int OfferCount => _offerCount;

        /// <summary>
        /// 이상값 방어. <c>[Min]</c> 은 인스펙터 입력만 막고 직렬화된 이상값은 통과시키므로
        /// 여기서 한 번 더 조인다. <b>풀의 내용은 보지 않는다</b> — 빈 풀도 <c>null</c> 항목도
        /// 유효한 상태다.
        /// </summary>
        internal void OnValidate()
        {
            _offerCount = Mathf.Max(MinimumOfferCount, _offerCount);
        }
    }
}
