using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Xunit;
using Yun.Tools.Core.IdGenerator;

namespace Yun.Tools.Core.UnitTests.IdGrenerator
{
    public class SnowflakeIdGeneratorTests
    {
        private static object slock = new object();
        private readonly IIdGenerator _snowflakeIdGenerator;
        public SnowflakeIdGeneratorTests()
        {
            _snowflakeIdGenerator = SnowflakeIdGenerator.Default;
        }

        [Fact]
        public async Task ConcurrentGeneration()
        {
            var total = 100000000;

            var result = new HashSet<long>();
            var g1 = Task.Run(() =>
            {
                for (var i = 0; i < total; i++)
                {
                    lock (slock)
                    {
                        result.Add(_snowflakeIdGenerator.NextId());
                    }
                }
            });
            var g2 = Task.Run(() =>
            {
                for (var i = 0; i < total; i++)
                {
                    lock (slock)
                    {
                        result.Add(_snowflakeIdGenerator.NextId());
                    }
                }
            });

            var tasks = new[] { g1, g2 };
            await Task.WhenAll(tasks);

            Assert.Equal(total * tasks.Length, result.Count);
        }

    }
}
