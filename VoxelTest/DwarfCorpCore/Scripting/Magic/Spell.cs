using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Spell
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ImageFrame Image { get; set; }
        public float ManaCost { get; set; }
        public SpellMode Mode { get; set; }
        public Timer RechargeTimer { get; set; }
        public string Hint { get; set; }
        public bool Recharges { get; set; }

        public enum SpellMode
        {
            SelectFilledVoxels,
            SelectEmptyVoxels,
            SelectEntities,
            Button,
            Continuous
        }


        public Spell()
        {
            
        }

        public virtual void OnVoxelsSelected(List<Voxel> voxels)
        {
            
        }

        public virtual void OnEntitiesSelected(List<Body> entities)
        {
            
        }

        public virtual void OnButtonTriggered()
        {
            PlayState.GUI.ToolTipManager.Popup(Hint);
        }

        public virtual void OnContinuousUpdate(DwarfTime time)
        {
            
        }

        public virtual void Update(DwarfTime time, VoxelSelector voxSelector, BodySelector bodySelector)
        {
            if(Recharges)
                RechargeTimer.Update(time);

            switch (Mode)
            {
                case SpellMode.Button:
                    break;
                case SpellMode.Continuous:
                    OnContinuousUpdate(time);
                    break;
                case SpellMode.SelectEmptyVoxels:
                    voxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
                    voxSelector.Enabled = true;
                    bodySelector.Enabled = false;
                    break;
                case SpellMode.SelectFilledVoxels:
                    voxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    voxSelector.Enabled = true;
                    bodySelector.Enabled = false;
                    break;
                case SpellMode.SelectEntities:
                    bodySelector.Enabled = true;
                    break;
            }

            if (!Recharges || RechargeTimer.HasTriggered)
            {
                PlayState.ParticleManager.Trigger("star_particle", PlayState.CursorLightPos + Vector3.Up * 0.5f, Color.White, 2);
            }
        }

        public virtual void Render(DwarfTime time)
        {
            if (Recharges && !RechargeTimer.HasTriggered)
            {
                Drawer2D.DrawLoadBar(PlayState.CursorLightPos - Vector3.Up, Color.White, Color.Black, 150, 20, RechargeTimer.CurrentTimeSeconds / RechargeTimer.TargetTimeSeconds);
                Drawer2D.DrawTextBox("Charging...", PlayState.CursorLightPos + Vector3.Up * 2);
            }
        }


    }
}
