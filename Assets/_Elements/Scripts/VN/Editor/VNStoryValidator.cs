#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using VN.Data;

namespace VN.Editor
{
    public static class VNStoryValidator
    {
        private const string ResourceRoot = "Assets/_ElementsResources/VN";
        private const string StoriesRoot = ResourceRoot + "/Stories";

        private static readonly HashSet<string> CommandTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "dialogue",
            "background",
            "showCharacter",
            "hideCharacter",
            "changeFace",
            "moveCharacter",
            "playBgm",
            "stopBgm",
            "playSfx",
            "wait",
            "choice",
            "jump",
            "if",
            "setVariable",
            "end"
        };

        private static readonly HashSet<string> VariableOps = new(StringComparer.OrdinalIgnoreCase)
        {
            "set",
            "add",
            "sub",
            "subtract"
        };

        private static readonly Regex ConditionRegex = new(@"^\s*(\w+)\s*(==|!=|>=|<=|>|<)\s*(-?\d+)\s*$");

        [MenuItem("Tools/VN/Validate Stories")]
        public static void ValidateStoriesFromMenu()
        {
            var report = ValidateAllStories();
            if (report.Errors.Count > 0)
            {
                Debug.LogError(report.ToLogString());
                EditorUtility.DisplayDialog("VN Story Validation", $"Failed with {report.Errors.Count} errors. See Console.", "OK");
                return;
            }

            Debug.Log(report.ToLogString());
            EditorUtility.DisplayDialog("VN Story Validation", $"Passed. Stories: {report.ValidatedStoryCount}", "OK");
        }

        public static void ValidateAllStoriesForCi()
        {
            var report = ValidateAllStories();
            if (report.Errors.Count > 0)
            {
                throw new BuildFailedException(report.ToLogString());
            }

            Debug.Log(report.ToLogString());
        }

        public static StoryValidationReport ValidateAllStories()
        {
            var report = new StoryValidationReport();
            var resources = ResourceIndex.Build();
            var storyPaths = FindStoryJsonFiles();

            if (storyPaths.Count == 0)
            {
                report.Errors.Add($"No story json files found under {StoriesRoot}.");
                return report;
            }

            foreach (var storyPath in storyPaths)
            {
                ValidateStoryFile(storyPath, resources, report);
            }

            return report;
        }

