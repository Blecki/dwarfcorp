using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public class OperatorTable
    {
        private Dictionary<String, int> Precedence = new Dictionary<string, int>();

        public void AddOperator(String Operator, int Precedence)
        {
            if (this.Precedence.ContainsKey(Operator)) this.Precedence[Operator] = Precedence;
            else this.Precedence.Add(Operator, Precedence);
        }

        public int FindPrecedence(String Operator)
        {
            if (this.Precedence.ContainsKey(Operator)) return this.Precedence[Operator];
            return 0;
        }

        public int PossibleMatches(String Operator)
        {
            return Precedence.Count(p => p.Key.StartsWith(Operator));
        }

        public int ExactMatches(String Operator)
        {
            return Precedence.Count(p => p.Key == Operator);
        }
    }
}
