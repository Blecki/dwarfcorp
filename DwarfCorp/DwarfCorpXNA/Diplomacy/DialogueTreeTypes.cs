using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Dialogue
{
    public class DialogueContext
    {
        private Action<DialogueContext> NextAction = null;
        public Gum.Widget Panel;
        public NewGui.TradePanel TradePanel;

        public Faction.TradeEnvoy Envoy;
        public String EnvoyName = "TODO";

        public Faction PlayerFaction;

        public void Say(String Text)
        {
            Panel.Text = Text;
            Panel.Layout();
        }

        public void ClearOptions()
        {
            Panel.Clear();
        }

        public void AddOption(String Prompt, Action<DialogueContext> Action)
        {
            Panel.AddChild(new Gum.Widget
            {
                Text = Prompt,
                OnClick = (sender, args) => Transition(Action),
                AutoLayout = Gum.AutoLayout.DockTop
            });

            Panel.Layout();
        }

        public void Transition(Action<DialogueContext> NextAction)
        {
            this.NextAction = NextAction;
        }

        public void Update()
        {
            var next = NextAction;
            NextAction = null;

            if (next != null)
                next(this);
        }
    }    
}
