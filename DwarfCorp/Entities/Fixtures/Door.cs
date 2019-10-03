using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Door : CraftedFixture
    {
        public Faction TeamFaction { get; set; }
        public Matrix ClosedTransform { get; set; }
        public Timer OpenTimer { get; set; }
        bool IsOpen { get; set; }
        bool IsMoving { get; set; }

        public Door()
        {
            IsOpen = false;
        }

        public Door(
            ComponentManager manager, 
            Vector3 position, 
            Faction team, 
            SpriteSheet Asset,
            Point Frame,
            Resource RawMaterials, 
            string craftType, 
            float HP) :
            base(manager, position, Asset, Frame, new CraftDetails(manager, RawMaterials), SimpleSprite.OrientMode.Fixed)
        {
            IsMoving = false;
            IsOpen = false;
            OpenTimer = new Timer(0.5f, false);
            TeamFaction = team;
            Name = craftType;
            Tags.Add("Door");

            OrientToWalls();
            ClosedTransform = LocalTransform;
            CollisionType = CollisionType.Static;

            AddChild(new Health(manager, "Health", HP, 0.0f, HP));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
                sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI);
            }
        }

        public Matrix CreateHingeTransform(float angle)
        {
            Matrix toReturn = Matrix.Identity;
            Vector3 hinge = new Vector3(0, 0, 0.5f);
            toReturn = Matrix.CreateTranslation(hinge) * toReturn;
            toReturn = Matrix.CreateRotationY(angle) * toReturn;
            toReturn = Matrix.CreateTranslation(-hinge)* toReturn;
            return toReturn;
        }

        public void Open()
        {
            if (!IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_open_generic, Position, true, 0.5f);
            }

            IsOpen = true;
        }

        public void Close()
        {
            if (IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_close_generic, Position, true, 0.5f);
            }
            IsOpen = false;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!Active)
                return;

            if (IsMoving)
            {
                OpenTimer.Update(gameTime);
                if (OpenTimer.HasTriggered)
                    IsMoving = false;
                else
                {
                    float t = Easing.CubicEaseInOut(OpenTimer.CurrentTimeSeconds, 0.0f, 1.0f,
                        OpenTimer.TargetTimeSeconds);

                    if (GetComponent<SimpleSprite>().HasValue(out var sprite))
                    {
                        // Transform the sprite instead of the entire thing.
                        if (IsOpen)
                            sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI) * CreateHingeTransform(t * 1.57f);
                        else
                            sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI) * CreateHingeTransform((1.0f - t) * 1.57f);
                        sprite.ProcessTransformChange();
                    }
                }
            }
            else
            {
                bool anyInside = false;
                foreach (CreatureAI minion in TeamFaction.Minions)
                {
                    if ((minion.Physics.Position - Position).LengthSquared() < 1)
                    {
                        if (!IsOpen)
                            Open();
                        anyInside = true;
                        break;
                    }
                }

                if (!IsMoving && !anyInside && IsOpen)
                    Close();
            }
        }
    }
}
