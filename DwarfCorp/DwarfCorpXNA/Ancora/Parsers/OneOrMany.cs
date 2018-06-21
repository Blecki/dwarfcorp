using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class OneOrMany : Parser
    {
        private Parser SubParser;

        public OneOrMany(Parser SubParser)
        {
            this.SubParser = SubParser;
        }

        protected override Parser ImplementClone()
        {
            return new OneOrMany(SubParser);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var r = new AstNode { NodeType = AstNodeType, Location = InputStream };

            while (true)
            {
                var subResult = SubParser.Parse(InputStream);
                if (subResult.ResultType == ResultType.Success)
                {
                    InputStream = subResult.After;
                    if (subResult.Node != null)
                            r.Children.Add(subResult.Node);
                }
                else if (subResult.ResultType == ResultType.HardError) return Error("Child produced hard error", subResult.FailReason);
                else return new ParseResult
                    {
                        ResultType = r.Children.Count > 0 ? ResultType.Success : ResultType.Failure, //Must match at least 1
                        Node = r,
                        After = InputStream
                    };
            }
        }
    }
}
