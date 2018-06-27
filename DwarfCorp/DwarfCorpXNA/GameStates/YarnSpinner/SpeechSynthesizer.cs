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
    /// <summary>
    /// This is a system for making "talky" noises.
    /// </summary>
    public class SpeechSynthesizer
    {
        private List<string> Syllables { get; set; }
        private List<string> Yays { get; set; }
        private List<string> Boos { get; set; }
 
        public SpeechSynthesizer(Language Language)
        {
            Syllables = Language.Syllables;
            Yays = Language.Yays;
            Boos = Language.Boos;
        }
        
        public void SayYay()
        {
            if (Yays != null && Yays.Count > 0)
                SoundManager.PlaySound(Datastructures.SelectRandom(Yays), 0.15f);
        }

        public void SayBoo()
        {
            if (Boos != null && Boos.Count > 0)
                SoundManager.PlaySound(Datastructures.SelectRandom(Boos), 0.15f);
        }

        public IEnumerable<String> Say(string sentence)
        {
            return SayUtterance(ConvertSentence(sentence));
        }

        private IEnumerable<String> ChunkWord(String Word, int ChunkSize)
        {
            var i = 0;
            for (i = 0; i < Word.Length - ChunkSize; i += ChunkSize)
                yield return Word.Substring(i, ChunkSize);
            if (i < Word.Length)
                yield return Word.Substring(i);
        }

        private IEnumerable<Utterance> ConvertSentence(string sentence)
        {
            var words = sentence.Split(' ').Select(w => w + " ");
            var syls = words.SelectMany(w => ChunkWord(w, 5));

            foreach (var word in syls)
            {
                yield return new Utterance
                {
                    Type = UtteranceType.Syllable,
                    Syllable = Syllables[(int)word[0] % Syllables.Count],
                    SubSentence = word,
                };

                if (word.Contains(".") || word.Contains(","))
                {
                    yield return new Utterance
                    {
                        Type = UtteranceType.Pause
                    };
                }
            }
        }

        private IEnumerable<String> SayUtterance(IEnumerable<Utterance> utterances)
        {
            foreach(Utterance utter in utterances)
            {
                if(utter.Type == UtteranceType.Pause)
                {
                    Timer pauseTimer = new Timer(0.25f, true, Timer.TimerMode.Real);

                    while (!pauseTimer.HasTriggered)
                    {
                        pauseTimer.Update(DwarfTime.LastTime);
                        yield return "";
                    }
                }
                else
                {
                    SoundEffectInstance inst = SoundManager.PlaySound(utter.Syllable, 0.2f, MathFunctions.Rand(-0.4f, 0.4f));// MathFunctions.Rand(1e-2f, 2e-2f));

                    yield return utter.SubSentence;

                    while (inst != null && inst.State == SoundState.Playing)
                        yield return "";
                }
            }
        }
    }

}