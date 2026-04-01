using System.Collections.Generic;
using UnityEngine;
using VN.Data;

namespace VN.Core
{
    public class StoryRuntime
    {
        private readonly Dictionary<string, NodeData> _nodeMap = new();
        private bool _skipAdvanceOnce;

        public StoryData Story { get; private set; }
        public NodeData CurrentNode { get; private set; }
        public int CommandIndex { get; private set; }
        public bool IsEnded { get; private set; }

        public readonly List<string> Backlog = new();

        public void Initialize(StoryData story)
        {
            Story = story;
            _nodeMap.Clear();
            IsEnded = false;
            CommandIndex = 0;
            _skipAdvanceOnce = false;
            Backlog.Clear();

            if (story?.nodes == null)
            {
                Debug.LogError("[StoryRuntime] Story or nodes is null.");
                IsEnded = true;
                return;
            }

            foreach (var node in story.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    continue;
                }
                _nodeMap[node.id] = node;
            }

            JumpToNode(story.startNode);
            // Initialization starts at the first command immediately,
            // so do not consume the first AdvanceCommand call.
            _skipAdvanceOnce = false;
        }

        public CommandData GetCurrentCommand()
        {
            if (IsEnded || CurrentNode?.commands == null)
            {
                return null;
            }

            if (CommandIndex < 0 || CommandIndex >= CurrentNode.commands.Length)
            {
                return null;
            }

            return CurrentNode.commands[CommandIndex];
        }

        public void AdvanceCommand()
        {
            if (IsEnded)
            {
                return;
            }

            if (_skipAdvanceOnce)
            {
                _skipAdvanceOnce = false;
                return;
            }

            CommandIndex++;
            if (CurrentNode?.commands == null || CommandIndex >= CurrentNode.commands.Length)
            {
                End();
            }
        }

        public void JumpToNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !_nodeMap.TryGetValue(nodeId, out var node))
            {
                Debug.LogError($"[StoryRuntime] Node not found: {nodeId}");
                End();
                return;
            }

            CurrentNode = node;
            CommandIndex = 0;
            _skipAdvanceOnce = true;
        }

        public void End()
        {
            IsEnded = true;
            Debug.Log("[StoryRuntime] Story ended.");
        }

        public void AddBacklog(string speaker, string text)
        {
            Backlog.Add($"{speaker}: {text}");
        }
    }
}