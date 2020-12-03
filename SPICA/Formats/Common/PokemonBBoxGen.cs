using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SPICA.Formats.Common
{
    class PokemonBBoxGen
    {
        public const string BBOX_MIN_MAX = "$BBoxMinMax";

        public static void CreateModelBBox(H3DModel Model)
        {
            Vector4 Min = Vector4.Zero;
            Vector4 Max = Vector4.Zero;

            bool isFirst = true;

            foreach (H3DMesh Mesh in Model.Meshes)
            {
                PICAVertex[] Vertices = Mesh.GetVertices();

                if (Vertices.Length == 0) continue;

                Vector4 MeshMin;
                Vector4 MeshMax;

                MeshMin = MeshMax = Vertices[0].Position;

                foreach (PICAVertex Vertex in Vertices)
                {
                    Vector4 P = Vertex.Position;

                    MeshMin = Vector4.Min(MeshMin, P);
                    MeshMax = Vector4.Max(MeshMax, P);
                }
                if (isFirst)
                {
                    Min = MeshMin;
                    Max = MeshMax;
                    isFirst = false;
                }
                else
                {
                    Min = Vector4.Min(Min, MeshMin);
                    Max = Vector4.Max(Max, MeshMax);
                }

                CreateMetaDataAndAddValue(Mesh, new H3DMetaDataValue(BBOX_MIN_MAX, new float[] { MeshMin.X, MeshMin.Y, MeshMin.Z, MeshMax.X, MeshMax.Y, MeshMax.Z }));
            }

            if (Model.MetaData == null)
            {
                Model.MetaData = new H3DMetaData();
            }
            int Find = Model.MetaData.Find(BBOX_MIN_MAX);
            if (Find != -1)
            {
                Model.MetaData.Remove(Model.MetaData[Find]);
            }
            Model.MetaData.Add(new H3DMetaDataValue(BBOX_MIN_MAX, new float[] { Min.X, Min.Y, Min.Z, Max.X, Max.Y, Max.Z }));
        }

        private static void CreateMetaDataAndAddValue(H3DMesh Mesh, H3DMetaDataValue Value)
        {
            if (Mesh.MetaData == null)
            {
                Mesh.MetaData = new H3DMetaData();
            }
            int Find = Mesh.MetaData.Find(Value.Name);
            if (Find != -1)
            {
                Mesh.MetaData.Remove(Mesh.MetaData[Find]);
            }
            Mesh.MetaData.Add(Value);
        }
    }
}
