using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// This is a convenience class for drawing lines, boxes, etc. to the screen.
    /// </summary>
    public class Drawer3D
    {
        private const int MaxTriangles = 64;
        private static VertexPositionColor[] Verticies = new VertexPositionColor[MaxTriangles * 3];
        private static int VertexCount;
        private static GraphicsDevice Device {  get { return GameStates.GameState.Game.GraphicsDevice; } }
        private static Shader Effect;
        private static OrbitCamera Camera;
        private static object renderLock = new object();

        public static void Cleanup()
        {
            Verticies = new VertexPositionColor[MaxTriangles * 3];
            VertexCount = 0;
            Effect = null;
            Camera = null;
            Boxes.Clear();
            Segments.Clear();

        }

        private struct Box
        {
            public BoundingBox RealBox;
            public float Thickness;
            public Color Color;
            public bool Warp;
        }

        private static List<Box> Boxes = new List<Box>();

        private struct Segment
        {
            public Vector3 A;
            public Vector3 B;
            public Color Color;
            public float Thickness;
        }

        private static List<Segment> Segments = new List<Segment>();

        public enum PrimitiveVertexCounts
        {
            Triangle = 3,
            LineSegment = 2 * Triangle,
            TopBox = 4 * 1 * LineSegment,
            Box = 2 * TopBox + 4 * LineSegment,
        }


        private static void _flush()
        {
            if (VertexCount == 0) return;

            //for (var i = 0; i < VertexCount; ++i)
            //    Verticies[i].Position += VertexNoise.GetNoiseVectorFromRepeatingTexture(Verticies[i].Position);

            BlendState origBlen = Device.BlendState;
            Device.BlendState = BlendState.NonPremultiplied;

            RasterizerState oldState = Device.RasterizerState;
            Device.RasterizerState = RasterizerState.CullNone;

            Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.Untextured];
            Effect.View = Camera.ViewMatrix;
            Effect.Projection = Camera.ProjectionMatrix;
            Effect.World = Matrix.Identity;
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawUserPrimitives(PrimitiveType.TriangleList, Verticies, 0, VertexCount / 3);
            }
            
            Effect.SetTexturedTechnique();
            
            if (oldState != null)
            {
                Device.RasterizerState = oldState;
            }

            if (origBlen != null)
            {
                Device.BlendState = origBlen;
            }

            VertexCount = 0;
        }

        public static int WriteTriangle(Vector3 A, Vector3 B, Vector3 C, Color Color, int start, VertexPositionColor[] buffer)
        {
            buffer[start] = new VertexPositionColor
            {
                Position = A,
                Color = Color,
            };

            buffer[start + 1] = new VertexPositionColor
            {
                Position = B,
                Color = Color,
            };

            buffer[start + 2] = new VertexPositionColor
            {
                Position = C,
                Color = Color,
            };
            return start + 3;
        }

        public static int WriteTriangle(Vector4 A, Vector4 B, Vector4 C, Vector3 direction, Color Color, int start, ThickLineVertex[] buffer)
        {
            buffer[start] = new ThickLineVertex
            {
                Position = A,
                Color = Color,
                Direction = direction
            };

            buffer[start + 1] = new ThickLineVertex
            {
                Position = B,
                Color = Color,
                Direction = direction
            };

            buffer[start + 2] = new ThickLineVertex
            {
                Position = C,
                Color = Color,
                Direction = direction
            };
            return start + 3;
        }

        private static void _addTriangle(Vector3 A, Vector3 B, Vector3 C, Color Color)
        {
            WriteTriangle(A, B, C, Color, VertexCount, Verticies);
            VertexCount += 3;
            if (VertexCount >= MaxTriangles * 3)
                _flush();
        }

        public static int WriteLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, bool Warp, int start, VertexPositionColor[] buffer)
        {
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
            }

            var aRay = Vector3.Up;
            var bRay = A - B;
            if (Math.Abs(Vector3.Dot(aRay, bRay)) > 0.99)
            {
                aRay = Vector3.Right;
            }
            var perp = Vector3.Cross(aRay, bRay);
            perp.Normalize();
            perp *= Thickness / 2;

            start = WriteTriangle(A + perp, B + perp, A - perp, Color, start, buffer);
            return WriteTriangle(A - perp, B - perp, B + perp, Color, start, buffer);
        }

        public static int WriteLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, bool Warp, int start, ThickLineVertex[] buffer)
        {
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
            }

            var bRay = A - B;
            bRay.Normalize();

            start = WriteTriangle(new Vector4(A, Thickness), new Vector4(B, Thickness), new Vector4(A, -Thickness), bRay, Color, start, buffer);
            return WriteTriangle(new Vector4(A, -Thickness), new Vector4(B, -Thickness), new Vector4(B, Thickness), bRay, Color, start, buffer);
        }


        private static void _addLineSegment(Vector3 A, Vector3 B, Color Color, float Thickness, bool Warp)
        {
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
            }

            var aRay = A - Camera.Position;
            var bRay = A - B;
            var perp = Vector3.Cross(aRay, bRay);
            perp.Normalize();
            perp *= Thickness / 2;

            _addTriangle(A + perp, B + perp, A - perp, Color);
            _addTriangle(A - perp, B - perp, B + perp, Color);
        }

        private static void _addBox(Vector3 M, Vector3 S, Color C, float T, bool Warp)
        {
            float halfT = T * 0.5f;
            S += Vector3.One * T;
            M -= Vector3.One * halfT;
            // Draw bottom loop.
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z), C, T, Warp);

            // Draw top loop.
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp);

            // Draw uprights
            _addLineSegment(new Vector3(M.X, M.Y, M.Z), new Vector3(M.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);
            _addLineSegment(new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), C, T, Warp);

        }

        private static float epsilon = 0.01f;
        private static Vector3[] boxOffsets =
        {
                new Vector3(-epsilon, -epsilon, -epsilon), new Vector3(0, -epsilon, -epsilon),
                new Vector3(0, -epsilon, -epsilon), new Vector3(0, -epsilon, 0),
                new Vector3(0, -epsilon, 0), new Vector3(-epsilon, -epsilon, 0),
                new Vector3(-epsilon, -epsilon, 0), new Vector3(-epsilon, -epsilon, -epsilon),

                new Vector3(-epsilon, 0, -epsilon), new Vector3(0, 0, -epsilon),
                new Vector3(0, 0, -epsilon), new Vector3(0, 0, 0),
                new Vector3(0, 0, 0), new Vector3(-epsilon, 0, 0),
                new Vector3(-epsilon, 0, 0), new Vector3(-epsilon, 0, -epsilon),

                new Vector3(-epsilon, -epsilon, -epsilon), new Vector3(-epsilon, 0, -epsilon),
                new Vector3(0, -epsilon, -epsilon), new Vector3(0, 0, -epsilon),
                new Vector3(0, -epsilon, 0), new Vector3(0, 0, 0),
                new Vector3(-epsilon, -epsilon, 0), new Vector3(-epsilon, 0, 0),
         };

        public static int WriteBox(Vector3 M, Vector3 S, Color C, float T, bool Warp, int start, ThickLineVertex[] buffer)
        {

            Vector3[] verts =
            {
                new Vector3(M.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z),
                new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y, M.Z + S.Z),
                new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z + S.Z),
                new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y, M.Z),

                new Vector3(M.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z),
                new Vector3(M.X + S.X, M.Y + S.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z),
                new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z),
                new Vector3(M.X, M.Y + S.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z),

                new Vector3(M.X, M.Y, M.Z), new Vector3(M.X, M.Y + S.Y, M.Z),
                new Vector3(M.X + S.X, M.Y, M.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z),
                new Vector3(M.X + S.X, M.Y, M.Z + S.Z), new Vector3(M.X + S.X, M.Y + S.Y, M.Z + S.Z),
                new Vector3(M.X, M.Y, M.Z + S.Z), new Vector3(M.X, M.Y + S.Y, M.Z + S.Z),
            };
            if (Warp)
            {
                for (int i =0; i < verts.Length; i++)
                {
                    verts[i] += VertexNoise.GetNoiseVectorFromRepeatingTexture(verts[i]);
                }
            }

            for (int i = 0; i < verts.Length; i+=2)
            {
                start = WriteLineSegment(verts[i] + boxOffsets[i], verts[i + 1] + boxOffsets[i + 1], C, T, false, start, buffer);
            }
            return start;
        }

        public static int WriteTopBox(Vector3 M, Vector3 S, Color color, float T, bool Warp, int start, ThickLineVertex[] buffer)
        {
            float halfT = T * 0.5f;

            float heightOffset = 0.05f;
            Vector3 A = M + new Vector3(0, S.Y, 0);
            Vector3 B = M + new Vector3(S.X, S.Y, 0);
            Vector3 C = M + new Vector3(S.X, S.Y, S.Z);
            Vector3 D = M + new Vector3(0, S.Y, S.Z);
            if (Warp)
            {
                A += VertexNoise.GetNoiseVectorFromRepeatingTexture(A);
                B += VertexNoise.GetNoiseVectorFromRepeatingTexture(B);
                C += VertexNoise.GetNoiseVectorFromRepeatingTexture(C);
                D += VertexNoise.GetNoiseVectorFromRepeatingTexture(D);
            }
            // Draw top loop.
            // A   1      B
            //   *+---+*
            // 4 |     | 2
            //   *+---+*
            // D   3      C
            start = WriteLineSegment(A + new Vector3(0, heightOffset, halfT), B + new Vector3(0, heightOffset, halfT), color, T, false, start, buffer);
            start = WriteLineSegment(B + new Vector3(-halfT, heightOffset, halfT), C + new Vector3(-halfT, heightOffset, -halfT), color, T, false, start, buffer);
            start = WriteLineSegment(C + new Vector3(0, heightOffset, -halfT),  D + new Vector3(halfT, heightOffset, -halfT), color, T, false, start, buffer);
            start = WriteLineSegment(D + new Vector3(halfT, heightOffset, 0), A + new Vector3(halfT, heightOffset, halfT), color, T, false, start, buffer);
            return start;
        }

        public static void Render(
            GraphicsDevice Device, 
            Shader Effect, 
            OrbitCamera Camera,
            DesignationSet Designations,
            WorldManager World)
        {
            lock (renderLock)
            {
                Drawer3D.Effect = Effect;
                Drawer3D.Camera = Camera;

                var colorModulation = Math.Abs(Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds*2.0f));

                DrawEntityDesignations(World, Designations);

                foreach (var box in Boxes)
                    _addBox(box.RealBox.Min, box.RealBox.Max - box.RealBox.Min, box.Color, box.Thickness, box.Warp);

                foreach (var segment in Segments)
                    _addLineSegment(segment.A, segment.B, segment.Color, segment.Thickness, false);

                _flush();

                Boxes.Clear();
                Segments.Clear();
            }
        }

        private static void DrawEntityDesignations(WorldManager World, DesignationSet Set)
        {
            // Todo: Can this be drawn by the entity, allowing it to be properly frustrum culled?
            // - Need to add a 'gestating' entity state to the alive/dead/active mess.

            foreach (var entity in Set.EnumerateEntityDesignations())
            {
                if ((entity.Type & World.Renderer.PersistentSettings.VisibleTypes) == entity.Type)
                {
                    var props = Library.GetDesignationTypeProperties(entity.Type).Value;

                    // Todo: More consistent drawing?
                    if (entity.Type == DesignationType.PlaceObject)
                    {
                        entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, true);
                        if (!entity.Body.Active)
                            entity.Body.SetVertexColorRecursive(props.Color);
                    }
                    else
                    {
                        var box = entity.Body.GetBoundingBox();
                        _addBox(box.Min, box.Max - box.Min, props.Color, props.LineWidth, false);
                        //entity.Body.SetVertexColorRecursive(props.Color);
                    }
                }
                else if (entity.Type == DesignationType.PlaceObject) // Make the ghost object invisible if these designations are turned off.
                    entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, false);
            }
        }

        public static void DrawBox(BoundingBox box, Color color, float thickness, bool warp)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                Boxes.Add(new Box
                {
                    RealBox = box,
                    Color = color,
                    Thickness = thickness,
                    Warp = warp
                });
            }
        }

        public static void DrawLine(Vector3 A, Vector3 B, Color Color, float Thickness)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                Segments.Add(new Segment
                {
                    A = A,
                    B = B,
                    Color = Color,
                    Thickness = Thickness
                });
            }
        }

        public static void DrawLineList(List<Vector3> points, List<Color> colors, float v)
        {
            if (!DwarfGame.HasRendered) return;
            lock (renderLock)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Segments.Add(new Segment { A = points[i], B = points[i + 1], Color = colors[i], Thickness = v});
                }
            }

        }
    }
}
