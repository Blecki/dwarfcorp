using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public abstract class Parser
    {
        internal String AstNodeType = "UNNAMED";
        internal Func<AstNode, AstNode> AstMutator = (a) => a;

        #region Failure

        private ParseResult Fail(String Message, ResultType ResultType)
        {
            var failReason = new Failure(this, Message);

            return new ParseResult
            {
                ResultType = ResultType,
                FailReason = failReason
            };
        }

        private ParseResult Fail(String Message, ResultType ResultType, Failure SubFailure)
        {
            var failReason = new CompoundFailure();
            failReason.FailedAt = this;
            failReason.Message = Message;
            failReason.AddFailure(SubFailure);

            return new ParseResult
            {
                ResultType = ResultType,
                FailReason = failReason
            };
        }

        public static string CollapseEscapeSequences(string value)
        {
            var r = "";
            var itr = new StringIterator(value);

            bool escaping = false;
            while (!itr.AtEnd)
            {
                if (escaping)
                {
                    if (itr.Next == 'n')
                        r += '\n';
                    else if (itr.Next == 'r')
                        r += '\r';
                    else if (itr.Next == 't')
                        r += '\t';
                    else
                        r += itr.Next;
                    itr = itr.Advance();
                    escaping = false;
                }
                else if (itr.Next == '\\')
                {
                    escaping = true;
                    itr = itr.Advance();
                }
                else
                {
                    r += itr.Next;
                    itr = itr.Advance();
                }
            }

            return r;
        }

        protected ParseResult Fail(String Message) { return Fail(Message, ResultType.Failure); }
        protected ParseResult Fail(String Message, Failure SubFailure) { return Fail(Message, ResultType.Failure, SubFailure); }
        protected ParseResult Error(String Message) { return Fail(Message, ResultType.HardError); }
        protected ParseResult Error(String Message, Failure SubFailure) { return Fail(Message, ResultType.HardError, SubFailure); }

        #endregion

        protected ParseResult WrapChild(ParseResult ChildResult)
        {
            return new ParseResult
            {
                ResultType = ResultType.Success,
                After = ChildResult.After,
                Node = AstNode.WrapChild(AstNodeType, ChildResult.Node)
            };
        }

        public Parser Ast(String NodeType)
        {
            var r = this.Clone();
            r.AstNodeType = NodeType;
            return r;
        }

        public Parser WithMutator(Func<AstNode, AstNode> Mutator)
        {
            var r = this.Clone();
            r.AstMutator = (a) => Mutator(AstMutator(a));
            return r;
        }

        public Parser WithoutMutators()
        {
            var r = this.Clone();
            r.AstMutator = (a) => a;
            return r;
        }

        protected Parser Clone()
        {
            var r = this.ImplementClone();
            r.AstNodeType = AstNodeType;
            r.AstMutator = AstMutator;
            return r;
        }

        public ParseResult Parse(StringIterator InputStream)
        {
            var r = ImplementParse(InputStream);
            if (r.ResultType == ResultType.Success)
                r.Node = AstMutator(r.Node);
            return r;
        }

        protected abstract ParseResult ImplementParse(StringIterator InputStream);
        protected abstract Parser ImplementClone();

        #region Construction Ops

        public static Parsers.Sequence operator +(Parser LHS, Parser RHS)
        {
            return new Parsers.Sequence(LHS, RHS);
        }

        public static Parsers.Sequence operator +(Parser LHS, char RHS)
        {
            return new Parsers.Sequence(LHS, new Parsers.Character(RHS));
        }

        public static Parsers.Sequence operator +(char LHS, Parser RHS)
        {
            return new Parsers.Sequence(new Parsers.Character(LHS), RHS);
        }

        public static Parsers.Sequence operator +(Parser LHS, String RHS)
        {
            return new Parsers.Sequence(LHS, new Parsers.KeyWord(RHS));
        }

        public static Parsers.Alternative operator |(Parser LHS, Parser RHS)
        {
            return new Parsers.Alternative(LHS, RHS);
        }

        public static Parsers.Alternative operator |(Parser LHS, char RHS)
        {
            return new Parsers.Alternative(LHS, new Parsers.Character(RHS));
        }

        #endregion

    }
}
