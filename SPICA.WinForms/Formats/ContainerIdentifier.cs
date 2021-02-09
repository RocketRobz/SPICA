using SPICA.Formats;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.Generic.CMIF;
using SPICA.Formats.Generic.StudioMdl;
using SPICA.Formats.Generic.WavefrontOBJ;
using SPICA.Formats.GFL;
using SPICA.Formats.GFL.Motion;
using SPICA.Formats.GFL2;
using SPICA.Formats.GFL2.Model;
using SPICA.Formats.GFL2.Motion;
using SPICA.Formats.GFL2.Texture;
using SPICA.Formats.ModelBinary;
using SPICA.Formats.MTFramework.Model;
using SPICA.Formats.MTFramework.Shader;
using SPICA.Formats.MTFramework.Texture;

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SPICA.WinForms.Formats
{
    static class ContainerIdentifier
    {
        public static H3D IdentifyAndOpen(string FileName, H3DDict<H3DBone> Skeleton = null)
        {
            H3D Output = null;

            using (FileStream FS = new FileStream(FileName, FileMode.Open)){
                if (GFPackage.IsValidPackage(FS))
                {
                    GFPackage.Header PackHeader = GFPackage.GetPackageHeader(FS);

                    switch (PackHeader.Magic)
                    {
                        case "AL": Output = GFAreaLOD.OpenAsH3D(FS, PackHeader, 1); break;
                        case "AD": Output = GFPackedTexture.OpenAsH3D(FS, PackHeader, 1); break;
                        //case "BG": Output = GFL2OverWorld.OpenAsH3D(FS, PackHeader, Skeleton); break;
                        case "BS": Output = GFBtlSklAnim.OpenAsH3D(FS, PackHeader, Skeleton); break;
                        case "CM": Output = GFCharaModel.OpenAsH3D(FS, PackHeader); break;
                        case "GR": Output = GFOWMapModel.OpenAsH3D(FS, PackHeader); break;
                        case "MM": Output = GFOWCharaModel.OpenAsH3D(FS, PackHeader); break;
                        case "PC": Output = GFPkmnModel.OpenAsH3D(FS, PackHeader, Skeleton); break;
                        case "LL":
                        default:
                        case "PT": Output = GFPackedTexture.OpenAsH3D(FS, PackHeader, 0); break;
                        case "PK":
                        case "PB":
                            Output = GFPkmnSklAnim.OpenAsH3D(FS, PackHeader, Skeleton); break;
                    }
                }
            }

            if (Output == null)
            {
                Output = FormatIdentifier.IdentifyAndOpen(FileName, Skeleton);
            }

            return Output;
        }
    }
}
