using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class EndConversation
    {
        [YarnCommand("end-conversation")]
        private static void _end_conversation(YarnState State, Ancora.AstNode Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.EndConversation();
        }
    }
}
