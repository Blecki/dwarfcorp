using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.Elevators
{
    public class ElevatorStack
    {
        public class EnqueuedRider
        {
            public CreatureAI Rider;
            public ElevatorMoveState RidePlan;
        }

        public enum States
        {
            Idle,
            MovingToRider,
            PickingUpRider,
            TransportingRider,
            DroppingOffRider
        }

        public States State = States.Idle;
        public EnqueuedRider CurrentRider = null;

        public ElevatorPlatform Platform = null;
        public List<ElevatorShaft> Pieces = new List<ElevatorShaft>(); // Make private
        public BoundingBox BoundingBox;
        public bool Invalid { get; private set; }
        public List<EnqueuedRider> RiderQueue = new List<EnqueuedRider>();

        private Timer TimeoutTimer = new Timer(10.0f, true, Timer.TimerMode.Game);

        public static ElevatorStack Create(IEnumerable<ElevatorShaft> Pieces)
        {
            var r = new ElevatorStack();
            r.Pieces.AddRange(Pieces);
            r.UpdateBoundingBox();
            r.Invalid = false;
            return r;
        }

        public void Destroy()
        {
            Invalid = true;
            RiderQueue.Clear();
        }

        private void UpdateBoundingBox()
        {
            BoundingBox = Pieces[0].BoundingBox;
            for (var i = 1; i < Pieces.Count; ++i)
                BoundingBox = BoundingBox.CreateMerged(BoundingBox, Pieces[i].BoundingBox);
        }

        public bool EnqueuDwarf(CreatureAI Rider, ElevatorMoveState RidePlan)
        {
            RiderQueue.RemoveAll(r => Object.ReferenceEquals(Rider, r.Rider));
            RiderQueue.Add(new EnqueuedRider
            {
                Rider = Rider,
                RidePlan = RidePlan
            });
            return true;
        }

        public bool ReadyToBoard(CreatureAI Rider)
        {
            if (State != States.PickingUpRider) return false;
            if (CurrentRider != null && Object.ReferenceEquals(Rider, CurrentRider.Rider)) return true;
            return false;
        }

        public void StartMotion(CreatureAI Rider)
        {
            if (State == States.PickingUpRider && CurrentRider != null && Object.ReferenceEquals(CurrentRider.Rider, Rider))
                State = States.TransportingRider;
        }

        public bool AtDestination(CreatureAI Rider)
        {
            if (State != States.DroppingOffRider) return false;
            if (CurrentRider != null && Object.ReferenceEquals(Rider, CurrentRider.Rider)) return true;
            return false;
        }

        public void Done(CreatureAI Rider)
        {
            if (State == States.DroppingOffRider && CurrentRider != null && Object.ReferenceEquals(CurrentRider.Rider, Rider))
            {
                State = States.Idle;
                CurrentRider = null;
            }
        }

        public void Update(DwarfTime Time)
        {
            if (Invalid) return; // ??
            TimeoutTimer.Update(Time);

            switch (State)
            {
                case States.Idle:
                    if (RiderQueue.Count == 0)
                        return;
                    CurrentRider = RiderQueue[0];
                    RiderQueue.RemoveAt(0);
                    State = States.MovingToRider;
                    TimeoutTimer.Reset();
                    break;
                case States.MovingToRider:
                    if (MovePlatform(CurrentRider.RidePlan.Entrance, null, Time))
                        State = States.PickingUpRider;
                    break;
                case States.PickingUpRider:
                    if (TimeoutTimer.HasTriggered)
                    {
                        CurrentRider = null;
                        State = States.Idle;
                    }
                    break;
                case States.TransportingRider:
                    if (MovePlatform(CurrentRider.RidePlan.Exit, CurrentRider.Rider, Time))
                        State = States.DroppingOffRider;
                    break;
                case States.DroppingOffRider:
                    if (TimeoutTimer.HasTriggered)
                    {
                        CurrentRider = null;
                        State = States.Idle;
                    }

                    break;
            }
            
            // Todo: Time out when rider doesn't enter fast enough. 
            // Todo: Time out and forget a rider when they don't exit fast enough.
        }

        private bool MovePlatform(ElevatorShaft Destination, CreatureAI Rider, DwarfTime Time)
        {
            var dest = Destination.Position - new Vector3(0, 0.5f, 0);
            var delta = dest - Platform.LocalPosition;
            if (delta.LengthSquared() < 0.05f)
                return true;
            delta.Normalize();
            delta *= (float)Time.ElapsedGameTime.TotalSeconds;

            Platform.LocalPosition = Platform.LocalPosition + delta;
            if (Rider != null)
            {
                Rider.Physics.LocalPosition = Platform.LocalPosition + new Vector3(0, Rider.Physics.BoundingBoxSize.Y, 0);
                Rider.Physics.Velocity = Vector3.Zero;
                Rider.Physics.PropogateTransforms();
            }

            return false;
        }
    }
}
