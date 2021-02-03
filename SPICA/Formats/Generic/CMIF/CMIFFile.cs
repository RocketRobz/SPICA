using SPICA.Formats.Common;
using SPICA.Math3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.PICA.Commands;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Converters;
using System.Numerics;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Texture;
using System.Drawing;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.LUT;

namespace SPICA.Formats.Generic.CMIF
{
    public class CMIFFile
    {
        public const string CMIF_MAGIC = "CMIF";
        public const int READER_VERSION = 9;

        public List<H3DModel> models = new List<H3DModel>();
        public List<H3DTexture> textures = new List<H3DTexture>();
        public List<H3DLUT> LUTs = new List<H3DLUT>();
        public List<H3DAnimation> skeletalAnimations = new List<H3DAnimation>();
        public List<H3DMaterialAnim> materialAnimations = new List<H3DMaterialAnim>();

        public CMIFFile(Stream s)
        {
            BinaryReader dis = new BinaryReader(s);
            if (dis.ReadPaddedString(4) != CMIF_MAGIC)
            {
                throw new ArgumentException("Invalid magic.");
            }
            uint version = dis.ReadUInt32();
            uint backwardsCompatibility = version;
            if (version >= 3)
            {
                backwardsCompatibility = dis.ReadUInt32();
            }
            if (backwardsCompatibility > READER_VERSION)
            {
                throw new NotSupportedException("File version is too new! - " + backwardsCompatibility);
            }

            uint stringTableOffset = dis.ReadUInt32();
            uint contentTableOffset = dis.ReadUInt32();

            dis.BaseStream.Seek(contentTableOffset, SeekOrigin.Begin);

            uint modelsPointerTableOffset = dis.ReadUInt32();
            uint texturesPointerTableOffset = dis.ReadUInt32();
            uint sklAnmPointerTableOffset = dis.ReadUInt32();
            uint matAnmPointerTableOffset = dis.ReadUInt32();
            uint othersPointerTableOffset = 0xFFFFFFFF;
            if (version >= 2)
            {
                othersPointerTableOffset = dis.ReadUInt32();
            }

            dis.BaseStream.Seek(modelsPointerTableOffset, SeekOrigin.Begin);
            PointerTable modelsPT = new PointerTable(dis);

            while (modelsPT.hasNext())
            {
                modelsPT.next(dis);
                models.Add(readModel(dis, version));
            }

            dis.BaseStream.Seek(texturesPointerTableOffset, SeekOrigin.Begin);
            PointerTable texPT = new PointerTable(dis);

            while (texPT.hasNext())
            {
                texPT.next(dis);
                doReadTexture(dis, version);
            }

            dis.BaseStream.Seek(sklAnmPointerTableOffset, SeekOrigin.Begin);
            PointerTable saPT = new PointerTable(dis);

            while (saPT.hasNext())
            {
                saPT.next(dis);
                skeletalAnimations.Add(readSkeletalAnime(dis));
            }

            dis.BaseStream.Seek(matAnmPointerTableOffset, SeekOrigin.Begin);
            PointerTable maPT = new PointerTable(dis);

            while (maPT.hasNext())
            {
                maPT.next(dis);
                materialAnimations.Add(readMaterialAnime(dis));
            }

            dis.Close();

            setMaterialLUTData();
        }

