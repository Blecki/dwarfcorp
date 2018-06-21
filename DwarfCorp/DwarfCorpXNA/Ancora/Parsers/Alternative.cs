using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora.Parsers
{
    public class Alternative : Parser
    {
        public List<Parser> SubParsers;

        public Alternative(params Parser[] SubParsers)
        {
            this.SubParsers = new List<Parser>(SubParsers);
        }

        public static Alternative operator |(Alternative LHS, Parser RHS)
        {
            var r = LHS.Clone() as Alternative;
            r.SubParsers.Add(RHS);
            return r;
        }

        protected override Parser ImplementClone()
        {
            return new Alternative(SubParsers.ToArray());
        }

        protected override ParseResult ImplementParse(StringIterator InputStream)
        {
            CompoundFailure failureReason = null;

           foreach (var sub in SubParsers)
           {
               var subResult = sub.Parse(InputStream);
               if (subResult.ResultType == ResultType.HardError)
                   return Error("Hard failure in child parser", subResult.FailReason);
               else if (subResult.ResultType == ResultType.Success)
                   return WrapChild(subResult);
               else if (subResult.FailReason != null)
               {
                   if (failureReason == null) failureReason = new CompoundFailure();
                   failureReason.CompoundedFailures.Add(subResult.FailReason);
               }
           }

           return Fail("No alternatives matched", failureReason);
        }
    }
}
