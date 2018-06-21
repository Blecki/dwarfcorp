using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Maybe : Parser
    {
        private Parser SubParser;

        public Maybe(Parser SubParser)
        {
            this.SubParser = SubParser;
        }

        protected override Parser ImplementClone()
        {
            return new Maybe(SubParser);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var subResult = SubParser.Parse(InputStream);
            if (subResult.ResultType == ResultType.Success) return WrapChild(subResult);
            else if (subResult.ResultType == ResultType.HardError) return Error("Maybe child produced hard error", subResult.FailReason);
            else return new ParseResult
            {
                ResultType = ResultType.Success,
                Node = new AstNode { NodeType = AstNodeType, Location = InputStream },
                After = InputStream
            };
        }
    }
}
