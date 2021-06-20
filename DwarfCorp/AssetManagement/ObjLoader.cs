using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class ObjLoader
    {

        public class ObjLineGrammar : Ancora.Grammar
        {
            public Func<Ancora.AstNode, Ancora.AstNode> KeepLeaves()
            {
                return a =>
                {
                    var tChildren = a.Children;
                    a.Children = new List<Ancora.AstNode>();
                    foreach (var child in tChildren)
                        if (child.Children.Count > 0)
                            a.Children.AddRange(child.Children);
                        else
                            a.Children.Add(child);
                    return a;
                };
            }

            public ObjLineGrammar()
            {
                var delimeters = " \r\n\t/";
                var digits = "0123456789";

                var ws = Maybe(Token(c => " \r\n\t".Contains(c))).Ast("WS")
                    .WithMutator(Discard()); // Throw away the ast node so it doesn't appear in the final syntax tree.
                var string_literal = new Ancora.Parsers.StringLiteral('"').Ast("STRING");
                var integerLiteral = Token(c => digits.Contains(c)).Ast("INTEGER");

                var numberLiteral = (Maybe((Character('-') | Character('+')).WithMutator(Collapse()))
                    + integerLiteral
                    + Maybe((Character('.') + integerLiteral).WithMutator(PassChild(1)))
                    + Maybe((Character('e') + Character('-') + integerLiteral).WithMutator(PassChild(2)))
                    ).WithMutator(node_in =>
                    {
                        var front = node_in.Children[1].Value.ToString();
                        var back = node_in.Children[2].Children.Count > 0 ? node_in.Children[2].Children[0].Value.ToString() : "0";
                        var si = node_in.Children[3].Children.Count > 0 ? node_in.Children[3].Children[0].Value.ToString() : "";
                        var floatValue = float.Parse(front + "." + back + (!String.IsNullOrEmpty(si) ? "e-" + si : ""));

                        if (node_in.Children[0].Children.Count > 0 && node_in.Children[0].Children[0].Value.ToString() == "-")
                            floatValue *= -1.0f;

                        return new Ancora.AstNode
                        {
                            Value = floatValue,
                            Location = node_in.Location,
                            NodeType = "NUMBER"
                        };
                    });

                var token = Token(c => !delimeters.Contains(c)).Ast("TOKEN");
                var face_divider = Token(c => c == '/').Ast("FACE_DIVIDER").WithMutator(Discard());
                var face = (OneOrMany( (numberLiteral + face_divider).WithMutator(Collapse()) ) + numberLiteral).WithMutator(KeepLeaves()).Ast("FACE");
                var term = (face | string_literal | numberLiteral | token).WithMutator(Collapse());
                var command = (OneOrMany((ws + term + ws).WithMutator(Collapse())) + new Ancora.Parsers.AllInput().WithMutator(Discard())).WithMutator(Collapse());

                this.Root = command;
            }
        }

        private struct Corner
        {
            public int Vertex;
            public int TexCoord;
        }

        private class Face : List<Corner>
        {

        }

        public static RawPrimitive LoadObject(string[] File)
        {
            var parser = new ObjLineGrammar();
            var verticies = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var faces = new List<Face>();

            foreach (var line in File)
            {
                if (String.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;

                var result = parser.ParseString(line);
                if (result.ResultType != Ancora.ResultType.Success)
                    continue;

                switch (result.Node.Children[0].Value.ToString())
                {
                    case "v":
                        try
                        {
                            verticies.Add(new Vector3((float)result.Node.Children[1].Value, (float)result.Node.Children[2].Value, (float)result.Node.Children[3].Value));
                        }
                        catch (Exception e)
                        {
                            var x = 5;
                        }
                        break;
                    case "vt":
                        texCoords.Add(new Vector2((float)result.Node.Children[1].Value, 1.0f - (float)result.Node.Children[2].Value));
                        break;
                    case "f":
                        {
                            try
                            {
                                var f = new Face();
                                for (var i = 1; i < result.Node.Children.Count; i += 1)
                                    f.Add(new Corner { Vertex = (int)(float)result.Node.Children[i].Children[0].Value, TexCoord = (int)(float)result.Node.Children[i].Children[1].Value });
                                faces.Add(f);
                            }
                            catch (Exception e)
                            {
                                var x = 5;
                            }
                        }
                        break;
                    default:
                        break;

                }
            }

            var r = new RawPrimitive();
            foreach (var face in faces)
                for (var i = 2; i < face.Count; ++i)
                    r.AddTriangle(
                        new ExtendedVertex(verticies[face[0].Vertex - 1], Color.White, Color.White, texCoords[face[0].TexCoord - 1], new Vector4(0, 0, 1, 1)),
                        new ExtendedVertex(verticies[face[i - 1].Vertex - 1], Color.White, Color.White, texCoords[face[i - 1].TexCoord - 1], new Vector4(0, 0, 1, 1)),
                        new ExtendedVertex(verticies[face[i].Vertex - 1], Color.White, Color.White, texCoords[face[i].TexCoord - 1], new Vector4(0, 0, 1, 1)));

            return r;
        }
    }
}