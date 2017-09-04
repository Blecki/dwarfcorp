// WallBuilder.cs
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
//using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A designation specifying that a creature should put a voxel of a given type
    /// at a location.
    /// </summary>
    public class WallBuilder
    {
        public VoxelHandle Vox;
        public VoxelType Type;
        public CreatureAI ReservedCreature = null;
        private WorldManager World { get; set; }
        List<VoxelHandle> highlighted = new List<VoxelHandle>(); 
        public WallBuilder(VoxelHandle v, VoxelType t, WorldManager world)
        {
            World = world;
            Vox = v;
            Type = t;
        }

        public void Put(ChunkManager manager)
        {
            VoxelHandle v = Vox;
            v.Type = Type;
            v.WaterCell = new WaterCell();
            v.Health = Type.StartingHealth;
            
            World.ParticleManager.Trigger("puff", v.WorldPosition, Color.White, 20);

            foreach(Physics phys in manager.World.CollisionManager.EnumerateIntersectingObjects(Vox.GetBoundingBox(), CollisionManager.CollisionType.Dynamic).OfType<Physics>())
            {
                phys.ApplyForce((phys.GlobalTransform.Translation - (Vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))) * 100, 0.01f);
                BoundingBox box = v.GetBoundingBox();
                Physics.Contact contact = new Physics.Contact();
                Physics.TestStaticAABBAABB(box, phys.GetBoundingBox(), ref contact);

                if(!contact.IsIntersecting)
                {
                    continue;
                }

                Vector3 diff = contact.NEnter * contact.Penetration;
                Matrix m = phys.LocalTransform;
                m.Translation += diff;
                phys.LocalTransform = m;
            }
        }
    }

    [JsonObject(IsReference = true)]
    public class PutDesignator
    {
        public Faction Faction { get; set; }
        public List<WallBuilder> Designations { get; set; }
        public VoxelType CurrentVoxelType { get; set; }
        private List<VoxelHandle> Selected { get; set; }
        private bool verified = false;
            [JsonIgnore]
        public WorldManager World { get; set; }

        [OnDeserialized]
        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public PutDesignator()
        {
            
        }

        public PutDesignator(Faction faction, WorldManager world)
        {
            World = world;
            Faction = faction;
            Designations = new List<WallBuilder>();
            Selected = new List<VoxelHandle>();
        }

        public CreatureAI GetReservedCreature(VoxelHandle reference)
        {
            WallBuilder des = GetDesignation(reference);

            if(des == null)
            {
                return null;
            }

            return des.ReservedCreature;
        }

        public bool IsDesignation(GlobalVoxelCoordinate Location)
        {
            return Designations.Any(d => d.Vox.Coordinate == Location);
        }

        // Todo: %KILL% 
        public bool IsDesignation(VoxelHandle reference)
        {
            foreach(WallBuilder put in Designations)
            {
                if (put.Vox == reference)
                    return true;
            }

            return false;
        }

        public WallBuilder GetDesignation(GlobalVoxelCoordinate Location)
        {
            return Designations.FirstOrDefault(d => d.Vox.Coordinate == Location);
        }

        // Todo: %KILL%
        public WallBuilder GetDesignation(VoxelHandle v)
        {
            foreach(WallBuilder put in Designations)
            {
                if (put.Vox == v)
                    return put;
            }

            return null;
        }

        public void AddDesignation(WallBuilder des)
        {
            Designations.Add(des);
        }

        public void RemoveDesignation(WallBuilder des)
        {
            Designations.Remove(des);
        }


        public void RemoveDesignation(VoxelHandle v)
        {
            WallBuilder des = GetDesignation(v);

            if(des != null)
            {
                RemoveDesignation(des);
            }
        }


        public void Render(DwarfTime gameTime, GraphicsDevice graphics, Shader effect)
        {
            DepthStencilState state = graphics.DepthStencilState;
            graphics.DepthStencilState = DepthStencilState.DepthRead;
            
            float t = (float)gameTime.TotalGameTime.TotalSeconds;
            float st = (float) Math.Sin(t * 4) * 0.5f + 0.5f;
            effect.MainTexture = World.ChunkManager.ChunkData.Tilemap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = new Color(0.1f, 0.9f, 1.0f, 0.5f * st + 0.45f);
            effect.SetTexturedTechnique();
            foreach(WallBuilder put in Designations)
            {
                //Drawer3D.DrawBox(put.Vox.GetBoundingBox(), Color.LightBlue, st * 0.01f + 0.05f);
                effect.World = Matrix.CreateTranslation(put.Vox.WorldPosition + Vector3.Up * 0.15f);

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    VoxelLibrary.GetPrimitive(put.Type.Name).Render(graphics);
                }
            }

            if (Selected == null)
            {
                Selected = new List<VoxelHandle>();
            }

            if (CurrentVoxelType == null)
            {
                Selected.Clear();
            }

            effect.VertexColorTint = verified ? new Color(0.0f, 1.0f, 0.0f, 0.5f * st + 0.45f) : new Color(1.0f, 0.0f, 0.0f, 0.5f * st + 0.45f);
            Vector3 offset = World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? Vector3.Zero : Vector3.Up * 0.15f;
            foreach (var voxel in Selected)
            {
  
                effect.World = Matrix.CreateTranslation(voxel.WorldPosition + offset);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    VoxelLibrary.GetPrimitive(CurrentVoxelType).Render(graphics);
                }
            }

            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.World = Matrix.Identity;
            graphics.DepthStencilState = state;
        }

        public void VoxelDragged(List<VoxelHandle> refs)
        {
            if (CurrentVoxelType == null)
                return;
            verified = true;
            //verified = Verify(refs, CurrentVoxelType.ResourceToRelease);

            if (!verified)
            {
                World.ShowToolPopup("Can't build this! Need at least " + refs.Count + " " + ResourceLibrary.Resources[CurrentVoxelType.ResourceToRelease].ResourceName + ".");
            }
            else
            {
                World.ShowToolPopup("Release to build.");
            }

            Selected.Clear();

            foreach (var voxel in refs)
            {
                Selected.Add(voxel);
            }
        }

        public bool Verify(List<VoxelHandle> refs, ResourceLibrary.ResourceType type)
        {
            ResourceAmount requiredResources = new ResourceAmount(type, refs.Count);
            List<ResourceAmount> res = new List<ResourceAmount>() {requiredResources};
            return Faction.HasResources(res);
        }

        public void VoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if (Faction == null)
            {
                Faction = World.PlayerFaction;
            }

            if(CurrentVoxelType == null)
            {
                return;
            }
            Selected.Clear();
            switch(button)
            {
                case (InputManager.MouseButton.Left):
                {
                    if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Build).Count == 0)
                    {
                        World.ShowToolPopup("None of the selected units can build walls.");
                        return;
                    }
                    List<Task> assignments = new List<Task>();
                    var validRefs = refs.Where(r => !IsDesignation(r) && World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? r.IsEmpty : !r.IsEmpty).ToList();

                    /*
                    if (!Verify(validRefs, CurrentVoxelType.ResourceToRelease))
                    {
                        World.ShowToolPopup("Can't build this! Need at least " + validRefs.Count + " " + ResourceLibrary.Resources[CurrentVoxelType.ResourceToRelease].ResourceName + ".");
                        return;
                    }
                     */

                    foreach (var r in validRefs)
                    {
                        AddDesignation(new WallBuilder(r, CurrentVoxelType, World));
                        assignments.Add(new BuildVoxelTask(r, CurrentVoxelType));
                    }

                    TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(World.Master.SelectedMinions, GameMaster.ToolMode.Build));

                    break;
                }
                case (InputManager.MouseButton.Right):
                {
                    foreach(var r in refs)
                    {
                        if(!IsDesignation(r))
                        {
                            continue;
                        }
                        else
                        {
                            RemoveDesignation(r);
                        }
                    }
                    break;
                }
            }
        }

        public int GetNumDesignations(ResourceLibrary.ResourceType resourceToRelease)
        {
            return Designations.Sum(d => d.Type.ResourceToRelease == resourceToRelease ? 1 : 0);
        }
    }

}
