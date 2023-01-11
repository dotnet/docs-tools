using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoMan
{
    public interface IRunnerItem
    {
        Task Run(State state);
    }
}
