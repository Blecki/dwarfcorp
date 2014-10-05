using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class WorldGUIObject : Body
    {
        public GUIComponent GUIObject { get; set; }
        public WorldGUIObject()
        {

        }

        public WorldGUIObject(Body parent, GUIComponent guiObject) :
            base("GUIObject", parent, Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            GUIObject = guiObject;
            AddToOctree = false;
            FrustrumCull = false;
            IsVisible = false;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (IsVisible && camera.IsInView(GetBoundingBox()))
            {
                Vector3 screenPos = camera.Project(GlobalTransform.Translation);
                GUIObject.LocalBounds = new Rectangle((int)screenPos.X - GUIObject.LocalBounds.Width/2, (int)screenPos.Y - GUIObject.LocalBounds.Height/2, GUIObject.LocalBounds.Width, GUIObject.LocalBounds.Height);

                GUIObject.IsVisible = true;
            }
            else
            {
                GUIObject.IsVisible = false;
            }

            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            GUIObject.Destroy();
            base.Die();
        }

        public override void Delete()
        {
            GUIObject.Destroy();
            base.Delete();
        }

        
    }
}

