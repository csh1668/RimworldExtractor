using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorInternal.Exceptions
{
    public class XlsxHeaderReadingException : XlsxReadingException
    {
        protected static readonly string FormatString = "엑셀 파일의 헤더를 읽는 중 에러가 발생했습니다: {0}";
        public XlsxHeaderReadingException(string message) : base(string.Format(FormatString, message))
        {
        }
    }
}
