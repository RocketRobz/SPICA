using SPICA.Formats;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.Generic.COLLADA;
using SPICA.Formats.Generic.StudioMdl;
using SPICA.Formats.GFL2;
using SPICA.Formats.GFL2.Model;
using SPICA.Formats.GFL2.Texture;
using SPICA.PICA.Shader;
using SPICA.Rendering;
using SPICA.Rendering.Shaders;
using System.IO;
using System.Windows.Forms;

namespace SPICA.WinForms.Formats
{
    public class FileIO
    {
        public static H3D Merge(string[] FileNames, Renderer Renderer, H3D Scene = null)
        {
            int OpenFiles = 0;

            foreach (string FileName in FileNames)
            {
                H3DDict<H3DBone> Skeleton = null;

                if (Scene != null && Scene.Models.Count > 0) Skeleton = Scene.Models[0].Skeleton;

                H3D Data = ContainerIdentifier.IdentifyAndOpen(FileName, Skeleton);

                if (Data != null)
                {
                    if (Scene == null)
                    {
                        Scene = Data;
                    }
                    else
                    {
                        Scene.Merge(Data);
                    }

                    Renderer.Merge(Data);

                    OpenFiles++;
                }
            }

            if (OpenFiles == 0)
            {
                MessageBox.Show(
                    "Unsupported file format!",
                    "Can't open file!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }

            return Scene;
        }

        public static void Save(H3D Scene, SceneState State)
        {
            if (Scene == null)
            {
                MessageBox.Show(
                    "Please load a file first!",
                    "No data",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                return;
            }

            using (SaveFileDialog SaveDlg = new SaveFileDialog())
            {
                SaveDlg.Filter = 
                    "COLLADA 1.4.1|*.dae" +
                    "|Valve StudioMdl|*.smd" +
                    "|Binary Ctr H3D v33|*.bch" +
                    "|Binary Ctr H3D v7|*.bch" +
                    "|Game Freak Binary Model Pack|*.gfbmdlp";

                SaveDlg.FileName = "Model";
                if (Scene.Models.Count > 0)
                {
                    SaveDlg.FileName = Scene.Models[0].Name;
                }

                if (SaveDlg.ShowDialog() == DialogResult.OK)
                {
                    int MdlIndex  = State.ModelIndex;
                    int AnimIndex = State.SklAnimIndex;

                    switch (SaveDlg.FilterIndex)
                    {
                        case 1: new DAE(Scene, MdlIndex, AnimIndex).Save(SaveDlg.FileName); break;
                        case 2: new SMD(Scene, MdlIndex, AnimIndex).Save(SaveDlg.FileName); break;
                        case 3:
                            Scene.BackwardCompatibility = 0x21;
                            Scene.ForwardCompatibility  = 0x21;
                            H3D.Save(SaveDlg.FileName, Scene); 
                            break;
                        case 4:
                            Scene.BackwardCompatibility = 0x7;
                            Scene.ForwardCompatibility  = 0x7;
                            H3D.Save(SaveDlg.FileName, Scene); 
                            break;
                        case 5:
                            MessageBox.Show(
                                "GFBMDLP writing comes with no warranty whatsoever. In fact, character and Pokémon models will most certainly not work at all.\n\n(Before you ask, this dialog can not be disabled. Intentionally.)",
                                "Disclaimer",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                            using (BinaryWriter Writer = new BinaryWriter(new FileStream(SaveDlg.FileName, FileMode.Create, FileAccess.Write)))
                            {
                                GFModelPack ModelPack = new GFModelPack(Scene);
                                ModelPack.Write(Writer);
                                Writer.Close();
                            }
                            break;
                    }
                }
            }
        }

        public static void Export(H3D Scene, int Index = -1)
        {
            if (Index != -1)
            {
                //Export one
                using (SaveFileDialog SaveDlg = new SaveFileDialog())
                {
                    SaveDlg.Filter = "Portable Network Graphics|*.png|"	+
									 "GFTexture|*.*;*.pc;*.bin";
                    SaveDlg.FileName = Scene.Textures[Index].Name;

					if (SaveDlg.ShowDialog() == DialogResult.OK)
                    {
						switch (SaveDlg.FilterIndex) {
							case 1:	//PNG
								TextureManager.GetTexture(Index).Save(SaveDlg.FileName);
								break;
							case 2:	//GFTexture
								new GFPackedTexture(Scene, Index).Save(SaveDlg.FileName);
								break;
						}
                    }
                }
            }
            else
            {
                //Export all (or don't export if format can only export a single item)
                using (FolderBrowserDialog FolderDlg = new FolderBrowserDialog())
                {
                    if (FolderDlg.ShowDialog() == DialogResult.OK)
                    {
                        for (int i = 0; i < Scene.Textures.Count; i++)
                        {
                            string FileName = Path.Combine(FolderDlg.SelectedPath, $"{Scene.Textures[i].Name}.png");

                            TextureManager.GetTexture(i).Save(FileName);
                        }
                    }
                }
            }
        }
    }
}
