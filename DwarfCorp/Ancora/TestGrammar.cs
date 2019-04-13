using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

namespace Ancora
{
    public class TestGrammar : Grammar
    {
        public TestGrammar()
        {
            var ws = Maybe(Token(c => " \r\n\t".Contains(c))).Ast("WS")
                .WithMutator(Discard()); // Throw away the ast node so it doesn't appear in the final syntax tree.

            var delimeters = "&%^|<>=,/-+*[]{}() \t\r\n.:;\"@";
            var digits = "0123456789";
            var id = Identifier(c => !digits.Contains(c) && !delimeters.Contains(c), c => !delimeters.Contains(c)).Ast
                ("IDENTIFIER");
            var numberLiteral = Token(c => digits.Contains(c)).Ast("NUMBERLIT");

            #region Setup Operator Table

            var opTable = new OperatorTable();

            opTable.AddOperator(".", 5);
            opTable.AddOperator("@", 5);

            opTable.AddOperator("&&", 1);
            opTable.AddOperator("||", 1);
            opTable.AddOperator("==", 1);
            opTable.AddOperator("!=", 1);
            opTable.AddOperator(">", 1);
            opTable.AddOperator("<", 1);
            opTable.AddOperator("<=", 1);
            opTable.AddOperator(">=", 1);

            opTable.AddOperator("+", 2);
            opTable.AddOperator("-", 2);

            opTable.AddOperator("*", 3);
            opTable.AddOperator("/", 3);
            opTable.AddOperator("%", 3);

            opTable.AddOperator("&", 4);
            opTable.AddOperator("|", 4);
            opTable.AddOperator("^", 4);

            #endregion

            var term = (id | numberLiteral)
                .WithMutator(Collapse()); // Discard the 'alteration' node.

            var typeName = LateBound();
            var typeParent = (ws + ':' + ws + typeName).WithMutator(PassChild(1));
            typeName.SetSubParser((id + Maybe(typeParent).WithMutator(Collapse())).Ast("TYPENAME").WithMutator(ChildValue(0)).WithMutator(DiscardChild(0)));

            var expressionTerm = (ws + term + ws).WithMutator(Collapse()); // Discard the 'sequence' node.

            this.Root = typeName;
        }
    }
}
