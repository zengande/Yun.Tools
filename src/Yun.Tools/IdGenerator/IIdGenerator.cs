using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Yun.Tools.Core.IdGenerator
{
    public interface IIdGenerator : IInitialize
    {
        long NextId();
    }
}
