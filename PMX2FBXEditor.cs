using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Autodesk.Fbx;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace PMX2FBX
{
    public class PMX2FBXEditor : EditorWindow
    {
        public enum Language { 汉语, 日语, 韩语, 英语 }
        public Language _lang = Language.汉语;

        private static readonly string[] LangNames = { "中文", "日本語", "한국어", "English" };

        private static readonly Dictionary<string, string[]> UI = new Dictionary<string, string[]>
        {
            // 字典 Key: [汉语, 日语, 韩语, 英语]
            { "title",       new[] { "PMX 模型转换工具", "PMX モデル変換ツール", "PMX 모델 변환 도구", "PMX Model Converter" } },
            { "sec_pmx",     new[] { "① PMX 文件",      "① PMX ファイル",      "① PMX 파일",        "① PMX File" } },
            { "no_file",     new[] { "未选择文件",        "ファイル未選択",        "파일 미선택",         "No file selected" } },
            { "select",      new[] { "选择…",            "選択…",                "선택…",              "Select…" } },
            { "sec_out",     new[] { "② 输出目录",       "② 出力ディレクトリ",    "② 출력 디렉토리",     "② Output Directory" } },
            { "auto_tex",    new[] { "自动查找同目录贴图","同ディレクトリ内テクスチャを自動検索", "동일 디렉토리 텍스처 자동 검색", "Auto-find textures in same folder" } },
            { "sec_opt",     new[] { "③ 转换选项",       "③ 変換オプション",      "③ 변환 옵션",         "③ Conversion Options" } },
            { "opt_skel",    new[] { "保留骨骼结构",      "骨骼構造を保持",        "본 구조 보존",        "Keep Bone Structure" } },
            { "opt_skel_tip",new[] { "将 PMX 骨骼层级写入 FBX，并绑定网格权重", "PMX骨骼階層をFBXに書き込み、网格重みをバインド", "PMX 본 계층을 FBX에 쓰기 및 메시 가중치 바인딩", "Write PMX bone hierarchy to FBX with mesh weights" } },
            { "opt_morph",   new[] { "保留 BlendShape",   "BlendShapeを保持",      "BlendShape 보존",    "Keep BlendShape" } },
            { "opt_morph_tip",new[]{"顶点变形 Morph → FBX BlendShapeChannel", "頂点変形のMorphをFBXのBlendShapeChannelに変換", "정점 변형 Morph → FBX BlendShapeChannel", "Vertex morph → FBX BlendShapeChannel" } },
            { "opt_flipz",   new[] { "Z 轴镜像",         "Z軸ミラー",            "Z축 미러",           "Z-axis Mirror" } },
            { "opt_flipz_tip",new[]{"PMX→FBX 坐标系修正（推荐开启）", "PMX→FBX 座標系修正（推奨）", "PMX→FBX 좌표계 수정 (권장)", "PMX→FBX coordinate correction (recommended)" } },
            { "opt_vrc",     new[] { "适配VRChat",        "VRChat対応",           "VRChat 적용",        "Adapt for VRChat" } },
            { "opt_vrc_tip", new[] { "为模型添加VRChat组件", "モデルにVRChatコンポーネントを追加", "모델에 VRChat 컴포넌트 추가", "Add VRChat components to model" } },
            { "vrc_add_ok",   new[] { "VRChat 组件添加成功", "VRChatコンポーネント追加成功", "VRChat 컴포넌트 추가 성공", "VRChat component added successfully" } },
            { "vrc_add_fail", new[] { "无法添加 VRChat 组件，可能缺少 VRChat SDK。", "VRChatコンポーネントの追加に失敗しました。VRChat SDK が不足している可能性があります。", "VRChat 컴포넌트를 추가할 수 없습니다. VRChat SDK이 누락되었을 수 있습니다.", "Failed to add VRChat component. VRChat SDK may be missing." } },
            { "opt_human",  new[] { "自动映射Humanoid骨骼", "Humanoidボーンを自動マッピング", "Humanoid 본 자동 매핑", "Auto-map Humanoid Bones" } },
            { "opt_human_tip", new[] { "自动将日文骨骼名映射到Unity Humanoid Avatar", "日本語ボーン名をUnity Humanoid Avatarに自動マッピング", "일본어 본 이름을 Unity Humanoid Avatar에 자동 매핑", "Auto-map PMX Japanese bone names to Unity Humanoid Avatar" } },
            { "bone_lang",   new[] { "骨骼命名",          "骨の命名",             "본 명명",            "Bone Naming" } },
            { "bone_jp",     new[] { "日文",              "日本語",               "일본어",             "Japanese" } },
            { "bone_en",     new[] { "英文",              "英語",                 "영어",               "English" } },
            { "bone_jpen",   new[] { "日文+英文",          "日本語+英語",          "일본어+영어",        "Japanese+English" } },
            { "bone_hint0",  new[] { "始终使用 PMX 日文名（NameJP）", "常にPMX日本語名（NameJP）を使用", "항상 PMX 일본어 이름 사용 (NameJP)", "Always use PMX Japanese name (NameJP)" } },
            { "bone_hint1",  new[] { "始终使用 PMX 英文名（NameEN），无英文则用日文", "常にPMX英語名（NameEN）を使用、なければ日本語", "항상 PMX 영어 이름 사용 (NameEN), 없으면 일본어", "Always use PMX English name (NameEN), fallback to Japanese" } },
            { "bone_hint2",  new[] { "格式为「日文 / 英文」", "形式は「日本語 / 英語」", "형식:「일본어 / 영어」", "Format: 「Japanese / English」" } },
            { "btn_convert", new[] { "  开始转换  ",       "  変換開始  ",         "  변환 시작  ",       "  Start Convert  " } },
            { "btn_converting",new[]{"转换中…",            "変換中…",               "변환 중…",            "Converting…" } },
            { "warn_readme", new[] { "*使用前请仔细阅读并遵循模型附带的ReadMe/使用规则文档", "※使用前にモデルに含まれるReadMe/使用規則ドキュメントを必ずご確認ください", "*사용 전에 모델에 포함된 ReadMe/사용 규칙 문서를 반드시 확인해 주세요", "*Please carefully read and follow the model's ReadMe/usage rules document before use" } },
            { "sec_log",     new[] { "日志",                "ログ",                  "로그",               "Log" } },
            { "lang_label",  new[] { "界面语言",            "インターフェース言語",   "인터페이스 언어",      "Interface Language" } },
            { "opt_blend",   new[] { "写入空白 BlendShape", "空白BlendShapeを書き込む", "빈 BlendShape 쓰기", "Write Empty BlendShape" } },
            { "opt_blend_tip",new[]{"在 FBX 中写入空的形态键通道，多用于VRChat声捕设置（推荐开启）", "FBXに空白の形態キー（Delta=0）を書き込み、VRChat音声キャプチャ設定等多用途（推奨）", "FBX에 빈 형태 키(Delt a=0) 쓰기, VRChat 음성 캡처 설정 등에 활용 (권장)", "Write empty morph channels (Delta=0) to FBX, commonly used for VRChat voice capture setup (recommended)" } },
            { "blend_name",  new[] { "BlendShape 名称",    "BlendShape名",         "BlendShape 이름",     "BlendShape Name" } },
            { "blend_mesh",  new[] { "目标网格名称（留空=全部）", "対象メッシュ名（空欄=全て）", "대상 메시 이름 (비움=전체)", "Target Mesh Name (empty=all)" } },
        };

        private string T(string key) => UI.TryGetValue(key, out var arr) ? arr[(int)_lang] : key;

        string  _pmxPath      = "";
        string  _outputDir    = "Assets/Models";
        bool    _writeSkel    = true;
        bool    _writeMorphs  = true;
        bool    _flipZ        = true;
        bool    _autoTextures = true;
        bool    _addVRCComponent = false;
        bool    _autoHumanoidMap = true;
        int     _boneNameLang = 0;
        bool    _addEmptyBlendShape = false;
        string  _blendShapeName = "Blink";
        string  _blendShapeMeshFilter = "";

        Vector2 _scroll;
        string  _log          = "";
        bool    _converting   = false;

        // ── 颜色 ──
        static readonly Color ColBg     = new Color(0.15f, 0.15f, 0.18f);
        static readonly Color ColCard   = new Color(0.20f, 0.20f, 0.24f);
        static readonly Color ColAccent = new Color(0.38f, 0.40f, 0.95f);
        static readonly Color ColOk     = new Color(0.30f, 0.85f, 0.50f);
        static readonly Color ColWarn   = new Color(0.95f, 0.75f, 0.25f);
        static readonly Color ColErr    = new Color(0.95f, 0.35f, 0.35f);

        GUIStyle _styleCard, _styleTitle, _styleSub, _styleLog,
                 _styleBtnMain, _styleBtnSec, _styleLabelBold, _styleSection;
        bool _stylesBuilt;

        [MenuItem("PMX2FBX/PMX2FBX Converter", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<PMX2FBXEditor>(false, "PMX → FBX");
            w.minSize = new Vector2(480, 600);
        }

        void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _styleCard = new GUIStyle(GUI.skin.box)
            {
                normal    = { background = MakeTex(ColCard) },
                padding   = new RectOffset(14, 14, 12, 12),
                margin    = new RectOffset(0, 0, 6, 6),
                border    = new RectOffset(4, 4, 4, 4)
            };

            _styleTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 18,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };

            _styleSub = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.6f, 0.6f, 0.65f) }
            };

            _styleSection = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 10,
                normal    = { textColor = new Color(0.5f, 0.5f, 0.6f) }
            };

            _styleLabelBold = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 12
            };

            _styleBtnMain = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { background = MakeTex(ColAccent), textColor = Color.white },
                hover     = { background = MakeTex(new Color(0.45f, 0.47f, 1f)),  textColor = Color.white },
                active    = { background = MakeTex(new Color(0.30f, 0.32f, 0.8f)), textColor = Color.white },
                padding   = new RectOffset(0, 0, 10, 10),
                border    = new RectOffset(4, 4, 4, 4)
            };

            _styleBtnSec = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                padding   = new RectOffset(8, 8, 5, 5)
            };

            _styleLog = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 11,
                wordWrap  = true,
                richText  = true,
                fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.7f, 0.7f, 0.75f) }
            };
        }

        static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        void OnGUI()
        {
            BuildStyles();

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ColBg);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Space(16);

            // ── 标题 ──
            GUILayout.Label("PMX → FBX", _styleTitle);
            GUILayout.Label(T("title"), _styleSub);
            GUILayout.Space(4);
            GUILayout.Label("by: ずっと子都", EditorStyles.miniLabel);
            GUILayout.Space(8);

            // ── 语言选择 ──
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var langLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, fixedWidth = 0 };
            float labelWidth = Mathf.Max(90, new GUIStyle(EditorStyles.label).CalcSize(new GUIContent(T("lang_label"))).x);
            GUILayout.Label(T("lang_label") + ":", langLabelStyle, GUILayout.Width(labelWidth));
            GUILayout.Space(4);
            _lang = (Language)EditorGUILayout.Popup((int)_lang, LangNames, GUILayout.Width(120));
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // ── 输入文件 ──
            DrawSection(T("sec_pmx"));
            GUILayout.BeginVertical(_styleCard);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.IsNullOrEmpty(_pmxPath) ? T("no_file") : Path.GetFileName(_pmxPath),
                _styleLabelBold, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(T("select"), _styleBtnSec, GUILayout.Width(72)))
            {
                string p = EditorUtility.OpenFilePanel("Select PMX File", "", "pmx");
                if (!string.IsNullOrEmpty(p)) { _pmxPath = p; _log = ""; }
            }
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_pmxPath))
            {
                GUILayout.Space(4);
                GUILayout.Label(ShortenPath(_pmxPath), EditorStyles.miniLabel);
            }
            GUILayout.EndVertical();

            // ── 输出路径 ──
            DrawSection(T("sec_out"));
            GUILayout.BeginVertical(_styleCard);
            GUILayout.BeginHorizontal();
            _outputDir = EditorGUILayout.TextField(_outputDir, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(T("select"), _styleBtnSec, GUILayout.Width(72)))
            {
                string p = EditorUtility.OpenFolderPanel("Select Output Folder", _outputDir, "");
                if (!string.IsNullOrEmpty(p))
                {
                    string proj = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? "");
                    string pNorm = Path.GetFullPath(p);
                    if (!string.IsNullOrEmpty(proj) && pNorm.StartsWith(proj, StringComparison.OrdinalIgnoreCase))
                        p = pNorm.Substring(proj.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    _outputDir = p;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            _autoTextures = EditorGUILayout.Toggle(T("auto_tex"), _autoTextures);
            GUILayout.EndVertical();

            // ── 选项 ──
            DrawSection(T("sec_opt"));
            GUILayout.BeginVertical(_styleCard);
            _writeSkel   = DrawToggle(T("opt_skel"), T("opt_skel_tip"), _writeSkel);
            GUILayout.Space(4);
            _writeMorphs = DrawToggle(T("opt_morph"), T("opt_morph_tip"), _writeMorphs);
            GUILayout.Space(4);
            _autoHumanoidMap = DrawToggle(T("opt_human"), T("opt_human_tip"), _autoHumanoidMap);
            GUILayout.Space(4);
            _flipZ       = DrawToggle(T("opt_flipz"), T("opt_flipz_tip"), _flipZ);
            GUILayout.Space(4);
            _addVRCComponent = DrawToggle(T("opt_vrc"), T("opt_vrc_tip"), _addVRCComponent);
            GUILayout.Space(4);
            _addEmptyBlendShape = DrawToggle(T("opt_blend"), T("opt_blend_tip"), _addEmptyBlendShape);
            if (_addEmptyBlendShape)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(T("blend_name"), GUILayout.Width(EditorStyles.label.CalcSize(new GUIContent(T("blend_name"))).x + 4));
                _blendShapeName = EditorGUILayout.TextField(_blendShapeName);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(T("blend_mesh"), GUILayout.Width(EditorStyles.label.CalcSize(new GUIContent(T("blend_mesh"))).x + 4));
                _blendShapeMeshFilter = EditorGUILayout.TextField(_blendShapeMeshFilter);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(4);
            DrawBoneLangSelector();
            GUILayout.EndVertical();

            // ── 按钮 ──
            GUILayout.Space(8);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_pmxPath) || _converting);
            string btnText = _converting ? T("btn_converting") : T("btn_convert");
            if (GUILayout.Button(btnText, _styleBtnMain, GUILayout.Height(46)))
                Convert();
            EditorGUI.EndDisabledGroup();

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.4f, 0.9f, 1f) } };
            GUILayout.Label(T("warn_readme"), labelStyle);

            // ── 日志 ──
            if (!string.IsNullOrEmpty(_log))
            {
                GUILayout.Space(10);
                DrawSection(T("sec_log"));
                GUILayout.BeginVertical(_styleCard);
                GUILayout.Label(_log, _styleLog);
                GUILayout.EndVertical();
            }

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        void DrawSection(string title)
        {
            GUILayout.Label(title.ToUpper(), _styleSection);
        }

        bool DrawToggle(string label, string tooltip, bool val)
        {
            GUILayout.BeginHorizontal();
            bool r = EditorGUILayout.Toggle(val, GUILayout.Width(18));
            GUILayout.BeginVertical();
            GUILayout.Label(new GUIContent(label, tooltip), _styleLabelBold);
            GUILayout.Label(tooltip, EditorStyles.miniLabel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return r;
        }

        void DrawBoneLangSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("bone_lang"), _styleLabelBold, GUILayout.Width(100));
            string[] options = { T("bone_jp"), T("bone_en"), T("bone_jpen") };
            _boneNameLang = GUILayout.SelectionGrid(_boneNameLang, options, 3, "toggle", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            string hint = _boneNameLang == 0 ? T("bone_hint0")
                             : _boneNameLang == 1 ? T("bone_hint1")
                             : T("bone_hint2");
            GUILayout.Label(hint, EditorStyles.miniLabel);
        }

        void Log(string msg, string color = null)
        {
            string line = color != null ? $"<color={color}>{msg}</color>" : msg;
            _log += (_log.Length > 0 ? "\n" : "") + line;
            Repaint();
        }

        void Convert()
        {
            _log = "";
            _converting = true;
            Repaint();

            try
            {
                if (!File.Exists(_pmxPath)) { Log("错误：找不到 PMX 文件", "#F87171"); return; }

                Log($"解析 PMX：{Path.GetFileName(_pmxPath)}", "#94A3B8");
                PMXModel pmx = PMXParser.Parse(_pmxPath);
                Log($"✓ 顶点: {pmx.Positions.Length}  三角面: {pmx.Faces.Length / 3}", "#4ADE80");
                Log($"  材质: {pmx.Materials.Length}  骨骼: {pmx.Bones.Length}  BlendShape: {CountVMorphs(pmx)}", "#94A3B8");

                string projectRoot = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? "");
                bool outputInProject = _outputDir.StartsWith("Assets", StringComparison.OrdinalIgnoreCase);
                string absOut = outputInProject
                    ? Path.GetFullPath(Path.Combine(projectRoot, _outputDir))
                    : Path.GetFullPath(_outputDir);
                if (outputInProject) Directory.CreateDirectory(absOut);

                var texPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (_autoTextures)
                {
                    string pmxDir = Path.GetDirectoryName(_pmxPath) ?? "";
                    foreach (string tp in pmx.Textures)
                    {
                        string fn = Path.GetFileName(tp.Replace('\\', '/'));
                        string direct = Path.Combine(pmxDir, tp.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(direct)) { texPaths[fn] = direct; continue; }
                        string byName = Path.Combine(pmxDir, fn);
                        if (File.Exists(byName)) { texPaths[fn] = byName; continue; }
                        foreach (string sub in Directory.GetFiles(pmxDir, fn, SearchOption.AllDirectories))
                        {
                            texPaths[fn] = sub; break;
                        }
                    }
                    int found = texPaths.Count;
                    int total = pmx.Textures.Length;
                    Log($"✓ 贴图: 找到 {found}/{total} 个", found == total ? "#4ADE80" : "#FAC775");
                    if (found < total)
                    {
                        foreach (string tp in pmx.Textures)
                        {
                            string fn = Path.GetFileName(tp.Replace('\\', '/'));
                            if (!texPaths.ContainsKey(fn)) Log($"  ⚠ 未找到: {fn}", "#FAC775");
                        }
                    }
                }

                string outName = Path.GetFileNameWithoutExtension(_pmxPath) + ".fbx";
                string outPath = Path.Combine(absOut, outName);

                if (File.Exists(outPath))
                {
                    if (!EditorUtility.DisplayDialog(
                        "文件已存在",
                        $"输出路径已存在同名文件：\n{outPath}\n\n是否覆盖？",
                        "是",
                        "否"))
                    { Log("已取消转换。", "#FAC775"); return; }
                    File.Delete(outPath);
                }

                Log("生成 FBX…", "#94A3B8");
                var wopts = new FBXWriterOptions
                {
                    WriteSkeleton    = _writeSkel,
                    WriteMorphs      = _writeMorphs,
                    WriteRigidBodies = false,
                    FlipZ            = _flipZ,
                    OutputDir        = absOut,
                    TexturePaths     = texPaths,
                    BoneNameLang     = _boneNameLang
                };
                FBXWriter.Write(pmx, outPath, wopts);

                long szKB = new FileInfo(outPath).Length / 1024;
                Log($"✓ FBX 已写入：{ShortenPath(outPath)} ({szKB} KB)", "#4ADE80");

                if (_addEmptyBlendShape)
                {
                    Log("写入空白 BlendShape…", "#94A3B8");
                    try { AddEmptyBlendShapesToFbx(outPath); }
                    catch (Exception bsex) { Log($"⚠ 写入空白 BlendShape 失败（已跳过）：{bsex.Message}", "#FAC775"); }
                }

                string relPath = "";
                if (!string.IsNullOrEmpty(projectRoot) &&
                    Path.GetFullPath(outPath).StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    Log("刷新 AssetDatabase…", "#94A3B8");
                    AssetDatabase.Refresh();

                    // 提取材质从 FBX 到 Materials 文件夹
                    relPath = Path.GetFullPath(outPath).Substring(projectRoot.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                    var importer = AssetImporter.GetAtPath(relPath) as ModelImporter;
                    if (importer != null)
                    {
                        importer.materialLocation = ModelImporterMaterialLocation.External;

                        importer.animationType = ModelImporterAnimationType.Generic;
                        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                        AssetDatabase.WriteImportSettingsIfDirty(relPath);
                        AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);

                        var importer2 = AssetImporter.GetAtPath(relPath) as ModelImporter;
                        if (importer2 != null)
                        {
                            importer2.animationType = ModelImporterAnimationType.Human;
                            if (_autoHumanoidMap)
                            {
                                if (PMXHumanoidMapper.ApplyMapping(relPath, out string humanReason, out int humanHits, out var missingBones))
                                {
                                    if (missingBones != null && missingBones.Count > 0)
                                    {
                                        Log($"  ⚠ Humanoid 部分匹配: {humanHits}/{PMXHumanoidMapper.RuleCount}，缺失 {missingBones.Count} 项", "#FAC775");
                                        EditorUtility.DisplayDialog(
                                            "Humanoid 骨骼未完全匹配",
                                            $"匹配到 {humanHits}/{PMXHumanoidMapper.RuleCount} 个骨骼。\n" +
                                            $"以下骨骼未在模型中找到：\n\n• {string.Join("\n• ", missingBones)}\n\n" +
                                            $"已尽量按『基础名 → D 系 → EX/IK』的优先级匹配（基本名优先）。如仍缺失，确认模型骨骼命名是否使用 PMX 日文名。",
                                            "知道了");
                                    }
                                    else
                                    {
                                        Log($"✓ Humanoid骨骼自动映射: {humanHits}/{PMXHumanoidMapper.RuleCount}", "#4ADE80");
                                    }
                                }
                                else
                                {
                                    Log($"  ⚠ Humanoid映射跳过: {humanReason}", "#FAC775");
                                    EditorUtility.DisplayDialog(
                                        "Humanoid 骨骼映射失败",
                                        $"映射未应用（0 个匹配）。\n\n原因：\n{humanReason}\n\n" +
                                        $"请确认模型使用的是 PMX 日文骨骼命名（腰 / 左足 / 左ひざ…）。",
                                        "知道了");
                                }

                            }
                            importer2.SaveAndReimport();
                        }

                        string matFolder = Path.Combine(absOut, "Materials");
                        Log($"✓ 材质已提取至：{ShortenPath(matFolder)}", "#4ADE80");
                    }
                    else
                    {
                        Log($"⚠ 无法获取 FBX 导入器", "#FAC775");
                    }

                    // 创建预制体并实例化到场景
                    GameObject prefabInstance = CreatePrefabAndInstantiate(relPath, outPath, absOut);
                    if (prefabInstance != null)
                    {
                        Log("✓ 预制体已创建并放置到场景", "#4ADE80");
                    }

                    Log("✓ 导入完成！", "#4ADE80");
                }
                else
                {
                    Log("输出目录在项目外，请手动导入 FBX。", "#FAC775");
                }
            }
            catch (Exception ex)
            {
                Log($"错误：{ex.Message}", "#F87171");
                Debug.LogException(ex);
            }
            finally
            {
                _converting = false;
                Repaint();
            }
        }

        int CountVMorphs(PMXModel pmx)
        {
            int n = 0;
            foreach (var m in pmx.Morphs) if (m.Type == 1 && m.Offsets != null) n++;
            return n;
        }

        static string ShortenPath(string p)
        {
            if (p.Length <= 55) return p;
            return "…" + p.Substring(p.Length - 52);
        }

        GameObject CreatePrefabAndInstantiate(string relPath, string outPath, string absOut)
        {
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relPath);
            if (fbxPrefab == null)
            {
                Log($"⚠ 无法加载 FBX 资源", "#FAC775");
                return null;
            }

            string prefabFolder = Path.Combine(absOut, "Prefabs");
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                string projectPath = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? "");
                string absOutFull = Path.GetFullPath(absOut);
                string parentFolder = absOutFull.Substring(projectPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                AssetDatabase.CreateFolder(parentFolder, "Prefabs");
            }

            string prefabName = Path.GetFileNameWithoutExtension(outPath) + ".prefab";
            string prefabRelPath = relPath.Replace(Path.GetFileName(relPath), "Prefabs/" + prefabName);

            GameObject sceneInstance = CreateLinkedPrefabFromFBX(fbxPrefab, prefabRelPath, _addVRCComponent);
            if (sceneInstance == null)
            {
                Log($"⚠ 无法创建预制体", "#FAC775");
                return null;
            }

            Log($"✓ 预制体已保存：{ShortenPath(prefabRelPath)}", "#4ADE80");

            sceneInstance.transform.position = Vector3.zero;
            sceneInstance.transform.rotation = Quaternion.identity;
            sceneInstance.transform.localScale = Vector3.one;

            Selection.activeGameObject = sceneInstance;
            SceneView.lastActiveSceneView?.FrameSelected();

            return sceneInstance;
        }

        GameObject CreateLinkedPrefabFromFBX(GameObject fbxPrefab, string prefabRelPath, bool addVRCComponent)
        {
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = PrefabUtility.InstantiatePrefab(fbxPrefab, previewScene) as GameObject;
                if (root == null)
                {
                    root = UnityEngine.Object.Instantiate(fbxPrefab);
                }
                root.name = fbxPrefab.name;

                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                if (addVRCComponent)
                {
                    var vrcType = Type.GetType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor, VRCSDK3A");
                    if (vrcType == null)
                    {
                        Log(T("vrc_add_fail"), "#F87171");
                        EditorUtility.DisplayDialog("VRChat", T("vrc_add_fail"), "OK");
                    }
                    else
                    {
                        if (root.GetComponent(vrcType) == null)
                        {
                            root.AddComponent(vrcType);
                            Log(T("vrc_add_ok"), "#4ADE80");
                        }
                    }
                }

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabRelPath);

                if (savedPrefab == null)
                {
                    Log($"⚠ 无法创建预制体", "#FAC775");
                    return null;
                }

                return PrefabUtility.InstantiatePrefab(savedPrefab) as GameObject;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        void AddEmptyBlendShapesToFbx(string fbxAbsPath)
        {
            if (!File.Exists(fbxAbsPath)) return;

            var manager = FbxManager.Create();
            FbxIOSettings ioSettings = null;
            FbxImporter importer = null;
            FbxScene scene = null;
            FbxExporter exporter = null;

            try
            {
                ioSettings = FbxIOSettings.Create(manager, Globals.IOSROOT);
                manager.SetIOSettings(ioSettings);

                importer = FbxImporter.Create(manager, "importer");
                if (!importer.Initialize(fbxAbsPath, -1, manager.GetIOSettings()))
                {
                    Debug.LogWarning($"[PMX2FBX] FBX Initialize 失败，跳过写入空白 BlendShape: {importer.GetStatus().GetErrorString()}");
                    return;
                }

                int fileFormat = DetectWriterFormat(manager, fbxAbsPath);
                scene = FbxScene.Create(manager, "scene");
                importer.Import(scene);
                importer.Destroy();
                importer = null;

                int meshCount = 0, channelAdded = 0;
                var root = scene.GetRootNode();
                if (root != null)
                    ProcessNodeRecursive(scene, root, ref meshCount, ref channelAdded);

                if (channelAdded > 0)
                {
                    exporter = FbxExporter.Create(manager, "exporter");
                    if (!exporter.Initialize(fbxAbsPath, fileFormat, manager.GetIOSettings()))
                    {
                        Debug.LogWarning($"[PMX2FBX] FBX 导出初始化失败，跳过写入空白 BlendShape: {exporter.GetStatus().GetErrorString()}");
                    }
                    else
                    {
                        exporter.Export(scene);
                    }
                    exporter.Destroy();
                    exporter = null;
                    Log($"✓ 空白 BlendShape \"{_blendShapeName}\" 已写入 ({channelAdded} 个网格)", "#4ADE80");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PMX2FBX] AddEmptyBlendShapesToFbx 失败: {ex.Message}");
            }
            finally
            {
                if (importer != null) importer.Destroy();
                if (scene != null) scene.Destroy();
                if (exporter != null) exporter.Destroy();
                if (ioSettings != null) ioSettings.Destroy();
                manager.Destroy();
            }
        }

        int DetectWriterFormat(FbxManager manager, string path)
        {
            bool isBinary = false;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);
                string header = System.Text.Encoding.ASCII.GetString(br.ReadBytes(20));
                isBinary = header.StartsWith("Kaydara FBX Binary");
            }
            catch { isBinary = true; }

            var registry = manager.GetIOPluginRegistry();
            return isBinary
                ? registry.FindWriterIDByDescription("FBX binary (*.fbx)")
                : registry.FindWriterIDByDescription("FBX ascii (*.fbx)");
        }

        void ProcessNodeRecursive(FbxScene scene, FbxNode node, ref int meshCount, ref int channelAdded)
        {
            var attr = node.GetNodeAttribute();
            if (attr != null && attr.GetAttributeType() == FbxNodeAttribute.EType.eMesh)
            {
                var mesh = node.GetMesh();
                if (mesh != null)
                {
                    meshCount++;
                    bool match = string.IsNullOrEmpty(_blendShapeMeshFilter)
                        || node.GetName() == _blendShapeMeshFilter
                        || mesh.GetName() == _blendShapeMeshFilter;
                    if (match && AddEmptyBlendShapeToMesh(scene, mesh))
                        channelAdded++;
                }
            }
            for (int i = 0; i < node.GetChildCount(); i++)
                ProcessNodeRecursive(scene, node.GetChild(i), ref meshCount, ref channelAdded);
        }

        bool AddEmptyBlendShapeToMesh(FbxScene scene, FbxMesh mesh)
        {
            int count = mesh.GetControlPointsCount();
            if (count <= 0) return false;

            FbxBlendShape blendShape = null;
            int dc = mesh.GetDeformerCount(FbxDeformer.EDeformerType.eBlendShape);
            if (dc > 0) blendShape = mesh.GetBlendShapeDeformer(0);

            if (blendShape == null)
            {
                blendShape = FbxBlendShape.Create(scene, mesh.GetName() + "_BlendShapes");
                mesh.AddDeformer(blendShape);
            }

            for (int i = 0; i < blendShape.GetBlendShapeChannelCount(); i++)
            {
                var ch = blendShape.GetBlendShapeChannel(i);
                if (ch != null && ch.GetName() == _blendShapeName) return false;
            }

            var channel = FbxBlendShapeChannel.Create(scene, _blendShapeName);
            var shape = FbxShape.Create(scene, _blendShapeName + "_Shape");
            shape.InitControlPoints(count);
            for (int i = 0; i < count; i++)
                shape.SetControlPointAt(mesh.GetControlPointAt(i), i);

            channel.AddTargetShape(shape, 100.0);
            blendShape.AddBlendShapeChannel(channel);
            return true;
        }
    }
}

