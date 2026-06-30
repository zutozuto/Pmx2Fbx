using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PMX2FBX
{
    public class FBXWriterOptions
    {
        public bool WriteSkeleton = true;
        public bool WriteMorphs = true;
        public bool WriteRigidBodies = false;
        public bool FlipZ = true;
        public float Scale = 1f;
        public string OutputDir;  
        public Dictionary<string, string> TexturePaths; 
        public int BoneNameLang = 1; // 0=日文, 1=英文, 2=日文+英文
    }

    public static class FBXWriter
    {
        static long _id;
        static long NextId() => ++_id;

        static string F(float v) => v.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

        public static void Write(PMXModel pmx, string fbxPath, FBXWriterOptions opts)
        {
            _id = 100000;
            bool flipZ = opts.FlipZ;
            int lang = opts.BoneNameLang;
            float scale = opts.Scale;

            long geomId = NextId();
            long meshId = NextId();
            long skinId = NextId();
            long rootId = NextId();
            long poseId = NextId();
            long bsDefId = NextId();

            long[] matIds = new long[pmx.Materials.Length];
            long[] texIds = new long[pmx.Textures.Length];
            long[] vidIds = new long[pmx.Textures.Length];
            long[] boneIds = new long[pmx.Bones.Length];
            long[] clusterIds = new long[pmx.Bones.Length];
            for (int i = 0; i < matIds.Length; i++) matIds[i] = NextId();
            for (int i = 0; i < texIds.Length; i++) texIds[i] = NextId();
            for (int i = 0; i < vidIds.Length; i++) vidIds[i] = NextId();
            for (int i = 0; i < boneIds.Length; i++) boneIds[i] = NextId();
            for (int i = 0; i < clusterIds.Length; i++) clusterIds[i] = NextId();

            var vmorphs = new List<PMXMorph>();
            foreach (var m in pmx.Morphs) if (m.Type == 1 && m.Offsets != null) vmorphs.Add(m);
            long[] bsChanIds = new long[vmorphs.Count];
            long[] bsShapeIds = new long[vmorphs.Count];
            for (int i = 0; i < vmorphs.Count; i++) { bsChanIds[i] = NextId(); bsShapeIds[i] = NextId(); }

            long[] rbIds = opts.WriteRigidBodies ? new long[pmx.RigidBodies.Length] : Array.Empty<long>();
            if (opts.WriteRigidBodies)
                for (int i = 0; i < rbIds.Length; i++) rbIds[i] = NextId();

            int boneCount = pmx.Bones.Length;
            Vector3[] boneWorldPos = new Vector3[boneCount];
            var sorted = new int[boneCount];
            int sortedCount = 0;
            var children = new System.Collections.Generic.List<int>[boneCount];
            for (int i = 0; i < boneCount; i++) children[i] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < boneCount; i++)
            {
                int p = pmx.Bones[i].ParentIndex;
                if (p >= 0 && p < boneCount) children[p].Add(i);
            }
            var queue = new System.Collections.Generic.Queue<int>();
            for (int i = 0; i < boneCount; i++)
                if (pmx.Bones[i].ParentIndex < 0) queue.Enqueue(i);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                sorted[sortedCount++] = i;
                foreach (int c in children[i]) queue.Enqueue(c);
            }
            if (sortedCount < boneCount) UnityEngine.Debug.LogWarning($"[PMX2FBX] Only {sortedCount}/{boneCount} bones reachable from roots. Possible cycle or orphan bones.");
            for (int k = 0; k < boneCount; k++)
            {
                int i = sorted[k];
                var b = pmx.Bones[i];
                Vector3 pos = b.Position;
                if (flipZ) pos.z = -pos.z;
                boneWorldPos[i] = pos;
            }

            Vector3[] boneLocalOffset = new Vector3[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = pmx.Bones[i];
                Vector3 wp = boneWorldPos[i];
                if (b.ParentIndex >= 0 && b.ParentIndex < boneCount)
                    boneLocalOffset[i] = wp - boneWorldPos[b.ParentIndex];
                else
                    boneLocalOffset[i] = wp;
            }

            // 处理贴图: 复制到输出目录
            var texBaseNames = new string[pmx.Textures.Length];
            for (int i = 0; i < pmx.Textures.Length; i++)
            {
                string tp = pmx.Textures[i].Replace('\\', '/');
                texBaseNames[i] = Path.GetFileName(tp);
                if (opts.TexturePaths != null && opts.OutputDir != null)
                {
                    string texDir = Path.Combine(opts.OutputDir, "Textures");
                    if (!Directory.Exists(texDir)) Directory.CreateDirectory(texDir);
                    string srcPath = null;
                    opts.TexturePaths.TryGetValue(texBaseNames[i], out srcPath);
                    if (srcPath == null) opts.TexturePaths.TryGetValue(tp, out srcPath);
                    if (srcPath != null && File.Exists(srcPath))
                    {
                        string dst = Path.Combine(texDir, texBaseNames[i]);
                        if (!File.Exists(dst)) File.Copy(srcPath, dst);
                    }
                }
            }

            int vCount = pmx.Positions.Length;

            using var sw = new StreamWriter(fbxPath, false, new UTF8Encoding(false));

            sw.WriteLine("; FBX 7.4.0 project file");
            sw.WriteLine("; Generated by PMX2FBX Unity Plugin");
            sw.WriteLine();
            sw.WriteLine("FBXHeaderExtension:  {");
            sw.WriteLine("\tFBXHeaderVersion: 1003");
            sw.WriteLine("\tFBXVersion: 7400");
            sw.WriteLine("\tCreationTimeStamp:  {");
            sw.WriteLine("\t\tVersion: 1000\n\t\tYear: 2024\n\t\tMonth: 1\n\t\tDay: 1");
            sw.WriteLine("\t\tHour: 0\n\t\tMinute: 0\n\t\tSecond: 0\n\t\tMillisecond: 0");
            sw.WriteLine("\t}");
            sw.WriteLine($"\tCreator: \"PMX2FBX Unity Plugin\"");
            sw.WriteLine("}");
            sw.WriteLine();

            sw.WriteLine("GlobalSettings:  {");
            sw.WriteLine("\tVersion: 1000");
            sw.WriteLine("\tProperties70:  {");
            sw.WriteLine("\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1");
            sw.WriteLine("\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
            sw.WriteLine("\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
            sw.WriteLine("\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
            sw.WriteLine("\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
            sw.WriteLine("\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
            sw.WriteLine("\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",8");
            sw.WriteLine("\t}");
            sw.WriteLine("}");
            sw.WriteLine();

            int deformerCount = (opts.WriteSkeleton ? boneCount + 1 : 0) +
                                (opts.WriteMorphs && vmorphs.Count > 0 ? vmorphs.Count + 1 : 0);
            sw.WriteLine("Definitions:  {");
            sw.WriteLine("\tVersion: 100");
            sw.WriteLine($"\tObjectType: \"GlobalSettings\" {{\n\t\tCount: 1\n\t}}");
            sw.WriteLine($"\tObjectType: \"Model\" {{\n\t\tCount: {(opts.WriteSkeleton ? boneCount + 1 : 0) + 1}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Geometry\" {{\n\t\tCount: 1\n\t}}");
            int shapeCount = opts.WriteMorphs ? vmorphs.Count : 0;
            if (shapeCount > 0)
                sw.WriteLine($"\tObjectType: \"Shape\" {{\n\t\tCount: {shapeCount}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Material\" {{\n\t\tCount: {pmx.Materials.Length}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Texture\" {{\n\t\tCount: {pmx.Textures.Length}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Video\" {{\n\t\tCount: {pmx.Textures.Length}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Deformer\" {{\n\t\tCount: {deformerCount}\n\t}}");
            sw.WriteLine($"\tObjectType: \"Pose\" {{\n\t\tCount: 1\n\t}}");
            sw.WriteLine("}");
            sw.WriteLine();

            sw.WriteLine("Objects:  {");

            // ── 网格 ──
            sw.WriteLine($"\tGeometry: {geomId}, \"Geometry::\", \"Mesh\" {{");

            var vBuf = new StringBuilder(vCount * 3 * 12);
            for (int i = 0; i < vCount; i++)
            {
                float x = pmx.Positions[i].x * scale;
                float y = pmx.Positions[i].y * scale;
                float z = flipZ ? -pmx.Positions[i].z * scale : pmx.Positions[i].z * scale;
                if (i > 0) vBuf.Append(',');
                vBuf.Append(F(x)).Append(',').Append(F(y)).Append(',').Append(F(z));
            }
            sw.WriteLine($"\t\tVertices: *{vCount * 3} {{");
            sw.WriteLine($"\t\t\ta: {vBuf}");
            sw.WriteLine("\t\t}");

            // 三角面
            int triCount = pmx.Faces.Length / 3;
            var pviBuf = new StringBuilder(triCount * 3 * 8);
            for (int i = 0; i < triCount; i++)
            {
                int a = pmx.Faces[i * 3], b2 = pmx.Faces[i * 3 + 1], c = pmx.Faces[i * 3 + 2];
                if (i > 0) pviBuf.Append(',');
                if (flipZ) pviBuf.Append(a).Append(',').Append(c).Append(',').Append(~b2);
                else pviBuf.Append(a).Append(',').Append(b2).Append(',').Append(~c);
            }
            sw.WriteLine($"\t\tPolygonVertexIndex: *{triCount * 3} {{");
            sw.WriteLine($"\t\t\ta: {pviBuf}");
            sw.WriteLine("\t\t}");

            // 法线 
            var nBuf = new StringBuilder(vCount * 3 * 12);
            for (int i = 0; i < vCount; i++)
            {
                float nx = pmx.Normals[i].x, ny = pmx.Normals[i].y;
                float nz = flipZ ? -pmx.Normals[i].z : pmx.Normals[i].z;
                if (i > 0) nBuf.Append(',');
                nBuf.Append(F(nx)).Append(',').Append(F(ny)).Append(',').Append(F(nz));
            }
            var niStr = pviBuf.ToString().Replace("~", "").Split(',');
            var niBuf = new StringBuilder(triCount * 3 * 6);
            for (int i = 0; i < triCount * 3; i++)
            {
                int idx;
                if (flipZ)
                {
                    int ti = i / 3, ki = i % 3;
                    if (ki == 0) idx = pmx.Faces[ti * 3];
                    else if (ki == 1) idx = pmx.Faces[ti * 3 + 2];
                    else idx = pmx.Faces[ti * 3 + 1];
                }
                else
                {
                    int ti = i / 3, ki = i % 3;
                    if (ki == 0) idx = pmx.Faces[ti * 3];
                    else if (ki == 1) idx = pmx.Faces[ti * 3 + 1];
                    else idx = pmx.Faces[ti * 3 + 2];
                }
                if (i > 0) niBuf.Append(',');
                niBuf.Append(idx);
            }

            sw.WriteLine("\t\tLayerElementNormal: 0 {");
            sw.WriteLine("\t\t\tVersion: 101\n\t\t\tName: \"\"");
            sw.WriteLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
            sw.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");
            sw.WriteLine($"\t\t\tNormals: *{vCount * 3} {{\n\t\t\t\ta: {nBuf}\n\t\t\t}}");
            sw.WriteLine($"\t\t\tNormalsIndex: *{triCount * 3} {{\n\t\t\t\ta: {niBuf}\n\t\t\t}}");
            sw.WriteLine("\t\t}");

            // UV 
            var uvBuf = new StringBuilder(vCount * 2 * 10);
            for (int i = 0; i < vCount; i++)
            {
                if (i > 0) uvBuf.Append(',');
                uvBuf.Append(F(pmx.UVs[i].x)).Append(',').Append(F(1f - pmx.UVs[i].y));
            }
            sw.WriteLine("\t\tLayerElementUV: 0 {");
            sw.WriteLine("\t\t\tVersion: 101\n\t\t\tName: \"UVMap\"");
            sw.WriteLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
            sw.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");
            sw.WriteLine($"\t\t\tUV: *{vCount * 2} {{\n\t\t\t\ta: {uvBuf}\n\t\t\t}}");
            sw.WriteLine($"\t\t\tUVIndex: *{triCount * 3} {{\n\t\t\t\ta: {niBuf}\n\t\t\t}}");
            sw.WriteLine("\t\t}");

            // 材质
            var matBuf = new StringBuilder(triCount * 2);
            for (int mi = 0; mi < pmx.Materials.Length; mi++)
            {
                int fc = pmx.Materials[mi].FaceCount / 3;
                for (int j = 0; j < fc; j++) { if (matBuf.Length > 0) matBuf.Append(','); matBuf.Append(mi); }
            }
            sw.WriteLine("\t\tLayerElementMaterial: 0 {");
            sw.WriteLine("\t\t\tVersion: 101\n\t\t\tName: \"\"");
            sw.WriteLine("\t\t\tMappingInformationType: \"ByPolygon\"");
            sw.WriteLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");
            sw.WriteLine($"\t\t\tMaterials: *{triCount} {{\n\t\t\t\ta: {matBuf}\n\t\t\t}}");
            sw.WriteLine("\t\t}");

            sw.WriteLine("\t\tLayer: 0 {\n\t\t\tVersion: 100");
            sw.WriteLine("\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementNormal\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}");
            sw.WriteLine("\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementUV\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}");
            sw.WriteLine("\t\t\tLayerElement:  {\n\t\t\t\tType: \"LayerElementMaterial\"\n\t\t\t\tTypedIndex: 0\n\t\t\t}");
            sw.WriteLine("\t\t}");
            sw.WriteLine("\t}"); // 几何体
            sw.WriteLine();

            // ── 网格模型 ──
            sw.WriteLine($"\tModel: {meshId}, \"Model::Body\", \"Mesh\" {{");
            sw.WriteLine("\t\tVersion: 232");
            sw.WriteLine("\t\tProperties70:  {");
            sw.WriteLine("\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",0,0,0");
            sw.WriteLine("\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0");
            sw.WriteLine("\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");
            sw.WriteLine("\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",1");
            sw.WriteLine("\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
            sw.WriteLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
            sw.WriteLine("\t\t}");
            sw.WriteLine("\t\tShading: Y\n\t\tCulling: \"CullingOff\"");
            sw.WriteLine("\t}");
            sw.WriteLine();

            if (opts.WriteSkeleton)
            {
                sw.WriteLine($"\tModel: {rootId}, \"Model::RootNode\", \"Null\" {{");
                WriteLimbProps(sw, 0, 0, 0);
                sw.WriteLine("\t}");
                sw.WriteLine();

                // 骨骼
                for (int k = 0; k < boneCount; k++)
                {
                    int i = sorted[k];
                    var b = pmx.Bones[i];
                    Vector3 localOff = boneLocalOffset[i] * scale;
                    float lx = localOff.x, ly = localOff.y, lz = localOff.z;
                    string bn = BoneName(b, lang);
                    sw.WriteLine($"\tModel: {boneIds[i]}, \"Model::{Esc(bn)}\", \"LimbNode\" {{");
                    sw.WriteLine("\t\tVersion: 232");
                    sw.WriteLine("\t\tProperties70:  {");
                    sw.WriteLine($"\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{F(lx)},{F(ly)},{F(lz)}");
                    sw.WriteLine("\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0");
                    sw.WriteLine("\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");
                    sw.WriteLine("\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",1");
                    sw.WriteLine("\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
                    sw.WriteLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",-1");
                    sw.WriteLine("\t\t\tP: \"filmboxTypeID\", \"short\", \"\", \"\",14");
                    sw.WriteLine("\t\t}");
                    sw.WriteLine("\t\tShading: Y\n\t\tCulling: \"CullingOff\"");
                    sw.WriteLine("\t}");
                    sw.WriteLine();
                }
            }

            // ── 材质 ──
            for (int i = 0; i < pmx.Materials.Length; i++)
            {
                var m = pmx.Materials[i];
                string mn = ChooseName(m.NameEN ?? "", m.NameJP ?? "", lang);
                if (string.IsNullOrWhiteSpace(mn)) mn = $"Mat{i}";
                sw.WriteLine($"\tMaterial: {matIds[i]}, \"Material::{Esc(mn)}\", \"\" {{");
                sw.WriteLine("\t\tVersion: 102\n\t\tShadingModel: \"phong\"\n\t\tMultiLayer: 0");
                sw.WriteLine("\t\tProperties70:  {");
                sw.WriteLine($"\t\t\tP: \"AmbientColor\", \"ColorRGB\", \"Color\", \"\",{F(m.Ambient.r)},{F(m.Ambient.g)},{F(m.Ambient.b)}");
                sw.WriteLine($"\t\t\tP: \"DiffuseColor\", \"ColorRGB\", \"Color\", \"\",{F(m.Diffuse.r)},{F(m.Diffuse.g)},{F(m.Diffuse.b)}");
                sw.WriteLine($"\t\t\tP: \"DiffuseFactor\", \"double\", \"Number\", \"\",{F(m.Diffuse.a)}");
                sw.WriteLine($"\t\t\tP: \"SpecularColor\", \"ColorRGB\", \"Color\", \"\",{F(m.Specular.r)},{F(m.Specular.g)},{F(m.Specular.b)}");
                sw.WriteLine($"\t\t\tP: \"Shininess\", \"double\", \"Number\", \"\",{F(m.SpecularPower)}");
                sw.WriteLine($"\t\t\tP: \"Opacity\", \"double\", \"Number\", \"\",{F(m.Diffuse.a)}");
                sw.WriteLine("\t\t}");
                sw.WriteLine("\t}");
                sw.WriteLine();
            }

            // ── 贴图 ──
            for (int i = 0; i < pmx.Textures.Length; i++)
            {
                string fname = texBaseNames[i];
                sw.WriteLine($"\tVideo: {vidIds[i]}, \"Video::{fname}\", \"Clip\" {{");
                sw.WriteLine("\t\tType: \"Clip\"");
                sw.WriteLine("\t\tProperties70:  {");
                sw.WriteLine($"\t\t\tP: \"Path\", \"KString\", \"XRefUrl\", \"\",\"./{fname}\"");
                sw.WriteLine("\t\t}");
                sw.WriteLine($"\t\tUseMipMap: 0");
                sw.WriteLine($"\t\tFilename: \"./{fname}\"");
                sw.WriteLine($"\t\tRelativeFilename: \"./{fname}\"");
                sw.WriteLine("\t}");
                sw.WriteLine();

                sw.WriteLine($"\tTexture: {texIds[i]}, \"Texture::{fname}\", \"\" {{");
                sw.WriteLine("\t\tType: \"TextureVideoClip\"\n\t\tVersion: 202");
                sw.WriteLine($"\t\tTextureName: \"Texture::{fname}\"");
                sw.WriteLine("\t\tProperties70:  {");
                sw.WriteLine("\t\t\tP: \"CurrentTextureBlendMode\", \"enum\", \"\", \"\",0");
                sw.WriteLine("\t\t\tP: \"UVSet\", \"KString\", \"\", \"\",\"UVMap\"");
                sw.WriteLine("\t\t\tP: \"UseMaterial\", \"bool\", \"\", \"\",1");
                sw.WriteLine("\t\t}");
                sw.WriteLine($"\t\tMedia: \"Video::{fname}\"");
                sw.WriteLine($"\t\tFileName: \"./{fname}\"");
                sw.WriteLine($"\t\tRelativeFilename: \"./{fname}\"");
                sw.WriteLine("\t\tModelUVTranslation: 0,0\n\t\tModelUVScaling: 1,1");
                sw.WriteLine("\t\tTexture_Alpha_Source: \"None\"\n\t\tCropping: 0,0,0,0");
                sw.WriteLine("\t}");
                sw.WriteLine();
            }

            // ── 骨骼蒙皮 ──
            if (opts.WriteSkeleton)
            {
                sw.WriteLine($"\tDeformer: {skinId}, \"Deformer::\", \"Skin\" {{");
                sw.WriteLine("\t\tVersion: 101\n\t\tLink_DeformAcuracy: 50");
                sw.WriteLine("\t}");
                sw.WriteLine();

                for (int bi = 0; bi < boneCount; bi++)
                {
                    var vidxList = new List<int>();
                    var wList = new List<float>();
                    for (int vi = 0; vi < vCount; vi++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            if (pmx.BoneIndices[vi * 4 + k] == bi)
                            {
                                float w = pmx.BoneWeights[vi * 4 + k];
                                if (w > 0f) { vidxList.Add(vi); wList.Add(w); }
                            }
                        }
                    }
                    string cn = BoneName(pmx.Bones[bi], lang);
                    Vector3 bw = boneWorldPos[bi] * scale;
                    float tx = bw.x, ty = bw.y, tz = bw.z;

                    sw.WriteLine($"\tDeformer: {clusterIds[bi]}, \"SubDeformer::{Esc(cn)}\", \"Cluster\" {{");
                    sw.WriteLine("\t\tVersion: 100\n\t\tUserData: \"\", \"\"");
                    if (vidxList.Count > 0)
                    {
                        sw.WriteLine($"\t\tIndexes: *{vidxList.Count} {{\n\t\t\ta: {string.Join(",", vidxList)}\n\t\t}}");
                        sw.WriteLine($"\t\tWeights: *{wList.Count} {{\n\t\t\ta: {JoinF(wList)}\n\t\t}}");
                    }
                    
                    sw.WriteLine($"\t\tTransform: *16 {{\n\t\t\ta: 1,0,0,0,0,1,0,0,0,0,1,0,{F(-tx)},{F(-ty)},{F(-tz)},1\n\t\t}}");
                    sw.WriteLine($"\t\tTransformLink: *16 {{\n\t\t\ta: 1,0,0,0,0,1,0,0,0,0,1,0,{F(tx)},{F(ty)},{F(tz)},1\n\t\t}}");
                    sw.WriteLine("\t}");
                    sw.WriteLine();
                }

                // 绑定姿势
                sw.WriteLine($"\tPose: {poseId}, \"Pose::BindPose\", \"BindPose\" {{");
                sw.WriteLine($"\t\tType: \"BindPose\"\n\t\tVersion: 100\n\t\tNbPoseNodes: {boneCount + 1}");
                sw.WriteLine($"\t\tPoseNode:  {{\n\t\t\tNode: {meshId}");
                sw.WriteLine("\t\t\tMatrix: *16 {\n\t\t\t\ta: 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1\n\t\t\t}");
                sw.WriteLine("\t\t}");
                for (int bi = 0; bi < boneCount; bi++)
                {
                    Vector3 bw = boneWorldPos[bi] * scale;
                    float tx = bw.x, ty = bw.y, tz = bw.z;
                    sw.WriteLine($"\t\tPoseNode:  {{\n\t\t\tNode: {boneIds[bi]}");
                    sw.WriteLine($"\t\t\tMatrix: *16 {{\n\t\t\t\ta: 1,0,0,0,0,1,0,0,0,0,1,0,{F(tx)},{F(ty)},{F(tz)},1\n\t\t\t}}");
                    sw.WriteLine("\t\t}");
                }
                sw.WriteLine("\t}");
                sw.WriteLine();
            }

            // ── BlendShape ──
            if (opts.WriteMorphs && vmorphs.Count > 0)
            {
                sw.WriteLine($"\tDeformer: {bsDefId}, \"Deformer::\", \"BlendShape\" {{\n\t\tVersion: 100\n\t}}");
                sw.WriteLine();

                for (int mi = 0; mi < vmorphs.Count; mi++)
                {
                    var mo = vmorphs[mi];
                    string mn = ChooseName(mo.NameEN ?? "", mo.NameJP ?? "", lang);
                    if (string.IsNullOrWhiteSpace(mn)) mn = $"Morph{mi}";

                    var idxBuf = new StringBuilder();
                    var dposBuf = new StringBuilder();
                    var dnrmBuf = new StringBuilder();
                    int shapeVCount = 0;

                    foreach (var off in mo.Offsets)
                    {
                        int vi = off.VertexIndex;
                        if (vi < 0 || vi >= vCount) continue;

                        if (off.Offset.x == 0f && off.Offset.y == 0f && off.Offset.z == 0f) continue;

                        float dx = off.Offset.x * scale;
                        float dy = off.Offset.y * scale;
                        float dz = flipZ ? -off.Offset.z * scale : off.Offset.z * scale;

                        if (shapeVCount > 0) { idxBuf.Append(','); dposBuf.Append(','); dnrmBuf.Append(','); }
                        idxBuf.Append(vi);
                        dposBuf.Append(F(dx)).Append(',').Append(F(dy)).Append(',').Append(F(dz));
                        dnrmBuf.Append("0,0,0"); 
                        shapeVCount++;
                    }

                    sw.WriteLine($"\tGeometry: {bsShapeIds[mi]}, \"Geometry::{Esc(mn)}\", \"Shape\" {{");
                    sw.WriteLine("\t\tVersion: 100");
                    if (shapeVCount > 0)
                    {
                        sw.WriteLine($"\t\tIndexes: *{shapeVCount} {{\n\t\t\ta: {idxBuf}\n\t\t}}");
                        sw.WriteLine($"\t\tVertices: *{shapeVCount * 3} {{\n\t\t\ta: {dposBuf}\n\t\t}}");
                        sw.WriteLine($"\t\tNormals: *{shapeVCount * 3} {{\n\t\t\ta: {dnrmBuf}\n\t\t}}");
                    }
                    sw.WriteLine("\t}");
                    sw.WriteLine();

                    sw.WriteLine($"\tDeformer: {bsChanIds[mi]}, \"SubDeformer::{Esc(mn)}\", \"BlendShapeChannel\" {{");
                    sw.WriteLine("\t\tVersion: 100\n\t\tDeformPercent: 0");
                    sw.WriteLine("\t\tFullWeights: *1 {\n\t\t\ta: 100\n\t\t}");
                    sw.WriteLine("\t}");
                    sw.WriteLine();
                }
            }

            // ── 刚体 ──
            if (opts.WriteRigidBodies)
            {
                for (int i = 0; i < pmx.RigidBodies.Length; i++)
                {
                    var rb = pmx.RigidBodies[i];
                    string rn = ChooseName(rb.NameEN ?? "", rb.NameJP ?? "", lang);
                    if (string.IsNullOrWhiteSpace(rn)) rn = $"RB{i}";
                    sw.WriteLine($"\tModel: {rbIds[i]}, \"Model::RB_{Esc(rn)}\", \"Null\" {{");
                    WriteLimbProps(sw, 0, 0, 0);
                    sw.WriteLine("\t}");
                    sw.WriteLine();
                }
            }

            sw.WriteLine("}");
            sw.WriteLine();

            sw.WriteLine("Connections:  {");
            sw.WriteLine($"\tC: \"OO\",{geomId},{meshId}");
            sw.WriteLine($"\tC: \"OO\",{meshId},0");
            for (int i = 0; i < pmx.Materials.Length; i++)
                sw.WriteLine($"\tC: \"OO\",{matIds[i]},{meshId}");
            for (int i = 0; i < pmx.Materials.Length; i++)
            {
                int ti = pmx.Materials[i].TextureIndex;
                if (ti >= 0 && ti < pmx.Textures.Length)
                    sw.WriteLine($"\tC: \"OP\",{texIds[ti]},{matIds[i]},\"DiffuseColor\"");
            }
            for (int i = 0; i < pmx.Textures.Length; i++)
                sw.WriteLine($"\tC: \"OO\",{vidIds[i]},{texIds[i]}");

            if (opts.WriteSkeleton)
            {
                sw.WriteLine($"\tC: \"OO\",{skinId},{geomId}");
                for (int bi = 0; bi < boneCount; bi++) sw.WriteLine($"\tC: \"OO\",{clusterIds[bi]},{skinId}");
                for (int bi = 0; bi < boneCount; bi++) sw.WriteLine($"\tC: \"OO\",{boneIds[bi]},{clusterIds[bi]}");
                sw.WriteLine($"\tC: \"OO\",{rootId},0");
                for (int bi = 0; bi < boneCount; bi++)
                {
                    int pi = pmx.Bones[bi].ParentIndex;
                    long parentId = (pi >= 0 && pi < boneCount) ? boneIds[pi] : rootId;
                    sw.WriteLine($"\tC: \"OO\",{boneIds[bi]},{parentId}");
                }
            }

            if (opts.WriteMorphs && vmorphs.Count > 0)
            {
                sw.WriteLine($"\tC: \"OO\",{bsDefId},{geomId}");
                for (int mi = 0; mi < vmorphs.Count; mi++)
                {
                    sw.WriteLine($"\tC: \"OO\",{bsChanIds[mi]},{bsDefId}");
                    sw.WriteLine($"\tC: \"OO\",{bsShapeIds[mi]},{bsChanIds[mi]}");
                }
            }

            if (opts.WriteRigidBodies)
            {
                for (int i = 0; i < pmx.RigidBodies.Length; i++)
                {
                    int bi2 = pmx.RigidBodies[i].BoneIndex;
                    long parentId = (opts.WriteSkeleton && bi2 >= 0 && bi2 < boneCount) ? boneIds[bi2] : 0L;
                    sw.WriteLine($"\tC: \"OO\",{rbIds[i]},{parentId}");
                }
            }

            sw.WriteLine("}");
        }

        static string Esc(string s) => s?.Replace("\"", "'") ?? "";
        static string BoneName(PMXBone b, int lang) =>
            lang == 0 ? ((b.NameJP?.Trim().Length > 0 ? b.NameJP : b.NameEN) ?? "Bone")
          : lang == 1 ? ((b.NameEN?.Trim().Length > 0 ? b.NameEN : b.NameJP) ?? "Bone")
          : ((b.NameJP?.Trim().Length > 0 ? b.NameJP : b.NameEN) ?? "Bone") + " / " +
            ((b.NameEN?.Trim().Length > 0 ? b.NameEN : b.NameJP) ?? "Bone");

        static string ChooseName(string en, string jp, int lang) =>
            lang == 0 ? ((jp?.Trim().Length > 0 ? jp : en) ?? "")
          : lang == 1 ? ((en?.Trim().Length > 0 ? en : jp) ?? "")
          : ((jp?.Trim().Length > 0 ? jp : en) ?? "") + " / " +
            ((en?.Trim().Length > 0 ? en : jp) ?? "");

        static void WriteLimbProps(StreamWriter sw, float x, float y, float z)
        {
            sw.WriteLine("\t\tVersion: 232\n\t\tProperties70:  {");
            sw.WriteLine($"\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{x},{y},{z}");
            sw.WriteLine("\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0");
            sw.WriteLine("\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");
            sw.WriteLine("\t\t}\n\t\tShading: Y\n\t\tCulling: \"CullingOff\"");
        }

        static string JoinF(List<float> lst)
        {
            var sb = new StringBuilder(lst.Count * 10);
            for (int i = 0; i < lst.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(lst[i])); }
            return sb.ToString();
        }
    }
}