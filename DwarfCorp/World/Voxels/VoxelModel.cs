using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsharpVoxReader;
using CsharpVoxReader.Chunks;

namespace DwarfCorp
{
    public class VoxelModel : CsharpVoxReader.IVoxLoader
    {
        public Point3 Dimensions;
        public byte[,,] Data;

        public byte Index(int X, int Y, int Z)
        {
            return Data[X,Y,Z];
        }

        void IVoxLoader.LoadModel(int sizeX, int sizeY, int sizeZ, byte[,,] data)
        {
            this.Dimensions = new Point3(sizeX, sizeY, sizeZ);
            Data = data;
        }

        void IVoxLoader.LoadPalette(uint[] palette)
        {

        }

        void IVoxLoader.NewGroupNode(int id, Dictionary<string, byte[]> attributes, int[] childrenIds)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.NewLayer(int id, Dictionary<string, byte[]> attributes)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.NewMaterial(int id, Dictionary<string, byte[]> attributes)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.NewShapeNode(int id, Dictionary<string, byte[]> attributes, int[] modelIds, Dictionary<string, byte[]>[] modelsAttributes)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.NewTransformNode(int id, int childNodeId, int layerId, Dictionary<string, byte[]>[] framesAttributes)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.SetMaterialOld(int paletteId, MaterialOld.MaterialTypes type, float weight, MaterialOld.PropertyBits property, float normalized)
        {
            //throw new NotImplementedException();
        }

        void IVoxLoader.SetModelCount(int count)
        {
            //throw new NotImplementedException();
        }
    }
}
