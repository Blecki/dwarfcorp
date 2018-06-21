using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public class Grammar
    {
        public Parser Root = null;

        public ParseResult ParseString(String S)
        {
            var iter = new StringIterator(S);
            return Root.Parse(iter);
        }

        #region Parser Factory Functions

        public Parsers.Sequence Sequence(params Parser[] SubParsers)
        {
            var r = new Parsers.Sequence(SubParsers);
            return r;
        }

        public Parsers.Alternative Alternative(params Parser[] SubParsers)
        {
            var r = new Parsers.Alternative(SubParsers);
            return r;
        }

        public Parsers.Character Character(Char C)
        {
            var r = new Parsers.Character(C);
            return r;
        }

        public Parsers.Expression Expression(Parser TermParser, Parser OperatorParser, OperatorTable OperatorTable)
        {
            var r = new Parsers.Expression(TermParser, OperatorParser, OperatorTable);
            return r;
        }

        public Parsers.Identifier Identifier(Func<Char, bool> IsLegalStartCharacter, Func<Char, bool> IsLegalCharacter)
        {
            var r = new Parsers.Identifier(IsLegalStartCharacter, IsLegalCharacter);
            return r;
        }

        public Parsers.KeyWord Keyword(String Word)
        {
            var r = new Parsers.KeyWord(Word);
            return r;
        }

        public Parsers.LateBound LateBound()
        {
            var r = new Parsers.LateBound();
            return r;
        }

        public Parsers.Maybe Maybe(Parser SubParser)
        {
            var r = new Parsers.Maybe(SubParser);
            return r;
        }

        public Parsers.NoneOrMany NoneOrMany(Parser SubParser)
        {
            var r = new Parsers.NoneOrMany(SubParser);
            return r;
        }

        public Parsers.OneOrMany OneOrMany(Parser SubParser)
        {
            var r = new Parsers.OneOrMany(SubParser);
            return r;
        }

        public Parsers.Operator Operator(OperatorTable OperatorTable)
        {
            var r = new Parsers.Operator(OperatorTable);
            return r;
        }

        public Parsers.Token Token(Func<Char, bool> IsLegalCharacter)
        {
            var r = new Parsers.Token(IsLegalCharacter);
            return r;
        }

        public Parsers.Debug Debug(Action<StringIterator> CallOnParse)
        {
            var r = new Parsers.Debug(CallOnParse);
            return r;
        }

        public Parsers.Debug Debug(String Message)
        {
            var r = new Parsers.Debug(p => Console.WriteLine(Message));
            return r;
        }

        public Parsers.HardError HardError(Parser SubParser)
        {
            var r = new Parsers.HardError(SubParser);
            return r;
        }

        public Parser DelimitedList(Parser TermParser, Parser SeperatorParser)
        {
            var r = TermParser + NoneOrMany(SeperatorParser + TermParser);
            return r;
        }

        public Parser DelimitedList(Parser TermParser, char SeperatorChar)
        {
            var r = TermParser + NoneOrMany(Character(SeperatorChar) + TermParser);
            return r;
        }

        #endregion

        #region Mutator Factory Functions

        public Func<AstNode, AstNode> PassChild(int Index)
        {
            return a =>
                {
                    if (a.Children.Count <= Index) return null;
                    return a.Children[Index];
                };
        }

        public Func<AstNode, AstNode> Collapse()
        {
            return PassChild(0);
        }

        public Func<AstNode, AstNode> ChildValue(int Index)
        {
            return a =>
                {
                    if (a.Children.Count <= Index) return a;
                    a.Value = a.Children[Index].Value;
                    return a;
                };
        }

        public Func<AstNode, AstNode> DiscardChildren()
        {
            return a =>
                {
                    a.Children.Clear();
                    return a;
                };
        }

        public Func<AstNode, AstNode> DiscardChild(int Index)
        {
            return a =>
                {
                    if (Index < a.Children.Count) a.Children.RemoveAt(Index);
                    return a;
                };
        }

        public Func<AstNode, AstNode> Identity()
        {
            return a => a;
        }

        public Func<AstNode, AstNode> Discard()
        {
            return a => null;
        }

        #endregion
    }
}
