using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Goals.Goals
{
    public class Ordu_Bullion : Goal
    {
        private DwarfBux Gold;

        public Ordu_Bullion()
        {
            Name = "Ordu: Bullion for the Dead";
            Description = @"You received a letter from Uzzikal the Necromancer. ""Dear Fleshy Dwarf, I am certain you hate the Elves as much as I do. We have traded favorably in the past, so I am coming to you for aid. We need DwarfBux to press our advantage against those toy-making fools. We are willing to trade for it, but we need 500 DwarfBux."" So, it looks like all you have to do is trade the Undead 500 DwarfBux.";
            GoalType = GoalTypes.UnavailableAtStartup;
        }

        public override void CreateGUI(Widget Widget)
        {
            if (State == GoalState.Complete)
                Widget.Text = "You received a letter from Uzzikal the Necromancer asking for your aid in his battle against the elves. You donated $500 to the cause.";
            else 
                Widget.Text = Description + "\n" + ((Gold >= 500) ? "Goal met!" :
                        String.Format("{0}/500", Gold));
        }

        public override void OnGameEvent(WorldManager World, TriggerEvent Event)
        {
            if (State == GoalState.Complete) return;
            var trade = Event as Events.Trade;
            if (trade != null && trade.OtherFaction.Name == "Ordu")
            {
                Gold += trade.PlayerGold - trade.OtherGold;
                if (Gold >= 500)
                {
                    State = GoalState.Complete;
                    World.MakeAnnouncement("Goal met. You have traded 500 gold to Uzzikal.");
                    World.GoalManager.UnlockGoal(typeof(Ordu_Elf_Envoy));
                }
            }
        }
    }
}
