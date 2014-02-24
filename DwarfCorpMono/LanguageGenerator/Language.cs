using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DwarfCorp
{
    public enum UtteranceType
    {
        Syllable,
        Pause
    }

    public struct Utterance
    {
        public UtteranceType Type;
        public SoundEffect Syllable;
    }

    public class Language
    {
        public List<SoundEffect> Syllables { get; set; }

        public Language(List<SoundEffect> syllables)
        {
            Syllables = syllables;
        }


        public void Say(string sentence)
        {
            SayUtterance(ConvertSentence(sentence));
        }

        public List<Utterance> ConvertSentence(string sentence)
        {
            List<Utterance> toReturn = new List<Utterance>();
            string[] words = sentence.Split(' ');
            foreach (string word in words)
            {

                if (word != "." && word != ",")
                {
                    Utterance utter = new Utterance();
                    utter.Type = UtteranceType.Syllable;
                    utter.Syllable = Syllables[Math.Abs(word.GetHashCode()) % Syllables.Count];

                    if (toReturn.Count == 0 || utter.Syllable != toReturn.Last().Syllable)
                    {
                        toReturn.Add(utter);
                    }
                }
                else
                {
                    Utterance pause = new Utterance();
                    pause.Type = UtteranceType.Pause;
                    toReturn.Add(pause);
                }

                

            }

            return toReturn;

        }

        public void SayUtterance(List<Utterance> utterances)
        {
            foreach (Utterance utter in utterances)
            {
                if (utter.Type == UtteranceType.Pause)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    SoundEffectInstance inst = utter.Syllable.CreateInstance();
                    inst.Play();

                    while (inst.State == SoundState.Playing)
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                   
                }
            }

        }
       
    }
}
