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
        private Health _health = null;
        [JsonIgnore]
        public Health Health
        {
            get
            {
                if (_health == null)
                {
                    _health = Parent.EnumerateAll().Where(c => c is Health).FirstOrDefault() as Health;
                    System.Diagnostics.Debug.Assert(_health != null, "Flammable could not find a Health component.");
                }

                return _health;
            }
        }

        [JsonIgnore]
        public bool IsOnFire
        {
            get
            {
                return Heat >= Flashpoint;
            }
        }

        public float Heat { get; set; }
        public float Flashpoint { get; set; }
        public float Damage { get; set; }

        public Timer CheckLavaTimer { get; set; }
        public Timer SoundTimer { get; set; }
        public Timer DamageTimer { get; set; }

        public Flammable()
        {
            UpdateRate = 100;
        }

        public Flammable(ComponentManager manager, string name) :
            base(name, manager)
        {
            UpdateRate = 100;
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 5.0f;
            CheckLavaTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            SoundTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            DamageTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
        }


        public void CheckForLavaAndWater(Body Body, DwarfTime gameTime, ChunkManager chunks)
        {
            foreach (var coordinate in VoxelHelpers.EnumerateCoordinatesInBoundingBox(Body.BoundingBox))
            {
                var voxel = new VoxelHandle(chunks.ChunkData, coordinate);
                if (!voxel.IsValid || voxel.LiquidLevel == 0) continue;

                if (voxel.LiquidType == LiquidType.Lava)
                    Heat += 100.0f;
                else if (voxel.LiquidType == LiquidType.Water)
                    Heat = Math.Max(0.0f, Heat - 100.0f);
            }
        }

        public int GetNumTrigger(Body Body)
        {
            return
                (int)
                    MathFunctions.Clamp((int) (Math.Abs(1*Body.BoundingBox.Max.Y - Body.BoundingBox.Min.Y)), 1,
                        3);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active) return;

            var body = Parent as Body;
            System.Diagnostics.Debug.Assert(body != null);

            DamageTimer.Update(gameTime);
            CheckLavaTimer.Update(gameTime);
            SoundTimer.Update(gameTime);
            if(CheckLavaTimer.HasTriggered)
            {
                CheckForLavaAndWater(body, gameTime, chunks);
            }
            Heat *= 0.999f;

            if(Heat > Flashpoint)
            {
                UpdateRate = 1;
                if(DamageTimer.HasTriggered)
                    Health.Damage(Damage, Health.DamageType.Fire);

                if(SoundTimer.HasTriggered)
                    SoundManager.PlaySound(ContentPaths.Audio.fire, body.Position, true, 1.0f);
                double totalSize = (body.BoundingBox.Max - body.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 4.0f) + 1;

                for(int i = 0; i < numFlames; i++)
                {
                    Vector3 extents = (body.BoundingBox.Max - body.BoundingBox.Min);
                    Vector3 randomPoint = body.BoundingBox.Min + new Vector3(extents.X * MathFunctions.Rand(), extents.Y * MathFunctions.Rand(), extents.Z * MathFunctions.Rand());
                    Manager.World.ParticleManager.Trigger("flame", randomPoint, Color.White, GetNumTrigger(body));
                }
            }
        }
    }

}