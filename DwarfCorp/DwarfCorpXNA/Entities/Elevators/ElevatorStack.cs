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

        public void Update(GameTime Time)
        {
            // Todo: State machine.
            // Time out when rider doesn't enter fast enough. 
            // Time out and forget a rider when they don't exit fast enough.
        }
    }
}
