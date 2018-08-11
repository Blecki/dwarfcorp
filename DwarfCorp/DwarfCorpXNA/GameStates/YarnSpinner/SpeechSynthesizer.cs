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
        public bool IsSkipping = false;
        public float Pitch = 0.0f;

        public SpeechSynthesizer(Language Language)
        {
            Syllables = Language.Syllables;
            Yays = Language.Yays;
            Boos = Language.Boos;
        }
        
        public void SayYay()
        {
            if (Yays != null && Yays.Count > 0)
                SoundManager.PlaySound(Datastructures.SelectRandom(Yays), 0.15f, Pitch);
        }

        public void SayBoo()
        {
            if (Boos != null && Boos.Count > 0)
                SoundManager.PlaySound(Datastructures.SelectRandom(Boos), 0.15f, Pitch);
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
            var syls = ChunkWord(sentence, 10);

            foreach (var word in syls)
            {
                yield return new Utterance
                {
                    Type = UtteranceType.Syllable,
                    Syllable = Syllables[(int)word[0] % Syllables.Count],
                    SubSentence = word,
                };

                if (word.Any(c => ".,!?:;".Contains(c)))
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

                    while (!pauseTimer.HasTriggered && !IsSkipping)
                    {
                        pauseTimer.Update(DwarfTime.LastTime);
                        yield return "";
                    }
                }
                else
                {
                    SoundEffectInstance inst = null;
                    if (!IsSkipping)
                        inst = SoundManager.PlaySound(utter.Syllable, 0.2f, MathFunctions.Clamp(MathFunctions.Rand(-0.4f, 0.4f) + Pitch, -1.0f, 1.0f));// MathFunctions.Rand(1e-2f, 2e-2f));

                    foreach (char c in utter.SubSentence)
                        yield return "" + c;

                    
                    while (!IsSkipping && inst != null && inst.State == SoundState.Playing)
                        yield return "";
                }
            }
            IsSkipping = false;
        }
    }

}