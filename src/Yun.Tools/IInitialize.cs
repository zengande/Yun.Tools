using System;
using System.Collections.Generic;
using System.Text;

namespace Yun.Tools.Core
{
    public interface IInitialize
    {
        void Initialize(IDictionary<string, object> parameters);
    }
}
