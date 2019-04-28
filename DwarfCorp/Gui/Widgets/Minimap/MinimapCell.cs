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
            Texture = new Texture2D(Device, VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeZ);
            ColorData = new Color[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ];
        }

        public void RedrawFromColumn(GlobalChunkCoordinate Column, ChunkManager Chunks)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var index = (z * VoxelConstants.ChunkSizeX) + x;
                    var surface = VoxelHelpers.FindFirstVisibleVoxelBelowIncludingWater(Chunks.CreateVoxelHandle(new GlobalVoxelCoordinate((Column.X * VoxelConstants.ChunkSizeX) + x, Chunks.World.WorldSizeInVoxels.Y - 1, (Column.Z * VoxelConstants.ChunkSizeZ) + z)));
                    ColorData[index] = ChooseColor(surface);
                }

            Texture.SetData(ColorData);
        }

        private Color ChooseColor(VoxelHandle Of)
        {
            if (!Of.IsValid) return new Color(0.0f, 0.0f, 0.0f, 1.0f);

            return new Color((byte)(Of.Coordinate.Y * 4), (byte)(Of.Coordinate.Y * 4), (byte)(Of.Coordinate.Y * 4), 255);
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