        private static List<string> FindStoryJsonFiles()
        {
            if (!Directory.Exists(StoriesRoot))
            {
                return new List<string>();
            }

            return Directory.GetFiles(StoriesRoot, "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ValidateStoryFile(string storyPath, ResourceIndex resources, StoryValidationReport report)
        {
            StoryData story = null;
            try
            {
                var json = File.ReadAllText(storyPath, Encoding.UTF8);
                story = JsonUtility.FromJson<StoryData>(json);
            }
            catch (Exception ex)
            {
                report.Errors.Add($"{storyPath}: JSON parse failed. {ex.Message}");
                return;
            }

            if (story == null)
            {
                report.Errors.Add($"{storyPath}: JSON parse returned null.");
                return;
            }

            report.ValidatedStoryCount++;

            if (string.IsNullOrWhiteSpace(story.storyId))
            {
                report.Errors.Add($"{storyPath}: storyId is empty.");
            }

            if (story.nodes == null || story.nodes.Length == 0)
            {
                report.Errors.Add($"{storyPath}: nodes is empty.");
                return;
            }

            var nodeMap = BuildNodeMap(storyPath, story, report);
            if (string.IsNullOrWhiteSpace(story.startNode) || !nodeMap.ContainsKey(story.startNode.Trim()))
            {
                report.Errors.Add($"{storyPath}: startNode '{story.startNode}' was not found.");
            }

            for (var nodeIndex = 0; nodeIndex < story.nodes.Length; nodeIndex++)
            {
                var node = story.nodes[nodeIndex];
                if (node == null || node.commands == null)
                {
                    continue;
                }

                for (var commandIndex = 0; commandIndex < node.commands.Length; commandIndex++)
                {
                    ValidateCommand(storyPath, node, commandIndex, node.commands[commandIndex], nodeMap, resources, report);
                }
            }
        }

        private static Dictionary<string, NodeData> BuildNodeMap(string storyPath, StoryData story, StoryValidationReport report)
        {
            var nodeMap = new Dictionary<string, NodeData>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < story.nodes.Length; i++)
            {
                var node = story.nodes[i];
                if (node == null)
                {
                    report.Errors.Add($"{storyPath}: node[{i}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.id))
                {
                    report.Errors.Add($"{storyPath}: node[{i}] id is empty.");
                    continue;
                }

                var id = node.id.Trim();
                if (nodeMap.ContainsKey(id))
                {
                    report.Errors.Add($"{storyPath}: duplicate node id '{id}'.");
                    continue;
                }

                nodeMap[id] = node;
            }

            return nodeMap;
        }

        private static void ValidateCommand(
            string storyPath,
            NodeData node,
            int commandIndex,
            CommandData command,
            IReadOnlyDictionary<string, NodeData> nodeMap,
            ResourceIndex resources,
            StoryValidationReport report)
        {
            var location = $"{storyPath} node='{node?.id}' command[{commandIndex}]";
            if (command == null)
            {
                report.Errors.Add($"{location}: command is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(command.type))
            {
                report.Errors.Add($"{location}: command type is empty.");
                return;
            }

            if (!CommandTypes.Contains(command.type))
            {
                report.Errors.Add($"{location}: unknown command type '{command.type}'.");
                return;
            }

            switch (command.type)
            {
                case "background":
                    RequireResource(location, "bg", command.bg, resources.Backgrounds, report);
                    break;
                case "showCharacter":
                    ValidateCharacterSprites(location, command, resources, requireFace: true, report);
                    break;
                case "changeFace":
                    ValidateCharacterSprites(location, command, resources, requireFace: true, report);
                    break;
                case "dialogue":
                    ValidateOptionalVoice(location, command, resources, report);
                    break;
                case "playBgm":
                    RequireResource(location, "bgm", command.bgm, resources.Bgm, report);
                    break;
                case "playSfx":
                    RequireResource(location, "sfx", command.sfx, resources.Sfx, report);
                    break;
                case "choice":
                    ValidateChoice(location, command, nodeMap, report);
                    break;
                case "jump":
                    RequireNodeTarget(location, "targetNodeId", command.targetNodeId, nodeMap, report);
                    break;
                case "if":
                    ValidateIf(location, command, nodeMap, report);
                    break;
                case "setVariable":
                    ValidateVariableMutation(location, command.name, command.op, report);
                    break;
            }
        }

        private static void RequireResource(string location, string fieldName, string value, ISet<string> resources, StoryValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Errors.Add($"{location}: required field '{fieldName}' is empty.");
                return;
            }

            var key = value.Trim();
            if (!resources.Contains(key))
            {
                report.Errors.Add($"{location}: missing {fieldName} resource '{key}'.");
            }
        }

        private static void ValidateCharacterSprites(
            string location,
            CommandData command,
            ResourceIndex resources,
            bool requireFace,
            StoryValidationReport report)
        {
            var characterIds = command.GetCharacterIds();
            if (characterIds.Length == 0)
            {
                report.Errors.Add($"{location}: characterId is empty.");
                return;
            }

            var faces = command.GetFaces();
            if (requireFace && faces.Length == 0)
            {
                report.Errors.Add($"{location}: face is empty.");
                return;
            }

            for (var i = 0; i < characterIds.Length; i++)
            {
                var characterId = NormalizeCharacterId(characterIds[i]);
                var face = ResolveValueByIndex(faces, i, command.face);
                if (string.IsNullOrWhiteSpace(face))
                {
                    continue;
                }

                var spriteKey = $"{characterId}/{characterId}_{NormalizeFaceKey(face)}";
                if (!resources.CharacterSprites.Contains(spriteKey))
                {
                    report.Errors.Add($"{location}: missing character sprite '{spriteKey}'.");
                }
            }
        }

        private static void ValidateOptionalVoice(string location, CommandData command, ResourceIndex resources, StoryValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(command.voice))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(command.characterId))
            {
                report.Errors.Add($"{location}: voice '{command.voice}' requires characterId.");
                return;
            }

            var characterId = NormalizeCharacterId(command.characterId);
            var voiceKey = $"{characterId}/voice/{command.voice.Trim()}";
            if (!resources.Voices.Contains(voiceKey))
            {
                report.Errors.Add($"{location}: missing voice resource '{voiceKey}'.");
            }
        }

        private static void ValidateChoice(
            string location,
            CommandData command,
            IReadOnlyDictionary<string, NodeData> nodeMap,
            StoryValidationReport report)
        {
            if (command.options == null || command.options.Length == 0)
            {
                report.Errors.Add($"{location}: choice options are empty.");
                return;
            }

            for (var i = 0; i < command.options.Length; i++)
            {
                var option = command.options[i];
                if (option == null)
                {
                    report.Errors.Add($"{location}: option[{i}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.text))
                {
                    report.Errors.Add($"{location}: option[{i}] text is empty.");
                }

                if (!string.IsNullOrWhiteSpace(option.jump))
                {
                    RequireNodeTarget(location, $"option[{i}].jump", option.jump, nodeMap, report);
                }

                if (!string.IsNullOrWhiteSpace(option.condition) && !ConditionRegex.IsMatch(option.condition))
                {
                    report.Errors.Add($"{location}: option[{i}] condition is invalid: '{option.condition}'.");
                }

                if (HasVariableMutation(option.set))
                {
                    ValidateVariableMutation(location, option.set.name, option.set.op, report);
                }
            }
        }

        private static void ValidateIf(
            string location,
            CommandData command,
            IReadOnlyDictionary<string, NodeData> nodeMap,
            StoryValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(command.condition) || !ConditionRegex.IsMatch(command.condition))
            {
                report.Errors.Add($"{location}: condition is invalid: '{command.condition}'.");
            }

            RequireNodeTarget(location, "then", command.@then, nodeMap, report);
            RequireNodeTarget(location, "else", command.@else, nodeMap, report);
        }

        private static void ValidateVariableMutation(string location, string name, string op, StoryValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                report.Errors.Add($"{location}: variable name is empty.");
            }

            if (string.IsNullOrWhiteSpace(op) || !VariableOps.Contains(op))
            {
                report.Errors.Add($"{location}: variable op is invalid: '{op}'.");
            }
        }

        private static bool HasVariableMutation(VariableMutationData mutation)
        {
            return mutation != null &&
                   (!string.IsNullOrWhiteSpace(mutation.name) ||
                    !string.IsNullOrWhiteSpace(mutation.op));
        }

        private static void RequireNodeTarget(
            string location,
            string fieldName,
            string nodeId,
            IReadOnlyDictionary<string, NodeData> nodeMap,
            StoryValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                report.Errors.Add($"{location}: required node target '{fieldName}' is empty.");
                return;
            }

            if (!nodeMap.ContainsKey(nodeId.Trim()))
            {
                report.Errors.Add($"{location}: node target '{nodeId}' was not found.");
            }
        }

        private static string ResolveValueByIndex(string[] values, int index, string fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            return index < values.Length ? values[index] : values[values.Length - 1];
        }

        private static string NormalizeCharacterId(string characterId)
        {
            return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId.Trim().PadLeft(4, '0');
        }

        private static string NormalizeFaceKey(string faceKey)
        {
            if (string.IsNullOrWhiteSpace(faceKey))
            {
                return string.Empty;
            }

            var trimmed = faceKey.Trim();
            return int.TryParse(trimmed, out _) ? trimmed.PadLeft(2, '0') : trimmed;
        }

        public sealed class StoryValidationReport
        {
            public int ValidatedStoryCount;
            public readonly List<string> Errors = new();

            public string ToLogString()
            {
                if (Errors.Count == 0)
                {
                    return $"[VNStoryValidator] Validation passed. Stories={ValidatedStoryCount}";
                }

                return $"[VNStoryValidator] Validation failed. Stories={ValidatedStoryCount}, Errors={Errors.Count}\n" +
                       string.Join("\n", Errors);
            }
        }

        private sealed class ResourceIndex
        {
            public readonly HashSet<string> Backgrounds = new(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Bgm = new(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Sfx = new(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> CharacterSprites = new(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Voices = new(StringComparer.OrdinalIgnoreCase);

            public static ResourceIndex Build()
            {
                var index = new ResourceIndex();
                if (!Directory.Exists(ResourceRoot))
                {
                    return index;
                }

                foreach (var path in Directory.GetFiles(ResourceRoot, "*.*", SearchOption.AllDirectories))
                {
                    var normalizedPath = path.Replace('\\', '/');
                    var extension = Path.GetExtension(normalizedPath);
                    var relative = normalizedPath.Substring(ResourceRoot.Length + 1);
                    var relativeWithoutExt = Path.ChangeExtension(relative, null)?.Replace('\\', '/');
                    if (string.IsNullOrWhiteSpace(relativeWithoutExt))
                    {
                        continue;
                    }

                    AddByCategory(index, relativeWithoutExt, extension);
                }

                return index;
            }

            private static void AddByCategory(ResourceIndex index, string relativeWithoutExt, string extension)
            {
                if (relativeWithoutExt.StartsWith("Backgrounds/", StringComparison.OrdinalIgnoreCase))
                {
                    index.Backgrounds.Add(relativeWithoutExt.Substring("Backgrounds/".Length));
                    return;
                }

                if (relativeWithoutExt.StartsWith("BGM/", StringComparison.OrdinalIgnoreCase))
                {
                    index.Bgm.Add(relativeWithoutExt.Substring("BGM/".Length));
                    return;
                }

                if (relativeWithoutExt.StartsWith("SFX/", StringComparison.OrdinalIgnoreCase))
                {
                    index.Sfx.Add(relativeWithoutExt.Substring("SFX/".Length));
                    return;
                }

                if (!relativeWithoutExt.StartsWith("Characters/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var characterKey = relativeWithoutExt.Substring("Characters/".Length);
                if (characterKey.IndexOf("/voice/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (IsAudioExtension(extension))
                    {
                        index.Voices.Add(characterKey);
                    }

                    return;
                }

                if (IsImageExtension(extension))
                {
                    index.CharacterSprites.Add(characterKey);
                }
            }

            private static bool IsImageExtension(string extension)
            {
                return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsAudioExtension(string extension)
            {
                return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
#endif
