using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class PutDesignation
    {
        public VoxelRef vox;
        public VoxelType type;
        public CreatureAIComponent reservedCreature = null;

        public PutDesignation(VoxelRef v, VoxelType t)
        {
            vox = v;
            type = t;
        }

        public void Put(ChunkManager manager)
        {
            VoxelChunk chunk = manager.ChunkMap[vox.ChunkID];
            chunk.VoxelGrid[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z] = new Voxel(vox.WorldPosition, type, VoxelLibrary.GetPrimitive(type.name), true);

            Voxel v = chunk.VoxelGrid[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z];
            v.Chunk = chunk;

            chunk.Water[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z].WaterLevel = 0;
            chunk.NotifyTotalRebuild(!chunk.VoxelGrid[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z].IsInterior);

            PlayState.ParticleManager.Trigger("puff", v.Position, Color.White, 20);

            List<LocatableComponent> components = new List<LocatableComponent>();
            manager.Components.GetComponentsIntersecting(vox.GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic);

            foreach(PhysicsComponent phys in components.OfType<PhysicsComponent>())
            {
                phys.ApplyForce((phys.GlobalTransform.Translation - (vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))) * 100, 0.01f);
                BoundingBox box = v.GetBoundingBox();
                PhysicsComponent.Contact contact = new PhysicsComponent.Contact();
                PhysicsComponent.TestStaticAABBAABB(box, phys.GetBoundingBox(), ref contact);

                if(!contact.isIntersecting)
                {
                    continue;
                }

                Vector3 diff = contact.nEnter * contact.penetration;
                Matrix m = phys.LocalTransform;
                m.Translation += diff;
                phys.LocalTransform = m;
            }
        }
    }

    [JsonObject(IsReference = true)]
    public class PutDesignator
    {
        public GameMaster Master { get; set; }
        public List<PutDesignation> Designations { get; set; }
        public VoxelType CurrentVoxelType { get; set; }

        [JsonIgnore]
        public Texture2D BlockTextures { get; set; }

        public PutDesignator(GameMaster master, Texture2D blockTextures)
        {
            Master = master;
            Designations = new List<PutDesignation>();
            Master.VoxSelector.Selected += VoxelsSelected;
            BlockTextures = blockTextures;
        }

        public CreatureAIComponent GetReservedCreature(VoxelRef reference)
        {
            PutDesignation des = GetDesignation(reference);

            if(des == null)
            {
                return null;
            }

            return des.reservedCreature;
        }

        public bool IsDesignation(VoxelRef reference)
        {
            foreach(PutDesignation put in Designations)
            {
                if((put.vox.WorldPosition - reference.WorldPosition).LengthSquared() < 0.1)
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
                if((put.vox.WorldPosition - v.WorldPosition).LengthSquared() < 0.1)
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
                effect.Parameters["xWorld"].SetValue(Matrix.CreateTranslation(put.vox.WorldPosition));

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    VoxelLibrary.GetPrimitive(put.type.name).Render(graphics);
                }
            }

            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
        }

        public void VoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if(CurrentVoxelType == null || Master.GodMode.IsActive)
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