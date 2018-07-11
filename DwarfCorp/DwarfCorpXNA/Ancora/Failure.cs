using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public class Failure
    {
        public String Message { get; internal set; }
        public Parser FailedAt { get; internal set; }

        public Failure(Parser FailedAt, String Message)
        {
            this.FailedAt = FailedAt;
            this.Message = Message;
        }
      
        public static CompoundFailure Compound(Failure A, Failure B)
        {
            var r = new CompoundFailure();
            if (A is CompoundFailure) r.CompoundedFailures.AddRange((A as CompoundFailure).CompoundedFailures);
            else r.CompoundedFailures.Add(A);
            if (B is CompoundFailure) r.CompoundedFailures.AddRange((B as CompoundFailure).CompoundedFailures);
            else r.CompoundedFailures.Add(B);
            return r;
        }
    }

    public class CompoundFailure : Failure
    {
        internal List<Failure> CompoundedFailures = new List<Failure>();

        public CompoundFailure() : base(null, "COMPOUND FAILURE") { }

        internal void AddFailure(Failure SubFailure)
        {
            CompoundedFailures.Add(SubFailure);
        }
    }
}
