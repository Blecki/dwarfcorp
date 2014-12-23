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
            CheckLavaTimer = new Timer(2.5f, false);
        }



        public void CheckForLava(GameTime gameTime, ChunkManager chunks)
        {

            BoundingBox expandedBoundingBox = LocParent.BoundingBox.Expand(0.5f);

            List<Voxel> voxels = chunks.GetVoxelsIntersecting(expandedBoundingBox);

            if((from currentVoxel in voxels
                where currentVoxel != null
                select currentVoxel.Water).Any(cell => cell.WaterLevel > 0 && cell.Type == LiquidType.Lava))
            {
                Heat += 100;
            }
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            CheckLavaTimer.Update(gameTime);

            if(CheckLavaTimer.HasTriggered)
            {
                CheckForLava(gameTime, chunks);
            }

            if(Heat > Flashpoint)
            {
                Heat *= 1.01f;
                Health.Damage(Damage * (float) gameTime.ElapsedGameTime.TotalSeconds);

                double totalSize = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 2.0f) + 1;

                for(int i = 0; i < numFlames; i++)
                {
                    Vector3 extents = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min);
                    Vector3 randomPoint = LocParent.BoundingBox.Min + new Vector3(extents.X * MathFunctions.Rand(), extents.Y * MathFunctions.Rand(), extents.Z * MathFunctions.Rand());
                    PlayState.ParticleManager.Trigger("flame", randomPoint, Color.White, 1);
                }
            }

            base.Update(gameTime, chunks, camera);
        }
    }

}