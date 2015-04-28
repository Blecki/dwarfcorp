using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        public Timer SelectionTimer = new Timer(0.25f, false);

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
                    ManaCost = 0;
                    Mode = Spell.SpellMode.SelectFilledVoxels;
                    Name = "Inspect Blocks";
                    Description = "Mouse over a block to get info about it";
                    Hint = "Mouse over a block for info";
                    RechargeTimer = new Timer(0.1f, true);
                    break;
                }
                case InspectType.InspectEntity:
                {
                    Image = new ImageFrame(icons, 32, 7, 1);
                    ManaCost = 0;
                    Mode = Spell.SpellMode.Continuous;
                    Name = "Inspect Objects";
                    Description = "Select an entity to get info about it";
                    Hint = "Mouse over entities for info";
                    RechargeTimer = new Timer(0.1f, true);
                    break;
                }
            }
        }

        public override void Update(DwarfTime time, VoxelSelector voxSelector, BodySelector bodySelector)
        {
            SelectionTimer.Update(time);
            if (SelectionTimer.HasTriggered)
            {
                if (Type == InspectType.InspectEntity)
                {
                    MouseState mouse = Mouse.GetState();
                    List<Body> selected = bodySelector.SelectBodies(new Rectangle(mouse.X - 10, mouse.Y - 10, 20, 20));

                    if (selected.Count > 0)
                    {
                        OnEntitiesSelected(PlayState.Master.Spells, selected);
                    }
                }
                else
                {
                    Voxel vox = new Voxel();
                    PlayState.ChunkManager.ChunkData.GetNonNullVoxelAtWorldLocation(PlayState.CursorLightPos, ref vox);

                    OnVoxelsSelected(PlayState.Master.Spells, new List<Voxel>(){vox});
                }
            }
            base.Update(time, voxSelector, bodySelector);
        }

        public override void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            if (this.Type != InspectType.InspectEntity) return;

            string desc = "";
            bool first = true;
            foreach (Body body in entities)
            {
                if (!first) desc += "\n";
                desc += body.GetDescription();
                first = false;
            }

            if (desc != "")
            {
                PlayState.GUI.ToolTipManager.ToolTip = desc;
            }
            else
            {
                PlayState.GUI.ToolTipManager.ToolTip = "";
            }
            base.OnEntitiesSelected(tree, entities);
        }

        public override void OnVoxelsSelected(SpellTree tree, List<Voxel> voxels)
        {
            if (this.Type != InspectType.InspectBlock) return;


            string description = "";
            bool first = true;
            foreach (Voxel selected in voxels)
            {
                if (!selected.IsEmpty)
                {
                    if (!first)
                    {
                        description += "\n";
                    }
                    else
                    {
                        first = false;
                    }
                    description +=  selected.TypeName + " at " + selected.GridPosition + ". Health: " + selected.Health;
                }
            }

            if (description != "")
            {
                PlayState.GUI.ToolTipManager.ToolTip = description;
            }
            else
            {
                PlayState.GUI.ToolTipManager.ToolTip = "";
            }
            base.OnVoxelsSelected(tree, voxels);
        }
    }
}
