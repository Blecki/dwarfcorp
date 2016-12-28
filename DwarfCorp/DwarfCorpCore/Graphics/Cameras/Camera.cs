// Camera.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Manages a 3D camera with projection and view matrices.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Camera
    {
        /// <summary>
        /// The camera populates its projection matrices differently depending on the mode.
        /// </summary>
        public enum ProjectionMode
        {
            /// <summary>
            /// Orthographic cameras have no vanishing perspective lines.
            /// </summary>
            Orthographic,
            /// <summary>
            /// Perspective cameras have a field of view and vanishing lines.
            /// </summary>
            Perspective
        }


        /// <summary>
        /// The position of the camera.
        /// </summary>
        private Vector3 position = Vector3.Zero;

        private Vector3 lastPosition = Vector3.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class.
        /// </summary>
        /// <param name="target">The target that the camera is looking at.</param>
        /// <param name="position">The position of the camera.</param>
        /// <param name="fov">The vertical field of view of the camera in radians.</param>
        /// <param name="aspectRatio">The aspect ratio (width/height)</param>
        /// <param name="nearPlane">The near plane (in voxels).</param>
        /// <param name="farPlane">The far plane (in voxels).</param>
        public Camera(Vector3 target, Vector3 position, float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            UpVector = Vector3.Up;
            Target = target;
            Position = position;
            NearPlane = nearPlane;
            FarPlane = farPlane;
            AspectRatio = aspectRatio;
            FOV = fov;
            Velocity = Vector3.Zero;
            Projection = ProjectionMode.Perspective;
            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        public Camera()
        {
        }

        /// <summary>
        /// Gets or sets the target. The target is the position in the world that the camera is
        /// looking at.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public Vector3 Target { get; set; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public Vector3 Position
        {
            get { return position; }
            set { setPosition(value); }
        }

        /// <summary>
        /// Gets or sets the field of view.
        /// </summary>
        /// <value>
        /// The vertical field of view of the camera in radians.
        /// </value>
        public float FOV { get; set; }
        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>
        /// The aspect ratio (width/height) of the projection matrix.
        /// </value>
        public float AspectRatio { get; set; }
        /// <summary>
        /// Gets or sets the near plane.
        /// </summary>
        /// <value>
        /// The near plane (in voxels) of the camera.
        /// </value>
        public float NearPlane { get; set; }
        /// <summary>
        /// Gets or sets the far plane.
        /// </summary>
        /// <value>
        /// The far plane (in voxels) of the camera.
        /// </value>
        public float FarPlane { get; set; }
        /// <summary>
        /// Gets or sets up vector.
        /// </summary>
        /// <value>
        /// The direction that the player perceives as "Up"
        /// </value>
        public Vector3 UpVector { get; set; }
        /// <summary>
        /// Gets or sets the view matrix.
        /// </summary>
        /// <value>
        /// The view matrix.
        /// </value>
        public Matrix ViewMatrix { get; set; }
        /// <summary>
        /// Gets or sets the projection matrix.
        /// </summary>
        /// <value>
        /// The projection matrix.
        /// </value>
        public Matrix ProjectionMatrix { get; set; }
        /// <summary>
        /// Gets or sets the camera's velocity.
        /// </summary>
        /// <value>
        /// The velocity.
        /// </value>
        public Vector3 Velocity { get; set; }

        /// <summary>
        /// Gets or sets the projection mode (orthographic or perspective)
        /// </summary>
        /// <value>
        /// The projection.
        /// </value>
        public ProjectionMode Projection { get; set; }

        /// <summary>
        /// The last mouse wheel (in ticks)
        /// </summary>
        /// <value>
        /// The last mouse wheel (in ticks)
        /// </value>
        public int LastWheel { get; set; }

        private void setPosition(Vector3 v)
        {
            position = v;
        }

        /// <summary>
        /// Projects the specified position onto the camera.
        /// </summary>
        /// <param name="pos">The position in the world.</param>
        /// <returns>A camera-centric coordinate where Z is out, X is to the right, and Y is down.</returns>
        public Vector3 Project(Vector3 pos)
        {
            return GameState.Game.Graphics.GraphicsDevice.Viewport.Project(pos, ProjectionMatrix, ViewMatrix,
                Matrix.Identity);
        }

        /// <summary>
        /// The inverse of <see cref="Camera.Project"/>
        /// </summary>
        /// <param name="pos">The camera-centric position.</param>
        /// <returns>A position in world coordinates.</returns>
        public Vector3 UnProject(Vector3 pos)
        {
            return GameState.Game.Graphics.GraphicsDevice.Viewport.Unproject(pos, ProjectionMatrix, ViewMatrix,
                Matrix.Identity);
        }

        /// <summary>
        ///Updates the camera.
        /// </summary>
        /// <param name="time">The current.</param>
        /// <param name="chunks">The chunks.</param>
        public virtual void Update(DwarfTime time, ChunkManager chunks)
        {
            lastPosition = Position;
            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        /// <summary>
        /// Creates a view matrix based on the camera's parameters.
        /// </summary>
        public virtual void UpdateViewMatrix()
        {
            ViewMatrix = Matrix.CreateLookAt(Position, Target, UpVector);
        }

        /// <summary>
        /// Creates a projection matrix based on the camera's parameters.
        /// </summary>
        public void UpdateProjectionMatrix()
        {
            if (Projection == ProjectionMode.Perspective)
            {
                ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(FOV, AspectRatio, NearPlane, FarPlane);
            }
            else
            {
                var height = (float) (30.0f*Math.Tan(FOV/2));
                var width = (float) (AspectRatio*30.0f*Math.Tan(FOV/2));
                ProjectionMatrix = Matrix.CreateOrthographic(width, height, NearPlane, FarPlane);
            }
        }

        /// <summary>
        /// Determines whether the specified bounding sphere is visible to the camera.
        /// </summary>
        /// <param name="boundingSphere">The bounding sphere.</param>
        /// <returns>
        ///   <c>true</c> if [is in view] [the specified bounding sphere]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInView(BoundingBox boundingSphere)
        {
            return GetFrustrum().Intersects(boundingSphere);
        }

        /// <summary>
        /// Returns a frustum (truncated pyramid) containing the camera's viewing volume
        /// </summary>
        /// <returns>The frustum bounding the camera's viewing volume.</returns>
        public BoundingFrustum GetFrustrum()
        {
            return new BoundingFrustum(ViewMatrix*ProjectionMatrix);
        }
    }
}