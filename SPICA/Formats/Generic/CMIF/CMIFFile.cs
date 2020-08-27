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

namespace SPICA.Formats.Generic.CMIF
{
    public class CMIFFile
    {
        public const string CMIF_MAGIC = "CMIF";
        public const int READER_VERSION = 1;

        public List<H3DModel> models = new List<H3DModel>();
        public List<H3DTexture> textures = new List<H3DTexture>();
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
            if (version > READER_VERSION)
            {
                throw new NotSupportedException("File version is too new!");
            }

            uint stringTableOffset  = dis.ReadUInt32();
            uint contentTableOffset = dis.ReadUInt32();

            dis.BaseStream.Seek(contentTableOffset, SeekOrigin.Begin);

            uint modelsPointerTableOffset = dis.ReadUInt32();
            uint texturesPointerTableOffset = dis.ReadUInt32();
            uint sklAnmPointerTableOffset = dis.ReadUInt32();
            uint matAnmPointerTableOffset = dis.ReadUInt32();

            dis.BaseStream.Seek(modelsPointerTableOffset, SeekOrigin.Begin);
            PointerTable modelsPT = new PointerTable(dis);

            while (modelsPT.hasNext())
            {
                modelsPT.next(dis);
                models.Add(readModel(dis));
            }

            dis.BaseStream.Seek(texturesPointerTableOffset, SeekOrigin.Begin);
            PointerTable texPT = new PointerTable(dis);

