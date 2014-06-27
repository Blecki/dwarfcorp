using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
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
        public VoxelRef Vox;
        public VoxelType Type;
        public CreatureAI ReservedCreature = null;

        public WallBuilder(VoxelRef v, VoxelType t)
        {
            Vox = v;
            Type = t;
        }

        public void Put(ChunkManager manager)
        {
            VoxelChunk chunk = manager.ChunkData.ChunkMap[Vox.ChunkID];
            chunk.VoxelGrid[(int) Vox.GridPosition.X][(int) Vox.GridPosition.Y][(int) Vox.GridPosition.Z] = new Voxel(Vox.WorldPosition, Type, VoxelLibrary.GetPrimitive(Type.Name), true);

            Voxel v = chunk.VoxelGrid[(int) Vox.GridPosition.X][(int) Vox.GridPosition.Y][(int) Vox.GridPosition.Z];
            v.Chunk = chunk;

            chunk.Water[(int) Vox.GridPosition.X][(int) Vox.GridPosition.Y][(int) Vox.GridPosition.Z].WaterLevel = 0;
            chunk.NotifyTotalRebuild(!chunk.VoxelGrid[(int) Vox.GridPosition.X][(int) Vox.GridPosition.Y][(int) Vox.GridPosition.Z].IsInterior);

            PlayState.ParticleManager.Trigger("puff", v.Position, Color.White, 20);

            List<Body> components = new List<Body>();
            manager.Components.GetBodiesIntersecting(Vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic);

            foreach(Physics phys in components.OfType<Physics>())
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

        public Texture2D BlockTextures { get; set; }

        public PutDesignator()
        {
            
        }

        public PutDesignator(Faction faction, Texture2D blockTextures)
        {
            Faction = faction;
            Designations = new List<WallBuilder>();
            BlockTextures = blockTextures;
        }

        public CreatureAI GetReservedCreature(VoxelRef reference)
        {
            WallBuilder des = GetDesignation(reference);

            if(des == null)
            {
                return null;
            }

            return des.ReservedCreature;
        }

        public bool IsDesignation(VoxelRef reference)
        {
            foreach(WallBuilder put in Designations)
            {
                if((put.Vox.WorldPosition - reference.WorldPosition).LengthSquared() < 0.1)
                {
                    return true;
                }
            }

            return false;
        }


        public WallBuilder GetDesignation(VoxelRef v)
        {
            foreach(WallBuilder put in Designations)
            {
                if((put.Vox.WorldPosition - v.WorldPosition).LengthSquared() < 0.1)
                {
                    return put;
                }
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


        public void RemoveDesignation(VoxelRef v)
        {
            WallBuilder des = GetDesignation(v);

            if(des != null)
            {
                RemoveDesignation(des);
            }
        }


        public void Render(GameTime gametime, GraphicsDevice graphics, Effect effect)
        {
            float t = (float) gametime.TotalGameTime.TotalSeconds;
            float st = (float) Math.Sin(t * 4) * 0.5f + 0.5f;
            effect.Parameters["xTexture"].SetValue(BlockTextures);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 2.0f, 0.5f * st + 0.45f));
            //Matrix oldWorld = effect.Parameters["xWorld"].GetValueMatrix();
            foreach(WallBuilder put in Designations)
            {
                effect.Parameters["xWorld"].SetValue(Matrix.CreateTranslation(put.Vox.WorldPosition));

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    VoxelLibrary.GetPrimitive(put.Type.Name).Render(graphics);
                }
            }

            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
        }

        public void VoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if(CurrentVoxelType == null)
            {
                return;
            }
            switch(button)
            {
                case (InputManager.MouseButton.Left):
                {
                    List<Task> assignments = new List<Task>();
                    foreach(VoxelRef r in refs)
                    {
                        if(IsDesignation(r) || r.TypeName != "empty")
                        {
                            continue;
                        }
                        else
                        {
                            AddDesignation(new WallBuilder(r, CurrentVoxelType));
                            assignments.Add(new BuildVoxelTask(new TagList(CurrentVoxelType.Name), r, CurrentVoxelType));
                        }
                    }

                    TaskManager.AssignTasks(assignments, PlayState.Master.SelectedMinions);

                    break;
                }
                case (InputManager.MouseButton.Right):
                {
                    foreach(VoxelRef r in refs)
                    {
                        if(!IsDesignation(r) || r.TypeName != "empty")
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
    }

}