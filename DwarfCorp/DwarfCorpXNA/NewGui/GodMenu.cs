using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class GodMenu : HorizontalMenuTray.Tray
    {
        public GameMaster Master;

        private void ActivateGodTool(String Command)
        {
            (Master.Tools[GameMaster.ToolMode.God] as GodModeTool).Command = Command;
            Master.ChangeTool(GameMaster.ToolMode.God);
        }

        public override void Construct()
        {
            IsRootTray = true;

            ItemSource = new Gum.Widget[]
            {
                new HorizontalMenuTray.MenuItem
                {
                    Text = "BUILD",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        ItemSource = RoomLibrary.GetRoomTypes().Select(r =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = r,
                                OnClick = (sender, args) => ActivateGodTool("Build/" + r)
                            })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "SPAWN",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 5,
                        ItemSource = EntityFactory.EntityFuncs.Keys.OrderBy(s => s).Select(s =>
                            new HorizontalMenuTray.MenuItem
                            {
                                Text = s,
                                OnClick = (sender, args) => ActivateGodTool("Spawn/" + s)
                            })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "PLACE",
                    ExpansionChild = new HorizontalMenuTray.Tray
                    {
                        Columns = 3,
                        ItemSource = VoxelLibrary.PrimitiveMap.Keys
                            .Where(t => t.Name != "empty" && t.Name != "water")
                            .OrderBy(s => s.Name)
                            .Select(s =>
                                new HorizontalMenuTray.MenuItem
                                {
                                    Text = s.Name,
                                    OnClick = (sender, args) => ActivateGodTool("Place/" + s.Name)
                                })
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "DELETE BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Delete Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL BLOCK",
                    OnClick = (sender, args) => ActivateGodTool("Kill Block")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "KILL THINGS",
                    OnClick = (sender, args) => ActivateGodTool("Kill Things")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL WATER",
                    OnClick = (sender, args) => ActivateGodTool("Fill Water")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FILL LAVA",
                    OnClick = (sender, args) => ActivateGodTool("Fill Lava")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "FIRE",
                    OnClick = (sender, args) => ActivateGodTool("Fire")
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "TRADE ENVOY",
                    OnClick = (sender, args) =>
                    {
                        var factionToSend = Datastructures.SelectRandom(
                            Master.World.ComponentManager.Factions.Factions.Values.Where(f =>
                            f.Race.IsIntelligent && f.Race.IsNative));
                        if (factionToSend != null)
                            Master.World.ComponentManager.Diplomacy.SendTradeEnvoy(factionToSend, Master.World);
                    }
                },

                new HorizontalMenuTray.MenuItem
                {
                    Text = "DWARF BUX",
                    OnClick = (sender, args) =>
                    {
                        Master.World.PlayerCompany.Assets += 1000.0f;
                    }
                },

                // Shouldn't this go into some kind of 'debug' menu?
                new HorizontalMenuTray.MenuItem
                {
                    Text = "DRAW PATHS",
                    OnClick = (sender, args) =>
                    {
                        GameSettings.Default.DrawPaths = !GameSettings.Default.DrawPaths;
                    }
                }
            };

            base.Construct();
        }        
    }
}
