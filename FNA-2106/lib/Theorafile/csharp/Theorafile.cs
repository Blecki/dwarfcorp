/* Theorafile# - C# Wrapper for Theorafile
 *
 * Copyright (c) 2017-2021 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

#region Using Statements
using System;
using System.Runtime.InteropServices;
using System.Text;
#endregion

public static class Theorafile
{
	#region Native Library Name

	const string nativeLibName = "libtheorafile";

	#endregion

	#region UTF8 Marshaling

	/* Used for heap allocated string marshaling
	 * Returned byte* must be free'd with FreeHGlobal.
	 */
	private static unsafe byte* Utf8Encode(string str)
	{
		int bufferSize = (str.Length * 4) + 1;
		byte* buffer = (byte*) Marshal.AllocHGlobal(bufferSize);
		fixed (char* strPtr = str)
		{
			Encoding.UTF8.GetBytes(
				strPtr,
				str.Length + 1,
				buffer,
				bufferSize
			);
		}
		return buffer;
	}

	#endregion

	#region C stdio Macros

	// Used by ov_callbacks, seek_func
	public enum SeekWhence : int
	{
		// Add TF_ prefix to prevent C macro conflicts
		TF_SEEK_SET = 0,
		TF_SEEK_CUR = 1,
		TF_SEEK_END = 2
	}

	#endregion

	#region Theorafile Delegates

	/* IntPtr refers to a size_t */
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr read_func(
		IntPtr ptr,		// Refers to a void*
		IntPtr size,		// Refers to a size_t
		IntPtr nmemb,		// Refers to a size_t
		IntPtr datasource	// Refers to a void*
	);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int seek_func(
		IntPtr datasource,	// Refers to a void*
		long offset,		// Refers to an ogg_int64_t
		SeekWhence whence
	);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int close_func(
		IntPtr datasource	// Refers to a void*
	);

	#endregion

	#region libtheora Enumerations

	public enum th_pixel_fmt
	{
		TH_PF_420,
		TH_PF_RSVD,
		TH_PF_422,
		TH_PF_444,
		TH_PF_NFORMATS
	}

	#endregion

	#region Theorafile Structures

	[StructLayout(LayoutKind.Sequential)]
	public struct tf_callbacks
	{
		public read_func read_func;
		public seek_func seek_func;
		public close_func close_func;
	}

	#endregion

	#region Theorafile Implementation

	[DllImport(nativeLibName, EntryPoint = "tf_open_callbacks", CallingConvention = CallingConvention.Cdecl)]
	private static extern int INTERNAL_tf_open_callbacks(
		IntPtr datasource,
		IntPtr file,
		tf_callbacks io
	);
	public static int tf_open_callbacks(
		IntPtr datasource,
		out IntPtr file,
		tf_callbacks io
	) {
		file = AllocTheoraFile();
		return INTERNAL_tf_open_callbacks(datasource, file, io);
	}

	[DllImport(nativeLibName, EntryPoint = "tf_fopen", CallingConvention = CallingConvention.Cdecl)]
	private static extern unsafe int INTERNAL_tf_fopen(
		byte* fname,
		IntPtr file
	);
	public static unsafe int tf_fopen(string fname, out IntPtr file)
	{
		file = AllocTheoraFile();

		byte* utf8Fname = Utf8Encode(fname);
		int result = INTERNAL_tf_fopen(utf8Fname, file);
		Marshal.FreeHGlobal((IntPtr) utf8Fname);
		return result;
	}

	[DllImport(nativeLibName, EntryPoint = "tf_close", CallingConvention = CallingConvention.Cdecl)]
	private static extern int INTERNAL_tf_close(IntPtr file);
	public static int tf_close(ref IntPtr file)
	{
		int result = INTERNAL_tf_close(file);
		Marshal.FreeHGlobal(file);
		file = IntPtr.Zero;
		return result;
	}

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_hasaudio(IntPtr file);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_hasvideo(IntPtr file);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_videoinfo(
		IntPtr file,
		out int width,
		out int height,
		out double fps,
		out th_pixel_fmt fmt
	);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_audioinfo(
		IntPtr file,
		out int channels,
		out int samplerate
	);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_setaudiotrack(IntPtr file, int track);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_eos(IntPtr file);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern void tf_reset(IntPtr file);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_readvideo(IntPtr file, IntPtr buffer, int numframes);

	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	public static extern int tf_readaudio(IntPtr file, IntPtr buffer, int length);

	#endregion

	#region OggTheora_File Allocator

	/* Notice that we did not implement an OggTheora_File struct, but are
	 * instead using a pointer natively malloc'd.
	 *
	 * C# Interop for Xiph structs is basically impossible to do, so
	 * we just alloc what _should_ be the full size of the structure for
	 * the OS and architecture, then pass that around as if that's a real
	 * struct. The size is just what you get from sizeof(OggTheora_File).
	 *
	 * Don't get mad at me, get mad at C#.
	 *
	 * -flibit
	 */

	private static IntPtr AllocTheoraFile()
	{
		// Do not attempt to understand these numbers at all costs!
		const int size32 = 1160;
		const int size64Unix = 1472;
		const int size64Windows = 1328;

		PlatformID platform = Environment.OSVersion.Platform;
		if (IntPtr.Size == 4)
		{
			/* Technically this could be a little bit smaller, but
			 * some 32-bit architectures may be higher even on Unix
			 * targets (like ARMv7).
			 * -flibit
			 */
			return Marshal.AllocHGlobal(size32);
		}
		if (IntPtr.Size == 8)
		{
			if (platform == PlatformID.Unix)
			{
				return Marshal.AllocHGlobal(size64Unix);
			}
			else if (platform == PlatformID.Win32NT)
			{
				return Marshal.AllocHGlobal(size64Windows);
			}
			throw new NotSupportedException("Unhandled platform!");
		}
		throw new NotSupportedException("Unhandled architecture!");
	}

	#endregion
}
