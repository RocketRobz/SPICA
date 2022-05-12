using SPICA.PICA.Converters;

using System.Collections.Generic;

namespace SPICA.Formats.Generic.WavefrontOBJ
{
    class OBJMesh
    {
        public string Name;

        public bool HasPosition;
        public bool HasNormal;
        public bool HasTexCoord;
        public bool HasColor;

        public string MaterialName;

        public List<PICAVertex> Vertices;

        public OBJMesh(string MaterialName = "NoMaterial")
        {
            this.MaterialName = MaterialName;

            Vertices = new List<PICAVertex>();
        }
    }
}