        void setMaterialLUTData()
        {
            foreach (H3DModel model in models)
            {
                foreach (H3DMaterial mat in model.Materials)
                {
                    mat.MaterialParams.LUTReflecRTableName = getLUTName(mat.MaterialParams.LUTReflecRSamplerName);
                    mat.MaterialParams.LUTReflecGTableName = getLUTName(mat.MaterialParams.LUTReflecGSamplerName);
                    mat.MaterialParams.LUTReflecBTableName = getLUTName(mat.MaterialParams.LUTReflecBSamplerName);
                    mat.MaterialParams.LUTDist0TableName = getLUTName(mat.MaterialParams.LUTDist0TableName);
                    mat.MaterialParams.LUTDist1TableName = getLUTName(mat.MaterialParams.LUTDist1TableName);
                    mat.MaterialParams.LUTFresnelTableName = getLUTName(mat.MaterialParams.LUTFresnelSamplerName);
                    mat.MaterialParams.LUTReflecRSamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTReflecRSamplerName);
                    mat.MaterialParams.LUTReflecGSamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTReflecGSamplerName);
                    mat.MaterialParams.LUTReflecBSamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTReflecBSamplerName);
                    mat.MaterialParams.LUTDist0SamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTDist0SamplerName);
                    mat.MaterialParams.LUTDist1SamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTDist1SamplerName);
                    mat.MaterialParams.LUTFresnelSamplerName = getRealLUTSamplerName(mat.MaterialParams.LUTFresnelSamplerName);
                    mat.MaterialParams.LUTInputAbsolute.ReflecR = (getLUTSampler(mat.MaterialParams.LUTReflecRTableName, mat.MaterialParams.LUTReflecRSamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                    mat.MaterialParams.LUTInputAbsolute.ReflecG = (getLUTSampler(mat.MaterialParams.LUTReflecGTableName, mat.MaterialParams.LUTReflecGSamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                    mat.MaterialParams.LUTInputAbsolute.ReflecB = (getLUTSampler(mat.MaterialParams.LUTReflecBTableName, mat.MaterialParams.LUTReflecBSamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                    mat.MaterialParams.LUTInputAbsolute.Dist0 = (getLUTSampler(mat.MaterialParams.LUTDist0TableName, mat.MaterialParams.LUTDist0SamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                    mat.MaterialParams.LUTInputAbsolute.Dist1 = (getLUTSampler(mat.MaterialParams.LUTDist1TableName, mat.MaterialParams.LUTDist1SamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                    mat.MaterialParams.LUTInputAbsolute.Fresnel = (getLUTSampler(mat.MaterialParams.LUTFresnelTableName, mat.MaterialParams.LUTFresnelSamplerName).Flags & H3DLUTFlags.IsAbsolute) > 0;
                }
            }
        }

        string getRealLUTSamplerName(string samplerName)
        {
            if (samplerName == null)
            {
                return null;
            }
            foreach (H3DLUT lut in LUTs)
            {
                foreach (H3DLUTSampler samp in lut.Samplers)
                {
                    if (samp.Name.Equals(samplerName))
                    {
                        return samplerName;
                    }
                    else if (samplerName.Equals(lut.Name + "_" + samp.Name))
                    {
                        return samp.Name;
                    }
                }
            }
            return samplerName;
        }

        H3DLUTSampler getLUTSampler(string tableName, string realSamplerName)
        {
            foreach (H3DLUT lut in LUTs)
            {
                if (lut.Name.Equals(tableName))
                {
                    foreach (H3DLUTSampler samp in lut.Samplers)
                    {
                        if (samp.Name.Equals(realSamplerName))
                        {
                            return samp;
                        }
                    }
                }
            }
            return new H3DLUTSampler();
        }
        string getLUTName(string samplerName)
        {
            if (samplerName == null)
            {
                return null;
            }
            foreach (H3DLUT lut in LUTs)
            {
                foreach (H3DLUTSampler samp in lut.Samplers)
                {
                    if (samp.Name.Equals(samplerName))
                    {
                        return lut.Name;
                    }
                    else if (samplerName.Equals(lut.Name + "_" + samp.Name))
                    {
                        return lut.Name;
                    }
                }
            }
            return "LookupTableSetContentCtrName";
        }

        public const string MAT_ANIME_MAGIC = "IFMA";
        public const string SKL_ANIM_MAGIC = "IFSA";

        static H3DAnimation readSkeletalAnime(BinaryReader dis)
        {
            H3DAnimation a = new H3DAnimation();
            a.AnimationType = H3DAnimationType.Skeletal;

            if (dis.ReadPaddedString(4) != SKL_ANIM_MAGIC)
            {
                throw new ArgumentException("Invalid skeletal animation magic.");
            }

            readAnimeCommonHeader(dis, a);

            int bonesCount = dis.ReadUInt16();
            for (int bone = 0; bone < bonesCount; bone++)
            {
                H3DAnimationElement e = new H3DAnimationElement();
                e.TargetType = H3DTargetType.Bone;
                e.PrimitiveType = H3DPrimitiveType.Transform;
                e.Name = readStringFromOffset(dis);

                H3DAnimTransform bt = new H3DAnimTransform();

                int elemCount = dis.ReadByte();
                for (int i = 0; i < elemCount; i++)
                {
                    SklAnimKeyFrameType type = (SklAnimKeyFrameType)dis.ReadByte();

                    H3DFloatKeyFrameGroup kfg = new H3DFloatKeyFrameGroup();

                    switch (type)
                    {
                        case SklAnimKeyFrameType.TX:
                            kfg = bt.TranslationX;
                            break;
                        case SklAnimKeyFrameType.TY:
                            kfg = bt.TranslationY;
                            break;
                        case SklAnimKeyFrameType.TZ:
                            kfg = bt.TranslationZ;
                            break;
                        case SklAnimKeyFrameType.RX:
                            kfg = bt.RotationX;
                            break;
                        case SklAnimKeyFrameType.RY:
                            kfg = bt.RotationY;
                            break;
                        case SklAnimKeyFrameType.RZ:
                            kfg = bt.RotationZ;
                            break;
                        case SklAnimKeyFrameType.SX:
                            kfg = bt.ScaleX;
                            break;
                        case SklAnimKeyFrameType.SY:
                            kfg = bt.ScaleY;
                            break;
                        case SklAnimKeyFrameType.SZ:
                            kfg = bt.ScaleZ;
                            break;
                    }

                    readFloatKeyFrameGroup(dis, kfg, a.FramesCount, i);
                }

                e.Content = bt;
                a.Elements.Add(e);
            }

            return a;
        }

        private enum SklAnimKeyFrameType
        {
            TX,
            TY,
            TZ,
            RX,
            RY,
            RZ,
            SX,
            SY,
            SZ
        }

        static H3DMaterialAnim readMaterialAnime(BinaryReader dis)
        {
            H3DMaterialAnim a = new H3DMaterialAnim();
            a.AnimationType = H3DAnimationType.Material;

            if (dis.ReadPaddedString(4) != MAT_ANIME_MAGIC)
            {
                throw new ArgumentException("Invalid material animation magic.");
            }

            readAnimeCommonHeader(dis, a);

            int bonesCount = dis.ReadUInt16();

            ushort curveIndex = 0;

            for (int i = 0; i < bonesCount; i++)
            {
                string boneName = readStringFromOffset(dis);

                //Some elements have to be created beforehand since ST values are not written as vectors in CMIF
                H3DAnimationElement[] tra = new H3DAnimationElement[3];
                H3DAnimationElement[] sca = new H3DAnimationElement[3];

                for (int n = 0; n < 3; n++)
                {
                    H3DAnimationElement tn = new H3DAnimationElement()
                    {
                        Name = boneName,
                        TargetType = H3DTargetType.MaterialTexCoord0Trans + (ushort)(n * 3),
                        PrimitiveType = H3DPrimitiveType.Vector2D,
                        Content = new H3DAnimVector2D()
                    };
                    tra[n] = tn;

                    H3DAnimationElement sn = new H3DAnimationElement()
                    {
                        Name = boneName,
                        TargetType = H3DTargetType.MaterialTexCoord0Scale + (ushort)(n * 3),
                        PrimitiveType = H3DPrimitiveType.Vector2D,
                        Content = new H3DAnimVector2D()
                    };
                    sca[n] = sn;
                }
                List<H3DAnimationElement> extraElements = new List<H3DAnimationElement>();

                int elemCount = dis.ReadByte();
                for (int elem = 0; elem < elemCount; elem++)
                {
                    int uvIndex = dis.ReadByte();

                    MatAnimKeyFrameType type = (MatAnimKeyFrameType)dis.ReadByte();
                    H3DAnimationElement e = new H3DAnimationElement()
                    {
                        Name = boneName
                    };
                    H3DFloatKeyFrameGroup kfg = new H3DFloatKeyFrameGroup();

                    bool vecIsY = false;

                    switch (type)
                    {
                        case MatAnimKeyFrameType.R:
                            e.PrimitiveType = H3DPrimitiveType.Float;
                            e.TargetType = H3DTargetType.MaterialTexCoord0Rot + (ushort)(uvIndex * 3);
                            H3DAnimFloat rContent = new H3DAnimFloat();
                            e.Content = rContent;
                            kfg = rContent.Value;
                            extraElements.Add(e);
                            break;
                        case MatAnimKeyFrameType.SX:
                            e = sca[uvIndex];
                            goto case MatAnimKeyFrameType.SUPPLEMENTARY_VEC2;
                        case MatAnimKeyFrameType.SY:
                            e = sca[uvIndex];
                            vecIsY = true;
                            goto case MatAnimKeyFrameType.SUPPLEMENTARY_VEC2;
                        case MatAnimKeyFrameType.TX:
                            e = tra[uvIndex];
                            goto case MatAnimKeyFrameType.SUPPLEMENTARY_VEC2;
                        case MatAnimKeyFrameType.TY:
                            e = tra[uvIndex];
                            vecIsY = true;
                            goto case MatAnimKeyFrameType.SUPPLEMENTARY_VEC2;
                        case MatAnimKeyFrameType.SUPPLEMENTARY_VEC2:
                            e.PrimitiveType = H3DPrimitiveType.Vector2D;
                            H3DAnimVector2D vecContent = (H3DAnimVector2D)e.Content;
                            kfg = vecIsY ? vecContent.Y : vecContent.X;
                            break;
                        case MatAnimKeyFrameType.TEX:
                            e.PrimitiveType = H3DPrimitiveType.Texture;
                            e.TargetType = H3DTargetType.MaterialMapper0Texture + (ushort)(uvIndex * 2);
                            H3DAnimFloat tContent = new H3DAnimFloat();
                            e.Content = tContent;
                            kfg = tContent.Value;
                            extraElements.Add(e);
                            break;
                    }


                    switch (type)
                    {
                        case MatAnimKeyFrameType.TEX:
                            kfg.CurveIndex = curveIndex;
                            kfg.StartFrame = 0;
                            kfg.EndFrame = a.FramesCount;
                            kfg.InterpolationType = H3DInterpolationType.Step;

                            int size = dis.ReadUInt16();
                            for (int n = 0; n < size; n++)
                            {
                                float frame = dis.ReadSingle();
                                string name = readStringFromOffset(dis);
                                int index = a.TextureNames.IndexOf(name);
                                if (index == -1)
                                {
                                    index = a.TextureNames.Count();
                                    a.TextureNames.Add(name);
                                }
                                kfg.KeyFrames.Add(new KeyFrame(frame, index));
                            }
                            break;
                        default:
                            readFloatKeyFrameGroup(dis, kfg, a.FramesCount, curveIndex);
                            break;
                    }

                    curveIndex++;
                }

                a.Elements.AddRange(extraElements);
                for (int j = 0; j < 3; j++)
                {
                    H3DAnimVector2D scaChk = (H3DAnimVector2D)sca[j].Content;
                    if (scaChk.X.KeyFrames.Count > 0 || scaChk.Y.KeyFrames.Count > 0)
                    {
                        a.Elements.Add(sca[j]);
                    }
                    H3DAnimVector2D traChk = (H3DAnimVector2D)tra[j].Content;
                    if (traChk.X.KeyFrames.Count > 0 || traChk.Y.KeyFrames.Count > 0)
                    {
                        a.Elements.Add(tra[j]);
                    }
                }
            }

            return a;
        }

        private enum MatAnimKeyFrameType
        {
            TX,
            TY,
            R,
            SX,
            SY,
            TEX,
            SUPPLEMENTARY_VEC2
        }

        static void readAnimeCommonHeader(BinaryReader dis, H3DAnimation a)
        {
            a.Name = readStringFromOffset(dis);
            a.FramesCount = dis.ReadSingle();
            if (dis.ReadBoolean())
            {
                a.AnimationFlags = H3DAnimationFlags.IsLooping;
            }
        }

        static H3DFloatKeyFrameGroup readFloatKeyFrameGroup(BinaryReader dis, H3DFloatKeyFrameGroup g, float framesCount, int curveIndex)
        {
            g.InterpolationType = (H3DInterpolationType)dis.ReadByte();
            g.StartFrame = 0;
            g.EndFrame = framesCount;
            g.CurveIndex = (ushort)curveIndex;
            int size = dis.ReadUInt16();

            for (int i = 0; i < size; i++)
            {
                KeyFrame kf = new KeyFrame();
                switch (g.InterpolationType)
                {
                    case H3DInterpolationType.Step:
                    case H3DInterpolationType.Linear:
                        kf = new KeyFrame(dis.ReadSingle(), dis.ReadSingle());
                        break;
                    case H3DInterpolationType.Hermite:
                        kf = new KeyFrame(dis.ReadSingle(), dis.ReadSingle(), dis.ReadSingle(), dis.ReadSingle());
                        break;
                }
                g.KeyFrames.Add(kf);
            }

            return g;
        }

        public H3D ToH3D()
        {
            H3D output = new H3D();

            foreach (H3DModel m in models)
            {
                output.Models.Add(m);
                output.CopyMaterials();
            }

            foreach (H3DTexture t in textures)
            {
                output.Textures.Add(t);
            }

            foreach (H3DAnimation sklanm in skeletalAnimations)
            {
                output.SkeletalAnimations.Add(sklanm);
            }

            foreach (H3DMaterialAnim matanm in materialAnimations)
            {
                output.MaterialAnimations.Add(matanm);
            }
            foreach (H3DLUT lut in LUTs)
            {
                output.LUTs.Add(lut);
            }

            return output;
        }

        public const string TEXTURE_MAGIC = "IFTX";

        void doReadTexture(BinaryReader dis, uint fileVersion)
        {
            if (dis.ReadPaddedString(4) != TEXTURE_MAGIC)
            {
                throw new ArgumentException("Invalid texture magic.");
            }
            string name = readStringFromOffset(dis);
            int width = dis.ReadUInt16();
            int height = dis.ReadUInt16();
            byte[] Buffer = readLZ(dis, fileVersion);

            byte[] Output = new byte[Buffer.Length];

            H3DMetaData metaData;
            if (fileVersion >= 6)
            {
                metaData = readMetaData(dis);
            }
            else
            {
                metaData = new H3DMetaData();
            }

            int Stride = width * 4;

            PICATextureFormat Format = PICATextureFormat.ETC1;
            if (metaData.Contains("DesiredTextureFormat"))
            {
                CMIFTextureFormat CMIFFormat = (CMIFTextureFormat)metaData[metaData.Find("DesiredTextureFormat")].Values[0];
                switch (CMIFFormat)
                {
                    case CMIFTextureFormat.ETC1:
                        Format = PICATextureFormat.ETC1;
                        break;
                    case CMIFTextureFormat.RGB_A:
                        Format = PICATextureFormat.RGB8;
                        break;
                    case CMIFTextureFormat.RGB565_5A1:
                        Format = PICATextureFormat.RGB565;
                        break;
                }
            }

            bool FoundAlpha = false;

            for (int Y = 0; Y < height; Y++)
            {
                int IOffs = Stride * Y;
                int OOffs = Stride * (fileVersion >= 4 ? Y : (height - 1 - Y));

                for (int X = 0; X < width; X++)
                {
                    Output[OOffs + 0] = Buffer[IOffs + 2];
                    Output[OOffs + 1] = Buffer[IOffs + 1];
                    Output[OOffs + 2] = Buffer[IOffs + 0];
                    Output[OOffs + 3] = Buffer[IOffs + 3];
                    if (!FoundAlpha && Output[OOffs + 3] != 255)
                    {
                        switch (Format)
                        {
                            case PICATextureFormat.RGB565:
                                Format = PICATextureFormat.RGBA5551;
                                break;
                            case PICATextureFormat.ETC1:
                                Format = PICATextureFormat.ETC1A4;
                                break;
                            case PICATextureFormat.RGB8:
                                Format = PICATextureFormat.RGBA8;
                                break;
                        }
                        FoundAlpha = true;
                    }

                    IOffs += 4;
                    OOffs += 4;
                }
            }

            Bitmap bmp = TextureConverter.GetBitmap(Output, width, height);

            bool TexAsLUT = false;
            if (metaData.Contains("CreativeStudio_TexAsLUT"))
            {
                TexAsLUT = (int)metaData["CreativeStudio_TexAsLUT"].Values[0] > 0;
            }
            if (!TexAsLUT)
            {
                H3DTexture tex = new H3DTexture(name, bmp, Format);
                textures.Add(tex);
            }
            else
            {
                string lutName = "LookupTableSetContentCtrName";
                if (metaData.Contains("H3DLookUpTableSetName"))
                {
                    lutName = (string)metaData["H3DLookUpTableSetName"].Values[0];
                }
                H3DLUT lut = getLUT(lutName);
                H3DLUTSampler sampler = new H3DLUTSampler();
                sampler.Name = name;
                string incName = lutName + "_";
                if (sampler.Name.StartsWith(incName))
                {
                    sampler.Name = sampler.Name.Substring(incName.Length);
                }
                bool asAbsolute = true;
                int absCmp = -1;
                for (int i = bmp.Width / 2; i < bmp.Width; i++)
                {
                    int r = bmp.GetPixel(i, 0).R;
                    if (absCmp == -1)
                    {
                        absCmp = r;
                    }
                    else
                    {
                        if (absCmp != r)
                        {
                            asAbsolute = false;
                            break;
                        }
                    }
                }

                float[] table = new float[256];
                int inputLength = bmp.Width;

                if (asAbsolute)
                {
                    inputLength = bmp.Width / 2;
                    sampler.Flags = H3DLUTFlags.IsAbsolute;
                }
                for (int i = 0; i < inputLength; i++)
                {
                    int tableOffs = (int)(i * (256f / inputLength));
                    table[tableOffs] = bmp.GetPixel(i, 0).R / 255f;
                    if (asAbsolute)
                    {
                        table[tableOffs + 1] = table[tableOffs];
                    }
                }
                sampler.Table = table;
                lut.Samplers.Add(sampler);
            }
        }

        H3DLUT getLUT(string name)
        {
            foreach (H3DLUT l in LUTs)
            {
                if (l.Name.Equals(name))
                {
                    return l;
                }
            }
            H3DLUT lut = new H3DLUT();
            lut.Name = name;
            LUTs.Add(lut);
            return lut;
        }

        public static byte[] readLZ(BinaryReader dis, uint fileVersion)
        {
            uint len = dis.ReadUInt32();
            bool decompressLZSS = (fileVersion >= 7 && ((len >> 31 & 1) > 0));
            len &= 0x7FFFFFFF;
            byte[] data = dis.ReadBytes((int)len);
            if (decompressLZSS)
            {
                data = Common.Compression.LZ11.decompress(data);
            }
            return data;
        }

        public const string MODEL_MAGIC = "IFMD";
        public const string SKELETON_JOINT_MAGIC = "SKLJ";
        public const string MESH_MAGIC = "IFPL";
        public const string MATERIAL_MAGIC = "IFMT";
        public const string TEVCONF_MAGIC = "TENV";

        static H3DModel readModel(BinaryReader dis, uint fileVersion)
        {
            H3DModel m = new H3DModel();

            if (dis.ReadPaddedString(4) != MODEL_MAGIC)
            {
                throw new ArgumentException("Invalid model magic.");
            }
            m.Name = readStringFromOffset(dis);
            if (fileVersion >= 4)
            {
                m.MetaData = readMetaData(dis);
            }
            else
            {
                m.MetaData = new H3DMetaData();
            }

            //these pointer tables are condensed - no pointers to them
            PointerTable bonesPT = new PointerTable(dis);
            PointerTable meshesPT = new PointerTable(dis);
            PointerTable matsPT = new PointerTable(dis);

            if (bonesPT.hasNext())
            {
                m.Flags = H3DModelFlags.HasSkeleton;
            }

            m.BoneScaling = H3DBoneScaling.Maya;

            while (bonesPT.hasNext())
            {
                bonesPT.next(dis);

                if (dis.ReadPaddedString(4) != SKELETON_JOINT_MAGIC)
                {
                    throw new ArgumentException("Invalid joint magic.");
                }

                H3DBone bone = new H3DBone();

                bone.Name = readStringFromOffset(dis);
                bone.ParentIndex = (short)dis.ReadUInt32();
                bone.Translation = VectorExtensions.ReadVector3(dis);
                bone.Rotation = VectorExtensions.ReadVector3(dis);
                bone.Scale = VectorExtensions.ReadVector3(dis);

                m.Skeleton.Add(bone);
            }

            foreach (H3DBone b in m.Skeleton)
            {
                b.CalculateTransform(m.Skeleton);
            }

            while (matsPT.hasNext())
            {
                matsPT.next(dis);

                if (dis.ReadPaddedString(4) != MATERIAL_MAGIC)
                {
                    throw new ArgumentException("Invalid material magic.");
                }

                string matName = readStringFromOffset(dis);

                string shaderName = "DefaultShader";
                byte shaderIndex = 0;
                byte lightSetIndex = 0;
                RGBA ambientColor = RGBA.White;
                RGBA diffuseColor = RGBA.White;
                RGBA specular0Color = RGBA.Black;
                RGBA specular1Color = RGBA.Black;
                byte translucencyKind = 0;
                if (fileVersion >= 5)
                {
                    shaderName = readStringFromOffset(dis);
                    shaderIndex = dis.ReadByte();
                }
                if (fileVersion >= 8)
                {
                    lightSetIndex = dis.ReadByte();
                    ambientColor = new RGBA(dis);
                    diffuseColor = new RGBA(dis);
                }
                if (fileVersion >= 9)
                {
                    specular0Color = new RGBA(dis);
                    specular1Color = new RGBA(dis);
                    translucencyKind = dis.ReadByte();
                }

                int textureCount = dis.ReadByte();
                string tex0Name = textureCount > 0 ? readStringFromOffset(dis) : null;

                H3DMaterial mat = H3DMaterial.GetSimpleMaterial(m.Name, matName, tex0Name, shaderName, shaderIndex);
                mat.MaterialParams.Flags = H3DMaterialFlags.IsFragmentLightingEnabled;
                //mat.MaterialParams.FaceCulling = PICAFaceCulling.BackFace;
                //mat.MaterialParams.FragmentFlags = H3DFragmentFlags.IsLUTReflectionEnabled;
                //mat.MaterialParams.LUTReflecRTableName = "LookupTableSetContentCtrName";
                //mat.MaterialParams.LUTReflecRSamplerName = "Toontable.tga";
                //mat.MaterialParams.ShaderReference = "0@FieldChar";
                mat.MaterialParams.LightSetIndex = lightSetIndex;
                mat.MaterialParams.EmissionColor = RGBA.Black;
                mat.MaterialParams.Specular0Color = specular0Color;
                mat.MaterialParams.Specular1Color = specular1Color;
                mat.MaterialParams.AmbientColor = ambientColor;
                mat.MaterialParams.DiffuseColor = diffuseColor;
                mat.MaterialParams.TranslucencyKind = (H3DTranslucencyKind)translucencyKind;

                float[] uvPtrs = new float[4];

                for (int i = 0; i < textureCount; i++)
                {
                    switch (i)
                    {
                        case 1:
                            mat.EnabledTextures[1] = true;
                            mat.Texture1Name = readStringFromOffset(dis);
                            break;
                        case 2:
                            mat.EnabledTextures[2] = true;
                            mat.Texture2Name = readStringFromOffset(dis);
                            break;
                    }

                    uvPtrs[i] = dis.ReadByte();

                    mat.MaterialParams.TextureCoords[i].Translation = VectorExtensions.ReadVector2(dis);
                    mat.MaterialParams.TextureCoords[i].Rotation = dis.ReadSingle();
                    mat.MaterialParams.TextureCoords[i].Scale = VectorExtensions.ReadVector2(dis);

                    mat.TextureMappers[i].WrapU = (PICATextureWrap)dis.ReadByte();
                    mat.TextureMappers[i].WrapV = (PICATextureWrap)dis.ReadByte();
                    mat.TextureMappers[i].MagFilter = (H3DTextureMagFilter)dis.ReadByte();
                    mat.TextureMappers[i].MinFilter = (H3DTextureMinFilter)dis.ReadByte();
                }

                mat.MaterialParams.TextureSources = uvPtrs;

                if (fileVersion >= 9)
                {
                    int lutCount = dis.ReadByte();
                    for (int i = 0; i < lutCount; i++)
                    {
                        CMIFLUTName name = (CMIFLUTName)dis.ReadByte();
                        PICALUTInput input = (PICALUTInput)dis.ReadByte();
                        string LUTSamplerName = readStringFromOffset(dis);

                        switch (name)
                        {
                            case CMIFLUTName.REFLEC_R:
                                mat.MaterialParams.LUTReflecRSamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.ReflecR = input;
                                mat.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTReflectionEnabled;
                                break;
                            case CMIFLUTName.REFLEC_G:
                                mat.MaterialParams.LUTReflecGSamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.ReflecG = input;
                                mat.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTReflectionEnabled;
                                break;
                            case CMIFLUTName.REFLEC_B:
                                mat.MaterialParams.LUTReflecBSamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.ReflecB = input;
                                mat.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTReflectionEnabled;
                                break;
                            case CMIFLUTName.DIST_0:
                                mat.MaterialParams.LUTDist0SamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.Dist0 = input;
                                break;
                            case CMIFLUTName.DIST_1:
                                mat.MaterialParams.LUTDist1SamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.Dist1 = input;
                                break;
                            case CMIFLUTName.FRESNEL:
                                mat.MaterialParams.LUTFresnelSamplerName = LUTSamplerName;
                                mat.MaterialParams.LUTInputSelection.Fresnel = input;
                                break;
                        }
                    }
                }

                //Depth test
                bool depthOpEnabled = dis.ReadBoolean();
                mat.MaterialParams.DepthColorMask.Enabled = depthOpEnabled;
                if (depthOpEnabled)
                {
                    byte depthOpByte = dis.ReadByte();
                    mat.MaterialParams.DepthColorMask.DepthFunc = (PICATestFunc)(depthOpByte & 7);
                    mat.MaterialParams.DepthColorMask.RedWrite = (depthOpByte & 8) > 0;
                    mat.MaterialParams.DepthColorMask.GreenWrite = (depthOpByte & 16) > 0;
                    mat.MaterialParams.DepthColorMask.BlueWrite = (depthOpByte & 32) > 0;
                    mat.MaterialParams.DepthColorMask.AlphaWrite = (depthOpByte & 64) > 0;
                    mat.MaterialParams.DepthColorMask.DepthWrite = (depthOpByte & 128) > 0;
                }

                //Alpha test
                byte alphaTestByte = dis.ReadByte();
                mat.MaterialParams.AlphaTest.Enabled = (alphaTestByte & 128) > 0;
                mat.MaterialParams.AlphaTest.Function = (PICATestFunc)(alphaTestByte & 7);
                mat.MaterialParams.AlphaTest.Reference = dis.ReadByte();

                //Blend config
                byte blendMasterByte = dis.ReadByte();
                byte blendRgbByte = dis.ReadByte();
                byte blendAlphaByte = dis.ReadByte();

                mat.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;  //nothing to disable it on H3D
                mat.MaterialParams.BlendFunction.ColorEquation = (PICABlendEquation)(blendMasterByte & 7);
                mat.MaterialParams.BlendFunction.AlphaEquation = (PICABlendEquation)((blendMasterByte >> 3) & 7);
                mat.MaterialParams.BlendFunction.ColorSrcFunc = (PICABlendFunc)(blendRgbByte & 15);
                mat.MaterialParams.BlendFunction.ColorDstFunc = (PICABlendFunc)((blendRgbByte >> 4) & 15);
                mat.MaterialParams.BlendFunction.AlphaSrcFunc = (PICABlendFunc)(blendAlphaByte & 15);
                mat.MaterialParams.BlendFunction.AlphaDstFunc = (PICABlendFunc)((blendAlphaByte >> 4) & 15);
                mat.MaterialParams.BlendColor = new RGBA(dis);

                //Stencil test
                if (fileVersion >= 4)
                {
                    byte stencilConfig = dis.ReadByte();
                    mat.MaterialParams.StencilTest.Enabled = (stencilConfig & 128) > 0;
                    mat.MaterialParams.StencilTest.Function = (PICATestFunc)(stencilConfig & 0x7F);
                    mat.MaterialParams.StencilTest.Reference = dis.ReadByte();
                    mat.MaterialParams.StencilTest.Mask = dis.ReadByte();
                    mat.MaterialParams.StencilTest.BufferMask = dis.ReadByte();

                    //Stencil operation
                    mat.MaterialParams.StencilOperation.FailOp = (PICAStencilOp)dis.ReadByte();
                    mat.MaterialParams.StencilOperation.ZFailOp = (PICAStencilOp)dis.ReadByte();
                    mat.MaterialParams.StencilOperation.ZPassOp = (PICAStencilOp)dis.ReadByte();
                }

                //Face culling
                if (fileVersion >= 9)
                {
                    mat.MaterialParams.FaceCulling = (PICAFaceCulling)dis.ReadByte();
                }

                //Bump mapping
                if (fileVersion >= 5)
                {
                    byte bumpByte = dis.ReadByte();
                    mat.MaterialParams.BumpMode = (H3DBumpMode)(bumpByte & 3);
                    mat.MaterialParams.BumpTexture = (byte)(bumpByte >> 2 & 3);
                }

                //TexEnv config
                if (dis.ReadPaddedString(4) != TEVCONF_MAGIC)
                {
                    throw new ArgumentException("Invalid TexEnvConfig magic.");
                }
                mat.MaterialParams.TexEnvBufferColor = new RGBA(dis);
                for (int stage = 0; stage < 6; stage++)
                {
                    mat.MaterialParams.TexEnvStages[stage].Color = new RGBA(dis);
                    byte scalingByte = dis.ReadByte();
                    mat.MaterialParams.TexEnvStages[stage].Scale.Color = (PICATextureCombinerScale)(scalingByte & 3);
                    mat.MaterialParams.TexEnvStages[stage].Scale.Alpha = (PICATextureCombinerScale)((scalingByte >> 2) & 3);
                    mat.MaterialParams.TexEnvStages[stage].UpdateColorBuffer = (scalingByte & 64) > 0;
                    mat.MaterialParams.TexEnvStages[stage].UpdateAlphaBuffer = (scalingByte & 128) > 0;

                    mat.MaterialParams.TexEnvStages[stage].Combiner.Color = (PICATextureCombinerMode)dis.ReadByte();
                    mat.MaterialParams.TexEnvStages[stage].Combiner.Alpha = (PICATextureCombinerMode)dis.ReadByte();

                    for (int i = 0; i < getCombinerModeArgumentCount(mat.MaterialParams.TexEnvStages[stage].Combiner.Color); i++)
                    {
                        mat.MaterialParams.TexEnvStages[stage].Source.Color[i] = (PICATextureCombinerSource)dis.ReadByte();
                        mat.MaterialParams.TexEnvStages[stage].Operand.Color[i] = (PICATextureCombinerColorOp)dis.ReadByte();
                    }

                    for (int i = 0; i < getCombinerModeArgumentCount(mat.MaterialParams.TexEnvStages[stage].Combiner.Alpha); i++)
                    {
                        mat.MaterialParams.TexEnvStages[stage].Source.Alpha[i] = (PICATextureCombinerSource)dis.ReadByte();
                        mat.MaterialParams.TexEnvStages[stage].Operand.Alpha[i] = (PICATextureCombinerAlphaOp)dis.ReadByte();
                    }
                }
                mat.MaterialParams.Constant0Color = mat.MaterialParams.TexEnvStages[0].Color;
                mat.MaterialParams.Constant1Color = mat.MaterialParams.TexEnvStages[1].Color;
                mat.MaterialParams.Constant2Color = mat.MaterialParams.TexEnvStages[2].Color;
                mat.MaterialParams.Constant3Color = mat.MaterialParams.TexEnvStages[3].Color;
                mat.MaterialParams.Constant4Color = mat.MaterialParams.TexEnvStages[4].Color;
                mat.MaterialParams.Constant5Color = mat.MaterialParams.TexEnvStages[5].Color;
                mat.MaterialParams.Constant0Assignment = 0;
                mat.MaterialParams.Constant1Assignment = 1;
                mat.MaterialParams.Constant2Assignment = 2;
                mat.MaterialParams.Constant3Assignment = 3;
                mat.MaterialParams.Constant4Assignment = 4;
                mat.MaterialParams.Constant5Assignment = 5;

                if (fileVersion >= 4)
                {
                    mat.MaterialParams.MetaData = readMetaData(dis);
                }
                else
                {
                    mat.MaterialParams.MetaData = new H3DMetaData();
                }

                if (mat.MaterialParams.ShaderReference.Contains("PokePack"))
                {
                    mat.MaterialParams.FresnelSelector = H3DFresnelSelector.Sec;
                }
                //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(mat, Newtonsoft.Json.Formatting.Indented));
                m.Materials.Add(mat);
            }

            List<H3DMesh> meshes = new List<H3DMesh>();

            while (meshesPT.hasNext())
            {
                meshesPT.next(dis);

                if (dis.ReadPaddedString(4) != MESH_MAGIC)
                {
                    throw new ArgumentException("Invalid mesh magic.");
                }

                string meshName = readStringFromOffset(dis);
                string materialName = readStringFromOffset(dis);
                int renderLayer = dis.ReadByte();
                H3DSubMeshSkinning skinning = H3DSubMeshSkinning.Smooth;
                H3DMetaData meshMetaData;
                if (fileVersion >= 4)
                {
                    meshMetaData = readMetaData(dis);
                    byte primitiveType = dis.ReadByte();
                    if (primitiveType > 0)
                    {
                        Console.WriteLine("WARN: PICA only supports triangle primitives. Skipping");
                        continue;
                    }
                }
                else
                {
                    meshMetaData = new H3DMetaData();
                }
                if (fileVersion >= 7)
                {
                    switch (dis.ReadByte() & 3)
                    {
                        case 0:
                            skinning = H3DSubMeshSkinning.None;
                            break;
                        case 1:
                            skinning = H3DSubMeshSkinning.Rigid;
                            break;
                        case 2:
                            skinning = H3DSubMeshSkinning.Smooth;
                            break;
                    }
                }

                m.MeshNodesVisibility.Add(true);
                int nodeIndex = m.MeshNodesTree.Count;
                m.MeshNodesTree.Add(meshName);

                int numAttributes = dis.ReadByte();
                List<VertexAttribute> attribs = new List<VertexAttribute>();

                for (int i = 0; i < numAttributes; i++)
                {
                    byte attrByte = dis.ReadByte();
                    PICAAttributeName name = PICAAttributeName.UserAttribute0;
                    switch ((VertexAttribName)(attrByte & 0xF))
                    {
                        case VertexAttribName.POSITION:
                            name = PICAAttributeName.Position;
                            break;
                        case VertexAttribName.NORMAL:
                            name = PICAAttributeName.Normal;
                            break;
                        case VertexAttribName.COLOR:
                            name = PICAAttributeName.Color;
                            break;
                        case VertexAttribName.UV0:
                            name = PICAAttributeName.TexCoord0;
                            break;
                        case VertexAttribName.UV1:
                            name = PICAAttributeName.TexCoord1;
                            break;
                        case VertexAttribName.UV2:
                            name = PICAAttributeName.TexCoord2;
                            break;
                        case VertexAttribName.BONE_IDX:
                            name = PICAAttributeName.BoneIndex;
                            break;
                        case VertexAttribName.BONE_WEIGHT:
                            name = PICAAttributeName.BoneWeight;
                            break;
                    }
                    if (name == PICAAttributeName.UserAttribute0)
                    {
                        throw new ArgumentNullException("Unrecognized attribute name.");
                    }
                    bool isFixed = (attrByte & 128) != 0;
                    attribs.Add(new VertexAttribute
                    {
                        name = name,
                        isFixed = isFixed
                    });
                }

                List<PICAVertex> vertices = new List<PICAVertex>();
                uint vertexCount = dis.ReadUInt32();
                //populate vertex array
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices.Add(new PICAVertex());
                }

                foreach (VertexAttribute a in attribs)
                {
                    if (a.isFixed)
                    {
                        a.fixedValue = new PICAVectorFloat24(dis.ReadSingle(), dis.ReadSingle(), dis.ReadSingle(), dis.ReadSingle());
                    }
                    else
                    {
                        for (int i = 0; i < vertexCount; i++)
                        {
                            PICAVertex v = vertices[i];
                            switch (a.name)
                            {
                                case PICAAttributeName.Position:
                                    v.Position = new Vector4(VectorExtensions.ReadVector3(dis), 1);
                                    break;
                                case PICAAttributeName.Normal:
                                    v.Normal = new Vector4(VectorExtensions.ReadVector3(dis), 1);
                                    break;
                                case PICAAttributeName.Color:
                                    v.Color = new RGBA(dis).ToVector4();
                                    break;
                                case PICAAttributeName.TexCoord0:
                                    v.TexCoord0 = new Vector4(VectorExtensions.ReadVector2(dis), 0, 0);
                                    break;
                                case PICAAttributeName.TexCoord1:
                                    v.TexCoord1 = new Vector4(VectorExtensions.ReadVector2(dis), 0, 0);
                                    break;
                                case PICAAttributeName.TexCoord2:
                                    v.TexCoord2 = new Vector4(VectorExtensions.ReadVector2(dis), 0, 0);
                                    break;
                                case PICAAttributeName.BoneIndex:
                                    v.Indices = new BoneIndices();
                                    int indicesCount = dis.ReadByte();
                                    for (int idx = 0; idx < 4; idx++)
                                    {
                                        if (idx < indicesCount)
                                        {
                                            v.Indices[idx] = dis.ReadUInt16();
                                        }
                                        else
                                        {
                                            v.Indices[idx] = -1;
                                        }
                                    }
                                    break;
                                case PICAAttributeName.BoneWeight:
                                    v.Weights = new BoneWeights();
                                    int weightsCount = dis.ReadByte();
                                    for (int idx = 0; idx < weightsCount; idx++)
                                    {
                                        v.Weights[idx] = dis.ReadSingle();
                                    }
                                    break;
                            }
                            vertices[i] = v;
                        }
                    }
                }

                List<PICAAttributeName> varAttrs = new List<PICAAttributeName>();

                foreach (VertexAttribute a in attribs)
                {
                    if (!a.isFixed)
                    {
                        varAttrs.Add(a.name);
                    }
                }

                List<PICAAttribute> Attributes = PICAAttribute.GetAttributes(varAttrs.ToArray());

                Vector3 MinVector = new Vector3();
                Vector3 MaxVector = new Vector3();

                Dictionary<PICAVertex, int> Vertices = new Dictionary<PICAVertex, int>();

                List<H3DSubMesh> SubMeshes = new List<H3DSubMesh>();

                Queue<PICAVertex> VerticesQueue = new Queue<PICAVertex>();

                foreach (PICAVertex Vertex in vertices)
                {
                    VerticesQueue.Enqueue(Vertex);
                }

                bool hasSkinning = false;
                foreach (VertexAttribute a in attribs)
                {
                    if (a.name == PICAAttributeName.BoneIndex)
                    {
                        hasSkinning = true;
                        break;
                    }
                }

                if (hasSkinning)
                {
                    while (VerticesQueue.Count > 2)
                    {
                        List<ushort> Indices = new List<ushort>();
                        List<ushort> BoneIndices = new List<ushort>();

                        int TriCount = VerticesQueue.Count / 3;

                        while (TriCount-- > 0)
                        {
                            PICAVertex[] Triangle = new PICAVertex[3];

                            Triangle[0] = VerticesQueue.Dequeue();
                            Triangle[1] = VerticesQueue.Dequeue();
                            Triangle[2] = VerticesQueue.Dequeue();

                            List<ushort> TempIndices = new List<ushort>();

                            for (int Tri = 0; Tri < 3; Tri++)
                            {
                                PICAVertex Vertex = Triangle[Tri];
                                for (int i = 0; i < 4; i++)
                                {
                                    ushort Index = (ushort)Vertex.Indices[i];

                                    if (!(BoneIndices.Contains(Index) || TempIndices.Contains(Index)))
                                    {
                                        TempIndices.Add(Index);
                                    }
                                }
                            }

                            if (BoneIndices.Count + TempIndices.Count > 20)
                            {
                                VerticesQueue.Enqueue(Triangle[0]);
                                VerticesQueue.Enqueue(Triangle[1]);
                                VerticesQueue.Enqueue(Triangle[2]);

                                continue;
                            }

                            for (int Tri = 0; Tri < 3; Tri++)
                            {
                                PICAVertex Vertex = Triangle[Tri];

                                for (int Index = 0; Index < 4; Index++)
                                {
                                    if (Vertex.Indices[Index] == -1)
                                    {
                                        continue;
                                    }
                                    int BoneIndex = BoneIndices.IndexOf((ushort)Vertex.Indices[Index]);

                                    if (BoneIndex == -1)
                                    {
                                        BoneIndex = BoneIndices.Count;
                                        BoneIndices.Add((ushort)Vertex.Indices[Index]);
                                    }

                                    Vertex.Indices[Index] = BoneIndex;
                                }

                                if (Vertices.ContainsKey(Vertex))
                                {
                                    Indices.Add((ushort)Vertices[Vertex]);
                                }
                                else
                                {
                                    Indices.Add((ushort)Vertices.Count);

                                    if (Vertex.Position.X < MinVector.X) MinVector.X = Vertex.Position.X;
                                    if (Vertex.Position.Y < MinVector.Y) MinVector.Y = Vertex.Position.Y;
                                    if (Vertex.Position.Z < MinVector.Z) MinVector.Z = Vertex.Position.Z;

                                    if (Vertex.Position.X > MaxVector.X) MaxVector.X = Vertex.Position.X;
                                    if (Vertex.Position.Y > MaxVector.Y) MaxVector.Y = Vertex.Position.Y;
                                    if (Vertex.Position.Z > MaxVector.Z) MaxVector.Z = Vertex.Position.Z;
                                    Vertices.Add(Vertex, Vertices.Count);
                                }
                            }
                        }

                        H3DSubMesh sm = new H3DSubMesh()
                        {
                            Skinning = skinning,
                            BoneIndicesCount = (ushort)BoneIndices.Count,
                            BoneIndices = BoneIndices.ToArray(),
                            Indices = Indices.ToArray()
                        };

                        if (sm.BoneIndicesCount == 0)
                        {
                            sm.Skinning = H3DSubMeshSkinning.None;
                        }
                        SubMeshes.Add(sm);
                    }
                }
                else
                {
                    while (VerticesQueue.Count > 2)
                    {
                        List<ushort> Indices = new List<ushort>();

                        while (VerticesQueue.Count > 0)
                        {
                            for (int Tri = 0; Tri < 3; Tri++)
                            {
                                PICAVertex Vertex = VerticesQueue.Dequeue();

                                if (Vertices.ContainsKey(Vertex))
                                {
                                    Indices.Add((ushort)Vertices[Vertex]);
                                }
                                else
                                {
                                    Indices.Add((ushort)Vertices.Count);

                                    if (Vertex.Position.X < MinVector.X) MinVector.X = Vertex.Position.X;
                                    if (Vertex.Position.Y < MinVector.Y) MinVector.Y = Vertex.Position.Y;
                                    if (Vertex.Position.Z < MinVector.Z) MinVector.Z = Vertex.Position.Z;

                                    if (Vertex.Position.X > MaxVector.X) MaxVector.X = Vertex.Position.X;
                                    if (Vertex.Position.Y > MaxVector.Y) MaxVector.Y = Vertex.Position.Y;
                                    if (Vertex.Position.Z > MaxVector.Z) MaxVector.Z = Vertex.Position.Z;

                                    Vertices.Add(Vertex, Vertices.Count);
                                }
                            }
                        }

                        H3DSubMesh SM = new H3DSubMesh();

                        SM.BoneIndices = new ushort[] { };
                        SM.Skinning = H3DSubMeshSkinning.None;
                        SM.Indices = Indices.ToArray();

                        SubMeshes.Add(SM);
                    }
                }

                H3DMesh mesh = new H3DMesh(Vertices.Keys, Attributes, SubMeshes)
                {
                    Skinning = skinning == H3DSubMeshSkinning.Smooth ? H3DMeshSkinning.Smooth : H3DMeshSkinning.Rigid,
                    MeshCenter = (MinVector + MaxVector) * 0.5f,
                    MaterialIndex = (ushort)m.Materials.Find(materialName)
                };
                mesh.NodeIndex = (ushort)nodeIndex;
                mesh.Layer = renderLayer;
                mesh.MetaData = meshMetaData;

                mesh.UpdateBoolUniforms(m.Materials[mesh.MaterialIndex]);

                foreach (VertexAttribute a in attribs)
                {
                    if (a.isFixed)
                    {
                        mesh.FixedAttributes.Add(new PICAFixedAttribute
                        {
                            Name = a.name,
                            Value = a.fixedValue
                        });
                    }
                }

                meshes.Add(mesh);
            }
            meshes = meshes.OrderBy(mesh => mesh.Layer).ToList();
            m.AddMeshes(meshes);
            m.MeshNodesCount = meshes.Count;

            return m;
        }

        public const string META_DATA_MAGIC = "META";

        static H3DMetaData readMetaData(BinaryReader dis)
        {
            H3DMetaData meta = new H3DMetaData();

            if (dis.ReadPaddedString(4) != META_DATA_MAGIC)
            {
                throw new InvalidMagicException("Invalid metadata magic.");
            }
            int count = dis.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string name = readStringFromOffset(dis);
                MetaDataValueType type = (MetaDataValueType)dis.ReadByte();
                int listSize = dis.ReadInt32();

                switch (type)
                {
                    case MetaDataValueType.FLOAT:
                        float[] floats = new float[listSize];
                        for (int v = 0; v < floats.Length; v++)
                        {
                            floats[v] = dis.ReadSingle();
                        }
                        meta.Add(new H3DMetaDataValue(name, floats));
                        break;
                    case MetaDataValueType.INT:
                        int[] ints = new int[listSize];
                        for (int v = 0; v < ints.Length; v++)
                        {
                            ints[v] = dis.ReadInt32();
                        }
                        meta.Add(new H3DMetaDataValue(name, ints));
                        break;
                    case MetaDataValueType.STRING:
                        string[] strings = new string[listSize];
                        for (int v = 0; v < strings.Length; v++)
                        {
                            strings[v] = dis.ReadNullTerminatedString();
                        }
                        meta.Add(new H3DMetaDataValue(name, strings));
                        break;
                }
            }

            return meta;
        }

        public enum CMIFTextureFormat
        {
            ETC1,
            RGB_A,
            RGB565_5A1
        }

        public enum CMIFLUTName
        {
            REFLEC_R,
            REFLEC_G,
            REFLEC_B,
            DIST_0,
            DIST_1,
            FRESNEL
        }

        public enum MetaDataValueType
        {
            FLOAT,
            INT,
            STRING,
            OTHER
        }

        class VertexAttribute
        {
            public PICAAttributeName name;
            public bool isFixed;
            public PICAVectorFloat24 fixedValue;
        }

        public enum VertexAttribName
        {
            POSITION,
            NORMAL,
            COLOR,
            UV0,
            UV1,
            UV2,
            BONE_IDX,
            BONE_WEIGHT,
            INVALID
        }

        static int getCombinerModeArgumentCount(PICATextureCombinerMode mode)
        {
            switch (mode)
            {
                case PICATextureCombinerMode.Replace:
                    return 1;
                case PICATextureCombinerMode.Modulate:
                case PICATextureCombinerMode.Add:
                case PICATextureCombinerMode.AddSigned:
                case PICATextureCombinerMode.Subtract:
                case PICATextureCombinerMode.DotProduct3Rgb:
                case PICATextureCombinerMode.DotProduct3Rgba:
                    return 2;
                case PICATextureCombinerMode.AddMult:
                case PICATextureCombinerMode.MultAdd:
                case PICATextureCombinerMode.Interpolate:
                    return 3;
            }
            return 1;
        }

        static string readStringFromOffset(BinaryReader dis)
        {
            uint offs = dis.ReadUInt32();
            long last = dis.BaseStream.Position;
            dis.BaseStream.Seek(offs, SeekOrigin.Begin);
            string str = dis.ReadNullTerminatedString();
            dis.BaseStream.Seek(last, SeekOrigin.Begin);
            return str;
        }

        public static void Main(string[] args)
        {
            CMIFFile f = new CMIFFile(new FileStream("D:/_REWorkspace/cmif_testing/test.cmif", FileMode.Open));
        }

        class PointerTable
        {
            public uint[] pointers;
            int idx = 0;

            public PointerTable(BinaryReader dis)
            {
                pointers = new uint[dis.ReadUInt32()];

                for (int i = 0; i < pointers.Length; i++)
                {
                    pointers[i] = dis.ReadUInt32();
                }
            }

            public bool hasNext()
            {
                return idx < pointers.Length;
            }

            public void next(BinaryReader dis)
            {
                dis.BaseStream.Seek(pointers[idx], SeekOrigin.Begin);
                idx++;
            }
        }
    }
}
