using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FontBuilder
{
    class Program
    {
        public static System.Drawing.Bitmap LoadImage(string Path)
        {
            var image = System.Drawing.Image.FromFile(Path);
            return new System.Drawing.Bitmap(image);
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var options = Newtonsoft.Json.JsonConvert.DeserializeObject<Options>(System.IO.File.ReadAllText(args[0]));
            // Todo: Make all file operations relative path to options file.

            var characters = new List<char>();
            foreach (var range in options.Ranges)
                for (var i = range.Low; range.High >= range.Low && i <= range.High; ++i)
                    characters.Add((char)i);

            if (options.SearchForCharacters)
                RecursivelySearchForCharacters(options.SearchPath, options, characters);
            characters = characters.Distinct().ToList();

            var bitmap = new System.Drawing.Bitmap(1, 1);
            var graphics = System.Drawing.Graphics.FromImage(bitmap);

            foreach (var target in options.Targets)
            {
                var glyphs = new List<Glyph>();
                Dictionary<char, System.Drawing.Bitmap> baseFontGlyphs;

                if (!String.IsNullOrEmpty(target.BaseFont))
                {
                    try
                    {
                        var baseFontImage = System.Drawing.Image.FromFile(target.BaseFont);
                        baseFontGlyphs = VariableWidthBitmapFont.DecodeVariableWidthBitmapFont(new System.Drawing.Bitmap(baseFontImage));
                        foreach (var glyph in baseFontGlyphs)
                            glyphs.Add(new Glyph
                            {
                                Code = glyph.Key,
                                X = 0,
                                Y = 0,
                                Width = glyph.Value.Width,
                                Height = glyph.Value.Height,
                                Bitmap = glyph.Value
                            });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error loading basefont: {0}", e.Message);
                        baseFontGlyphs = new Dictionary<char, System.Drawing.Bitmap>();
                    }
                }
                else
                    baseFontGlyphs = new Dictionary<char, System.Drawing.Bitmap>();

                var font = new System.Drawing.Font(options.FontName, target.FontSize);


                foreach (var c in characters)
                {
                    if (baseFontGlyphs.ContainsKey(c)) continue;

                    try
                    {
                        var stringSize = graphics.MeasureString(new string(c, 1), font);
                        var g = new Glyph { Code = c, Width = (int)stringSize.Width, Height = (int)stringSize.Height };
                        g.Bitmap = new System.Drawing.Bitmap(g.Width, g.Height);

                        var glyphGraphics = System.Drawing.Graphics.FromImage(g.Bitmap);
                        glyphGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                        glyphGraphics.DrawString(new string(c, 1), font, System.Drawing.Brushes.White, 0, 0);
                        glyphGraphics.Flush();
                        glyphGraphics.Dispose();
                        glyphs.Add(g);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                    }
                }

                font.Dispose();

                if (glyphs.Count > 0)
                {
                    var atlas = AtlasCompiler.Compile(glyphs);

                    bitmap = new System.Drawing.Bitmap(atlas.Dimensions.Width, atlas.Dimensions.Height);
                    var composeGraphics = System.Drawing.Graphics.FromImage(bitmap);
                    foreach (var glyph in atlas.Glyphs)
                        composeGraphics.DrawImageUnscaled(glyph.Bitmap, new System.Drawing.Point(glyph.X, glyph.Y));
                    composeGraphics.Flush();

                    var imagePath = String.IsNullOrEmpty(target.OutputName) ? String.Format("__{0}.bmp", options.FontName) : target.OutputName + ".bmp";
                    bitmap.Save(imagePath);

                    var jsonPath = String.IsNullOrEmpty(target.OutputName) ? String.Format("__{0}_def.json", options.FontName) : target.OutputName + "_def.json";
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(atlas);
                    System.IO.File.WriteAllText(jsonPath, json);


                    composeGraphics.Dispose();

                    Console.WriteLine("Generated target {0}", imagePath);
                }
                else
                    Console.WriteLine("Target generated no glyphs.");

                graphics.Dispose();

            }
        }

        static void RecursivelySearchForCharacters(String Path, Options Options, List<char> Into)
        {
            if (!System.IO.Directory.Exists(Path)) return;

            foreach (var file in System.IO.Directory.EnumerateFiles(Path))
            {
                var extension = System.IO.Path.GetExtension(file);
                if (Options.SearchExtensions.Contains(extension))
                    Into.AddRange(System.IO.File.ReadAllText(file).Distinct());
            }

            foreach (var directory in System.IO.Directory.EnumerateDirectories(Path))
                RecursivelySearchForCharacters(directory, Options, Into);
        }
    }
}
