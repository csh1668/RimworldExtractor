using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Compats
{
    public class CompatPriorityAttribute : Attribute
    {
        public int Priority { get; private set; }

        public CompatPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        public CompatPriorityAttribute() : this(100) {}
    }
}
