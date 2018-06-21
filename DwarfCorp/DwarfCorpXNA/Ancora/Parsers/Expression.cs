using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Expression : Parser
    {
        private Parser TermParser;
        private Parser OperatorParser;
        private OperatorTable OperatorTable;

        public Expression(Parser TermParser, Parser OperatorParser, OperatorTable OperatorTable)
        {
            this.TermParser = TermParser;
            this.OperatorParser = OperatorParser;
            this.OperatorTable = OperatorTable;
        }

        protected override Parser ImplementClone()
        {
            return new Expression(TermParser, OperatorParser, OperatorTable);
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            var firstLhs = TermParser.Parse(InputStream);
            if (firstLhs.ResultType == ResultType.HardError) return firstLhs;
            if (firstLhs.ResultType != ResultType.Success || firstLhs.Node == null) return Fail("LHS parse failed, or did not produce an AST node.");

            return ParseExpression(firstLhs.Node, firstLhs.After, 0);
        }

        private ParseResult ParseExpression(AstNode LHS, StringIterator InputStream, int MinimumPrecedence)
        {
            var originalStream = InputStream;

            while (true)
            {
                var opParser = OperatorParser.Parse(InputStream);
                if (opParser.ResultType == ResultType.HardError) return Error("Operator parser produced hard error", opParser.FailReason);
                else if (opParser.ResultType == ResultType.Failure)
                    return new ParseResult { Node = LHS, ResultType = ResultType.Success, After = InputStream };

                var precedence = OperatorTable.FindPrecedence(opParser.Node.Value.ToString());
                if (precedence < MinimumPrecedence) return new ParseResult { Node = LHS, ResultType = ResultType.Success, After = InputStream };

                var op = opParser.Node.Value.ToString();
                InputStream = opParser.After;
                var rhsParse = TermParser.Parse(InputStream);

                if (rhsParse.ResultType == ResultType.HardError) return rhsParse;
                else if (rhsParse.ResultType == ResultType.Failure) return rhsParse;

                while (true)
                {
                    opParser = OperatorParser.Parse(rhsParse.After);
                    if (opParser.ResultType == ResultType.HardError) return opParser;
                    else if (opParser.ResultType == ResultType.Success)
                    {
                        var nextPrecedence = OperatorTable.FindPrecedence(opParser.Node.Value.ToString());
                        if (nextPrecedence > precedence)
                            rhsParse = ParseExpression(rhsParse.Node, rhsParse.After, nextPrecedence);
                        else
                            break;
                    }
                    else
                        break;
                }

                InputStream = rhsParse.After;

                var r = new AstNode
                {
                    NodeType = "BINARYOP",
                    Value = op,
                    Location = originalStream,
                };

                r.Children.Add(LHS);
                r.Children.Add(rhsParse.Node);

                LHS = r;
            }
        }
    }
}
