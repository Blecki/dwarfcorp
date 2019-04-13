using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class KeyWord : Parser
    {
        private String Word;

        public KeyWord(String Word)
        {
            this.Word = Word;
        }

        protected override Parser ImplementClone()
        {
            return new KeyWord(Word);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var text = InputStream.Peek(Word.Length);
            if (text == Word)
                return new ParseResult
                {
                    ResultType = Ancora.ResultType.Success,
                    Node = new AstNode { NodeType = AstNodeType, Value = Word, Location = InputStream },
                    After = InputStream.Advance(Word.Length)
                };
            else
                return Fail("KeyWord not found");
        }
    }
}
