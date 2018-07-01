// OrientedAnimation.cs
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
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.LayeredSprites
{
    public class LayerDefinitionGrammar : Ancora.Grammar
    {
        public LayerDefinitionGrammar()
        {
            var digits = "0123456789";

            var ws = Maybe(Token(c => " \r\n\t".Contains(c))).Ast("WS")
                .WithMutator(Discard()); // Throw away the ast node so it doesn't appear in the final syntax tree.
            var string_literal = new Ancora.Parsers.StringLiteral('"').Ast("STRING");
            var integerLiteral = Token(c => digits.Contains(c)).Ast("INTEGER");

            Root = Token(c => c != ' ').Ast("LAYER-TYPE") + ws + integerLiteral.Ast("PRECEDENCE") + ws + string_literal + new Ancora.Parsers.AllInput().WithMutator(Discard());
        }
    }

    public class Layer
    {
        public String LayerType;
        public String AssetName;
        public int Precedence;
    }

    public class LayerLibrary
    {
        private static List<Layer> Layers;

        private static void Initialize()
        {
            if (Layers != null) return;

            var grammar = new LayerDefinitionGrammar();
            Layers = new List<Layer>();
            var definitions = FileUtils.LoadConfigurationLinesFromMultipleSources(ContentPaths.dwarf_layers);
            foreach (var line in definitions)
            {
                if (String.IsNullOrEmpty(line)) return;
                if (line[0] == ';') return;

                var parsed = grammar.ParseString(line);
                Layers.Add(new Layer
                {
                    LayerType = (string)parsed.Node.Children[0].Value,
                    Precedence = (int)parsed.Node.Children[1].Value,
                    AssetName = (string)parsed.Node.Children[2].Value
                });
            }
        }

        public static IEnumerable<Layer> EnumerateLayers(String LayerType)
        {
            return Layers.Where(l => l.LayerType == LayerType);
        }

        public static void SortLayerList(List<Layer> Layers)
        {
            Layers.Sort((a, b) => a.Precedence - b.Precedence);
        }
    }
}
