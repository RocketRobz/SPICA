using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using System.Collections.Generic;

namespace SPICA.Formats.GFL2.Model.Mesh
{
    public class GFSubMesh
    {
        public string Name;

        public byte BoneIndicesCount;

        public byte[] BoneIndices;

        public int VertexStride;

        public ushort[] Indices;

        //Note: All the models observed when writing the model creation logic uses 16 bits
        //for the indices, even those where the indices are always < 256.
        //You can make this store the indices more efficiently when MaxIndex
        //of the Indices buffer is < 256.
        public bool IsIdx8Bits = false;

        public byte[] RawBuffer;

        public PICAPrimitiveMode PrimitiveMode;

        public readonly List<PICAAttribute>      Attributes;
        public readonly List<PICAFixedAttribute> FixedAttributes;

        public GFSubMesh()
        {
            BoneIndices = new byte[0x1f];

            Attributes      = new List<PICAAttribute>();
            FixedAttributes = new List<PICAFixedAttribute>();            
        }

        public GFSubMesh(H3DSubMesh SubMesh, H3DMesh Parent, List<PICAVertex> Vertices, string Name)
        {
            this.Name = Name;
            BoneIndicesCount = (byte)SubMesh.BoneIndicesCount;
            BoneIndices = new byte[SubMesh.BoneIndicesCount];
            for (int i = 0; i < SubMesh.BoneIndicesCount; i++)
            {
                BoneIndices[i] = (byte)SubMesh.BoneIndices[i];
            }

            Indices = SubMesh.Indices;
            IsIdx8Bits = true;
            foreach (ushort Index in Indices)
            {
                if (Index > 0xFF)
                {
                    IsIdx8Bits = false;
                    break;
                }
            }
            
            PrimitiveMode = SubMesh.PrimitiveMode;
            VertexStride = Parent.VertexStride;
            
            Attributes = Parent.Attributes;
            FixedAttributes = Parent.FixedAttributes;

            List<PICAVertex> NewVertices = new List<PICAVertex>();
            ushort[] NewIndices = new ushort[Indices.Length];
            for (int i = 0; i < NewIndices.Length; i++)
            {
                PICAVertex Vertex = Vertices[Indices[i]];
                int NewIndex = NewVertices.IndexOf(Vertex);
                if (NewIndex == -1)
                {
                    NewIndex = NewVertices.Count;
                    NewVertices.Add(Vertex);
                }
                NewIndices[i] = (ushort)NewIndex; 
            }

            Indices = NewIndices;
            RawBuffer = VerticesConverter.GetBuffer(NewVertices, Attributes);
        }
    }
}
