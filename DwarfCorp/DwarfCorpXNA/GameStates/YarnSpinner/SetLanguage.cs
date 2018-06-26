using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class SetLanguage
    {
        [YarnCommand("set_language", "STRING")]
        private static void _set_language(YarnState State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            State.SetLanguage(RaceLibrary.FindRace(Arguments[0].Value.ToString()).Speech.Language);
        }
    }
}
