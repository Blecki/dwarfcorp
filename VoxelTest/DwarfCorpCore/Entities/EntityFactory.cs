using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This class is used to create entities. It should probably be replaced with a more modular system (or a set of data files)
    /// Right now, its just an ugly class for initializing most of the entities in the game.
    /// </summary>
    internal class EntityFactory
    {
        public static InstanceManager InstanceManager = null;



        public static Body CreateBalloon(Vector3 target, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ShipmentOrder order, Faction master)
        {
            Body balloon = new Body("Balloon", componentManager.RootComponent,
                Matrix.CreateTranslation(position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0));

            Texture2D tex = TextureManager.GetTexture(ContentPaths.Entities.Balloon.Sprites.balloon);
            List<Point> points = new List<Point>
            {
                new Point(0, 0)
            };
            Animation balloonAnimation = new Animation(graphics, tex, "balloon", points, false, Color.White, 0.001f, false);
            Sprite sprite = new Sprite(componentManager, "sprite", balloon, Matrix.Identity, tex, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
            };
            sprite.AddAnimation(balloonAnimation);

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
            Shadow shadow = new Shadow(componentManager, "shadow", balloon, shadowTransform, TextureManager.GetTexture(ContentPaths.Effects.shadowcircle));
            BalloonAI balloonAI = new BalloonAI(balloon, target, order, master);

            MinimapIcon minimapIcon = new MinimapIcon(balloon, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 2, 0));

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
            if (id == "Wheat")
            {
                return GenerateWheat(position + new Vector3(0, offset, 0), componentManager, content, graphics);
            }
            if (id == "Mushroom")
            {
                return GenerateMushroom(position + new Vector3(0, offset, 0), componentManager, content, graphics);
            }
            return null;
        }

        public static string[] ComponentList =
        {
            "Crate",
            "Balloon",
            "Wood Resource",
            "Iron Resource",
            "Dirt Resource",
            "Stone Resource",
            "Gold Resource",
            "Coal Resource",
            "Mana Resource",
            "Apple Resource",
            "Grain Resource",
            "Mushroom Resource",
            "Work Pile",
            "pine",
            "snowpine",
            "palm",
            "berrybush",
            "apple_tree",
            "Bird",
            "Deer",
            "Dwarf",
            "AxeDwarf",
            "CraftsDwarf",
             "Wizard",
            "Goblin",
            "Skeleton",
            "Necromancer",
            "Bed",
            "BearTrap",
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
            "PotionTable",
            "Snake"
        };


        public static Body CreateCrate(Vector3 position, float rot)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);

            Body crate = new Body("Crate", PlayState.ComponentManager.RootComponent, matrix, new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f));
            Box crateModel = new Box(PlayState.ComponentManager, "Cratebox", crate, Matrix.CreateRotationY(rot), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.5f, 0.5f, 0.5f), PrimitiveLibrary.BoxPrimitives["crate"], spriteSheet);

            crate.Tags.Add("Crate");
            crate.CollisionType = CollisionManager.CollisionType.Static;
            return crate;
        }

        public static Body GenerateComponent(string id, 
            Vector3 position,
            ComponentManager componentManager, 
            ContentManager content, 
            GraphicsDevice graphics, 
            ChunkManager chunks, 
            FactionLibrary factions,
            Camera camera)
        {
            switch(id)
            {
                case "Crate":
                    return CreateCrate(position - new Vector3(0.5f, 0.5f, 0.5f), MathFunctions.Rand(-0.1f, 0.1f));
                case "Balloon":
                    return CreateBalloon(position, position + new Vector3(0, 2, 0), componentManager, content, graphics, new ShipmentOrder(0, null), factions.Factions["Player"]);
                case "Wood Resource":
                case "Iron Resource":
                case "Dirt Resource":
                case "Stone Resource":
                case "Gold Resource":
                case "Mana Resource":
                case "Apple Resource":
                case "Grain Resource":
                case "Coal Resource":
                case "Mushroom Resource":
                {
                    string resourceID = id.Split(' ').First();
                    return GenerateResource(resourceID, position);   
                }
                case "Work Pile":
                    return new WorkPile(position);
                case "palm":
                case "snowpine":
                case "apple_tree":
                case "berrybush":
                case "pine":
                {
                    float s = MathFunctions.Rand() * 0.8f + 0.5f;
                    return (Body) GenerateVegetation(id, s, 1.0f, position, componentManager, content, graphics);
                }
                case "Dwarf":
                    return (Body) GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Player"], PlayState.PlanService, "Dwarf", JobLibrary.Classes[JobLibrary.JobType.Worker], 0);
                case "AxeDwarf":
                    return (Body) GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Player"], PlayState.PlanService, "Dwarf", JobLibrary.Classes[JobLibrary.JobType.AxeDwarf], 0);
                case "CraftsDwarf":
                    return (Body)GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Player"], PlayState.PlanService, "Dwarf", JobLibrary.Classes[JobLibrary.JobType.CraftsDwarf], 0);
                case "Wizard":
                    return (Body)GenerateDwarf(position, componentManager, content, graphics, chunks, camera, factions.Factions["Player"], PlayState.PlanService, "Dwarf", JobLibrary.Classes[JobLibrary.JobType.Wizard], 0);
                case "Goblin":
                    return (Body)GenerateGoblin(position, componentManager, content, graphics, chunks, camera, factions.Factions["Goblins"], PlayState.PlanService, "Goblins");
                case "Skeleton":
                    return (Body)GenerateSkeleton(position, componentManager, content, graphics, chunks, camera, factions.Factions["Undead"], PlayState.PlanService, "Undead");
                case "Necromancer":
                    return (Body)GenerateNecromancer(position, componentManager, content, graphics, chunks, camera, factions.Factions["Undead"], PlayState.PlanService, "Undead");
                case "Bed":
                    return GenerateBed(position, componentManager, content, graphics);
                case "BearTrap":
                    return GenerateCraftItem(CraftLibrary.CraftItemType.BearTrap, position - new Vector3(0.5f, 0.5f, 0.5f));
                case "Lamp":
                    return (Body) GenerateLamp(position, componentManager, content, graphics);
                case "Table":
                    return (Body) GenerateTable(position, componentManager, content, graphics);
                case "Chair":
                    return (Body) GenerateChair(position, componentManager, content, graphics);
                case "Flag":
                    return (Body) GenerateFlag(position, componentManager, content, graphics);
                case "Wheat":
                    return (Body) GenerateWheat(position, componentManager, content, graphics);
                case "Mushroom":
                    return (Body) GenerateMushroom(position, componentManager, content, graphics);
                case "SpikeTrap":
                    return (Body) GenerateSpikeTrap(position, componentManager, content, graphics);
                case "BookTable":
                    return (Body) GenerateBookTable(position, componentManager, content, graphics);
                case "PotionTable":
                    return (Body) GeneratePotionTable(position, componentManager, content, graphics);
                case "Book":
                    return (Body) GenerateBook(position, componentManager, content, graphics, componentManager.RootComponent);
                case "Potion":
                    return (Body) GeneratePotions(position, componentManager, content, graphics, componentManager.RootComponent);
                case "Bird":
                    return (Body) GenerateBird(position, componentManager, content, graphics, chunks);
                case "Deer":
                    return (Body)GenerateDeer(position, componentManager, content, graphics, chunks);
                case "Snake":
                    return (Body)GenerateSnake(position, componentManager, content, graphics, chunks);
                default:
                    return null;
            }
        }

        public static Body GenerateSkeleton(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, Camera camera, Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new SkeletonClass(), 0);
            return new Skeleton(stats, allies, planService, faction, componentManager, "Skeleton", chunks, graphics, content, position).Physics;
        }


        public static Body GenerateNecromancer(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, Camera camera, Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new NecromancerClass(), 0);
            return new Necromancer(stats, allies, planService, faction, componentManager, "Necromancer", chunks, graphics, content, position).Physics;
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

                InstanceManager.RemoveInstances(name, motes);

                motes.Clear();


                float minNorm = float.MaxValue;
                float maxNorm = float.MinValue;
                foreach(Vector3 p in positions)
                {
                    if(p.LengthSquared() > maxNorm)
                    {
                        maxNorm = p.LengthSquared();
                    }
                    else if(p.LengthSquared() < minNorm)
                    {
                        minNorm = p.LengthSquared();
                    }
                }



                for(int i = 0; i < positions.Count; i++)
                {
                    float rot = scales[i] * scales[i];
                    Matrix trans = Matrix.CreateTranslation(positions[i]);
                    Matrix scale = Matrix.CreateScale(scales[i]);
                    motes.Add(new InstanceData(scale * Matrix.CreateRotationY(rot) * trans, colors[i], true));
                }

                foreach(InstanceData data in motes.Where(data => data != null))
                {
                    InstanceManager.AddInstance(name, data);
                }

                return motes;
            }
            catch(ContentLoadException e)
            {
                throw e;
            }
        }

        public static Body GenerateResource(ResourceLibrary.ResourceType resourceType, Vector3 position)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Resource type = ResourceLibrary.Resources[resourceType];
            Texture2D spriteSheet = type.Image.Image;

            Physics physics = new Physics("Physics", PlayState.ComponentManager.RootComponent, matrix, new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            int frameX = type.Image.SourceRect.X / 32;
            int frameY = type.Image.SourceRect.Y / 32;

            List<Point> frames = new List<Point>
            {
                new Point(frameX, frameY)
            };
            Animation animation = new Animation(GameState.Game.GraphicsDevice, spriteSheet, "Animation", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            Sprite sprite = new Sprite(PlayState.ComponentManager, "Sprite", physics, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical,
                LightsWithVoxels = !type.SelfIlluminating
            };
            sprite.AddAnimation(animation);


            animation.Play();

            physics.Tags.Add(type.ResourceName);
            physics.Tags.Add("Resource");
            Bobber bobber = new Bobber(0.05f, 2.0f, MathFunctions.Rand() * 3.0f, sprite);
            return physics;
        }
        
        public static Body GenerateResource(string name, Vector3 position)
        {
            return GenerateResource(ResourceLibrary.GetResourceByName(name).Type, position);
        }

        public static Body GenerateChair(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Body toReturn =  (Body) GenerateTableLike(position + new Vector3(0, -0.22f, 0), componentManager, content, graphics, new Point(2, 6), new Point(3, 6));
            toReturn.Tags.Add("Chair");
            return toReturn;
        }

        public static Body GenerateBed(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);

            Body bed = new Body("Bed", componentManager.RootComponent, matrix, new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f));
           
            Box bedModel = new Box(componentManager, "bedbox", bed, Matrix.Identity, new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), PrimitiveLibrary.BoxPrimitives["bed"], spriteSheet);

            Voxel voxelUnder = new Voxel();


            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, bed, PlayState.ChunkManager, voxelUnder);
            }

            bed.Tags.Add("Bed");
            bed.CollisionType = CollisionManager.CollisionType.Static;

            return bed;
        }

        public static GameComponent GenerateAnvil(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            Body table = new Body("Anvil", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 3)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Anvil", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = Sprite.OrientMode.Spherical;
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
            Body table = new Body("Target", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 5)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Target", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false);
            sprite.OrientationType = Sprite.OrientMode.Spherical;
            sprite.AddAnimation(tableAnimation);

            tableAnimation.Play();
            table.Tags.Add("Target");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateStrawman(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position - new Vector3(0.5f, 0, 0.5f);
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            Body table = new Body("Strawman", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(1, 5)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Strawman", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
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
            matrix.Translation = position + new Vector3(0.5f, 0, 0.5f);
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.wheat);
            Body table = new Body("Wheat", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Wheat", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", table, Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();
            bool success =  PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder);

            if (success)
            {
                VoxelListener listener = new VoxelListener(componentManager, table, PlayState.ChunkManager, voxelUnder);
            }

            Inventory inventory = new Inventory("Inventory", table)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 1,
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>()
                    {
                        {
                            ResourceLibrary.ResourceType.Grain,
                            new ResourceAmount(ResourceLibrary.ResourceType.Grain)
                        }
                    }
                }
            };

            Health health = new Health(componentManager, "HP", table, 30, 0.0f, 30);
            tableAnimation.Play();
            table.Tags.Add("Wheat");
            table.Tags.Add("Vegetation");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateMushroom(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position + new Vector3(0.5f, 0.0f, 0.5f);
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.mushroom);
            Body table = new Body("Mushroom", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Mushroom", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", table, Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, table, PlayState.ChunkManager, voxelUnder);
            }


            Inventory inventory = new Inventory("Inventory", table)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 1,
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>()
                    {
                        {
                            ResourceLibrary.ResourceType.Mushroom,
                            new ResourceAmount(ResourceLibrary.ResourceType.Mushroom)
                        }
                    }
                }
            };

            Health health = new Health(componentManager, "HP", table, 30, 0.0f, 30);


            tableAnimation.Play();
            table.Tags.Add("Mushroom");
            table.Tags.Add("Vegetation");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateSpikeTrap(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.CreateRotationY((float) Math.PI * 0.5f);
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            Body spikeTrap = new Body("Spikes", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);

            TrapSensor sensor = new TrapSensor(componentManager, "TrapSensor", spikeTrap, Matrix.Identity, new Vector3(1, 1, 1), Vector3.Zero); // that 20,5,20 is the bounding box

            List<Point> frames = new List<Point>
            {
                new Point(2, 4)
            };
            Animation tableAnimation = new Animation(graphics, spriteSheet, "Spikes", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", spikeTrap, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", spikeTrap, Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
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
            Body table = new Body("Table", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);

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

            Sprite tabletopSprite = new Sprite(componentManager, "sprite1", table, Matrix.CreateRotationX((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            tabletopSprite.AddAnimation(tableTop);

            Sprite sprite = new Sprite(componentManager, "sprite", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", table, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float) Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, table, PlayState.ChunkManager, voxelUnder);
            }


            tableAnimation.Play();
            table.Tags.Add("Table");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            return GenerateTableLike(position + new Vector3(0, 0.15f, 0), componentManager, content, graphics, new Point(0, 6), new Point(1, 6));
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

            Sprite sprite = new Sprite(componentManager, "sprite", parent, matrix, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
            };
            sprite.AddAnimation(tableAnimation);


            tableAnimation.Play();
            sprite.Tags.Add("Book");
            sprite.Tags.Add("Research");
            sprite.DrawInFrontOfSiblings = true;
            sprite.CollisionType = CollisionManager.CollisionType.Static;
            return sprite;
        }

        public static GameComponent GenerateBookTable(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Body table = (Body) GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("BookTable");
            GameComponent book = GenerateBook(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);


            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, table, PlayState.ChunkManager, voxelUnder);
            }

            table.UpdateTransformsRecursive();
            table.DrawInFrontOfSiblings = true;
            table.Tags.Add("Research");
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

            Sprite sprite = new Sprite(componentManager, "sprite", parent, matrix, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
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
            Body table = (Body) GenerateTable(position, componentManager, content, graphics);
            table.Tags.Add("PotionTable");
            table.Name = "PotionTable";
            GameComponent potion = GeneratePotions(new Vector3(0, 0.1f, 0), componentManager, content, graphics, table);


            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, table, PlayState.ChunkManager, voxelUnder);
            }

            table.UpdateTransformsRecursive();
            table.Tags.Add("Research");
            table.CollisionType = CollisionManager.CollisionType.Static;
            return table;
        }

        public static GameComponent GenerateFlag(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position;
            Texture2D spriteSheet = TextureManager.GetTexture("InteriorSheet");
            Body flag = new Body("Flag", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 2),
                new Point(1, 2),
                new Point(2, 2),
                new Point(1, 2)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Flag", 32, 32, frames, true, Color.White, 5.0f + MathFunctions.Rand(), 1f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", flag, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);



            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, flag, PlayState.ChunkManager, voxelUnder);
            }

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
            Body lamp = new Body("Lamp", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(0, 1),
                new Point(2, 1),
                new Point(1, 1),
                new Point(2, 1)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Lamp", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false,
                OrientationType = Sprite.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Lamp");



            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, lamp, PlayState.ChunkManager, voxelUnder);
            }


            LightEmitter light = new LightEmitter("light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8)
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
            Body lamp = new Body("Forge", componentManager.RootComponent, matrix, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero);
            List<Point> frames = new List<Point>
            {
                new Point(1, 3),
                new Point(3, 3),
                new Point(2, 3),
                new Point(3, 3)
            };
            Animation lampAnimation = new Animation(graphics, spriteSheet, "Forge", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            Sprite sprite = new Sprite(componentManager, "sprite", lamp, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            lamp.Tags.Add("Forge");


            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, lamp, PlayState.ChunkManager, voxelUnder);
            }

            LightEmitter light = new LightEmitter("light", lamp, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4)
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
            Body tree = new Body(asset, componentManager.RootComponent, matrix, new Vector3(treeSize * 2, treeSize * 3, treeSize * 2), new Vector3(treeSize , treeSize * 1.5f, treeSize));
            Mesh modelInstance = new Mesh(componentManager, "Model", tree, Matrix.CreateRotationY((float)(PlayState.Random.NextDouble() * Math.PI)) * Matrix.CreateScale(treeSize * 4, treeSize * 4, treeSize * 4) * Matrix.CreateTranslation(new Vector3(0.7f, treeSize * offset, 0.7f)), asset, false);

            Health health = new Health(componentManager, "HP", tree, 100.0f * treeSize, 0.0f, 100.0f * treeSize);
            Flammable flame = new Flammable(componentManager, "Flames", tree, health);


            tree.Tags.Add("Vegetation");
            tree.Tags.Add("EmitsWood");

            MinimapIcon minimapIcon = new MinimapIcon(tree, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 1, 0));
            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, tree, PlayState.ChunkManager, voxelUnder);
            }

            Inventory inventory = new Inventory("Inventory", tree)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = (int) (treeSize * 10)
                }
            };

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int) (treeSize * 10),
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Wood]
            });


            ParticleTrigger particleTrigger = new ParticleTrigger("Leaves", componentManager, "LeafEmitter", tree,
                Matrix.Identity, new Vector3(treeSize*2, treeSize, treeSize*2), Vector3.Zero)
            {
                SoundToPlay = ContentPaths.Audio.vegetation_break
            };


            tree.AddToOctree = true;
            tree.CollisionType = CollisionManager.CollisionType.Static;
            return tree;
        }

        public static GameComponent GenerateBerryBush(float bushSize, Vector3 position, float offset, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position + new Vector3(0.5f, 0, 0.5f);
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.berrybush);
            Body tree = new Body("Bush", componentManager.RootComponent, matrix, new Vector3(bushSize, bushSize, bushSize), Vector3.Zero);
            Mesh modelInstance = new Mesh(componentManager, "Model", tree, Matrix.CreateScale(bushSize, bushSize, bushSize) * Matrix.CreateTranslation(new Vector3(0.0f, bushSize * offset - 0.1f, 0.0f)), "berrybush", false);

            Health health = new Health(componentManager, "HP", tree, 30 * bushSize, 0.0f, 30 * bushSize);



            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, tree, PlayState.ChunkManager, voxelUnder);
            }

            tree.Tags.Add("Vegetation");
            tree.Tags.Add("Bush");
            tree.Tags.Add("EmitsFood");

            /*
            List<Body> apples = new List<Body>();

            for(int i = 0; i < (int) (bushSize * 5); i++)
            {
                apples.Add(GenerateAppleResource(position + new Vector3(0, bushSize, 0), componentManager, content, graphics));
            }

            foreach(Body apple in apples)
            {
                apple.SetVisibleRecursive(false);
                apple.SetActiveRecursive(false);
             
            }

            apples.AddRange(apples);

            DeathComponentSpawner spawner = new DeathComponentSpawner(componentManager, "Component Spawner", tree, Matrix.Identity, new Vector3(bushSize * 4, bushSize * 2, bushSize * 4), Vector3.Zero, apples);
            */
            Inventory inventory = new Inventory("Inventory", tree)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = (int)(bushSize * 5)
                }
            };

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int)(bushSize * 5),
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Apple]
            });

            tree.AddToOctree = true;
            tree.CollisionType = CollisionManager.CollisionType.Static;
            return tree;
        }


        public static void CreateIntersectingBillboard(GameComponent component, Texture2D spriteSheet, float xSize, float ySize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            BatchedSprite billboard = new BatchedSprite(componentManager, "BatchedSprite", component, Matrix.Identity, spriteSheet, 2, graphics)
            {
                Primitive = PrimitiveLibrary.BatchBillboardPrimitives["tree"],
                LightsWithVoxels = true,
                CullDistance = 70 * 70,
                LocalTransform = Matrix.CreateScale(xSize * 4, ySize * 4, xSize * 4)
            };
        }


        public static Body GenerateGoblin(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new SwordGoblinClass(), 0);
            return new Goblin(stats, allies, planService, faction, componentManager, "Goblin", chunkManager, graphics, content, position).Physics;
        }

        public static GameComponent GenerateDwarf(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies, EmployeeClass dwarfClass, int level)
        {
            CreatureStats stats = new CreatureStats(dwarfClass, level);
            Dwarf toReturn =  new Dwarf(stats, allies, planService, faction, "Dwarf", chunkManager, graphics, content, dwarfClass, position);
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, PlayState.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static GameComponent GenerateBird(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager)
        {
          return new Bird(ContentPaths.Entities.Animals.Birds.GetRandomBird(), position, componentManager, chunkManager, graphics, content, "Bird").Physics;
        }

        public static GameComponent GenerateDeer(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunks)
        {
            return new Deer(ContentPaths.Entities.Animals.Deer.deer, position, componentManager, chunks, graphics, content, "Deer").Physics;
        }

        public static GameComponent GenerateSnake(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunks)
        {
            return new Snake(ContentPaths.Entities.Animals.Snake.snake, position, componentManager, chunks, graphics, content, "Snake").Physics;
        }

        public static Body GenerateCraftItem(CraftLibrary.CraftItemType itemType, Vector3 position)
        {
            switch (itemType)
            {
                    case CraftLibrary.CraftItemType.BearTrap:
                        return new BearTrap(position + new Vector3(0.5f, 0.5f, 0.5f));
                    case CraftLibrary.CraftItemType.Lamp:
                        return (Body) GenerateLamp(position + new Vector3(0.5f, 0.5f, 0.5f), PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice);
            }

            return null;
        }
    }

}