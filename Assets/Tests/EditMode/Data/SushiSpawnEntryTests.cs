using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// <see cref="SushiSpawnEntry"/> 는 덱 슬롯일 뿐이라는 것을 고정한다.
    ///
    /// <para>
    /// 여기서 검증할 것이 적은 것이 정상이다 — 이 타입에 필드가 늘어나면 등장 비율의
    /// 근거가 가격 말고 하나 더 생겼다는 뜻이고, 그 순간 둘이 어긋날 수 있게 된다.
    /// </para>
    /// </summary>
    public sealed class SushiSpawnEntryTests
    {
        private readonly List<Object> _disposables = new();
        private StageConfig _config;

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();

            if (_config != null)
            {
                Object.DestroyImmediate(_config);
                _config = null;
            }
        }

        [Test]
        public void Sushi_AssignedEntry_ExposesReference()
        {
            var sushi = StageConfigBuilder.CreateSushi(_disposables);

            _config = new StageConfigBuilder().WithSpawnEntry(sushi).Build();

            Assert.AreSame(sushi, _config.SpawnTable[0].Sushi);
        }

        [Test]
        public void SpawnTable_MultipleEntries_PreservesDeckOrder()
        {
            // 덱 순서는 배출 동률을 끝내는 기준이다 (step-03 의 credit 누적).
            // 순서가 흔들리면 스폰 결정성이 같이 흔들린다.
            var first = StageConfigBuilder.CreateSushi(_disposables);
            var second = StageConfigBuilder.CreateSushi(_disposables);

            _config = new StageConfigBuilder()
                .WithSpawnEntry(first)
                .WithSpawnEntry(second)
                .Build();

            Assert.AreSame(first, _config.SpawnTable[0].Sushi);
            Assert.AreSame(second, _config.SpawnTable[1].Sushi);
        }

        [Test]
        public void Sushi_EntryWithoutAssignment_IsNull()
        {
            _config = new StageConfigBuilder().WithSpawnEntry(null).Build();

            Assert.IsNull(_config.SpawnTable[0].Sushi);
        }
    }
}
