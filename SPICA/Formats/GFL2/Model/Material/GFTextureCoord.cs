using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Math3D;

using System.IO;
using System.Numerics;

namespace SPICA.Formats.GFL2.Model.Material
{
    public struct GFTextureCoord : INamed
    {
        public string Name { get; set; }

        public byte UnitIndex;

        public GFTextureMappingType MappingType;

        public Vector2 Scale;
        public float   Rotation;
        public Vector2 Translation;

        public GFTextureWrap WrapU;
        public GFTextureWrap WrapV;

        public GFMagFilter MagFilter;
        public GFMinFilter MinFilter;

        public uint MinLOD;

        public GFTextureCoord(BinaryReader Reader)
        {
            Name = new GFHashName(Reader).Name;

            UnitIndex = Reader.ReadByte();

            MappingType = (GFTextureMappingType)Reader.ReadByte();

            Scale       = Reader.ReadVector2();
            Rotation    = Reader.ReadSingle();
            Translation = Reader.ReadVector2();

            WrapU = (GFTextureWrap)Reader.ReadUInt32();
            WrapV = (GFTextureWrap)Reader.ReadUInt32();

            MagFilter = (GFMagFilter)Reader.ReadUInt32(); //Not sure
            MinFilter = (GFMinFilter)Reader.ReadUInt32(); //Not sure

            MinLOD = Reader.ReadUInt32(); //Not sure
        }

        public GFTextureCoord(H3DTextureCoord Coord, H3DTextureMapper Mapper, string TextureName, byte UnitIndex)
        {
            Name = TextureName;
            
            this.UnitIndex = UnitIndex;
            
            MappingType = (GFTextureMappingType)Coord.MappingType;
            
            Scale = Coord.Scale;
            Rotation = Coord.Rotation;
            Translation = Coord.Translation;
            
            WrapU = (GFTextureWrap)Mapper.WrapU;
            WrapV = (GFTextureWrap)Mapper.WrapV;
            
            MagFilter = (GFMagFilter)Mapper.MagFilter;
            MinFilter = (GFMinFilter)Mapper.MinFilter;
            
            MinLOD = Mapper.MinLOD;
        }

        public Matrix3x4 GetTransform()
        {
            return TextureTransform.GetTransform(
                Scale,
                Rotation,
                Translation,
                TextureTransformType.DccMaya);
        }

        public void Write(BinaryWriter Writer)
        {
            new GFHashName(Name).Write(Writer);

            Writer.Write(UnitIndex);

            Writer.Write((byte)MappingType);

            Writer.Write(Scale);
            Writer.Write(Rotation);
            Writer.Write(Translation);

            Writer.Write((uint)WrapU);
            Writer.Write((uint)WrapV);

            Writer.Write((uint)MagFilter);
            Writer.Write((uint)MinFilter);

            Writer.Write(MinLOD);
        }
    }
}
