#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VN.Data;

namespace VN.Editor
{
    public sealed class VNStoryJsonAuthoringWindow : EditorWindow
    {
        private const string StoriesRoot = "Assets/_ElementsResources/VN/Stories";
        private const string DefaultCatalogPath = "Assets/_ElementsResources/VN/VNResourceCatalog.asset";

        private static readonly string[] CommandTypes =
        {
            "dialogue", "background", "showCharacter", "hideCharacter", "changeFace", "moveCharacter",
            "playBgm", "stopBgm", "playSfx", "wait", "choice", "jump", "if", "setVariable", "end"
        };

        private static readonly string[] SlotOptions = { "left", "center", "right" };
        private static readonly string[] EffectOptions = { "", "fadeIn", "fadeOut", "shake", "angry", "punch", "pop", "zoomIn", "slideLeft", "slideRight" };

        [Serializable]
        private sealed class NodeDraft
        {
            public string id = "node_001";
            public bool foldout = true;
            public List<CommandData> commands = new();
        }

        private VNResourceCatalog _catalog;
        [SerializeField] private string _catalogPath = DefaultCatalogPath;

        [SerializeField] private string _storyId = "storydata_0000001";
        [SerializeField] private string _startNode = "node_001";
        [SerializeField] private string _savePath = StoriesRoot + "/storydata_0000001.json";

        [SerializeField] private List<NodeDraft> _nodes = new();
        [SerializeField] private List<string> _collapsedCommandKeys = new();
        private Vector2 _scroll;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _hintLabelStyle;

        [MenuItem("Tools/VN/Story JSON Authoring")]
        public static void OpenWindow()
        {
            var window = GetWindow<VNStoryJsonAuthoringWindow>("VN Story Tool");
            window.minSize = new Vector2(900f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();

            if (_nodes == null || _nodes.Count == 0)
            {
                _nodes = new List<NodeDraft>
                {
                    new() { id = "node_001", commands = new List<CommandData> { new() { type = "dialogue" } } }
                };
            }

            TryLoadCatalogFromPath();
        }

        private void InitializeStyles()
        {
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            _hintLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();
            DrawStoryInfo();
            EditorGUILayout.Space();
            DrawNodes();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("VN Story JSON Authoring Tool", _titleStyle);
            EditorGUILayout.HelpBox(
                "초보자 빠른 시작\n" +
                "1) Story ID / Start Node를 먼저 입력\n" +
                "2) + Add Node → + Add Command 순서로 작성\n" +
                "3) 자동완성 버튼(파란 버튼)을 적극 활용\n" +
                "4) 마지막에 Save JSON으로 저장",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _catalog = (VNResourceCatalog)EditorGUILayout.ObjectField("Resource Catalog", _catalog, typeof(VNResourceCatalog), false);
                if (GUILayout.Button("Reload", GUILayout.Width(90f)))
                {
                    TryLoadCatalogFromPath(force: true);
                }
            }

            _catalogPath = EditorGUILayout.TextField("Catalog Path", _catalogPath);
            EditorGUILayout.LabelField("Catalog Path가 맞으면 Character/BG/BGM 자동완성이 동작합니다.", _hintLabelStyle);
        }

        private void DrawStoryInfo()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Story 기본 설정", _sectionHeaderStyle);
                _storyId = EditorGUILayout.TextField("Story ID", _storyId);
                _startNode = EditorGUILayout.TextField("Start Node", _startNode);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _savePath = EditorGUILayout.TextField("Save JSON Path", _savePath);
                    if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                    {
                        var selectedPath = EditorUtility.SaveFilePanelInProject("Save Story JSON", Path.GetFileNameWithoutExtension(_savePath), "json", "저장할 JSON 파일을 선택하세요.");
                        if (!string.IsNullOrWhiteSpace(selectedPath))
                        {
                            _savePath = selectedPath;
                        }
                    }
                }
                EditorGUILayout.LabelField("권장: Assets/_ElementsResources/VN/Stories 폴더에 저장", _hintLabelStyle);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Load JSON", GUILayout.Height(28f)))
                    {
                        LoadStoryJson();
                    }

