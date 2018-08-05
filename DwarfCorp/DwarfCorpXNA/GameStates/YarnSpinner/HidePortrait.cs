using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class HidePortrait
    {
        [YarnCommand("hide_portrait")]
        private static void _hide_portrait(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.PlayerInterface.HidePortrait();
        }
    }
}
