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
        public string Syllable;
        public string SubSentence;
    }

    /// <summary>
    /// This is a system for making "talky" noises.
    /// </summary>
    public class Language
    {
        public List<string> Syllables { get; set; }
        public List<string> Yays { get; set; }
        public List<string> Boos { get; set; }
 
        public Language()
        {
            
        }

        public Language(List<string> syllables)
        {
            Syllables = syllables;
        }

        public void SayYay()
        {
            if (Yays != null && Yays.Count > 0)
            {
                SoundManager.PlaySound(Datastructures.SelectRandom(Yays), 0.15f);
            }
        }

        public void SayBoo()
        {
            if (Boos != null && Boos.Count > 0)
            {
                SoundManager.PlaySound(Datastructures.SelectRandom(Boos), 0.15f);
            }
        }

        public IEnumerable<Utterance> Say(string sentence)
        {
            return SayUtterance(ConvertSentence(sentence));
        }

        private IEnumerable<Utterance> ConvertSentence(string sentence)
        {
            string[] words = sentence.Split(' ');
            for(int i = 0; i < words.Length; i++)
            {
                words[i] += " ";
            }
            
            List<string> syls = new List<string>();
            int chunkSize = 5;
            foreach (var word in words)
            {
                int i = 0;
                for (i = 0; i < word.Length - chunkSize; i += chunkSize)
                {
                    syls.Add(word.Substring(i, chunkSize));
                }

                if (i < word.Length)
                {
                    syls.Add(word.Substring(i));
                }
            }


            if (syls.Count == 0)
            {
                syls.Add(sentence);
            }
            int utterances = 0;
            string lastUtterance = null;
            string builtSentence = "";
            foreach(string word in syls)
            {
                builtSentence += word;
                string subSentence = new string(builtSentence.ToCharArray());
                if(!(word.Contains(".") || word.Contains(",")))
                {
                    Utterance utter = new Utterance();
                    utter.Type = UtteranceType.Syllable;
                    utter.SubSentence = subSentence;
                    utter.Syllable = Syllables[(int)word[0] % Syllables.Count];

                    if(utterances == 0 || utter.Syllable != lastUtterance)
                    {
                        lastUtterance = utter.Syllable;
                        yield return utter;
                    }
                }
                else
                {
                    Utterance utter = new Utterance();
                    utter.Type = UtteranceType.Syllable;
                    utter.SubSentence = subSentence;
                    utter.Syllable = Syllables[(int)word[0] % Syllables.Count];

                    if (utterances == 0 || utter.Syllable != lastUtterance)
                    {
                        lastUtterance = utter.Syllable;
                        yield return utter;
                    }
                    Utterance pause = new Utterance {Type = UtteranceType.Pause, SubSentence = subSentence};
                    yield return pause;
                }
            }
        }

        private IEnumerable<Utterance> SayUtterance(IEnumerable<Utterance> utterances)
        {
            foreach(Utterance utter in utterances)
            {
                if(utter.Type == UtteranceType.Pause)
                {
                    Timer pauseTimer = new Timer(0.25f, true, Timer.TimerMode.Real);

                    while (!pauseTimer.HasTriggered)
                    {
                        pauseTimer.Update(DwarfTime.LastTime);
                        yield return utter;
                    }
                }
                else
                {
                    SoundEffectInstance inst = SoundManager.PlaySound(utter.Syllable, 0.2f);// MathFunctions.Rand(1e-2f, 2e-2f));
                    if (inst == null)
                    {
                        yield return utter;
                        continue;
                    }
                    inst.Pitch = MathFunctions.Rand(-0.4f, 0.4f);
                    inst.Play();

                    while(inst.State == SoundState.Playing)
                    {
                        yield return utter;
                    }
                }
            }
        }
    }

}