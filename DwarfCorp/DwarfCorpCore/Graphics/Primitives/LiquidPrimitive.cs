using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    /// <summary>
    /// Represents a list of liquid surfaces which are to be rendered. Only the top surfaces of liquids
    /// get rendered. Liquids are also "smoothed" in terms of their positions.
    /// </summary>
    public class LiquidPrimitive : GeometricPrimitive
    {
        public LiquidType LiqType { get; set; }
        private Vector3[] faceDeltas = new Vector3[6];
        private bool[] faceExists = new bool[6];
        private bool[] drawFace = new bool[6];
        private static readonly BoxPrimitive primitive = VoxelLibrary.GetPrimitive("water");
        public bool IsBuilding = false;

        public LiquidPrimitive(LiquidType type) :
            base()
        {
            LiqType = type;
            faceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
            faceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
            faceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
            faceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
            faceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
            faceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);
        }


        private class IntVector
        {
            public int x;
            public int y;
            public int z;

            public IntVector(Vector3 vec)
            {
                x = (int) vec.X;
                y = (int) vec.Y;
                z = (int) vec.Z;
            }

            public override bool Equals(object obj)
            {
                if(obj is IntVector)
                {
                    return (x == ((IntVector) obj).x && y == ((IntVector) obj).y && z == ((IntVector) obj).z);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z;
            }
        }

        public void InitializeFromChunk(VoxelChunk chunk)
        {
            if (IsBuilding)
                return;

            IsBuilding = true;

            int maxY = (int)Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY);


            Voxel myVoxel = chunk.MakeVoxel(0, 0, 0);
            Voxel vox = chunk.MakeVoxel(0, 0, 0);
            int maxVertex = 0;
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < maxY; y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        int index = chunk.Data.IndexAt(x, y, z);
                        if (GameSettings.Default.FogofWar && !chunk.Data.IsExplored[index]) continue;

                        if(chunk.Data.Water[index].WaterLevel > 0 && chunk.Data.Water[index].Type == LiqType)
                        {
                            bool isTop = false;

                            myVoxel.GridPosition = new Vector3(x, y, z);

                            for(int i = 0; i < 6; i++)
                            {
                                if (Vertices == null)
                                {
                                    Vertices = new ExtendedVertex[256];
                                }
                                else if (Vertices.Length <= maxVertex + 6)
                                {
                                    ExtendedVertex[] newVerts = new ExtendedVertex[Vertices.Length * 2];
                                    Vertices.CopyTo(newVerts, 0);
                                    Vertices = newVerts;
                                }

                                BoxFace face = (BoxFace) i;
                                if(face == BoxFace.Bottom)
                                {
                                    continue;
                                }

                                Vector3 delta = faceDeltas[(int)face];


                                bool success = chunk.Manager.ChunkData.GetVoxel(chunk, new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z) + chunk.Origin, ref vox);

                                if(success)
                                {
                                    if(face == BoxFace.Top)
                                    {
                                        if(vox.WaterLevel == 0 || y == (int) chunk.Manager.ChunkData.MaxViewingLevel)
                                        {
                                            drawFace[(int)face] = true;
                                        }
                                        else
                                        {
                                            drawFace[(int)face] = false;
                                        }
                                    }
                                    else
                                    {
                                        if(vox.WaterLevel == 0 && vox.IsEmpty)
                                        {
                                            drawFace[(int)face] = true;
                                        }
                                        else
                                        {
                                            drawFace[(int)face] = false;
                                        }

                                        bool gotVox = chunk.Manager.ChunkData.GetVoxel(chunk, new Vector3(x, y + 1, z) + chunk.Origin, ref vox);

                                        isTop = !gotVox || vox.IsEmpty || vox.WaterLevel == 0;
                                    }
                                }
                                else
                                {
                                    drawFace[(int)face] = true;
                                }


                                if(!drawFace[(int)face])
                                {
                                    continue;
                                }

                                CreateWaterFace(myVoxel, face, chunk, x, y, z, isTop, Vertices, maxVertex);
                                maxVertex += 6;
                            }
                        }
                    }
                }
            }

            if (maxVertex > 0)
            {
                try
                {
                    lock (VertexLock)
                    {
                        MaxVertex = maxVertex;
                        VertexBuffer = null;
                    }
                }
                catch (System.Threading.AbandonedMutexException e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
            else
            {
                try
                {
                    lock (VertexLock)
                    {
                        VertexBuffer = null;
                        MaxVertex = -1;
                        MaxIndex = -1;
                    }
                }
                catch (System.Threading.AbandonedMutexException e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
            IsBuilding = false;
        }

        private static void CreateWaterFace(Voxel voxel, 
                                            BoxFace face, 
                                            VoxelChunk chunk, 
                                            int x, int y, int z, 
                                            bool top, 
                                            ExtendedVertex[] vertices, 
                                            int startVertex)
        {

            int idx = 0;
            int vertexCount = 0;
            int vertOffset = 0;
            int numVerts = 0;
            primitive.GetFace(face, primitive.UVs, out idx, out vertexCount, out vertOffset, out numVerts);

            for (int i = idx; i < idx + vertexCount; i++)
            {
                vertices[i + startVertex - idx] = primitive.Vertices[primitive.Indexes[i]];
            }

            Vector3 origin = chunk.Origin + new Vector3(x, y, z);
            List<Voxel> neighborsVertex = new List<Voxel>();

            for(int i = 0; i < vertexCount; i ++)
            {
                VoxelVertex currentVertex = VoxelChunk.GetNearestDelta(vertices[i + startVertex].Position);
                chunk.GetNeighborsVertex(currentVertex, voxel, neighborsVertex);
                int index = chunk.Data.IndexAt(x, y, z);
                float averageWaterLevel = chunk.Data.Water[index].WaterLevel;
                float count = 1.0f;
                float emptyNeighbors = 0.0f;

                foreach(byte level in neighborsVertex.Select(vox => vox.WaterLevel))
                {
                    averageWaterLevel += level;
                    count++;

                    if(level < 1)
                    {
                        emptyNeighbors++;
                    }
                }

                averageWaterLevel = averageWaterLevel / count;

                float averageWaterHeight = (float)averageWaterLevel / 8.0f;
                float foaminess = emptyNeighbors / count;

                if(foaminess <= 0.5f)
                {
                    foaminess = 0.0f;
                }

                /*
                if(chunk.Data.Water[index].IsFalling || !top)
                {
                    averageWaterHeight = 1.0f;
                }
                 */

                Vector3 pos = vertices[i + startVertex].Position;
                pos.Y *= averageWaterHeight;
                pos += origin;

                switch (face)
                {
                    case BoxFace.Back:
                    case BoxFace.Front:
                        vertices[i + startVertex].Set(pos,
                                                      new Color(foaminess, 0.0f, 1.0f, 1.0f),
                                                      Color.White,
                                                      new Vector2(pos.X, pos.Y),
                                                      new Vector4(0, 0, 1, 1));
                        break;
                    case BoxFace.Right:
                    case BoxFace.Left:
                        vertices[i + startVertex].Set(pos,
                                                    new Color(foaminess, 0.0f, 1.0f, 1.0f),
                                                    Color.White,
                                                    new Vector2(pos.Z, pos.Y),
                                                    new Vector4(0, 0, 1, 1));
                        break;
                    case BoxFace.Top:
                        vertices[i + startVertex].Set(pos,
                                            new Color(foaminess, 0.0f, 1.0f, 1.0f),
                                            Color.White,
                                            new Vector2(pos.X, pos.Z),
                                            new Vector4(0, 0, 1, 1));
                        break;
                }
            }
        }
    }

}