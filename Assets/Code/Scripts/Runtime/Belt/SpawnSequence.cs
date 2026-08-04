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
        /// 덱 순서를 그대로 배출 순서로 삼는다. 초밥이 비어 있는 항목은 빠진다.
        ///
        /// <para>
        /// <b>임시 구현이다.</b> 가격에서 유도한 share 로 배출 빈도를 정하는 credit 누적 방식이
        /// 본래 설계이며 step-03 에서 들어온다. 여기서는 가중치 필드가 사라진 자리를 메우기
        /// 위해 항목당 1회 균등 배출만 한다 (작업서 step-01).
        /// </para>
        /// </summary>
        public SpawnSequence(IReadOnlyList<SushiSpawnEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            // 순서를 미리 펼쳐 둔다. Next() 안에서 목록을 다시 만들면 스폰 경로에
            // 할당이 쌓인다 — 배포 타깃이 WebGL 이라 그대로 히칭이 된다.
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.Sushi == null)
                {
                    continue;
                }

                _order.Add(entry.Sushi);
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
