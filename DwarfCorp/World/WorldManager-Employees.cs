using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DwarfCorp
{
    public partial class PersistentWorldData
    {
        public struct ApplicantArrival
        {
            public Applicant Applicant;
            public DateTime ArrivalTime;
        }

        public List<ApplicantArrival> NewArrivals = new List<ApplicantArrival>();
        public List<CreatureAI> SelectedMinions = new List<CreatureAI>();

        public float CorporateFoodCostPolicy = 1.0f;
    }

    public partial class WorldManager
    {
        public DateTime Hire(Applicant currentApplicant, int delay)
        {
            var startDate = Time.CurrentDate;
            if (PersistentData.NewArrivals.Count > 0)
                startDate = PersistentData.NewArrivals.Last().ArrivalTime;

            PersistentData.NewArrivals.Add(new PersistentWorldData.ApplicantArrival()
            {
                Applicant = currentApplicant,
                ArrivalTime = startDate + new TimeSpan(0, delay, 0, 0, 0)
            });

            PlayerFaction.AddMoney(-(decimal)(currentApplicant.Level.Pay * 4));
            return PersistentData.NewArrivals.Last().ArrivalTime;
        }

        public void HireImmediately(Applicant currentApplicant)
        {
            var rooms = EnumerateZones().Where(room => room.Type.Name == "Balloon Port").ToList();

            if (rooms.Count == 0) // No balloon port - okay, just... pick a damn room.
                rooms = EnumerateZones().ToList();

            Vector3 spawnLoc = Renderer.Camera.Position;
            if (rooms.Count > 0)
                spawnLoc = rooms.First().GetBoundingBox().Center() + Vector3.UnitY;

            var spawnOffset = MathFunctions.RandVector2Circle();
            spawnLoc += new Vector3(spawnOffset.X, 0.0f, spawnOffset.Y);

            var dwarfPhysics = DwarfFactory.GenerateDwarf(
                    spawnLoc,
                    ComponentManager, currentApplicant.ClassName, currentApplicant.LevelIndex, currentApplicant.Gender, currentApplicant.RandomSeed);
            ComponentManager.RootComponent.AddChild(dwarfPhysics);

            var newMinion = dwarfPhysics.EnumerateAll().OfType<Dwarf>().FirstOrDefault();
            Debug.Assert(newMinion != null);

            newMinion.Stats.AllowedTasks = currentApplicant.Class.Actions;
            newMinion.Stats.LevelIndex = currentApplicant.LevelIndex - 1;
            newMinion.Stats.LevelUp(newMinion);
            newMinion.Stats.FullName = currentApplicant.Name;
            newMinion.AI.AddMoney(currentApplicant.Level.Pay * 4m);
            newMinion.AI.Biography = currentApplicant.Biography;

            MakeAnnouncement(
                new Gui.Widgets.QueuedAnnouncement
                {
                    Text = String.Format("{0} was hired as a {1}.", currentApplicant.Name, currentApplicant.Level.Name),
                    ClickAction = (gui, sender) => newMinion.AI.ZoomToMe()
                });

            ParticleManager.Trigger("dwarf_puff", spawnLoc, Color.DarkViolet, 90);

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
        }

        public void FireEmployee(CreatureAI Employee)
        {
            PlayerFaction.Minions.Remove(Employee);
            PersistentData.SelectedMinions.Remove(Employee);
            PlayerFaction.AddMoney(-(decimal)(Employee.Stats.CurrentLevel.Pay * 4));
        }

        public int CalculateSupervisionCap() // Todo: Cache these somewhere and only calculate once per frame.
        {
            return PlayerFaction.Minions.Sum(c => c.Stats.CurrentClass.Managerial ? (int)c.Stats.Intelligence : 0) + 4;
        }

        public int CalculateSupervisedEmployees()
        {
            return PlayerFaction.Minions.Where(c => c.Stats.CurrentClass.RequiresSupervision).Count() + PersistentData.NewArrivals.Where(c => c.Applicant.Class.RequiresSupervision).Count();
        }

        public void PayEmployees()
        {
            DwarfBux total = 0;
            bool noMoney = false;
            foreach (CreatureAI creature in PlayerFaction.Minions)
            {
                if (creature.Stats.IsOverQualified)
                    creature.Creature.AddThought("I am overqualified for this job.", new TimeSpan(1, 0, 0, 0), -10.0f);

                if (creature.Physics.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                    thoughts.Thoughts.RemoveAll(thought => thought.Description.Contains("paid"));

                DwarfBux pay = creature.Stats.CurrentLevel.Pay;
                total += pay;

                if (total >= PlayerFaction.Economy.Funds)
                {
                    if (!noMoney)
                    {
                        MakeAnnouncement("We have no money!");
                        Tutorial("money");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                    }
                    noMoney = true;
                }
                else
                    creature.Creature.AddThought("I got paid recently.", new TimeSpan(1, 0, 0, 0), 10.0f);

                creature.AssignTask(new ActWrapperTask(new GetMoneyAct(creature, pay)) { AutoRetry = true, Name = "Get paid.", Priority = TaskPriority.High });
            }

            MakeAnnouncement(String.Format("We paid our employees {0} today.", total));
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.15f);
            Tutorial("pay");
        }

        public bool AreAllEmployeesAsleep()
        {
            return (PlayerFaction.Minions.Count > 0) && PlayerFaction.Minions.All(minion => !minion.Active || minion.Creature.Stats.IsAsleep || minion.IsDead);
        }
    }
}
