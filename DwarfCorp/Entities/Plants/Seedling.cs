using Microsoft.Xna.Framework;
using System;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Seedling : Plant
    {
        public double GrowthTime = 0.0f;
        public double GrowthHours = 0.0f;
        public float MaxSize = 2.0f;
        public float MinSize = 0.2f;
        public String AdultName;

        public String GoodBiomes = "";
        public String BadBiomes = "";
        private String CachedBiome = null;

        [JsonProperty] private bool HasGrown = false;

        public Seedling()
        {
            SetFlag(Flag.DontUpdate, false);
        }

        public Seedling(ComponentManager Manager, String AdultName, Vector3 position, String Asset) :
            base(Manager, "seedling", position, 0.0f, Vector3.One, Asset, 1.0f)
        {
            IsGrown = false;
            Name = AdultName + " seedling";
            this.AdultName = AdultName;
            AddChild(new Health(Manager, "HP", 1.0f, 0.0f, 1.0f));
            AddChild(new Flammable(Manager, "Flames"));
            CollisionType = CollisionType.Static;

            SetFlag(Flag.DontUpdate, false);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (HasGrown)
            {
                Die();
                return;
            }

            if (String.IsNullOrEmpty(CachedBiome))
                if (World.Overworld.Map.GetBiomeAt(LocalPosition).HasValue(out var biome))
                    CachedBiome = biome.Name;

            var factor = 1.0f;

            if (CachedBiome != null)
            {
                if (GoodBiomes.Contains(CachedBiome))
                    factor = 1.5f;
                if (BadBiomes.Contains(CachedBiome))
                    factor = 0.5f;
            }

            GrowthTime += gameTime.ElapsedGameTime.TotalMinutes * factor;

            var scale = (float)(MinSize + (MaxSize - MinSize) * (GrowthTime / GrowthHours));
            ReScale(scale);

            if (GrowthTime >= GrowthHours)
            {
                HasGrown = true;
                CreateAdult();
            }
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateCrossPrimitive(MeshAsset);
            base.CreateCosmeticChildren(Manager);
        }

        public void CreateAdult()
        {
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, Position, true);
            var adult = EntityFactory.CreateEntity<Plant>(AdultName, LocalPosition);
            adult.IsGrown = true;

            if (Farm != null)
            {
                adult.Farm = Farm;
                if (GameSettings.Current.AllowAutoFarming)
                {
                    var task = new ChopEntityTask(adult) { Priority = TaskPriority.Low };
                    World.TaskManager.AddTask(task);
                }
            }

            Die();
        }
    }
}