            while (texPT.hasNext())
            {
                texPT.next(dis);
                textures.Add(readTexture(dis));
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

                for (int n = 0; n < 3; n++) {
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

            return output;
        }

        public const string TEXTURE_MAGIC = "IFTX";

        static H3DTexture readTexture(BinaryReader dis)
        {
            if (dis.ReadPaddedString(4) != TEXTURE_MAGIC)
            {
                throw new ArgumentException("Invalid texture magic.");
            }
            string name = readStringFromOffset(dis);
            int width = dis.ReadUInt16();
            int height = dis.ReadUInt16();
            byte[] Buffer = dis.ReadBytes(dis.ReadInt32());

            byte[] Output = new byte[Buffer.Length];

            int Stride = width * 4;

            PICATextureFormat Format = PICATextureFormat.ETC1;

            for (int Y = 0; Y < height; Y++)
            {
                int IOffs = Stride * Y;
                int OOffs = Stride * (height - 1 - Y);

                for (int X = 0; X < width; X++)
                {
                    Output[OOffs + 0] = Buffer[IOffs + 2];
                    Output[OOffs + 1] = Buffer[IOffs + 1];
                    Output[OOffs + 2] = Buffer[IOffs + 0];
                    Output[OOffs + 3] = Buffer[IOffs + 3];
                    if (Output[OOffs + 3] != 255)
                    {
                        Format = PICATextureFormat.ETC1A4;
                    }

                    IOffs += 4;
                    OOffs += 4;
                }
            }

            Bitmap bmp = TextureConverter.GetBitmap(Output, width, height);

            return new H3DTexture(name, bmp, Format);
        }

        public const string MODEL_MAGIC = "IFMD";
        public const string SKELETON_JOINT_MAGIC = "SKLJ";
        public const string MESH_MAGIC = "IFPL";
        public const string MATERIAL_MAGIC = "IFMT";
	    public const string TEVCONF_MAGIC = "TENV";

        static H3DModel readModel(BinaryReader dis)
        {
            H3DModel m = new H3DModel();

            if (dis.ReadPaddedString(4) != MODEL_MAGIC)
            {
                throw new ArgumentException("Invalid model magic.");
            }
            m.Name = readStringFromOffset(dis);

            //these pointer tables are condensed - no pointers to them
            PointerTable bonesPT = new PointerTable(dis);
            PointerTable meshesPT = new PointerTable(dis);
            PointerTable matsPT = new PointerTable(dis);

            if (bonesPT.hasNext())
            {
                m.Flags = H3DModelFlags.HasSkeleton;
            }

            m.BoneScaling = H3DBoneScaling.Maya;
            m.MeshNodesVisibility.Add(true);

            while (bonesPT.hasNext())
            {
                bonesPT.next(dis);

                if (dis.ReadPaddedString(4) != SKELETON_JOINT_MAGIC)
                {
                    throw new ArgumentException("Invalid joint magic.");
                }

                H3DBone bone = new H3DBone();

                bone.Name           = readStringFromOffset(dis);
                bone.ParentIndex    = (short)dis.ReadUInt32();
                bone.Translation    = VectorExtensions.ReadVector3(dis);
                bone.Rotation       = VectorExtensions.ReadVector3(dis);
                bone.Scale          = VectorExtensions.ReadVector3(dis);

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

                int textureCount = dis.ReadByte();
                string tex0Name = textureCount > 0 ? readStringFromOffset(dis) : null;

                H3DMaterial mat = H3DMaterial.GetSimpleMaterial(m.Name, matName, tex0Name);
                mat.MaterialParams.Flags = H3DMaterialFlags.IsFragmentLightingEnabled;

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

                    mat.TextureMappers[i].WrapU     = (PICATextureWrap)dis.ReadByte();
                    mat.TextureMappers[i].WrapV     = (PICATextureWrap)dis.ReadByte();
                    mat.TextureMappers[i].MagFilter = (H3DTextureMagFilter)dis.ReadByte();
                    mat.TextureMappers[i].MinFilter = (H3DTextureMinFilter)dis.ReadByte();
                }
                mat.MaterialParams.TextureSources = uvPtrs;

                //Depth test
                bool depthOpEnabled = dis.ReadBoolean();
                mat.MaterialParams.DepthColorMask.Enabled = depthOpEnabled;
                if (depthOpEnabled) {
                    byte depthOpByte = dis.ReadByte();
                    mat.MaterialParams.DepthColorMask.DepthFunc     = (PICATestFunc)(depthOpByte & 7);
                    mat.MaterialParams.DepthColorMask.RedWrite      = (depthOpByte & 8) > 0;
                    mat.MaterialParams.DepthColorMask.GreenWrite    = (depthOpByte & 16) > 0;
                    mat.MaterialParams.DepthColorMask.BlueWrite     = (depthOpByte & 32) > 0;
                    mat.MaterialParams.DepthColorMask.AlphaWrite    = (depthOpByte & 64) > 0;
                    mat.MaterialParams.DepthColorMask.DepthWrite    = (depthOpByte & 128) > 0;
                }

                //Alpha test
                byte alphaTestByte = dis.ReadByte();
                mat.MaterialParams.AlphaTest.Enabled    = (alphaTestByte & 128) > 0;
                mat.MaterialParams.AlphaTest.Function   = (PICATestFunc)(alphaTestByte & 7);
                mat.MaterialParams.AlphaTest.Reference  = dis.ReadByte();

                //Blend config
                byte blendMasterByte = dis.ReadByte();
                byte blendRgbByte = dis.ReadByte();
                byte blendAlphaByte = dis.ReadByte();

                mat.MaterialParams.ColorOperation.BlendMode     = PICABlendMode.Blend;  //nothing to disable it on H3D
                mat.MaterialParams.BlendFunction.ColorEquation  = (PICABlendEquation)(blendMasterByte & 7);
                mat.MaterialParams.BlendFunction.AlphaEquation  = (PICABlendEquation)((blendMasterByte >> 3) & 7);
                mat.MaterialParams.BlendFunction.ColorSrcFunc   = (PICABlendFunc)(blendRgbByte & 15);
                mat.MaterialParams.BlendFunction.ColorDstFunc   = (PICABlendFunc)((blendRgbByte >> 4) & 15);
                mat.MaterialParams.BlendFunction.AlphaSrcFunc   = (PICABlendFunc)(blendAlphaByte & 15);
                mat.MaterialParams.BlendFunction.AlphaDstFunc   = (PICABlendFunc)((blendAlphaByte >> 4) & 15);
                mat.MaterialParams.BlendColor                   = new RGBA(dis);

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
                m.Materials.Add(mat);
            }

            while (meshesPT.hasNext())
            {
                meshesPT.next(dis);

                if (dis.ReadPaddedString(4) != MESH_MAGIC)
                {
                    throw new ArgumentException("Invalid mesh magic.");
                }

                string meshName = readStringFromOffset(dis);    //suprisingly, SPICA does not have support for object names
                string materialName = readStringFromOffset(dis);
                int renderLayer = dis.ReadByte();

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
                                    for (int idx = 0; idx < indicesCount; idx++)
                                    {
                                        v.Indices[idx] = dis.ReadUInt16();
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
                            Skinning = H3DSubMeshSkinning.Smooth,
                            BoneIndicesCount = (ushort)BoneIndices.Count,
                            BoneIndices = BoneIndices.ToArray(),
                            Indices = Indices.ToArray()
                        };
                        switch (sm.BoneIndicesCount)
                        {
                            case 0:
                                sm.Skinning = H3DSubMeshSkinning.None;
                                break;
                            case 1:
                                sm.Skinning = H3DSubMeshSkinning.Rigid;
                                break;
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
                    Skinning = H3DMeshSkinning.Smooth,
                    MeshCenter = (MinVector + MaxVector) * 0.5f,
                    MaterialIndex = (ushort)m.Materials.Find(materialName)
                };
                mesh.Layer = renderLayer;
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

                m.AddMesh(mesh);
            }

            return m;
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
