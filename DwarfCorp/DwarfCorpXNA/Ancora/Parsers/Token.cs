using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Token : Parser
    {
        private Func<char, bool> IsLegalCharacter;

        public Token(Func<char,bool> IsLegalCharacter)
        {
            this.IsLegalCharacter = IsLegalCharacter;
        }

        protected override Parser ImplementClone()
        {
            return new Token(IsLegalCharacter);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var text = "";
            var location = InputStream;

            while (!InputStream.AtEnd && IsLegalCharacter(InputStream.Next))
            {
                text += InputStream.Next;
                InputStream = InputStream.Advance();
            }

            return new ParseResult
            {
                ResultType = String.IsNullOrEmpty(text) ? ResultType.Failure : ResultType.Success,
                Node = new AstNode { NodeType = AstNodeType, Value = text, Location = location },
                After = InputStream
            };
        }
    }
}
