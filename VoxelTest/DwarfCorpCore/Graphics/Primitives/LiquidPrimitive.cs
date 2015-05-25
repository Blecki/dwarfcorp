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
        private Dictionary<BoxFace, Vector3> faceDeltas = new Dictionary<BoxFace, Vector3>();
        private Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
        private Dictionary<BoxFace, bool> drawFace = new Dictionary<BoxFace, bool>();
        private List<ExtendedVertex> accumulatedVertices = new List<ExtendedVertex>();
        private static BoxPrimitive m_canconicalPrimitive = VoxelLibrary.GetPrimitive("water");
        public bool IsBuilding = false;

        public LiquidPrimitive(LiquidType type) :
            base()
        {
            LiqType = type;
            faceDeltas[BoxFace.Back] = new Vector3(0, 0, 1);
            faceDeltas[BoxFace.Front] = new Vector3(0, 0, -1);
            faceDeltas[BoxFace.Left] = new Vector3(-1, 0, 0);
            faceDeltas[BoxFace.Right] = new Vector3(1, 0, 0);
            faceDeltas[BoxFace.Top] = new Vector3(0, 1, 0);
            faceDeltas[BoxFace.Bottom] = new Vector3(0, -1, 0);
        }


        private class IntVector
        {
            public int x;
            public int y;
            public int z;

            public IntVector(Vector3 Vec)
            {
                x = (int) Vec.X;
                y = (int) Vec.Y;
                z = (int) Vec.Z;
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

        public void InitializeFromChunk(VoxelChunk chunk, GraphicsDevice graphics)
        {
            //chunk.PrimitiveMutex.WaitOne();
            if(!chunk.IsVisible || IsBuilding)
            {
               // chunk.PrimitiveMutex.ReleaseMutex();
                return;
            }

            IsBuilding = true;
            //chunk.PrimitiveMutex.ReleaseMutex();

            accumulatedVertices.Clear();
            faceExists.Clear();
            drawFace.Clear();

            int[,,] totalDepth = new int[chunk.SizeX, chunk.SizeY, chunk.SizeZ];
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    bool drynessEncountered = false;
                    int previousSum = 0;

                    for(int y = 0; y < chunk.SizeY; y++)
                    {
                        WaterCell cell = chunk.Data.Water[chunk.Data.IndexAt(x, y, z)];
                        byte waterLevel = cell.WaterLevel;

                        if(cell.Type != LiqType)
                        {
                            waterLevel = 0;
                        }

                        if(drynessEncountered)
                        {
                            if(waterLevel > 0)
                            {
                                drynessEncountered = false;
                                previousSum += waterLevel;
                                totalDepth[x, y, z] = previousSum;
                            }
                        }
                        else
                        {
                            if(waterLevel > 0)
                            {
                                previousSum += waterLevel;
                                totalDepth[x, y, z] = previousSum;
                            }
                            else
                            {
                                drynessEncountered = true;
                                previousSum = 0;
                                totalDepth[x, y, z] = 0;
                            }
                        }
                    }
                }
            }

            int maxY = chunk.SizeY;

            if(chunk.Manager.ChunkData.Slice == ChunkManager.SliceMode.Y)
            {
                maxY = (int) Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY);
            }


            Voxel myVoxel = chunk.MakeVoxel(0, 0, 0);
            Voxel vox = chunk.MakeVoxel(0, 0, 0);
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < maxY; y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        int index = chunk.Data.IndexAt(x, y, z);
                        if(chunk.Data.Water[index].WaterLevel > 0 && chunk.Data.Water[index].Type == LiqType)
                        {
                            bool isTop = false;

                            myVoxel.GridPosition = new Vector3(x, y, z);

                            for(int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace) i;
                                if(face == BoxFace.Bottom)
                                {
                                    continue;
                                }

                                Vector3 delta = faceDeltas[face];


                                bool success = chunk.Manager.ChunkData.GetVoxel(chunk, new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z) + chunk.Origin, ref vox);

                                if(success)
                                {
                                    if(face == BoxFace.Top)
                                    {
                                        if(vox.WaterLevel == 0 || y == (int) chunk.Manager.ChunkData.MaxViewingLevel)
                                        {
                                            drawFace[face] = true;
                                        }
                                        else
                                        {
                                            drawFace[face] = false;
                                        }
                                    }
                                    else
                                    {
                                        if(vox.WaterLevel == 0 && vox.IsEmpty)
                                        {
                                            drawFace[face] = true;
                                        }
                                        else
                                        {
                                            drawFace[face] = false;
                                        }

                                        bool gotVox = chunk.Manager.ChunkData.GetVoxel(chunk, new Vector3(x, y + 1, z) + chunk.Origin, ref vox);

                                        isTop = !gotVox || vox.IsEmpty || vox.WaterLevel == 0;
                                    }
                                }
                                else
                                {
                                    drawFace[face] = true;
                                }


                                if(!drawFace[face])
                                {
                                    continue;
                                }

                                IEnumerable<ExtendedVertex> vertices = CreateWaterFace(myVoxel, face, chunk, x, y, z, totalDepth[x, y, z], isTop);


                                foreach(ExtendedVertex newVertex in vertices.Select(vertex => new ExtendedVertex(vertex.Position + VertexNoise.GetRandomNoiseVector(vertex.Position),
                                    vertex.Color, vertex.TextureCoordinate, vertex.TextureBounds)))
                                {
                                    accumulatedVertices.Add(newVertex);
                                }
                            }
                        }
                    }
                }
            }


            try
            {
                ExtendedVertex[] vertex = new ExtendedVertex[accumulatedVertices.Count];

                for(int i = 0; i < accumulatedVertices.Count; i++)
                {
                    vertex[i] = accumulatedVertices[i];
                }


                Vertices = vertex;

                chunk.PrimitiveMutex.WaitOne();
                ResetBuffer(graphics);
                chunk.PrimitiveMutex.ReleaseMutex();
            }
            catch(System.Threading.AbandonedMutexException e)
            {
                Console.Error.WriteLine(e.Message);
            }

            IsBuilding = false;
        }

        private static IEnumerable<ExtendedVertex> CreateWaterFace(Voxel voxel, BoxFace face, VoxelChunk chunk, int x, int y, int z, int totalDepth, bool top)
        {
            List<ExtendedVertex> toReturn = new List<ExtendedVertex>();
            int idx = 0;
            int c = 0;
            int vertOffset = 0;
            int numVerts = 0;
            m_canconicalPrimitive.GetFace(face, m_canconicalPrimitive.UVs, out idx, out c, out vertOffset, out numVerts);

            for (int i = idx; i < idx + c; i++)
            {
                toReturn.Add(m_canconicalPrimitive.Vertices[m_canconicalPrimitive.Indices[i]]);
            }

            Vector3 origin = chunk.Origin + new Vector3(x, y, z);
            List<Voxel> neighborsVertex = new List<Voxel>();
            for(int i = 0; i < toReturn.Count; i ++)
            {
                VoxelVertex currentVertex = VoxelChunk.GetNearestDelta(toReturn[i].Position);
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

                float averageWaterHeight = (float) averageWaterLevel / 255.0f;
                float puddleness = 0;
                Vector2 uv;

                float foaminess = emptyNeighbors / count;

                if(foaminess <= 0.5f)
                {
                    foaminess = 0.0f;
                }

                if(totalDepth < 5)
                {
                    foaminess = 0.75f;
                    puddleness = 0;
                    uv = new Vector2((toReturn[i].Position.X + origin.X) / 80.0f, (toReturn[i].Position.Z + origin.Z) / 80.0f);
                }
                else
                {
                    uv = new Vector2((toReturn[i].Position.X + origin.X) / 80.0f, (toReturn[i].Position.Z + origin.Z) / 80.0f);
                }
                Vector4 bounds = new Vector4(0, 0, 1, 1);

                if(chunk.Data.Water[index].IsFalling || !top)
                {
                    averageWaterHeight = 1.0f;
                }

                if(face == BoxFace.Top)
                {
                    toReturn[i] = new ExtendedVertex(toReturn[i].Position + origin + new Vector3(0, (averageWaterHeight * 0.4f - 1.0f), 0),
                        new Color(foaminess, puddleness, (float) totalDepth / 512.0f, 1.0f),
                        uv, bounds);
                }
                else
                {
                    Vector3 offset = Vector3.Zero;
                    switch(face)
                    {
                        case BoxFace.Back:
                        case BoxFace.Front:
                            uv = new Vector2((Math.Abs(toReturn[i].Position.X + origin.X) / 80.0f), (Math.Abs(toReturn[i].Position.Y + origin.Y) / 80.0f));
                            foaminess = 1.0f;
                            offset = new Vector3(0, -0.5f, 0);
                            break;
                        case BoxFace.Right:
                        case BoxFace.Left:
                            uv = new Vector2((Math.Abs(toReturn[i].Position.Z + origin.Z) / 80.0f), (Math.Abs(toReturn[i].Position.Y + origin.Y) / 80.0f));
                            foaminess = 1.0f;
                            offset = new Vector3(0, -0.5f, 0);
                            break;
                        case BoxFace.Top:
                            offset = new Vector3(0, -0.5f, 0);
                            break;
                    }

                    toReturn[i] = new ExtendedVertex(toReturn[i].Position + origin + offset, new Color(foaminess, 0.0f, 1.0f, 1.0f), uv, bounds);
                }
            }

            return toReturn;
        }
    }

}