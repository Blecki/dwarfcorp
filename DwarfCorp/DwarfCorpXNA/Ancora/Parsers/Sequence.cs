using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Sequence : Parser
    {
        private List<Parser> SubParsers;

        public Sequence(params Parser[] SubParsers)
        {
            this.SubParsers = new List<Parser>(SubParsers);
        }

        public static Sequence operator +(Sequence LHS, Parser RHS)
        {
            var r = LHS.Clone() as Sequence;
            r.SubParsers.Add(RHS);
            return r;
        }

        public static Sequence operator +(Sequence LHS, char RHS)
        {
            var r = LHS.Clone() as Sequence;
            r.SubParsers.Add(new Character(RHS));
            return r;
        }

        public static Sequence operator +(Sequence LHS, String RHS)
        {
            var r = LHS.Clone() as Sequence;
            r.SubParsers.Add(new KeyWord(RHS));
            return r;
        }

        protected override Parser ImplementClone()
        {
            return new Sequence(SubParsers.ToArray());
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var r = new AstNode { NodeType = AstNodeType, Location = InputStream };

            foreach (var sub in SubParsers)
            {
                var subResult = sub.Parse(InputStream);

                if (subResult.ResultType == ResultType.HardError)
                    return Error("Child produced hard error", subResult.FailReason);
                else if (subResult.ResultType == ResultType.Failure)
                    return Fail("Child failed", subResult.FailReason);

                InputStream = subResult.After;

                if (subResult.Node != null)
                    r.Children.Add(subResult.Node);
            }

            return new ParseResult
            {
                ResultType = ResultType.Success,
                After = InputStream,
                Node = r
            };
        }
    }
}