                    if (GUILayout.Button("Save JSON", GUILayout.Height(28f)))
                    {
                        SaveStoryJson();
                    }
                }
            }
        }

        private void DrawNodes()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                var totalCommands = _nodes.Sum(n => n.commands?.Count ?? 0);
                EditorGUILayout.LabelField("Nodes / Commands 편집", _sectionHeaderStyle);
                EditorGUILayout.LabelField($"현재 노드 {_nodes.Count}개 / 커맨드 {totalCommands}개", _hintLabelStyle);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Add Node", GUILayout.Width(140f)))
                    {
                        _nodes.Add(new NodeDraft
                        {
                            id = $"node_{_nodes.Count + 1:000}",
                            commands = new List<CommandData> { new() { type = "dialogue" } }
                        });
                    }

                    if (GUILayout.Button("Sort by Node ID", GUILayout.Width(140f)))
                    {
                        _nodes = _nodes.OrderBy(n => n.id, StringComparer.OrdinalIgnoreCase).ToList();
                    }
                }

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (var i = 0; i < _nodes.Count; i++)
                {
                    DrawSingleNode(i);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSingleNode(int nodeIndex)
        {
            var node = _nodes[nodeIndex];
            var commandCount = node.commands?.Count ?? 0;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    node.foldout = EditorGUILayout.Foldout(node.foldout, $"Node {nodeIndex + 1}: {node.id}  ({commandCount} commands)", true);
                    if (GUILayout.Button("Duplicate", GUILayout.Width(90f)))
                    {
                        _nodes.Insert(nodeIndex + 1, CloneNode(node));
                        return;
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(80f)))
                    {
                        _nodes.RemoveAt(nodeIndex);
                        return;
                    }
                }

                if (!node.foldout)
                {
                    return;
                }

                EditorGUILayout.LabelField("노드 ID 예시: node_010, prologue_intro", _hintLabelStyle);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Add Command", GUILayout.Width(140f)))
                    {
                        node.commands.Add(new CommandData { type = "dialogue" });
                    }
                }

                for (var commandIndex = 0; commandIndex < node.commands.Count; commandIndex++)
                {
                    DrawCommand(node, nodeIndex, commandIndex);
                }
            }
        }

        private void DrawCommand(NodeDraft node, int nodeIndex, int commandIndex)
        {
            var command = node.commands[commandIndex];
            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = GetCommandColor(command.type);
            var expanded = IsCommandExpanded(node, nodeIndex, commandIndex);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var nextExpanded = EditorGUILayout.Foldout(expanded, $"Command {commandIndex + 1} · {command.type}", true);
                    if (nextExpanded != expanded)
                    {
                        SetCommandExpanded(node, nodeIndex, commandIndex, nextExpanded);
                        expanded = nextExpanded;
                    }
                    if (GUILayout.Button("▲", GUILayout.Width(28f)) && commandIndex > 0)
                    {
                        (node.commands[commandIndex - 1], node.commands[commandIndex]) = (node.commands[commandIndex], node.commands[commandIndex - 1]);
                        GUI.backgroundColor = prevBgColor;
                        return;
                    }
                    if (GUILayout.Button("▼", GUILayout.Width(28f)) && commandIndex < node.commands.Count - 1)
                    {
                        (node.commands[commandIndex + 1], node.commands[commandIndex]) = (node.commands[commandIndex], node.commands[commandIndex + 1]);
                        GUI.backgroundColor = prevBgColor;
                        return;
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(70f)))
                    {
                        node.commands.RemoveAt(commandIndex);
                        GUI.backgroundColor = prevBgColor;
                        return;
                    }
                }

                if (!expanded)
                {
                    GUI.backgroundColor = prevBgColor;
                    return;
                }

                var typeIndex = Mathf.Max(0, Array.IndexOf(CommandTypes, command.type ?? string.Empty));
                var newTypeIndex = EditorGUILayout.Popup("Type", typeIndex, CommandTypes);
                var newType = CommandTypes[newTypeIndex];
                if (!string.Equals(command.type, newType, StringComparison.Ordinal))
                {
                    command.type = newType;
                    ApplyCommandDefaults(command);
                }

                DrawCommandGuide(command.type);

                DrawCommandFields(nodeIndex, commandIndex, command);
            }
            
            GUI.backgroundColor = prevBgColor;
        }

        private bool IsCommandExpanded(NodeDraft node, int nodeIndex, int commandIndex)
        {
            var key = GetCommandFoldoutKey(node, nodeIndex, commandIndex);
            return !_collapsedCommandKeys.Contains(key);
        }

        private void SetCommandExpanded(NodeDraft node, int nodeIndex, int commandIndex, bool expanded)
        {
            var key = GetCommandFoldoutKey(node, nodeIndex, commandIndex);
            if (expanded)
            {
                _collapsedCommandKeys.RemoveAll(k => string.Equals(k, key, StringComparison.Ordinal));
                return;
            }

            if (!_collapsedCommandKeys.Contains(key))
            {
                _collapsedCommandKeys.Add(key);
            }
        }

        private static string GetCommandFoldoutKey(NodeDraft node, int nodeIndex, int commandIndex)
        {
            var nodeId = string.IsNullOrWhiteSpace(node?.id) ? "node" : node.id.Trim();
            return $"{nodeIndex}:{nodeId}:{commandIndex}";
        }

        private static Color GetCommandColor(string commandType)
        {
            return commandType switch
            {
                "dialogue" => new Color(0.88f, 0.95f, 1f),
                "choice" => new Color(1f, 0.95f, 0.86f),
                "if" => new Color(0.94f, 0.89f, 1f),
                "setVariable" => new Color(0.9f, 1f, 0.9f),
                "jump" => new Color(1f, 0.92f, 0.92f),
                _ => new Color(0.95f, 0.95f, 0.95f)
            };
        }

        private static void DrawCommandGuide(string commandType)
        {
            var message = commandType switch
            {
                "dialogue" => "대사 1줄을 출력합니다. Speaker / Text를 우선 입력하세요.",
                "background" => "배경 이미지를 전환합니다. Background Key를 자동완성으로 선택하세요.",
                "showCharacter" => "캐릭터를 화면에 표시합니다. Character ID, Slot을 설정하세요.",
                "hideCharacter" => "캐릭터를 화면에서 숨깁니다.",
                "changeFace" => "같은 캐릭터의 표정만 변경합니다.",
                "moveCharacter" => "캐릭터 슬롯(left/center/right) 이동에 사용합니다.",
                "choice" => "선택지를 추가합니다. 각 옵션은 Jump 노드 지정이 중요합니다.",
                "jump" => "지정한 노드로 즉시 이동합니다.",
                "if" => "조건 분기를 실행합니다. Then / Else 노드를 모두 지정하세요.",
                "setVariable" => "변수 값을 설정/증감합니다. Name, Op, Value를 확인하세요.",
                "end" => "스토리를 종료합니다.",
                _ => "필수 파라미터를 확인하여 작성하세요."
            };

            EditorGUILayout.HelpBox(message, MessageType.None);
        }

        private static void ApplyCommandDefaults(CommandData command)
        {
            if (command == null)
            {
                return;
            }

            switch (command.type)
            {
                case "dialogue":
                    command.duration = 0.2f;
                    break;
                case "background":
                    command.duration = 0.2f;
                    break;
                case "showCharacter":
                case "hideCharacter":
                case "changeFace":
                case "moveCharacter":
                    command.duration = 0.2f;
                    break;
                case "wait":
                    if (command.waitDuration <= 0f)
                    {
                        command.waitDuration = 1f;
                    }
                    break;
                case "setVariable":
                    command.op = string.IsNullOrWhiteSpace(command.op) ? "set" : command.op;
                    break;
            }
        }

        private void DrawCommandFields(int nodeIndex, int commandIndex, CommandData command)
        {
            switch (command.type)
            {
                case "dialogue":
                    command.speaker = EditorGUILayout.TextField("Speaker", command.speaker);
                    command.characterId = DrawCharacterIdField("Character ID", command.characterId);
                    command.face = DrawFaceKeyField("Face", command.characterId, command.face);
                    command.voice = DrawResourceKeyField("Voice Key", command.voice, VNResourceCategory.Character, "voice/");
                    command.text = EditorGUILayout.TextArea(command.text ?? string.Empty, GUILayout.MinHeight(50f));
                    command.transition = DrawSimpleOption("Effect", command.transition, EffectOptions);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "background":
                    command.bg = DrawResourceKeyField("Background Key", command.bg, VNResourceCategory.Background, string.Empty);
                    command.transition = DrawSimpleOption("Transition", command.transition, EffectOptions);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "showCharacter":
                    command.characterId = DrawMultiCharacterIdField("Character ID(s)", command.characterId);
                    command.face = DrawFaceKeyField("Face(s)", FirstToken(command.characterId), command.face, multiValue: true);
                    command.slot = DrawSimpleOption("Slot", command.slot, SlotOptions, allowCustom: true);
                    command.transition = DrawSimpleOption("Transition", command.transition, EffectOptions);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "hideCharacter":
                    command.characterId = DrawMultiCharacterIdField("Character ID(s)", command.characterId);
                    command.transition = DrawSimpleOption("Transition", command.transition, EffectOptions);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "changeFace":
                    command.characterId = DrawCharacterIdField("Character ID", command.characterId);
                    command.face = DrawFaceKeyField("Face", command.characterId, command.face);
                    command.transition = DrawSimpleOption("Transition", command.transition, EffectOptions);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "moveCharacter":
                    command.characterId = DrawCharacterIdField("Character ID", command.characterId);
                    command.toSlot = DrawSimpleOption("To Slot", command.toSlot, SlotOptions, allowCustom: true);
                    command.duration = EditorGUILayout.FloatField("Duration", command.duration);
                    break;

                case "playBgm":
                    command.bgm = DrawResourceKeyField("BGM Key", command.bgm, VNResourceCategory.Bgm, string.Empty);
                    break;

                case "stopBgm":
                    EditorGUILayout.HelpBox("No parameters", MessageType.None);
                    break;

                case "playSfx":
                    command.sfx = DrawResourceKeyField("SFX Key", command.sfx, VNResourceCategory.Sfx, string.Empty);
                    break;

                case "wait":
                    command.waitDuration = EditorGUILayout.FloatField("Wait Duration", command.waitDuration);
                    break;

                case "choice":
                    DrawChoiceEditor(command);
                    break;

                case "jump":
                    command.targetNodeId = DrawNodeIdField("Target Node", command.targetNodeId);
                    break;

                case "if":
                    command.condition = EditorGUILayout.TextField("Condition", command.condition);
                    command.@then = DrawNodeIdField("Then", command.@then);
                    command.@else = DrawNodeIdField("Else", command.@else);
                    break;

                case "setVariable":
                    command.name = EditorGUILayout.TextField("Name", command.name);
                    command.op = EditorGUILayout.TextField("Op", command.op);
                    command.value = EditorGUILayout.IntField("Value", command.value);
                    break;

                case "end":
                    EditorGUILayout.HelpBox("No parameters", MessageType.None);
                    break;
            }
        }

        private void DrawChoiceEditor(CommandData command)
        {
            var options = command.options?.ToList() ?? new List<ChoiceOptionData>();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Option", GUILayout.Width(120f)))
                {
                    options.Add(new ChoiceOptionData
                    {
                        text = "Choice text",
                        jump = _nodes.Count > 0 ? _nodes[0].id : string.Empty,
                        set = new VariableMutationData { name = "flag", op = "set", value = 1 }
                    });
                }
            }

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i] ?? new ChoiceOptionData();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel);
                        if (GUILayout.Button("Delete", GUILayout.Width(70f)))
                        {
                            options.RemoveAt(i);
                            break;
                        }
                    }

                    option.text = EditorGUILayout.TextField("Text", option.text);
                    option.jump = DrawNodeIdField("Jump", option.jump);

                    if (option.set == null)
                    {
                        option.set = new VariableMutationData();
                    }

                    option.set.name = EditorGUILayout.TextField("Set.Name", option.set.name);
                    option.set.op = EditorGUILayout.TextField("Set.Op", option.set.op);
                    option.set.value = EditorGUILayout.IntField("Set.Value", option.set.value);
                    options[i] = option;
                }
            }

            command.options = options.ToArray();
        }

        private string DrawResourceKeyField(string label, string currentValue, VNResourceCategory category, string requiredContains)
        {
            var value = EditorGUILayout.TextField(label, currentValue ?? string.Empty);
            var matches = GetResourceMatches(value, category, requiredContains);
            DrawSuggestionButtons(matches, v => value = v);
            return value;
        }

        private string DrawCharacterIdField(string label, string currentValue)
        {
            var value = EditorGUILayout.TextField(label, currentValue ?? string.Empty);
            var ids = GetCharacterIds();
            DrawSuggestionButtons(FilterByKeyword(ids, value), v => value = v);
            return value;
        }

        private string DrawMultiCharacterIdField(string label, string currentValue)
        {
            var value = EditorGUILayout.TextField(label, currentValue ?? string.Empty);
            var lastToken = LastToken(value);
            var ids = GetCharacterIds();
            var matches = FilterByKeyword(ids, lastToken);
            DrawSuggestionButtons(matches, picked => value = ReplaceLastToken(value, picked));
            return value;
        }

        private string DrawFaceKeyField(string label, string characterId, string currentValue, bool multiValue = false)
        {
            var value = EditorGUILayout.TextField(label, currentValue ?? string.Empty);
            var lookupKey = multiValue ? LastToken(value) : value;
            var candidates = GetFaceKeys(characterId);
            var matches = FilterByKeyword(candidates, lookupKey);
            if (multiValue)
            {
                DrawSuggestionButtons(matches, picked => value = ReplaceLastToken(value, picked));
            }
            else
            {
                DrawSuggestionButtons(matches, picked => value = picked);
            }
            return value;
        }

        private string DrawNodeIdField(string label, string currentValue)
        {
            var value = EditorGUILayout.TextField(label, currentValue ?? string.Empty);
            var nodeIds = _nodes.Select(n => n.id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var matches = FilterByKeyword(nodeIds, value);
            DrawSuggestionButtons(matches, picked => value = picked);
            return value;
        }

        private static void DrawSuggestionButtons(IReadOnlyList<string> matches, Action<string> onSelect)
        {
            if (matches == null || matches.Count == 0)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("자동완성", GUILayout.Width(60f));
                var displayCount = Mathf.Min(6, matches.Count);
                for (var i = 0; i < displayCount; i++)
                {
                    if (GUILayout.Button(matches[i], EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        onSelect?.Invoke(matches[i]);
                    }
                }
            }
        }

        private static string DrawSimpleOption(string label, string value, IReadOnlyList<string> options, bool allowCustom = true)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var current = value ?? string.Empty;
                current = EditorGUILayout.TextField(label, current);
                if (options != null && options.Count > 0)
                {
                    var optionArray = options.ToArray();
                    var selected = EditorGUILayout.Popup(Mathf.Max(0, Array.IndexOf(optionArray, current)), optionArray, GUILayout.Width(140f));
                    current = options[selected];
                }

                return allowCustom ? current : (options != null && options.Contains(current) ? current : options?[0] ?? string.Empty);
            }
        }

        private List<string> GetResourceMatches(string keyword, VNResourceCategory category, string requiredContains)
        {
            if (_catalog == null)
            {
                return new List<string>();
            }

            var normalizedKeyword = (keyword ?? string.Empty).Trim();
            var result = new List<string>();

            foreach (var entry in _catalog.Entries)
            {
                if (entry == null || entry.category != category)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(requiredContains) &&
                    (entry.relativeKey == null || entry.relativeKey.IndexOf(requiredContains, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(normalizedKeyword) ||
                    entry.relativeKey.IndexOf(normalizedKeyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.fileName.IndexOf(normalizedKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(entry.relativeKey);
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).Take(12).ToList();
        }

        private List<string> GetCharacterIds()
        {
            if (_catalog == null)
            {
                return new List<string>();
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _catalog.Entries)
            {
                if (entry == null || entry.category != VNResourceCategory.Character || string.IsNullOrWhiteSpace(entry.relativeKey))
                {
                    continue;
                }

                var chunks = entry.relativeKey.Split('/');
                if (chunks.Length > 0 && !string.IsNullOrWhiteSpace(chunks[0]))
                {
                    ids.Add(chunks[0]);
                }
            }

            return ids.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private List<string> GetFaceKeys(string characterId)
        {
            if (_catalog == null || string.IsNullOrWhiteSpace(characterId))
            {
                return new List<string>();
            }

            var prefix = characterId.Trim() + "/";
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _catalog.Entries)
            {
                if (entry == null || entry.category != VNResourceCategory.Character || string.IsNullOrWhiteSpace(entry.relativeKey))
                {
                    continue;
                }

                if (!entry.relativeKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var file = Path.GetFileNameWithoutExtension(entry.relativeKey);
                var marker = characterId + "_";
                if (file.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(file.Substring(marker.Length));
                }
            }

            return results.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> FilterByKeyword(IReadOnlyList<string> source, string keyword)
        {
            if (source == null)
            {
                return new List<string>();
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return source.Take(12).ToList();
            }

            return source.Where(s => s != null && s.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(12)
                .ToList();
        }

        private void SaveStoryJson()
        {
            if (string.IsNullOrWhiteSpace(_storyId))
            {
                EditorUtility.DisplayDialog("Invalid", "Story ID is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_startNode))
            {
                EditorUtility.DisplayDialog("Invalid", "Start Node is required.", "OK");
                return;
            }

            var nodes = _nodes.Select(n => new NodeData
            {
                id = n.id,
                commands = n.commands?.ToArray() ?? Array.Empty<CommandData>()
            }).ToArray();

            var story = new StoryData
            {
                storyId = _storyId,
                startNode = _startNode,
                nodes = nodes
            };

            var json = BuildCompactStoryJson(story);
            EnsureDirectory(_savePath);
            File.WriteAllText(_savePath, json);

            AssetDatabase.ImportAsset(_savePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log($"[VNStoryJsonAuthoringWindow] Saved story json: {_savePath}");
            EditorUtility.DisplayDialog("Saved", $"Saved JSON\n{_savePath}", "OK");
        }

        private static string BuildCompactStoryJson(StoryData story)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("{");

            var rootFields = new List<string>
            {
                JsonProperty("storyId", story.storyId),
                JsonProperty("startNode", story.startNode),
                $"\"nodes\": {BuildNodesJson(story.nodes)}"
            };

            AppendFields(sb, rootFields, 1);
            sb.AppendLine();
            sb.Append('}');
            return sb.ToString();
        }

        private static string BuildNodesJson(IReadOnlyList<NodeData> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return "[]";
            }

            var sb = new StringBuilder(2048);
            sb.AppendLine("[");

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                sb.Append("    {");

                var fields = new List<string>
                {
                    JsonProperty("id", node?.id),
                    $"\"commands\": {BuildCommandsJson(node?.commands)}"
                };

                if (fields.Count > 0)
                {
                    sb.AppendLine();
                    AppendFields(sb, fields, 2);
                    sb.AppendLine();
                    sb.Append("    }");
                }
                else
                {
                    sb.Append('}');
                }

                if (i < nodes.Count - 1)
                {
                    sb.Append(',');
                }

                sb.AppendLine();
            }

            sb.Append("  ]");
            return sb.ToString();
        }

        private static string BuildCommandsJson(IReadOnlyList<CommandData> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                return "[]";
            }

            var sb = new StringBuilder(2048);
            sb.AppendLine("[");
            var wroteAny = false;

            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                if (command == null || string.IsNullOrWhiteSpace(command.type))
                {
                    continue;
                }

                var fields = BuildCommandFields(command);
                if (fields.Count == 0)
                {
                    continue;
                }

                if (wroteAny)
                {
                    sb.AppendLine(",");
                }

                sb.AppendLine("      {");
                AppendFields(sb, fields, 4);
                sb.AppendLine();
                sb.Append("      }");
                wroteAny = true;
            }

            if (wroteAny)
            {
                sb.AppendLine();
            }

            sb.Append("    ]");
            return sb.ToString();
        }

        private static List<string> BuildCommandFields(CommandData command)
        {
            var fields = new List<string> { JsonProperty("type", command.type) };

            switch (command.type)
            {
                case "dialogue":
                    AddIfNotEmpty(fields, "speaker", command.speaker);
                    AddIfNotEmpty(fields, "characterId", command.characterId);
                    AddIfNotEmpty(fields, "text", command.text);
                    AddIfNotEmpty(fields, "voice", command.voice);
                    AddIfNotEmpty(fields, "face", command.face);
                    AddIfNotEmpty(fields, "transition", command.transition);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "background":
                    AddIfNotEmpty(fields, "bg", command.bg);
                    AddIfNotEmpty(fields, "transition", command.transition);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "showCharacter":
                    AddIfNotEmpty(fields, "characterId", command.characterId);
                    AddIfNotEmpty(fields, "slot", command.slot);
                    AddIfNotEmpty(fields, "face", command.face);
                    AddIfNotEmpty(fields, "transition", command.transition);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "hideCharacter":
                    AddIfNotEmpty(fields, "characterId", command.characterId);
                    AddIfNotEmpty(fields, "transition", command.transition);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "changeFace":
                    AddIfNotEmpty(fields, "characterId", command.characterId);
                    AddIfNotEmpty(fields, "face", command.face);
                    AddIfNotEmpty(fields, "transition", command.transition);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "moveCharacter":
                    AddIfNotEmpty(fields, "characterId", command.characterId);
                    AddIfNotEmpty(fields, "toSlot", command.toSlot);
                    AddIfFloat(fields, "duration", command.duration, 0.2f);
                    break;
                case "playBgm":
                    AddIfNotEmpty(fields, "bgm", command.bgm);
                    break;
                case "playSfx":
                    AddIfNotEmpty(fields, "sfx", command.sfx);
                    break;
                case "wait":
                    if (command.waitDuration > 0f)
                    {
                        fields.Add(JsonNumberProperty("waitDuration", command.waitDuration));
                    }
                    break;
                case "choice":
                    AddIfChoiceOptions(fields, command.options);
                    break;
                case "jump":
                    AddIfNotEmpty(fields, "targetNodeId", command.targetNodeId);
                    break;
                case "if":
                    AddIfNotEmpty(fields, "condition", command.condition);
                    AddIfNotEmpty(fields, "then", command.@then);
                    AddIfNotEmpty(fields, "else", command.@else);
                    break;
                case "setVariable":
                    AddIfNotEmpty(fields, "name", command.name);
                    AddIfNotEmpty(fields, "op", command.op);
                    fields.Add(JsonNumberProperty("value", command.value));
                    break;
            }

            return fields;
        }

        private static void AddIfChoiceOptions(List<string> fields, ChoiceOptionData[] options)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            var serialized = new List<string>();
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    continue;
                }

                var optionFields = new List<string>();
                AddIfNotEmpty(optionFields, "text", option.text);
                AddIfNotEmpty(optionFields, "jump", option.jump);

                if (option.set != null && !string.IsNullOrWhiteSpace(option.set.name) && !string.IsNullOrWhiteSpace(option.set.op))
                {
                    optionFields.Add("\"set\": {" +
                                     $"{JsonProperty("name", option.set.name)}, " +
                                     $"{JsonProperty("op", option.set.op)}, " +
                                     $"{JsonNumberProperty("value", option.set.value)}" +
                                     "}");
                }

                if (optionFields.Count > 0)
                {
                    serialized.Add("{ " + string.Join(", ", optionFields) + " }");
                }
            }

            if (serialized.Count > 0)
            {
                fields.Add("\"options\": [" + string.Join(", ", serialized) + "]");
            }
        }

        private static void AddIfNotEmpty(List<string> fields, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                fields.Add(JsonProperty(key, value));
            }
        }

        private static void AddIfFloat(List<string> fields, string key, float value, float defaultValue)
        {
            if (Mathf.Abs(value - defaultValue) > 0.0001f)
            {
                fields.Add(JsonNumberProperty(key, value));
            }
        }

        private static void AppendFields(StringBuilder sb, IReadOnlyList<string> fields, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);
            for (var i = 0; i < fields.Count; i++)
            {
                sb.Append(indent);
                sb.Append(fields[i]);
                if (i < fields.Count - 1)
                {
                    sb.Append(',');
                }
                sb.AppendLine();
            }
        }

        private static string JsonProperty(string key, string value)
        {
            return $"\"{EscapeJson(key)}\": \"{EscapeJson(value ?? string.Empty)}\"";
        }

        private static string JsonNumberProperty(string key, float value)
        {
            return $"\"{EscapeJson(key)}\": {value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        private static string JsonNumberProperty(string key, int value)
        {
            return $"\"{EscapeJson(key)}\": {value}";
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private void LoadStoryJson()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(_savePath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Not Found", $"JSON not found: {_savePath}", "OK");
                return;
            }

            var data = JsonUtility.FromJson<StoryData>(asset.text);
            if (data == null)
            {
                EditorUtility.DisplayDialog("Parse Error", "Failed to parse story json.", "OK");
                return;
            }

            _storyId = data.storyId;
            _startNode = data.startNode;
            _nodes = new List<NodeDraft>();
            if (data.nodes != null)
            {
                foreach (var node in data.nodes)
                {
                    _nodes.Add(new NodeDraft
                    {
                        id = node.id,
                        foldout = true,
                        commands = node.commands?.ToList() ?? new List<CommandData>()
                    });
                }
            }

            if (_nodes.Count == 0)
            {
                _nodes.Add(new NodeDraft { id = "node_001", commands = new List<CommandData> { new() { type = "dialogue" } } });
            }

            Debug.Log($"[VNStoryJsonAuthoringWindow] Loaded story json: {_savePath}");
        }

        private void TryLoadCatalogFromPath(bool force = false)
        {
            if (!force && _catalog != null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_catalogPath))
            {
                _catalog = AssetDatabase.LoadAssetAtPath<VNResourceCatalog>(_catalogPath);
            }
        }

        private static NodeDraft CloneNode(NodeDraft source)
        {
            return new NodeDraft
            {
                id = source.id + "_copy",
                foldout = true,
                commands = source.commands?.Select(CloneCommand).ToList() ?? new List<CommandData>()
            };
        }

        private static CommandData CloneCommand(CommandData source)
        {
            var json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<CommandData>(json);
        }

        private static string FirstToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chunks = value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            return chunks.Length > 0 ? chunks[0].Trim() : string.Empty;
        }

        private static string LastToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chunks = value.Split(',');
            return chunks.Length == 0 ? string.Empty : chunks[chunks.Length - 1].Trim();
        }

        private static string ReplaceLastToken(string origin, string replacement)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return replacement;
            }

            var chunks = origin.Split(',').Select(c => c.Trim()).ToList();
            if (chunks.Count == 0)
            {
                return replacement;
            }

            chunks[chunks.Count - 1] = replacement;
            return string.Join(", ", chunks);
        }

        private static void EnsureDirectory(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
#endif
