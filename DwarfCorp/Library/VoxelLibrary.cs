using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static VoxelType _EmptyVoxelType = null;
        public static VoxelType EmptyVoxelType { get { InitializeVoxels(); return _EmptyVoxelType; } }
        private static VoxelType _DesignationVoxelType = null;
        public static VoxelType DesignationVoxelType { get { InitializeVoxels(); return _DesignationVoxelType; } }

        private static Dictionary<string, VoxelType> VoxelTypes = new Dictionary<string, VoxelType>();
        private static List<VoxelType> VoxelTypeList = null;
        private static Dictionary<String, BoxPrimitive> VoxelPrimitives = new Dictionary<String, BoxPrimitive>();

        private static bool VoxelsInitialized = false;

        private static Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords> CreateTransitionUVs(Texture2D textureMap, int width, int height, Point[] tiles,  VoxelType.TransitionType transitionType = VoxelType.TransitionType.Horizontal)
        {
            var transitionTextures = new Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords>();

            for(int i = 0; i < 16; i++)
            {
                Point topPoint = new Point(tiles[0].X + i, tiles[0].Y);

                if (transitionType == VoxelType.TransitionType.Horizontal)
                {
                    BoxTransition transition = new BoxTransition()
                    {
                        Top = (TransitionTexture) i
                    };
                    transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                        textureMap.Height, width, height, tiles[2], tiles[2], topPoint, tiles[1], tiles[2], tiles[2]);
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                    { 
                         Point sidePoint = new Point(tiles[0].X + j, tiles[0].Y);
                        // TODO: create every iteration of frontback vs. left right. There should be 16 of these.
                        BoxTransition transition = new BoxTransition()
                        {
                            Left = (TransitionTexture)i,
                            Right = (TransitionTexture)i,
                            Front = (TransitionTexture)j,
                            Back = (TransitionTexture)j
                        };
                        transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                            textureMap.Height, width, height, sidePoint, sidePoint, tiles[2], tiles[1], topPoint, topPoint);
                    }
                }
            }

            return transitionTextures;
        }

        private static BoxPrimitive CreateVoxelPrimitive(Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom)
        {
            BoxPrimitive.BoxTextureCoords coords = new BoxPrimitive.BoxTextureCoords(textureMap.Width, textureMap.Height, width, height, sides, sides, top, bottom, sides, sides);
            BoxPrimitive cube = new BoxPrimitive(coords);

            return cube;
        }

        private static void InitializeVoxels()
        {
            if (VoxelsInitialized) return;
            VoxelsInitialized = true;

            var cubeTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            VoxelTypeList = FileUtils.LoadJsonListFromDirectory<VoxelType>(ContentPaths.voxel_types, null, v => v.Name);

            short id = 2;
            foreach (VoxelType type in VoxelTypeList)
            {
                VoxelTypes[type.Name] = type;

                    VoxelPrimitives[type.Name] = CreateVoxelPrimitive(cubeTexture, 32, 32, type.Top, type.Bottom, type.Sides);
                if (type.Name == "_empty")
                {
                    _EmptyVoxelType = type;
                    type.ID = 0;
                }
                else if (type.Name == "_designation")
                {
                    _DesignationVoxelType = type;
                    type.ID = 1;
                }
                else
                {
                    type.ID = id;
                    id += 1;
                }

                if (type.HasTransitionTextures)
                    type.TransitionTextures = CreateTransitionUVs(cubeTexture, 32, 32, type.TransitionTiles, type.Transitions);

                type.ExplosionSound = SoundSource.Create(type.ExplosionSoundResource);
                type.HitSound = SoundSource.Create(type.HitSoundResources);
            }

            VoxelTypeList = VoxelTypeList.OrderBy(v => v.ID).ToList();

            if (VoxelTypeList.Count > VoxelConstants.MaximumVoxelTypes)
                throw new InvalidProgramException(String.Format("There can be only {0} voxel types.", VoxelConstants.MaximumVoxelTypes));

            Console.WriteLine("Loaded Voxel Library.");
        }

        public static MaybeNull<VoxelType> GetVoxelType(short id)
        {
            InitializeVoxels();
            return VoxelTypeList[id];
        }

        public static MaybeNull<VoxelType> GetVoxelType(string name)
        {
            InitializeVoxels();
            if (VoxelTypes.ContainsKey(name))
                return VoxelTypes[name];
            else
                return null;
        }

        public static MaybeNull<BoxPrimitive> GetVoxelPrimitive(VoxelType type)
        {
            InitializeVoxels();

            if (VoxelPrimitives.ContainsKey(type.Name))
                return VoxelPrimitives[type.Name];
            return null;
        }

        public static IEnumerable<VoxelType> EnumerateVoxelTypes()
        {
            InitializeVoxels();
            return VoxelTypeList;
        }

        public static Dictionary<int, String> GetVoxelTypeMap()
        {
            InitializeVoxels();
            var r = new Dictionary<int, String>();
            for (var i = 0; i < VoxelTypeList.Count; ++i)
                r.Add(i, VoxelTypeList[i].Name);
            return r;
        }

        // Todo: Use Sheet.TileHeight as well.
        [TextureGenerator("Voxels")]
        public static Texture2D RenderVoxelIcons(GraphicsDevice device, Microsoft.Xna.Framework.Content.ContentManager Content, Gui.TileSheetDefinition Sheet)
        {
            InitializeVoxels();

            var shader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);

            var sqrt = (int)(Math.Ceiling(Math.Sqrt(VoxelPrimitives.Count)));
            var width = MathFunctions.NearestPowerOf2(sqrt * Sheet.TileWidth);
            var height = MathFunctions.NearestPowerOf2(sqrt * Sheet.TileWidth);

            RenderTarget2D toReturn = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
        
            device.SetRenderTarget(toReturn);
            device.Clear(Color.Transparent);
            shader.SetIconTechnique();
            shader.MainTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            shader.SelfIlluminationEnabled = true;
            shader.SelfIlluminationTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_illumination);
            shader.EnableShadows = false;
            shader.EnableLighting = false;
            shader.ClippingEnabled = false;
            shader.CameraPosition = new Vector3(-0.5f, 0.5f, 0.5f);
            shader.VertexColorTint = Color.White;
            shader.LightRamp = Color.White;
            shader.SunlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.sungradient);
            shader.AmbientOcclusionGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.ambientgradient);
            shader.TorchlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.torchgradient);

            Viewport oldview = device.Viewport;
            int rows = height  / Sheet.TileWidth;
            int cols = width/ Sheet.TileWidth;
            device.ScissorRectangle = new Rectangle(0, 0, Sheet.TileWidth, Sheet.TileWidth);
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.Default;
            Vector3 half = Vector3.One*0.5f;
            half = new Vector3(half.X, half.Y, half.Z);

            List<VoxelType> voxelsByType = VoxelTypes.Select(type => type.Value).ToList();
            voxelsByType.Sort((a, b) => a.ID < b.ID ? -1 : 1);

            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                foreach (var type in voxelsByType)
                {
                    int row = type.ID/cols;
                    int col = type.ID%cols;

                    var maybePrimitive = GetVoxelPrimitive(type);
                    if (maybePrimitive.HasValue(out BoxPrimitive primitive))
                    {
                        if (type.HasTransitionTextures)
                            primitive = new BoxPrimitive(type.TransitionTextures[new BoxTransition()]);

                        device.Viewport = new Viewport(col * Sheet.TileWidth, row * Sheet.TileWidth, Sheet.TileWidth, Sheet.TileWidth);
                        Matrix viewMatrix = Matrix.CreateLookAt(new Vector3(-1.2f, 1.0f, -1.5f), Vector3.Zero, Vector3.Up);
                        Matrix projectionMatrix = Matrix.CreateOrthographic(1.5f, 1.5f, 0, 5);
                        shader.View = viewMatrix;
                        shader.Projection = projectionMatrix;
                        shader.World = Matrix.CreateTranslation(-half);
                        shader.VertexColorTint = type.Tint;
                        pass.Apply();
                        primitive.Render(device);
                    }
                }
            }
            device.Viewport = oldview;
            device.SetRenderTarget(null);
            return (Texture2D) toReturn;
        }
    }
}