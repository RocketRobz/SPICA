﻿using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Camera;
using SPICA.Formats.CtrH3D.Fog;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Formats.CtrH3D.Scene;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;
using SPICA.Serialization.Serializer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SPICA.Formats.CtrH3D
{
    enum H3DSectionId
    {
        Contents,
        Strings,
        Commands,
        RawData,
        RawExt,
        Relocation
    }

    public class H3D : ICustomSerialization
    {
        [Ignore] public List<object> SourceData = new List<object>();

        public readonly H3DDict<H3DModel>          Models;
        public readonly H3DDict<H3DMaterialParams> Materials;
        public readonly H3DDict<H3DShader>         Shaders;
        public readonly H3DDict<H3DTexture>        Textures;
        public readonly H3DDict<H3DLUT>            LUTs;
        public readonly H3DDict<H3DLight>          Lights;
        public readonly H3DDict<H3DCamera>         Cameras;
        public readonly H3DDict<H3DFog>            Fogs;
        public readonly H3DDict<H3DAnimation>      SkeletalAnimations;
        public readonly H3DDict<H3DMaterialAnim>   MaterialAnimations;
        public readonly H3DDict<H3DAnimation>      VisibilityAnimations;
        public readonly H3DDict<H3DAnimation>      LightAnimations;
        public readonly H3DDict<H3DAnimation>      CameraAnimations;
        public readonly H3DDict<H3DAnimation>      FogAnimations;
        public readonly H3DDict<H3DScene>          Scenes;

        [Ignore] public byte BackwardCompatibility;
        [Ignore] public byte ForwardCompatibility;

        [Ignore] public ushort ConverterVersion;

        [Ignore] public H3DFlags Flags;

        public H3D()
        {
            Models               = new H3DDict<H3DModel>();
            Materials            = new H3DDict<H3DMaterialParams>();
            Shaders              = new H3DDict<H3DShader>();
            Textures             = new H3DDict<H3DTexture>();
            LUTs                 = new H3DDict<H3DLUT>();
            Lights               = new H3DDict<H3DLight>();
            Cameras              = new H3DDict<H3DCamera>();
            Fogs                 = new H3DDict<H3DFog>();
            SkeletalAnimations   = new H3DDict<H3DAnimation>();
            MaterialAnimations   = new H3DDict<H3DMaterialAnim>();
            VisibilityAnimations = new H3DDict<H3DAnimation>();
            LightAnimations      = new H3DDict<H3DAnimation>();
            CameraAnimations     = new H3DDict<H3DAnimation>();
            FogAnimations        = new H3DDict<H3DAnimation>();
            Scenes               = new H3DDict<H3DScene>();

            BackwardCompatibility = 0x21;
            ForwardCompatibility  = 0x21;

            ConverterVersion = 42607;

            Flags = H3DFlags.IsFromNewConverter;
        }

        public static H3D Open(byte[] Data)
        {
            using (MemoryStream MS = new MemoryStream(Data))
            {
                return Open(MS);
            }
        }

        public static H3D Open(MemoryStream MS)
        {
            //Please note that data should be on Memory when opening because addresses are relocated.
            //Otherwise the original file would be corrupted!
            BinaryDeserializer Deserializer = new BinaryDeserializer(MS, GetSerializationOptions());

            H3DHeader Header = Deserializer.Deserialize<H3DHeader>();

            new H3DRelocator(MS, Header).ToAbsolute();

            H3D Scene = Deserializer.Deserialize<H3D>();

            Scene.BackwardCompatibility = Header.BackwardCompatibility;
            Scene.ForwardCompatibility  = Header.ForwardCompatibility;

            Scene.ConverterVersion = Header.ConverterVersion;

            Scene.Flags = Header.Flags;

            /*Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Scene, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            }));*/

            if (Scene.Models.Count > 0)
            {
                foreach (H3DMaterial mat in Scene.Models[0].Materials)
                {
                    /*  mat.MaterialParams.StencilTest.Reference = 255;
                       mat.MaterialParams.MetaData[mat.MaterialParams.MetaData.Find("$EdgeID")].Values[0] = 255;
                       //mat.MaterialParams.MetaData[mat.MaterialParams.MetaData.Find("$EdgeType")].Values[0] = 0;
                    //mat.MaterialParams.MetaData.Clear();
                    //mat.MaterialParams.VtxShaderUniforms.Clear();
                    mat.MaterialParams.FragmentFlags = H3DFragmentFlags.IsLUTReflectionEnabled;
                    mat.MaterialParams.LUTInputSelection.ReflecR = PICALUTInput.CosNormalView;
                    mat.MaterialParams.LUTInputSelection.ReflecG = PICALUTInput.CosNormalView;
                    mat.MaterialParams.LUTInputSelection.ReflecB = PICALUTInput.CosNormalView;
                    mat.MaterialParams.LUTReflecRSamplerName = null;
                    mat.MaterialParams.LUTReflecRTableName = null;
                    mat.MaterialParams.LUTReflecGSamplerName = null;
                    mat.MaterialParams.LUTReflecGTableName = null;
                    mat.MaterialParams.LUTReflecBSamplerName = null;
                    mat.MaterialParams.LUTReflecBTableName = null;*/
                    /*mat.MaterialParams.TextureCoords[0].Flags = H3DTextureCoordFlags.IsDirty;
                    mat.MaterialParams.TextureCoords[1].Flags = H3DTextureCoordFlags.IsDirty;
                    mat.MaterialParams.TextureCoords[2].Flags = H3DTextureCoordFlags.IsDirty;
                    mat.MaterialParams.TextureCoords[0].ReferenceCameraIndex = 0;
                    mat.MaterialParams.TextureCoords[1].ReferenceCameraIndex = 0;
                    mat.MaterialParams.TextureCoords[2].ReferenceCameraIndex = 0;*/
                }
                /*Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Scene, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                }));*/
            }

            /*RGBA DesiredDarkYellow = new RGBA(0, 128, 0, 255);
            RGBA DesiredLightYellow = new RGBA(0, 255, 0, 255);
            RGBA DesiredCrimson = new RGBA(0, 128, 0, 255);

            List<string> MaterialNamesToReplace = new List<string>(new string[] { "ArmL_Lay1_yellow", "ArmR_Lay1_yellow", "BodyFront_Lay1_yellow", "BodyMid_Lay1_yellow", "Tail_Lay1_yellow" });

            foreach (H3DModel Model in Scene.Models)
            {
                foreach (H3DMaterial Material in Model.Materials)
                {
                    if (MaterialNamesToReplace.Contains(Material.Name))
                    {
                        SetConstantColor(Material.MaterialParams.GetConstantIndex(2), Material.MaterialParams, DesiredDarkYellow);
                        SetConstantColor(Material.MaterialParams.GetConstantIndex(3), Material.MaterialParams, DesiredLightYellow);
                        SetConstantColor(Material.MaterialParams.GetConstantIndex(4), Material.MaterialParams, DesiredCrimson);
                    }
                }
            }*/

            /*RGBA DesiredOuterFireColor = new RGBA(0, 255, 0, 255); //Change this to whatever color you want the fire to be
            RGBA DesiredInnerFireColor = new RGBA(0, 128, 0, 255); //Change these to whatever color you want the fire to be

            foreach (H3DModel Model in Scene.Models)
            {
                foreach (H3DMaterial Material in Model.Materials)
                {
                    if (Material.Name.Equals("FireStenA"))
                    {
                        SetConstantColor(Material.MaterialParams.GetConstantIndex(0), Material.MaterialParams, DesiredOuterFireColor);
                        SetConstantColor(Material.MaterialParams.GetConstantIndex(1), Material.MaterialParams, DesiredInnerFireColor);
                    }
                }
            }*/

            return Scene;
        }

        private static void SetConstantColor(int id, H3DMaterialParams mparams, RGBA target)
        {
            switch (id)
            {
                case 0:
                    mparams.Constant0Color = target;
                    break;
                case 1:
                    mparams.Constant1Color = target;
                    break;
                case 2:
                    mparams.Constant2Color = target;
                    break;
                case 3:
                    mparams.Constant3Color = target;
                    break;
                case 4:
                    mparams.Constant4Color = target;
                    break;
                case 5:
                    mparams.Constant5Color = target;
                    break;
            }
        }

        public static void Save(string FileName, H3D Scene)
        {
            foreach (H3DModel Model in Scene.Models)
            {
                PokemonBBoxGen.CreateModelBBox(Model);
            }

            using (FileStream FS = new FileStream(FileName, FileMode.Create))
            {
                H3DHeader Header = new H3DHeader();

                H3DRelocator Relocator = new H3DRelocator(FS, Header);

                BinarySerializer Serializer = new BinarySerializer(FS, GetSerializationOptions());

                Section Contents = Serializer.Sections[(uint)H3DSectionId.Contents];

                Contents.Header = Header;

                /*
                 * Those comparisons are used to sort Strings and data buffers.
                 * Strings are sorted in alphabetical order (like on the original file),
                 * while buffers places textures first, and then vertex/index data after.
                 * It's unknown why textures needs to come first, but placing the textures
                 * at the end or at random order causes issues on the game.
                 * It's most likely an alignment issue.
                 */
                Comparison<RefValue> CompStr = H3DComparers.GetComparisonStr();
                Comparison<RefValue> CompRaw = H3DComparers.GetComparisonRaw();

                Section Strings    = new Section(0x10, CompStr);
                Section Commands   = new Section(0x80);
                Section RawData    = new Section(0x80, CompRaw);
                Section RawExt     = new Section(0x80, CompRaw);
                Section Relocation = new Section();

                Serializer.AddSection((uint)H3DSectionId.Strings,    Strings,  typeof(string));
                Serializer.AddSection((uint)H3DSectionId.Strings,    Strings,  typeof(H3DStringUtf16));
                Serializer.AddSection((uint)H3DSectionId.Commands,   Commands, typeof(uint[]));
                Serializer.AddSection((uint)H3DSectionId.RawData,    RawData);
                Serializer.AddSection((uint)H3DSectionId.RawExt,     RawExt);
                Serializer.AddSection((uint)H3DSectionId.Relocation, Relocation);

                Header.BackwardCompatibility = Scene.BackwardCompatibility;
                Header.ForwardCompatibility  = Scene.ForwardCompatibility;

                Header.ConverterVersion = Scene.ConverterVersion;

                Header.Flags = Scene.Flags;

                Serializer.Serialize(Scene);

                Header.AddressCount  = (ushort)RawData.Values.Count;
                Header.AddressCount += (ushort)RawExt.Values.Count;

                Header.UnInitDataLength = Header.AddressCount * 4;

                Header.ContentsAddress = Contents.Position;
                Header.StringsAddress  = Strings.Position;
                Header.CommandsAddress = Commands.Position;
                Header.RawDataAddress  = RawData.Position;
                Header.RawExtAddress   = RawExt.Position;

                Header.RelocationAddress = Relocation.Position;

                Header.ContentsLength = Contents.Length;
                Header.StringsLength  = Strings.Length;
                Header.CommandsLength = Commands.Length;
                Header.RawDataLength  = RawData.Length;
                Header.RawExtLength   = RawExt.Length;

                Relocator.ToRelative(Serializer);

                FS.Seek(0, SeekOrigin.Begin);

                Serializer.WriteValue(Header);
            }
        }

        private static SerializationOptions GetSerializationOptions()
        {
            return new SerializationOptions(LengthPos.AfterPtr, PointerType.Absolute);
        }

        public void Merge(H3D SceneData)
        {
            if (SceneData != null)
            {
                AddUnique(SceneData.Models, Models);
                AddUnique(SceneData.Materials, Materials);
                AddUnique(SceneData.Shaders, Shaders);
                AddUnique(SceneData.Textures, Textures);
                AddUnique(SceneData.LUTs, LUTs);
                AddUnique(SceneData.Lights, Lights);
                AddUnique(SceneData.Cameras, Cameras);
                AddUnique(SceneData.Fogs, Fogs);
                AddUnique(SceneData.SkeletalAnimations, SkeletalAnimations);
                AddUnique(SceneData.MaterialAnimations, MaterialAnimations);
                AddUnique(SceneData.VisibilityAnimations, VisibilityAnimations);
                AddUnique(SceneData.LightAnimations, LightAnimations);
                AddUnique(SceneData.CameraAnimations, CameraAnimations);
                AddUnique(SceneData.FogAnimations, FogAnimations);
                AddUnique(SceneData.Scenes, Scenes);

                SourceData.AddRange(SceneData.SourceData);
            }
        }

        private void AddUnique<T>(H3DDict<T> Src, H3DDict<T> Tgt) where T : INamed
        {
            //We need to make sure that the name isn't already contained on the Tree.
            //Otherwise it would throw an exception due to duplicate Keys.
            foreach (T Value in Src)
            {
                string Name = Value.Name;

                int Index = 0;

                /*while (Tgt.Contains(Name))
                {
                    Name = $"{Value.Name}_{++Index}";
                }*/
                if (Tgt.Contains(Name))
                {
                    Tgt.Remove(Tgt[Tgt.Find(Name)]);
                }

                Value.Name = Name;

                Tgt.Add(Value);
            }
        }

        public void CopyMaterials()
        {
            Materials.Clear();

            foreach (H3DModel Model in Models)
            {
                foreach (H3DMaterial Material in Model.Materials)
                {
                    //Note: The IF is a workaround for multiple models with same material names.
                    //This kind of problem doesn't happen on BCH, but may happen on converted formats.
                    if (!Materials.Contains(Material.Name))
                    {
                        Materials.Add(Material.MaterialParams);
                    }
                }
            }
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer) { }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            //The original tool seems to add this empty name for some reason.
            //Serializer.Sections[(uint)H3DSectionId.Strings].Values.Add(new RefValue(string.Empty));

            return false;
        }
    }
}
