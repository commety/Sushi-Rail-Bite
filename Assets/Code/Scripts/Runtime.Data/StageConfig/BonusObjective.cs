using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 클리어 외 추가 보상을 주는 목표 하나. M0 은 식별과 표시 문구까지만 정의한다 —
    /// 달성 조건의 표현 방식은 보상 시스템이 서는 M3 에서 정한다.
    /// </summary>
    [Serializable]
    public sealed class BonusObjective
    {
        [SerializeField] private string _id;
        [SerializeField, TextArea] private string _description;

        /// <summary>보상 지급·달성 기록에서 이 목표를 가리키는 안정적 키.</summary>
        public string Id => _id;

        /// <summary>플레이어에게 보여줄 목표 문구.</summary>
        public string Description => _description;
    }
}
