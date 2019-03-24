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

        private List<ElevatorShaft> Objects = new List<ElevatorShaft>();
        private List<ElevatorStack> Shafts = new List<ElevatorStack>();
        
        public override void ComponentCreated(GameComponent C)
        {
            if (C is ElevatorShaft elevatorTrack)
                Objects.Add(elevatorTrack);
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            if (C is ElevatorShaft elevatorTrack)
            {
                RemoveSegmentFromShaft(elevatorTrack);
                
                Objects.Remove(elevatorTrack);
                RemoveShaft(elevatorTrack.Shaft);

                DetachFromNeighbors(elevatorTrack.Manager, elevatorTrack);
            }
        }

        private void RemoveShaft(ElevatorStack Shaft)
        {
            if (Shaft.Platform != null)
            {
                Shaft.Platform.Delete();
                Shaft.Platform = null;
            }

            Shafts.Remove(Shaft);
        }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            foreach (var shaft in Shafts)
                Drawer3D.DrawBox(shaft.BoundingBox, Color.Orange, 0.1f, false);
        }

        public override void Update(DwarfTime GameTime)
        {
            // Todo: Limit update rate.

            var segmentsNeedingShaftUpdate = new List<ElevatorShaft>();

            foreach (var elevatorTrack in Objects)
            {
                if (elevatorTrack.NeedsConnectionUpdate || elevatorTrack.HasMoved)
                {
                    elevatorTrack.NeedsConnectionUpdate = false;

                    RemoveSegmentFromShaft(elevatorTrack);

                    elevatorTrack.PropogateTransforms();
                    DetachFromNeighbors(elevatorTrack.Manager, elevatorTrack);
                    AttachToNeighbors(elevatorTrack.Manager, elevatorTrack);

                    segmentsNeedingShaftUpdate.Add(elevatorTrack);
                    elevatorTrack.NeedsShaftUpdate = true;
                }
            }

            foreach (var elevatorTrack in segmentsNeedingShaftUpdate)
            {
                if (!elevatorTrack.NeedsShaftUpdate) continue;
                MergeShafts(elevatorTrack);
            }

                // Todo: Generate platform object possibly
            
        }

        private void RemoveSegmentFromShaft(ElevatorShaft Track)
        {
            var above = Track.Manager.FindComponent(Track.TrackAbove) as ElevatorShaft;
            if (above != null)
                BuildShaftUpward(above);

            var below = Track.Manager.FindComponent(Track.TrackBelow) as ElevatorShaft;
            if (below != null)
                BuildShaftDownward(below);

            CreateNewShaft(new ElevatorShaft[] { Track });
        }

        private void BuildShaftUpward(ElevatorShaft Track)
        {
            // Build list of all segments in shaft.
            var segments = new List<ElevatorShaft>();
            segments.Add(Track);

            while (true)
            {
                var upper = Track.Manager.FindComponent(Track.TrackAbove) as ElevatorShaft;
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

        private void CreateNewShaft(IEnumerable<ElevatorShaft> segments)
        {
            var newShaft = ElevatorStack.Create(segments);
            Shafts.Add(newShaft);
            newShaft.Platform = new ElevatorPlatform(segments.First().Manager, segments.First().Position);

            foreach (var segment in segments)
            {
                RemoveShaft(segment.Shaft);
                segment.Shaft = newShaft;
                segment.NeedsShaftUpdate = false;
            }
        }

        private void BuildShaftDownward(ElevatorShaft Track)
        {
            // Build list of all segments in shaft.
            var segments = new List<ElevatorShaft>();
            segments.Add(Track);

            while (true)
            {
                var lower = Track.Manager.FindComponent(Track.TrackBelow) as ElevatorShaft;
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

        private void MergeShafts(ElevatorShaft Track)
        {
            // Find bottom of shaft.
            var bottom = Track;
            while (true)
            {
                var lower = Track.Manager.FindComponent(bottom.TrackBelow) as ElevatorShaft;
                if (lower != null)
                    bottom = lower;
                else
                    break;
            }

            BuildShaftUpward(bottom);
        }

        private bool FindNeighbor(ComponentManager Manager, BoundingBox Bounds, out ElevatorShaft Neighbor)
        {
            Neighbor = null;

            foreach (var entity in Manager.World.EnumerateIntersectingObjects(Bounds, CollisionType.Static))
            {
                if (Object.ReferenceEquals(entity, this)) continue;
                if (entity is ElevatorShaft found)
                {
                    Neighbor = found;
                    return true;
                }
            }

            return false;
        }

        public void AttachToNeighbors(ComponentManager Manager, ElevatorShaft Segment)
        {
            System.Diagnostics.Debug.Assert(Segment.TrackAbove == ComponentManager.InvalidID && Segment.TrackBelow == ComponentManager.InvalidID);

            if (FindNeighbor(Manager, Segment.BoundingBox.Offset(0.0f, 1.0f, 0.0f).Expand(-0.2f), out ElevatorShaft aboveNeighbor))
            {
                Segment.TrackAbove = aboveNeighbor.GlobalID;
                aboveNeighbor.TrackBelow = Segment.GlobalID;
            }

            if (FindNeighbor(Manager, Segment.BoundingBox.Offset(0.0f, -1.0f, 0.0f).Expand(-0.2f), out ElevatorShaft belowNeighbor))
            {
                Segment.TrackBelow = belowNeighbor.GlobalID;
                belowNeighbor.TrackAbove = Segment.GlobalID;
            }
        }

        public void DetachFromNeighbors(ComponentManager Manager, ElevatorShaft Segment)
        {
            if (Manager.FindComponent(Segment.TrackAbove) is ElevatorShaft neighbor)
                neighbor.TrackBelow = ComponentManager.InvalidID;
            if (Manager.FindComponent(Segment.TrackBelow) is ElevatorShaft neighbor2)
                neighbor2.TrackAbove = ComponentManager.InvalidID;

            Segment.TrackAbove = ComponentManager.InvalidID;
            Segment.TrackBelow = ComponentManager.InvalidID;
        }
    }
}
