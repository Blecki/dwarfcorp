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

        public GoalManager()
        {
            // Discover all possible goals.
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                if (type.IsSubclassOf(typeof(Goal)))
                {
                    var newGoal = Activator.CreateInstance(type) as Goal;
                    newGoal.SystemName = type.FullName;
                    AllGoals.Add(type.FullName, newGoal);
                }
        }

        public IEnumerable<Goal> EnumerateGoals()
        {
            foreach (var goal in AllGoals)
                yield return goal.Value;
        }

        public void Initialize(GoalMemory Memory)
        {
            foreach (var goal in AllGoals)
            {
                goal.Value.Memory = Memory;

                if (goal.Value.State == GoalState.Active)
                    ActiveGoals.Add(goal.Value);

                if (goal.Value.GoalType == GoalTypes.Achievement &&
                    goal.Value.State == GoalState.Unavailable)
                {
                    ActiveGoals.Add(goal.Value);
                    goal.Value.State = GoalState.Active;
                }

                if (goal.Value.GoalType == GoalTypes.AvailableAtStartup &&
                    goal.Value.State == GoalState.Unavailable)
                    goal.Value.State = GoalState.Available;
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
        }

        public Goal FindGoal(String Name)
        {
            return AllGoals[Name];
        }

        public Goal.ActivationResult ActivateGoal(WorldManager World, Goal Goal)
        {
            var activationResult = Goal.Activate(World);
            if (activationResult.Succeeded)
            {
                Goal.State = GoalState.Active;
                ActiveGoals.Add(Goal);
            }
            return activationResult;
        }

        public void UnlockGoal(Goal Goal)
        {
            if (Goal.State == GoalState.Unavailable)
                Goal.State = GoalState.Available;
        }
    }
}
