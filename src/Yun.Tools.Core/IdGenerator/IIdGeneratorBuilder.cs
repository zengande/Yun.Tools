using System;
using System.Collections.Generic;
using System.Text;

namespace Yun.Tools.Core.IdGenerator
{
    public interface IIdGeneratorBuilder
    {
        IIdGenerator Build(string type, IDictionary<string, object> parameters);
    }
}
