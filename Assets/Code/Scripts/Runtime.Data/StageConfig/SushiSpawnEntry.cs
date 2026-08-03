using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 스테이지에서 어떤 초밥이 어떤 비중으로 벨트에 오르는지 한 줄.
    /// <b>스폰 타이밍은 여기서 정하지 않는다</b> — 한 번에 전부인지 시간에 걸쳐인지는 M1 의 미결이다
    /// (<c>docs/plan/README.md</c> Q2).
    /// </summary>
    [Serializable]
    public sealed class SushiSpawnEntry
    {
        [SerializeField] private SushiData _sushi;
        [SerializeField, Min(0)] private int _weight;

        /// <summary>벨트에 올릴 초밥 종류.</summary>
        public SushiData Sushi => _sushi;

        /// <summary>다른 항목 대비 상대 비중. 0 이면 이 스테이지에 나오지 않는다.</summary>
        public int Weight => _weight;
    }
}
