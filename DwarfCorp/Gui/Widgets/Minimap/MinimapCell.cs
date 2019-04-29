using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.Gui.Widgets.Minimap
{
    public class MinimapCell : IDisposable
    {
        public Texture2D Texture;
        public Color[] ColorData;

        public MinimapCell(GraphicsDevice Device)
        {
            Texture = new Texture2D(Device, VoxelConstants.ChunkSizeX * 2, VoxelConstants.ChunkSizeZ * 2);
            ColorData = new Color[VoxelConstants.ChunkSizeX * 2 * VoxelConstants.ChunkSizeZ * 2];
        }

        private static void FindSurfaceColor(VoxelHandle V, WorldManager World, out Color Color, out VoxelHandle SurfaceVoxel)
        {
            Color = Color.Black;
            SurfaceVoxel = V;

            if (!V.IsValid) return;

            SurfaceVoxel = V.Chunk.Manager.CreateVoxelHandle(V.Coordinate + new GlobalVoxelOffset(0, -1, 0));

            while (true)
            {
                if (!SurfaceVoxel.IsValid) return;

                if (SurfaceVoxel.IsVisible)
                {
                    foreach (var designation in World.PlayerFaction.Designations.EnumerateDesignations(SurfaceVoxel))
                    {
                        if ((designation.Type & World.DesignationDrawer.VisibleTypes) == designation.Type)
                        {
                            var props = DesignationDrawer.DefaultProperties;
                            if (DesignationDrawer.DesignationProperties.ContainsKey(designation.Type))
                                props = DesignationDrawer.DesignationProperties[designation.Type];

                            Color = props.Color;
                            return;
                        }
                    }

                    if (!SurfaceVoxel.IsExplored)
                    {
                        Color = Color.Black;
                        return;
                    }
                    else if (SurfaceVoxel.LiquidType == LiquidType.Water)
                    {
                        Color = VoxelLibrary.GetVoxelType("water").MinimapColor;
                        return;
                    }
                    else if (SurfaceVoxel.LiquidType == LiquidType.Lava)
                    {
                        Color = VoxelLibrary.GetVoxelType("lava").MinimapColor;
                        return;
                    }
                    else if (SurfaceVoxel.GrassType != 0)
                    {
                        Color = GrassLibrary.GetGrassType(SurfaceVoxel.GrassType).MinimapColor;
                        return;
                    }
                    else if (!SurfaceVoxel.IsEmpty)
                    {
                        Color = SurfaceVoxel.Type.MinimapColor;
                        return;
                    }
                }

                SurfaceVoxel = SurfaceVoxel.Chunk.Manager.CreateVoxelHandle(SurfaceVoxel.Coordinate + new GlobalVoxelOffset(0, -1, 0));
            }
        }

        public void RedrawFromColumn(GlobalChunkCoordinate Column, ChunkManager Chunks)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var v = Chunks.CreateVoxelHandle(new GlobalVoxelCoordinate((Column.X * VoxelConstants.ChunkSizeX) + x, Chunks.World.WorldSizeInVoxels.Y - 1, (Column.Z * VoxelConstants.ChunkSizeZ) + z));

                    var color = Color.Black;
                    var surface = VoxelHandle.InvalidHandle;
                    FindSurfaceColor(v, Chunks.World, out color, out surface);
                    var depthAdjust = ((float)surface.Coordinate.Y / (Chunks.World.WorldSizeInVoxels.Y * 2.0f)) + 0.5f;
                    color *= depthAdjust;
                    color.A = 255;

                    var secondary = color * 0.75f;
                    secondary.A = 255;

                    var index = (z * VoxelConstants.ChunkSizeX * 2 * 2) + (x * 2);

                    ColorData[index] = color;
                    ColorData[index + 1] = secondary;
                    ColorData[index + (VoxelConstants.ChunkSizeX * 2)] = secondary;
                    ColorData[index + (VoxelConstants.ChunkSizeX * 2) + 1] = color;
                }

            Texture.SetData(ColorData);
        }
        
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                Texture.Dispose();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MinimapCell() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
