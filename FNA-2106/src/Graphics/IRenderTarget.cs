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
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	/// <summary>
	/// Represents a render target.
	/// </summary>
	internal interface IRenderTarget
	{
		/// <summary>
		/// Gets the width of the render target in pixels
		/// </summary>
		/// <value>The width of the render target in pixels.</value>
		int Width
		{
			get;
		}

		/// <summary>
		/// Gets the height of the render target in pixels
		/// </summary>
		/// <value>The height of the render target in pixels.</value>
		int Height
		{
			get;
		}

		/// <summary>
		/// Gets the mip level count of the render target
		/// </summary>
		/// <value>The number of mip levels in the render target.</value>
		int LevelCount
		{
			get;
		}

		/// <summary>
		/// Gets the usage mode of the render target.
		/// </summary>
		/// <value>The usage mode of the render target.</value>
		RenderTargetUsage RenderTargetUsage
		{
			get;
		}

		/// <summary>
		/// Gets the DepthFormat of the depth-stencil buffer.
		/// </summary>
		/// <value>The DepthFormat of the DepthStencilBuffer.</value>
		DepthFormat DepthStencilFormat
		{
			get;
		}

		/// <summary>
		/// Gets the handle of the depth-stencil buffer.
		/// </summary>
		/// <value>The depth-stencil buffer handle.</value>
		IntPtr DepthStencilBuffer
		{
			get;
		}

		/// <summary>
		/// Gets the handle of the color buffer.
		/// </summary>
		/// <value>The color buffer handle.</value>
		IntPtr ColorBuffer
		{
			get;
		}

		int MultiSampleCount
		{
			get;
		}
	}
}
