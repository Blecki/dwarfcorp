/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Goals
{
    public class BuildStockpile : Goal
    {
        public BuildStockpile()
        {
            Name = "Build a stockpile";
            Description = "You built an additional stockpile.";
            GoalType = GoalTypes.Achievement;
        }

        public override void OnGameEvent(WorldManager World, Trigger Event)
        {
            var builtRoomEvent = Event as Triggers.BuiltRoom;
            if (builtRoomEvent != null && builtRoomEvent.RoomType.StartsWith("Stockpile"))
            {
                World.PlayerFaction.AddMoney(20m);
                World.MakeAnnouncement(String.Format("{0} awarded for building a stockpile.", new DwarfBux(20m)));
                State = GoalState.Complete;
            }
        }
    }

    public class BuildBalloonPort : Goal
    {
        public BuildBalloonPort()
        {
            Name = "Build a balloon port";
            Description = "You built an additional balloon port.";
            GoalType = GoalTypes.Achievement;
        }

        public override void OnGameEvent(WorldManager World, Trigger Event)
        {
            var builtRoomEvent = Event as Triggers.BuiltRoom;
            if (builtRoomEvent != null && builtRoomEvent.RoomType.StartsWith("BalloonPort"))
            {
                World.PlayerFaction.AddMoney(20m);
                World.MakeAnnouncement(String.Format("{0} awarded for building a balloon port.", new DwarfBux(20m)));
                State = GoalState.Complete;
            }
        }
    }

    public class BuildBedRoom : Goal
    {
        public BuildBedRoom()
        {
            Name = "Build a bedroom";
            Description = "You built a bedroom.";
            GoalType = GoalTypes.Achievement;
        }

        public override void OnGameEvent(WorldManager World, Trigger Event)
        {
            var builtRoomEvent = Event as Triggers.BuiltRoom;
            if (builtRoomEvent != null && builtRoomEvent.RoomType.StartsWith("BedRoom"))
            {
                World.PlayerFaction.AddMoney(20m);
                World.MakeAnnouncement(String.Format("{0} awarded for building a bed room.", new DwarfBux(20m)));
                State = GoalState.Complete;
            }
        }
    }

    public class BuildCommonRoom : Goal
    {
        public BuildCommonRoom()
        {
            Name = "Build a common room";
            Description = "You built a common room.";
            GoalType = GoalTypes.Achievement;
        }

        public override void OnGameEvent(WorldManager World, Trigger Event)
        {
            var builtRoomEvent = Event as Triggers.BuiltRoom;
            if (builtRoomEvent != null && builtRoomEvent.RoomType.StartsWith("CommonRoom"))
            {
                World.PlayerFaction.AddMoney(20m);
                World.MakeAnnouncement(String.Format("{0} awarded for building a common room.", new DwarfBux(20m)));
                State = GoalState.Complete;
            }
        }
    }
}
*/