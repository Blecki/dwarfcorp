using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        public DwarfThoughts(ComponentManager Manager, string name) : base(name, Manager)
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
                global::System.Diagnostics.Debug.Assert(_cachedCreature != null, "AI Could not find creature");
                return _cachedCreature;
            }
        }

        public List<Thought> Thoughts { get; set; }     // Todo: Make thoughts more generic. No ThoughtType enum!

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
                    Creature.NoiseMaker.MakeNoise("Pleased", Creature.Physics.Position, true);
                else
                    Creature.NoiseMaker.MakeNoise("Tantrum", Creature.Physics.Position, true);
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
                Creature.Physics.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        override public void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            Thoughts.RemoveAll(thought => thought.IsOver(Manager.World.Time.CurrentDate));
            Creature.Stats.Happiness.CurrentValue = 50.0f;

            foreach (Thought thought in Thoughts)
                Creature.Stats.Happiness.CurrentValue += thought.HappinessModifier;

            if (Creature.Stats.IsAsleep)
                AddThought(Thought.ThoughtType.Slept);
            else if (Creature.Stats.Energy.IsDissatisfied())
                AddThought(Thought.ThoughtType.FeltSleepy);

            if (Creature.Stats.Hunger.IsDissatisfied())
                AddThought(Thought.ThoughtType.FeltHungry);
        }
    }
}
