using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class DwarfRunner
    {
        private ImageFrame Soil;
        private ImageFrame Grass;
        private Texture2D Balloon;
        private Texture2D Dwarf;
        private List<ImageFrame> DwarfFrames;
        private Vector2 DwarfPosition = new Vector2(-0.6f, 8.0f);
        private float DwarfVelocity = 0.0f;
        private float ScrollSpeed = 6.0f;
        private float HorizontalOffset = 0;
        private Point TileSize = new Point(32, 32);
        private Random Random = new Random();
        private Point WorldSize = new Point(1, 1);
        private ImageFrame UpFrame;
        private ImageFrame DownFrame;
        private class Tile
        {
            public bool Solid = false;
            public ImageFrame Image = null;
        }

        private Tile[,] World;

        public DwarfRunner(DwarfGame game)
        {
            TileSize.Y = game.GraphicsDevice.Viewport.Height / 10;
            TileSize.X = TileSize.Y;

            WorldSize = new Point((game.GraphicsDevice.Viewport.Width / TileSize.X) + 4, 10);
            World = new Tile[WorldSize.X, WorldSize.Y];

            String[] sheets =
            {
                ContentPaths.Entities.Dwarf.Sprites.soldier, ContentPaths.Entities.Dwarf.Sprites.worker,
                ContentPaths.Entities.Dwarf.Sprites.crafter, ContentPaths.Entities.Dwarf.Sprites.wizard,
                ContentPaths.Entities.Dwarf.Sprites.musket
            };

            Texture2D tiles = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);
            Balloon = TextureManager.GetTexture(ContentPaths.Entities.Balloon.Sprites.balloon);
            Dwarf = TextureManager.GetTexture(Datastructures.SelectRandom(sheets));
            
            Soil = new ImageFrame(tiles, 32, 2, 0);
            Grass = new ImageFrame(tiles, 32, 3, 0);

            DwarfFrames = new List<ImageFrame>(new ImageFrame[]
            {
                new ImageFrame(Dwarf, new Rectangle(0, 80, 32, 40)),
                new ImageFrame(Dwarf, new Rectangle(32, 80, 32, 40)),
                new ImageFrame(Dwarf, new Rectangle(64, 80, 32, 40)),
                new ImageFrame(Dwarf, new Rectangle(96, 80, 32, 40)),
            });

            UpFrame = new ImageFrame(Dwarf, new Rectangle(0, 239, 32, 40));
            DownFrame = new ImageFrame(Dwarf, new Rectangle(32, 239, 32, 40));
            for (var x = 0; x < WorldSize.X; ++x)
                World[x, 0] = new Tile
                {
                    Solid = true,
                    Image = Soil
                };

            for (var x = 0; x < WorldSize.X; ++x)
                World[x, 1] = new Tile
                {
                    Solid = true,
                    Image = Grass
                };
        }

        private Tile GetTileUnderFoot(float bias)
        {
            var tileUnderFoot = new Point((int)(DwarfPosition.X - HorizontalOffset), (int)(DwarfPosition.Y - bias));
            if (tileUnderFoot.X >= 0 && tileUnderFoot.X < WorldSize.X && tileUnderFoot.Y >= 0 && tileUnderFoot.Y < WorldSize.Y)
                return World[tileUnderFoot.X, tileUnderFoot.Y];
            return null;
        }

        private Tile GetTileAhead()
        { 
            var tileAhead = new Point((int)(DwarfPosition.X + 0.55f - HorizontalOffset), (int)(DwarfPosition.Y + 0.5f));
            if (tileAhead.X >= 0 && tileAhead.X < WorldSize.X && tileAhead.Y >= 0 && tileAhead.Y < WorldSize.Y)
                return World[tileAhead.X, tileAhead.Y];
            return null;
        }

        private float PenetrationDepth()
        {
            return (DwarfPosition.X + 0.55f) - (float)Math.Floor(DwarfPosition.X + 0.55f);
        }

        public void Jump()
        {
            if (GetTileUnderFoot(0.05f) != null)
                DwarfVelocity = 12.0f;
        }

        public void Update(DwarfTime Time)
        {
            HorizontalOffset -= (float)Time.ElapsedRealTime.TotalSeconds * ScrollSpeed;
            if (HorizontalOffset <= -2.0f)
            {
                HorizontalOffset += 1.0f;

                //Scroll map.
                for (var x = 0; x < (WorldSize.X - 1); ++x)
                    for (var y = 0; y < WorldSize.Y; ++y)
                        World[x, y] = World[x + 1, y];

                //Generate new column.
                World[WorldSize.X - 1, 0] = new Tile { Solid = true, Image = Soil };
                //if (Random.NextDouble() > 0.8)
                //{
                //    World[WorldSize.X - 1, 1] = new Tile { Solid = true, Image = Soil };
                //    World[WorldSize.X - 1, 2] = new Tile { Solid = true, Image = Grass };
                //}
                //else
                {
                    World[WorldSize.X - 1, 1] = new Tile { Solid = true, Image = Grass };
                    World[WorldSize.X - 1, 2] = null;
                }
            }

            DwarfVelocity -= (float)Time.ElapsedRealTime.TotalSeconds * 30.0f;
            
            DwarfPosition.Y += DwarfVelocity * (float)Time.ElapsedRealTime.TotalSeconds;
            if (GetTileUnderFoot(0.05f) != null)
            {
                DwarfPosition.Y = (float)Math.Round(DwarfPosition.Y);
                DwarfVelocity = 0.0f;
            }

            if (DwarfPosition.X < 3.0f && GetTileAhead() == null)
                DwarfPosition.X += (float)Time.ElapsedRealTime.TotalSeconds * 6.0f;

            if (GetTileAhead() != null)
                DwarfPosition.X -= /*Math.Max(PenetrationDepth(), */(float)Time.ElapsedRealTime.TotalSeconds * ScrollSpeed;//);

            if (DwarfPosition.X < -0.5f)
                DwarfPosition = new Vector2(-0.6f, 6.0f);
        }

        public void Render(GraphicsDevice graphics, SpriteBatch sprites, DwarfTime time)
        {
            try
            {
                sprites.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                    DepthStencilState.Default, RasterizerState.CullNone);

                // Draw ground.
                for (var x = 0; x < WorldSize.X; ++x)
                    for (var y = 0; y < WorldSize.Y; ++y)
                        if (World[x, y] != null && World[x, y].Solid)
                            sprites.Draw(World[x, y].Image.Image,
                                new Rectangle((int) ((x*TileSize.X) + (HorizontalOffset*TileSize.X)),
                                    graphics.Viewport.Height - ((y + 1)*TileSize.Y), TileSize.X, TileSize.Y),
                                World[x, y].Image.SourceRect, Color.White);

                sprites.Draw(Balloon, new Vector2(graphics.Viewport.Width*0.75f,
                    (float) Math.Sin(time.TotalRealTime.TotalSeconds)*64), Color.White);

                var dwarfFrame = (int) (time.TotalRealTime.TotalSeconds*16)%4;
                var tile = DwarfFrames[dwarfFrame].SourceRect;
                if (GetTileUnderFoot(0.05f) == null)
                {
                    if (DwarfVelocity > 0.5f)
                    {
                        tile = UpFrame.SourceRect;
                    }
                    else if (DwarfVelocity < -0.5f)
                    {
                        tile = DownFrame.SourceRect;
                    }
                }
                sprites.Draw(Dwarf,
                    new Rectangle(
                        (int) (DwarfPosition.X*TileSize.X) - (TileSize.X/2),
                        graphics.Viewport.Height - (int) (DwarfPosition.Y*TileSize.Y) - (int) (TileSize.Y*1.25f),
                        TileSize.X,
                        (int) (TileSize.Y*1.25f)),
                    tile, Color.White);

                sprites.End();
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception.ToString());
                //throw exception;
            }

        }
    }
}
