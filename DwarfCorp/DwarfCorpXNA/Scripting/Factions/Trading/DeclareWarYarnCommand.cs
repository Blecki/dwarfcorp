using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class DeclareWarYarnCommand
    {
        [YarnCommand("declare_war")]
        private static void _declare_war(YarnState State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.Output("Command 'declare_war' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            world.GoalManager.OnGameEvent(new Goals.Triggers.DeclareWar
            {
                PlayerFaction = playerFaction,
                OtherFaction = envoy.OwnerFaction
            });
        }
    }
}
