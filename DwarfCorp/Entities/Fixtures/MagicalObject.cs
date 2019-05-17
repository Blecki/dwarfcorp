using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MagicalObject : GameComponent
    {
        public int MaxCharges = 10;

        [JsonProperty]
        private int _currentCharges = 10;

        private Timer DrawLoadBarTimer = new Timer(0.5f, true);

        [JsonIgnore]
        public int CurrentCharges
        {
            get
            {
                return _currentCharges;
            }
            set
            {
                var orig = _currentCharges;
                _currentCharges = MathFunctions.Clamp(value, 0, MaxCharges);
                if (_currentCharges != orig)
                {
                    DrawLoadBarTimer.Reset();
                }
                if (orig > 0 && _currentCharges == 0)
                {
                    if (Active)
                    {
                        NotifyRecharge();
                    }

                    Activate(false);
                }
                else if (orig == 0 && _currentCharges > 0)
                {
                    Activate(true);
                }
            }
        }

        public MagicalObject()
        {
        }


        public MagicalObject(ComponentManager Manager) :
            base(Manager)
        {
            Name = "MagicalObject";
            DrawLoadBarTimer = new Timer(0.5f, true);
            DrawLoadBarTimer.HasTriggered = true;
        }

        public void NotifyRecharge()
        {
            if (World.PlayerFaction.OwnedObjects.Contains(this.GetRoot()))
                World.TaskManager.AddTask(new RechargeObjectTask(this));
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            DrawLoadBarTimer.Update(Time);
            if (!DrawLoadBarTimer.HasTriggered)
                Drawer2D.DrawLoadBar(World.Renderer.Camera, (GetRoot() as GameComponent).Position, Color.Cyan, Color.Black, 32, 4, (float)(_currentCharges) / MaxCharges);
            base.Update(Time, Chunks, Camera);
        }

        public void Activate(bool active)
        {
            if (active != Active)
            {
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research, (GetRoot() as GameComponent).Position, true, 1.0f);
            }
            GetRoot().SetFlagRecursive(GameComponent.Flag.Active, active);
            GetRoot().SetVertexColorRecursive(active ? Color.White : Color.DarkGray);
        }
    }
}
