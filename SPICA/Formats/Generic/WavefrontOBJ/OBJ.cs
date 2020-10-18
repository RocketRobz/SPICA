using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace SPICA.Formats.Generic.WavefrontOBJ
{
    public class OBJ
    {
        private string MtlFile;

        List<OBJMesh> Meshes;

        public OBJ(string FileName)
        {
            using (FileStream FS = new FileStream(FileName, FileMode.Open))
            {
                textureOnlyFile = FileName;
                OBJModelImpl(FS, FileName.EndsWith(".mtl"));
            }
        }

        public OBJ(Stream Stream)
        {
            OBJModelImpl(Stream, false);
        }

        public bool textureOnly = false;
        public string textureOnlyFile;

        private void OBJModelImpl(Stream Stream, bool textureOnly)
        {
            this.textureOnly = textureOnly;
            if (!textureOnly)
            {
                Meshes = new List<OBJMesh>();

                List<Vector4> Positions = new List<Vector4>();
                List<Vector4> Colors = new List<Vector4>();
                List<Vector4> Normals = new List<Vector4>();
                List<Vector4> TexCoords = new List<Vector4>();

                OBJMesh Mesh = new OBJMesh();

                string Name = "";

                TextReader Reader = new StreamReader(Stream);

                for (string Line; (Line = Reader.ReadLine()) != null;)
                {
                    string[] Params = Line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (Params.Length == 0) continue;

                    switch (Params[0])
                    {
                        case "o":
                            Name = Params[1];
                            break;
                        case "v":
                        case "vc":
                        case "vn":
                            if (Params.Length >= 4)
                            {
                                (Params[0] == "v"
                                    ? Positions
                                    : Params[0] == "vn" ? Normals : Colors)
                                    .Add(new Vector4()
                                    {
                                        X = float.Parse(Params[1], CultureInfo.InvariantCulture),
                                        Y = float.Parse(Params[2], CultureInfo.InvariantCulture),
                                        Z = float.Parse(Params[3], CultureInfo.InvariantCulture),
                                        W = (Params.Length > 4) ? float.Parse(Params[4], CultureInfo.InvariantCulture) : 1f
                                    });
                            }
                            break;
                        case "vt":
                            if (Params.Length >= 3)
                            {
                                TexCoords.Add(new Vector4()
                                {
                                    X = float.Parse(Params[1], CultureInfo.InvariantCulture),
                                    Y = float.Parse(Params[2], CultureInfo.InvariantCulture)
                                });
                            }
                            break;

                        case "f":
                            string[][] Indices = new string[Params.Length - 1][];

                            for (int Index = 0; Index < Params.Length - 1; Index++)
                            {
                                Indices[Index] = Params[Index + 1].Split('/');
                            }

                            for (int Index = 0; Index < Indices.Length; Index++)
                            {
                                if (Index > 2)
                                {
                                    Mesh.Vertices.Add(Mesh.Vertices[Mesh.Vertices.Count - 3]);
                                    Mesh.Vertices.Add(Mesh.Vertices[Mesh.Vertices.Count - 2]);
                                }

                                PICAVertex Vertex = new PICAVertex();

                                if (Indices[Index].Length > 0 && Indices[Index][0] != string.Empty)
                                {
                                    Mesh.HasPosition = true;

                                    Vertex.Position = Positions[GetIndex(Indices[Index][0], Positions.Count)];
                                }

                                if (Indices[Index].Length > 1 && Indices[Index][1] != string.Empty)
                                {
                                    Mesh.HasTexCoord = true;

                                    Vertex.TexCoord0 = TexCoords[GetIndex(Indices[Index][1], Normals.Count)];

                                    Mesh.HasColor = true;

                                    if (Colors.Count > 0) {

                                        Vertex.Color = Colors[GetIndex(Indices[Index][1], Normals.Count)];
                                    }
                                    else
                                    {
                                        Vertex.Color = Vector4.One;
                                    }
                                }

                                if (Indices[Index].Length > 2 && Indices[Index][2] != string.Empty)
                                {
                                    Mesh.HasNormal = true;

                                    Vertex.Normal = Normals[GetIndex(Indices[Index][2], TexCoords.Count)];
                                }

                                Mesh.Vertices.Add(Vertex);
                            }
                            break;

                        case "usemtl":
                            if (Params.Length > 1)
                            {
                                string MaterialName = Line.Substring(Line.IndexOf(" ")).Trim();

                                if (Mesh.Vertices.Count > 0)
                                {
                                    Meshes.Add(Mesh);

                                    Mesh = new OBJMesh(MaterialName);
                                    Mesh.Name = Name;
                                }
                                else
                                {
                                    Mesh.Name = Name;
                                    Mesh.MaterialName = MaterialName;
                                }
                            }
                            break;

                        case "mtllib":
                            string MtlLibName = Line.Substring(Line.IndexOf(" ")).Trim();

                            if (Params.Length > 1)
                            {
                                MtlFile = MtlLibName;
                            }
                            break;
                    }
                }
                if (Mesh.Vertices.Count > 0) Meshes.Add(Mesh);
            }
        }

        private int GetIndex(string Value, int Count)
        {
            int Index = int.Parse(Value);

            if (Index < 0)
                return Count + Index;
            else
                return Index - 1;
        }

        private struct OBJMaterial
        {
            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Specular;

            public string DiffuseTexture;
        }

        public H3D ToH3D(string TextureAndMtlSearchPath = null, bool noTextures = false)
        {
            H3D Output = new H3D();

            Dictionary<string, OBJMaterial> Materials = new Dictionary<string, OBJMaterial>();

            if (TextureAndMtlSearchPath != null)
            {
                TextReader Reader = null;
                if (textureOnly)
                {
                    Reader = new StreamReader(textureOnlyFile);
                }
                else
                {
                    string MaterialFile = Path.Combine(TextureAndMtlSearchPath, MtlFile);

                    if (File.Exists(MaterialFile))
                    {
                        Reader = new StreamReader(MaterialFile);
                    }

                }
                if (Reader != null)
                {
                    string MaterialName = null;

                    OBJMaterial Material = default(OBJMaterial);

                    for (string Line; (Line = Reader.ReadLine()) != null;)
                    {
                        string[] Params = Line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (Params.Length == 0) continue;

                        switch (Params[0])
                        {
                            case "newmtl":
                                if (Params.Length > 1)
                                {
                                    if (MaterialName != null && Material.DiffuseTexture != null)
                                    {
                                        Materials.Add(MaterialName, Material);
                                    }

                                    Material = new OBJMaterial();

                                    MaterialName = Line.Substring(Line.IndexOf(" ")).Trim();
                                }
                                break;

                            case "map_Kd":
                                if (Params.Length > 1)
                                {
                                    string Name = Line.Substring(Line.IndexOf(Params[1]));

                                    string TextureFile = Path.Combine(TextureAndMtlSearchPath, Name);
                                    string TextureName = Path.GetFileName(TextureFile);

                                    if (File.Exists(TextureFile) && !noTextures)
                                    {
                                        if (Output.Textures.Contains(TextureName))
                                        {
                                            Output.Textures.Remove(Output.Textures[Output.Textures.Find(TextureName)]);
                                        }
                                        Output.Textures.Add(new H3DTexture(TextureFile));
                                    }

                                    Material.DiffuseTexture = TextureName;
                                }
                                break;

                            case "Ka":
                            case "Kd":
                            case "Ks":
                                if (Params.Length >= 4)
                                {
                                    Vector4 Color = new Vector4(
                                        float.Parse(Params[1], CultureInfo.InvariantCulture),
                                        float.Parse(Params[2], CultureInfo.InvariantCulture),
                                        float.Parse(Params[3], CultureInfo.InvariantCulture), 1);

                                    switch (Params[0])
                                    {
                                        case "Ka": Material.Ambient = Color; break;
                                        case "Kd": Material.Diffuse = Color; break;
                                        case "Ks": Material.Specular = Color; break;
                                    }
                                }
                                break;
                        }
                    }

                    Reader.Dispose();

                    if (MaterialName != null && !textureOnly)
                    {
                        Materials.Add(MaterialName, Material);
                    }
                }
            }

            if (!textureOnly)
            {
                H3DModel Model = new H3DModel();

                string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter model name: ", "Name", Model.Name);

                if (newName != "") ;
                {
                    Model.Name = newName;
                }

                ushort MaterialIndex = 0;

                Model.Flags = 0;
                Model.BoneScaling = H3DBoneScaling.Standard;
                Model.MeshNodesVisibility.Clear();
                Model.Skeleton.Clear();
                Model.MeshNodesVisibility.Add(true);

                float Height = 0;

                Meshes.Sort((x, y) => string.Compare(x.Name, y.Name));

                foreach (OBJMesh Mesh in Meshes)
                {
                    Vector3 MinVector = new Vector3();
                    Vector3 MaxVector = new Vector3();

                    Dictionary<PICAVertex, int> Vertices = new Dictionary<PICAVertex, int>();

                    List<H3DSubMesh> SubMeshes = new List<H3DSubMesh>();

                    Queue<PICAVertex> VerticesQueue = new Queue<PICAVertex>();

                    foreach (PICAVertex Vertex in Mesh.Vertices)
                    {
                        VerticesQueue.Enqueue(Vertex);
                    }

                    while (VerticesQueue.Count > 2)
                    {
                        List<ushort> Indices = new List<ushort>();

                        while (VerticesQueue.Count > 0)
                        {
                            for (int Tri = 0; Tri < 3; Tri++)
                            {
                                PICAVertex Vertex = VerticesQueue.Dequeue();

                                if (Mesh.Name.Contains("uncolor"))
                                {
                                    Vertex.Color = Vector4.One;
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

                        H3DSubMesh SM = new H3DSubMesh();

                        SM.BoneIndices = new ushort[] { };
                        SM.Skinning = H3DSubMeshSkinning.None;
                        SM.Indices = Indices.ToArray();

                        SubMeshes.Add(SM);
                    }

                    //Mesh
                    List<PICAAttribute> Attributes = PICAAttribute.GetAttributes(
                        PICAAttributeName.Position,
                        PICAAttributeName.Normal,
                        PICAAttributeName.TexCoord0,
                        PICAAttributeName.Color
                    );

                    H3DMesh M = new H3DMesh(Vertices.Keys, Attributes, SubMeshes)
                    {
                        Skinning = H3DMeshSkinning.Rigid,
                        MeshCenter = (MinVector + MaxVector) * 0.5f,
                        MaterialIndex = MaterialIndex,
                    };

                    if (Height < MaxVector.Y)
                        Height = MaxVector.Y;

                    //Material
                    string MatName = $"Mat{MaterialIndex++.ToString("D5")}_{Mesh.MaterialName}";

                    H3DMaterial Material = H3DMaterial.GetSimpleMaterial(Model.Name, Mesh.MaterialName, Materials[Mesh.MaterialName].DiffuseTexture);

                    Material.Texture0Name = Materials[Mesh.MaterialName].DiffuseTexture;

                    Material.MaterialParams.FaceCulling = PICAFaceCulling.BackFace;
                    Material.MaterialParams.Flags = H3DMaterialFlags.IsFragmentLightingEnabled | H3DMaterialFlags.IsVertexLightingEnabled | H3DMaterialFlags.IsFragmentLightingPolygonOffsetDirty;
                    Material.MaterialParams.FragmentFlags = 0;
                    Material.MaterialParams.LightSetIndex = 0;
                    Material.MaterialParams.FogIndex = 0;
                    Material.MaterialParams.LogicalOperation = PICALogicalOp.Noop;

                    Material.MaterialParams.AmbientColor = RGBA.White;
                    Material.MaterialParams.DiffuseColor = RGBA.White;
                    Material.MaterialParams.EmissionColor = RGBA.Black;
                    Material.MaterialParams.Specular0Color = RGBA.Black;
                    Material.MaterialParams.Specular1Color = RGBA.Black;
                    Material.MaterialParams.Constant0Color = RGBA.White;
                    Material.MaterialParams.Constant1Color = RGBA.White;
                    Material.MaterialParams.Constant2Color = RGBA.White;
                    Material.MaterialParams.Constant3Color = RGBA.White;
                    Material.MaterialParams.Constant4Color = RGBA.White;
                    Material.MaterialParams.Constant5Color = RGBA.White;
                    Material.MaterialParams.BlendColor = RGBA.Black;
                    Material.MaterialParams.Constant2Assignment = 2;

                    Material.MaterialParams.ColorScale = 1;

                    Material.MaterialParams.FresnelSelector = H3DFresnelSelector.No;
                    Material.MaterialParams.BumpMode = H3DBumpMode.NotUsed;
                    Material.MaterialParams.BumpTexture = 0;
                    Material.MaterialParams.PolygonOffsetUnit = 0;

                    Material.MaterialParams.TexEnvBufferColor = RGBA.White;

                    Material.MaterialParams.ColorOperation.FragOpMode = PICAFragOpMode.Default;
                    Material.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;

                    Material.MaterialParams.ColorBufferRead = true;
                    Material.MaterialParams.ColorBufferWrite = true;

                    Material.MaterialParams.StencilBufferRead = true;
                    Material.MaterialParams.StencilBufferWrite = true;

                    Material.MaterialParams.DepthBufferRead = true;
                    Material.MaterialParams.DepthBufferWrite = false;
                    Material.MaterialParams.TexEnvStages[0].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Texture0, PICATextureCombinerSource.PrimaryColor, PICATextureCombinerSource.Texture0 };
                    Material.MaterialParams.TexEnvStages[0].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Texture0, PICATextureCombinerSource.Texture0, PICATextureCombinerSource.Texture0 };
                    Material.MaterialParams.TexEnvStages[0].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[0].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[0].Combiner.Color = PICATextureCombinerMode.Modulate;
                    Material.MaterialParams.TexEnvStages[0].Combiner.Alpha = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[0].Color = RGBA.White;
                    Material.MaterialParams.TexEnvStages[0].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[0].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[0].UpdateColorBuffer = false;
                    Material.MaterialParams.TexEnvStages[0].UpdateAlphaBuffer = false;

                    Material.MaterialParams.TexEnvStages[1].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.FragmentPrimaryColor, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[1].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[1].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[1].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[1].Combiner.Color = PICATextureCombinerMode.Modulate;
                    Material.MaterialParams.TexEnvStages[1].Combiner.Alpha = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[1].Color = RGBA.Black;
                    Material.MaterialParams.TexEnvStages[1].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[1].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[1].UpdateColorBuffer = false;
                    Material.MaterialParams.TexEnvStages[1].UpdateAlphaBuffer = false;

                    Material.MaterialParams.TexEnvStages[2].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[2].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.PrimaryColor, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[2].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[2].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[2].Combiner.Color = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[2].Combiner.Alpha = PICATextureCombinerMode.Modulate;
                    Material.MaterialParams.TexEnvStages[2].Color = RGBA.White;
                    Material.MaterialParams.TexEnvStages[2].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[2].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[2].UpdateColorBuffer = false;
                    Material.MaterialParams.TexEnvStages[2].UpdateAlphaBuffer = false;

                    Material.MaterialParams.TexEnvStages[3].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[3].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Constant, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[3].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[3].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[3].Combiner.Color = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[3].Combiner.Alpha = PICATextureCombinerMode.Modulate;
                    Material.MaterialParams.TexEnvStages[3].Color = RGBA.White;
                    Material.MaterialParams.TexEnvStages[3].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[3].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[3].UpdateColorBuffer = false;
                    Material.MaterialParams.TexEnvStages[3].UpdateAlphaBuffer = false;

                    Material.MaterialParams.TexEnvStages[4].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[4].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[4].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[4].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[4].Combiner.Color = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[4].Combiner.Alpha = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[4].Color = RGBA.Black;
                    Material.MaterialParams.TexEnvStages[4].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[4].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[4].UpdateColorBuffer = true;
                    Material.MaterialParams.TexEnvStages[4].UpdateAlphaBuffer = false;

                    Material.MaterialParams.TexEnvStages[5].Source.Color = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[5].Source.Alpha = new PICATextureCombinerSource[] { PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous, PICATextureCombinerSource.Previous };
                    Material.MaterialParams.TexEnvStages[5].Operand.Color = new PICATextureCombinerColorOp[] { PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color, PICATextureCombinerColorOp.Color };
                    Material.MaterialParams.TexEnvStages[5].Operand.Alpha = new PICATextureCombinerAlphaOp[] { PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha, PICATextureCombinerAlphaOp.Alpha };
                    Material.MaterialParams.TexEnvStages[5].Combiner.Color = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[5].Combiner.Alpha = PICATextureCombinerMode.Replace;
                    Material.MaterialParams.TexEnvStages[5].Color = RGBA.Black;
                    Material.MaterialParams.TexEnvStages[5].Scale.Color = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[5].Scale.Alpha = PICATextureCombinerScale.One;
                    Material.MaterialParams.TexEnvStages[5].UpdateColorBuffer = false;
                    Material.MaterialParams.TexEnvStages[5].UpdateAlphaBuffer = false;

                    if (Mesh.Name.Contains("alpha"))
                    {
                        if (Mesh.Name.Contains("alphablend"))
                        {
                            M.Layer = 1;
                            Material.MaterialParams.ColorOperation.BlendMode = PICABlendMode.Blend;
                            Material.MaterialParams.BlendFunction.ColorDstFunc = PICABlendFunc.OneMinusSourceAlpha;
                            Material.MaterialParams.BlendFunction.ColorEquation = PICABlendEquation.FuncAdd;
                            Material.MaterialParams.BlendFunction.ColorSrcFunc = PICABlendFunc.SourceAlpha;
                            Material.MaterialParams.BlendFunction.AlphaDstFunc = PICABlendFunc.OneMinusSourceAlpha;
                            Material.MaterialParams.BlendFunction.AlphaEquation = PICABlendEquation.FuncAdd;
                            Material.MaterialParams.BlendFunction.AlphaSrcFunc = PICABlendFunc.SourceAlpha;
                            if (Mesh.Name.Contains("rl"))
                            {
                                int digit = Mesh.Name[Mesh.Name.IndexOf("rl") + 2] - '0';
                                if (digit >= 0 && digit <= 3)
                                {
                                    //set renderlayer
                                    M.Priority = digit;
                                }
                            }
                        }
                        Material.MaterialParams.AlphaTest.Enabled = true;
                        Material.MaterialParams.AlphaTest.Function = PICATestFunc.Greater;
                        Material.MaterialParams.AlphaTest.Reference = 0;

                    }

                    if (Mesh.Name.Contains("unculled"))
                    {
                        Material.MaterialParams.FaceCulling = PICAFaceCulling.Never;
                    }

                    if (Material.Name.Contains("mado") || Material.Name.Contains("window"))
                    {
                        Material.MaterialParams.LightSetIndex = 1;
                    }

                    Material.TextureMappers[0].MagFilter = H3DTextureMagFilter.Linear;

                    if (Model.Materials.Find(Material.Name) != -1)
                    {
                        M.MaterialIndex = (ushort)Model.Materials.Find(Material.Name);

                        MaterialIndex--;

                        M.UpdateBoolUniforms(Model.Materials[M.MaterialIndex]);
                    }
                    else
                    {
                        Model.Materials.Add(Material);

                        M.UpdateBoolUniforms(Material);
                    }

                    Model.AddMesh(M);
                }


                /*
                    * On Pokémon, the root bone (on the animaiton file) is used by the game to move
                    * characters around, and all rigged bones are parented to this bone.
                    * It's usually the Waist bone, that points upward and is half the character height.
                    */
                /*Model.Skeleton.Add(new H3DBone(
                    new Vector3(0, Height * 0.5f, 0),
                    new Vector3(0, 0, (float)(Math.PI * 0.5)),
                    Vector3.One,
                    "Waist",
                    -1));*/

                //Model.Skeleton[0].CalculateTransform(Model.Skeleton);

                Output.Models.Add(Model);

                Output.CopyMaterials();
            }

            return Output;
        }
    }
}
