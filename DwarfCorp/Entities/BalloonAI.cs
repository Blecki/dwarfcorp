using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp
{
    public class BalloonAI : GameComponent
    {
        public PIDController VelocityController = new PIDController(0.9f, 0.5f, 0.0f);
        public Vector3 TargetPosition;
        public float MaxVelocity = 2.0f;
        public float MaxForce = 15.0f;
        public BalloonState State = BalloonState.DeliveringGoods;
        public Faction Faction;
        public Timer WaitTimer = new Timer(5.0f, true);

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

        public BalloonAI(ComponentManager Manager, Vector3 target, Faction faction) :
            base("BalloonAI", Manager)
        {
            TargetPosition = target;
            Faction = faction;
        }

        public override void Die()
        {
            if (!IsDead)
                Parent.Die();
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            var body = Parent as GameComponent;
            global::System.Diagnostics.Debug.Assert(body != null);

            Vector3 targetVelocity = TargetPosition - body.GlobalTransform.Translation;

            if(targetVelocity.LengthSquared() > 0.0001f)
            {
                targetVelocity.Normalize();
                targetVelocity *= MaxVelocity;
            }

            Matrix m = body.LocalTransform;
            m.Translation += targetVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            body.LocalTransform = m;

            body.HasMoved = true;

            switch(State)
            {
                case BalloonState.DeliveringGoods:
                    {
                        var voxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(body.GlobalTransform.Translation));

                        if (voxel.IsValid)
                        {
                            var surfaceVoxel = VoxelHelpers.FindFirstVoxelBelow(voxel);
                            var height = surfaceVoxel.Coordinate.Y + 1;

                            TargetPosition = new Vector3(body.GlobalTransform.Translation.X, height + 5, body.GlobalTransform.Translation.Z);

                            Vector3 diff = body.GlobalTransform.Translation - TargetPosition;

                            if (diff.LengthSquared() < 2)
                                State = BalloonState.Waiting;
                        }
                        else
                            State = BalloonState.Leaving;
                    }
                    break;
                case BalloonState.Leaving:
                    TargetPosition = Vector3.UnitY * 100 + body.GlobalTransform.Translation;

                    if(body.GlobalTransform.Translation.Y > World.WorldSizeInVoxels.Y + 2)
                        Die();

                    break;
                case BalloonState.Waiting:
                    TargetPosition = body.GlobalTransform.Translation;
                    if (!WaitTimer.HasTriggered)
                    {
                        var voxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(body.GlobalTransform.Translation));

                        if (voxel.IsValid)
                        {
                            var surfaceVoxel = VoxelHelpers.FindFirstVoxelBelow(voxel);
                            var height = surfaceVoxel.Coordinate.Y + 6;

                            TargetPosition = new Vector3(body.GlobalTransform.Translation.X, height + 0.5f * (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds), body.GlobalTransform.Translation.Z);
                        }
                        WaitTimer.Update(gameTime);
                        break;
                    }

                    if (!shipmentGiven)
                        shipmentGiven = true;
                    else
                        State = BalloonState.Leaving;

                    break;
            }
        }
    }
}