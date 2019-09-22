using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SPICA.Formats.CtrH3D;
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
                if (args[0] == "texturemerge" || args[0] == "objconvert")
                {
                    String input = null;
                    String donor = null;
                    String output = null;
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

                    if (donor == null || input == null)
                    {
                        Console.WriteLine("Input or donor arguments missing");
                        return;
                    }

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

                                H3D Scene = H3D.Open(Reader.ReadBytes((int)FS.Length));

                                FS.Dispose();


                                String outFile = output == null ? donor : output;

                                if (argsList.Contains("-notextures"))
                                {
                                    Console.WriteLine("No texture mode");
                                    Scene.Merge(new OBJ(input, Scene).ToH3D(Directory.GetParent(input).FullName, true));
                                }
                                else
                                {
                                    Scene.Merge(new OBJ(input, Scene).ToH3D(Directory.GetParent(input).FullName));
                                }

                                Console.WriteLine("Merging OBJ " + input);

                                H3D.Save(outFile, Scene);

                                Console.WriteLine("Saved as " + outFile);
                            }
                        }
                    }
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

            Console.WriteLine("\nRequired arguments:\n");
            Console.WriteLine("-i <input file path>");
            Console.WriteLine("-d <donor/inject target file path>");

            Console.WriteLine("\nOptional arguments:\n");
            Console.WriteLine("-o <output file path> (If not specified, the donor is overwritten)");

            Console.WriteLine("\nCLI switches:\n");
            Console.WriteLine("-notextures - Don't embed OBJ textures into the BCH container. Requires them being merged to a global texture pack with texturemerge.");

            Console.WriteLine("\n\nSPICA is made by gdkchan an licensed under the Unlicense at https://github.com/gdkchan/SPICA");
            Console.WriteLine("ESPICA is made by HelloOO7 as part of https://github.com/HelloOO7/CTRMap at https://github.com/HelloOO7/SPICA");
        }
    }
}
