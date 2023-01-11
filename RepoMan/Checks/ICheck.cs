using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoMan.Checks
{
    public interface ICheck
    {
        Task<bool> Run(State state);
    }
}
