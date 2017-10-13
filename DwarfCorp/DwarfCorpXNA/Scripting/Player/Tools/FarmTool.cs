// BuildTool.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FarmTool : PlayerTool
    {
        public string PlantType { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; } 
        public enum FarmMode
        {
            Harvesting,
            WranglingAnimals
        }

        public FarmMode Mode { get; set; }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
        
        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
        
        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            switch (Mode)
            {
                case FarmMode.Harvesting:
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (Body tree in bodies.Where(c => c.Tags.Contains("Vegetation")))
                        {
                            if (!tree.IsVisible || tree.IsAboveCullPlane(Player.World.ChunkManager)) continue;

                            switch (button)
                            {
                                case InputManager.MouseButton.Left:
                                    if (Player.Faction.AddEntityDesignation(tree, DesignationType.Chop) == Faction.AddEntityDesignationResult.Added)
                                    {
                                        tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
                                        this.Player.World.ShowToolPopup("Will harvest this " + tree.Name);
                                    }
                                    break;
                                case InputManager.MouseButton.Right:
                                    if (Player.Faction.RemoveEntityDesignation(tree, DesignationType.Chop) == Faction.RemoveEntityDesignationResult.Removed)
                                        this.Player.World.ShowToolPopup("Harvest cancelled " + tree.Name);
                                    break;
                            }
                        }
                        if (tasks.Count > 0 && Player.SelectedMinions.Count > 0)
                        {
                            TaskManager.AssignTasks(tasks, Player.SelectedMinions);
                            OnConfirm(Player.SelectedMinions);
                        }
                    }
                    break;
                case FarmMode.WranglingAnimals:
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (Body animal in bodies.Where(c => c.Tags.Contains("DomesticAnimal")))
                        {
                            Drawer3D.DrawBox(animal.BoundingBox, Color.Tomato, 0.1f, false);
                            switch (button)
                            {
                                case InputManager.MouseButton.Left:
                                    {
                                        var pens = Player.Faction.GetRooms().Where(room => room is AnimalPen).Cast<AnimalPen>().Where(pen => pen.IsBuilt && 
                                        (pen.Species == "" || pen.Species == animal.GetRoot().GetComponent<Creature>().Species));

                                        if (pens.Any())
                                        {
                                            Player.Faction.AddEntityDesignation(animal, DesignationType.Wrangle);
                                            tasks.Add(new WrangleAnimalTask(animal.GetRoot().GetComponent<Creature>()));
                                            this.Player.World.ShowToolPopup("Will wrangle this " +
                                                                            animal.GetRoot().GetComponent<Creature>().Species);
                                        }
                                        else
                                        {
                                            this.Player.World.ShowToolPopup("Can't wrangle this " +
                                                                            animal.GetRoot().GetComponent<Creature>().Species +
                                                                            " : need more animal pens.");
                                        }
                                    }
                                    break;
                                case InputManager.MouseButton.Right:
                                    if (Player.Faction.RemoveEntityDesignation(animal, DesignationType.Wrangle) == Faction.RemoveEntityDesignationResult.Removed)
                                        this.Player.World.ShowToolPopup("Wrangle cancelled for " + animal.GetRoot().GetComponent<Creature>().Species);
                                    break;
                            }
                        }
                        if (tasks.Count > 0 && Player.SelectedMinions.Count > 0)
                        {
                            TaskManager.AssignTasks(tasks, Player.SelectedMinions);
                            OnConfirm(Player.SelectedMinions);
                        }
                    }
                    break;
            }
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }


        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            //FarmPanel.TweenOut(Drawer2D.Alignment.Right, 0.25f);
            foreach (FarmTile tile in Player.Faction.FarmTiles)
            {
                Drawer3D.UnHighlightVoxel(tile.Voxel);
            }
            Player.VoxSelector.Clear();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.BodySelector.AllowRightClickSelection = true;

            switch (Mode)
            {
                case FarmMode.Harvesting:
                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
                    break;
                case FarmMode.WranglingAnimals:
                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
                    break;
            }

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }

        public KillEntityTask AutoFarm()
        {
            return (from tile in Player.Faction.FarmTiles where tile.PlantExists() && tile.Plant.IsGrown && !tile.IsCanceled select new KillEntityTask(tile.Plant, KillEntityTask.KillType.Chop)).FirstOrDefault();
        }
    }
}
