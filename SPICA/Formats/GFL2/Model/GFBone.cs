using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Math3D;
using System.IO;
using System.Numerics;

namespace SPICA.Formats.GFL2.Model
{
    public struct GFBone
    {
        public string Name;
        public string Parent;
        public byte   Flags;

        public Vector3 Scale;
        public Vector3 Rotation;
        public Vector3 Translation;

        public GFBone(BinaryReader Reader)
        {
            Name   = Reader.ReadByteLengthString();
            Parent = Reader.ReadByteLengthString();
            Flags  = Reader.ReadByte();

            Scale       = Reader.ReadVector3();
            Rotation    = Reader.ReadVector3();
            Translation = Reader.ReadVector3();
        }

        public GFBone(H3DBone Bone, H3DDict<H3DBone> Skeleton)
        {
            Flags = (byte)(Bone.ParentIndex == -1 ? 2 : 1);
            Name = Bone.Name;
            Rotation = Bone.Rotation;
            Translation = Bone.Translation;
            Scale = Bone.Scale;
            if (Bone.ParentIndex != -1)
            {
                Parent = Skeleton[Bone.ParentIndex].Name;
            }
            else
            {
                Parent = "Origin";
            }
        }

        public void Write(BinaryWriter Writer)
        {
            Writer.WriteByteLengthString(Name);
            Writer.WriteByteLengthString(Parent);
            Writer.Write(Flags);

            Writer.Write(Scale);
            Writer.Write(Rotation);
            Writer.Write(Translation);
        }
    }
}
