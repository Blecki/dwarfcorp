using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// The player's tools are a state machine. A build tool is a particular player
    /// state. Contains callbacks to when voxels are selected.
    /// </summary>
    public abstract class PlayerTool
    {
        public GameMaster Player { get; set; }

        public abstract void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button);
        public abstract void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button);

        public abstract void Update(DwarfGame game, GameTime time);

        public abstract void Render(DwarfGame game, GraphicsDevice graphics, GameTime time);

    }
}
