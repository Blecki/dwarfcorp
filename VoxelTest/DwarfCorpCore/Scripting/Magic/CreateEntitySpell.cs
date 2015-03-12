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
    public class CreateEntitySpell : Spell
    {
        public string Entity { get; set; }
        public bool Transmute { get; set; }

        public CreateEntitySpell()
        {
            
        }

        public CreateEntitySpell(string entity, bool transmute)
        {
            Entity = entity;
            Transmute = transmute;
            ManaCost = 50;
            Mode = transmute ? SpellMode.SelectEntities : SpellMode.SelectEmptyVoxels;
            Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 4, 2);
        }

        public void CreateEntity(Vector3 position)
        {
            EntityFactory.CreateEntity<Body>(Entity, position);
            PlayState.ParticleManager.Trigger("star_particle", position, Color.White, 4);
            Vector3 p = position + Vector3.Up;
            IndicatorManager.DrawIndicator("-" + ManaCost + " M", p, 1.0f, Color.Red);
        }

        public override void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            if (!Transmute) return;

            foreach (Body body in entities)
            {
                if (OnCast(tree))
                {
                    CreateEntity(body.Position);
                    body.Delete();
                }
            }

            base.OnEntitiesSelected(tree, entities);
        }

        public override void OnVoxelsSelected(SpellTree tree, List<Voxel> voxels)
        {
            if (Transmute) return;
            bool got = false;
            foreach (Voxel voxel in voxels)
            {
                if (voxel.IsEmpty)
                {
                    if (OnCast(tree))
                    {
                        CreateEntity(voxel.Position + Vector3.One*0.5f);
                        got = true;
                    }
                }
            }

            if (got)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, PlayState.CursorLightPos, true, 1.0f);
            }

            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
