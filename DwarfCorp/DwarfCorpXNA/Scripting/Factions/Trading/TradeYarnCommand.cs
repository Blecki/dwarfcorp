using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class TradeYarnCommand
    {
        [YarnCommand("trade")]
        private static void _trade(YarnState State, Ancora.AstNode Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player-faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            var tradeState = new TradeGameState(
                State.StateManager.Game,
                State.StateManager,
                envoy,
                playerFaction,
                world);

            tradeState.CallWhenDone = () =>
            {
                //Setup memory variables with trade results.

                State.Unpause();
            };

            State.Pause();
            State.StateManager.PushState(tradeState);
        }
    }
}
