using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

namespace DwarfCorp
{
    public class YarnCommandGrammar : Ancora.Grammar
    {
        public YarnCommandGrammar()
        {
            var delimeters = "&%^|<>=,/-+*[]{}() \t\r\n.:;\"@";
            var digits = "0123456789";

            var ws = Maybe(Token(c => " \r\n\t".Contains(c))).Ast("WS")
                .WithMutator(Discard()); // Throw away the ast node so it doesn't appear in the final syntax tree.
            var string_literal = new Ancora.Parsers.StringLiteral('"').Ast("STRING");
            var integerLiteral = Token(c => digits.Contains(c)).Ast("INTEGER");

            var numberLiteral = (Maybe((Character('-') | Character('+')).WithMutator(Collapse()))
                + integerLiteral 
                + Maybe((Character('.') + integerLiteral).WithMutator(PassChild(1)))
                ).WithMutator(node_in =>
            {
                var front = node_in.Children[1].Value.ToString();
                var back = node_in.Children[2].Children.Count > 0 ? node_in.Children[2].Children[0].Value.ToString() : "0";
                var floatValue = float.Parse(front + "." + back);

                if (node_in.Children[0].Children.Count > 0 && node_in.Children[0].Children[0].Value.ToString() == "-")
                    floatValue *= -1.0f;

                return new Ancora.AstNode
                {
                    Value = floatValue,
                    Location = node_in.Location,
                    NodeType = "NUMBER"
                };
            });

            var token = Token(c => !delimeters.Contains(c)).Ast("TOKEN");
            var term = (string_literal | numberLiteral | token).WithMutator(Collapse());
            var command = (OneOrMany((ws + term + ws).WithMutator(Collapse())) + new Ancora.Parsers.AllInput().WithMutator(Discard())).WithMutator(Collapse());

            this.Root = command;
        }
    }
}
