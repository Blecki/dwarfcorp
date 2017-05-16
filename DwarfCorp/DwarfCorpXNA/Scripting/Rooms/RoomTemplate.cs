// RoomTemplate.cs
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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    [Flags]
    public enum RoomTile
    {
        None = 0,
        Pillow = 1 << 0,
        Bed = 1 << 1,
        Table = 1 << 2,
        Lamp = 1 << 3,
        Barrel = 1 << 4,
        Wall = 1 << 5,
        Open = 1 << 6,
        Edge = 1 << 7,
        Chair = 1 << 8,
        Flag = 1 << 9,
        Anvil = 1 << 10,
        Forge = 1 << 11,
        Books = 1 << 12,
        Target = 1 << 13,
        Strawman = 1 << 14,
        Wheat = 1 << 15,
        Mushroom = 1 << 16,
        BookShelf = 1 << 17,
        KitchenTable = 1 << 18,
        Stove = 1 << 19
    }

    public enum PlacementType
    {
        Random,
        All
    }


    /// <summary>
    /// Describes how a BuildRoom should be populated with items. A template has a number of "required" items,
    /// and "accessory" items. Templates will fill up a BuildRoom until no more can be placed. Templates can be
    /// rotated to fill more space.
    /// </summary>
    public class RoomTemplate
    {
        public RoomTile[,] Template { get; set; }
        public RoomTile[,] Accessories { get; set; }

        public PlacementType PlacementType { get; set; }
        public float Rotation { get; set; }
        public bool CanRotate { get; set; }
        public float Probability { get; set; }

        public void RotateClockwise(int numRotations)
        {
            for(int i = 0; i < numRotations; i++)
            {
                Template = Datastructures.RotateClockwise(Template);
                Accessories = Datastructures.RotateClockwise(Accessories);
                Rotation -= (float)Math.PI*0.5f;
            }
        }

        public RoomTemplate(RoomTemplate other)
        {
            Template = new RoomTile[other.Template.GetLength(0), other.Template.GetLength(1)];

            for (int x = 0; x < other.Template.GetLength(0); x++)
            {
                for (int y = 0; y < other.Template.GetLength(1); y++)
                {
                    Template[x, y] = other.Template[x, y];
                }
            }

            Accessories = new RoomTile[other.Accessories.GetLength(0), other.Accessories.GetLength(1)];

            for (int x = 0; x < other.Accessories.GetLength(0); x++)
            {
                for (int y = 0; y < other.Accessories.GetLength(1); y++)
                {
                    Accessories[x, y] = other.Accessories[x, y];
                }
            }
            
            PlacementType = other.PlacementType;
            Rotation = other.Rotation;
            CanRotate = other.CanRotate;
            Probability = other.Probability;
        }

        public RoomTemplate()
        {
            Rotation = 0.0f;
            Probability = 1.0f;
        }

        public RoomTemplate(PlacementType type, RoomTile[,] template, RoomTile[,] accessories)
        {
            PlacementType = type;
            Template = template;
            Accessories = accessories;
            CanRotate = true;
            Probability = 1.0f;
            Rotation = 0.0f;
        }

        public RoomTemplate(int sx, int sy)
        {
            Template = new RoomTile[sx, sy];
            Accessories = new RoomTile[sx, sy];

            for(int x = 0; x < sx; x++)
            {
                for(int y = 0; y < sy; y++)
                {
                    Template[x, y] = RoomTile.None;
                    Accessories[x, y] = RoomTile.None;
                }
            }
            Probability = 1.0f;
        }

        public int PlaceTemplate(ref RoomTile[,] room, ref float[,] rotations, int seedR, int seedC)
        {
            int nr = room.GetLength(0);
            int nc = room.GetLength(1);
            int tr = Template.GetLength(0);
            int tc = Template.GetLength(1);

            for(int r = 0; r < tr; r++)
            {
                for(int c = 0; c < tc; c++)
                {
                    int x = seedR + r;
                    int y = seedC + c;

                    RoomTile desired = Template[r, c];


                    // Ignore tiles with unspecified conditions
                    if(desired == RoomTile.None)
                    {
                        continue;
                    }

                    bool hasWall = Has(desired, RoomTile.Wall);
                    bool hasOpen = Has(desired, RoomTile.Open);
                    bool hasEdge = Has(desired, RoomTile.Edge);
                    bool onEdge = (x >= nr - 1 || y >= nc - 1 || x < 1 || y < 1);
                    bool outOfBounds = onEdge && (x >= nr  || y >= nc  || x < 0 || y < 0);
                    if(onEdge && !hasEdge)
                    {
                        return -1;
                    }
                    else if(outOfBounds && desired != RoomTile.None)
                    {
                        return -1;
                    }
                    else if(outOfBounds)
                    {
                        continue;
                    }

                    RoomTile curent = room[x, y];

                    bool meetsWallRequirements = !hasWall || (curent == RoomTile.Wall);
                    bool meetsEdgeRequirements = (!hasEdge && !hasWall) || (hasEdge && curent == RoomTile.Edge);
                    bool meetsOpenRequriments = !hasOpen || (curent == RoomTile.Open);
                    bool doesntIntersect = curent == RoomTile.Open || curent == RoomTile.Edge || curent == RoomTile.Wall;

                    // Tiles conflict when walls exist in the BuildRoom already, or other objects
                    // block the template.
                    if (!(((meetsWallRequirements || meetsEdgeRequirements) && meetsOpenRequriments && doesntIntersect)))
                    {
                        return -1;
                    }
                }
            }

            int toReturn = 0;
            // Otherwise, we return the number of tiles which could be successfully placed.
            for(int r = 0; r < tr; r++)
            {
                for(int c = 0; c < tc; c++)
                {
                    int x = seedR + r;
                    int y = seedC + c;

                    if(x >= nr - 1 || y >= nc - 1 || x <= 0 || y <= 0)
                    {
                        continue;
                    }

                    RoomTile desiredTile = Template[r, c];
                    RoomTile unimport = Accessories[r, c];
                    RoomTile currentTile = room[x, y];

                    if((currentTile == RoomTile.Open || currentTile == RoomTile.Edge) && desiredTile != RoomTile.None && !Has(desiredTile, RoomTile.Edge) && ! Has(desiredTile, RoomTile.Wall) && desiredTile != RoomTile.Open)
                    {
                        room[x, y] = desiredTile;
                        rotations[x, y] = Rotation;
                        toReturn++;
                    }

                    if((currentTile != RoomTile.Open && currentTile != RoomTile.Edge) || unimport == RoomTile.Open || unimport == RoomTile.None || unimport == RoomTile.Edge)
                    {
                        continue;
                    }

                    room[x, y] = unimport;
                    toReturn++;
                }
            }

            return toReturn;
        }

        public static bool Has(RoomTile requirements, RoomTile value)
        {
            return (requirements & value) == value;
        }

        public static RoomTile[,] CreateFromRoom(List<Voxel> voxelsInRoom, ChunkManager chunks)
        {
            BoundingBox box0 = MathFunctions.GetBoundingBox(voxelsInRoom);
            BoundingBox box = new BoundingBox(box0.Min + Vector3.Up, box0.Max + Vector3.Up);
            BoundingBox bigBox = new BoundingBox(box0.Min + Vector3.Up + new Vector3(-1, 0, -1), box0.Max + Vector3.Up + new Vector3(1, 0, 1));
            int nr = Math.Max((int) (box.Max.X - box.Min.X), 1);
            int nc = Math.Max((int) (box.Max.Z - box.Min.Z), 1);

            RoomTile[,] toReturn = new RoomTile[nr + 2, nc + 2];

            Dictionary<Point, Voxel> voxelDict = new Dictionary<Point, Voxel>();
            foreach(Voxel vox in voxelsInRoom)
            {
                voxelDict[new Point((int)(vox.Position.X - box.Min.X) + 1, (int)(vox.Position.Z - box.Min.Z) + 1)] = vox;
            }

            for(int r = 0; r < nr + 2; r++)
            {
                for(int c = 0; c < nc + 2; c++)
                {
                    toReturn[r, c] = RoomTile.Edge;
                }
            }

            foreach(KeyValuePair<Point, Voxel> voxPair in voxelDict)
            {
                Voxel vox = voxPair.Value.GetVoxelAbove();
                Point p = voxPair.Key;

                if(vox.IsEmpty && p.X > 0 && p.X < nr + 1 && p.Y > 0 && p.Y < nc + 1)
                {
                    toReturn[p.X, p.Y] = RoomTile.Open;
                }
                else if(!vox.IsEmpty)
                {
                    toReturn[p.X, p.Y] = RoomTile.Wall;
                }
            }

            return toReturn;
        }
    }

}