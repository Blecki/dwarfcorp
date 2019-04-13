using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public enum ResultType
    {
        Success,
        Failure,
        HardError
    }

    public struct ParseResult
    {
        public ResultType ResultType;
        public AstNode Node;
        public StringIterator After;
        public Failure FailReason;
    }
}
