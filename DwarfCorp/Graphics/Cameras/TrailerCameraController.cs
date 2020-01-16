using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class TrailerCameraController
    {
        public Vector3 Velocity = Vector3.Zero;
        public float Spin = 0.0f;
        public float Zoom = 0.0f;
        public Timer Duration = new Timer(10.0f, true, Timer.TimerMode.Real);
        private bool _lightActive = false;
        public bool Active = true;

        public void Activate(WorldManager world, OrbitCamera camera)
        {
            Active = true;
            _lightActive = GameSettings.Current.CursorLightEnabled;
            GameSettings.Current.CursorLightEnabled = false;
            world.UserInterface.Gui.RootItem.Hidden = true;
            world.UserInterface.Gui.RootItem.Invalidate();
            
            camera.Control = OrbitCamera.ControlType.Overhead;
            camera.EnableControl = false;
        }

        public void Deactivate(WorldManager world, OrbitCamera camera)
        {
            Active = false;
            GameSettings.Current.CursorLightEnabled = _lightActive;
            world.UserInterface.Gui.RootItem.Hidden = false;
            world.UserInterface.Gui.RootItem.Invalidate();
            camera.Control = OrbitCamera.ControlType.Overhead;
            camera.EnableControl = true;
        }

        public void Update(WorldManager world, OrbitCamera camera)
        {
            float dt = (float)DwarfTime.LastTime.ElapsedRealTime.TotalSeconds;
            Duration.Update(DwarfTime.LastTime);
            Vector3 forward = (camera.Target - camera.Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, camera.UpVector);
            Vector3 up = Vector3.Cross(right, forward);
            right.Normalize();
            up.Normalize();
            Vector3 vel = Velocity.X * right + Velocity.Y * up - Velocity.Z * forward;
            float l = vel.Length();
            vel.Y = 0;
            
            if (vel.Length() > 0.001f)
            {
                vel.Normalize();
                vel *= l;
            }
            camera.Position += vel * dt;
            camera.Target += vel * dt;
            camera.OverheadUpdate(DwarfTime.LastTime, world.ChunkManager, 0.0f, Spin * dt, Zoom * dt);

            if (Duration.HasTriggered)
            {
                Deactivate(world, camera);
            }
        }
    }
}