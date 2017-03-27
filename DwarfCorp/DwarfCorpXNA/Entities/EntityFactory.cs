// EntityFactory.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
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
        public static WorldManager World = null;
        private static ComponentManager Components { get { return World.ComponentManager; } }
        public static InstanceManager InstanceManager = null;

        public static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

        public static Body GenerateTestDwarf(WorldManager world, Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Dwarf.dwarf);
            Creature toReturn =  new Creature(world.ComponentManager, position, dwarfDef, "Wizard", 0, "Player");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, world.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static Body GenerateTestGoblin(WorldManager world, Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Goblin.goblin);
            Creature toReturn = new Creature(world.ComponentManager, position, dwarfDef, "Sword Goblin", 0, "Goblins");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, world.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static Body GenerateTestSeketon(WorldManager world, Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Skeleton.skeleton);
            Creature toReturn = new Creature(world.ComponentManager, position, dwarfDef, "Skeleton", 0, "Undead");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, world.Time.CurrentDate), false);
            return toReturn.Physics;
        }


        public static Body GenerateTestMoleman(WorldManager world, Vector3 position)
        {
            CreatureDef dwarfDef = ContentPaths.LoadFromJson<CreatureDef>(ContentPaths.Entities.Moleman.moleman);
            Creature toReturn = new Creature(world.ComponentManager, position, dwarfDef, "Moleman Miner", 0, "Molemen");
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, world.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static void Initialize(WorldManager world)
        {
            World = world;
            RegisterEntity("Crate", (position, data) => new Crate(world.ComponentManager, position));
            RegisterEntity("Balloon", (position, data) => CreateBalloon(position + new Vector3(0, 1000, 0), position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, null, world.PlayerFaction));
            RegisterEntity("Work Pile", (position, data) => new WorkPile(world.ComponentManager, position));
            RegisterEntity("Pine Tree", (position, data) => new Tree(world.ComponentManager, position, "pine", ResourceLibrary.ResourceType.PineCone, data.GetData("Scale", 1.0f)));
            RegisterEntity("Snow Pine Tree", (position, data) => new Tree(world.ComponentManager, position, "snowpine", ResourceLibrary.ResourceType.PineCone, data.GetData("Scale", 1.0f)));
            RegisterEntity("Palm Tree", (position, data) => new Tree(world.ComponentManager, position, "palm", ResourceLibrary.ResourceType.Coconut, data.GetData("Scale", 1.0f)));
            RegisterEntity("Cactus", (position, data) => new Cactus(world.ComponentManager, position, "cactus", data.GetData("Scale", 1.0f)));
            RegisterEntity("Berry Bush", (position, data) => new Bush(world.ComponentManager, position, "berrybush", data.GetData("Scale", 1.0f)));
            RegisterEntity("Bird", (position, data) => new Bird(ContentPaths.Entities.Animals.Birds.GetRandomBird(), position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Bird"));
            RegisterEntity("Bat", (position, data) => new Bat(world.ComponentManager, position));
            RegisterEntity("Scorpion", (position, data) => new Scorpion(ContentPaths.Entities.Animals.Scorpion.scorption_animation, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Scorpion"));
            RegisterEntity("Spider", (position, data) => new Spider(world.ComponentManager, ContentPaths.Entities.Animals.Spider.spider_animation, position));
            RegisterEntity("Frog", (position, data) => new Frog(ContentPaths.Entities.Animals.Frog.frog0_animation, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Frog"));
            RegisterEntity("Tree Frog", (position, data) => new Frog(ContentPaths.Entities.Animals.Frog.frog1_animation, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Frog"));
            RegisterEntity("Brown Rabbit", (position, data) => new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit0_animation, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Brown Rabbit"));
            RegisterEntity("White Rabbit", (position, data) => new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit1_animation, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "White Rabbit"));
            RegisterEntity("Deer", (position, data) => new Deer(ContentPaths.Entities.Animals.Deer.deer, position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Deer"));
            RegisterEntity("Dwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Worker], 0));
            //RegisterEntity("TestDwarf", (position, data) => GenerateTestDwarf(position));
            //RegisterEntity("TestGoblin", (position, data) => GenerateTestGoblin(position));
            //RegisterEntity("TestSkeleton", (position, data) => GenerateTestSeketon(position));
            //RegisterEntity("TestMoleman", (position, data) => GenerateTestMoleman(position));
            RegisterEntity("AxeDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.AxeDwarf], 0));
            RegisterEntity("CraftsDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.CraftsDwarf], 0));
            RegisterEntity("Wizard", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Wizard], 0));
            RegisterEntity("MusketDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.MusketDwarf], 0));
            RegisterEntity("Moleman", (position, data) => GenerateMoleman(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.ComponentManager.Factions.Factions["Molemen"], world.PlanService, "Molemen"));
            RegisterEntity("Goblin", (position, data) => GenerateGoblin(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.ComponentManager.Factions.Factions["Goblins"], world.PlanService, "Goblins"));
            RegisterEntity("Skeleton", (position, data) => GenerateSkeleton(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.ComponentManager.Factions.Factions["Undead"], world.PlanService, "Undead"));
            RegisterEntity("Necromancer", (position, data) => GenerateNecromancer(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.ComponentManager.Factions.Factions["Undead"], world.PlanService, "Undead"));
            RegisterEntity("Bed", (position, data) => new Bed(world.ComponentManager, position));
            RegisterEntity("Barrel", (position, data) => new Barrel(world.ComponentManager, position));
            RegisterEntity("Bear Trap", (position, data) => new BearTrap(world.ComponentManager, position));
            RegisterEntity("Lamp", (position, data) => new Lamp(world.ComponentManager, position));
            RegisterEntity("Table", (position, data) => new Table(world.ComponentManager, position));
            RegisterEntity("Chair", (position, data) => new Chair(world.ComponentManager, position));
            RegisterEntity("Flag", (position, data) => new Flag(world.ComponentManager, position));
            RegisterEntity("Mushroom", (position, data) => new Mushroom(world.ComponentManager, position, ContentPaths.Entities.Plants.mushroom, ResourceLibrary.ResourceType.Mushroom, 2, false));
            RegisterEntity("Cave Mushroom", (position, data) => new Mushroom(world.ComponentManager, position, ContentPaths.Entities.Plants.cavemushroom, ResourceLibrary.ResourceType.CaveMushroom, 4, true));
            RegisterEntity("Wheat", (position, data) => new Wheat(world.ComponentManager, position));
            RegisterEntity("Kitchen Table", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 7)) { Tags = new List<string>() { "Cutting Board" } });
            RegisterEntity("Books", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 4)) { Tags = new List<string>() { "Research" }, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f } });
            RegisterEntity("Potions", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 4)) { Tags = new List<string>() { "Research" }, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f } });
            RegisterEntity("Anvil", (position, data) => new Anvil(world.ComponentManager, position));
            RegisterEntity("Forge", (position, data) => new Forge(world.ComponentManager, position));
            RegisterEntity("Elf", (position, data) => GenerateElf(world, position, world.ComponentManager.Factions.Factions["Elf"], "Elf"));
            RegisterEntity("Demon", (position, data) => GenerateDemon(world, position, world.ComponentManager.Factions.Factions["Demon"], "Demon"));
            RegisterEntity("Arrow", (position, data) => new ArrowProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Bullet", (position, data) => new BulletProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Web", (position, data) => new WebProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fireball", (position, data) => new FireballProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fairy", (position, data) => new Fairy(world.ComponentManager, "Player", position));
            RegisterEntity("Target", (position, data) => new Target(world.ComponentManager, position));
            RegisterEntity("Stove", (position, data) => new Stove(world.ComponentManager, position));
            RegisterEntity("Strawman", (position, data) =>
            {
                float value = (float)MathFunctions.Random.NextDouble();
                return value < 0.33
                    ? (Body)(new Strawman(world.ComponentManager, position))
                    : (value < 0.66 ? (Body)(new WeightRack(world.ComponentManager,position)) : (Body)(new PunchingBag(world.ComponentManager, position)));
            });
            RegisterEntity("Bookshelf", (position, data) => new Bookshelf(world.ComponentManager, position));
            RegisterEntity("Wooden Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 1), 50));
            RegisterEntity("Metal Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 8), 100));
            RegisterEntity("Stone Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 8), 75));
            RegisterEntity("Wooden Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 0)));
            RegisterEntity("Stone Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 8)));
            RegisterEntity("Metal Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 8)));
            RegisterEntity("RandTrinket", (position, data) => CreateRandomTrinket(world, position));
            RegisterEntity("RandFood", (position, data) => CreateRandomFood(world, position));
            RegisterEntity("Turret", (position, data) => new TurretTrap(world.ComponentManager, position, world.PlayerFaction));
            RegisterEntity("Snow Cloud", (position, data) => new Weather.Cloud(world.ComponentManager, 0.1f, 50, 40, position) {TypeofStorm = Weather.StormType.SnowStorm});
            RegisterEntity("Rain Cloud", (position, data) => new Weather.Cloud(world.ComponentManager, 0.1f, 50, 40, position) { TypeofStorm = Weather.StormType.RainStorm });
            RegisterEntity("Storm", (position, data) =>
            {
                Weather.CreateForecast(world.Time.CurrentDate, world.ChunkManager.Bounds, world, 3); 
                Weather.CreateStorm(MathFunctions.RandVector3Cube() * 10, MathFunctions.Rand(0.05f, 1.0f), world);
                                                            return new Weather.Cloud(world.ComponentManager, 0.1f, 50, 40, position);
            });
            RegisterEntity("Chicken", (position, data) => new Chicken(position, world.ComponentManager, world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Chicken"));
            RegisterEntity("MudGolem", (position, data) => new MudGolem(new CreatureStats(new MudGolemClass(), 0), "Carnivore", world.PlanService,  world.ComponentManager.Factions.Factions["Carnivore"], world.ComponentManager, "Mud Golem", world.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, position));
            RegisterEntity("Mud", (position, data) => new MudProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
        }

        private static GameComponent CreateRandomFood(WorldManager world, Vector3 position)
        {
            List<Resource> foods = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.RawFood);

            Resource randresource = ResourceLibrary.CreateMeal(Datastructures.SelectRandom(foods).Type,
                Datastructures.SelectRandom(foods).Type);
            return new ResourceEntity(world.ComponentManager, randresource.Type, position);
        }


        public static ResourceEntity CreateRandomTrinket(WorldManager world, Vector3 pos)
        {
            Resource randResource = ResourceLibrary.GenerateTrinket("Gold", MathFunctions.Rand(0.1f, 3.5f));

            if (MathFunctions.RandEvent(0.5f))
            {
                randResource = ResourceLibrary.EncrustTrinket(randResource.Type, "Emerald");
            }

            return new ResourceEntity(world.ComponentManager, randResource.Type, pos);
        }

        public static void RegisterEntity<T>(string id, Func<Vector3, Blackboard, T> function) where T : GameComponent
        {
            if (EntityFuncs == null)
            {
                EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();
            }
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
            Body balloon = new Body(componentManager, "Balloon", componentManager.RootComponent,
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

            MinimapIcon minimapIcon = new MinimapIcon(balloon, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 0));

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
                return new List<InstanceData>();
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


        public static Body GenerateElf(WorldManager worldManger, Vector3 position, Faction faction, string allies)
        {
            CreatureStats stats = new CreatureStats(new ElfClass(), 0);
            return new Elf(stats, allies, worldManger.PlanService, faction, worldManger.ComponentManager, "Elf", worldManger.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, position).Physics;
        }


        public static Body GenerateDemon(WorldManager worldManager, Vector3 position, Faction faction, string allies)
        {
            CreatureStats stats = new CreatureStats(new DemonClass(), 0);
            return new Demon(stats, allies, worldManager.PlanService, faction, worldManager.ComponentManager, "Demon", worldManager.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, position).Physics;
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
            Dwarf toReturn =  new Dwarf(componentManager, stats, allies, planService, faction, "Dwarf", chunkManager, graphics, content, dwarfClass, position);
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, componentManager.World.Time.CurrentDate), false);
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
