using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Satrabel.AIChat.Tools
{
    public interface IAIChatTool
    {
        string Name { get; }

        string Description { get; }

        bool ReadOnly { get; }

        string RulesFolder { get; }

        MethodInfo Function { get; }
    }
}
