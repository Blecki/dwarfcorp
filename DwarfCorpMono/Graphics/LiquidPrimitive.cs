using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{

        public class LiquidPrimitive : GeometricPrimitive
        {
            public LiquidType LiqType { get; set; }
            Dictionary<BoxFace, Vector3> faceDeltas = new Dictionary<BoxFace, Vector3>();
            Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
            Dictionary<BoxFace, bool> drawFace = new Dictionary<BoxFace, bool>();
            List<ExtendedVertex> accumulatedVertices = new List<ExtendedVertex>();
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


            class IntVector
            {

                public int x;
                public int y;
                public int z;

                public IntVector(Vector3 Vec)
                {
                    x = (int)Vec.X;
                    y = (int)Vec.Y;
                    z = (int)Vec.Z;
                }

                public override bool Equals(object obj)
                {
                    if (obj is IntVector)
                    {
                        return (x == ((IntVector)obj).x && y == ((IntVector)obj).y && z == ((IntVector)obj).z);
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
                chunk.PrimitiveMutex.WaitOne();
                if (!chunk.IsVisible || IsBuilding)
                {
                    chunk.PrimitiveMutex.ReleaseMutex();
                    return;
                }

                IsBuilding = true;
                chunk.PrimitiveMutex.ReleaseMutex();

                accumulatedVertices.Clear();
                faceExists.Clear();
                drawFace.Clear();

                Vector3 delta = Vector3.Zero;
                List<Voxel> neighbors = new List<Voxel>();
                List<VoxelRef> currentVox = new List<VoxelRef>();
                Dictionary<IntVector, List<KeyValuePair<VertexPositionColorTexture, int>>> indexedVertices = new Dictionary<IntVector, List<KeyValuePair<VertexPositionColorTexture, int>>>();
                int[, , ] totalDepth = new int[chunk.SizeX, chunk.SizeY, chunk.SizeZ];
                for (int x = 0; x < chunk.SizeX; x++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        bool drynessEncountered = false;
                        int previousSum = 0;

                        for (int y = 0; y < chunk.SizeY; y++)
                        {
                            WaterCell cell = chunk.Water[x][y][z];
                            byte waterLevel = cell.WaterLevel;

                            if (cell.Type != LiqType)
                            {
                                waterLevel = 0;
                            }

                            if (drynessEncountered)
                            {
                                if (waterLevel > 0)
                                {
                                    drynessEncountered = false;
                                    previousSum += waterLevel;
                                    totalDepth[x, y, z] = previousSum;
                                }
                            }
                            else
                            {
                                if (waterLevel > 0)
                                {
                                    drynessEncountered = false;
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

                if (chunk.Manager.Slice == ChunkManager.SliceMode.Y)
                {
                    maxY = (int)Math.Min(chunk.Manager.MaxViewingLevel + 1, chunk.SizeY);
                }

                for (int x = 0; x < chunk.SizeX; x++)
                {
                    for (int y = 0; y < maxY; y++)
                    {
                        for (int z = 0; z < chunk.SizeZ; z++)
                        {
                            if ( chunk.Water[x][y][ z].WaterLevel > 0 && chunk.Water[x][y][z].Type == LiqType)
                            {
                                bool isTop = false;

                                VoxelRef myVoxel = new VoxelRef();
                                myVoxel.ChunkID = chunk.ID;
                                myVoxel.WorldPosition = new Vector3(x, y, z) + chunk.Origin;
                                myVoxel.GridPosition = new Vector3(x, y, z);
                                myVoxel.TypeName = "empty";
                                

                                for (int i = 0; i < 6; i++)
                                {
                                    BoxFace face = (BoxFace)i;
                                    if (face == BoxFace.Bottom)
                                    {
                                        continue;
                                    }

                                    delta = faceDeltas[face];
      

                                    List<VoxelRef> vox = new List<VoxelRef>();
                                    chunk.Manager.GetVoxelReferencesAtWorldLocation(chunk, new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z) + chunk.Origin, vox);

                                    if (vox.Count > 0)
                                    {
                                        if (face == BoxFace.Top)
                                        {
                                            if (vox[0].GetWaterLevel(chunk.Manager) == 0 || y == (int)chunk.Manager.MaxViewingLevel)
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
                                            if ((int)vox[0].GetWaterLevel(chunk.Manager) == 0 && vox[0].TypeName == "empty")
                                            {
                                                drawFace[face] = true;
                                            }
                                            else
                                            {
                                                drawFace[face] = false;
                                            }

                                            vox.Clear();
                                            chunk.Manager.GetVoxelReferencesAtWorldLocation(chunk, new Vector3(x , y + 1, z ) + chunk.Origin, vox);

                                            isTop = vox.Count == 0 || vox[0].GetWaterLevel(chunk.Manager) == 0;
                                        }
                                    }
                                    else
                                    {
                                        drawFace[face] = true;
                                    }
         

                                    if (drawFace[face])
                                    {
                                        List<ExtendedVertex> vertices = createWaterFace(myVoxel, face, chunk, x, y, z, totalDepth[x, y, z], isTop);


                                        foreach (ExtendedVertex vertex in vertices)
                                        {
                                            ExtendedVertex newVertex;
             
                                                    newVertex
                                                         = new ExtendedVertex(vertex.Position + VertexNoise.GetRandomNoiseVector(vertex.Position),
                                                                                          vertex.Color, vertex.TextureCoordinate, vertex.TextureBounds);

   
                                                accumulatedVertices.Add(newVertex);
                                        }

                                    }
                                }

                            }

                        }
                    }
                }


                try
                {
                    

                    ExtendedVertex[] vertex = new ExtendedVertex[accumulatedVertices.Count];
                  
                    for (int i = 0; i < accumulatedVertices.Count; i++)
                    {
                        vertex[i] = accumulatedVertices[i];
                    }



                    m_vertices = vertex;

                    chunk.PrimitiveMutex.WaitOne();
                    ResetBuffer(graphics);
                    chunk.PrimitiveMutex.ReleaseMutex();
                }
                catch (System.Threading.AbandonedMutexException e)
                {
                    Console.Error.WriteLine(e.Message);
                }

                IsBuilding = false;
            }

            List<ExtendedVertex> createWaterFace(VoxelRef voxel, BoxFace face, VoxelChunk chunk, int x, int y, int z, int totalDepth, bool top)
            {
                List<ExtendedVertex> toReturn = new List<ExtendedVertex>();
                toReturn.AddRange(m_canconicalPrimitive.GetFace(face));
               
                Vector3 origin = chunk.Origin + new Vector3(x, y, z);
                List<VoxelRef> neighborsVertex = new List<VoxelRef>();
                for (int i = 0; i < toReturn.Count; i ++)
                {
                    VoxelVertex currentVertex = VoxelChunk.GetNearestDelta(toReturn[i].Position);
                    neighborsVertex.Clear();
                    chunk.GetNeighborsVertex(currentVertex, voxel, neighborsVertex, true);

                   float totalWaterLevel = totalDepth;
                   float averageWaterLevel = chunk.Water[x][ y][ z].WaterLevel;
                   float count = 1.0f;
                   float emptyNeighbors = 0.0f;

                    foreach (VoxelRef vox in neighborsVertex)
                    {
                        float level = vox.GetWaterLevel(chunk.Manager);
                        totalWaterLevel += level;
                        averageWaterLevel += level;

                        /*
                        if(level < 0.01f)
                        {
                            averageWaterLevel += 200;
                            totalWaterLevel += 200;
                        }
                         */

                        count++;

                        if (level < 0.1f)
                        {
                            emptyNeighbors++;
                
                        }

                    }

                    totalWaterLevel = ((float)totalDepth / (float)totalWaterLevel);
                    averageWaterLevel = averageWaterLevel / count;

                    float averageWaterHeight = (float)averageWaterLevel / 255.0f;
                    float waterHeight = ((float)chunk.Water[x][y][z].WaterLevel / 255.0f);
                    float puddleness = 0;
                    Vector2 UV = toReturn[i].TextureCoordinate;


                    float foaminess = emptyNeighbors / count;

                    if (foaminess <= 0.5f)
                    {
                        foaminess = 0.0f;
                    }

                    if (totalDepth < 5)
                    {
                        foaminess = 0.75f;
                        puddleness = 0;
                        UV = new Vector2((toReturn[i].Position.X + origin.X) / 80.0f , (toReturn[i].Position.Z + origin.Z) / 80.0f);
                    }
                    else
                    {
                        UV = new Vector2((toReturn[i].Position.X + origin.X) / 80.0f , (toReturn[i].Position.Z + origin.Z) / 80.0f);
                    }
                    Vector4 bounds = new Vector4(0, 0, 1, 1);

                    if (chunk.Water[x][ y][ z].IsFalling || !top)
                    {
                        averageWaterHeight = 1.0f;
                    }

                    if (face == BoxFace.Top)
                    {
                        Vector3 flow = chunk.Water[x][ y][ z].FluidFlow;
                        toReturn[i] = new ExtendedVertex(toReturn[i].Position + origin + new Vector3(0, (averageWaterHeight * 0.4f - 1.0f), 0),
                                                                    new Color(foaminess, puddleness, (float)totalDepth / 512.0f, 1.0f),
                                                                     UV, bounds);
                    }
                    else
                    {
                        if (face == BoxFace.Front || face == BoxFace.Back)
                        {
                            UV = new Vector2((Math.Abs(toReturn[i].Position.X + origin.X) / 80.0f), (Math.Abs(toReturn[i].Position.Y + origin.Y) / 80.0f));
                            foaminess = 1.0f;
                        }
                        else if (face == BoxFace.Left || face == BoxFace.Right)
                        {
                            UV = new Vector2((Math.Abs(toReturn[i].Position.Z + origin.Z) / 80.0f), (Math.Abs(toReturn[i].Position.Y + origin.Y) / 80.0f));
                            foaminess = 1.0f;
                        }

                        toReturn[i] = new ExtendedVertex(toReturn[i].Position + origin + new Vector3(0, (averageWaterHeight * 0.4f - 0.61f), 0),
                                                                     new Color(foaminess, 0.0f, 1.0f, 1.0f),
                                                                     UV, bounds);
                    }
                }
                
                return toReturn;
            }

        }
    
}

