using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Identifier : Parser
    {
        private Func<char, bool> IsLegalCharacter;
        private Func<char, bool> IsLegalStartCharacter;

        public Identifier(Func<char,bool> IsLegalStartCharacter, Func<char,bool> IsLegalCharacter)
        {
            this.IsLegalStartCharacter = IsLegalStartCharacter;
            this.IsLegalCharacter = IsLegalCharacter;
        }

        protected override Parser ImplementClone()
        {
            return new Identifier(IsLegalStartCharacter, IsLegalCharacter);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var location = InputStream;

            if (InputStream.AtEnd || !IsLegalStartCharacter(InputStream.Next)) return Fail("Unexpected end of stream");

            var text = "";
            while (!InputStream.AtEnd && IsLegalCharacter(InputStream.Next))
            {
                text += InputStream.Next;
                InputStream = InputStream.Advance();
            }

            return new ParseResult
            {
                ResultType = Ancora.ResultType.Success,
                Node = new AstNode { NodeType =  AstNodeType, Value = text, Location = location },
                After = InputStream
            };
        }
    }
}
