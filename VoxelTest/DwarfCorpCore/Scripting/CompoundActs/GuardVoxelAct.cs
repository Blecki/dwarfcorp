using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel, and then waits there until cancelled.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GuardVoxelAct : CompoundCreatureAct
    {
        public Voxel Voxel { get; set; }


        public GuardVoxelAct()
        {

        }

        public bool LoopCondition()
        {
            return Agent.Faction.IsGuardDesignation(Voxel) && !EnemiesNearby() && !Creature.Status.Energy.IsUnhappy() && !Creature.Status.Hunger.IsUnhappy();
        }

        public bool GuardDesignationExists()
        {
            return Agent.Faction.IsGuardDesignation(Voxel);
        }

        public bool ExitCondition()
        {
            if (EnemiesNearby())
            {
                Creature.AI.OrderEnemyAttack();
            }

            return !GuardDesignationExists();
        }


        public bool EnemiesNearby()
        {
            return (Agent.Sensor.Enemies.Count > 0);
        }

        public GuardVoxelAct(CreatureAI agent, Voxel voxel) :
            base(agent)
        {
            Voxel = voxel;
            Name = "Guard Voxel " + voxel;

            Tree = new Sequence
                (
                    new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, agent),
                    new StopAct(Agent),
                    new WhileLoop(new WanderAct(Agent, 1.0f, 0.5f, 0.1f), new Condition(LoopCondition)),
                    new Condition(ExitCondition)
                );
        }
    }

}