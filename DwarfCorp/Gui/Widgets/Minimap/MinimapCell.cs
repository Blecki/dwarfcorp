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

        public void RedrawFromColumn(GlobalChunkCoordinate Column, ChunkManager Chunks)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var index = (z * VoxelConstants.ChunkSizeX * 2 * 2) + (x * 2);
                    var surface = VoxelHelpers.FindFirstVisibleVoxelBelowIncludingWater(Chunks.CreateVoxelHandle(new GlobalVoxelCoordinate((Column.X * VoxelConstants.ChunkSizeX) + x, Chunks.World.WorldSizeInVoxels.Y - 1, (Column.Z * VoxelConstants.ChunkSizeZ) + z)));
                    var color = ChooseColor(surface);

                    ColorData[index] = new Color(color);
                    ColorData[index + 1] = new Color(color * new Vector4(0.75f, 0.75f, 0.75f, 1.0f));
                    ColorData[index + (VoxelConstants.ChunkSizeX * 2)] = new Color(color * new Vector4(0.75f, 0.75f, 0.75f, 1.0f));
                    ColorData[index + (VoxelConstants.ChunkSizeX * 2) + 1] = new Color(color);
                }

            Texture.SetData(ColorData);
        }

        private Vector4 ChooseColor(VoxelHandle Of)
        {
            if (!Of.IsValid) return new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            if (Of.GrassType != 0)
            {
                var grass = GrassLibrary.GetGrassType(Of.GrassType);
            }

            return new Vector4((Of.Coordinate.Y * 4) / 256.0f, (Of.Coordinate.Y * 4) / 256.0f, (Of.Coordinate.Y * 4) / 256.0f, 1.0f);
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
