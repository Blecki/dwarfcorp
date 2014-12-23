using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class InspectSpell : Spell
    {
        public enum InspectType
        {
            InspectBlock,
            InspectEntity
        }

        public InspectType Type { get; set; }

        public InspectSpell(InspectType type)
        {
            Type = type;
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);
            Recharges = false;
            switch (type)
            {
                case InspectType.InspectBlock:
                {
                    Image = new ImageFrame(icons, 32, 7, 1);
                    ManaCost = 10;
                    Mode = Spell.SpellMode.SelectFilledVoxels;
                    Name = "Inspect Blocks";
                    Description = "Click on a block to get info about it";
                    Hint = "Click a block for info";
                    RechargeTimer = new Timer(0.1f, true);
                    break;
                }
                case InspectType.InspectEntity:
                {
                    Image = new ImageFrame(icons, 32, 7, 1);
                    ManaCost = 50;
                    Mode = Spell.SpellMode.SelectEntities;
                    Name = "Inspect Objects";
                    Description = "Select an entity to get info about it";
                    Hint = "Click an entity for info";
                    RechargeTimer = new Timer(0.1f, true);
                    break;
                }
            }
        }

        public override void OnEntitiesSelected(List<Body> entities)
        {
            if (this.Type != InspectType.InspectEntity) return;

            string desc = "";
            foreach (Body body in entities)
            {
                desc += body.GetDescription() + "\n";
            }

            if(desc != "")
                PlayState.GUI.ToolTipManager.Popup(desc);
            base.OnEntitiesSelected(entities);
        }

        public override void OnVoxelsSelected(List<Voxel> voxels)
        {
            if (this.Type != InspectType.InspectBlock) return;

            if (!RechargeTimer.HasTriggered) return;

            string description = "";

            foreach (Voxel selected in voxels)
            {
                if (!selected.IsEmpty)
                {
                    description += selected.TypeName + ". Health: " + selected.Health + "\n";
                }
            }
            
            if(description != "")
                PlayState.GUI.ToolTipManager.Popup(description);
            base.OnVoxelsSelected(voxels);
        }
    }
}
