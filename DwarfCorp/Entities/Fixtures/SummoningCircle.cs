using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SummoningCircle : CraftedFixture
    {
        public static int TeleportDistance = 15;

        [EntityFactory("Summoning Circle")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new SummoningCircle(Manager, Position, Data);
        }

        [JsonIgnore]
        public Timer ParticleTimer = new Timer(0.5f, false);

        public SummoningCircle()
        {
            ParticleTimer = new Timer(0.5f + MathFunctions.Rand(-0.25f, 0.25f), false);
        }

        public SummoningCircle(ComponentManager Manager, Vector3 Position, Blackboard Data) :
            base("Summoning Circle", new String[] { "Teleporter" },
                Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(4, 2),
                Data.GetData<List<ResourceAmount>>("Resources", null))
        {
            OrientMode = SimpleSprite.OrientMode.Fixed;
            ParticleTimer = new Timer(0.5f + MathFunctions.Rand(-0.25f, 0.25f), false);
            AddChild(new MagicalObject(Manager));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);
            var sprite = GetComponent<SimpleSprite>();
            sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
            sprite.LocalTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f) * Matrix.CreateTranslation(Vector3.UnitY * -0.45f);
            sprite.LightsWithVoxels = false;
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (Active)
            {                
                ParticleTimer.Update(Time);
                if (ParticleTimer.HasTriggered)
                {
                    float t = (float)Time.TotalGameTime.TotalSeconds * 0.5f;
                    Vector3 pos = Position + MathFunctions.RandVector3Cube();
                    World.ParticleManager.Trigger("green_flame", pos, Color.White, 1);
                }
                
            }
            base.Update(Time, Chunks, Camera);
        }
    }
}
