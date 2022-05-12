using SPICA.Formats.Common;
using System.IO;
using System.Text;
using SPICA.Formats.GFL2.Texture;
using SPICA.Formats.CtrH3D;
using System.Collections.Generic;
using System.Linq;

namespace SPICA.WinForms.Formats
{
    class GFAreaLOD
    {
        public static H3D OpenAsH3D(Stream Input, GFPackage.Header Header, int StartIndex)
        {
            H3D Output = new H3D();

            //Textures and animations
            for (int i = StartIndex; i < Header.Entries.Length; i++)
            {
                byte[] Buffer = new byte[Header.Entries[i].Length];

                Input.Seek(Header.Entries[i].Address, SeekOrigin.Begin);

                Input.Read(Buffer, 0, Buffer.Length);

                Stream Input2 = new MemoryStream(Buffer);

                GFPackage.Header h = GFPackage.GetPackageHeader(Input2);

                for (int j = 0; j < h.Entries.Length; j++) {
                    byte[] Buffer2 = new byte[h.Entries[j].Length];

                    Input2.Seek(h.Entries[j].Address, SeekOrigin.Begin);

                    Input2.Read(Buffer2, 0, Buffer2.Length);
                    using (MemoryStream MS = new MemoryStream(Buffer2))
                    {
                        Output.Merge(H3D.Open(MS));
                    }
                }
            }

            return Output;
        }
	}
}
