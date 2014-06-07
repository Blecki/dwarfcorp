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
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuildTool : PlayerTool
    {
        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {
            Player.Faction.RoomBuilder.VoxelsSelected(voxels, button);
            Player.Faction.PutDesignator.VoxelsSelected(voxels, button);
        }

        public override void Update(DwarfGame game, GameTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            PlayState.GUI.IsMouseVisible = true;

            PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver() ? GUISkin.MousePointer.Pointer : GUISkin.MousePointer.Build;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            Player.Faction.RoomBuilder.Render(time, PlayState.ChunkManager.Graphics);
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }
}
