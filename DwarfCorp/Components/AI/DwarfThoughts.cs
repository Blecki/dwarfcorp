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
        public List<Thought> Thoughts = new List<Thought>();

        public DwarfThoughts()
        {
        }

        public DwarfThoughts(ComponentManager Manager, string name) : base(name, Manager)
        {
        }

        private Creature _cachedCreature = null;
        [JsonIgnore] public Creature Creature
        {
            get
            {
                if (Parent == null) return null;
                if (_cachedCreature == null) _cachedCreature = Parent.EnumerateAll().OfType<Creature>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_cachedCreature != null, "AI Could not find creature");
                return _cachedCreature;
            }
        }

        public bool HasThought(String Description) 
        {
            return Thoughts.Any(t => t.Description == Description);
        }

        /// <summary> Remove a standard thought from the creature. </summary>
        public void RemoveThought(Thought Thought)
        {
            Thoughts.Remove(Thought);
        }

        /// <summary> Add a custom thought to the creature </summary>
        public void AddThought(Thought thought)
        {
            if (HasThought(thought.Description))
                return;

            Thoughts.Add(thought);

            var good = thought.HappinessModifier > 0;
            var textColor = good ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red);
            var prefix = good ? "+" : "";
            var postfix = good ? ":)" : ":(";
            IndicatorManager.DrawIndicator(prefix + thought.HappinessModifier + " " + postfix, Creature.Physics.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 1.0f, textColor);
        }

        override public void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            Thoughts.RemoveAll(thought => thought.IsOver(Manager.World.Time.CurrentDate));
            Creature.Stats.Happiness.CurrentValue = 50.0f;

            foreach (Thought thought in Thoughts)
                Creature.Stats.Happiness.CurrentValue += thought.HappinessModifier;

            // Todo: Should this be here?
            if (Creature.Stats.IsAsleep)
                Creature.AddThought("I slept recently.", new TimeSpan(0, 8, 0, 0), 5.0f);

            else if (Creature.Stats.Energy.IsDissatisfied())
                Creature.AddThought("I was sleepy recently.", new TimeSpan(0, 4, 0, 0), -3.0f);

            if (Creature.Stats.Hunger.IsDissatisfied())
                Creature.AddThought("I was hungry recently.", new TimeSpan(0, 8, 0, 0), -3.0f);
        }
    }
}
