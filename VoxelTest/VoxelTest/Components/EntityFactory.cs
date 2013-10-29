using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    class EntityFactory
    {
        public static InstanceManager instanceManager = null;

        public static LocatableComponent CreateBalloon(Vector3 target, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ShipmentOrder order, GameMaster master)
        {
            PhysicsComponent balloon = new PhysicsComponent(componentManager, "Balloon", componentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0), 4, 1, 0.99f, 0.99f, Vector3.Zero);
            //CreateIntersectingBillboard(balloon, content.Load<Texture2D>("balloon"), 1.3f, 4, Vector3.Zero, componentManager, content, graphics);
            //balloon.DrawBoundingBox = true;
            balloon.OrientWithVelocity = false;
            balloon.FixedOrientation = true;
    
            Texture2D tex = content.Load<Texture2D>("balloon");
            List<Point> points = new List<Point>();
            points.Add(new Point(0, 0));
            Animation BalloonAnimation = new Animation(graphics, content.Load<Texture2D>("balloon"), "balloon", points, false, Color.White, 0.001f, false);
            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", balloon, Matrix.Identity, content.Load<Texture2D>("balloon"), false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(BalloonAnimation);
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            ShadowComponent shadow = new ShadowComponent(componentManager, "shadow", balloon, shadowTransform, content.Load<Texture2D>("shadowcircle"));

            BalloonAI balloonAI = new BalloonAI(balloon, target, order, master);

            return balloon;
        }

      
        public static GameComponent GenerateVegetation(string id, float size, float offset, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            if (id == "pine" || id == "snowpine" || id == "palm")
            {
                return GenerateTree(size, position + new Vector3(0.5f, size * offset, 0.5f), componentManager, content, graphics, id);
            }
            else if (id == "berrybush")
            {
                return GenerateBerryBush(size, position + new Vector3(0.5f, size * offset - 0.1f, 0.5f), componentManager, content, graphics);
            }
            else if (id == "apple_tree")
            {
                return GenerateAppleTree(size, position + new Vector3(0.5f, size * offset, 0.5f), componentManager, content, graphics);
            }
            else
            {
                return null;
            }
        }

        public static string[] ComponentList =  { "Balloon", "Wood", "Iron", "Dirt", "Stone", "Gold",
            "Mana", "Apple", "pine", "snowpine", "palm", "berrybush", "apple_tree", "Dwarf",
            "DarkDwarf", "Goblin", "Bed", "Lamp", "Table", "Chair", "Flag", "SpikeTrap", "Mushroom", "Wheat", "Potion", "Book", "BookTable", "PotionTable"};

        public static LocatableComponent GenerateComponent(string id, Vector3 position,
            ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, GameMaster master, Camera camera)
        {
            if (id == "Balloon")
            {
                return CreateBalloon(position + new Vector3(0, 100, 0), position + new Vector3(0, 2, 0), componentManager, content, graphics, new ShipmentOrder(0, null), master);
            }
            if (id == "Wood")
            {
                return GenerateWoodResource(position, componentManager, content, graphics);
            }
            else if (id == "Iron")
            {
                return GenerateIronResource(position, componentManager, content, graphics);
            }
            else if (id == "Dirt")
            {
                return GenerateDirtResource(position, componentManager, content, graphics);
            }
            else if (id == "Stone")
            {
                return GenerateStoneResource(position, componentManager, content, graphics);
            }
            else if (id == "Gold")
            {
                return GenerateGoldResource(position, componentManager, content, graphics);
            }
            else if (id == "Mana")
            {
                return GenerateManaResource(position, componentManager, content, graphics);
            }
            else if (id == "Apple")
            {
                return GenerateAppleResource(position, componentManager, content, graphics);
            }
            else if (id == "pine" || id == "berrybush" || id == "apple_tree" || id == "snowpine" || id == "palm")
            {
                float s = (float)PlayState.random.NextDouble() * 0.8f + 0.5f;
                return (LocatableComponent)GenerateVegetation(id, s, 1.0f, position, componentManager, content, graphics);
            }
            else if (id == "Dwarf")
            {
                return (LocatableComponent)GenerateDwarf(position, componentManager, content, graphics, chunks, camera, master, PlayState.planService, "Dwarf");
            }
            else if (id == "DarkDwarf")
            {
                return (LocatableComponent)GenerateDwarf(position, componentManager, content, graphics, chunks, camera, master, PlayState.planService, "Undead");
            }
            else if (id == "Goblin")
            {
                return (LocatableComponent)GenerateGoblin(position, componentManager, content, graphics, chunks, camera, master, PlayState.planService, "Goblin");
            }
            else if (id == "Bed")
            {
                return GenerateBed(position, componentManager, content, graphics);
            }
            else if (id == "Lamp")
            {
                return (LocatableComponent)GenerateLamp(position, componentManager, content, graphics);
            }
            else if (id == "Table")
            {
                return (LocatableComponent)GenerateTable(position, componentManager, content, graphics);
            }
            else if (id == "Chair")
            {
                return (LocatableComponent)GenerateChair(position, componentManager, content, graphics);
            }
            else if (id == "Flag")
            {
                return (LocatableComponent)GenerateFlag(position, componentManager, content, graphics);
            }
            else if (id == "Wheat")
            {
                return (LocatableComponent)GenerateWheat(position, componentManager, content, graphics);
            }
            else if (id == "Mushroom")
            {
                return (LocatableComponent)GenerateMushroom(position, componentManager, content, graphics);
            }
            else if (id == "SpikeTrap")
            {
                return (LocatableComponent)GenerateSpikeTrap(position, componentManager, content, graphics);
            }
            else if (id == "BookTable")
            {
                return (LocatableComponent)GenerateBookTable(position, componentManager, content, graphics);
            }
            else if (id == "PotionTable")
            {
                return (LocatableComponent)GeneratePotionTable(position, componentManager, content, graphics);
            }
            else if (id == "Book")
            {
                return (LocatableComponent)GenerateBook(position, componentManager, content, graphics, componentManager.RootComponent);
            }
            else if (id == "Potion")
            {
                return (LocatableComponent)GeneratePotions(position, componentManager, content, graphics, componentManager.RootComponent);
            }
            else
            {
                return null;
            }

        }

        public static LocatableComponent GenerateManaResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Stone = new PhysicsComponent(componentManager, "Mana Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(1, 0));
            Animation StoneAnimation = new Animation(graphics, spriteSheet, "Mana", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Stone, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(StoneAnimation);
            sprite.LightsWithVoxels = false;

            StoneAnimation.Play();

            Stone.Tags.Add("Mana");
            Stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Stone;
        }

        public static LocatableComponent GenerateStoneResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Stone = new PhysicsComponent(componentManager, "Stone Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(3, 0));
            Animation StoneAnimation = new Animation(graphics, spriteSheet, "Stone", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Stone, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(StoneAnimation);


            StoneAnimation.Play();

            Stone.Tags.Add("Stone");
            Stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Stone;
        }

        public static LocatableComponent GenerateGoldResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Stone = new PhysicsComponent(componentManager, "Gold Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 0));
            Animation StoneAnimation = new Animation(graphics, spriteSheet, "Gold", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Stone, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(StoneAnimation);


            StoneAnimation.Play();

            Stone.Tags.Add("Gold");
            Stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Stone;
        }

        public static LocatableComponent GenerateIronResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Stone = new PhysicsComponent(componentManager, "Iron Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(2, 0));
            Animation StoneAnimation = new Animation(graphics, spriteSheet, "Iron", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Stone, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(StoneAnimation);


            StoneAnimation.Play();

            Stone.Tags.Add("Iron");
            Stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Stone;
        }


        public static List<InstanceData> GenerateGrassMotes(List<Vector3> positions,
                                                            List<Color> colors,
                                                            List<float> scales,
            ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, List<InstanceData> motes, string asset)
        {


            if (!GameSettings.Default.GrassMotes)
            {
                return null;
            }

            try
            {
                Texture2D spriteSheet = content.Load<Texture2D>(asset);


                instanceManager.RemoveInstances(asset, motes); 

                motes.Clear();

                Vector3 avg = Vector3.Zero;
                Vector3 min = Vector3.Zero;
                Vector3 max = Vector3.Zero;
                float minNorm = float.MaxValue;
                float maxNorm = float.MinValue;
                foreach (Vector3 p in positions)
                {
                    avg += p;
                    if (p.LengthSquared() > maxNorm)
                    {
                        max = p;
                        maxNorm = p.LengthSquared();
                    }
                    else if (p.LengthSquared() < minNorm)
                    {
                        min = p;
                        minNorm = p.LengthSquared();
                    }

                }

                if (positions.Count > 0)
                {
                    avg /= positions.Count;
                }

             

                for (int i = 0; i < positions.Count; i++)
                {
                    float rot = scales[i] * scales[i];
                    Matrix trans = Matrix.CreateTranslation(positions[i]);
                    Matrix scale = Matrix.CreateScale(scales[i]);
                    motes.Add(new InstanceData(scale * Matrix.CreateRotationY((float)(rot)) *  trans,  colors[i], true));
                }

                foreach (InstanceData data in motes)
                {
                    if (data != null)
                    {
                        instanceManager.AddInstance(asset, data);
                    }
                }

                return motes;
            }
            catch (ContentLoadException e)
            {
                throw e;
            }
        
        }

        public static LocatableComponent GenerateDirtResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Dirt = new PhysicsComponent(componentManager, "Dirt Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 1));
            Animation DirtAnimation = new Animation(graphics, spriteSheet, "Dirt", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Dirt, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(DirtAnimation);


            DirtAnimation.Play();

            Dirt.Tags.Add("Dirt");
            Dirt.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Dirt;
        }

        public static LocatableComponent GenerateWoodResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent Wood = new PhysicsComponent(componentManager, "Wood Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));
            
            List<Point> frames = new List<Point>();
            frames.Add(new Point(3, 1));
            Animation WoodAnimation = new Animation(graphics, spriteSheet, "Wood", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", Wood, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(WoodAnimation);


            WoodAnimation.Play();

            Wood.Tags.Add("Wood");
            Wood.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);
            return Wood;
        }
        
        public static LocatableComponent GenerateAppleResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent apple = new PhysicsComponent(componentManager, "Apple Resource", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>();
            frames.Add(new Point(2, 1));
            Animation appleAnimation = new Animation(graphics, spriteSheet, "Apple", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", apple, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(appleAnimation);

            FoodComponent food = new FoodComponent(componentManager, "Food", apple, 15f); 

            appleAnimation.Play();

            apple.Tags.Add("Apple");
            apple.Tags.Add("Resource");
            apple.Tags.Add("Food");

            SinMover sinMover = new SinMover(0.05f, 2.0f, (float)PlayState.random.NextDouble() * 3.0f, sprite);

            return apple;
        }

        public static LocatableComponent GenerateChair(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            return (LocatableComponent)GenerateTableLike(position + new Vector3(0, -0.1f, 0), componentManager, content, graphics,  new Point(2, 6), new Point(3, 6));
        }



        public static LocatableComponent GenerateBed(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>("bedtex");

            LocatableComponent bed = new LocatableComponent(componentManager, "bed", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f));
            BedComponent logicalBed = new BedComponent(componentManager, "Bed", bed, 1);
            TexturedBoxObject bedModel = new TexturedBoxObject(componentManager, "bedbox", bed, Matrix.Identity, new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), PrimitiveLibrary.BoxPrimitives["bed"], spriteSheet);

            bed.Tags.Add("Bed");

            return bed;
        }

        public static GameComponent GenerateAnvil(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Anvil", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 3));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Anvil", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Anvil");

            return table;

        }

        public static GameComponent GenerateTarget(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Target", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 5));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Target", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Target");

            return table;

        }

        public static GameComponent GenerateStrawman(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Strawman", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(1, 5));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Strawman", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Strawman");

            return table;

        }

        public static GameComponent GenerateWheat(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>("wheat");
            LocatableComponent table = new LocatableComponent(componentManager, "wheat", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 0));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "wheat", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Wheat");

            return table;

        }

        public static GameComponent GenerateMushroom(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>("shroom");
            LocatableComponent table = new LocatableComponent(componentManager, "wheat", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 0));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "wheat", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", table, Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false);
            sprite2.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            table.Tags.Add("Mushroom");

            return table;

        }

        public static GameComponent GenerateSpikeTrap(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "spikes", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(2, 4));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "spikes", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", table, Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false);
            sprite2.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            table.Tags.Add("Trap");
            table.Tags.Add("Spikes");

            return table;

        }

        public static GameComponent GenerateTableLike(Vector3 position,
                                                      ComponentManager componentManager,
                                                      ContentManager content,
                                                      GraphicsDevice graphics,
                                                       Point topFrame, Point sideFrame)
        {
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "table", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);

            List<Point> frames = new List<Point>();
            frames.Add(topFrame);

            List<Point> sideframes = new List<Point>();
            sideframes.Add(sideFrame);

            Animation tableTop = new Animation(graphics, spriteSheet, "tableTop", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);
            Animation tableAnimation = new Animation(graphics, spriteSheet, "tableTop", 32, 32, sideframes, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent tabletopSprite = new BillboardSpriteComponent(componentManager, "sprite1", table, Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, false);
            tabletopSprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            tabletopSprite.AddAnimation(tableTop);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false);
            sprite2.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            table.Tags.Add("Table");

            return table;
        }

        public static GameComponent GenerateTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {

            return GenerateTableLike(position + new Vector3(0, 0.2f, 0), componentManager, content, graphics,  new Point(0, 6), new Point(1, 6));
           
        }

       


        public static GameComponent GenerateBook(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, GameComponent parent)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 4));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Book", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", parent, matrix, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            sprite.Tags.Add("Book");
            sprite.DrawInFrontOfSiblings = true;
            return sprite;
        }

        public static GameComponent GenerateBookTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            LocatableComponent table = (LocatableComponent)GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("BookTable");
            GameComponent book = GenerateBook(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);

            table.UpdateTransformsRecursive();
            table.DrawInFrontOfSiblings = true;
            return table;

        }

        public static GameComponent GeneratePotions(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, GameComponent parent)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            List<Point> frames = new List<Point>();
            frames.Add(new Point(1, 4));
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Potion", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", parent, matrix, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            sprite.Tags.Add("Potion");
            sprite.DrawInFrontOfSiblings = true;
            return sprite;

        }

        public static GameComponent GeneratePotionTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            LocatableComponent table = (LocatableComponent)GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("PotionTable");
            GameComponent potion = GeneratePotions(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);
            
            table.UpdateTransformsRecursive();
            return table;

        }

        public static GameComponent GenerateFlag(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent flag = new LocatableComponent(componentManager, "flag", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 2));
            frames.Add(new Point(1, 2));
            frames.Add(new Point(2, 2));
            frames.Add(new Point(1, 2));
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Flag", 32, 32, frames, true, Color.White, 5.0f + (float)PlayState.random.NextDouble(), 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", flag, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.YAxis;
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            flag.Tags.Add("Flag");


            return flag;

        }

        public static GameComponent GenerateLamp(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent lamp = new LocatableComponent(componentManager, "lamp", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(0, 1));
            frames.Add(new Point(2, 1));
            frames.Add(new Point(1, 1));
            frames.Add(new Point(2, 1));
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Lamp", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false);
            sprite.LightsWithVoxels = false;
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.YAxis;
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Lamp");

            LightComponent light = new LightComponent(componentManager, "light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8);
            light.HasMoved = true;

            return lamp;

        }

        public static GameComponent GenerateForge(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent lamp = new LocatableComponent(componentManager, "Forge", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>();
            frames.Add(new Point(1, 3));
            frames.Add(new Point(3, 3));
            frames.Add(new Point(2, 3));
            frames.Add(new Point(3, 3));
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Forge", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false);
            sprite.LightsWithVoxels = false;
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Forge");

            LightComponent light = new LightComponent(componentManager, "light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4);
            light.HasMoved = true;

            return lamp;

        }

        public static GameComponent GenerateTree(float treeSize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, string asset)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>(asset);
            LocatableComponent tree = new LocatableComponent(componentManager, "Tree", componentManager.RootComponent, matrix, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), Vector3.Zero);
            ModelInstanceComponent modelInstance = new ModelInstanceComponent(componentManager, "Model", tree, Matrix.CreateRotationY((float)(PlayState.random.NextDouble() * Math.PI)) * Matrix.CreateScale(treeSize * 4, treeSize * 4, treeSize * 4), asset, false);
            //CreateIntersectingBillboard(tree, spriteSheet, treeSize, treeSize, position, componentManager, content, graphics);
            
          

            HealthComponent health = new HealthComponent(componentManager, "Health", tree, 100.0f * treeSize, 0.0f, 100.0f * treeSize);

            FlammableComponent flame = new FlammableComponent(componentManager, "Flames", tree, health);


            tree.Tags.Add("Tree");
            tree.Tags.Add("EmitsWood");

            List<LocatableComponent> Woods = new List<LocatableComponent>();

            for (int i = 0; i < (int)(treeSize * 10); i++)
            {
                Woods.Add(GenerateWoodResource(new Vector3((float)(PlayState.random.NextDouble() * treeSize * 2) - treeSize, (float)(PlayState.random.NextDouble() * treeSize ) - treeSize * 0.5f, (float)(PlayState.random.NextDouble() * treeSize * 2) - treeSize ) + position, componentManager, content, graphics));
            }

            foreach(LocatableComponent Wood in Woods)
            {
                Wood.SetVisibleRecursive(false);
                Wood.SetActiveRecursive(false);
                //GameComponent removed = null;
                //while (!componentManager.RootComponent.Children.TryRemove(Wood.LocalID, out removed)) { }

                //componentManager.RemoveComponent(Wood);
            }

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(treeSize * 2, treeSize, treeSize * 2), Vector3.Zero, Woods);

            EmitterComponent emitter = new EmitterComponent("Leaves", componentManager, "LeafEmitter", tree, Matrix.Identity, new Vector3(treeSize * 2, treeSize, treeSize * 2), Vector3.Zero);

            tree.AddToOctree = true;
            return tree;
            
        }

        public static GameComponent GenerateBerryBush(float bushSize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>("berrybush");
            LocatableComponent tree = new LocatableComponent(componentManager, "Bush", componentManager.RootComponent, matrix, new Vector3(bushSize , bushSize , bushSize ), Vector3.Zero);
            ModelInstanceComponent modelInstance = new ModelInstanceComponent(componentManager, "Model", tree, Matrix.CreateScale(bushSize , bushSize , bushSize ), "berrybush", false);

            HealthComponent health = new HealthComponent(componentManager, "Health", tree, 30 * bushSize, 0.0f, 30 * bushSize);


            tree.Tags.Add("Tree");
            tree.Tags.Add("Bush");
            tree.Tags.Add("EmitsFood");



            List<LocatableComponent> apples = new List<LocatableComponent>();

            for (int i = 0; i < (int)(bushSize * 5); i++)
            {
                apples.Add(GenerateAppleResource(new Vector3((float)(PlayState.random.NextDouble() * bushSize * 4) - bushSize * 2, (float)(PlayState.random.NextDouble() * bushSize * 2) - bushSize, (float)(PlayState.random.NextDouble() * bushSize * 4) - bushSize * 2) + position, componentManager, content, graphics));
            }

            foreach (LocatableComponent apple in apples)
            {
                apple.SetVisibleRecursive(false);
                apple.SetActiveRecursive(false);

                /*
                GameComponent removed = null;
                while (!componentManager.RootComponent.Children.TryRemove(apple.LocalID, out removed)) { }
                */

                //componentManager.RemoveComponent(apple);
                 
            }

            apples.AddRange(apples);

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(bushSize * 4, bushSize * 2, bushSize * 4), Vector3.Zero, apples);

            tree.AddToOctree = true;
            return tree;

        }

        public static GameComponent GenerateAppleTree(float treeSize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = content.Load<Texture2D>("apple_tree");
            LocatableComponent tree = new LocatableComponent(componentManager, "Tree", componentManager.RootComponent, matrix, new Vector3(treeSize * 4, treeSize * 6, treeSize * 4), Vector3.Zero);

            CreateIntersectingBillboard(tree, spriteSheet, treeSize * 3, treeSize * 7, position, componentManager, content, graphics);



            HealthComponent health = new HealthComponent(componentManager, "Health", tree, 100.0f * treeSize, 0.0f, 100.0f * treeSize);


            tree.Tags.Add("Tree");
            tree.Tags.Add("EmitsWood");
            tree.Tags.Add("EmitsFood");

            List<LocatableComponent> Woods = new List<LocatableComponent>();

            for (int i = 0; i < (int)(treeSize * 5); i++)
            {
                Woods.Add(GenerateWoodResource(new Vector3((float)(PlayState.random.NextDouble() * treeSize * 4) - treeSize * 2, (float)(PlayState.random.NextDouble() * treeSize * 2) - treeSize, (float)(PlayState.random.NextDouble() * treeSize * 4) - treeSize * 2) + position, componentManager, content, graphics));
            }

            foreach (LocatableComponent Wood in Woods)
            {
                Wood.SetVisibleRecursive(false);
                Wood.SetActiveRecursive(false);

                /*
                GameComponent removed = null;
                while (!componentManager.RootComponent.Children.TryRemove(Wood.LocalID, out removed)) { }

                componentManager.RemoveComponent(Wood);
                 */
            }

            List<LocatableComponent> apples = new List<LocatableComponent>();

            for (int i = 0; i < (int)(treeSize * 10); i++)
            {
                apples.Add(GenerateAppleResource(new Vector3((float)(PlayState.random.NextDouble() * treeSize * 4) - treeSize * 2, (float)(PlayState.random.NextDouble() * treeSize * 2) - treeSize, (float)(PlayState.random.NextDouble() * treeSize * 4) - treeSize * 2) + position, componentManager, content, graphics));
            }

            foreach (LocatableComponent apple in apples)
            {
                apple.SetVisibleRecursive(false);
                apple.SetActiveRecursive(false);

                /*
                GameComponent removed = null;
                while (!componentManager.RootComponent.Children.TryRemove(apple.LocalID, out removed)) { }

                componentManager.RemoveComponent(apple);
                 */
            }

            Woods.AddRange(apples);

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(treeSize * 4, treeSize * 2, treeSize * 4), Vector3.Zero, Woods);

            tree.AddToOctree = true;
            return tree;

        }

        public static void CreateIntersectingBillboard(GameComponent component, Texture2D spriteSheet, float xSize, float ySize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            BatchBillboard billboard = new BatchBillboard(componentManager, "BatchBillboard", component, Matrix.Identity, spriteSheet, 2, graphics);
            billboard.Primitive = PrimitiveLibrary.BatchBillboardPrimitives["tree"];
            billboard.LightsWithVoxels = true;
            billboard.CullDistance = 70 * 70;
            billboard.LocalTransform = Matrix.CreateScale(xSize * 4, ySize * 4, xSize * 4);

            /*
            for (int i = 0; i < 4; i++)
            {
                List<Point> frames = new List<Point>();
                if (i < 2)
                {
                    frames.Add(new Point(0, 0));
                }
                else
                {
                    frames.Add(new Point(1, 0));
                }

                bool shouldFlip = (i < 2 && i % 2 != 0) || (i >= 2 && i % 2 == 0);


                Animation animation = new Animation(graphics, spriteSheet, "Sprite", spriteSheet.Width / 2, spriteSheet.Height, frames, false, Color.White, 0.01f, 1.0f, 1.0f, shouldFlip);
                Matrix otherSpriteMatrix = Matrix.CreateRotationY(1.57f * i);
                otherSpriteMatrix *= Matrix.CreateScale(new Vector3(xSize, ySize, xSize));
                otherSpriteMatrix.Translation = new Vector3((float)Math.Cos(1.57f * i) * (xSize * 0.5f), 0, (float)Math.Sin(1.57f * i) * (xSize * 0.5f));
                

                BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", component, otherSpriteMatrix, spriteSheet, false);

                sprite.OrientationType = BillboardSpriteComponent.OrientMode.Fixed;
                sprite.AddAnimation(animation);
                animation.Play();
            }
             */
        }


        public static LocatableComponent GenerateGoblin(Vector3 position,
                                                  ComponentManager componentManager,
                                                  ContentManager content,
                                                  GraphicsDevice graphics,
                                                  ChunkManager chunkManager, Camera camera,
                                                  GameMaster master, PlanService planService, string allies)
        {

            CreatureStats stats = new CreatureStats();
            stats.BaseDigSpeed = 20.0f;
            stats.MaxAcceleration = 8.0f;
            stats.StoppingForce = 60.0f;
            stats.MaxSpeed = 5.0f;
            stats.JumpForce = 1000.0f;
            stats.BaseChopSpeed = 5.0f;
            stats.MaxHealth = 100.0f;
            stats.EnergyLoss = 0.0005f;
            stats.EnergyRecharge = 0.1f;
            stats.EnergyRechargeBed = 0.005f;
            stats.HungerIncrease = 0.005f;
            stats.HungerThreshold = 0.8f;
            stats.SleepyThreshold = 0.15f;
            stats.PlanRateLimit = 0.1f;
            stats.MaxExpansions = 20000;
            return new Goblin(stats, allies, planService, master, componentManager, "Goblin", chunkManager, graphics, content, TextureManager.GetTexture("GoblinSheet"), position).Physics;
        }

        public static GameComponent GenerateDwarf(Vector3 position,
                                                  ComponentManager componentManager,
                                                  ContentManager content,
                                                  GraphicsDevice graphics, 
                                                  ChunkManager chunkManager, Camera camera,
                                                  GameMaster master, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats();
            stats.BaseDigSpeed = 20.0f;
            stats.MaxAcceleration = 8.0f;
            stats.StoppingForce = 60.0f;
            stats.MaxSpeed = 5.0f;
            stats.JumpForce = 1000.0f;
            stats.BaseChopSpeed = 5.0f;
            stats.MaxHealth = 100.0f;
            stats.EnergyLoss = 0.0005f;
            stats.EnergyRecharge = 0.1f;
            stats.EnergyRechargeBed = 0.005f;
            stats.HungerIncrease = 0.005f;
            stats.HungerThreshold = 0.8f;
            stats.SleepyThreshold = 0.15f;
            stats.PlanRateLimit = 0.1f;
            stats.MaxExpansions = 20000;
            return new Dwarf(stats, allies, planService, master, componentManager, "Dwarf", chunkManager, graphics, content, TextureManager.GetTexture("DwarfSheet"), position).Physics;
        }
    }
}
