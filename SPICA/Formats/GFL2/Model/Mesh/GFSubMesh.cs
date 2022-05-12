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
        
        //Hello007's note:
        //Silly gdkchan, you can't have drip.
        //I mean, efficient vertex indexing. You probably could if the format was not designed by Game Freak, but you can't.
        //Doing so results in the indexing breaking completely.
        //I don't blame you though, this really should be working.
        public bool IsIdx8Bits = false; //DO NOT USE

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

        public GFSubMesh(H3DSubMesh SubMesh, H3DMesh Parent, List<PICAVertex> Vertices, string Name) : this()
        {
            this.Name = Name;
            BoneIndicesCount = (byte)SubMesh.BoneIndicesCount;
            BoneIndices = new byte[SubMesh.BoneIndicesCount];
            for (int i = 0; i < SubMesh.BoneIndicesCount; i++)
            {
                BoneIndices[i] = (byte)SubMesh.BoneIndices[i];
            }

            Indices = SubMesh.Indices;
            
            PrimitiveMode = SubMesh.PrimitiveMode;
            VertexStride = Parent.VertexStride;
            
            Attributes = Parent.Attributes;

            foreach (PICAFixedAttribute Old in Parent.FixedAttributes)
            {
                PICAFixedAttribute New = new PICAFixedAttribute()
                {
                    Name = Old.Name,
                    Value = Old.Value
                };
                FixedAttributes.Add(New);
            }

            List<PICAVertex> NewVertices = new List<PICAVertex>();
            Dictionary<ushort, ushort> OldNewIndexMap = new Dictionary<ushort, ushort>();
            ushort[] NewIndices = new ushort[Indices.Length];

            for (int i = 0; i < NewIndices.Length; i++)
            {
                if (OldNewIndexMap.ContainsKey(Indices[i]))
                {
                    NewIndices[i] = OldNewIndexMap[Indices[i]];
                }
                else
                {
                    PICAVertex Vertex = Vertices[Indices[i]];
                    ushort NewIndex = (ushort)NewVertices.Count;
                    NewVertices.Add(Vertex);
                    NewIndices[i] = NewIndex;
                    OldNewIndexMap.Add(Indices[i], NewIndex);
                }
            }

            Indices = NewIndices;
            IsIdx8Bits = /*NewIndices.Length <= 0x100*/false;
            RawBuffer = VerticesConverter.GetBuffer(NewVertices, Attributes);
        }
    }
}
