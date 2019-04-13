using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class EndConversation
    {
        [YarnCommand("end_conversation")]
        private static void _end_conversation(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.EndConversation();
        }
    }
}
