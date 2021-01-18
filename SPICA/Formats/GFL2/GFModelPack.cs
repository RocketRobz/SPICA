using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.GFL2.Model;
using SPICA.Formats.GFL2.Model.Material;
using SPICA.Formats.GFL2.Shader;
using SPICA.Formats.GFL2.Texture;
using SPICA.PICA.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SPICA.Formats.GFL2
{
    public class GFModelPack
    {
        private enum Section
        {
            Model,
            Texture,
            VertShader,
            Unknown3,
            MaterialShader
        }

        public const uint MagicNum = 0x00010000;

        public readonly List<GFModel>   Models;
        public readonly List<GFTexture> Textures;
        public readonly List<GFShader>  VertexShaders;
        public readonly List<GFShader>  MaterialShaders;

        public GFModelPack()
        {
            Models   = new List<GFModel>();
            Textures = new List<GFTexture>();
            VertexShaders = new List<GFShader>();
            MaterialShaders  = new List<GFShader>();
        }

        public GFModelPack(Stream Input) : this(new BinaryReader(Input)) { }

        public GFModelPack(BinaryReader Reader) : this()
        {
            long Position = Reader.BaseStream.Position;

            uint MagicNumber = Reader.ReadUInt32();

            uint[] Counts = new uint[5];

            for (int Index = 0; Index < Counts.Length; Index++)
            {
                Counts[Index] = Reader.ReadUInt32();
            }

            long PointersAddr = Reader.BaseStream.Position;

            for (int Sect = 0; Sect < Counts.Length; Sect++)
            {
                for (int Entry = 0; Entry < Counts[Sect]; Entry++)
                {
                    Reader.BaseStream.Seek(PointersAddr + Entry * 4, SeekOrigin.Begin);
                    Reader.BaseStream.Seek(Position + Reader.ReadUInt32(), SeekOrigin.Begin);

                    string Name = Reader.ReadByteLengthString();
                    uint Address = Reader.ReadUInt32();

                    Reader.BaseStream.Seek(Position + Address, SeekOrigin.Begin);

                    switch ((Section)Sect)
                    {
                        case Section.Model:   
                            Models.Add(new GFModel(Reader, Name)); 
                            break;
                        case Section.Texture: 
                            Textures.Add(new GFTexture(Reader));   
                            break;
                        case Section.VertShader:
                            VertexShaders.Add(new GFShader(Reader));
                            break;
                        case Section.MaterialShader:
                            MaterialShaders.Add(new GFShader(Reader));     
                            break;
                    }
                }

                PointersAddr += Counts[Sect] * 4;
            }
        }

        public void Write(BinaryWriter Writer)
        {
            long Position = Writer.BaseStream.Position;
            Writer.Write(MagicNum);

            for (Section Sec = 0; Sec <= Section.MaterialShader; Sec++)
            {
                switch (Sec)
                {
                    case Section.Model:
                        Writer.Write(Models.Count);
                        break;
                    case Section.Texture:
                        Writer.Write(Textures.Count);
                        break;
                    case Section.MaterialShader:
                        Writer.Write(MaterialShaders.Count);
                        break;
                    case Section.VertShader:
                        Writer.Write(VertexShaders.Count);
                        break;
                    case Section.Unknown3:
                        Writer.Write((uint)0);
                        break;
                }
            }

            //Allocate the pointer tables
            Dictionary<GFModel, int> modelEntryOffsets = AllocPointerTable(Models, Writer);
            Dictionary<GFTexture, int> textureEntryOffsets = AllocPointerTable(Textures, Writer);
            Dictionary<GFShader, int> vertShaderEntryOffsets = AllocPointerTable(VertexShaders, Writer);
            Dictionary<GFShader, int> shaderEntryOffsets = AllocPointerTable(MaterialShaders, Writer);

            Dictionary<GFModel, int> modelPointerOffsets = AllocNamePointerTable(modelEntryOffsets, Writer, Position);
            Dictionary<GFTexture, int> texturePointerOffsets = AllocNamePointerTable(textureEntryOffsets, Writer, Position);
            Dictionary<GFShader, int> vertShaderPointerOffsets = AllocNamePointerTable(vertShaderEntryOffsets, Writer, Position);
            Dictionary<GFShader, int> shaderPointerOffsets = AllocNamePointerTable(shaderEntryOffsets, Writer, Position);

            WritePadding(Writer);

            foreach (GFModel Model in Models)
            {
                SetNamePointerTableValueHere(modelPointerOffsets, Model, Writer, Position);
                Model.Write(Writer);
                WritePadding(Writer);
            }

            foreach (GFTexture Texture in Textures)
            {
                SetNamePointerTableValueHere(texturePointerOffsets, Texture, Writer, Position);
                Texture.Write(Writer);
                WritePadding(Writer);
            }

            foreach (GFShader Shader in VertexShaders)
            {
                SetNamePointerTableValueHere(vertShaderPointerOffsets, Shader, Writer, Position);
                Shader.Write(Writer);
                WritePadding(Writer);
            }

            foreach (GFShader Shader in MaterialShaders)
            {
                SetNamePointerTableValueHere(shaderPointerOffsets, Shader, Writer, Position);
                Shader.Write(Writer);
                WritePadding(Writer);
            }
        }

        private static void WritePadding(BinaryWriter Writer)
        {
            while (Writer.BaseStream.Position % 0x80 > 0)
            {
                Writer.Write((byte)0);
            }
        }

        private static void SetNamePointerTableValueHere<T>(Dictionary<T, int> NamePtrTable, T Entry, BinaryWriter Writer, long PositionBase) where T : INamed
        {
            int RememberPosition = (int)Writer.BaseStream.Position;
            Writer.Seek(NamePtrTable[Entry], SeekOrigin.Begin);
            Writer.Write((int)(RememberPosition - PositionBase));
            Writer.Seek(RememberPosition, SeekOrigin.Begin);
        }

        private static Dictionary<T, int> AllocPointerTable<T>(List<T> List, BinaryWriter Writer)
        {
            Dictionary<T, int> Dict = new Dictionary<T, int>();
            foreach (T Elem in List)
            {
                Dict.Add(Elem, (int)Writer.BaseStream.Position);
                Writer.Write((uint)0);
            }
            return Dict;
        }
        private static Dictionary<T, int> AllocNamePointerTable<T>(Dictionary<T, int> OffsetTable, BinaryWriter Writer, long PositionBase) where T : INamed
        {
            Dictionary<T, int> Dict = new Dictionary<T, int>();
            foreach (KeyValuePair<T, int> Entry in OffsetTable)
            {
                int RememberPosition = (int)Writer.BaseStream.Position;
                Writer.Seek((int)Entry.Value, SeekOrigin.Begin);
                Writer.Write((uint)(RememberPosition - PositionBase));
                Writer.Seek(RememberPosition, SeekOrigin.Begin);
                Writer.WriteByteLengthString(Path.GetFileNameWithoutExtension(Entry.Key.Name));
                Dict.Add(Entry.Key, (int)Writer.BaseStream.Position);
                Writer.Write((uint)0);
            }
            return Dict;
        }

        public GFModelPack(H3D Scene) : this()
        {
            MergeH3D(Scene);
        }

        public void MergeGFModelPack(GFModelPack ToMerge)
        {
            foreach (GFModel M in ToMerge.Models)
            {
                AddUnique(Models, M);
            }
            foreach (GFTexture T in ToMerge.Textures)
            {
                AddUnique(Textures, T);
            }
            foreach (GFShader S in ToMerge.MaterialShaders)
            {
                AddUnique(MaterialShaders, S);
            }
            foreach (GFShader S in ToMerge.VertexShaders)
            {
                if (!VertexShaders.Contains(S))
                {
                    VertexShaders.Add(S); //Do not use AddUnique since the shaders sometimes contain duplicates (no idea why)
                }
            }
        }

        public void MergeH3D(H3D Scene) 
        {
            foreach (object SourceData in Scene.SourceData)
            {
                if (SourceData is GFModelPack)
                {
                    MergeGFModelPack((GFModelPack)SourceData);
                }
            }
            Models.Clear();
            Textures.Clear();

            Dictionary<uint, GFShader> ShaderHashes = new Dictionary<uint, GFShader>();

            foreach (GFShader Shader in MaterialShaders)
            {
                uint Hash = GetTexEnvConfigHash(Shader);
                if (!ShaderHashes.ContainsKey(Hash))
                {
                    ShaderHashes.Add(Hash, Shader);
                }
            }

            foreach (H3DModel Model in Scene.Models)
            {
                GFModel GFM = new GFModel(Model, Scene.LUTs);

                foreach (H3DMaterial Material in Model.Materials)
                {
                    bool IsAllowOriginalShader = false;
                    uint Hash = GetTexEnvConfigHash(Material.MaterialParams);
                    H3DMetaDataValue OriginHash = Material.MaterialParams.MetaData.Get("OriginMaterialHash");
                    if (OriginHash != null)
                    {
                        IsAllowOriginalShader = (int)OriginHash.Values[0] == Hash && ShaderHashes.ContainsKey(Hash);
                    }

                    if (!IsAllowOriginalShader)
                    {
                        GFShader Shader;
                        if (ShaderHashes.ContainsKey(Hash))
                        {
                            Shader = ShaderHashes[Hash];
                        }
                        else
                        {
                            Shader = new GFShader(Material, Material.Name + "_SHA");
                            ShaderHashes.Add(GetTexEnvConfigHash(Shader), Shader);
                            AddUnique(MaterialShaders, Shader);
                        }
                        
                        foreach (GFMaterial GFMat in GFM.Materials)
                        {
                            if (GFMat.Name == Material.Name)
                            {
                                GFMat.ShaderName = Shader.Name;
                                GFMat.FragShaderName = Shader.Name;
                                break;
                            }
                        }
                    }
                }

                AddUnique(Models, GFM);
            }
            foreach (H3DTexture Texture in Scene.Textures)
            {
                AddUnique(Textures, new GFTexture(Texture));
            }
        }

        private static void AddUnique<T>(List<T> List, T Elem) where T : INamed
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].Name == Elem.Name)
                {
                    List.RemoveAt(i);
                    i--;
                }
            }
            List.Add(Elem);
        }

        public static uint GetTexEnvConfigHash(H3DMaterialParams Params)
        {
            FNV1a FNV = new FNV1a();
            foreach (PICATexEnvStage Stage in Params.TexEnvStages)
            {
                FNV.Hash(Stage.Color.ToUInt32());
                FNV.Hash(Stage.Combiner.ToUInt32());
                FNV.Hash(Stage.Operand.ToUInt32());
                FNV.Hash(Stage.Scale.ToUInt32());
                FNV.Hash(Stage.Source.ToUInt32());
                FNV.Hash(Stage.UpdateAlphaBuffer ? 1 : 0);
                FNV.Hash(Stage.UpdateColorBuffer ? 1 : 0);
            }
            FNV.Hash(Params.TexEnvBufferColor.ToUInt32());
            return FNV.HashCode;
        }

        public static uint GetTexEnvConfigHash(GFShader Shader)
        {
            FNV1a FNV = new FNV1a();
            foreach (PICATexEnvStage Stage in Shader.TexEnvStages)
            {
                FNV.Hash(Stage.Color.ToUInt32());
                FNV.Hash(Stage.Combiner.ToUInt32());
                FNV.Hash(Stage.Operand.ToUInt32());
                FNV.Hash(Stage.Scale.ToUInt32());
                FNV.Hash(Stage.Source.ToUInt32());
                FNV.Hash(Stage.UpdateAlphaBuffer ? 1 : 0);
                FNV.Hash(Stage.UpdateColorBuffer ? 1 : 0);
            }
            FNV.Hash(Shader.TexEnvBufferColor.ToUInt32());
            return FNV.HashCode;
        }

        public H3D ToH3D()
        {
            H3D Output = new H3D();
            Output.SourceData.Add(this);

            H3DLUT L = new H3DLUT();

            L.Name = GFModel.DefaultLUTName;

            for (int MdlIndex = 0; MdlIndex < Models.Count; MdlIndex++)
            {
                GFModel  Model = Models[MdlIndex];
                H3DModel Mdl   = Model.ToH3DModel();

                for (int MatIndex = 0; MatIndex < Model.Materials.Count; MatIndex++)
                {
                    H3DMaterialParams Params = Mdl.Materials[MatIndex].MaterialParams;

                    string FragShaderName = Model.Materials[MatIndex].FragShaderName;
                    string VtxShaderName  = Model.Materials[MatIndex].VtxShaderName;

                    GFShader FragShader = MaterialShaders.FirstOrDefault(x => x.Name == FragShaderName);
                    GFShader VtxShader  = MaterialShaders.FirstOrDefault(x => x.Name == VtxShaderName);

                    if (FragShader != null)
                    {
                        Params.TexEnvBufferColor = FragShader.TexEnvBufferColor;

                        Array.Copy(FragShader.TexEnvStages, Params.TexEnvStages, 6);
                    }

                    Params.MetaData.Add(new H3DMetaDataValue("OriginMaterialHash", (int)GetTexEnvConfigHash(Params)));

                    if (VtxShader != null)
                    {
                        foreach (KeyValuePair<uint, Vector4> KV in VtxShader.VtxShaderUniforms)
                        {
                            Params.VtxShaderUniforms.Add(KV.Key, KV.Value);
                        }

                        foreach (KeyValuePair<uint, Vector4> KV in VtxShader.GeoShaderUniforms)
                        {
                            Params.GeoShaderUniforms.Add(KV.Key, KV.Value);
                        }
                    }
                }

                foreach (GFLUT LUT in Model.LUTs)
                {
                    L.Samplers.Add(new H3DLUTSampler()
                    {
                        Name  = LUT.Name,
                        Table = LUT.Table
                    });
                }

                Output.Models.Add(Mdl);
            }

            Output.LUTs.Add(L);

            Output.CopyMaterials();

            foreach (GFTexture Texture in Textures)
            {
                Output.Textures.Add(Texture.ToH3DTexture());
            }

            /*Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Output, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                }));*/

            return Output;
        }
    }
}
