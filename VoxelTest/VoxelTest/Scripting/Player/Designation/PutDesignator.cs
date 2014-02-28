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
    public class PutDesignation
    {
        public VoxelRef Vox;
        public VoxelType Type;
        public CreatureAIComponent ReservedCreature = null;

        public PutDesignation(VoxelRef v, VoxelType t)
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

            List<LocatableComponent> components = new List<LocatableComponent>();
            manager.Components.GetComponentsIntersecting(Vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic);

            foreach(PhysicsComponent phys in components.OfType<PhysicsComponent>())
            {
                phys.ApplyForce((phys.GlobalTransform.Translation - (Vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))) * 100, 0.01f);
                BoundingBox box = v.GetBoundingBox();
                PhysicsComponent.Contact contact = new PhysicsComponent.Contact();
                PhysicsComponent.TestStaticAABBAABB(box, phys.GetBoundingBox(), ref contact);

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
        public List<PutDesignation> Designations { get; set; }
        public VoxelType CurrentVoxelType { get; set; }

        public Texture2D BlockTextures { get; set; }

        public PutDesignator()
        {
            
        }

        public PutDesignator(Faction faction, Texture2D blockTextures)
        {
            Faction = faction;
            Designations = new List<PutDesignation>();
            BlockTextures = blockTextures;
        }

        public CreatureAIComponent GetReservedCreature(VoxelRef reference)
        {
            PutDesignation des = GetDesignation(reference);

            if(des == null)
            {
                return null;
            }

            return des.ReservedCreature;
        }

        public bool IsDesignation(VoxelRef reference)
        {
            foreach(PutDesignation put in Designations)
            {
                if((put.Vox.WorldPosition - reference.WorldPosition).LengthSquared() < 0.1)
                {
                    return true;
                }
            }

            return false;
        }


        public PutDesignation GetDesignation(VoxelRef v)
        {
            foreach(PutDesignation put in Designations)
            {
                if((put.Vox.WorldPosition - v.WorldPosition).LengthSquared() < 0.1)
                {
                    return put;
                }
            }

            return null;
        }

        public void AddDesignation(PutDesignation des)
        {
            Designations.Add(des);
        }

        public void RemoveDesignation(PutDesignation des)
        {
            Designations.Remove(des);
        }


        public void RemoveDesignation(VoxelRef v)
        {
            PutDesignation des = GetDesignation(v);

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
            foreach(PutDesignation put in Designations)
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
                    foreach(VoxelRef r in refs)
                    {
                        if(IsDesignation(r) || r.TypeName != "empty")
                        {
                            continue;
                        }
                        else
                        {
                            AddDesignation(new PutDesignation(r, CurrentVoxelType));
                        }
                    }
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