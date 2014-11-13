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
    public class WorkPile : Body
    {
        public Sprite Sprite { get; set; }

        public WorkPile()
        {
            
        }

        public WorkPile(Vector3 position) :
            base("WorkPile", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), Vector3.One, Vector3.Zero, false)
        {
            Sprite = new Sprite(Manager, "WorkSprite", this, Matrix.Identity, TextureManager.GetTexture(ContentPaths.Entities.DwarfObjects.underconstruction), false);
            Sprite.SetSingleFrameAnimation();
        }
    }
}
