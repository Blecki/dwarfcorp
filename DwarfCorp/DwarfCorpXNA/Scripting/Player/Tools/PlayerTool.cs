// PlayerTool.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        public abstract void OnVoxelsDragged(List<TemporaryVoxelHandle> voxels, InputManager.MouseButton button);
        public abstract void OnVoxelsSelected(List<TemporaryVoxelHandle> voxels, InputManager.MouseButton button);
        public abstract void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button);
        public abstract void OnMouseOver(IEnumerable<Body> bodies);
        public abstract void Update(DwarfGame game, DwarfTime time);
        public abstract void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time);
        public abstract void OnBegin();
        public abstract void OnEnd();

        public virtual void OnConfirm(List<CreatureAI> minions)
        {
            if (minions.Count > 0)
            {
                Vector3 avgPostiion = Vector3.Zero;
                foreach (CreatureAI creature in minions)
                {
                    avgPostiion += creature.Position;
                }
                avgPostiion /= minions.Count;
                minions.First().Creature.NoiseMaker.MakeNoise("Ok", avgPostiion);
            }
        }

        public virtual void DefaultOnMouseOver(IEnumerable<Body> bodies)
        {
            StringBuilder sb = new StringBuilder();

            List<Body> bodyList = bodies.ToList();
            for (int i = 0; i < bodyList.Count; i++)
            {
                sb.Append(bodyList[i].Name);
                if (i < bodyList.Count - 1)
                {
                    sb.Append("\n");
                }
            }
            Player.World.ShowTooltip(sb.ToString());
        }

        public virtual void Destroy()
        {

        }
    }
}
