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
    /// <summary>
    /// Component causes the object its attached to to become flammable. Flammable objects have "heat"
    /// when the heat is above a "flashpoint" they get damaged until they are destroyed, and emit flames.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Flammable : GameComponent
    {
        public Body LocParent { get; set; }
        public Health Health { get; set; }

        public float Heat { get; set; }
        public float Flashpoint { get; set; }
        public float Damage { get; set; }

        public Timer CheckLavaTimer { get; set; }
        public Timer SoundTimer { get; set; }
        public Timer DamageTimer { get; set; }

        public Flammable()
        {
            
        }

        public Flammable(ComponentManager manager, string name, Body parent, Health health) :
            base(name, parent)
        {
            LocParent = parent;
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 50.0f;
            Health = health;
            CheckLavaTimer = new Timer(1.0f, false);
            SoundTimer = new Timer(1.0f, false);
            DamageTimer = new Timer(1.0f, false);
        } 



        public void CheckForLavaAndWater(DwarfTime gameTime, ChunkManager chunks)
        {

            BoundingBox expandedBoundingBox = LocParent.BoundingBox.Expand(0.5f);

            List<Voxel> voxels = chunks.GetVoxelsIntersecting(expandedBoundingBox);

            foreach(Voxel currentVoxel in voxels)
            {
                WaterCell cell = currentVoxel.Water;

                if (cell.WaterLevel == 0) continue;
                else if (cell.Type == LiquidType.Lava)
                {
                    Heat += 100;
                }
                else if (cell.Type == LiquidType.Water)
                {
                    Heat -= 100;
                    Heat = Math.Max(0.0f, Heat);
                }
            }
        }

        public int GetNumTrigger()
        {
            return
                (int)
                    MathFunctions.Clamp((int) (Math.Abs(1*LocParent.BoundingBox.Max.Y - LocParent.BoundingBox.Min.Y)), 1,
                        3);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            DamageTimer.Update(gameTime);
            CheckLavaTimer.Update(gameTime);
            SoundTimer.Update(gameTime);
            if(CheckLavaTimer.HasTriggered)
            {
                CheckForLavaAndWater(gameTime, chunks);
            }

            if(Heat > Flashpoint)
            {
                Heat *= 1.01f;

                if(DamageTimer.HasTriggered)
                    Health.Damage(Damage, Health.DamageType.Fire);

                if(SoundTimer.HasTriggered)
                    SoundManager.PlaySound(ContentPaths.Audio.fire, LocParent.Position, true, 0.5f);
                double totalSize = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 4.0f) + 1;

                for(int i = 0; i < numFlames; i++)
                {
                    Vector3 extents = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min);
                    Vector3 randomPoint = LocParent.BoundingBox.Min + new Vector3(extents.X * MathFunctions.Rand(), extents.Y * MathFunctions.Rand(), extents.Z * MathFunctions.Rand());
                    PlayState.ParticleManager.Trigger("flame", randomPoint, Color.White, GetNumTrigger());
                }
            }

            base.Update(gameTime, chunks, camera);
        }
    }

}