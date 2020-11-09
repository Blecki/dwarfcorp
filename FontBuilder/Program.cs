using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using System.Drawing;
using System.Runtime.InteropServices;

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

            var options = Newtonsoft.Json.JsonConvert.DeserializeObject<Options>(File.ReadAllText(args[0]));
            var workingDirectory = args[1];

            var characters = new List<char>();
            foreach (var range in options.Ranges)
                for (var i = range.Low; range.High >= range.Low && i <= range.High; ++i)
                    characters.Add((char)i);

            if (options.SearchForCharacters)
                RecursivelySearchForCharacters(Path.Combine(workingDirectory, options.SearchPath), options, characters);
            characters = characters.Distinct().ToList();

            foreach (var target in options.Targets)
            {
                var glyphs = new List<Glyph>();
                Dictionary<char, System.Drawing.Bitmap> baseFontGlyphs;

                if (!String.IsNullOrEmpty(target.BaseFont))
                {
                    try
                    {
                        var baseFontImage = System.Drawing.Image.FromFile(Path.Combine(workingDirectory, target.BaseFont));
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

                var font = new System.Drawing.Font(options.FontName, target.FontSize, System.Drawing.GraphicsUnit.Pixel);
                var bitmap = new System.Drawing.Bitmap(1, 1);
                var graphics = System.Drawing.Graphics.FromImage(bitmap);

                var typeface = new Typeface(options.FontName);

                // create glyphtypeface
                typeface.TryGetGlyphTypeface(out var glyphTypeface);

                foreach (var c in characters)
                {
                    if (c == 'W')
                    {
                        var x = 5;
                    }

                    if (baseFontGlyphs.ContainsKey(c)) continue;

                    try
                    {
                        var stringSize = graphics.MeasureString(new string(c, 1), font);
                        var g = new Glyph { Code = c, Width = (int)stringSize.Width, Height = (int)stringSize.Height };
                        g.Bitmap = new System.Drawing.Bitmap(g.Width, g.Height);

                        var glyphGraphics = System.Drawing.Graphics.FromImage(g.Bitmap);
                        glyphGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                        glyphGraphics.DrawString(new string(c, 1), font, System.Drawing.Brushes.White, 0, 0);
                        glyphGraphics.Flush();
                        glyphGraphics.Dispose();


                        if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out var fontGlyph))
                        {
                            glyphTypeface.RightSideBearings.TryGetValue(fontGlyph, out var rightBearing);
                            if (glyphTypeface.AdvanceWidths.TryGetValue(fontGlyph, out var advanceWidth))
                                g.AdvanceWidth = (int)Math.Round(advanceWidth * target.FontSize);
                            else
                                g.AdvanceWidth = g.Width;

                            if (glyphTypeface.LeftSideBearings.TryGetValue(fontGlyph, out var leftBearing))
                                g.LeftBearing = (int)Math.Round(leftBearing * target.FontSize);
                            else
                                g.LeftBearing = 0;
                        }
                        else
                        {
                            g.AdvanceWidth = g.Width;
                            g.LeftBearing = 0;
                        }


                        glyphs.Add(g);

                        Console.Write(c);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{2} {3} Exception: {0} {1}", e.Message, e.StackTrace, target.FontSize, c);
                        break;
                    }
                }

                Console.WriteLine();

                //font.Dispose();

                var imagePath = String.IsNullOrEmpty(target.OutputName) ? String.Format("__{0}.bmp", options.FontName) : target.OutputName + ".bmp";

                if (glyphs.Count > 0)
                {
                    var atlas = AtlasCompiler.Compile(glyphs);

                    bitmap = new System.Drawing.Bitmap(atlas.Dimensions.Width, atlas.Dimensions.Height);
                    var composeGraphics = System.Drawing.Graphics.FromImage(bitmap);
                    foreach (var glyph in atlas.Glyphs)
                        composeGraphics.DrawImageUnscaled(glyph.Bitmap, new System.Drawing.Point(glyph.X, glyph.Y));
                    composeGraphics.Flush();


                    bitmap.Save(Path.Combine(workingDirectory, imagePath));

                    var kernings = GetKerningPairs(font, composeGraphics);
                    atlas.Kernings = new Dictionary<string, int>();
                    foreach (var entry in kernings)
                    {
                        if (characters.Contains((char)entry.wFirst) && characters.Contains((char)entry.wSecond))
                            atlas.Kernings.Add(new string((char)entry.wFirst, 1) + new string((char)entry.wSecond, 1), entry.iKernAmount);
                    }

                    var jsonPath = String.IsNullOrEmpty(target.OutputName) ? String.Format("__{0}_def.font", options.FontName) : target.OutputName + "_def.font";
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(atlas, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(Path.Combine(workingDirectory, jsonPath), json);




                    composeGraphics.Dispose();

                    Console.WriteLine("Generated target {0}", imagePath);
                }
                else
                    Console.WriteLine("Target {0} generated no glyphs.", imagePath);

                graphics.Dispose();

            }
        }

        static void RecursivelySearchForCharacters(String Path, Options Options, List<char> Into)
        {
            Console.WriteLine("Directory: {0}", Path);

            if (!System.IO.Directory.Exists(Path))
            {
                Console.WriteLine("Attempted to search directory that does not exist.");
                return;
            }

            foreach (var file in System.IO.Directory.EnumerateFiles(Path))
            {
                var extension = System.IO.Path.GetExtension(file);
                if (Options.SearchExtensions.Contains(extension))
                {
                    Console.WriteLine("File: {0}", file);
                    var chars = File.ReadAllText(file).Distinct();
                    foreach (var c in chars)
                    {
                        if (Into.Contains(c)) continue;
                        Into.Add(c);
                        Console.WriteLine("Found {0}", c);
                    }
                }
            }

            foreach (var directory in System.IO.Directory.EnumerateDirectories(Path))
                RecursivelySearchForCharacters(directory, Options, Into);
        }

        // Shamelessly stolen from
        //  https://github.com/SudoMike/SudoFont
        public static KerningPair[] GetKerningPairs(Font font, Graphics graphics)
        {
            // Select the HFONT into the HDC.
            IntPtr hDC = graphics.GetHdc();
            Font fontClone = (Font)font.Clone();
            IntPtr hFont = fontClone.ToHfont();
            SelectObject(hDC, hFont);

            // Find out how many pairs there are and allocate them.
            int numKerningPairs = GetKerningPairs(hDC.ToInt32(), 0, null);
            KerningPair[] kerningPairs = new KerningPair[numKerningPairs];

            // Get the pairs.
            GetKerningPairs(hDC.ToInt32(), kerningPairs.Length, kerningPairs);

            DeleteObject(hFont);
            graphics.ReleaseHdc();

            return kerningPairs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KerningPair
        {
            public Int16 wFirst;
            public Int16 wSecond;
            public Int32 iKernAmount;
        }

        [DllImport("Gdi32.dll", EntryPoint = "GetKerningPairs", SetLastError = true)]
        static extern int GetKerningPairs(int hdc, int nNumPairs, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] KerningPair[] kerningPairs);

        [DllImport("Gdi32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("Gdi32.dll", CharSet = CharSet.Unicode)]
        static extern bool DeleteObject(IntPtr hdc);
    }
}
