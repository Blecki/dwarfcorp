using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a collection of voxels with a surface mesh. Efficiently culls away
    /// invisible voxels, and properly constructs ramps.
    /// </summary>
    public partial class VoxelListPrimitive : GeometricPrimitive
    {
        private static GlobalVoxelOffset[] NeighborVoxelCoordinateDeltas = null;

        // Describes which top face verts to grab for aligning fringe.
        private static int[,] FringeIndicies = new int[,]
        {
            // First four describe the vertex order
            // Last 2 are the indecies of the verts to use to extend in the fringe direction
            {3, 2, 1, 0, 2, 1}, // North
            {2, 1, 0, 3, 3, 2}, // East
            {1, 0, 3, 2, 0, 3 }, // South
            {0, 3, 2, 1, 1, 0 }, // West

            // First four describe vertex order
            {3,0,1,2,1,3 }, // North West
            {0,3,2,1,2,0 }, // North East
            {1,2,3,0,3,1 }, // South East
            {2,1,0,3,0,2 }, // South West
        };

        private static Vector2[] SideFringeUVScales = new Vector2[]
        {
            new Vector2(1.0f, 0.5f),
            new Vector2(0.5f, 1.0f),
            new Vector2(1.0f, 0.5f),
            new Vector2(0.5f, 1.0f)
        };

        private static Matrix DesignationTransform = Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix.CreateScale(1.1f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);

        protected void InitializeStatics()
        {
            if (NeighborVoxelCoordinateDeltas == null)
            {
                NeighborVoxelCoordinateDeltas = new GlobalVoxelOffset[6];
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Back] = new GlobalVoxelOffset(0, 0, 1);
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Front] = new GlobalVoxelOffset(0, 0, -1);
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Left] = new GlobalVoxelOffset(-1, 0, 0);
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Right] = new GlobalVoxelOffset(1, 0, 0);
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Top] = new GlobalVoxelOffset(0, 1, 0);
                NeighborVoxelCoordinateDeltas[(int)BoxFace.Bottom] = new GlobalVoxelOffset(0, -1, 0);
            }
        }

        public VoxelListPrimitive() :
            base()
        {
            InitializeStatics();
        }

        public static bool IsTopVertex(VoxelVertex v)
        {
            return v == VoxelVertex.BackTopLeft || v == VoxelVertex.FrontTopLeft || v == VoxelVertex.FrontTopRight || v == VoxelVertex.BackTopRight;
        }

        protected static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            switch (face)
            {
                case BoxFace.Top:
                case BoxFace.Bottom:
                    return true;
                case BoxFace.Front:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopBackRight,
                        neighborRamp, RampType.TopFrontLeft, RampType.TopFrontRight);
                case BoxFace.Back:
                    return CheckRamps(myRamp, RampType.TopFrontLeft, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopBackRight);
                case BoxFace.Left:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopFrontLeft,
                        neighborRamp, RampType.TopBackRight, RampType.TopFrontRight);
                case BoxFace.Right:
                    return CheckRamps(myRamp, RampType.TopBackRight, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopFrontLeft);
                default:
                    return false;
            }
        }

        private static bool RampSet(RampType ToCheck, RampType For)
        {
            return (ToCheck & For) != 0;
        }

        private static bool CheckRamps(RampType A, RampType A1, RampType A2, RampType B, RampType B1, RampType B2)
        {
            return (!RampSet(A, A1) && RampSet(B, B1)) || (!RampSet(A, A2) && RampSet(B, B2));
        }

        protected static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }

        protected static bool IsFaceVisible(VoxelHandle voxel, VoxelHandle neighbor, BoxFace face)
        {
            if (!voxel.IsValid)
                return false;

            if (!neighbor.IsValid)
            {
                if (voxel.IsExplored)
                    return !voxel.IsEmpty;
                else
                    return true;
            }
            else
            {
                if (!voxel.IsExplored)
                {
                    if (!neighbor.IsVisible)
                        return true;

                    if (neighbor.IsExplored && neighbor.IsEmpty) return true;

                    if (!neighbor.Type.CanRamp)
                        return false;

                    if (neighbor.RampType == RampType.None)
                        return false;

                    if (!IsSideFace(face))
                        return false;

                    if (!neighbor.IsExplored)
                        return false;

                    return true;
                }
                else
                {
                    if (neighbor.IsExplored && neighbor.IsEmpty)
                        return true;

                    if (face == BoxFace.Top && !neighbor.IsVisible)
                        return true;

                    if (neighbor.Type.IsTransparent && !voxel.Type.IsTransparent)
                        return true;

                    if (neighbor.Type.CanRamp
                        && neighbor.RampType != RampType.None
                        && IsSideFace(face)
                        && ShouldDrawFace(face, neighbor.RampType, voxel.RampType)
                        && neighbor.IsExplored)
                        return true;

                    return false;
                }
            }
        }

        private class Cache
        {
            public int[] AmbientValues = new int[4];
            public Dictionary<GlobalVoxelCoordinate, VertexColorInfo> LightCache = new Dictionary<GlobalVoxelCoordinate, VertexColorInfo>();
            public Dictionary<GlobalVoxelCoordinate, bool> ExploredCache = new Dictionary<GlobalVoxelCoordinate, bool>();

            public void Clear()
            {
                LightCache.Clear();
                ExploredCache.Clear();
            }
        }

        public void InitializeFromChunk(VoxelChunk chunk, WorldManager World)
        {
            DebugHelper.AssertNotNull(chunk);
            DebugHelper.AssertNotNull(World);

            var sliceStack = new List<RawPrimitive>();
            var cache = new Cache();
            int maxViewingLevel = World.Renderer.PersistentSettings.MaxViewingLevel;

            for (var localY = 0; localY < maxViewingLevel - chunk.Origin.Y && localY < VoxelConstants.ChunkSizeY; ++localY)
            {
                RawPrimitive sliceGeometry = null;

                lock (chunk.Data.SliceCache)
                {
                    var cachedSlice = chunk.Data.SliceCache[localY];

                    if (cachedSlice != null)
                    {
                        cache.Clear();

                        sliceStack.Add(cachedSlice);

                        if (GameSettings.Current.GrassMotes)
                            chunk.RebuildMoteLayerIfNull(localY);

                        continue;
                    }

                    sliceGeometry = new RawPrimitive
                    {
                        Vertices = null,
                        Indexes = null
                    };

                    chunk.Data.SliceCache[localY] = sliceGeometry;
                }

                if (GameSettings.Current.CalculateRamps)
                {
                    UpdateCornerRamps(World.ChunkManager, chunk, localY);
                    UpdateNeighborEdgeRamps(World.ChunkManager, chunk, localY);
                }

                if (GameSettings.Current.GrassMotes)
                    chunk.RebuildMoteLayer(localY);

                DebugHelper.AssertNotNull(sliceGeometry);
                BuildSliceGeometry(chunk, cache, localY, sliceGeometry, World.PersistentData.Designations, World);

                sliceStack.Add(sliceGeometry);
            }

            var combinedGeometry = RawPrimitive.Concat(sliceStack);

            Vertices = combinedGeometry.Vertices;
            VertexCount = combinedGeometry.VertexCount;
            Indexes = combinedGeometry.Indexes.Select(s => (ushort)s).ToArray();
            IndexCount = combinedGeometry.IndexCount;
        }

        private static void BuildSliceGeometry(
            VoxelChunk chunk, 
            //BoxPrimitive bedrockModel, 
            Cache Cache,
            int LocalY, 
            RawPrimitive sliceGeometry,
            DesignationSet DesignationSet,
            WorldManager World)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    BuildVoxelGeometry(sliceGeometry, x, LocalY, z, chunk, Cache, DesignationSet, World);
        }

        private static GlobalVoxelCoordinate GetCacheKey(VoxelHandle Handle, VoxelVertex Vertex)
        {
            var coord = Handle.Coordinate;

            if ((Vertex & VoxelVertex.Front) == VoxelVertex.Front)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y, coord.Z + 1);

            if ((Vertex & VoxelVertex.Top) == VoxelVertex.Top)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y + 1, coord.Z);

            if ((Vertex & VoxelVertex.Right) == VoxelVertex.Right)
                coord = new GlobalVoxelCoordinate(coord.X + 1, coord.Y, coord.Z);

            return coord;
        }

        public override void Render(GraphicsDevice device) // Todo: Move this somewhere else in file
        {
            device.RasterizerState = new RasterizerState()
            {
                CullMode = CullMode.CullCounterClockwiseFace
            };

            base.Render(device);

            device.RasterizerState = new RasterizerState()
            {
                CullMode = CullMode.None
            };
        }

        private static void BuildVoxelGeometry(
            RawPrimitive Into,
            int X,
            int Y,
            int Z,
            VoxelChunk Chunk,
            //BoxPrimitive BedrockModel,
            Cache Cache,
            DesignationSet Designations,
            WorldManager World)
        {
            var v = VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(X, Y, Z));
            if (!v.IsValid || !v.IsVisible) return; // How did this even get called then??
            BuildDesignationGeometry(Into, Chunk, Cache, Designations, World, v);

            if ((v.IsExplored && v.IsEmpty)) return;
            if (!v.IsExplored && v.Sunlight)
                return;

            if (v.IsEmpty)
            {
                var x = 5;
            }

            if (Library.GetVoxelPrimitive(v.Type).HasValue(out BoxPrimitive primitive))
                BuildVoxelGeometryFromPrimitive(Into, Chunk, Cache, v, primitive);
            
        }

        private static void BuildVoxelGeometryFromPrimitive(RawPrimitive Into, VoxelChunk Chunk, Cache Cache, VoxelHandle v, BoxPrimitive primitive)
        {
            var tint = v.Type.Tint;
            var uvs = primitive.UVs;

            if (v.Type.HasTransitionTextures && v.IsExplored)
                uvs = ComputeTransitionTexture(new VoxelHandle(v.Chunk.Manager, v.Coordinate));

            BuildVoxelTopFaceGeometry(Into, Chunk, Cache, primitive, v, uvs);

            for (int i = 1; i < 6; i++)
                BuildVoxelFaceGeometry(Into, Chunk, Cache, primitive, v, tint, uvs, Matrix.Identity, (BoxFace)i, true);
        }

        private static void BuildDesignationGeometry(RawPrimitive Into, VoxelChunk Chunk, Cache Cache, DesignationSet Designations, WorldManager World, VoxelHandle v)
        {
            // Todo: Store designations per chunk.
            foreach (var designation in Designations == null ? new List<DesignationSet.VoxelDesignation>() : Designations.EnumerateDesignations(v).ToList())
            {
                if ((designation.Type & World.Renderer.PersistentSettings.VisibleTypes) != designation.Type) // If hidden by player, do not draw.
                    return;

                var designationProperties = Library.GetDesignationTypeProperties(designation.Type).Value;
                var designationVisible = false;

                if (designation.Type == DesignationType.Put)
                    designationVisible = v.Coordinate.Y < World.Renderer.PersistentSettings.MaxViewingLevel;
                else
                    designationVisible = VoxelHelpers.DoesVoxelHaveVisibleSurface(World, v);

                if (designationVisible
                    && Library.GetVoxelPrimitive(Library.DesignationVoxelType).HasValue(out BoxPrimitive designationPrimitive))
                {
                    switch (designationProperties.DrawType)
                    {
                        case DesignationDrawType.FullBox:
                            for (int i = 0; i < 6; i++)
                                BuildVoxelFaceGeometry(Into, Chunk, Cache, designationPrimitive, v, designationProperties.Color, designationPrimitive.UVs, DesignationTransform, (BoxFace)i, false);
                            break;

                        case DesignationDrawType.TopBox:
                            BuildVoxelFaceGeometry(Into, Chunk, Cache, designationPrimitive, v, designationProperties.Color, designationPrimitive.UVs, DesignationTransform, 0, false);
                            break;

                        case DesignationDrawType.PreviewVoxel:
                            {
                                if (Library.GetVoxelType(designation.Tag.ToString()).HasValue(out VoxelType voxelType)
                                    && Library.GetVoxelPrimitive(voxelType).HasValue(out BoxPrimitive previewPrimitive))
                                {
                                    var offsetMatrix = Matrix.Identity;
                                    if (!v.IsEmpty)
                                        offsetMatrix = Matrix.CreateTranslation(0.0f, 0.1f, 0.0f);
                                    for (int i = 0; i < 6; i++)
                                        BuildVoxelFaceGeometry(Into, Chunk, Cache, previewPrimitive, v, designationProperties.Color, previewPrimitive.UVs, offsetMatrix, (BoxFace)i, false);
                                }
                            }
                            break;
                    }

                }
            }
        }

        private static bool IsFaceVisible(VoxelHandle V, BoxFace Face, ChunkManager Chunks, out VoxelHandle Neighbor)
        {
            var delta = NeighborVoxelCoordinateDeltas[(int)Face];
            Neighbor = new VoxelHandle(Chunks, V.Coordinate + delta);
            return IsFaceVisible(V, Neighbor, Face);
        }

        private static void BuildVoxelFaceGeometry(
            RawPrimitive Into,
            VoxelChunk Chunk,
            Cache Cache,
            BoxPrimitive Primitive,
            VoxelHandle V,
            Color Tint,
            BoxPrimitive.BoxTextureCoords UVs,
            Matrix VertexTransform,
            BoxFace BoxFace,
            bool ApplyLighting)
        {
            if (!IsFaceVisible(V, BoxFace, Chunk.Manager, out var neighbor))
                return;

            var faceDescriptor = Primitive.GetFace(BoxFace);
            var indexOffset = Into.VertexCount;

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
            {
                var vertex = Primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];
                var voxelVertex = Primitive.VertexClassifications[faceDescriptor.VertexOffset + faceVertex];
                var vertexColor = new VertexColorInfo
                {
                    SunColor = 255,
                    AmbientColor = 255,
                    DynamicColor = 255,
                };

                if (ApplyLighting)
                {
                    var cacheKey = GetCacheKey(V, voxelVertex);

                    if (!Cache.LightCache.TryGetValue(cacheKey, out vertexColor))
                    {
                        vertexColor = CalculateVertexLight(V, voxelVertex, Chunk.Manager);
                        Cache.LightCache.Add(cacheKey, vertexColor);
                    }

                    Cache.AmbientValues[faceVertex] = vertexColor.AmbientColor;

                    if (!V.IsExplored && !neighbor.IsValid) // Turns the outside of the world black when it's not explored.
                        Tint = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                }

                var rampOffset = Vector3.Zero;
                if (V.IsExplored && V.Type.CanRamp && ShouldRamp(voxelVertex, V.RampType))
                    rampOffset = new Vector3(0, -0.5f, 0);

                var baseWorldPosition = V.WorldPosition + vertex.Position + rampOffset;
                var noise = VertexNoise.GetNoiseVectorFromRepeatingTexture(baseWorldPosition);
                var localPosition = Vector3.Transform(vertex.Position + rampOffset + noise, VertexTransform);

                Into.AddVertex(new ExtendedVertex(
                    V.WorldPosition + localPosition,
                    vertexColor.AsColor(),
                    Tint,
                    UVs.Uvs[faceDescriptor.VertexOffset + faceVertex],
                    UVs.Bounds[faceDescriptor.IndexOffset / 6]));
            }

            // Sometimes flip the quad to smooth out lighting.
            bool flippedQuad = ApplyLighting && (Cache.AmbientValues[0] + Cache.AmbientValues[2] >
                              Cache.AmbientValues[1] + Cache.AmbientValues[3]);

            for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount +
                faceDescriptor.IndexOffset; idx++)
            {
                ushort offset = flippedQuad ? Primitive.FlippedIndexes[idx] : Primitive.Indexes[idx];
                ushort offset0 = flippedQuad ? Primitive.FlippedIndexes[faceDescriptor.IndexOffset] : Primitive.Indexes[faceDescriptor.IndexOffset];
                Into.AddIndex((short)(indexOffset + offset - offset0));
            }
        }

        private static void BuildVoxelTopFaceGeometry(
            RawPrimitive Into,
            VoxelChunk Chunk,
            Cache Cache,
            BoxPrimitive Primitive,
            VoxelHandle V,
            BoxPrimitive.BoxTextureCoords UVs)
        {
            if (!IsFaceVisible(V, BoxFace.Top, Chunk.Manager, out var _))
                return;

            var faceDescriptor = Primitive.GetFace(BoxFace.Top);
            int exploredVerts = 0;
            var vertexColors = new VertexColorInfo[4];
            var vertexTint = new Color[4];

            // Find all verticies to use for geometry later, and for the fringe
            var vertexPositions = new Vector3[4];

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
            {
                var vertex = Primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];
                var voxelVertex = Primitive.VertexClassifications[faceDescriptor.VertexOffset + faceVertex];

                var rampOffset = Vector3.Zero;
                if (V.IsExplored && V.Type.CanRamp && ShouldRamp(voxelVertex, V.RampType))
                    rampOffset = new Vector3(0, -0.5f, 0);

                var worldPosition = V.WorldPosition + vertex.Position + rampOffset;
                //worldPosition += VertexNoise.GetNoiseVectorFromRepeatingTexture(worldPosition);

                vertexPositions[faceVertex] = worldPosition;
            }

            // Figure out if this vertex is adjacent to any explored voxel.
            if (V.IsExplored)
                exploredVerts = 4;
            else
            {
                for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; ++faceVertex)
                {
                    var voxelVertex = Primitive.VertexClassifications[faceDescriptor.VertexOffset + faceVertex];
                    var cacheKey = GetCacheKey(V, voxelVertex);
                    var anyNeighborExplored = true;

                    if (!Cache.ExploredCache.TryGetValue(cacheKey, out anyNeighborExplored))
                    {
                        anyNeighborExplored = VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, voxelVertex)
                            .Select(c => new VoxelHandle(V.Chunk.Manager, c))
                            .Any(n => n.IsValid && n.IsExplored);
                        Cache.ExploredCache.Add(cacheKey, anyNeighborExplored);
                    }

                    if (anyNeighborExplored)
                        exploredVerts += 1;
                }

            }

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; ++faceVertex)
            {
                var voxelVertex = Primitive.VertexClassifications[faceDescriptor.VertexOffset + faceVertex];
                var cacheKey = GetCacheKey(V, voxelVertex);

                VertexColorInfo vertexColor;
                if (!Cache.LightCache.TryGetValue(cacheKey, out vertexColor))
                {
                    vertexColor = CalculateVertexLight(V, voxelVertex, Chunk.Manager);
                    Cache.LightCache.Add(cacheKey, vertexColor);
                }

                vertexColors[faceVertex] = vertexColor;

                vertexTint[faceVertex] = new Color(1.0f, 1.0f, 1.0f, 1.0f);

                // Turn face solid black if there are no explored neighbors - this is an optimization; it means we do not need to apply a solid black decal.
                if (exploredVerts != 4)
                {
                    var anyNeighborExplored = true;
                    if (!Cache.ExploredCache.TryGetValue(cacheKey, out anyNeighborExplored))
                        throw new InvalidProgramException("Failed cache lookup");

                    if (!anyNeighborExplored) vertexTint[faceVertex] = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                }
                
                vertexTint[faceVertex] = new Color(vertexTint[faceVertex].ToVector4() * V.Type.Tint.ToVector4());
            }

            if (exploredVerts != 0)
            {
                var baseUVs = UVs.Uvs[11]; // EW

                var baseUVBounds = new Vector4(baseUVs.X + 0.001f, baseUVs.Y + 0.001f, baseUVs.X + (1.0f / 16.0f) - 0.001f, baseUVs.Y + (1.0f / 16.0f) - 0.001f);

                // Draw the base voxel
                AddTopFaceGeometry(Into,
                    Cache.AmbientValues, Primitive,
                    faceDescriptor,
                    vertexPositions,
                    vertexColors,
                    vertexTint,
                    Vector2.One,
                    baseUVs, baseUVBounds);

                if (V.GrassType != 0)
                    BuildGrassFringeGeometry(Into, Chunk, Cache, Primitive, V, vertexColors, vertexTint, vertexPositions, faceDescriptor);

                if (V.DecalType != 0)
                    BuildDecalGeometry(Into, Chunk, Cache, Primitive, V, vertexColors, vertexTint, vertexPositions, faceDescriptor, exploredVerts);
            }
            else
            {
                // Apparently being unexplored hides all grass and decals. Is that the behavior we actually want?

                if (!Debugger.Switches.HideSliceTop)
                {
                    var indexOffset = Into.VertexCount;

                    for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
                    {
                        Into.AddVertex(new ExtendedVertex(
                            vertexPositions[faceVertex] + VertexNoise.GetNoiseVectorFromRepeatingTexture(vertexPositions[faceVertex]),
                            new Color(0, 0, 0, 255),
                            new Color(0, 0, 0, 255),
                            new Vector2(12.5f / 16.0f, 0.5f / 16.0f),
                            new Vector4(12.0f / 16.0f, 0.0f, 13.0f / 16.0f, 1.0f / 16.0f)));
                    }

                    for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount +
                        faceDescriptor.IndexOffset; idx++)
                    {
                        ushort offset = Primitive.Indexes[idx];
                        ushort offset0 = Primitive.Indexes[faceDescriptor.IndexOffset];
                        Into.AddIndex((short)(indexOffset + offset - offset0));
                    }
                }
            }
        }

        private static void BuildDecalGeometry(
            RawPrimitive Into,
            VoxelChunk Chunk,
            Cache Cache,
            BoxPrimitive Primitive,
            VoxelHandle V,
            VertexColorInfo[] VertexColors,
            Color[] VertexTint,
            Vector3[] VertexPositions,
            BoxPrimitive.FaceDescriptor Face,
            int ExploredVerts)
        {
            if (V.DecalType == 0) return;

            var decalType = Library.GetDecalType(V.DecalType);

            AddDecalGeometry(Into, Cache.AmbientValues, Primitive, Face, ExploredVerts, VertexPositions, VertexColors, VertexTint, decalType);
        }

        private static void AddDecalGeometry(
            RawPrimitive Into,
            int[] AmbientScratchSpace,
            BoxPrimitive Primitive,
            BoxPrimitive.FaceDescriptor faceDescriptor,
            int exploredVerts,
            Vector3[] VertexPositions,
            VertexColorInfo[] VertexColors,
            Color[] VertexTints,
            DecalType Decal)
        {
            var indexOffset = Into.VertexCount;
            var UV = new Vector2(Decal.Tile.X * (1.0f / 16.0f), Decal.Tile.Y * (1.0f / 16.0f));
            var UVBounds = new Vector4(UV.X + 0.001f, UV.Y + 0.001f, UV.X + (1.0f / 16.0f) - 0.001f, UV.Y + (1.0f / 16.0f) - 0.001f);

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
            {
                var vertex = Primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];

                AmbientScratchSpace[faceVertex] = VertexColors[faceVertex].AmbientColor;

                Into.AddVertex(new ExtendedVertex(
                    VertexPositions[faceVertex] + VertexNoise.GetNoiseVectorFromRepeatingTexture(VertexPositions[faceVertex]),
                    VertexColors[faceVertex].AsColor(),
                    VertexTints[faceVertex],
                    UV + new Vector2(vertex.Position.X / 16.0f, vertex.Position.Z / 16.0f),
                    UVBounds));
            }

            bool flippedQuad = AmbientScratchSpace[0] + AmbientScratchSpace[2] > AmbientScratchSpace[1] + AmbientScratchSpace[3];

            for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount +
                faceDescriptor.IndexOffset; idx++)
            {
                ushort offset = flippedQuad ? Primitive.FlippedIndexes[idx] : Primitive.Indexes[idx];
                ushort offset0 = flippedQuad ? Primitive.FlippedIndexes[faceDescriptor.IndexOffset] : Primitive.Indexes[faceDescriptor.IndexOffset];
                Into.AddIndex((short)(indexOffset + offset - offset0));
            }
        }

        private static void BuildGrassFringeGeometry(
            RawPrimitive Into,
            VoxelChunk Chunk,
            Cache Cache,
            BoxPrimitive Primitive,
            VoxelHandle V,
            VertexColorInfo[] VertexColors,
            Color[] VertexTint,
            Vector3[] VertexPositions,
            BoxPrimitive.FaceDescriptor Face)
        {
            if (V.GrassType == 0) return;

            var decalType = Library.GetGrassType(V.GrassType);

            AddGrassGeometry(Into, Cache.AmbientValues, Primitive, Face, VertexPositions, VertexColors, VertexTint, decalType);

            // Draw fringe
            if (decalType.FringeTransitionUVs == null) return;

            for (var s = 0; s < 4; ++s)
            {
                var neighborCoord = V.Coordinate + VoxelHelpers.ManhattanNeighbors2D[s];
                var neighbor = new VoxelHandle(Chunk.Manager, neighborCoord);

                if (!neighbor.IsValid) continue;

                var aboveNeighbor = new VoxelHandle(Chunk.Manager, neighborCoord + new GlobalVoxelOffset(0, 1, 0));

                if (!aboveNeighbor.IsValid || aboveNeighbor.IsEmpty)
                {
                    // Draw horizontal fringe.
                    if (!neighbor.IsEmpty)
                    {
                        if (neighbor.GrassType != 0 &&
                            Library.GetGrassType(neighbor.GrassType).FringePrecedence >= decalType.FringePrecedence)
                            continue;
                    }

                    // Twizzle vertex positions.
                    var newPositions = new Vector3[4];
                    newPositions[FringeIndicies[s, 0]] = VertexPositions[FringeIndicies[s, 4]];
                    newPositions[FringeIndicies[s, 1]] = VertexPositions[FringeIndicies[s, 4]]
                        + (VoxelHelpers.ManhattanNeighbors2D[s].AsVector3() * 0.5f);
                    newPositions[FringeIndicies[s, 2]] = VertexPositions[FringeIndicies[s, 5]]
                        + (VoxelHelpers.ManhattanNeighbors2D[s].AsVector3() * 0.5f);
                    newPositions[FringeIndicies[s, 3]] = VertexPositions[FringeIndicies[s, 5]];

                    var newColors = new VertexColorInfo[4];
                    newColors[FringeIndicies[s, 0]] = VertexColors[FringeIndicies[s, 4]];
                    newColors[FringeIndicies[s, 1]] = VertexColors[FringeIndicies[s, 4]];
                    newColors[FringeIndicies[s, 2]] = VertexColors[FringeIndicies[s, 5]];
                    newColors[FringeIndicies[s, 3]] = VertexColors[FringeIndicies[s, 5]];

                    var slopeTweak = new Vector3(0.0f, 0.0f, 0.0f);
                    if (neighbor.IsEmpty)
                        slopeTweak.Y = -0.5f;
                    else
                        slopeTweak.Y = 0.125f;

                    newPositions[FringeIndicies[s, 1]] += slopeTweak;
                    newPositions[FringeIndicies[s, 2]] += slopeTweak;

                    var newTints = new Color[4];
                    newTints[FringeIndicies[s, 0]] = VertexTint[FringeIndicies[s, 4]];
                    newTints[FringeIndicies[s, 1]] = VertexTint[FringeIndicies[s, 4]];
                    newTints[FringeIndicies[s, 2]] = VertexTint[FringeIndicies[s, 5]];
                    newTints[FringeIndicies[s, 3]] = VertexTint[FringeIndicies[s, 5]];

                    AddTopFaceGeometry(Into,
                        Cache.AmbientValues, Primitive,
                        Face,
                        newPositions,
                        newColors,
                        newTints,
                        SideFringeUVScales[s],
                        decalType.FringeTransitionUVs[s].UV,
                        decalType.FringeTransitionUVs[s].Bounds);
                }
                else
                {
                    // Draw vertical fringe!

                    var newPositions = new Vector3[4];
                    newPositions[FringeIndicies[s, 0]] = VertexPositions[FringeIndicies[s, 4]];
                    newPositions[FringeIndicies[s, 1]] = VertexPositions[FringeIndicies[s, 4]]
                        + new Vector3(0.0f, 0.5f, 0.0f)
                        + (VoxelHelpers.ManhattanNeighbors2D[s].AsVector3() * -0.05f);
                    newPositions[FringeIndicies[s, 2]] = VertexPositions[FringeIndicies[s, 5]]
                        + new Vector3(0.0f, 0.5f, 0.0f)
                        + (VoxelHelpers.ManhattanNeighbors2D[s].AsVector3() * -0.05f);
                    newPositions[FringeIndicies[s, 3]] = VertexPositions[FringeIndicies[s, 5]];

                    var newColors = new VertexColorInfo[4];
                    newColors[FringeIndicies[s, 0]] = VertexColors[FringeIndicies[s, 4]];
                    newColors[FringeIndicies[s, 1]] = VertexColors[FringeIndicies[s, 4]];
                    newColors[FringeIndicies[s, 2]] = VertexColors[FringeIndicies[s, 5]];
                    newColors[FringeIndicies[s, 3]] = VertexColors[FringeIndicies[s, 5]];

                    var newTints = new Color[4];
                    newTints[FringeIndicies[s, 0]] = VertexTint[FringeIndicies[s, 4]];
                    newTints[FringeIndicies[s, 1]] = VertexTint[FringeIndicies[s, 4]];
                    newTints[FringeIndicies[s, 2]] = VertexTint[FringeIndicies[s, 5]];
                    newTints[FringeIndicies[s, 3]] = VertexTint[FringeIndicies[s, 5]];

                    AddTopFaceGeometry(Into,
                        Cache.AmbientValues, Primitive,
                        Face,
                        newPositions,
                        newColors,
                        newTints,
                        SideFringeUVScales[s],
                        decalType.FringeTransitionUVs[s].UV,
                        decalType.FringeTransitionUVs[s].Bounds);
                }
            }

            for (var s = 0; s < 4; ++s)
            {
                var neighborCoord = V.Coordinate + VoxelHelpers.DiagonalNeighbors2D[s];
                var handle = new VoxelHandle(Chunk.Manager, neighborCoord);

                if (handle.IsValid)
                {
                    if (!handle.IsEmpty)
                    {
                        if (handle.GrassType != 0 &&
                            Library.GetGrassType(handle.GrassType).FringePrecedence >= decalType.FringePrecedence)
                            continue;
                    }

                    var manhattanA = new VoxelHandle(Chunk.Manager, V.Coordinate + VoxelHelpers.ManhattanNeighbors2D[s]);
                    if (!manhattanA.IsValid || (manhattanA.GrassType == V.GrassType))
                        continue;

                    manhattanA = new VoxelHandle(Chunk.Manager, V.Coordinate + VoxelHelpers.ManhattanNeighbors2D[FringeIndicies[4 + s, 5]]);
                    if (!manhattanA.IsValid || (manhattanA.GrassType == V.GrassType))
                        continue;

                    // Twizzle vertex positions.
                    var newPositions = new Vector3[4];
                    var pivot = VertexPositions[FringeIndicies[4 + s, 4]];
                    var nDelta = VoxelHelpers.DiagonalNeighbors2D[s].AsVector3();

                    newPositions[FringeIndicies[4 + s, 0]] = pivot;
                    newPositions[FringeIndicies[4 + s, 1]] = pivot + new Vector3(nDelta.X * 0.5f, 0, 0);
                    newPositions[FringeIndicies[4 + s, 2]] = pivot + new Vector3(nDelta.X * 0.5f, 0, nDelta.Z * 0.5f);
                    newPositions[FringeIndicies[4 + s, 3]] = pivot + new Vector3(0, 0, nDelta.Z * 0.5f);

                    var slopeTweak = new Vector3(0.0f, 0.0f, 0.0f);
                    if (handle.IsEmpty)
                        slopeTweak.Y = -0.5f;
                    else
                        slopeTweak.Y = 0.125f;

                    newPositions[FringeIndicies[4 + s, 1]] += slopeTweak;
                    newPositions[FringeIndicies[4 + s, 2]] += slopeTweak;
                    newPositions[FringeIndicies[4 + s, 3]] += slopeTweak;

                    var newColors = new VertexColorInfo[4];
                    newColors[FringeIndicies[4 + s, 0]] = VertexColors[FringeIndicies[4 + s, 4]];
                    newColors[FringeIndicies[4 + s, 1]] = VertexColors[FringeIndicies[4 + s, 4]];
                    newColors[FringeIndicies[4 + s, 2]] = VertexColors[FringeIndicies[4 + s, 4]];
                    newColors[FringeIndicies[4 + s, 3]] = VertexColors[FringeIndicies[4 + s, 4]];

                    var newTints = new Color[4];
                    newTints[FringeIndicies[4 + s, 0]] = VertexTint[FringeIndicies[4 + s, 4]];
                    newTints[FringeIndicies[4 + s, 1]] = VertexTint[FringeIndicies[4 + s, 4]];
                    newTints[FringeIndicies[4 + s, 2]] = VertexTint[FringeIndicies[4 + s, 4]];
                    newTints[FringeIndicies[4 + s, 3]] = VertexTint[FringeIndicies[4 + s, 4]];

                    AddTopFaceGeometry(Into,
                        Cache.AmbientValues, Primitive,
                        Face,
                        newPositions,
                        newColors,
                        newTints,
                        new Vector2(0.5f, 0.5f),
                        decalType.FringeTransitionUVs[4 + s].UV,
                        decalType.FringeTransitionUVs[4 + s].Bounds);
                }
            }
        }

        private static void AddTopFaceGeometry(
            RawPrimitive Into,
            int[] AmbientScratchSpace,
            BoxPrimitive Primitive,
            BoxPrimitive.FaceDescriptor faceDescriptor,
            Vector3[] VertexPositions,
            VertexColorInfo[] VertexColors,
            Color[] VertexTints,
            Vector2 UVScale,
            Vector2 UV,
            Vector4 UVBounds)
        {
            var indexOffset = Into.VertexCount;

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
            {
                var vertex = Primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];
               
                AmbientScratchSpace[faceVertex] = VertexColors[faceVertex].AmbientColor;

                Into.AddVertex(new ExtendedVertex(
                    VertexPositions[faceVertex] + VertexNoise.GetNoiseVectorFromRepeatingTexture(VertexPositions[faceVertex]),
                    VertexColors[faceVertex].AsColor(),
                    VertexTints[faceVertex],
                    UV + new Vector2(vertex.Position.X / 16.0f * UVScale.X, vertex.Position.Z / 16.0f * UVScale.Y),
                    UVBounds));
            }

            bool flippedQuad = AmbientScratchSpace[0] + AmbientScratchSpace[2] > AmbientScratchSpace[1] + AmbientScratchSpace[3];

            for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount +
                faceDescriptor.IndexOffset; idx++)
            {
                ushort offset = flippedQuad ? Primitive.FlippedIndexes[idx] : Primitive.Indexes[idx];
                ushort offset0 = flippedQuad ? Primitive.FlippedIndexes[faceDescriptor.IndexOffset] : Primitive.Indexes[faceDescriptor.IndexOffset];
                Into.AddIndex((short)(indexOffset + offset - offset0));
            }
        }

        private static void AddGrassGeometry(
            RawPrimitive Into,
            int[] AmbientScratchSpace,
            BoxPrimitive Primitive,
            BoxPrimitive.FaceDescriptor faceDescriptor,
            Vector3[] VertexPositions,
            VertexColorInfo[] VertexColors,
            Color[] VertexTints,
            GrassType Decal)
        {
            var indexOffset = Into.VertexCount;
            var UV = new Vector2(Decal.Tile.X * (1.0f / 16.0f), Decal.Tile.Y * (1.0f / 16.0f));
            var UVBounds = new Vector4(UV.X + 0.001f, UV.Y + 0.001f, UV.X + (1.0f / 16.0f) - 0.001f, UV.Y + (1.0f / 16.0f) - 0.001f);

            for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
            {
                var vertex = Primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];

                AmbientScratchSpace[faceVertex] = VertexColors[faceVertex].AmbientColor;

                Into.AddVertex(new ExtendedVertex(
                    VertexPositions[faceVertex] + VertexNoise.GetNoiseVectorFromRepeatingTexture(VertexPositions[faceVertex]),
                    VertexColors[faceVertex].AsColor(),
                    VertexTints[faceVertex],
                    UV + new Vector2(vertex.Position.X / 16.0f, vertex.Position.Z / 16.0f),
                    UVBounds));
            }

            bool flippedQuad = (AmbientScratchSpace[0] + AmbientScratchSpace[2]) > (AmbientScratchSpace[1] + AmbientScratchSpace[3]);

            for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount + faceDescriptor.IndexOffset; ++idx)
            {
                ushort offset = flippedQuad ? Primitive.FlippedIndexes[idx] : Primitive.Indexes[idx];
                ushort offset0 = flippedQuad ? Primitive.FlippedIndexes[faceDescriptor.IndexOffset] : Primitive.Indexes[faceDescriptor.IndexOffset];
                Into.AddIndex((short)(indexOffset + offset - offset0));
            }
        }
    }
}