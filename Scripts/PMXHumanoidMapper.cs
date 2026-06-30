using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PMX2FBX
{
    public static class PMXHumanoidMapper
    {
        // ─── PMX 日文骨骼名 → HumanBodyBones 映射 ────
        private static readonly Dictionary<string, HumanBodyBones> PMXBoneMap =
            new Dictionary<string, HumanBodyBones>
        {
            { "腰",             HumanBodyBones.Hips },
            { "上半身",         HumanBodyBones.Spine },
            { "上半身2",        HumanBodyBones.Chest },
            { "上半身3",        HumanBodyBones.UpperChest },
            { "首",             HumanBodyBones.Neck },
            { "頭",             HumanBodyBones.Head },

            { "左肩P",          HumanBodyBones.LeftShoulder },
            { "左腕",           HumanBodyBones.LeftUpperArm },
            { "左ひじ",         HumanBodyBones.LeftLowerArm },
            { "左手首",         HumanBodyBones.LeftHand },

            { "右肩P",          HumanBodyBones.RightShoulder },
            { "右腕",           HumanBodyBones.RightUpperArm },
            { "右ひじ",         HumanBodyBones.RightLowerArm },
            { "右手首",         HumanBodyBones.RightHand },

            { "左足",           HumanBodyBones.LeftUpperLeg },
            { "左ひざ",         HumanBodyBones.LeftLowerLeg },
            { "左足首",         HumanBodyBones.LeftFoot },
            { "左つま先",       HumanBodyBones.LeftToes },

            { "右足",           HumanBodyBones.RightUpperLeg },
            { "右ひざ",         HumanBodyBones.RightLowerLeg },
            { "右足首",         HumanBodyBones.RightFoot },
            { "右つま先",       HumanBodyBones.RightToes },

            { "左目",           HumanBodyBones.LeftEye },
            { "右目",           HumanBodyBones.RightEye },

            { "左親指０",       HumanBodyBones.LeftThumbProximal },
            { "左親指１",       HumanBodyBones.LeftThumbIntermediate },
            { "左親指２",       HumanBodyBones.LeftThumbDistal },

            { "左人指１",       HumanBodyBones.LeftIndexProximal },
            { "左人指２",       HumanBodyBones.LeftIndexIntermediate },
            { "左人指３",       HumanBodyBones.LeftIndexDistal },

            { "左中指１",       HumanBodyBones.LeftMiddleProximal },
            { "左中指２",       HumanBodyBones.LeftMiddleIntermediate },
            { "左中指３",       HumanBodyBones.LeftMiddleDistal },

            { "左薬指１",       HumanBodyBones.LeftRingProximal },
            { "左薬指２",       HumanBodyBones.LeftRingIntermediate },
            { "左薬指３",       HumanBodyBones.LeftRingDistal },

            { "左小指１",       HumanBodyBones.LeftLittleProximal },
            { "左小指２",       HumanBodyBones.LeftLittleIntermediate },
            { "左小指３",       HumanBodyBones.LeftLittleDistal },

            { "右親指０",       HumanBodyBones.RightThumbProximal },
            { "右親指１",       HumanBodyBones.RightThumbIntermediate },
            { "右親指２",       HumanBodyBones.RightThumbDistal },

            { "右人指１",       HumanBodyBones.RightIndexProximal },
            { "右人指２",       HumanBodyBones.RightIndexIntermediate },
            { "右人指３",       HumanBodyBones.RightIndexDistal },

            { "右中指１",       HumanBodyBones.RightMiddleProximal },
            { "右中指２",       HumanBodyBones.RightMiddleIntermediate },
            { "右中指３",       HumanBodyBones.RightMiddleDistal },

            { "右薬指１",       HumanBodyBones.RightRingProximal },
            { "右薬指２",       HumanBodyBones.RightRingIntermediate },
            { "右薬指３",       HumanBodyBones.RightRingDistal },

            { "右小指１",       HumanBodyBones.RightLittleProximal },
            { "右小指２",       HumanBodyBones.RightLittleIntermediate },
            { "右小指３",       HumanBodyBones.RightLittleDistal },
        };

        public static int RuleCount => PMXBoneMap.Count;

        public static bool ApplyMapping(string fbxAssetPath, out string outReason, out int matchCount)
        {
            outReason = null;
            matchCount = 0;

            ModelImporter importer = AssetImporter.GetAtPath(fbxAssetPath) as ModelImporter;
            if (importer == null)
            {
                outReason = "该资产不是 ModelImporter。";
                return false;
            }
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                outReason = "Rig 类型不是 Humanoid。";
                return false;
            }

            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            Transform root = fbxPrefab != null ? fbxPrefab.transform : null;
            if (root == null)
            {
                outReason = "无法加载 FBX 根节点。";
                return false;
            }

            var allBones = new Dictionary<string, string>();
            CollectBones(root, allBones);

            var humanBones = new List<HumanBone>();
            int hits = 0;
            foreach (var kv in PMXBoneMap)
            {
                string pmxName = kv.Key;
                HumanBodyBones humanBone = kv.Value;
                string humanBoneName = HumanTrait.BoneName[(int)humanBone];

                if (allBones.TryGetValue(pmxName, out string foundName))
                {
                    humanBones.Add(new HumanBone
                    {
                        humanName = humanBoneName,
                        boneName  = foundName,
                        limit     = new HumanLimit { useDefaultValues = true }
                    });
                    hits++;
                }
            }
            matchCount = hits;

            if (hits == 0)
            {
                outReason = "没有任何骨骼被匹配，请确认模型使用的是 PMX 日文骨骼名称。";
                return false;
            }

            HumanDescription desc = importer.humanDescription;
            desc.human    = humanBones.ToArray();
            desc.skeleton = BuildSkeletonBones(root);
            importer.humanDescription = desc;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            outReason = $"匹配完成：{hits} / {PMXBoneMap.Count}";
            return true;
        }

        private static void CollectBones(Transform t, Dictionary<string, string> dict)
        {
            if (!dict.ContainsKey(t.name))
                dict[t.name] = t.name;
            foreach (Transform child in t)
                CollectBones(child, dict);
        }

        private static SkeletonBone[] BuildSkeletonBones(Transform root)
        {
            var list = new List<SkeletonBone>();
            BuildSkeletonBonesRecursive(root, list);
            return list.ToArray();
        }

        private static void BuildSkeletonBonesRecursive(Transform t, List<SkeletonBone> list)
        {
            list.Add(new SkeletonBone
            {
                name     = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale    = t.localScale
            });
            foreach (Transform child in t)
                BuildSkeletonBonesRecursive(child, list);
        }
    }
}
