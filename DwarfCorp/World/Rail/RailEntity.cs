using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public class RailEntity : CraftedBody, ITintable
    {
        public class NeighborConnection
        {
            public uint NeighborID;
            public Vector3 Position;
            public bool Raised;
        }

        [JsonProperty]
        private JunctionPiece Piece;

        [JsonProperty]
        private VoxelHandle Location;

        [JsonIgnore]
        public List<NeighborConnection> NeighborRails = new List<NeighborConnection>();
        
        private VoxelHandle ContainingVoxel {  get { return GetContainingVoxel(); } }

        private const float sqrt2 = 1.41421356237f;
        private SpriteSheet Sheet;
        private Point Frame;
        private RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;
        private string previousEffect = null;

        public void SetVertexColor(Color Tint)
        {
            this.VertexColor = Tint;
        }

        public void SetLightRamp(Color Tint)
        {
            this.LightRamp = Tint;
        }

        public void SetOneShotTint(Color Tint)
        { }

        private static float[,] VertexHeightOffsets =
        {
            { 0.0f, 0.0f, 0.0f, 0.0f },
            { 1.0f, 0.0f, 0.0f, 1.0f },
            { 0.0f, 1.0f, 1.0f, 0.0f },
            { 1.0f, 1.0f, 1.0f, 1.0f }
        };

        public VoxelHandle GetLocation()
        {
            return Location;
        }

        public VoxelHandle GetContainingVoxel()
        {
            return Location.Chunk.Manager.CreateVoxelHandle(Location.Coordinate + (Piece == null ? new GlobalVoxelOffset(0, 0, 0) : new GlobalVoxelOffset(Piece.Offset.X, 0, Piece.Offset.Y)));
        }

        public JunctionPiece GetPiece()
        {
            return Piece;
        }

        public void ResetPrimitive()
        {
            Primitive = null;
        }

        private float AngleBetweenVectors(Vector2 A, Vector2 B)
        {
            A.Normalize();
            B.Normalize();
            float DotProduct = Vector2.Dot(A, B);
            DotProduct = MathHelper.Clamp(DotProduct, -1.0f, 1.0f);
            float Angle = (float)global::System.Math.Acos(DotProduct);
            if (CrossZ(A, B) < 0) return -Angle;
            return Angle;
        }

        private float CrossZ(Vector2 A, Vector2 B)
        {
            return (B.Y * A.X) - (B.X * A.Y);
        }

        private float Sign(float F)
        {
            if (F < 0) return -1.0f;
            return 1.0f;
        }

        public RailEntity()
        {
            CollisionType = CollisionType.Static;
        }

        public RailEntity(
            ComponentManager Manager,
            VoxelHandle Location,
            JunctionPiece Piece) :

            base(Manager, "Rail", 
                Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y)), 
                Vector3.One,
                Vector3.Zero,
                new CraftDetails(Manager, new Resource("Rail")))
        {
            this.Piece = Piece;
            this.Location = Location;

            CollisionType = CollisionType.Static;
            AddChild(new Health(Manager, "Hp", 100, 0, 100));
            
            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            var piece = Library.GetRailPiece(Piece.RailPiece);
            Sheet = new SpriteSheet(ContentPaths.rail_tiles, 32, 32);
            
            AddChild(new GenericVoxelListener(manager, Matrix.Identity, new Vector3(0.8f, 1.5f, 0.8f), Vector3.Zero, (_event) =>
            {
                if (!Active) return;

                if (_event.Type == VoxelChangeEventType.VoxelTypeChanged && _event.NewVoxelType == 0)
                {
                    Die();
                    var designation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.PlaceObject).FirstOrDefault(d => Object.ReferenceEquals(d.Body, this));
                    if (designation != null)
                    {
                        World.PersistentData.Designations.RemoveEntityDesignation(this, DesignationType.PlaceObject);
                        var craftDesignation = designation.Tag as PlacementDesignation;
                        if (craftDesignation.WorkPile != null)
                            craftDesignation.WorkPile.Die();
                    }
                }
            })).SetFlag(Flag.ShouldSerialize, false);

            UpdatePiece(Piece, Location);
        }

        public Vector3 InterpolateSpline(float t, Vector3 origin, Vector3 destination)
        {
            if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var piece))
            {
                List<Vector3> selectedSpline = null;
                bool isReversed = false;
                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
                double closestEndpoint = double.MaxValue;

                if (piece.AutoSlope)
                {
                    var transformedConnections = GetTransformedConnections();
                    var matchingNeighbor1 = NeighborRails.FirstOrDefault(n => (n.Position - transformedConnections[0].Item1 - new Vector3(0.0f, 1.0f, 0.0f)).LengthSquared() < 0.001f);
                    var matchingNeighbor2 = NeighborRails.FirstOrDefault(n => (n.Position - transformedConnections[1].Item1 - new Vector3(0.0f, 1.0f, 0.0f)).LengthSquared() < 0.001f);

                    selectedSpline = new List<Vector3>();
                    if (matchingNeighbor1 != null && matchingNeighbor1.Raised)
                        selectedSpline.Add(piece.SplinePoints[0][0] + Vector3.UnitY);
                    else
                        selectedSpline.Add(piece.SplinePoints[0][0]);

                    if (matchingNeighbor2 != null && matchingNeighbor2.Raised)
                        selectedSpline.Add(piece.SplinePoints[0][1] + Vector3.UnitY);
                    else
                        selectedSpline.Add(piece.SplinePoints[0][1]);

                    var distStart = (Vector3.Transform(selectedSpline.First(), transform) - destination).LengthSquared();
                    var distEnd = (Vector3.Transform(selectedSpline.Last(), transform) - destination).LengthSquared();
                    if (distEnd > distStart)
                        isReversed = true;
                }
                else
                {
                    foreach (var spline in piece.SplinePoints)
                    {
                        var startPoint = Vector3.Transform(spline.First(), transform);
                        var distStart = (startPoint - destination).LengthSquared();
                        if (distStart < closestEndpoint)
                        {
                            isReversed = true;
                            selectedSpline = spline;
                            closestEndpoint = distStart;
                        }

                        var endPoint = Vector3.Transform(spline.Last(), transform);
                        var distEnd = (endPoint - destination).LengthSquared();
                        if (distEnd < closestEndpoint)
                        {
                            isReversed = false;
                            selectedSpline = spline;
                            closestEndpoint = distEnd;
                        }
                    }
                }

                if (selectedSpline == null)
                {
                    return origin + t * (destination - origin);
                }

                if (isReversed)
                {
                    t = 1.0f - t;
                }

                float idx = (selectedSpline.Count - 1) * t;
                int k = MathFunctions.Clamp((int)idx, 0, selectedSpline.Count - 1);
                float remainder = idx - k;
                //Drawer3D.DrawLine(Vector3.Transform(selectedSpline[k], transform), Vector3.Transform(selectedSpline[k + 1], transform), isReversed ? Color.Red : Color.Blue, 0.1f);
                var next = ((int)k + 1) >= selectedSpline.Count ? (int)k : ((int)k + 1);
                return Vector3.Transform(selectedSpline[k] * (1.0f - remainder) + selectedSpline[next] * remainder, transform);

            }
            else
                return this.Position;
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

            if (Debugger.Switches.DrawRailNetwork)
            {
                //Drawer3D.DrawBox(GetContainingVoxel().GetBoundingBox(), Color.White, 0.01f, true);
                //Drawer3D.DrawLine(GetContainingVoxel().GetBoundingBox().Center(), GlobalTransform.Translation, Color.White, 0.01f);
                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
                if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var piece))
                {

                    foreach (var spline in piece.SplinePoints)
                        for (var i = 1; i < spline.Count; ++i)
                            Drawer3D.DrawLine(Vector3.Transform(spline[i - 1], transform),
                                Vector3.Transform(spline[i], transform), Color.Purple, 0.1f);

                    foreach (var connection in piece.EnumerateConnections())
                        Drawer3D.DrawLine(Vector3.Transform(connection.Item1, transform) + new Vector3(0.0f, 0.2f, 0.0f),
                            Vector3.Transform(connection.Item2, transform) + new Vector3(0.0f, 0.2f, 0.0f),
                            Color.Brown, 0.1f);


                    //foreach (var neighborConnection in NeighborRails)
                    //{
                    //    var neighbor = Manager.FindComponent(neighborConnection.NeighborID);
                    //    if (neighbor == null)
                    //        Drawer3D.DrawLine(Position, Position + Vector3.UnitY, Color.CornflowerBlue, 0.1f);
                    //    else
                    //        Drawer3D.DrawLine(Position + new Vector3(0.0f, 0.5f, 0.0f), (neighbor as Body).Position + new Vector3(0.0f, 0.5f, 0.0f), Color.Teal, 0.1f);
                    //}
                }

                
            }

            if (!IsVisible)
                return;

            if (Primitive == null)
            {
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(Frame, out bounds);

                if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var rawPiece))
                {
                    var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation);
                    var realShape = 0;

                    if (rawPiece.AutoSlope)
                    {
                        var transformedConnections = GetTransformedConnections();
                        var matchingNeighbor1 = NeighborRails.FirstOrDefault(n => (n.Position - transformedConnections[0].Item1 - new Vector3(0.0f, 1.0f, 0.0f)).LengthSquared() < 0.001f);
                        var matchingNeighbor2 = NeighborRails.FirstOrDefault(n => (n.Position - transformedConnections[1].Item1 - new Vector3(0.0f, 1.0f, 0.0f)).LengthSquared() < 0.001f);

                        if (matchingNeighbor1 != null && matchingNeighbor2 != null)
                            realShape = 3;
                        else if (matchingNeighbor1 != null)
                            realShape = 1;
                        else if (matchingNeighbor2 != null)
                            realShape = 2;
                    }

                    Primitive = new RawPrimitive();
                    Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, VertexHeightOffsets[realShape, 0], 0.5f), transform), Color.White, Color.White, uvs[0], bounds));
                    Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, VertexHeightOffsets[realShape, 1], 0.5f), transform), Color.White, Color.White, uvs[1], bounds));
                    Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, VertexHeightOffsets[realShape, 2], -0.5f), transform), Color.White, Color.White, uvs[2], bounds));
                    Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, VertexHeightOffsets[realShape, 3], -0.5f), transform), Color.White, Color.White, uvs[3], bounds));
                    Primitive.AddIndicies(new short[] { 0, 1, 3, 1, 2, 3 });

                    var sideBounds = Vector4.Zero;
                    Vector2[] sideUvs = null;

                    sideUvs = Sheet.GenerateTileUVs(new Point(3, 4), out sideBounds);

                    AddScaffoldGeometry(transform, sideBounds, sideUvs, -1.0f, false);

                    if (realShape == 3)
                    {
                        AddScaffoldGeometry(transform, sideBounds, sideUvs, 0.0f, false);
                    }
                    else if (realShape == 1)
                    {
                        sideUvs = Sheet.GenerateTileUVs(new Point(0, 4), out sideBounds);
                        AddScaffoldGeometry(transform, sideBounds, sideUvs, 0.0f, true);
                    }
                    else if (realShape == 2)
                    {
                        sideUvs = Sheet.GenerateTileUVs(new Point(0, 4), out sideBounds);
                        AddScaffoldGeometry(transform, sideBounds, sideUvs, 0.0f, false);
                    }

                    // Todo: Make these static and avoid recalculating them constantly.
                    var bumperBackBounds = Vector4.Zero;
                    var bumperBackUvs = Sheet.GenerateTileUVs(new Point(0, 5), out bumperBackBounds);
                    var bumperFrontBounds = Vector4.Zero;
                    var bumperFrontUvs = Sheet.GenerateTileUVs(new Point(1, 5), out bumperFrontBounds);
                    var bumperSideBounds = Vector4.Zero;
                    var bumperSideUvs = Sheet.GenerateTileUVs(new Point(2, 5), out bumperSideBounds);

                    foreach (var connection in GetTransformedConnections())
                    {
                        var matchingNeighbor = NeighborRails.FirstOrDefault(n => (n.Position - connection.Item1).LengthSquared() < 0.001f);
                        if (matchingNeighbor == null && rawPiece.AutoSlope)
                            matchingNeighbor = NeighborRails.FirstOrDefault(n => (n.Position - connection.Item1 - new Vector3(0.0f, 1.0f, 0.0f)).LengthSquared() < 0.001f);

                        if (matchingNeighbor == null)
                        {
                            var bumperOffset = connection.Item1 - GlobalTransform.Translation;
                            var bumperGap = Vector3.Normalize(bumperOffset) * 0.1f;
                            var bumperAngle = AngleBetweenVectors(new Vector2(bumperOffset.X, bumperOffset.Z), new Vector2(0, 0.5f));

                            var xDiag = bumperOffset.X < -0.001f || bumperOffset.X > 0.001f;
                            var zDiag = bumperOffset.Z < -0.001f || bumperOffset.Z > 0.001f;

                            if (xDiag && zDiag)
                            {
                                var y = bumperOffset.Y;
                                bumperOffset *= sqrt2;
                                bumperOffset.Y = y;

                                var endBounds = Vector4.Zero;
                                var endUvs = Sheet.GenerateTileUVs(new Point(6, 2), out endBounds);
                                Primitive.AddQuad(
                                    Matrix.CreateRotationY((float)Math.PI * 1.25f)
                                    * Matrix.CreateRotationY(bumperAngle)
                                    // This offset would not be correct if diagonals could slope.
                                    * Matrix.CreateTranslation(new Vector3(Sign(bumperOffset.X), 0.0f, Sign(bumperOffset.Z))),
                                    Color.White, Color.White, endUvs, endBounds);
                            }

                            Primitive.AddQuad(
                                Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                                * Matrix.CreateTranslation(0.0f, 0.3f, -0.2f)
                                * Matrix.CreateRotationY(bumperAngle)
                                * Matrix.CreateTranslation(bumperOffset + bumperGap),
                                Color.White, Color.White, bumperBackUvs, bumperBackBounds);

                            Primitive.AddQuad(
                                Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                                * Matrix.CreateTranslation(0.0f, 0.3f, -0.2f)
                                * Matrix.CreateRotationY(bumperAngle)
                                * Matrix.CreateTranslation(bumperOffset),
                                Color.White, Color.White, bumperFrontUvs, bumperFrontBounds);

                            var firstVoxelBelow = VoxelHelpers.FindFirstVoxelBelow(GetContainingVoxel());
                            if (firstVoxelBelow.IsValid && firstVoxelBelow.RampType == RampType.None)

                           //     if (VoxelHelpers.FindFirstVoxelBelow(GetContainingVoxel()).RampType == RampType.None)
                            {
                                Primitive.AddQuad(
                                    Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                                    * Matrix.CreateRotationY(-(float)Math.PI * 0.5f)
                                    * Matrix.CreateTranslation(0.3f, 0.3f, 0.18f)
                                    * Matrix.CreateRotationY(bumperAngle)
                                    * Matrix.CreateTranslation(bumperOffset),
                                    Color.White, Color.White, bumperSideUvs, bumperSideBounds);

                                Primitive.AddQuad(
                                    Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                                    * Matrix.CreateRotationY(-(float)Math.PI * 0.5f)
                                    * Matrix.CreateTranslation(-0.3f, 0.3f, 0.18f)
                                    * Matrix.CreateRotationY(bumperAngle)
                                    * Matrix.CreateTranslation(bumperOffset),
                                    Color.White, Color.White, bumperSideUvs, bumperSideBounds);
                            }
                        }
                    }
                }
            }

            // Everything that draws should set it's tint, making this pointless.

            var under = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

                if (under.IsValid)
                {
                    Color color = new Color(under.Sunlight ? 255 : 0, 255, 0);
                    LightRamp = color;
                }            
            else
                LightRamp = new Color(200, 255, 0);
            
            Color origTint = effect.VertexColorTint;
            if (!Active)
            {
                DoStipple(effect);
            }
            effect.VertexColorTint = VertexColor;
            effect.LightRamp = LightRamp;
            effect.World = GlobalTransform;
          
            effect.MainTexture = Sheet.GetTexture();


            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
            if (!Active)
            {
                EndDraw(effect);
            }
        }

        public void DoStipple(Shader effect)
        {
#if DEBUG
            if (effect.CurrentTechnique.Name == Shader.Technique.Stipple)
            {
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
            }
#endif
            if (effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced])
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
            {
                previousEffect = null;
            }
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
        }

        private void AddScaffoldGeometry(Matrix transform, Vector4 sideBounds, Vector2[] sideUvs, float HeightOffset, bool FlipTexture)
        {
            var uvDelta = sideUvs[1].X - sideUvs[0].X;

            if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var rawPiece))
                foreach (var railSpline in rawPiece.RailSplines)
                {
                    var uvStep = 1.0f / (railSpline.Count - 1);

                    for (var i = 1; i < railSpline.Count; ++i)
                    {
                        var baseIndex = Primitive.VertexCount;

                        Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(railSpline[i - 1].X, HeightOffset + 1.0f, railSpline[i - 1].Y), transform), Color.White, Color.White,
                            FlipTexture ? new Vector2(sideUvs[0].X + uvDelta - uvDelta * (uvStep * (i - 1)), sideUvs[0].Y) :
                            new Vector2(sideUvs[0].X + uvDelta * (uvStep * (i - 1)), sideUvs[0].Y),
                            sideBounds));

                        Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(railSpline[i].X, HeightOffset + 1.0f, railSpline[i].Y), transform), Color.White, Color.White,
                            FlipTexture ? new Vector2(sideUvs[0].X + uvDelta - uvDelta * (uvStep * i), sideUvs[0].Y) :
                            new Vector2(sideUvs[0].X + uvDelta * (uvStep * i), sideUvs[0].Y),
                            sideBounds));

                        Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(railSpline[i].X, HeightOffset, railSpline[i].Y), transform), Color.White, Color.White,
                            FlipTexture ? new Vector2(sideUvs[0].X + uvDelta - uvDelta * (uvStep * i), sideUvs[2].Y) :
                            new Vector2(sideUvs[0].X + uvDelta * (uvStep * i), sideUvs[2].Y),
                            sideBounds));

                        Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(railSpline[i - 1].X, HeightOffset, railSpline[i - 1].Y), transform), Color.White, Color.White,
                            FlipTexture ? new Vector2(sideUvs[0].X + uvDelta - uvDelta * (uvStep * (i - 1)), sideUvs[2].Y) :
                            new Vector2(sideUvs[0].X + uvDelta * (uvStep * (i - 1)), sideUvs[2].Y),
                            sideBounds));

                        Primitive.AddOffsetIndicies(new short[] { 0, 1, 3, 1, 2, 3 }, baseIndex, 6);
                    }
                }
        }

        public List<Tuple<Vector3, Vector3>> GetTransformedConnections()
        {
            if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var piece))
            {
                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Piece.Orientation) * GlobalTransform;
                return piece.EnumerateConnections().Select(l => Tuple.Create(Vector3.Transform(l.Item1, transform), Vector3.Transform(l.Item2, transform))).ToList();
            }
            else
                return new List<Tuple<Vector3, Vector3>>();
        }

        private void DetachFromNeighbors()
        {
            foreach (var neighbor in NeighborRails.Select(connection => Manager.FindComponent(connection.NeighborID)))
            {
                if (neighbor is RailEntity)
                    (neighbor as RailEntity).DetachNeighbor(this.GlobalID);
            }

            NeighborRails.Clear();
        }

        private void DetachNeighbor(uint ID)
        {
            NeighborRails.RemoveAll(connection => connection.NeighborID == ID);
            ResetPrimitive();
        }

        private void AttachToNeighbors()
        {
            global::System.Diagnostics.Debug.Assert(NeighborRails.Count == 0);

            if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var myPiece))
            {
                var myEndPoints = GetTransformedConnections().SelectMany(l => new Vector3[] { l.Item1, l.Item2 });
                foreach (var entity in Manager.World.EnumerateIntersectingRootObjects(this.BoundingBox.Expand(0.5f), CollisionType.Static))
                {
                    if (Object.ReferenceEquals(entity, this)) continue;
                    var neighborRail = entity as RailEntity;
                    if (neighborRail == null) continue;
                    var neighborEndPoints = neighborRail.GetTransformedConnections().SelectMany(l => new Vector3[] { l.Item1, l.Item2 });
                    foreach (var point in myEndPoints)
                    {
                        foreach (var nPoint in neighborEndPoints)
                            if ((nPoint - point).LengthSquared() < 0.01f)
                            {
                                AttachNeighbor(neighborRail.GlobalID, point, false);
                                neighborRail.AttachNeighbor(this.GlobalID, point, false);
                                goto __CONTINUE;
                            }

                        if (myPiece.AutoSlope)
                        {
                            var raisedPoint = point + new Vector3(0.0f, 1.0f, 0.0f);
                            foreach (var nPoint in neighborEndPoints)
                                if ((nPoint - raisedPoint).LengthSquared() < 0.01f)
                                {
                                    AttachNeighbor(neighborRail.GlobalID, raisedPoint, true);
                                    neighborRail.AttachNeighbor(this.GlobalID, raisedPoint, false);
                                    goto __CONTINUE;
                                }
                        }

                        if (Library.GetRailPiece(neighborRail.Piece.RailPiece).HasValue(out var neighborPiece))
                            if (neighborPiece.AutoSlope)
                            {
                                var loweredPoint = point - new Vector3(0.0f, 1.0f, 0.0f);
                                foreach (var nPoint in neighborEndPoints)
                                    if ((nPoint - loweredPoint).LengthSquared() < 0.01f)
                                    {
                                        AttachNeighbor(neighborRail.GlobalID, point, false);
                                        neighborRail.AttachNeighbor(this.GlobalID, point, true);
                                        goto __CONTINUE;
                                    }
                            }
                    }
                    __CONTINUE:;
                }
            }
        }

        private void AttachNeighbor(uint ID, Vector3 Position, bool Raised)
        {
            NeighborRails.Add(new NeighborConnection
            {
                NeighborID = ID,
                Position = Position,
                Raised = Raised
            });

            ResetPrimitive();
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

        public void UpdatePiece(JunctionPiece Piece, VoxelHandle Location)
        {
            DetachFromNeighbors();

            this.Piece = Piece;
            this.Location = Location;

            LocalTransform = Matrix.CreateTranslation(Location.WorldPosition + new Vector3(Piece.Offset.X, 0, Piece.Offset.Y) + new Vector3(0.5f, 0.2f, 0.5f));

            if (Library.GetRailPiece(Piece.RailPiece).HasValue(out var piece))
                Frame = piece.Tile;

            ResetPrimitive();

            // Hack to make the listener update it's damn bounding box
            var deathTrigger = EnumerateChildren().OfType<GenericVoxelListener>().FirstOrDefault();
            if (deathTrigger != null)
                deathTrigger.LocalTransform = Matrix.Identity;

            AttachToNeighbors();
            PropogateTransforms();
        }
    }
}
