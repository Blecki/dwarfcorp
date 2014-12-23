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
            ManaCost = 100;
            Mode = Spell.SpellMode.SelectFilledVoxels;
            Name = "Destroy Blocks";
            Hint = "Click and drag to destroy blocks";
            RechargeTimer = new Timer(5.0f, true);
            Recharges = true;
        }
        public override void OnVoxelsSelected(List<Voxel> voxels)
        {
            if (!RechargeTimer.HasTriggered) return;
            bool destroyed = false;
            int i = 0;
            foreach (Voxel selected in voxels)
            {

                if (!selected.IsEmpty && !selected.Type.IsInvincible)
                {
                    PlayState.ParticleManager.Trigger("star_particle", selected.Position + Vector3.One * 0.5f, Color.White, 4);
                    selected.Kill();
                    i++;
                    destroyed = true;
                }

                if (i >= 8) break;
            }
            if (destroyed)
            {
                SoundManager.PlaySound(ContentPaths.Audio.tinkle, PlayState.CursorLightPos, true, 1.0f);
            }
            RechargeTimer.Reset(RechargeTimer.TargetTimeSeconds);
            base.OnVoxelsSelected(voxels);
        }
    }
}
