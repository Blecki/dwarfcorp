using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Debug : Parser
    {
        private Action<StringIterator> CallOnParse;

        public Debug(Action<StringIterator> CallOnParse)
        {
            this.CallOnParse = CallOnParse;
        }

        protected override Parser ImplementClone()
        {
            return new Debug(CallOnParse);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            CallOnParse(InputStream);
            return new ParseResult { ResultType = ResultType.Success, After = InputStream };
        }
    }
}
