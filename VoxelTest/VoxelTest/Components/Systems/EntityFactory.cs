using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// This class is used to create entities. It should probably be replaced with a more modular system (or a set of data files)
    /// Right now, its just an ugly class for initializing most of the entities in the game.
    /// </summary>
    internal class EntityFactory
    {
        public static InstanceManager instanceManager = null;

        public static LocatableComponent CreateBalloon(Vector3 target, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ShipmentOrder order, Faction master)
        {
            PhysicsComponent balloon = new PhysicsComponent(componentManager, "Balloon", componentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0), 4, 1, 0.99f, 0.99f, Vector3.Zero)
            {
                OrientWithVelocity = false,
                FixedOrientation = true
            };
            //CreateIntersectingBillboard(balloon, content.Load<Texture2D>("balloon"), 1.3f, 4, Vector3.Zero, componentManager, content, graphics);
            //balloon.DrawBoundingBox = true;

            Texture2D tex = TextureManager.GetTexture(ContentPaths.Entities.Balloon.Sprites.balloon);
            List<Point> points = new List<Point>
            {
                new Point(0, 0)
            };
            Animation balloonAnimation = new Animation(graphics, tex, "balloon", points, false, Color.White, 0.001f, false);
            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", balloon, Matrix.Identity, tex, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(balloonAnimation);

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
            ShadowComponent shadow = new ShadowComponent(componentManager, "shadow", balloon, shadowTransform, TextureManager.GetTexture(ContentPaths.Effects.shadowcircle));
            BalloonAI balloonAI = new BalloonAI(balloon, target, order, master);


            return balloon;
        }


        public static GameComponent GenerateVegetation(string id, float size, float offset, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            if(id == "pine" || id == "snowpine" || id == "palm")
            {
                return GenerateTree(size, position, offset, componentManager, content, graphics, id);
            }
            if(id == "berrybush")
            {
                return GenerateBerryBush(size, position, offset, componentManager, content, graphics);
            }
            return null;
        }

        public static string[] ComponentList =
        {
            "Balloon",
            "Wood",
            "Iron",
            "Dirt",
            "Stone",
            "Gold",
            "Mana",
            "Apple",
            "pine",
            "snowpine",
            "palm",
            "berrybush",
            "apple_tree",
            "Dwarf",
            "DarkDwarf",
            "Goblin",
            "Bed",
            "Lamp",
            "Table",
            "Chair",
            "Flag",
            "SpikeTrap",
            "Mushroom",
            "Wheat",
            "Potion",
            "Book",
            "BookTable",
            "PotionTable"
        };

        public static LocatableComponent GenerateComponent(string id, Vector3 position,
            ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, FactionLibrary factions, Camera camera)
        {
            switch(id)
            {
                case "Balloon":
                    return CreateBalloon(position, position + new Vector3(0, 2, 0), componentManager, content, graphics, new ShipmentOrder(0, null), factions.Factions["Player"]);
                case "Wood":
                    return GenerateWoodResource(position, componentManager, content, graphics);
                case "Iron":
                    return GenerateIronResource(position, componentManager, content, graphics);
                case "Dirt":
                    return GenerateDirtResource(position, componentManager, content, graphics);
                case "Stone":
                    return GenerateStoneResource(position, componentManager, content, graphics);
                case "Gold":
                    return GenerateGoldResource(position, componentManager, content, graphics);
                case "Mana":
                    return GenerateManaResource(position, componentManager, content, graphics);
                case "Apple":
                    return GenerateAppleResource(position, componentManager, content, graphics);
                case "palm":
                case "snowpine":
                case "apple_tree":
                case "berrybush":
                case "pine":
                {
                    float s = (float) PlayState.Random.NextDouble() * 0.8f + 0.5f;
                    return (LocatableComponent) GenerateVegetation(id, s, 1.0f, position, componentManager, content, graphics);
                }
                case "Dwarf":
                    return (LocatableComponent) GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Player"], PlayState.PlanService, "Dwarf");
                case "DarkDwarf":
                    return (LocatableComponent) GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Goblins"], PlayState.PlanService, "Undead");
                case "Goblin":
                    return (LocatableComponent)GenerateGoblin(position, componentManager, content, graphics, chunks, camera, factions.Factions["Goblins"], PlayState.PlanService, "Goblin");
                case "Bed":
                    return GenerateBed(position, componentManager, content, graphics);
                case "Lamp":
                    return (LocatableComponent) GenerateLamp(position, componentManager, content, graphics);
                case "Table":
                    return (LocatableComponent) GenerateTable(position, componentManager, content, graphics);
                case "Chair":
                    return (LocatableComponent) GenerateChair(position, componentManager, content, graphics);
                case "Flag":
                    return (LocatableComponent) GenerateFlag(position, componentManager, content, graphics);
                case "Wheat":
                    return (LocatableComponent) GenerateWheat(position, componentManager, content, graphics);
                case "Mushroom":
                    return (LocatableComponent) GenerateMushroom(position, componentManager, content, graphics);
                case "SpikeTrap":
                    return (LocatableComponent) GenerateSpikeTrap(position, componentManager, content, graphics);
                case "BookTable":
                    return (LocatableComponent) GenerateBookTable(position, componentManager, content, graphics);
                case "PotionTable":
                    return (LocatableComponent) GeneratePotionTable(position, componentManager, content, graphics);
                case "Book":
                    return (LocatableComponent) GenerateBook(position, componentManager, content, graphics, componentManager.RootComponent);
                case "Potion":
                    return (LocatableComponent) GeneratePotions(position, componentManager, content, graphics, componentManager.RootComponent);
                default:
                    return null;
            }
        }

        public static LocatableComponent GenerateManaResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent stone = new PhysicsComponent(componentManager, "Mana", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(1, 0)
            };
            Animation stoneAnimation = new Animation(graphics, spriteSheet, "Mana", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", stone, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(stoneAnimation);
            sprite.LightsWithVoxels = false;

            stoneAnimation.Play();

            stone.Tags.Add("Mana");
            stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return stone;
        }

        public static LocatableComponent GenerateStoneResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent stone = new PhysicsComponent(componentManager, "Stone", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(3, 0)
            };
            Animation stoneAnimation = new Animation(graphics, spriteSheet, "Stone", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", stone, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(stoneAnimation);


            stoneAnimation.Play();

            stone.Tags.Add("Stone");
            stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return stone;
        }

        public static LocatableComponent GenerateGoldResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent stone = new PhysicsComponent(componentManager, "Gold", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation stoneAnimation = new Animation(graphics, spriteSheet, "Gold", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", stone, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(stoneAnimation);


            stoneAnimation.Play();

            stone.Tags.Add("Gold");
            stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return stone;
        }

        public static LocatableComponent GenerateIronResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent stone = new PhysicsComponent(componentManager, "Iron", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(2, 0)
            };
            Animation stoneAnimation = new Animation(graphics, spriteSheet, "Iron", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", stone, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(stoneAnimation);


            stoneAnimation.Play();

            stone.Tags.Add("Iron");
            stone.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return stone;
        }


        public static List<InstanceData> GenerateGrassMotes(List<Vector3> positions,
            List<Color> colors,
            List<float> scales,
            ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, List<InstanceData> motes, string asset, string name)
        {
            if(!GameSettings.Default.GrassMotes)
            {
                return null;
            }

            try
            {
                Texture2D spriteSheet = TextureManager.GetTexture(asset);


                instanceManager.RemoveInstances(name, motes);

                motes.Clear();

                Vector3 avg = Vector3.Zero;
                float minNorm = float.MaxValue;
                float maxNorm = float.MinValue;
                foreach(Vector3 p in positions)
                {
                    avg += p;
                    if(p.LengthSquared() > maxNorm)
                    {
                        maxNorm = p.LengthSquared();
                    }
                    else if(p.LengthSquared() < minNorm)
                    {
                        minNorm = p.LengthSquared();
                    }
                }

                if(positions.Count > 0)
                {
                    avg /= positions.Count;
                }


                for(int i = 0; i < positions.Count; i++)
                {
                    float rot = scales[i] * scales[i];
                    Matrix trans = Matrix.CreateTranslation(positions[i]);
                    Matrix scale = Matrix.CreateScale(scales[i]);
                    motes.Add(new InstanceData(scale * Matrix.CreateRotationY((float) (rot)) * trans, colors[i], true));
                }

                foreach(InstanceData data in motes.Where(data => data != null))
                {
                    instanceManager.AddInstance(name, data);
                }

                return motes;
            }
            catch(ContentLoadException e)
            {
                throw e;
            }
        }

        public static LocatableComponent GenerateDirtResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent dirt = new PhysicsComponent(componentManager, "Dirt", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(0, 1)
            };
            Animation dirtAnimation = new Animation(graphics, spriteSheet, "Dirt", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", dirt, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(dirtAnimation);


            dirtAnimation.Play();

            dirt.Tags.Add("Dirt");
            dirt.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return dirt;
        }

        public static LocatableComponent GenerateWoodResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent wood = new PhysicsComponent(componentManager, "Wood", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(3, 1)
            };
            Animation woodAnimation = new Animation(graphics, spriteSheet, "Wood", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", wood, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(woodAnimation);


            woodAnimation.Play();

            wood.Tags.Add("Wood");
            wood.Tags.Add("Resource");
            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);
            return wood;
        }

        public static LocatableComponent GenerateAppleResource(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("ResourceSheet");

            PhysicsComponent apple = new PhysicsComponent(componentManager, "Apple", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            List<Point> frames = new List<Point>
            {
                new Point(2, 1)
            };
            Animation appleAnimation = new Animation(graphics, spriteSheet, "Apple", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", apple, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(appleAnimation);

            FoodComponent food = new FoodComponent(componentManager, "Food", apple, 15f);

            appleAnimation.Play();

            apple.Tags.Add("Apple");
            apple.Tags.Add("Resource");
            apple.Tags.Add("Food");

            SinMover sinMover = new SinMover(0.05f, 2.0f, (float) PlayState.Random.NextDouble() * 3.0f, sprite);

            return apple;
        }

        public static LocatableComponent GenerateChair(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            return (LocatableComponent) GenerateTableLike(position + new Vector3(0, -0.1f, 0), componentManager, content, graphics, new Point(2, 6), new Point(3, 6));
        }


        public static LocatableComponent GenerateBed(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);

            LocatableComponent bed = new LocatableComponent(componentManager, "Bed", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f));
           
            TexturedBoxObject bedModel = new TexturedBoxObject(componentManager, "bedbox", bed, Matrix.Identity, new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), PrimitiveLibrary.BoxPrimitives["bed"], spriteSheet);

            bed.Tags.Add("Bed");
            bed.CollisionType = CollisionManager.CollisionType.Static;

            return bed;
        }

        public static GameComponent GenerateAnvil(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Anvil", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 3)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Anvil", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Anvil");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateTarget(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Target", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 5)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Target", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = BillboardSpriteComponent.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Target");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateStrawman(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Strawman", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(1, 5)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Strawman", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Strawman");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateWheat(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.wheat);
            LocatableComponent table = new LocatableComponent(componentManager, "Wheat", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Wheat", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Wheat");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateMushroom(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.mushroom);
            LocatableComponent table = new LocatableComponent(componentManager, "Mushroom", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Mushroom", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", table, Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            table.Tags.Add("Mushroom");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        // Biar's workspace
        public static GameComponent GenerateSpikeTrap(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent spikeTrap = new LocatableComponent(componentManager, "Spikes", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);

            TrapSensor sensor = new TrapSensor(componentManager, "TrapSensor", spikeTrap, Matrix.Identity, new Vector3(1, 1, 1), Vector3.Zero); // that 20,5,20 is the bounding box

            List<Point> frames = new List<Point>
            {
                new Point(2, 4)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Spikes", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", spikeTrap, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", spikeTrap, Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            spikeTrap.Tags.Add("Trap");
            spikeTrap.Tags.Add("Spikes");
            spikeTrap.CollisionType = CollisionManager.CollisionType.Static;
            return spikeTrap;
        }

        public static GameComponent GenerateTableLike(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            Point topFrame, Point sideFrame)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent table = new LocatableComponent(componentManager, "Table", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);

            List<Point> frames = new List<Point>
            {
                topFrame
            };

            List<Point> sideframes = new List<Point>
            {
                sideFrame
            };

            Animation tableTop = new Animation(graphics, spriteSheet, "tableTop", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);
            Animation tableAnimation = new Animation(graphics, spriteSheet, "tableTop", 32, 32, sideframes, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent tabletopSprite = new BillboardSpriteComponent(componentManager, "sprite1", table, Matrix.CreateRotationX((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            tabletopSprite.AddAnimation(tableTop);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            BillboardSpriteComponent sprite2 = new BillboardSpriteComponent(componentManager, "sprite2", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);


            tableAnimation.Play();
            table.Tags.Add("Table");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            return GenerateTableLike(position + new Vector3(0, 0.2f, 0), componentManager, content, graphics, new Point(0, 6), new Point(1, 6));
        }


        public static GameComponent GenerateBook(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, GameComponent parent)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            List<Point> frames = new List<Point>
            {
                new Point(0, 4)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Book", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", parent, matrix, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            sprite.Tags.Add("Book");
            sprite.DrawInFrontOfSiblings = true;
            sprite.CollisionType = CollisionManager.CollisionType.Static;
            return sprite;
        }

        public static GameComponent GenerateBookTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            LocatableComponent table = (LocatableComponent) GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("BookTable");
            GameComponent book = GenerateBook(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);

            table.UpdateTransformsRecursive();
            table.DrawInFrontOfSiblings = true;
            table.CollisionType = CollisionManager.CollisionType.Static;
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

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", parent, matrix, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.Spherical
            };
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            sprite.Tags.Add("Potion");
            sprite.DrawInFrontOfSiblings = true;
            sprite.CollisionType = CollisionManager.CollisionType.Static;
            return sprite;
        }

        public static GameComponent GeneratePotionTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            LocatableComponent table = (LocatableComponent) GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("PotionTable");
            table.Name = "PotionTable";
            GameComponent potion = GeneratePotions(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);

            table.UpdateTransformsRecursive();
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateFlag(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent flag = new LocatableComponent(componentManager, "Flag", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 2),
                new Point(1, 2),
                new Point(2, 2),
                new Point(1, 2)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Flag", 32, 32, frames, true, Color.White, 5.0f + (float) PlayState.Random.NextDouble(), 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", flag, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = BillboardSpriteComponent.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            flag.Tags.Add("Flag");

            flag.CollisionType = CollisionManager.CollisionType.Static;
            return flag;
        }

        public static GameComponent GenerateLamp(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent lamp = new LocatableComponent(componentManager, "Lamp", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 1),
                new Point(2, 1),
                new Point(1, 1),
                new Point(2, 1)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Lamp", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false,
                OrientationType = BillboardSpriteComponent.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Lamp");

            LightComponent light = new LightComponent(componentManager, "light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8)
            {
                HasMoved = true
            };
            lamp.CollisionType = CollisionManager.CollisionType.Static;
            return lamp;
        }

        public static GameComponent GenerateForge(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            LocatableComponent lamp = new LocatableComponent(componentManager, "Forge", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(1, 3),
                new Point(3, 3),
                new Point(2, 3),
                new Point(3, 3)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Forge", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            BillboardSpriteComponent sprite = new BillboardSpriteComponent(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Forge");

            LightComponent light = new LightComponent(componentManager, "light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4)
            {
                HasMoved = true
            };
            lamp.CollisionType = CollisionManager.CollisionType.Static;
            return lamp;
        }

        public static GameComponent GenerateTree(float treeSize, Vector3 position, float offset, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, string asset)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            LocatableComponent tree = new LocatableComponent(componentManager, asset, componentManager.RootComponent, matrix, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize , treeSize * 1.5f, treeSize));
            ModelInstanceComponent modelInstance = new ModelInstanceComponent(componentManager, "Model", tree, Matrix.CreateRotationY((float)(PlayState.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(treeSize * 4, treeSize * 4, treeSize * 4) * Matrix.CreateTranslation(new Vector3(0.7f, treeSize * offset, 0.7f)), asset, false);

            HealthComponent health = new HealthComponent(componentManager, "Health", tree, 100.0f * treeSize, 0.0f, 100.0f * treeSize);

            FlammableComponent flame = new FlammableComponent(componentManager, "Flames", tree, health);


            tree.Tags.Add("Tree");
            tree.Tags.Add("EmitsWood");

            List<LocatableComponent> woods = new List<LocatableComponent>();

            for(int i = 0; i < (int) (treeSize * 10); i++)
            {
                woods.Add(GenerateWoodResource(position + new Vector3(treeSize, treeSize * 1.5f, treeSize), componentManager, content, graphics));
            }

            foreach(LocatableComponent wood in woods)
            {
                wood.SetVisibleRecursive(false);
                wood.SetActiveRecursive(false);
            }

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(treeSize * 2, treeSize, treeSize * 2), Vector3.Zero, woods);

            EmitterComponent emitter = new EmitterComponent("Leaves", componentManager, "LeafEmitter", tree, Matrix.Identity, new Vector3(treeSize * 2, treeSize, treeSize * 2), Vector3.Zero);

            tree.AddToOctree = true;
            tree.CollisionType = CollisionManager.CollisionType.Static;
            return tree;
        }

        public static GameComponent GenerateBerryBush(float bushSize, Vector3 position, float offset, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.berrybush);
            LocatableComponent tree = new LocatableComponent(componentManager, "Bush", componentManager.RootComponent, matrix, new Vector3(bushSize, bushSize, bushSize), Vector3.Zero);
            ModelInstanceComponent modelInstance = new ModelInstanceComponent(componentManager, "Model", tree, Matrix.CreateScale(bushSize, bushSize, bushSize) * Matrix.CreateTranslation(new Vector3(0.0f, bushSize * offset - 0.1f, 0.0f)), "berrybush", false);

            HealthComponent health = new HealthComponent(componentManager, "Health", tree, 30 * bushSize, 0.0f, 30 * bushSize);


            tree.Tags.Add("Tree");
            tree.Tags.Add("Bush");
            tree.Tags.Add("EmitsFood");


            List<LocatableComponent> apples = new List<LocatableComponent>();

            for(int i = 0; i < (int) (bushSize * 5); i++)
            {
                apples.Add(GenerateAppleResource(position + new Vector3(0, bushSize, 0), componentManager, content, graphics));
            }

            foreach(LocatableComponent apple in apples)
            {
                apple.SetVisibleRecursive(false);
                apple.SetActiveRecursive(false);
             
            }

            apples.AddRange(apples);

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(bushSize * 4, bushSize * 2, bushSize * 4), Vector3.Zero, apples);

            tree.AddToOctree = true;
            tree.CollisionType = CollisionManager.CollisionType.Static;
            return tree;
        }


        public static void CreateIntersectingBillboard(GameComponent component, Texture2D spriteSheet, float xSize, float ySize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            BatchBillboard billboard = new BatchBillboard(componentManager, "BatchBillboard", component, Matrix.Identity, spriteSheet, 2, graphics)
            {
                Primitive = PrimitiveLibrary.BatchBillboardPrimitives["tree"],
                LightsWithVoxels = true,
                CullDistance = 70 * 70,
                LocalTransform = Matrix.CreateScale(xSize * 4, ySize * 4, xSize * 4)
            };
        }


        public static LocatableComponent GenerateGoblin(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats
            {
                Dexterity = 5,
                Constitution = 5,
                Strength = 5,
                Wisdom = 5,
                Charisma = 5,
                Intelligence = 5,
                Size = 1
            };
            return new Goblin(stats, allies, planService, faction, componentManager, "Goblin", chunkManager, graphics, content, TextureManager.GetTexture("GoblinSheet"), position).Physics;
        }

        public static GameComponent GenerateDwarf(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats
            {
                Dexterity = 5,
                Constitution = 5,
                Strength = 5,
                Wisdom = 5,
                Charisma = 5,
                Intelligence = 5,
                Size = 1
            };
            return new Dwarf(stats, allies, planService, faction, componentManager, "Dwarf", chunkManager, graphics, content, TextureManager.GetTexture("DwarfSheet"), position).Physics;
        }
    }

}