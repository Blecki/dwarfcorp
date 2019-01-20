// CreatureAI.cs
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
//using System.Windows.Forms;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DwarfThoughts : GameComponent
    {
        public int MaxMessages = 10;
        public List<string> MessageBuffer = new List<string>();

        public DwarfThoughts()
        {
        }

        public DwarfThoughts(
            ComponentManager Manager,
            string name) :
            base(name, Manager)
        {
            Thoughts = new List<Thought>();
            UpdateRate = 100;
        }

        private Creature _cachedCreature = null;
        [JsonIgnore] public Creature Creature
        {
            get
            {
                if (Parent == null)
                    return null;
                if (_cachedCreature == null)
                    _cachedCreature = Parent.EnumerateAll().OfType<Creature>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_cachedCreature != null, "AI Could not find creature");
                return _cachedCreature;
            }
        }

        [JsonIgnore]
        public CreatureStatus Status
        {
            get { return Creature.Status; }
            set { Creature.Status = value; }
        }

        public List<Thought> Thoughts { get; set; }
    

        /// <summary> Wrapper around Creature.Physics.GlobalTransform.Translation </summary>
        [JsonIgnore]
        public Vector3 Position
        {
            get { return Creature.Physics.GlobalTransform.Translation; }
            set
            {
                Matrix newTransform = Creature.Physics.LocalTransform;
                newTransform.Translation = value;
                Creature.Physics.LocalTransform = newTransform;
            }
        }

        /// <summary> returns whether or not the creature already has a thought of the given type. </summary>
        public bool HasThought(Thought.ThoughtType type)
        {
            return Thoughts.Any(existingThought => existingThought.Type == type);
        }

        /// <summary> Add a standard thought to the creature. </summary>
        public void AddThought(Thought.ThoughtType type)
        {
            if (!HasThought(type))
            {
                var thought = Thought.CreateStandardThought(type, Manager.World.Time.CurrentDate);
                AddThought(thought, true);

                if (thought.HappinessModifier > 0.01)
                {
                    Creature.NoiseMaker.MakeNoise("Pleased", Position, true);
                }
                else
                {
                    Creature.NoiseMaker.MakeNoise("Tantrum", Position, true);
                }
            }
        }

        /// <summary> Remove a standard thought from the creature. </summary>
        public void RemoveThought(Thought.ThoughtType thoughtType)
        {
            Thoughts.RemoveAll(thought => thought.Type == thoughtType);
        }

        /// <summary> Add a custom thought to the creature </summary>
        public void AddThought(Thought thought, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                Thoughts.Add(thought);
            }
            else
            {
                if (HasThought(thought.Type))
                {
                    return;
                }

                Thoughts.Add(thought);
            }
            bool good = thought.HappinessModifier > 0;
            Color textColor = good ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red);
            string prefix = good ? "+" : "";
            string postfix = good ? ":)" : ":(";
            IndicatorManager.DrawIndicator(prefix + thought.HappinessModifier + " " + postfix,
                Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        override public void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {

            Thoughts.RemoveAll(thought => thought.IsOver(Manager.World.Time.CurrentDate));
            Status.Happiness.CurrentValue = 50.0f;

            foreach (Thought thought in Thoughts)
            {
                Status.Happiness.CurrentValue += thought.HappinessModifier;
            }

            if (Status.IsAsleep)
            {
                AddThought(Thought.ThoughtType.Slept);
            }
            else if (Status.Energy.IsDissatisfied())
            {
                AddThought(Thought.ThoughtType.FeltSleepy);
            }

            if (Status.Hunger.IsDissatisfied())
            {
                AddThought(Thought.ThoughtType.FeltHungry);
            }
        }
    }
}
