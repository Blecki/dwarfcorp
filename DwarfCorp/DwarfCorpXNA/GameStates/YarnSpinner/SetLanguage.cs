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
            var race = RaceLibrary.FindRace(Arguments[0].Value.ToString());
            if (race != null)
                State.SetLanguage(race.Speech.Language);
            else
                State.Output("ERROR setting language: Race not found.");
        }
    }
}
