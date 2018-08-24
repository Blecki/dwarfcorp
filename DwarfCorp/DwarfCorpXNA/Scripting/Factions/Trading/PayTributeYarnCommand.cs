using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class PayTributeYarnCommand
    {
        [YarnCommand("pay_tribute")]
        private static void _pay_tribute(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.PlayerInterface.Output("Command 'pay_tribute' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            envoy.TributeDemanded = (decimal)0.0f;
            playerFaction.AddMoney(-Math.Min(world.PlayerFaction.Economy.CurrentMoney,
                envoy.TributeDemanded));
            Memory.SetValue("$envoy_tribute_demanded", new Yarn.Value(0.0f));
            Memory.SetValue("$envoy_demands_tribute", new Yarn.Value(false));
        }
    }
}
