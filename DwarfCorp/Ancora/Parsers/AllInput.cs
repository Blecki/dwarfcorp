using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class AllInput : Parser
    {
        protected override Parser ImplementClone()
        {
            return new AllInput();
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            if (!InputStream.AtEnd)
                Fail("Did not consume all input");

            return new ParseResult
            {
                ResultType = ResultType.Success,
                After = InputStream,
                Node = new AstNode { NodeType = AstNodeType, Location = InputStream }
            };
        }
    }
}
