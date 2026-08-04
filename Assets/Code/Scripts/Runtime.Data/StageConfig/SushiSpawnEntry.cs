using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 이 스테이지의 덱에 든 초밥 1종. <b>슬롯일 뿐 비중을 담지 않는다.</b>
    ///
    /// <para>
    /// 등장 비율은 <see cref="SushiData.Price"/> 와 <see cref="StageConfig.SparsityExponent"/>
    /// 에서 유도된다 (<c>share ∝ (덱 내 최저가 / 가격) ^ α</c>). 손으로 적는 가중치 필드를
    /// 두지 않는 이유는, 그것이 <b>가격과 어긋나는 두 번째 진실</b>이 되기 때문이다 — 둘이
    /// 갈라지는 순간 어느 쪽이 맞는지 판정할 방법이 없다.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class SushiSpawnEntry
    {
        [SerializeField] private SushiData _sushi;

        /// <summary>이 슬롯에 담긴 초밥 종류.</summary>
        public SushiData Sushi => _sushi;
    }
}
