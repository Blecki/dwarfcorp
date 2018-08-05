using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.Factions.Trading
{
    static class TradeYarnCommand
    {
        [YarnCommand("trade")]
        private static void _trade(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var realState = State.PlayerInterface as YarnState; // THIS IS A HACK.

            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (envoy == null || playerFaction == null || world == null)
            {
                State.PlayerInterface.Output("Command 'trade' can only be called from a TradeEnvoy initiated conversation.");
                return;
            }

            var tradeState = new TradeGameState(
                realState.StateManager.Game,
                realState.StateManager,
                envoy,
                playerFaction,
                world);

            tradeState.CallWhenDone = () =>
            {
                //Setup memory variables with trade results.
                var transaction = tradeState.TradePanel.Transaction;

                if (tradeState.TradePanel.Result == Gui.Widgets.TradeDialogResult.Cancel)
                    Memory.SetValue("$trade_result", new Yarn.Value("cancelled"));
                else if (tradeState.TradePanel.Result == Gui.Widgets.TradeDialogResult.RejectProfit)
                    Memory.SetValue("$trade_result", new Yarn.Value("unprofitable"));
                else if (transaction.PlayerItems.Select(i => ResourceLibrary.GetResourceByName(i.ResourceType))
                    .SelectMany(i => i.Tags)
                    .Any(tag => envoy.OwnerFaction.Race.HatedResources.Contains(tag)))
                    Memory.SetValue("$trade_result", new Yarn.Value("hated"));
                else if (transaction.PlayerItems.Select(i => ResourceLibrary.GetResourceByName(i.ResourceType))
                    .SelectMany(i => i.Tags)
                    .Any(tag => envoy.OwnerFaction.Race.LikedResources.Contains(tag)))
                    Memory.SetValue("$trade_result", new Yarn.Value("liked"));
                else
                    Memory.SetValue("$trade_result", new Yarn.Value("acceptable"));

                Memory.SetValue("$trade_transaction", new Yarn.Value(transaction));
                State.Unpause();
            };

            State.Pause();
            realState.StateManager.PushState(tradeState);
        }

        [YarnCommand("finalize_trade")]
        private static void _finalize_trade(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var transaction = Memory.GetValue("$trade_transaction").AsObject as Trade.TradeTransaction;
            var envoy = Memory.GetValue("$envoy").AsObject as TradeEnvoy;
            var playerFaction = Memory.GetValue("$player_faction").AsObject as Faction;
            var world = Memory.GetValue("$world").AsObject as WorldManager;

            if (transaction == null || envoy == null || playerFaction == null || world == null)
            {
                State.PlayerInterface.Output("Command 'finalize_trade' can only be called from a TradeEnvoy initiated conversation.");
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
