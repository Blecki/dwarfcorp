using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Action
    {
        public WorldState PreCondition { get; set; }
        public WorldState Effects { get; set; }
        public float Cost { get; set; }
        public string Name { get; set; }

        public enum ValidationStatus
        {
            Ok,
            Replan,
            Invalid
        }

        public enum PerformStatus
        {
            Success,
            Failure,
            InProgress,
            Invalid
        }

        public Action()
        {
            PreCondition = new WorldState();
            Effects = new WorldState();
            Cost = 0;
            Name = "";
        }

        public Action(string name, WorldState pre, WorldState post, float cost)
        {
            PreCondition = pre;
            Effects = post;
            Cost = cost;
            Name = name;
        }

        public Action(Action other)
        {
            PreCondition = new WorldState(other.PreCondition);
            Effects = new WorldState(other.Effects);
            Cost = other.Cost;
            Name = other.Name;
        }

        public virtual bool CanPerform(WorldState currentState)
        {
            return currentState.MeetsRequirements(PreCondition);
        }

        public bool EffectsContainsTags(WorldState state)
        {
            foreach (string s in state.Specification.Keys)
            {
                if (Effects.Specification.ContainsKey(s))
                {
                    return true;
                }
            }

            return false;
        }


        public virtual bool Satisfies(WorldState currentState)
        {

            WorldState after = new WorldState(currentState);
            UndoEffects(after);

            if (!after.Conflicts(PreCondition) && !Effects.Conflicts(currentState) && EffectsContainsTags(currentState))
            {
                return true;
            }
            else
            {
                //Console.Out.WriteLine(Name + "does not satisfy: " + currentState.ToString());
                return false;
            }
        }

        public virtual void Apply(WorldState state)
        {
            foreach (string s in Effects.Specification.Keys)
            {
                if (Effects.Specification.ContainsKey(s))
                {
                    state[s] = Effects[s];
                }
            }
        }

        public virtual void UndoEffects(WorldState state)
        {
            foreach (string s in Effects.Specification.Keys)
            {
                if (PreCondition.Specification.ContainsKey(s))
                {
                    state[s] = PreCondition[s];
                }
            }
        }

        public virtual void UnApply(WorldState state)
        {
            foreach (string s in PreCondition.Specification.Keys)
            {
                if (PreCondition.Specification.ContainsKey(s))
                {
                    state[s] = PreCondition[s];
                }
                else if (Effects.Specification.ContainsKey(s) && state.Specification.ContainsKey(s))
                {
                    state.Specification.Remove(s);
                }
            }
        }

        public virtual ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            if (CanPerform(creature.Goap.Belief))
            {
                return ValidationStatus.Ok;
            }
            else
            {
                return ValidationStatus.Replan;
            }
        }

        public virtual PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            return PerformStatus.Success;
        }


    }
}
