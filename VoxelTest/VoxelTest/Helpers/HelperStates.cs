using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    public class RandomState : State
    {
        public Dictionary<string, int> ProbabilityDistribution { get; set; }
        public Random Generator { get; set; }

        public RandomState(string name, Dictionary<string, int> probabilities)
            : base(name)
        {
            ProbabilityDistribution = probabilities;
            Generator = new Random();
        }

        public override void Render(GameTime time, GraphicsDevice device)
        {
            base.Render(time, device);
        }

        public override void Update(GameTime time)
        {
            ShouldTransition = true;
            TransitionTo = RandomChoice(ProbabilityDistribution, Generator);
            base.Update(time);
        }

        public static string RandomChoice(Dictionary<string, int> probabilityDistribution, Random rand)
        {
            int totalweight = probabilityDistribution.Sum(c => c.Value);
            int choice = rand.Next(totalweight);
            int sum = 0;


            foreach (KeyValuePair<string, int> obj in probabilityDistribution)
            {
                for (float i = sum; i < obj.Value + sum; i++)
                {
                    if (i >= choice)
                    {
                        return obj.Key;
                    }
                }
                sum += obj.Value;
            }

            return null;
        }
    }
    
    public class TimeoutState : State
    {
        public Timer TransitionTimer { get; set; }
        public string StateOnTimeout { get; set; }

        public TimeoutState(string name, float timeToTransition, string stateToTransitionTo)
            : base(name)
        {
            TransitionTimer = new Timer(timeToTransition, false);
        }

        public override void OnEnter()
        {
            TransitionTimer.Reset(TransitionTimer.TargetTimeSeconds);
            base.OnEnter();
        }

        public override void Update(GameTime time)
        {
            if (TransitionTimer.HasTriggered)
            {
                ShouldTransition = true;
                TransitionTo = StateOnTimeout;
            }
            else
            {
                TransitionTimer.Update(time);
            }

            base.Update(time);
        }
    }
}
