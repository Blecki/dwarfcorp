using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class DestroyBlockSpell : Spell
    {
        public DestroyBlockSpell()
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            Description = "Magically destroys up to 8 stone, dirt, or other blocks.";
            Image = new ImageFrame(icons, 32, 2, 2);
            ManaCost = 10;
            Mode = Spell.SpellMode.SelectFilledVoxels;
            Name = "Destroy Blocks";
            Hint = "Click and drag to destroy blocks";
            Recharges = false;
        }
        public override void OnVoxelsSelected(SpellTree tree, List<Voxel> voxels)
        {
            bool destroyed = false;
            foreach (Voxel selected in voxels)
            {
                if (!selected.IsEmpty && !selected.Type.IsInvincible)
                {
                    if (OnCast(tree))
                    {
                        Vector3 p = selected.Position + Vector3.One * 0.5f;
                        IndicatorManager.DrawIndicator("-" + ManaCost + " M", p, 1.0f, Color.Red);
                        PlayState.ParticleManager.Trigger("star_particle", p,
                            Color.White, 4);
                        selected.Kill();
                        destroyed = true;
                    }
                }

            }
            if (destroyed)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, PlayState.CursorLightPos, true, 1.0f);
            }

            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
