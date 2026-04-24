using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Exceptions
{
    public class XlsxReadingException : Exception
    {
        public XlsxReadingException(string message) : base(message)
        {

        }
    }
}
