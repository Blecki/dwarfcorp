using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Dialogue
{
    public class DialogueContext
    {
        private Action<DialogueContext> NextAction = null;
        public Gui.Widget ChoicePanel;
        public Gui.Widget SpeechBubble;
        public Gui.Widgets.TradePanel TradePanel;
        public Animation SpeakerAnimation;

        public TradeEnvoy Envoy;
        public String EnvoyName = "TODO";

        public Faction PlayerFaction;

        public Diplomacy.Politics Politics;
        public WorldManager World;

        private IEnumerator<Utterance> speech; 

        public void Say(String Text)
        {
            SpeechBubble.Text = "";
            SpeechBubble.Invalidate();

            speech = Envoy.OwnerFaction.Race.Speech.Language.Say(Text).GetEnumerator();
        }

        public void ClearOptions()
        {
            ChoicePanel.Clear();
        }

        public void AddOption(String Prompt, Action<DialogueContext> Action)
        {
            ChoicePanel.AddChild(new Gui.Widget
            {
                Text = Prompt,
                OnClick = (sender, args) => Transition(Action),
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font-hires",
                TextColor = Color.Black.ToVector4(),
                ChangeColorOnHover = true,
                HoverTextColor = Color.DarkRed.ToVector4()
            });

            ChoicePanel.Layout();
        }

        public void Transition(Action<DialogueContext> NextAction)
        {
            this.NextAction = NextAction;
        }

        public void Update(DwarfTime Time)
        {
            if (speech != null)
            {
                Utterance utter = speech.Current;
                if (utter.SubSentence != null && utter.SubSentence != SpeechBubble.Text)
                {
                    if (SpeakerAnimation.IsDone())
                        SpeakerAnimation.Reset();
                    SpeakerAnimation.Play();
                    SpeechBubble.Text = utter.SubSentence;
                    SpeechBubble.Invalidate();
                }
                speech.MoveNext();
            }
            SpeakerAnimation.Update(Time, Timer.TimerMode.Real);

            var next = NextAction;
            NextAction = null;

            if (next != null)
                next(this);


        }
    }    
}
