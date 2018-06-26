using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class PayTributeYarnCommand
    {
        [YarnCommand("pay_tribute")]
        private static void _pay_tribute(YarnState State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.Output("Command 'pay_tribute' can only be called from a TradeEnvoy initiated conversation.", false);
                return;
            }

            playerFaction.Economy.CurrentMoney -= Math.Min(world.PlayerFaction.Economy.CurrentMoney,
                envoy.TributeDemanded);
        }
    }
}
