using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class NoneOrMany : Parser
    {
        private Parser SubParser;

        public NoneOrMany(Parser SubParser)
        {
            this.SubParser = SubParser;
        }

        protected override Parser ImplementClone()
        {
            return new NoneOrMany(SubParser);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var r = new AstNode { NodeType = AstNodeType, Location = InputStream };

            while (true)
            {
                var subResult = SubParser.Parse(InputStream);
                if (subResult.ResultType == ResultType.HardError) return Error("Child produced hard error", subResult.FailReason);
                else if (subResult.ResultType == ResultType.Success)
                {
                    InputStream = subResult.After;
                    if (subResult.Node != null)
                        r.Children.Add(subResult.Node);
                }
                else
                    return new ParseResult
                    {
                        ResultType = ResultType.Success, //Acceptable to match none.
                        Node = r,
                        After = InputStream
                    };
            }
        }
    }
}
