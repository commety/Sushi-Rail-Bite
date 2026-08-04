using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Belt
{
    /// <summary>
    /// <see cref="StageConfig.SpawnTable"/> 을 결정적으로 소화한다.
    ///
    /// <para>
    /// <b>난수를 쓰지 않는다.</b> M1 의 완료 판정이 "순차번호가 낮은 초밥부터 집는다" 라서
    /// 스폰 순서가 흔들리면 그 검증이 같이 흔들린다. 같은 스테이지는 항상 같은 순서로 나온다
    /// (작업서 D4).
    /// </para>
    /// </summary>
    public sealed class SpawnSequence
    {
        private readonly List<SushiData> _order = new();
        private int _cursor;

        /// <summary>스폰할 초밥이 하나도 없다. 벨트는 아무것도 올리지 않는다.</summary>
        public bool IsEmpty => _order.Count == 0;

        /// <summary>
        /// 가중치만큼 반복 배치한 순서를 만든다. 가중치 0 이나 초밥이 비어 있는 항목은 빠진다.
        /// </summary>
        public SpawnSequence(IReadOnlyList<SushiSpawnEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            // 순서를 미리 펼쳐 둔다. Next() 가 매번 가중치를 다시 계산하면
            // 스폰 경로에 연산이 쌓이고, 펼친 목록은 생성자에서 한 번만 잡힌다.
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.Sushi == null)
                {
                    continue;
                }

                for (var repeat = 0; repeat < entry.Weight; repeat++)
                {
                    _order.Add(entry.Sushi);
                }
            }
        }

        /// <summary>다음에 올릴 초밥. 끝에 닿으면 처음으로 돌아간다.</summary>
        public SushiData Next()
        {
            if (IsEmpty)
            {
                return null;
            }

            var sushi = _order[_cursor];
            _cursor = (_cursor + 1) % _order.Count;
            return sushi;
        }

        /// <summary>처음부터 다시 소화한다.</summary>
        public void Reset()
        {
            _cursor = 0;
        }
    }
}
