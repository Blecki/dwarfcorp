using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals
{
    public class GoalManager
    {
        private List<Goal> AllGoals = new List<Goal>();
        private List<Goal> ActiveGoals = new List<Goal>();
        private List<TriggerEvent> Events = new List<TriggerEvent>();
        private List<Goal> NewlyActivatedGoals = new List<Goal>();
        public EventScheduler EventScheduler = new EventScheduler();

        public int NewAvailableGoals { get; private set; }
        public int NewCompletedGoals { get; private set; }

        public GoalManager()
        {
            NewAvailableGoals = 0;
            NewCompletedGoals = 0;
        }

        public void ResetNewAvailableGoals()
        {
            NewAvailableGoals = 0;
        }

        public void ResetNewCompletedGoals()
        {
            NewCompletedGoals = 0;
        }

        public IEnumerable<Goal> EnumerateGoals()
        {
            foreach (var goal in AllGoals)
                yield return goal;
        }

        public void Initialize(List<Goal> SerializedGoals)
        {
            // Discover all possible goals.
            // If loading a save game, half of these will be thrown away. :(
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(Goal)))
                {
                    var serializedGoal = SerializedGoals.FirstOrDefault(g => g.GetType() == type);
                    if (serializedGoal == null)
                        AllGoals.Add(Activator.CreateInstance(type) as Goal);
                    else
                        AllGoals.Add(serializedGoal);
                }
            }

            foreach (var goal in AllGoals)
            {
                if (goal.State == GoalState.Active)
                    ActiveGoals.Add(goal);

                if (goal.GoalType == GoalTypes.Achievement &&
                    goal.State == GoalState.Unavailable)
                {
                    ActiveGoals.Add(goal);
                    goal.State = GoalState.Active;
                }

                if (goal.GoalType == GoalTypes.AvailableAtStartup &&
                    goal.State == GoalState.Unavailable)
                    goal.State = GoalState.Available;
            }
        }

        public void OnGameEvent(TriggerEvent Event)
        {
            Events.Add(Event);
        }

        public void Update(WorldManager World)
        {
            var events = Events;
            Events = new List<TriggerEvent>();

            foreach (var goal in ActiveGoals)
                foreach (var @event in events)
                {
                    goal.OnGameEvent(World, @event);
                    if (goal.State == GoalState.Complete)
                        NewCompletedGoals += 1;
                }

            ActiveGoals.RemoveAll(g => g.State != GoalState.Active);
            ActiveGoals.AddRange(NewlyActivatedGoals);
            NewlyActivatedGoals.Clear();
            EventScheduler.Update(World, World.Time.CurrentDate);
        }
               
        public Goal.ActivationResult TryActivateGoal(WorldManager World, Goal Goal)
        {
            var activationResult = Goal.CanActivate(World);
            if (activationResult.Succeeded)
            {
                Goal.Activate(World);
                Goal.State = GoalState.Active;
                NewlyActivatedGoals.Add(Goal);
            }
            return activationResult;
        }

        public void UnlockGoal(Goal Goal)
        {
            if (Goal.State == GoalState.Unavailable)
            {
                Goal.State = GoalState.Available;
                NewAvailableGoals += 1;
            }
        }

        public void UnlockGoal(Type Type)
        {
            UnlockGoal(FindGoal(Type));
        }

        public Goal FindGoal(Type Type)
        {
            return AllGoals.FirstOrDefault(g => g.GetType() == Type);
        }
    }
}
