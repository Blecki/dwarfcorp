using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class GameCycle
    {
        public delegate void CycleChanged(OrderCylce cycle);

        public event CycleChanged OnCycleChanged;

        public enum OrderCylce
        {
            BalloonAtMotherland,
            BalloonAtColony,
            WaitingForMotherland,
            WaitingForColony
        }

        public OrderCylce CurrentCycle { get; set; }
        public Dictionary<OrderCylce, Timer> CycleTimers { get; set; }


        public string GetStatusString(OrderCylce cycle)
        {
            switch(cycle)
            {
                case OrderCylce.WaitingForMotherland:
                    return "Going to Motherland";

                case OrderCylce.WaitingForColony:
                    return "Coming to Colony";

                case OrderCylce.BalloonAtColony:
                    return "At Colony (Click to order!)";

                case OrderCylce.BalloonAtMotherland:
                    return "At Motherland (Click to order!)";
            }

            return "";
        }

        public GameCycle()
        {
            CycleTimers = new Dictionary<OrderCylce, Timer>();
            CurrentCycle = OrderCylce.WaitingForMotherland;
            CycleTimers[OrderCylce.BalloonAtColony] = new Timer(60, true);
            CycleTimers[OrderCylce.BalloonAtMotherland] = new Timer(60, true);
            CycleTimers[OrderCylce.WaitingForMotherland] = new Timer(60, true);
            CycleTimers[OrderCylce.WaitingForColony] = new Timer(60, true);
            OnCycleChanged += GameCycle_OnCycleChanged;
        }

        private void GameCycle_OnCycleChanged(GameCycle.OrderCylce cycle)
        {
        }

        public Color GetColor(GameCycle.OrderCylce cylce, double t)
        {
            float x = (float) Math.Sin(t * 2.0f) * 0.5f + 0.5f;
            switch(cylce)
            {
                case OrderCylce.WaitingForMotherland:
                case OrderCylce.WaitingForColony:
                    return new Color(1.0f - x * 0.5f, 1.0f - x * 0.5f, 1.0f - x * 0.5f, 255);
                case OrderCylce.BalloonAtColony:
                case OrderCylce.BalloonAtMotherland:
                    return new Color(1.0f - x * 0.5f, 1.0f - x * 0.5f, 0.0f, 255);
            }

            return Color.White;
        }

        public OrderCylce GetNextCycle()
        {
            switch(CurrentCycle)
            {
                case OrderCylce.WaitingForColony:
                    return OrderCylce.BalloonAtColony;
                case OrderCylce.BalloonAtColony:
                    return OrderCylce.WaitingForMotherland;
                case OrderCylce.WaitingForMotherland:
                    return OrderCylce.BalloonAtMotherland;
                case OrderCylce.BalloonAtMotherland:
                    return OrderCylce.WaitingForColony;
            }

            return OrderCylce.WaitingForColony;
        }

        public void Update(GameTime time)
        {
            CycleTimers[CurrentCycle].Update(time);
            if(CycleTimers[CurrentCycle].HasTriggered)
            {
                CycleTimers[CurrentCycle].Reset(CycleTimers[CurrentCycle].TargetTimeSeconds);
                CurrentCycle = GetNextCycle();
                OnCycleChanged.Invoke(CurrentCycle);
            }
        }
    }

}