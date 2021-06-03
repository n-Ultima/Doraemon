using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Common.CommandHelp
{
    /// <summary>
    /// Hides the module or command from display
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HiddenFromHelpAttribute : Attribute
    {

    }
}
