using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class FlammableComponent : GameComponent
    {
        public LocatableComponent LocParent { get; set; }
        public HealthComponent Health { get; set; }
       
        public float Heat { get; set; }
        public float Flashpoint { get; set; }
        public float Damage { get; set; }

        public FlammableComponent(ComponentManager manager, string name, LocatableComponent parent, HealthComponent health) :
            base(manager, name, parent)
        {
            LocParent = parent;
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 50.0f;
            Health = health;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Heat > Flashpoint)
            {
                Heat *= 1.01f;
                Health.Damage(Damage * (float)gameTime.ElapsedGameTime.TotalSeconds);

                double totalSize = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min).Length();
                int numFlames = (int)(totalSize / 2.0f) + 1;

                for (int i = 0; i < numFlames; i++)
                {
                    Vector3 extents = (LocParent.BoundingBox.Max - LocParent.BoundingBox.Min);
                    Vector3 randomPoint = LocParent.BoundingBox.Min + new Vector3(extents.X * (float)PlayState.random.NextDouble(), extents.Y * (float)PlayState.random.NextDouble(), extents.Z * (float)PlayState.random.NextDouble());
                    PlayState.ParticleManager.Trigger("flame", randomPoint, Color.White, 1);
                }

            }

            base.Update(gameTime, chunks, camera);
        }
    }
}
