using System.Collections.Generic;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 이 게임에 <b>존재하는</b> 카드 전체. 백과사전과 덱 표시가 여기서 목록을 읽는다.
    ///
    /// <para>
    /// <see cref="RewardCatalog"/> 와 다른 질문에 답한다 — 저쪽은 <i>보상으로 줄 수 있는</i>
    /// 카드이고 여기는 <i>존재하는</i> 카드다. 시작 덱의 초밥은 보상으로 나오지 않으므로
    /// 두 목록은 같지 않으며, 한 애셋에 몰면 "보상 풀에서 뺐더니 사전에서도 사라졌다" 가 난다.
    /// </para>
    /// <para>
    /// <b>런의 진행 상태를 담지 않는다.</b> 무엇을 이미 가졌는지는 런 상태가 알고, 사전은
    /// 소유 여부와 무관하게 전부를 보여 준다 — 미획득을 가리면 사전이 아니다.
    /// </para>
    /// <para>
    /// <b>목록이 비어 있는 것은 오류가 아니다.</b> 빈 슬롯이 섞이는 것도 마찬가지이며,
    /// 거르는 책임은 읽는 쪽에 있다 (<see cref="RewardCatalog"/> 와 같은 판단이다).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Card Catalog", fileName = "CardCatalog")]
    public sealed class CardCatalog : ScriptableObject
    {
        [SerializeField] private List<SushiData> _allSushi = new();
        [SerializeField] private List<CustomerData> _allCustomers = new();

        /// <summary>
        /// 이 게임의 초밥 전부. <b>순서를 유지한다</b> — 사전이 이 순서대로 그려지므로,
        /// 흔들리면 같은 화면이 열 때마다 달라 보인다.
        /// </summary>
        public IReadOnlyList<SushiData> AllSushi => _allSushi;

        /// <summary>이 게임의 손님 전부. 순서를 유지한다.</summary>
        public IReadOnlyList<CustomerData> AllCustomers => _allCustomers;
    }
}
