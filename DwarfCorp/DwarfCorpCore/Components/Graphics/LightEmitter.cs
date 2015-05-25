using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This component dynamically lights up voxels around it with torch light.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class LightEmitter : Body
    {
        public DynamicLight Light { get; set; }


        public LightEmitter()
        {
            
        }

        public LightEmitter(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, float intensity, float range) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Light = new DynamicLight(intensity, range);
        }

        public void UpdateLight()
        {
            Light.Position = GlobalTransform.Translation;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            UpdateLight();

            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            Light.Destroy();
            base.Die();
        }
    }

}