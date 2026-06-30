using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PMX2FBX
{
    public static class PMXParser
    {
        public static PMXModel Parse(string path)
        {
            byte[] buf = File.ReadAllBytes(path);
            var r = new BinaryReader(new MemoryStream(buf));

            // ── Header ──
            string magic = Encoding.ASCII.GetString(r.ReadBytes(4));
            if (magic != "PMX ") throw new Exception("不是有效的 PMX 文件");
            float version = r.ReadSingle();
            int globCount = r.ReadByte();
            byte[] g = r.ReadBytes(globCount);

            int enc       = g[0]; // 0=UTF16LE 1=UTF8
            int addUV     = g[1];
            int vidxSz    = g[2];
            int tidxSz    = g[3];
            int midxSz    = g[4];
            int bidxSz    = g[5];
            int morphIdxSz = g[6];
            int rbIdxSz   = g[7];

            Func<int, int> readIdx = sz =>
            {
                if (sz == 1) { byte v = r.ReadByte(); return v == 0xFF ? -1 : v; }
                if (sz == 2) { ushort v = r.ReadUInt16(); return v == 0xFFFF ? -1 : v; }
                return r.ReadInt32();
            };

            Func<string> readStr = () =>
            {
                int len = r.ReadInt32();
                if (len == 0) return "";
                byte[] bytes = r.ReadBytes(len);
                return enc == 0
                    ? Encoding.Unicode.GetString(bytes)
                    : Encoding.UTF8.GetString(bytes);
            };

            Func<Vector2> rv2 = () => new Vector2(r.ReadSingle(), r.ReadSingle());
            Func<Vector3> rv3 = () => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            Func<Vector4> rv4 = () => new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

            var model = new PMXModel();

            // ── Info ──
            model.ModelNameJP = readStr();
            model.ModelNameEN = readStr();
            readStr(); readStr(); 

            // ── Vertices ──
            int vCount = r.ReadInt32();
            model.Positions   = new Vector3[vCount];
            model.Normals     = new Vector3[vCount];
            model.UVs         = new Vector2[vCount];
            model.BoneIndices = new int[vCount * 4];
            model.BoneWeights = new float[vCount * 4];
            for (int i = 0; i < vCount * 4; i++) model.BoneIndices[i] = -1;

            for (int i = 0; i < vCount; i++)
            {
                model.Positions[i] = rv3();
                model.Normals[i]   = rv3();
                model.UVs[i]       = rv2();
                for (int j = 0; j < addUV; j++) rv4();

                int wtype = r.ReadByte();
                if (wtype == 0)
                {
                    model.BoneIndices[i * 4]     = readIdx(bidxSz);
                    model.BoneWeights[i * 4]     = 1f;
                }
                else if (wtype == 1)
                {
                    model.BoneIndices[i * 4]     = readIdx(bidxSz);
                    model.BoneIndices[i * 4 + 1] = readIdx(bidxSz);
                    float w = r.ReadSingle();
                    model.BoneWeights[i * 4]     = w;
                    model.BoneWeights[i * 4 + 1] = 1f - w;
                }
                else if (wtype == 2)
                {
                    for (int k = 0; k < 4; k++) model.BoneIndices[i * 4 + k] = readIdx(bidxSz);
                    for (int k = 0; k < 4; k++) model.BoneWeights[i * 4 + k] = r.ReadSingle();
                }
                else if (wtype == 3) 
                {
                    model.BoneIndices[i * 4]     = readIdx(bidxSz);
                    model.BoneIndices[i * 4 + 1] = readIdx(bidxSz);
                    float w = r.ReadSingle();
                    model.BoneWeights[i * 4]     = w;
                    model.BoneWeights[i * 4 + 1] = 1f - w;
                    rv3(); rv3(); rv3(); 
                }
                else if (wtype == 4) 
                {
                    for (int k = 0; k < 4; k++) model.BoneIndices[i * 4 + k] = readIdx(bidxSz);
                    for (int k = 0; k < 4; k++) model.BoneWeights[i * 4 + k] = r.ReadSingle();
                }
                r.ReadSingle(); 
            }

            // ── Faces ──
            int fCount = r.ReadInt32();
            model.Faces = new int[fCount];
            for (int i = 0; i < fCount; i++) model.Faces[i] = readIdx(vidxSz);

            // ── Textures ──
            int texCount = r.ReadInt32();
            model.Textures = new string[texCount];
            for (int i = 0; i < texCount; i++) model.Textures[i] = readStr();

            // ── Materials ──
            int matCount = r.ReadInt32();
            model.Materials = new PMXMaterial[matCount];
            int faceOff = 0;
            for (int i = 0; i < matCount; i++)
            {
                var m = new PMXMaterial();
                m.NameJP = readStr(); m.NameEN = readStr();
                Vector4 diff = rv4();
                m.Diffuse = new Color(diff.x, diff.y, diff.z, diff.w);
                Vector3 spec = rv3();
                m.SpecularPower = r.ReadSingle();
                m.Specular = new Color(spec.x, spec.y, spec.z);
                Vector3 amb = rv3();
                m.Ambient = new Color(amb.x, amb.y, amb.z);
                r.ReadByte(); 
                rv4(); r.ReadSingle(); 
                m.TextureIndex = readIdx(tidxSz);
                readIdx(tidxSz); 
                r.ReadByte(); 
                int toonShared = r.ReadByte();
                if (toonShared == 0) readIdx(tidxSz); else r.ReadByte();
                readStr(); 
                m.FaceCount  = r.ReadInt32();
                m.FaceOffset = faceOff;
                faceOff += m.FaceCount;
                model.Materials[i] = m;
            }

            // ── 骨骼 ──
            int boneCount = r.ReadInt32();
            model.Bones = new PMXBone[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = new PMXBone();
                b.NameJP = readStr(); b.NameEN = readStr();
                b.Position    = rv3();
                b.ParentIndex = readIdx(bidxSz);
                r.ReadInt32(); 
                ushort flags = r.ReadUInt16();
                if ((flags & 0x0001) != 0) readIdx(bidxSz); else rv3(); // tail
                if ((flags & 0x0100) != 0 || (flags & 0x0200) != 0) { readIdx(bidxSz); r.ReadSingle(); }
                if ((flags & 0x0400) != 0) rv3();
                if ((flags & 0x0800) != 0) { rv3(); rv3(); }
                if ((flags & 0x2000) != 0) readIdx(bidxSz);
                if ((flags & 0x0020) != 0) 
                {
                    readIdx(bidxSz); r.ReadInt32(); r.ReadSingle();
                    int linkCount = r.ReadInt32();
                    for (int j = 0; j < linkCount; j++)
                    {
                        readIdx(bidxSz);
                        if (r.ReadByte() != 0) { rv3(); rv3(); }
                    }
                }
                model.Bones[i] = b;
            }
            for (int i = 0; i < boneCount; i++)
            {
                int p = model.Bones[i].ParentIndex;
                if (p >= 0 && p < boneCount) model.Bones[p].Children.Add(i);
            }

            // ── BlendShape ──
            int morphCount = r.ReadInt32();
            var morphList = new List<PMXMorph>(morphCount);
            for (int i = 0; i < morphCount; i++)
            {
                var mo = new PMXMorph();
                mo.NameJP = readStr(); mo.NameEN = readStr();
                r.ReadByte(); 
                mo.Type = r.ReadByte();
                int offCount = r.ReadInt32();
                if (mo.Type == 1)
                {
                    mo.Offsets = new PMXVertexOffset[offCount];
                    for (int j = 0; j < offCount; j++)
                        mo.Offsets[j] = new PMXVertexOffset { VertexIndex = readIdx(vidxSz), Offset = rv3() };
                }
                else
                {
                    for (int j = 0; j < offCount; j++)
                    {
                        switch (mo.Type)
                        {
                            case 0: readIdx(morphIdxSz); r.ReadSingle(); break;
                            case 2: readIdx(bidxSz); rv3(); rv4(); break;
                            case 3: case 4: case 5: case 6: case 7: readIdx(vidxSz); rv4(); break;
                            case 8: readIdx(midxSz); r.ReadByte(); rv4(); rv3(); r.ReadSingle(); rv3(); rv4(); r.ReadSingle(); rv4(); rv4(); rv4();  break;
                            case 9: readIdx(morphIdxSz); break;
                            case 10: readIdx(rbIdxSz); r.ReadByte(); rv3(); rv3(); break;
                        }
                    }
                }
                morphList.Add(mo);
            }
            model.Morphs = morphList.ToArray();

            int dfCount = r.ReadInt32();
            for (int i = 0; i < dfCount; i++)
            {
                readStr(); readStr(); r.ReadByte();
                int elemCount = r.ReadInt32();
                for (int j = 0; j < elemCount; j++)
                {
                    int t = r.ReadByte();
                    if (t == 0) readIdx(bidxSz); else readIdx(morphIdxSz);
                }
            }

            // ── 刚体 ──
            int rbCount = r.ReadInt32();
            model.RigidBodies = new PMXRigidBody[rbCount];
            for (int i = 0; i < rbCount; i++)
            {
                var rb = new PMXRigidBody();
                rb.NameJP = readStr(); rb.NameEN = readStr();
                rb.BoneIndex = readIdx(bidxSz);
                r.ReadByte(); r.ReadUInt16(); 
                rb.Shape = r.ReadByte();
                rv3(); rv3(); rv3(); 
                r.ReadSingle(); r.ReadSingle(); r.ReadSingle(); r.ReadSingle(); r.ReadSingle(); // physics
                rb.Mode = r.ReadByte();
                model.RigidBodies[i] = rb;
            }

            return model;
        }
    }
}
