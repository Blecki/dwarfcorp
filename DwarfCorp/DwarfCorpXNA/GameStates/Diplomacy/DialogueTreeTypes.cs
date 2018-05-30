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

    public class SpeakerWidget
    {
        public Gui.Widget SpeechBubble;
        public AnimationPlayer SpeakerAnimation;
        private IEnumerator<Utterance> speech;
        private string sentence;
        public Race Race;

        public void Say(string Text)
        {
            sentence = Text;
            SpeechBubble.Text = "";
            SpeechBubble.Invalidate();

            speech = Race.Speech.Language.Say(Text).GetEnumerator();
        }

        public bool IsDone()
        {
            return speech == null || (speech.Current.SubSentence != null && speech.Current.SubSentence.Length == sentence.Length + 1);
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
            SpeakerAnimation.Update(Time, false, Timer.TimerMode.Real);
        }

        public void Skip()
        {
            speech = null;
            SpeechBubble.Text = sentence;
            SpeakerAnimation.Stop();
        }
    }

    public class DialogueContext
    {
        private Action<DialogueContext> NextAction = null;
        public Gui.Widget ChoicePanel;
        public Gui.Widgets.TradePanel TradePanel;

        public TradeEnvoy Envoy;
        public int NumOffensiveTrades = 0;
        public String EnvoyName = "TODO";

        public Faction PlayerFaction;

        public Diplomacy.Politics Politics;
        public WorldManager World;

        public SpeakerWidget Speaker;

        public void Say(String Text)
        {
            Speaker.Say(Text);
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
                Font = "font16",
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
            Speaker.Update(Time);

            var next = NextAction;
            NextAction = null;

            if (next != null)
                next(this);

            if (TradePanel != null && !TradePanel.Hidden && TradePanel.Speaker != null && TradePanel.Root != null)
            {
                TradePanel.UpdateSpeakerAnimation(Time);
            }
        }

        public void Skip()
        {
            Speaker.Skip();
        }
    }    
}
