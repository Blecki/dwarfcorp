using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class MakePeaceYarnCommand
    {
        [YarnCommand("make_peace")]
        private static void _make_peace(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.PlayerInterface.Output("Command 'make_peace' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            world.GetPolitics(playerFaction, envoy.OwnerFaction).IsAtWar = false;
        }
    }
}
