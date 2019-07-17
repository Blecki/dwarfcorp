using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class SetLanguage
    {
        [YarnCommand("set_language", "STRING")]
        private static void _set_language(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            if (Library.GetRace(Arguments[0].Value.ToString()).HasValue(out var race))
                State.PlayerInterface.SetLanguage(race.Language);
            else
                State.PlayerInterface.Output("ERROR setting language: Race not found.");
        }
    }
}
