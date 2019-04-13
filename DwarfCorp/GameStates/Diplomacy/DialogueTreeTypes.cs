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
}
