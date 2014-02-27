using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A simple hacked AI script for the DwarfCorp balloon. Has a state machine which makes it go up and down.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BalloonAI : GameComponent
    {
        public PhysicsComponent Physics { get; set; }
        public PIDController VelocityController { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float MaxVelocity { get; set; }
        public float MaxForce { get; set; }
        public BalloonState State { get; set; }
        public ShipmentOrder Shipment { get; set; }
        public Faction Faction { get; set; }

        public List<ResourceAmount> CurrentResources { get; set; }

        private bool shipmentGiven = false;

        public enum BalloonState
        {
            DeliveringGoods,
            Waiting,
            Leaving
        }

        public BalloonAI()
        {
            
        }

        public BalloonAI(PhysicsComponent physics, Vector3 target, ShipmentOrder shipment, Faction faction) :
            base(physics.Manager, "BalloonAI", physics)
        {
            Physics = physics;
            VelocityController = new PIDController(0.9f, 0.5f, 0.0f);
            MaxVelocity = 5.0f;
            MaxForce = 15.0f;
            TargetPosition = target;
            State = BalloonState.DeliveringGoods;
            Shipment = shipment;
            Faction = faction;
            CurrentResources = new List<ResourceAmount>();
        }

        public override void Die()
        {
            if(!IsDead)
            {
                Parent.Die();
            }
        }

        public void PutResource(LocatableComponent loc)
        {
            string resourceName = loc.Tags[0];

            /*
            foreach(ResourceAmount r in CurrentResources.Where(r => r.ResourceType.ResourceName == resourceName))
            {
                r.NumResources++;
                return;
            }
             */

            ResourceAmount newResource = new ResourceAmount
            {
                NumResources = 1,
                ResourceType = ResourceLibrary.Resources[resourceName]
            };

            CurrentResources.Add(newResource);
        }

        public float GetSellOrder(Item i)
        {
            return (from amount in Shipment.SellOrder
                where amount.ResourceType.ResourceName == i.UserData.Tags[0]
                select amount.ResourceType.MoneyValue * Faction.Economy.SellMultiplier).FirstOrDefault();
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            Vector3 targetVelocity = TargetPosition - Physics.GlobalTransform.Translation;

            if(targetVelocity.LengthSquared() > 0.0001f)
            {
                targetVelocity.Normalize();
                targetVelocity *= MaxVelocity;
            }

            Vector3 force = VelocityController.GetOutput((float) gameTime.ElapsedGameTime.TotalSeconds, targetVelocity, Physics.Velocity);

            if(force.Length() > MaxForce)
            {
                force.Normalize();
                force *= MaxForce;
            }

            Physics.ApplyForce(force, (float) gameTime.ElapsedGameTime.TotalSeconds);


            Physics.HasMoved = true;
            Physics.IsSleeping = false;


            switch(State)
            {
                case BalloonState.DeliveringGoods:
                    VoxelChunk chunk = chunks.ChunkData.GetVoxelChunkAtWorldLocation(Physics.GlobalTransform.Translation);

                    if(chunk != null)
                    {
                        Vector3 gridPos = chunk.WorldToGrid(Physics.GlobalTransform.Translation);
                        float height = chunk.GetFilledVoxelGridHeightAt((int) gridPos.X, (int) gridPos.Y, (int) gridPos.Z) + chunk.Origin.Y;
                        TargetPosition = new Vector3(Physics.GlobalTransform.Translation.X, height + 5, Physics.GlobalTransform.Translation.Z);

                        Vector3 diff = Physics.GlobalTransform.Translation - TargetPosition;

                        if(diff.LengthSquared() < 2)
                        {
                            State = BalloonState.Waiting;
                        }
                    }
                    else
                    {
                        TargetPosition = new Vector3(Physics.GlobalTransform.Translation.X, 0, Physics.GlobalTransform.Translation.Z);
                    }

                    break;
                case BalloonState.Leaving:
                    TargetPosition = Vector3.UnitY * 100 + Physics.GlobalTransform.Translation;

                    if(Physics.GlobalTransform.Translation.Y > 50)
                    {
                        Die();
                    }

                    break;
                case BalloonState.Waiting:
                    TargetPosition = Physics.GlobalTransform.Translation;

                    if(!shipmentGiven)
                    {
                        foreach(ResourceAmount amount in Shipment.BuyOrder)
                        {
                            for(int i = 0; i < amount.NumResources; i++)
                            {
                                Vector3 pos = Physics.GlobalTransform.Translation + new Vector3((float) PlayState.Random.NextDouble() - 0.5f, (float) PlayState.Random.NextDouble() - 0.5f, (float) PlayState.Random.NextDouble() - 0.5f) * 2;
                                LocatableComponent loc = EntityFactory.GenerateComponent(amount.ResourceType.ResourceName, pos, Manager, chunks.Content, chunks.Graphics, chunks, Manager.Factions, camera);
                                Faction.AddGatherDesignation(loc);
                                Faction.Economy.CurrentMoney -= amount.ResourceType.MoneyValue * Faction.Economy.BuyMultiplier;
                            }
                        }


                        shipmentGiven = true;
                    }
                    else
                    {
                        if(Shipment.Destination != null)
                        {
                            foreach(Item i in Shipment.Destination.ListItems())
                            {
                                if(i.UserData.CollisionType == CollisionManager.CollisionType.Dynamic)
                                {
                                    i.UserData.Die();
                                    Faction.Economy.CurrentMoney += GetSellOrder(i);
                                }
                            }
                            Shipment.Destination.ClearItems();
                        }
                        State = BalloonState.Leaving;
                    }


                    break;
            }


            base.Update(gameTime, chunks, camera);
        }
    }

}