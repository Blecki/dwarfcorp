using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class StringLiteral : Parser
    {
        private char Quote;

        public StringLiteral(char Quote)
        {
            this.Quote = Quote;
        }

        protected override Parser ImplementClone()
        {
            return new StringLiteral(Quote);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var text = "";
            var location = InputStream;

            if (InputStream.AtEnd || InputStream.Next != Quote)
                return Fail("Not a string");

            InputStream = InputStream.Advance();

            while (!InputStream.AtEnd && InputStream.Next != Quote)
            {
                if (InputStream.Next == '\\')
                {
                    text += InputStream.Peek(2);
                    InputStream = InputStream.Advance(2);
                    continue;
                }

                text += InputStream.Next;
                InputStream = InputStream.Advance();
            }

            if (InputStream.AtEnd || InputStream.Next != Quote)
                return Fail("Unexpected end of string literal");

            InputStream = InputStream.Advance();

            return new ParseResult
            {
                ResultType = ResultType.Success,
                Node = new AstNode { NodeType = AstNodeType, Value = text, Location = location },
                After = InputStream
            };
        }
    }
}
