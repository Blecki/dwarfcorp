#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2021 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.IO;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public abstract class Texture : GraphicsResource
	{
		#region Public Properties

		public SurfaceFormat Format
		{
			get;
			protected set;
		}

		public int LevelCount
		{
			get;
			protected set;
		}

		#endregion

		#region Internal FNA3D Variables

		internal IntPtr texture;

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.Textures.RemoveDisposedTexture(this);
				GraphicsDevice.VertexTextures.RemoveDisposedTexture(this);
				FNA3D.FNA3D_AddDisposeTexture(
					GraphicsDevice.GLDevice,
					texture
				);
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal Context Reset Method

		internal protected override void GraphicsDeviceResetting()
		{
			// FIXME: Do we even want to bother with DeviceResetting for GL? -flibit
		}

		#endregion

		#region Static SurfaceFormat Size Methods

		internal static int GetFormatSize(SurfaceFormat format)
		{
			switch (format)
			{
				case SurfaceFormat.Dxt1:
					return 8;
				case SurfaceFormat.Dxt3:
				case SurfaceFormat.Dxt5:
					return 16;
				case SurfaceFormat.Alpha8:
					return 1;
				case SurfaceFormat.Bgr565:
				case SurfaceFormat.Bgra4444:
				case SurfaceFormat.Bgra5551:
				case SurfaceFormat.HalfSingle:
				case SurfaceFormat.NormalizedByte2:
					return 2;
				case SurfaceFormat.Color:
				case SurfaceFormat.Single:
				case SurfaceFormat.Rg32:
				case SurfaceFormat.HalfVector2:
				case SurfaceFormat.NormalizedByte4:
				case SurfaceFormat.Rgba1010102:
				case SurfaceFormat.ColorBgraEXT:
					return 4;
				case SurfaceFormat.HalfVector4:
				case SurfaceFormat.Rgba64:
				case SurfaceFormat.Vector2:
				case SurfaceFormat.HdrBlendable:
					return 8;
				case SurfaceFormat.Vector4:
					return 16;
				default:
					throw new ArgumentException("Should be a value defined in SurfaceFormat", "Format");
			}
		}

		internal static int GetPixelStoreAlignment(SurfaceFormat format) 
		{
			/*
			 * https://github.com/FNA-XNA/FNA/pull/238
			 * https://www.khronos.org/registry/OpenGL/specs/gl/glspec21.pdf
			 * OpenGL 2.1 Specification, section 3.6.1, table 3.1 specifies that the pixelstorei alignment cannot exceed 8
			 */
			return Math.Min(8, GetFormatSize(format));
		}

		internal static void ValidateGetDataFormat(
			SurfaceFormat format,
			int elementSizeInBytes
		) {
			if (GetFormatSize(format) % elementSizeInBytes != 0)
			{
				throw new ArgumentException(
					"The type you are using for T in this" +
					" method is an invalid size for this" +
					" resource"
				);
			}
		}

		#endregion

		#region Static Mipmap Level Calculator

		internal static int CalculateMipLevels(
			int width,
			int height = 0,
			int depth = 0
		) {
			int levels = 1;
			for (
				int size = Math.Max(Math.Max(width, height), depth);
				size > 1;
				levels += 1
			) {
				size /= 2;
			}
			return levels;
		}

		#endregion

		#region Static DDS Parser

		// DDS loading extension, based on MojoDDS
		internal static void ParseDDS(
			BinaryReader reader,
			out SurfaceFormat format,
			out int width,
			out int height,
			out int levels,
			out int levelSize,
			out int blockSize
		) {
			// A whole bunch of magic numbers, yay DDS!
			const uint DDS_MAGIC = 0x20534444;
			const uint DDS_HEADERSIZE = 124;
			const uint DDS_PIXFMTSIZE = 32;
			const uint DDSD_CAPS = 0x1;
			const uint DDSD_HEIGHT = 0x2;
			const uint DDSD_WIDTH = 0x4;
			const uint DDSD_PITCH = 0x8;
			const uint DDSD_FMT = 0x1000;
			const uint DDSD_LINEARSIZE = 0x80000;
			const uint DDSD_REQ = (
				DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_FMT
			);
			const uint DDSCAPS_MIPMAP = 0x400000;
			const uint DDSCAPS_TEXTURE = 0x1000;
			const uint DDSCAPS2_CUBEMAP = 0x200;
			const uint DDPF_FOURCC = 0x4;
			const uint DDPF_RGB = 0x40;
			const uint FOURCC_DXT1 = 0x31545844;
			const uint FOURCC_DXT3 = 0x33545844;
			const uint FOURCC_DXT5 = 0x35545844;
			// const uint FOURCC_DX10 = 0x30315844;
			const uint pitchAndLinear = (
				DDSD_PITCH | DDSD_LINEARSIZE
			);

			// File should start with 'DDS '
			if (reader.ReadUInt32() != DDS_MAGIC)
			{
				throw new NotSupportedException("Not a DDS!");
			}

			// Texture info
			uint size = reader.ReadUInt32();
			if (size != DDS_HEADERSIZE)
			{
				throw new NotSupportedException("Invalid DDS header!");
			}
			uint flags = reader.ReadUInt32();
			if ((flags & DDSD_REQ) != DDSD_REQ)
			{
				throw new NotSupportedException("Invalid DDS flags!");
			}
			if ((flags & pitchAndLinear) == pitchAndLinear)
			{
				throw new NotSupportedException("Invalid DDS flags!");
			}
			height = reader.ReadInt32();
			width = reader.ReadInt32();
			reader.ReadUInt32(); // dwPitchOrLinearSize, unused
			reader.ReadUInt32(); // dwDepth, unused
			levels = reader.ReadInt32();

			// "Reserved"
			reader.ReadBytes(4 * 11);

			// Format info
			uint formatSize = reader.ReadUInt32();
			if (formatSize != DDS_PIXFMTSIZE)
			{
				throw new NotSupportedException("Bogus PIXFMTSIZE!");
			}
			uint formatFlags = reader.ReadUInt32();
			uint formatFourCC = reader.ReadUInt32();
			uint formatRGBBitCount = reader.ReadUInt32();
			uint formatRBitMask = reader.ReadUInt32();
			uint formatGBitMask = reader.ReadUInt32();
			uint formatBBitMask = reader.ReadUInt32();
			uint formatABitMask = reader.ReadUInt32();

			// dwCaps "stuff"
			uint caps = reader.ReadUInt32();
			if ((caps & DDSCAPS_TEXTURE) == 0)
			{
				throw new NotSupportedException("Not a texture!");
			}
			uint caps2 = reader.ReadUInt32();
			if (	caps2 != 0 &&
				(caps2 & DDSCAPS2_CUBEMAP) != DDSCAPS2_CUBEMAP	)
			{
				throw new NotSupportedException("Invalid caps2!");
			}
			reader.ReadUInt32(); // dwCaps3, unused
			reader.ReadUInt32(); // dwCaps4, unused

			// "Reserved"
			reader.ReadUInt32();

			// Mipmap sanity check
			if ((caps & DDSCAPS_MIPMAP) != DDSCAPS_MIPMAP)
			{
				levels = 1;
			}

			// Determine texture format
			blockSize = 0;
			if ((formatFlags & DDPF_FOURCC) == DDPF_FOURCC)
			{
				if (formatFourCC == FOURCC_DXT1)
				{
					format = SurfaceFormat.Dxt1;
					blockSize = 8;
				}
				else if (formatFourCC == FOURCC_DXT3)
				{
					format = SurfaceFormat.Dxt3;
					blockSize = 16;
				}
				else if (formatFourCC == FOURCC_DXT5)
				{
					format = SurfaceFormat.Dxt5;
					blockSize = 16;
				}
				else
				{
					throw new NotSupportedException(
						"Unsupported DDS texture format"
					);
				}
				levelSize = (
					((width > 0 ? ((width + 3) / 4) : 1) * blockSize) *
					(height > 0 ? ((height + 3) / 4) : 1)
				);
			}
			else if ((formatFlags & DDPF_RGB) == DDPF_RGB)
			{
				if (	formatRGBBitCount != 32 ||
					formatRBitMask != 0x00FF0000 ||
					formatGBitMask != 0x0000FF00 ||
					formatBBitMask != 0x000000FF ||
					formatABitMask != 0xFF000000	)
				{
					throw new NotSupportedException(
						"Unsupported DDS texture format"
					);
				}

				format = SurfaceFormat.ColorBgraEXT;
				levelSize = (int) (
					(((width * formatRGBBitCount) + 7) / 8) *
					height
				);
			}
			else
			{
				throw new NotSupportedException(
					"Unsupported DDS texture format"
				);
			}
		}

		#endregion
	}
}