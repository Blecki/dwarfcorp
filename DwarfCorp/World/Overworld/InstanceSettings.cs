using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace DwarfCorp
{
    public enum LoadType
    {
        CreateNew,
        LoadFromFile
    }

    public class InstanceSettings
    {
        public LoadType LoadType = LoadType.CreateNew;

        [JsonIgnore] public Embarkment InitalEmbarkment = null;
        [JsonIgnore] public Vector2 Origin => new Vector2(Cell.Bounds.X, Cell.Bounds.Y);

        public ColonyCell Cell = null;

        public InstanceSettings()
        {
            int x = 5;
        }

        public InstanceSettings(ColonyCell Cell, GameStates.Overworld Overworld)
        {
            this.Cell = Cell;
            InitalEmbarkment = new Embarkment(Overworld);
        }

        public DwarfBux CalculateLandValue()
        {
            return Cell.Bounds.Width * Cell.Bounds.Height * GameSettings.Current.LandCost;
        }
        
        public DwarfBux TotalCreationCost()
        {
            return CalculateLandValue() + InitalEmbarkment.TotalCost();
        }

        public enum ValidationResult
        {
            Reject,
            Query,
            Pass
        }

        public static ValidationResult ValidateEmbarkment(GameStates.Overworld Settings, out String Message)
        {
            if (Settings.InstanceSettings.TotalCreationCost() > Settings.PlayerCorporationFunds)
            {
                Message = "You do not have enough funds.";
                return ValidationResult.Reject;
            }

            return Embarkment.ValidateEmbarkment(Settings, out Message);
        }
    }
}
