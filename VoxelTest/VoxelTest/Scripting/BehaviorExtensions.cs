using System;
using System.Collections.Generic;

namespace DwarfCorp
{

    /// <summary>
    /// This static class adds implicit functions to acts to allow them to convert to and from
    /// boolean or enumerable functions.
    /// </summary>
    public static class BehaviorExtensions
    {
        public static Act GetAct(this Func<bool> condition)
        {
            return new Condition(condition);
        }

        public static Act GetAct(this Func<IEnumerable<Act.Status>> func)
        {
            return new Wrap(func);
        }
    }

}