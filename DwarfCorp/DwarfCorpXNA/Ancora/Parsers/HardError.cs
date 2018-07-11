using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class HardError : Parser
    {
        private Parser SubParser;

        public HardError(Parser SubParser)
        {
            this.SubParser = SubParser;
        }

        protected override Parser ImplementClone()
        {
            return new HardError(SubParser);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var subResult = SubParser.Parse(InputStream);
            if (subResult.ResultType == ResultType.Success) return subResult;
            else
            {
                subResult.ResultType = ResultType.HardError;
                return subResult;
            }
        }
    }
}
