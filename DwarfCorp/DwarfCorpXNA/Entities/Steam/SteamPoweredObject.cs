using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class SteamPoweredObject : Body
    {
        [JsonIgnore]
        public List<UInt32> NeighborPipes = new List<UInt32>();

        public float SteamPressure = 0.0f;
        public float GeneratedSteam = 0.0f;
        public bool Generator = false;
        
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }

        public SteamPoweredObject()
        {
            CollisionType = CollisionType.Static;
        }

        public SteamPoweredObject(
            ComponentManager Manager) :
            base(Manager, "Steam Powered", Matrix.Identity, 
                Vector3.One,
                Vector3.Zero)
        { 
            CollisionType = CollisionType.Static;
            
            CreateCosmeticChildren(Manager);
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (HasMoved)
            {
                DetachFromNeighbors();
                AttachToNeighbors();
            }

            base.Update(Time, Chunks, Camera);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Debugger.Switches.DrawPipeNetwork)
            {
                foreach (var neighborConnection in NeighborPipes)
                {
                    var neighbor = Manager.FindComponent(neighborConnection);
                    if (neighbor == null)
                        Drawer3D.DrawLine(Position, Position + Vector3.UnitY, Color.CornflowerBlue, 0.1f);
                    else
                        Drawer3D.DrawLine(Position + new Vector3(0.0f, 0.5f, 0.0f), (neighbor as Body).Position + new Vector3(0.0f, 0.5f, 0.0f), new Color(SteamPressure, 0.0f, 0.0f, 1.0f), 0.1f);
                }

                Drawer3D.DrawBox(GetBoundingBox(), Color.Red, 0.01f, false);
            }
        }

        private void DetachFromNeighbors()
        {
            foreach (var neighbor in NeighborPipes.Select(connection => Manager.FindComponent(connection)))
            {
                if (neighbor is SteamPoweredObject neighborPipe)
                    neighborPipe.DetachNeighbor(this.GlobalID);
            }

            NeighborPipes.Clear();
        }

        private void DetachNeighbor(uint ID)
        {
            NeighborPipes.RemoveAll(connection => connection == ID);
        }

        private void AttachToNeighbors()
        {
            System.Diagnostics.Debug.Assert(NeighborPipes.Count == 0);

            foreach (var entity in Manager.World.EnumerateIntersectingObjects(this.BoundingBox.Expand(0.1f), CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                var neighborPipe = entity as SteamPoweredObject;
                if (neighborPipe == null) continue;

                var distance = (neighborPipe.Position - Position).Length2D();
                if (distance > 1.1f) continue;

                AttachNeighbor(neighborPipe.GlobalID);
                neighborPipe.AttachNeighbor(this.GlobalID);
            }
        }

        private void AttachNeighbor(uint ID)
        {
            NeighborPipes.Add(ID);
        }

        public override void Delete()
        {
            base.Delete();
            DetachFromNeighbors();
        }

        public override void Die()
        {
            base.Die();
            DetachFromNeighbors();
        }
    }
}
