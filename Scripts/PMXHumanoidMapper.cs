using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace PMX2FBX
{
    public static class PMXHumanoidMapper
    {
        // ─── HumanBodyBones → 多候选 PMX 骨骼名（顺序即优先级：基础名 → D → EX/IK）──
        private static readonly Dictionary<HumanBodyBones, List<string>> PMXBoneCandidates =
            new Dictionary<HumanBodyBones, List<string>>
        {
            { HumanBodyBones.Hips,            new List<string>{ "腰","グルーブ" } },

            { HumanBodyBones.Spine,           new List<string>{ "上半身" } },
            { HumanBodyBones.Chest,           new List<string>{ "上半身2" } },
            { HumanBodyBones.UpperChest,      new List<string>{ "上半身3" } },
            { HumanBodyBones.Neck,            new List<string>{ "首" } },
            { HumanBodyBones.Head,            new List<string>{ "頭" } },

            { HumanBodyBones.LeftShoulder,    new List<string>{ "左肩P" } },
            { HumanBodyBones.LeftUpperArm,    new List<string>{ "左腕" } },
            { HumanBodyBones.LeftLowerArm,    new List<string>{ "左ひじ" } },
            { HumanBodyBones.LeftHand,        new List<string>{ "左手首" } },

            { HumanBodyBones.RightShoulder,   new List<string>{ "右肩P" } },
            { HumanBodyBones.RightUpperArm,   new List<string>{ "右腕" } },
            { HumanBodyBones.RightLowerArm,   new List<string>{ "右ひじ" } },
            { HumanBodyBones.RightHand,       new List<string>{ "右手首" } },

            // ── 双腿：先基本名，再 D，最后 EX/IK ──
            { HumanBodyBones.LeftUpperLeg,    new List<string>{ "左足", "左足D", "左足EX" } },
            { HumanBodyBones.LeftLowerLeg,    new List<string>{ "左ひざ", "左ひざD", "左ひざEX" } },
            { HumanBodyBones.LeftFoot,        new List<string>{ "左足首", "左足首D", "左足首EX" } },
            { HumanBodyBones.LeftToes,        new List<string>{ "左足つま先", "左足先EX", "左足先" } },

            { HumanBodyBones.RightUpperLeg,   new List<string>{ "右足", "右足D", "右足EX" } },
            { HumanBodyBones.RightLowerLeg,   new List<string>{ "右ひざ", "右ひざD", "右ひざEX" } },
            { HumanBodyBones.RightFoot,       new List<string>{ "右足首", "右足首D", "右足首EX" } },
            { HumanBodyBones.RightToes,       new List<string>{ "右足つま先", "右足先EX", "右足先" } },

            { HumanBodyBones.LeftEye,         new List<string>{ "左目" } },
            { HumanBodyBones.RightEye,        new List<string>{ "右目" } },

            { HumanBodyBones.LeftThumbProximal,        new List<string>{ "左親指０" } },
            { HumanBodyBones.LeftThumbIntermediate,    new List<string>{ "左親指１" } },
            { HumanBodyBones.LeftThumbDistal,          new List<string>{ "左親指２" } },

            { HumanBodyBones.LeftIndexProximal,        new List<string>{ "左人指１" } },
            { HumanBodyBones.LeftIndexIntermediate,    new List<string>{ "左人指２" } },
            { HumanBodyBones.LeftIndexDistal,          new List<string>{ "左人指３" } },

            { HumanBodyBones.LeftMiddleProximal,       new List<string>{ "左中指１" } },
            { HumanBodyBones.LeftMiddleIntermediate,   new List<string>{ "左中指２" } },
            { HumanBodyBones.LeftMiddleDistal,         new List<string>{ "左中指３" } },

            { HumanBodyBones.LeftRingProximal,         new List<string>{ "左薬指１" } },
            { HumanBodyBones.LeftRingIntermediate,     new List<string>{ "左薬指２" } },
            { HumanBodyBones.LeftRingDistal,           new List<string>{ "左薬指３" } },

            { HumanBodyBones.LeftLittleProximal,       new List<string>{ "左小指１" } },
            { HumanBodyBones.LeftLittleIntermediate,   new List<string>{ "左小指２" } },
            { HumanBodyBones.LeftLittleDistal,         new List<string>{ "左小指３" } },

            { HumanBodyBones.RightThumbProximal,       new List<string>{ "右親指０" } },
            { HumanBodyBones.RightThumbIntermediate,   new List<string>{ "右親指１" } },
            { HumanBodyBones.RightThumbDistal,         new List<string>{ "右親指２" } },

            { HumanBodyBones.RightIndexProximal,       new List<string>{ "右人指１" } },
            { HumanBodyBones.RightIndexIntermediate,   new List<string>{ "右人指２" } },
            { HumanBodyBones.RightIndexDistal,         new List<string>{ "右人指３" } },

            { HumanBodyBones.RightMiddleProximal,      new List<string>{ "右中指１" } },
            { HumanBodyBones.RightMiddleIntermediate,  new List<string>{ "右中指２" } },
            { HumanBodyBones.RightMiddleDistal,        new List<string>{ "右中指３" } },

            { HumanBodyBones.RightRingProximal,        new List<string>{ "右薬指１" } },
            { HumanBodyBones.RightRingIntermediate,    new List<string>{ "右薬指２" } },
            { HumanBodyBones.RightRingDistal,          new List<string>{ "右薬指３" } },

            { HumanBodyBones.RightLittleProximal,      new List<string>{ "右小指１" } },
            { HumanBodyBones.RightLittleIntermediate,  new List<string>{ "右小指２" } },
            { HumanBodyBones.RightLittleDistal,        new List<string>{ "右小指３" } },
        };

        public static int RuleCount => PMXBoneCandidates.Count;

        public static bool ApplyMapping(string fbxAssetPath, out string outReason, out int matchCount, out List<string> missingHumanBones)
        {
            outReason = null;
            matchCount = 0;
            missingHumanBones = new List<string>();

            ModelImporter importer = AssetImporter.GetAtPath(fbxAssetPath) as ModelImporter;
            if (importer == null)
            {
                outReason = "资产不是 ModelImporter，无法应用 Humanoid 映射。";
                Debug.LogWarning($"[PMXHumanoidMapper] {outReason} ({fbxAssetPath})");
                return false;
            }
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                outReason = "Rig 类型不是 Humanoid。请先在 FBX 导入器中切到 Humanoid 再运行映射。";
                Debug.LogWarning($"[PMXHumanoidMapper] {outReason} ({fbxAssetPath})");
                return false;
            }

            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            Transform root = fbxPrefab != null ? fbxPrefab.transform : null;
            if (root == null)
            {
                outReason = $"无法加载 FBX 根节点：{fbxAssetPath}";
                Debug.LogWarning($"[PMXHumanoidMapper] {outReason}");
                return false;
            }

            var allBones = new Dictionary<string, Transform>();
            CollectBones(root, allBones);

            var humanBones = new List<HumanBone>();
            foreach (var kv in PMXBoneCandidates)
            {
                HumanBodyBones humanBone = kv.Key;
                var candidates = kv.Value;
                Transform matched = null;
                string matchedName = null;
                foreach (string name in candidates)
                {
                    Transform t;
                    if (allBones.TryGetValue(name, out t))
                    {
                        matched = t;
                        matchedName = name;
                        break;
                    }
                }
                if (matched == null)
                {
                    missingHumanBones.Add($"{HumanTrait.BoneName[(int)humanBone]} (= 候选 {string.Join("/", candidates)})");
                    continue;
                }

                humanBones.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)humanBone],
                    boneName  = matchedName,
                    limit     = new HumanLimit { useDefaultValues = true }
                });
                matchCount++;
            }

            HumanDescription desc = importer.humanDescription;
            desc.human    = humanBones.ToArray();
            desc.skeleton = BuildSkeletonBones(root);
            importer.humanDescription = desc;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // 区分"完全没匹配" vs "部分匹配"
            if (matchCount == 0)
            {
                outReason = "没有任何骨骼被匹配，请确认模型使用的是 PMX 日文骨骼命名。";
                Debug.LogError($"[PMXHumanoidMapper] 失败：{outReason} ({fbxAssetPath})");
                return false;
            }

            if (missingHumanBones.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("部分 Humanoid 骨骼未匹配（Humanoid 映射保存了，已存在的别名）。缺失：");
                sb.Append(string.Join(", ", missingHumanBones));
                outReason = sb.ToString();
                Debug.LogWarning($"[PMXHumanoidMapper] 部分成功 {matchCount}/{PMXBoneCandidates.Count}：{outReason} ({fbxAssetPath})");
            }
            else
            {
                outReason = $"匹配完成：{matchCount} / {PMXBoneCandidates.Count}";
            }
            return true;
        }

        public static bool ApplyMapping(string fbxAssetPath, out string outReason, out int matchCount)
        {
            return ApplyMapping(fbxAssetPath, out outReason, out matchCount, out _);
        }

        private static void CollectBones(Transform t, Dictionary<string, Transform> dict)
        {
            if (!dict.ContainsKey(t.name)) dict[t.name] = t;
            foreach (Transform child in t) CollectBones(child, dict);
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
            foreach (Transform child in t) BuildSkeletonBonesRecursive(child, list);
        }
    }
}
