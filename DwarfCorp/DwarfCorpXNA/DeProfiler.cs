using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
/*using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;*/
using Microsoft.Xna.Framework.Graphics;
/*using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;*/
//using Microsoft.Xna.Framework.Storage;
//using DustAetPatchingPlatform;
using NativeAssemblerInjection;
using System.Windows.Forms;

namespace fbDeprofiler
{
    internal class GraphicsAdapterReplacement
    {
        public bool IsProfileSupported(GraphicsProfile profile)
        {
            return true;
        }

        //[return: MarshalAs(UnmanagedType.U1)]
        internal bool IsProfileSupported(Enum deviceType, GraphicsProfile graphicsProfile)
        {
            return true;
        }
    }

    public class DeProfiler
    {
        public static void Run()
        {
            // I think at a lower level this call uses the one below so may not be necessary
            MethodBase checkMethod = typeof(GraphicsAdapter).GetMethod("IsProfileSupported", BindingFlags.Instance | BindingFlags.Public);
            MethodBase replaceCheckMethod = typeof(GraphicsAdapterReplacement).GetMethod("IsProfileSupported", BindingFlags.Instance | BindingFlags.Public);
            MethodUtil.ReplaceMethod(replaceCheckMethod, checkMethod);

            // Disable the profile capabilities check
            MethodBase profileMethod = typeof(GraphicsAdapter).GetMethod("IsProfileSupported", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodBase replaceProfileMethod = typeof(GraphicsAdapterReplacement).GetMethod("IsProfileSupported", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodUtil.ReplaceMethod(replaceProfileMethod, profileMethod);
        }

        public static void Load()
        {
            //StorageDevice device = StorageDevice.EndShowSelector(StorageDevice.BeginShowSelector(PlayerIndex.One, null, null));
            //using (StorageContainer container = device.EndOpenContainer(device.BeginOpenContainer("PatchingPlatform", null, null)))
            //{
            //    if (container.FileExists("deprofile.flg"))
            //    {
            //        Run();
            //        return;
            //    }
            //}

            if (!GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef))
            {
                DialogResult result = MessageBox.Show(
                    "Your graphics adapter does not support the HiDef profile. That means not all of the features of the game are supported on by graphics card. Would you like to continue anyway? (WARNING: this may lead to crashes/instability).",
                    "HIDEF Not Supported!",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Cancel) return;
                Run();
//                if (result == DialogResult.Yes)
//                {
//                    using (StorageContainer container = device.EndOpenContainer(device.BeginOpenContainer("PatchingPlatform", null, null)))
//                    {
//#pragma warning disable 0642
//                        using (System.IO.Stream stream = container.CreateFile("deprofile.flg")) ;
//                    }
//                }
            }
        }
    }
}
