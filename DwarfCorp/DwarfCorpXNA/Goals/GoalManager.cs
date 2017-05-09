using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public class GoalManager
    {
        private Dictionary<String, Goal> AllGoals = new Dictionary<string, Goal>();
        private List<Goal> ActiveGoals = new List<Goal>();
        private List<GameEvent> Events = new List<GameEvent>();
        private List<Goal> PendingActivation = new List<Goal>();

        public GoalManager(GoalMemory Memory)
        {
            // Discover all possible goals.
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                if (type.IsSubclassOf(typeof(Goal)))
                {
                    var newGoal = Activator.CreateInstance(type, type.FullName, Memory) as Goal;
                    AllGoals.Add(type.FullName, newGoal);
                    if (newGoal.GoalType == GoalTypes.Achievement)
                    {
                        newGoal.State = GoalState.Active;
                        ActiveGoals.Add(newGoal);
                    }
                }
        }

        public void OnGameEvent(GameEvent Event)
        {
            Events.Add(Event);
        }

        public void Update(WorldManager World)
        {
            var events = Events;
            Events = new List<GameEvent>();

            foreach (var goal in ActiveGoals)
                foreach (var @event in events)
                    goal.OnGameEvent(World, @event);

            ActiveGoals.RemoveAll(g => g.State != GoalState.Active);

            var activation = PendingActivation;
            PendingActivation = new List<Goal>();

            foreach (var goal in activation)
            {
                goal.OnActivated(World);
                if (goal.State == GoalState.Active)
                    ActiveGoals.Add(goal);
            }
        }

        public Goal FindGoal(String Name)
        {
            return AllGoals[Name];
        }

        public void ActivateGoal(String Name)
        {
            PendingActivation.Add(AllGoals[Name]);
        }

        public void UnlockGoal(String Name)
        {
            var goal = FindGoal(Name);
            if (goal.State == GoalState.Unavailable)
                goal.State = GoalState.Available;
        }
    }
}
