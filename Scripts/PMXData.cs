using System.Collections.Generic;
using UnityEngine;

namespace PMX2FBX
{
    public class PMXModel
    {
        public string ModelNameJP;
        public string ModelNameEN;

        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector2[] UVs;
        public int[]     BoneIndices; 
        public float[]   BoneWeights; 
        public int[]     Faces;       

        public string[]    Textures;
        public PMXMaterial[] Materials;
        public PMXBone[]   Bones;
        public PMXMorph[]  Morphs;
        public PMXRigidBody[] RigidBodies;
    }

    public class PMXMaterial
    {
        public string NameJP, NameEN;
        public Color  Diffuse;
        public Color  Specular;
        public float  SpecularPower;
        public Color  Ambient;
        public int    TextureIndex;
        public int    FaceOffset; 
        public int    FaceCount; 
    }

    public class PMXBone
    {
        public string NameJP, NameEN;
        public Vector3 Position;
        public int ParentIndex;
        public List<int> Children = new List<int>();
    }

    public class PMXMorph
    {
        public string NameJP, NameEN;
        public int Type; 
        public PMXVertexOffset[] Offsets; 
    }

    public struct PMXVertexOffset
    {
        public int VertexIndex;
        public Vector3 Offset;
    }

    public class PMXRigidBody
    {
        public string NameJP, NameEN;
        public int BoneIndex;
        public int Shape;
        public int Mode;
    }
}
