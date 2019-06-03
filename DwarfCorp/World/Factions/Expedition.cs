// Faction.cs
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
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DwarfCorp
{
    [JsonObject(IsReference =true)]
    public class Expedition
    {
        public enum State
        {
            Leaving,
            Arriving,
            Fighting,
            Trading
        }

        public DateTimer DeathTimer { get; set; }
        public List<CreatureAI> Creatures { get; set; }
        public Faction OwnerFaction { get; set; }
        public Faction OtherFaction { get; set; }
        public State ExpiditionState { get; set; }
        public bool ShouldRemove { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            // nothing for now.
        }

        public Expedition()
        {

        }

        public Expedition(DateTime date)
        {
            ExpiditionState = State.Arriving;
            ShouldRemove = false;
            Creatures = new List<CreatureAI>();
            DeathTimer = new DateTimer(date, new TimeSpan(1, 12, 0, 0));
        }
    }
}