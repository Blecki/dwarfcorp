using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class AlwaysTalk
    {
        [YarnCommand("always_talk")]
        private static void _always_talk(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.PlayerInterface.ActivateAlwaysTalk();
        }

        [YarnCommand("clear")]
        private static void _clear(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.PlayerInterface.ClearOutput();
        }

    }
}
