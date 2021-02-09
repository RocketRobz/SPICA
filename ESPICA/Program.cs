using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ESPICA.CLI;
using SPICA.Formats;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.Generic.CMIF;
using SPICA.Formats.Generic.WavefrontOBJ;
using SPICA.Formats.GFL2;

namespace ESPICA
{
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentBuilder ab = new ArgumentBuilder(
                new ArgumentPattern("input", "One or more converter input files", ArgumentType.STRING, null, true, "-i"),
                new ArgumentPattern("output", "An optional specified output file.", ArgumentType.STRING, null, "-o"),
                new ArgumentPattern("outputType", "The output format type (h3d/gfmbdlp)", ArgumentType.STRING, "h3d", "-t", "--type"),
                new ArgumentPattern("outputVersion", "The output format version", ArgumentType.INT, (int)0x21, "-v", "--version"),
                new ArgumentPattern("filter", "Output format filters (model/texture/animation/all)", ArgumentType.STRING, "all", true, "-f", "--filter")
            );

            Console.WriteLine("SPICA Embedded Command Line Interface\n");
            ab.parse(args);

            ArgumentContent inputs = ab.getContent("input");
            if (inputs.contents.Count == 0)
            {
                Console.WriteLine("No inputs given. Stopping.\n");

                printHelp(ab);
            }
            else
            {
                string formatName = ab.getContent("outputType").stringValue();
                string formatExtension;
                switch (formatName)
                {
                    case "h3d":
                        formatExtension = "bch";
                        break;
                    case "gfbmdlp":
                        formatExtension = formatName;
                        break;
                    default:
                        Console.WriteLine("Unknown output type: " + formatName);
                        return;
                }

                H3D Scene = new H3D();

                Scene.BackwardCompatibility = (byte)ab.getContent("outputVersion").intValue();
                Scene.ForwardCompatibility = Scene.BackwardCompatibility;

                for (int i = 0; i < inputs.contents.Count; i++)
                {
                    string inPath = inputs.stringValue(i);
                    if (File.Exists(inPath))
                    {
                        Scene.Merge(FormatIdentifier.IdentifyAndOpen(inPath));
                    }
                }

                ArgumentContent flt = ab.getContent("filter");

                bool deleteModel = true;
                bool deleteTex = true;
                bool deleteAnime = true;

                for (int i = 0; i < flt.contents.Count; i++)
                {
                    string filter = flt.stringValue(i);
                    switch (filter)
                    {
                        case "model":
                            deleteModel = false;
                            break;
                        case "texture":
                            deleteModel = false;
                            break;
                        case "animation":
                            deleteAnime = false;
                            break;
                        case "all":
                            deleteAnime = false;
                            deleteModel = false;
                            deleteTex = false;
                            break;
                        default:
                            Console.WriteLine("Warning: unknown filter - " + filter);
                            break;
                    }
                }

                if (deleteModel)
                {
                    Scene.Models.Clear();
                }
                if (deleteTex)
                {
                    Scene.Textures.Clear();
                }
                if (deleteAnime)
                {
                    Scene.MaterialAnimations.Clear();
                    Scene.SkeletalAnimations.Clear();
                    Scene.VisibilityAnimations.Clear();
                    Scene.FogAnimations.Clear();
                    Scene.CameraAnimations.Clear();
                    Scene.LightAnimations.Clear();
                }

                string outputFilePath = Path.Combine(Directory.GetParent(inputs.stringValue(0)).FullName, Path.GetFileNameWithoutExtension(inputs.stringValue(0)) + "." + formatExtension);
                ArgumentContent outCnt = ab.getContent("output", true);
                if (outCnt != null)
                {
                    outputFilePath = outCnt.stringValue();
                }

                switch (formatName)
                {
                    case "h3d":
                        H3D.Save(outputFilePath, Scene);
                        break;
                    case "gfbmdlp":
                        using (BinaryWriter Writer = new BinaryWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write)))
                        {
                            GFModelPack ModelPack = new GFModelPack(Scene);
                            ModelPack.Write(Writer);
                            Writer.Close();
                        }
                        break;
                }
            }
        }

        static void printHelp(ArgumentBuilder ab)
        {
            ab.print();

            Console.WriteLine("\n\nSPICA is made by gdkchan an licensed under the Unlicense at https://github.com/gdkchan/SPICA");
            Console.WriteLine("ESPICA is made by HelloOO7 as part of https://github.com/HelloOO7/CTRMap at https://github.com/HelloOO7/SPICA");
        }

        enum WorkMode
        {
            None,
            TextureMerge,
            OBJConvert,
            CMIFConvert
        }
    }
}
