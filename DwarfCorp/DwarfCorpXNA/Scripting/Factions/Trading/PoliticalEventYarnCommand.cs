using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class PoliticalEventYarnCommand
    {
        [YarnCommand("political_event", "STRING", "NUMBER", "NUMBER")]
        private static void _political_event(YarnState State, Ancora.AstNode Arguments, Yarn.MemoryVariableStore Memory)
        {
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.Output("Command 'political_event' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            var politics = world.Diplomacy.GetPolitics(playerFaction, envoy.OwnerFaction);
            politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
            {
                Change = (float)Arguments.Children[1].Value,
                Description = (string)Arguments.Children[0].Value,
                Duration = new TimeSpan((int)((float)Arguments.Children[2].Value), 0, 0, 0),
                Time = world.Time.CurrentDate
            });
        }
    }
}
