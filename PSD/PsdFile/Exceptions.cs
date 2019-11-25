/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is perovided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2012 Tao Yue
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PhotoshopFile
{
  [Serializable]
  public class PsdInvalidException : Exception
  {
    public PsdInvalidException()
    {
    }

    public PsdInvalidException(string message)
      : base(message)
    {
    }
  }

  [Serializable]
  public class RleException : Exception
  {
    public RleException() { }

    public RleException(string message) : base(message) { }
  }
}
