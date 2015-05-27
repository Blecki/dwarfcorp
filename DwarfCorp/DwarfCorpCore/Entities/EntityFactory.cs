﻿using System;
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

        public static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

        public static Body GenerateTestDwarf(Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Dwarf.dwarf);
            Creature toReturn =  new Creature(position, dwarfDef, "Wizard", 0, "Player");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, PlayState.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static Body GenerateTestGoblin(Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Goblin.goblin);
            Creature toReturn = new Creature(position, dwarfDef, "Sword Goblin", 0, "Goblins");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, PlayState.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static Body GenerateTestSeketon(Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Skeleton.skeleton);
            Creature toReturn = new Creature(position, dwarfDef, "Skeleton", 0, "Undead");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, PlayState.Time.CurrentDate), false);
            return toReturn.Physics;
        }


        public static Body GenerateTestMoleman(Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Moleman.moleman);
            Creature toReturn = new Creature(position, dwarfDef, "Moleman Miner", 0, "Molemen");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, PlayState.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static void Initialize()
        {
            EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();
            RegisterEntity("Crate", (position, data) => new Crate(position));
            foreach (var resource in ResourceLibrary.Resources)
            {
                ResourceLibrary.ResourceType type = resource.Value.Type;
                RegisterEntity(resource.Key + " Resource", (position, data) => new ResourceEntity(type, position));
            }
            RegisterEntity("Balloon", (position, data) => CreateBalloon(position + new Vector3(0, 1000, 0), position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, null, PlayState.PlayerFaction));
            RegisterEntity("Work Pile", (position, data) => new WorkPile(position));
            RegisterEntity("Pine Tree", (position, data) => new Tree(position, "pine", data.GetData("Scale", 1.0f)));
            RegisterEntity("Snow Pine Tree", (position, data) => new Tree(position, "snowpine", data.GetData("Scale", 1.0f)));
            RegisterEntity("Palm Tree", (position, data) => new Tree(position, "palm", data.GetData("Scale", 1.0f)));
            RegisterEntity("Berry Bush", (position, data) => new Bush(position, "berrybush", data.GetData("Scale", 1.0f)));
            RegisterEntity("Bird", (position, data) => new Bird(ContentPaths.Entities.Animals.Birds.GetRandomBird(), position, PlayState.ComponentManager, PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Bird"));
            RegisterEntity("Deer", (position, data) => new Deer(ContentPaths.Entities.Animals.Deer.deer, position, PlayState.ComponentManager, PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Deer"));
            RegisterEntity("Dwarf", (position, data) => GenerateDwarf(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.PlayerFaction, PlayState.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Worker], 0));
            RegisterEntity("TestDwarf", (position, data) => GenerateTestDwarf(position));
            RegisterEntity("TestGoblin", (position, data) => GenerateTestGoblin(position));
            RegisterEntity("TestSkeleton", (position, data) => GenerateTestSeketon(position));
            RegisterEntity("TestMoleman", (position, data) => GenerateTestMoleman(position));
            RegisterEntity("AxeDwarf", (position, data) => GenerateDwarf(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.PlayerFaction, PlayState.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.AxeDwarf], 0));
            RegisterEntity("CraftsDwarf", (position, data) => GenerateDwarf(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.PlayerFaction, PlayState.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.CraftsDwarf], 0));
            RegisterEntity("Wizard", (position, data) => GenerateDwarf(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.PlayerFaction, PlayState.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Wizard], 0));
            RegisterEntity("Moleman", (position, data) => GenerateMoleman(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.ComponentManager.Factions.Factions["Molemen"], PlayState.PlanService, "Molemen"));
            RegisterEntity("Goblin", (position, data) => GenerateGoblin(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.ComponentManager.Factions.Factions["Goblins"], PlayState.PlanService, "Goblins"));
            RegisterEntity("Skeleton", (position, data) => GenerateSkeleton(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.ComponentManager.Factions.Factions["Undead"], PlayState.PlanService, "Undead"));
            RegisterEntity("Necromancer", (position, data) => GenerateNecromancer(position, PlayState.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager, PlayState.Camera, PlayState.ComponentManager.Factions.Factions["Undead"], PlayState.PlanService, "Undead"));
            RegisterEntity("Bed", (position, data) => new Bed(position));
            RegisterEntity("Bear Trap", (position, data) => new BearTrap(position));
            RegisterEntity("Lamp", (position, data) => new Lamp(position));
            RegisterEntity("Table", (position, data) => new Table(position));
            RegisterEntity("Chair", (position, data) => new Chair(position));
            RegisterEntity("Flag", (position, data) => new Flag(position));
            RegisterEntity("Mushroom", (position, data) => new Mushroom(position));
            RegisterEntity("Wheat", (position, data) => new Wheat(position));
            RegisterEntity("BookTable", (position, data) => new Table(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 4)) {Tags = new List<string>(){"Research"}, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f }});
            RegisterEntity("PotionTable", (position, data) => new Table(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 4)) { Tags = new List<string>(){"Research"}, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f } });
            RegisterEntity("Anvil", (position, data) => new Anvil(position));
            RegisterEntity("Forge", (position, data) => new Forge(position));
            RegisterEntity("Elf", (position, data) => GenerateElf(position, PlayState.ComponentManager.Factions.Factions["Elf"], "Elf"));
            RegisterEntity("Arrow", (position, data) => new ArrowProjectile(position, data.GetData("Velocity", Vector3.Up*10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData("Faction", "Elf")));
            RegisterEntity("Fairy", (position, data) => new Fairy("Player", position));
            RegisterEntity("Target", (position, data) => new Target(position));
            RegisterEntity("Strawman", (position, data) => new Strawman(position));
            RegisterEntity("Bookshelf", (position, data) => new Bookshelf(position));
            RegisterEntity("Door", (position, data) => new Door(position));
            RegisterEntity("Ladder", (position, data) => new Ladder(position));
        }

        

        public static void RegisterEntity<T>(string id, Func<Vector3, Blackboard, T> function) where T : GameComponent
        {
            EntityFuncs[id] = function;
        }

        public static T CreateEntity<T>(string id, Vector3 location, Blackboard data = null) where T : GameComponent
        {
            if(data == null) data = new Blackboard();
            if (EntityFuncs.ContainsKey(id))
            {
                return EntityFuncs[id].Invoke(location, data) as T;
            }
            else
            {
                string err = id ?? "null";
                throw new KeyNotFoundException("Unable to create entity of type " + err);   
            }
        }

        public static Func<Vector3, T> GetFunc<T>(string id) where T : GameComponent
        {
            return EntityFuncs[id] as Func<Vector3, T>;
        }

        public static Body CreateBalloon(Vector3 target, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ShipmentOrder order, Faction master)
        {
            Body balloon = new Body("Balloon", componentManager.RootComponent,
                Matrix.CreateTranslation(position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0));

            SpriteSheet tex = new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon);
            List<Point> points = new List<Point>
            {
                new Point(0, 0)
            };
            Animation balloonAnimation = new Animation(graphics, new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon), "balloon", points, false, Color.White, 0.001f, false);
            Sprite sprite = new Sprite(componentManager, "sprite", balloon, Matrix.Identity, tex, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
            };
            sprite.AddAnimation(balloonAnimation);

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
            Shadow shadow = new Shadow(componentManager, "shadow", balloon, shadowTransform, new SpriteSheet(ContentPaths.Effects.shadowcircle));
            BalloonAI balloonAI = new BalloonAI(balloon, target, order, master);

            MinimapIcon minimapIcon = new MinimapIcon(balloon, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 2, 0));

            return balloon;
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
            ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, List<InstanceData> motes,
            string asset, string name)
        {
            if (!GameSettings.Default.GrassMotes)
            {
                return null;
            }

            try
            {

                InstanceManager.RemoveInstances(name, motes);

                motes.Clear();


                float minNorm = float.MaxValue;
                float maxNorm = float.MinValue;
                foreach (Vector3 p in positions)
                {
                    if (p.LengthSquared() > maxNorm)
                    {
                        maxNorm = p.LengthSquared();
                    }
                    else if (p.LengthSquared() < minNorm)
                    {
                        minNorm = p.LengthSquared();
                    }
                }



                for (int i = 0; i < positions.Count; i++)
                {
                    float rot = scales[i]*scales[i];
                    Matrix trans = Matrix.CreateTranslation(positions[i]);
                    Matrix scale = Matrix.CreateScale(scales[i]);
                    motes.Add(new InstanceData(scale*Matrix.CreateRotationY(rot)*trans, colors[i], true));
                }

                foreach (InstanceData data in motes.Where(data => data != null))
                {
                    InstanceManager.AddInstance(name, data);
                }

                return motes;
            }
            catch (ContentLoadException e)
            {
                throw e;
            }
        }

        public static void CreateIntersectingBillboard(GameComponent component, SpriteSheet spriteSheet, float xSize, float ySize, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics)
        {
            BatchedSprite billboard = new BatchedSprite(componentManager, "BatchedSprite", component, Matrix.Identity, spriteSheet, 2, graphics)
            {
                Primitive = PrimitiveLibrary.BatchBillboardPrimitives["tree"],
                LightsWithVoxels = true,
                CullDistance = 70 * 70,
                LocalTransform = Matrix.CreateScale(xSize * 4, ySize * 4, xSize * 4)
            };
        }


        public static Body GenerateElf(Vector3 position, Faction faction, string allies)
        {
            CreatureStats stats = new CreatureStats(new ElfClass(), 0);
            return new Elf(stats, allies, PlayState.PlanService, faction, PlayState.ComponentManager, "Goblin", PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, position).Physics;
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

        public static Body GenerateMoleman(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new MolemanMinerClass(), 0);
            return new Moleman(stats, allies, planService, faction, componentManager, "Moleman", chunkManager, graphics, content, position).Physics;
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

        /*
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
         */
    }

}