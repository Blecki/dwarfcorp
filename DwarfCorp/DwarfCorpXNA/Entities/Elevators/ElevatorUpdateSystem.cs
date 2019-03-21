using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.Elevators
{
    public class ElevatorUpdateSystem : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new ElevatorUpdateSystem();
        }

        private List<ElevatorTrack> Objects = new List<ElevatorTrack>();
        private List<ElevatorShaft> Shafts = new List<ElevatorShaft>();

        public override void ComponentCreated(GameComponent C)
        {
            if (C is ElevatorTrack elevatorTrack)
                Objects.Add(elevatorTrack);
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            if (C is ElevatorTrack elevatorTrack)
                Objects.Remove(elevatorTrack);
        }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            foreach (var shaft in Shafts)
                Drawer3D.DrawBox(shaft.BoundingBox, Color.Orange, 0.1f, false);
        }

        public override void Update(DwarfTime GameTime)
        {
            // Todo: Limit update rate.

            var segmentsNeedingShaftUpdate = new List<ElevatorTrack>();

            foreach (var elevatorTrack in Objects)
            {
                if (elevatorTrack.NeedsConnectionUpdate || elevatorTrack.HasMoved)
                {
                    elevatorTrack.NeedsConnectionUpdate = false;

                    RemoveSegmentFromShaft(elevatorTrack);

                    elevatorTrack.PropogateTransforms();
                    elevatorTrack.DetachFromNeighbors();
                    elevatorTrack.AttachToNeighbors();

                    segmentsNeedingShaftUpdate.Add(elevatorTrack);
                }
            }

            foreach (var elevatorTrack in segmentsNeedingShaftUpdate)
            {
                if (!elevatorTrack.NeedsShaftUpdate) continue;
                MergeShafts(elevatorTrack);
            }

                // Todo: Generate platform object possibly
            
        }

        private void RemoveSegmentFromShaft(ElevatorTrack Track)
        {
            var above = Track.Manager.FindComponent(Track.TrackAbove) as ElevatorTrack;
            if (above != null)
                BuildShaftUpward(above);

            var below = Track.Manager.FindComponent(Track.TrackBelow) as ElevatorTrack;
            if (below != null)
                BuildShaftDownward(below);

            Shafts.Remove(Track.Shaft);
            Track.Shaft = ElevatorShaft.Create(Track);
            Shafts.Add(Track.Shaft);
            Track.NeedsShaftUpdate = true;
        }

        private void BuildShaftUpward(ElevatorTrack Track)
        {
            // Build list of all segments in shaft.
            var segments = new List<ElevatorTrack>();
            segments.Add(Track);

            while (true)
            {
                var upper = Track.Manager.FindComponent(Track.TrackAbove) as ElevatorTrack;
                if (upper != null)
                {
                    segments.Add(upper);
                    Track = upper;
                }
                else
                    break;
            }

            CreateNewShaft(segments);
        }

        private void CreateNewShaft(List<ElevatorTrack> segments)
        {
            var newShaft = ElevatorShaft.Create(segments);
            Shafts.Add(newShaft);

            foreach (var segment in segments)
            {
                Shafts.Remove(segment.Shaft);
                segment.Shaft = newShaft;
                segment.NeedsShaftUpdate = false;
            }
        }

        private void BuildShaftDownward(ElevatorTrack Track)
        {
            // Build list of all segments in shaft.
            var segments = new List<ElevatorTrack>();
            segments.Add(Track);

            while (true)
            {
                var lower = Track.Manager.FindComponent(Track.TrackBelow) as ElevatorTrack;
                if (lower != null)
                {
                    segments.Add(lower);
                    Track = lower;
                }
                else
                    break;
            }

            CreateNewShaft(segments);
        }

        private void MergeShafts(ElevatorTrack Track)
        {
            // Find bottom of shaft.
            var bottom = Track;
            while (true)
            {
                var lower = Track.Manager.FindComponent(bottom.TrackBelow) as ElevatorTrack;
                if (lower != null)
                    bottom = lower;
                else
                    break;
            }

            BuildShaftUpward(bottom);
        }
    }
}
