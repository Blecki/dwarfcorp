// Language.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

    /// <summary>
    /// This is a weird (probably deprecated) system for making "talky" noises.
    /// </summary>
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
            foreach(string word in words)
            {
                if(word != "." && word != ",")
                {
                    Utterance utter = new Utterance();
                    utter.Type = UtteranceType.Syllable;
                    utter.Syllable = Syllables[Math.Abs(word.GetHashCode()) % Syllables.Count];

                    if(toReturn.Count == 0 || utter.Syllable != toReturn.Last().Syllable)
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
            foreach(Utterance utter in utterances)
            {
                if(utter.Type == UtteranceType.Pause)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    SoundEffectInstance inst = utter.Syllable.CreateInstance();
                    inst.Play();

                    while(inst.State == SoundState.Playing)
                    {
                        System.Threading.Thread.Sleep(5);
                    }
                }
            }
        }
    }

}