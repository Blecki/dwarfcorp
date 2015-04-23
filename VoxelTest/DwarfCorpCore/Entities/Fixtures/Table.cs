using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Table : Body
    {
        public ManaBattery Battery { get; set; }

        public class ManaBattery
        {
            public ResourceEntity ManaSprite { get; set; }
            public float Charge { get; set; }
            public float MaxCharge { get; set; }
            public static float ChargeRate = 5.0f;
            public Timer ReCreateTimer { get; set; }
            public Timer ChargeTimer { get; set; }
            public ManaBattery()
            {
                ReCreateTimer = new Timer(3.0f, false);
                ChargeTimer = new Timer(1.0f, false);
            }

            public void Update(DwarfTime time)
            {
                ChargeTimer.Update(time);
                if (ManaSprite != null && Charge > 0.0f && PlayState.Master.Spells.Mana < PlayState.Master.Spells.MaxMana)
                {
                    if (ChargeTimer.HasTriggered)
                    {
                        SoundManager.PlaySound(ContentPaths.Audio.tinkle, ManaSprite.Position);
                        IndicatorManager.DrawIndicator("+" + (int)ChargeRate + " M", ManaSprite.Position, 1.0f, Color.Green);
                        PlayState.ParticleManager.Trigger("star_particle", ManaSprite.Position, Color.White, 1);
                        Charge -= ChargeRate;
                        PlayState.Master.Spells.Recharge(ChargeRate);
                    }

                }
                else if (Charge <= 0.01f)
                {
                    ReCreateTimer.Update(time);
                }
            }

            public bool CreateNewManaSprite(Faction faction, Vector3 position)
            {
                if (ManaSprite != null)
                {
                    ManaSprite.Die();  
                    PlayState.ParticleManager.Trigger("star_particle", position, Color.White, 5);
                    SoundManager.PlaySound(ContentPaths.Audio.wurp, position, true);
                    ManaSprite = null;
                    ReCreateTimer.Reset();
                }
                if (ReCreateTimer.HasTriggered)
                {
                    if (faction.RemoveResources(
                        new List<ResourceAmount>() {new ResourceAmount(ResourceLibrary.ResourceType.Mana)}, position + Vector3.Up * 0.5f))
                    {
                        ManaSprite = EntityFactory.CreateEntity<ResourceEntity>("Mana Resource", position);
                        ManaSprite.Gravity = Vector3.Zero;
                        ManaSprite.CollideMode = Physics.CollisionMode.None;
                        
                        ManaSprite.Tags.Clear();
                        return true;
                    }
                }

                return false;
            }

            public void Reset(Faction faction, Vector3 position)
            {
                if (CreateNewManaSprite(faction, position))
                {
                    Charge = MaxCharge;
                }
            }
        }

        public Table()
        {
            
        }

        public Table(Vector3 position):
            this(position, null, Point.Zero)
        {
            
        }

        public Table(Vector3 position, string asset) :
            this(position, new SpriteSheet(asset), Point.Zero)
        {

        }

        public override void Update(DwarfTime time, ChunkManager chunks, Camera camera)
        {
            if (Battery != null)
            {
                Battery.Update(time);

                if(Battery.Charge <= 0)
                {
                    Battery.Reset(PlayState.PlayerFaction, Position + Vector3.Up);
                }
            }
            base.Update(time, chunks, camera);
        }

        public Table(Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame) :
            base("Table", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            Point topFrame = new Point(0, 6);
            Point sideFrame = new Point(1, 6);

            List<Point> frames = new List<Point>
            {
                topFrame
            };

            List<Point> sideframes = new List<Point>
            {
                sideFrame
            };

            Animation tableTop = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "tableTop", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);
            Animation tableAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "tableTop", 32, 32, sideframes, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite tabletopSprite = new Sprite(componentManager, "sprite1", this, Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            tabletopSprite.AddAnimation(tableTop);

            Sprite sprite = new Sprite(componentManager, "sprite", this, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", this, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }


            tableAnimation.Play();
            Tags.Add("Table");
            CollisionType = CollisionManager.CollisionType.Static;

            if (fixtureAsset != null)
            {
                new Fixture(new Vector3(0, 0.3f, 0), fixtureAsset, fixtureFrame, this);
            }
        }
    }
}
