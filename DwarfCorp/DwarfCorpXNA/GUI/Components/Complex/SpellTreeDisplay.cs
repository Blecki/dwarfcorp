// SpellTreeDisplay.cs
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
using System.Net.Mime;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class SpellTreeDisplay : GUIComponent
    {
        public SpellTree Tree { get; set; }
        public List<SpellButton> SpellButtons { get; set; }
        public ScrollView Scroller { get; set; }
        public GUIComponent MainPanel { get; set; }
        public delegate void SpellButtonClicked(SpellButton button);

        public event SpellButtonClicked OnSpellButtonClicked;

        public class SpellButton
        {
            public int Mod { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public Button ImageButton { get; set; }
            public SpellTree.Node Spell { get; set; }
            public List<SpellButton> Children { get; set; }
            public int Level { get; set; }
            public SpellButton Parent { get; set; }

            public SpellButton()
            {
            }

            public bool IsLeaf()
            {
                return this.Children.Count == 0;
            }

            public bool IsLeftMost()
            {
                if (this.Parent == null)
                    return true;

                return this.Parent.Children[0] == this;
            }

            public bool IsRightMost()
            {
                if (this.Parent == null)
                    return true;

                return this.Parent.Children[this.Parent.Children.Count - 1] == this;
            }

            public SpellButton GetPreviousSibling()
            {
                if (this.Parent == null || this.IsLeftMost())
                    return null;

                return this.Parent.Children[this.Parent.Children.IndexOf(this) - 1];
            }

            public SpellButton GetNextSibling()
            {
                if (this.Parent == null || this.IsRightMost())
                    return null;

                return this.Parent.Children[this.Parent.Children.IndexOf(this) + 1];
            }

            public SpellButton GetLeftMostSibling()
            {
                if (this.Parent == null)
                    return null;

                if (this.IsLeftMost())
                    return this;

                return this.Parent.Children[0];
            }

            public SpellButton GetLeftMostChild()
            {
                if (this.Children.Count == 0)
                    return null;

                return this.Children[0];
            }

            public SpellButton GetRightMostChild()
            {
                if (this.Children.Count == 0)
                    return null;

                return this.Children[Children.Count - 1];
            }

            public void Update()
            {
                bool parentResearched = Spell.Parent == null || Spell.Parent.ResearchProgress >= Spell.ResearchTime;
                bool isResearched = Spell.ResearchProgress >= Spell.ResearchTime;

                if (isResearched)
                {
                    ImageButton.TextColor = Color.Black;
                    ImageButton.Transparency = 255;
                }
                else if (parentResearched)
                {
                    ImageButton.TextColor = Color.Gray;
                    ImageButton.Transparency = 255;
                }
                else
                {
                    ImageButton.Transparency = 128;
                }
            }

            public void Render(DwarfTime time, SpriteBatch batch)
            {
                bool isResearched = Spell.IsResearched;
                bool parentResearched = Spell.Parent == null || Spell.Parent.IsResearched;

                if (!isResearched && parentResearched)
                {
                    float progress = Spell.ResearchProgress/Spell.ResearchTime;
                    Drawer2D.FillRect(batch, new Rectangle(ImageButton.GlobalBounds.X, ImageButton.GlobalBounds.Y - 12, (int)(ImageButton.GlobalBounds.Width * progress), 10), Color.Cyan);
                    Drawer2D.DrawRect(batch, new Rectangle(ImageButton.GlobalBounds.X, ImageButton.GlobalBounds.Y - 12, ImageButton.GlobalBounds.Width, 10), Color.Black, 1);
                }

                Vector2 line1 = new Vector2(ImageButton.GlobalBounds.X + ImageButton.GlobalBounds.Width, ImageButton.GlobalBounds.Y + ImageButton.GlobalBounds.Height / 2);
                foreach (SpellButton child in Children)
                {
                    Color drawColor = Color.DarkGray;
                    if (isResearched)
                    {
                        drawColor = Color.DarkCyan;
                    }

                    Vector2 line2 = new Vector2(child.ImageButton.GlobalBounds.X, child.ImageButton.GlobalBounds.Y + child.ImageButton.GlobalBounds.Height / 2);

                    Drawer2D.DrawLine(batch, line1, line2, drawColor, 4);
                }   
            }

            public void CountLevelsRecursive(Dictionary<int, List<SpellButton>> levels)
            {
                if (levels.ContainsKey(Level))
                {
                    levels[Level].Add(this);
                }
                else
                {
                    levels.Add(Level, new List<SpellButton>(){this});
                }

                foreach (SpellButton child in Children)
                {
                    child.CountLevelsRecursive(levels);
                }
            }
         
        }

        public SpellTreeDisplay()
        {
            
        }

        private GameMaster Master { get; set; }

        public SpellTreeDisplay(DwarfGUI gui, GUIComponent parent, SpellTree tree, GameMaster master) : base(gui, parent)
        {
            Master = master;
            Tree = tree;

            Scroller = new ScrollView(gui, this)
            {
                HeightSizeMode = SizeMode.Fit,
                WidthSizeMode = SizeMode.Fit
            };

            MainPanel = new GUIComponent(gui, Scroller)
            {
                HeightSizeMode = SizeMode.Fixed,
                WidthSizeMode = SizeMode.Fixed
            };

            InitializeSpellButtons();
        }

    
        public void CalculateNodePositions(SpellButton rootNode, int nodeSize, int siblingDistance, int treeDistance)
        {
            // initialize node x, y, and mod values
            InitializeNodes(rootNode, 0);

            // assign initial X and Mod values for nodes
            CalculateInitialX(rootNode, 1, 1, 1);

            // ensure no node is being drawn off screen
            CheckAllChildrenOnScreen(rootNode);

            // assign final X values to nodes
            CalculateFinalPositions(rootNode, 0);

            UpdateRects(40, 75);
        }

        public void UpdateRects(int rowHeight, int columnWidth)
        {
            List<Rectangle> boundingRects = new List<Rectangle>();
            foreach (SpellButton button in SpellButtons)
            {
                button.ImageButton.LocalBounds = new Rectangle((button.Y - 1) * columnWidth, (button.X + 1) * rowHeight, button.ImageButton.LocalBounds.Width, button.ImageButton.LocalBounds.Height);
                boundingRects.Add(button.ImageButton.LocalBounds);
            }
            MainPanel.LocalBounds = MathFunctions.GetBoundingRectangle(boundingRects);
        }
        
        // recusrively initialize x, y, and mod values of nodes
        private void InitializeNodes(SpellButton node, int depth)
        {
            node.X = -1;
            node.Y = depth;
            node.Mod = 0;

            foreach (var child in node.Children)
                InitializeNodes(child, depth + 1);
        }

        private void CalculateFinalPositions(SpellButton node, int modSum)
        {
            node.X += modSum;
            modSum += node.Mod;

            foreach (var child in node.Children)
                CalculateFinalPositions(child, modSum);

            if (node.Children.Count == 0)
            {
                node.Width = node.X;
                node.Height = node.Y;
            }
            else
            {
                node.Width = node.Children.OrderByDescending(p => p.Width).First().Width;
                node.Height = node.Children.OrderByDescending(p => p.Height).First().Height;
            }
        }

        private void CalculateInitialX(SpellButton node, int nodeSize, int siblingDistance, int treeDistance)
        {
            foreach (var child in node.Children)
                CalculateInitialX(child, nodeSize, siblingDistance, treeDistance);
 
            // if no children
            if (node.IsLeaf())
            {
                // if there is a previous sibling in this set, set X to prevous sibling + designated distance
                if (!node.IsLeftMost())
                    node.X = node.GetPreviousSibling().X + nodeSize + siblingDistance;
                else
                    // if this is the first node in a set, set X to 0
                    node.X = 0;
            }
            // if there is only one child
            else if (node.Children.Count == 1)
            {
                // if this is the first node in a set, set it's X value equal to it's child's X value
                if (node.IsLeftMost())
                {
                    node.X = node.Children[0].X;
                }
                else
                {
                    node.X = node.GetPreviousSibling().X + nodeSize + siblingDistance;
                    node.Mod = node.X - node.Children[0].X;
                } 
            }
            else
            {
                var leftChild = node.GetLeftMostChild();
                var rightChild = node.GetRightMostChild();
                var mid = (leftChild.X + rightChild.X) / 2;
 
                if (node.IsLeftMost())
                {
                    node.X = mid;
                }
                else
                {
                    node.X = node.GetPreviousSibling().X + nodeSize + siblingDistance;
                    node.Mod = node.X - mid;
                }
            }
            
            if (node.Children.Count > 0 && !node.IsLeftMost())
            {
                // Since subtrees can overlap, check for conflicts and shift tree right if needed
                CheckForConflicts(node, treeDistance, nodeSize);
            }
 
        }

        private void CheckForConflicts(SpellButton node, int treeDistance, int nodeSize)
        {
            var minDistance = treeDistance + nodeSize;
            var shiftValue = 0F;
 
            var nodeContour = new Dictionary<int, float>();
            GetLeftContour(node, 0, ref nodeContour);
 
            var sibling = node.GetLeftMostSibling();
            while (sibling != null && sibling != node)
            {
                var siblingContour = new Dictionary<int, float>();
                GetRightContour(sibling, 0, ref siblingContour);
 
                for (int level = node.Y + 1; level <= Math.Min(siblingContour.Keys.Max(), nodeContour.Keys.Max()); level++)
                {
                    var distance = nodeContour[level] - siblingContour[level];
                    if (distance + shiftValue < minDistance)
                    {
                        shiftValue = minDistance - distance;
                    }
                }
 
                if (shiftValue > 0)
                {
                    node.X += (int)shiftValue;
                    node.Mod += (int)shiftValue;

                    CenterNodesBetween(node, sibling, treeDistance, nodeSize);

                    shiftValue = 0;
                }
 
                sibling = sibling.GetNextSibling();
            }
        }

        private void CenterNodesBetween(SpellButton leftNode, SpellButton rightNode, int treeDistance, int nodeSize)
        {
            var leftIndex = leftNode.Parent.Children.IndexOf(rightNode);
            var rightIndex = leftNode.Parent.Children.IndexOf(leftNode);
                    
            var numNodesBetween = (rightIndex - leftIndex) - 1;

            if (numNodesBetween > 0)
            {
                var distanceBetweenNodes = (leftNode.X - rightNode.X) / (numNodesBetween + 1);

                int count = 1;
                for (int i = leftIndex + 1; i < rightIndex; i++)
                {
                    var middleNode = leftNode.Parent.Children[i];

                    var desiredX = rightNode.X + (distanceBetweenNodes * count);
                    var offset = desiredX - middleNode.X;
                    middleNode.X += offset;
                    middleNode.Mod += offset;

                    count++;
                }

                CheckForConflicts(leftNode, treeDistance, nodeSize);
            }
        }
 
        private void CheckAllChildrenOnScreen(SpellButton node)
        {
            var nodeContour = new Dictionary<int, float>();
            GetLeftContour(node, 0, ref nodeContour);

            int shiftAmount = 0;
            foreach (var y in nodeContour.Keys)
            {
                if (nodeContour[y] + shiftAmount < 0)
                    shiftAmount = (int)(nodeContour[y] * -1);
            }

            if (shiftAmount > 0)
            {
                node.X += shiftAmount;
                node.Mod += shiftAmount;
            }
        }
 
        private void GetLeftContour(SpellButton node, float modSum, ref Dictionary<int, float> values)
        {
            if (!values.ContainsKey(node.Y))
                values.Add(node.Y, node.X + modSum);
            else
                values[node.Y] = Math.Min(values[node.Y], node.X + modSum);
 
            modSum += node.Mod;
            foreach (var child in node.Children)
            {
                GetLeftContour(child, modSum, ref values);
            }
        }
 
        private void GetRightContour(SpellButton node, float modSum, ref Dictionary<int, float> values)
        {
            if (!values.ContainsKey(node.Y))
                values.Add(node.Y, node.X + modSum);
            else
                values[node.Y] = Math.Max(values[node.Y], node.X + modSum);
 
            modSum += node.Mod;
            foreach (var child in node.Children)
            {
                GetRightContour(child, modSum, ref values);
            }
        }

        public void InitializeSpellButtons()
        {
            SpellButtons = new List<SpellButton>();

            int xOffset = 15;
            int yOffset = 32;
            SpellButton fakeButton = new SpellButton()
            {
                Children = new List<SpellButton>()
            };
            foreach (SpellTree.Node spell in Tree.RootSpells)
            {
                SpellButton newButton = new SpellButton()
                {
                    Spell = spell,
                    ImageButton = new Button(GUI, MainPanel, spell.Spell.Name, GUI.SmallFont, Button.ButtonMode.ImageButton, spell.Spell.Image)
                    {
                        ToolTip = spell.Spell.Description,
                        KeepAspectRatio = true,
                        DontMakeSmaller = true,
                        DontMakeBigger = true,
                        LocalBounds = new Rectangle(xOffset, yOffset, 0, 0),
                    },
                    Children = new List<SpellButton>(),
                    Level = 0,
                    Parent = fakeButton

                };
                SpellTree.Node rSpell = spell;
                newButton.ImageButton.OnClicked += () => ImageButton_OnClicked(rSpell);
                SpellButtons.Add(newButton);
                InitializeSpellButtonRecursive(newButton);
                fakeButton.Children.Add(newButton);
            }

            //Rectangle newRect = AlignImagesTree(fakeButton, xOffset, yOffset);
            //xOffset = newRect.Left;
            //yOffset = newRect.Bottom;
            //MainPanel.LocalBounds = new Rectangle(0, 0, Math.Max(MainPanel.LocalBounds.Width, newRect.Width + xOffset), Math.Max(MainPanel.LocalBounds.Height, newRect.Height + yOffset));
            CalculateNodePositions(fakeButton, 48, 10, 100);

        }

        private void ImageButton_OnClicked(SpellTree.Node spell)
        {
            if (spell.IsResearched || (spell.Parent != null &&  !spell.Parent.IsResearched)) return;
            else
            {
                List<CreatureAI> wizards = Faction.FilterMinionsWithCapability(Master.SelectedMinions, GameMaster.ToolMode.Magic);

                foreach (CreatureAI wizard in wizards)
                {
                    wizard.Tasks.Add(new ActWrapperTask(new GoResearchSpellAct(wizard, spell))
                    {
                        Priority = Task.PriorityType.Low
                    });
                }
            }
        }

        public Rectangle AlignImagesTree(SpellButton root, int xOffset, int yOffset)
        {
            Dictionary<int, List<SpellButton> > levels = new Dictionary<int, List<SpellButton>>();
            root.CountLevelsRecursive(levels);
            int rowHeight = 80;
            int colWidth = 120;
            int maxLevel = 1;
            foreach (var level in levels)
            {
                maxLevel = Math.Max(maxLevel, level.Value.Count);
            }

            int height = maxLevel * rowHeight;
            int width = colWidth*levels.Count;
            int x = xOffset;
            
            foreach (var level in levels)
            {
                int y = yOffset + (maxLevel - level.Value.Count) * rowHeight / 2;
                foreach (SpellButton button in level.Value)
                {
                    if (button.ImageButton != null)
                    {
                        button.ImageButton.LocalBounds = new Rectangle(x, y, button.ImageButton.LocalBounds.Width,
                            button.ImageButton.LocalBounds.Height);
                        y += rowHeight;
                    }
                }
                x += colWidth;
            }

            for (int i = levels.Values.Count - 2; i >= 0; i--)
            {
                List<SpellButton> level = levels.Values.ElementAt(i);
                //List<SpellButton> childless = new List<SpellButton>();
                int maxY = 0;
                foreach (SpellButton button in level)
                {
                    if (button.ImageButton == null) continue;
                    int avg = 0;

                    foreach (SpellButton child in button.Children)
                    {
                        avg += child.ImageButton.LocalBounds.Y;
                    }

                    

                    if (avg > 0)
                    {
                        avg /= button.Children.Count;
                        button.ImageButton.LocalBounds = new Rectangle(button.ImageButton.LocalBounds.X, avg, button.ImageButton.LocalBounds.Width, button.ImageButton.LocalBounds.Height);
                        maxY = Math.Max(maxY, avg);
                    }
                    else
                    {
                        button.ImageButton.LocalBounds = new Rectangle(button.ImageButton.LocalBounds.X, maxY + rowHeight, button.ImageButton.LocalBounds.Width, button.ImageButton.LocalBounds.Height);
                        maxY += rowHeight;
                    }
                }

                /*
                foreach (SpellButton button in childless)
                {
                    button.ImageButton.LocalBounds =  new Rectangle(button.ImageButton.LocalBounds.X, maxY + rowHeight, button.ImageButton.LocalBounds.Width, button.ImageButton.LocalBounds.Height);
                    maxY += rowHeight;
                }
                 */
            }

            return  new Rectangle(xOffset, yOffset, width, height);

        }

       

        public void InitializeSpellButtonRecursive(SpellButton button)
        {
            foreach (SpellTree.Node spell in button.Spell.Children)
            {
                SpellButton newButton = new SpellButton()
                {
                    Spell = spell,
                    ImageButton = new Button(GUI, MainPanel, spell.Spell.Name, GUI.SmallFont, Button.ButtonMode.ImageButton, spell.Spell.Image)
                    {
                        ToolTip = spell.Spell.Description,
                        KeepAspectRatio = true,
                        DontMakeSmaller = true,
                        DontMakeBigger = true
                    },
                    Level = button.Level +1,
                    Children = new List<SpellButton>(),
                    Parent = button
                };
                SpellTree.Node rSpell = spell;
                newButton.ImageButton.OnClicked += () => ImageButton_OnClicked(rSpell);
                button.Children.Add(newButton);
                SpellButtons.Add(newButton);
                InitializeSpellButtonRecursive(newButton);
            }
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (!IsVisible) return;
            Rectangle originalRect = Scroller.StartClip(batch);
            foreach (SpellButton button in SpellButtons)
            {
                button.Render(time, batch);
            }
            Scroller.EndClip(originalRect, batch);
            base.Render(time, batch);
        }

        public override void Update(DwarfTime time)
        {
            foreach (SpellButton button in SpellButtons)
            {
                button.Update();
            }
            base.Update(time);
        }

    }
}
