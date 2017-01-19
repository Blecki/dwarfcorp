﻿// NoiseMaker.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DwarfCorp
{
    /// <summary>
    /// Just holds a collection of noises, and can only make one such noise
    /// at a time (used, for instance, to make creatures have noises).
    /// </summary>
    public class NoiseMaker
    {
        public Sound3D CurrentSound { get; set; }

        public Dictionary<string, List<string>> Noises { get; set; }

        public NoiseMaker()
        {
            Noises = new Dictionary<string, List<string>>();
        }

        public void MakeNoise(string noise, Vector3 position, bool randomPitch = false, float volume = 1.0f)
        {
            if (!Noises.ContainsKey(noise))
            {
                return;
            }

            if (CurrentSound == null || CurrentSound.EffectInstance == null || CurrentSound.EffectInstance.IsDisposed || CurrentSound.EffectInstance.State == SoundState.Stopped || CurrentSound.EffectInstance.State == SoundState.Paused)
            {
                List<string> availableNoises = Noises[noise];
                int index = WorldManager.Random.Next(availableNoises.Count);

                if (index >= 0 && index < availableNoises.Count)
                {
                    CurrentSound = SoundManager.PlaySound(availableNoises[index], position, randomPitch, volume);
                }
               
            }


        }
    }
}
