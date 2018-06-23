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

            if (envoy == null || playerFaction == null || world == null)
            {
                State.Output("Command 'trade' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            var tradeState = new TradeGameState(
                State.StateManager.Game,
                State.StateManager,
                envoy,
                playerFaction,
                world);

            tradeState.CallWhenDone = () =>
            {
                //Setup memory variables with trade results.
                var transaction = tradeState.TradePanel.Transaction;

                if (tradeState.TradePanel.Result == Gui.Widgets.TradeDialogResult.Cancel)
                    Memory.SetValue("$trade-result", new Yarn.Value("cancelled"));
                else if (tradeState.TradePanel.Result == Gui.Widgets.TradeDialogResult.RejectProfit)
                    Memory.SetValue("$trade-result", new Yarn.Value("unprofitable"));
                else if (transaction.PlayerItems.Select(i => ResourceLibrary.GetResourceByName(i.ResourceType))
                    .SelectMany(i => i.Tags)
                    .Any(tag => envoy.OwnerFaction.Race.HatedResources.Contains(tag)))
                    Memory.SetValue("$trade-result", new Yarn.Value("hated"));
                else if (transaction.PlayerItems.Select(i => ResourceLibrary.GetResourceByName(i.ResourceType))
                    .SelectMany(i => i.Tags)
                    .Any(tag => envoy.OwnerFaction.Race.LikedResources.Contains(tag)))
                    Memory.SetValue("$trade-result", new Yarn.Value("liked"));
                else
                    Memory.SetValue("$trade-result", new Yarn.Value("acceptable"));

                Memory.SetValue("$trade-transaction", new Yarn.Value(transaction));
                State.Unpause();
            };

            State.Pause();
            State.StateManager.PushState(tradeState);
        }

        [YarnCommand("finalize-trade")]
        private static void _finalize_trade(YarnState State, Ancora.AstNode Arguments, Yarn.MemoryVariableStore Memory)
        {
            var transaction = Memory.GetValue("$trade-transaction").AsObject as Trade.TradeTransaction;
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player-faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (transaction == null || envoy == null || playerFaction == null || world == null)
            {
                State.Output("Command 'finalize-trade' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            transaction.Apply(world);
            
            world.GoalManager.OnGameEvent(new Goals.Triggers.Trade
            {
                PlayerFaction = playerFaction,
                PlayerGold = transaction.PlayerMoney,
                PlayerGoods = transaction.PlayerItems,
                OtherFaction = envoy.OwnerFaction,
                OtherGold = transaction.EnvoyMoney,
                OtherGoods = transaction.EnvoyItems
            });
        }
    }
}
