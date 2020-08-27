using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.Generic.CMIF;
using SPICA.Formats.Generic.WavefrontOBJ;
namespace ESPICA
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            if (args.Length > 0)
            {
                WorkMode mode = WorkMode.None;
                switch (args[0])
                {
                    case "texturemerge":
                        mode = WorkMode.TextureMerge;
                        break;
                    case "objconvert":
                        mode = WorkMode.OBJConvert;
                        break;
                    case "cmif":
                        mode = WorkMode.CMIFConvert;
                        break;
                }
                if (mode != WorkMode.None)
                {
                    string input = null;
                    string donor = null;
                    string output = null;
                    if (argsList.Contains("-i"))
                    {
                        int index = argsList.IndexOf("-i") + 1;
                        if (index >= argsList.Count)
                        {
                            Console.WriteLine("Argument out of reach - input");
                            return;
                        }
                        else
                        {
                            input = args[index];
                        }
                    }

                    if (argsList.Contains("-d"))
                    {
                        int index = argsList.IndexOf("-d") + 1;
                        if (index >= argsList.Count)
                        {
                            Console.WriteLine("Argument out of reach - donor");
                            return;
                        }
                        else
                        {
                            donor = args[index];
                        }
                    }

                    if (argsList.Contains("-o"))
                    {
                        int index = argsList.IndexOf("-o") + 1;
                        if (index >= argsList.Count)
                        {
                            Console.WriteLine("Argument out of reach - output");
                            return;
                        }
                        else
                        {
                            output = args[index];
                        }
                    }

                    if (input == null)
                    {
                        Console.WriteLine("Input argument missing");
                        return;
                    }
                    if (mode == WorkMode.TextureMerge && donor == null)
                    {
                        Console.WriteLine("Texture merge donor argument missing");
                        return;
                    }

                    H3D Scene = new H3D();

                    if (donor != null)
                    {
                        using (FileStream FS = new FileStream(donor, FileMode.Open))
                        {
                            Console.WriteLine("Starting conversion");
                            if (FS.Length > 4)
                            {
                                BinaryReader Reader = new BinaryReader(FS);

                                uint MagicNum = Reader.ReadUInt32();

                                FS.Seek(-4, SeekOrigin.Current);

                                string Magic = Encoding.ASCII.GetString(Reader.ReadBytes(4));

                                FS.Seek(0, SeekOrigin.Begin);

                                if (Magic.StartsWith("BCH"))
                                {
                                    Console.WriteLine("Merging H3D " + donor);

                                    Scene = H3D.Open(Reader.ReadBytes((int)FS.Length));

                                    FS.Dispose();
                                }
                            }
                        }
                    }

                    string outFile = output;
                    if (outFile == null)
                    {
                        outFile = Path.GetFileNameWithoutExtension(input) + "_conv.bch";
                    }

                    switch (mode)
                    {
                        case WorkMode.OBJConvert:
                            Scene.Materials.Clear();
                            Scene.Models.Clear();
                            goto case WorkMode.TextureMerge;
                        case WorkMode.TextureMerge:
                            {
                                bool textureless = false;
                                if (argsList.Contains("-notextures"))
                                {
                                    Console.WriteLine("No texture mode");
                                    textureless = true;
                                }
                                Console.WriteLine("Merging OBJ " + input);

                                Scene.Merge(new OBJ(input).ToH3D(Directory.GetParent(input).FullName, textureless));
                            }
                            break;
                        case WorkMode.CMIFConvert:
                            {
                                Console.WriteLine("Converting Common Interchange file to H3D...");

                                Scene.Merge(new CMIFFile(new FileStream(input, FileMode.Open)).ToH3D());
                            }
                            break;
                    }

                       

                    H3D.Save(outFile, Scene);

                    Console.WriteLine("Saved as " + outFile);
                }
            }
            else
            {
                printHelp();
            }
        }

        static void printHelp()
        {
            Console.WriteLine("SPICA Embedded Command Line Interface\n");

            Console.WriteLine("Usage:\n");
            Console.WriteLine("Add new textures to a BCH pack: ESPICA.exe texturemerge [args]");
            Console.WriteLine("Convert an OBJ file to BCH: ESPICA.exe objconvert [args]");
            Console.WriteLine("Convert a CMIF asset pack to BCH: ESPICA.exe cmif [args]");

            Console.WriteLine("\nRequired arguments:\n");
            Console.WriteLine("-i <input file path>");

            Console.WriteLine("\nOptional arguments:\n");
            Console.WriteLine("-d <donor/inject target file path> Scene to merge the input with. Required for texturemerge.");
            Console.WriteLine("-o <output file path> (Default: <input>_conv.bch)");

            Console.WriteLine("\nCLI switches:\n");
            Console.WriteLine("-notextures - Don't embed OBJ textures into the BCH container. Requires them being merged to a global texture pack with texturemerge.");

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
