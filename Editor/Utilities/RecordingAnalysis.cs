using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.Utilities
{
    internal class RecordingAnalysis : EditorWindow
    {
        const string s_LabelIdName = "label-id";
        const string s_LabelCommandName = "command-label";
        const string s_LabelExtraName = "extra-label";

        MemoryMappedFile m_File;
        MemoryMappedViewStream m_ViewStream;
        BinaryReader m_Reader;

        struct CommandInfo
        {
            // offset of this command (bytes)
            public long offset;

            // the command
            public PolySpatialCommand cmd;

            // number of child commands; e.g. a frame will have the number of elements in the frame (+ the EndFrame)
            public int childCount;
        }

        List<CommandInfo> m_Commands;
        List<TreeViewItemData<int>> m_TreeItems;

        public void Analyze(string path)
        {
            m_ViewStream?.Dispose();
            m_File?.Dispose();

            m_File = MemoryMappedFile.CreateFromFile(path, FileMode.Open);

            m_Commands = new();

            m_ViewStream = m_File.CreateViewStream();
            m_Reader = new BinaryReader(m_ViewStream);

            var reader = m_Reader;

            RecordingData.ReadHeader(reader, out var header);

            int lastFrameIndex = -1;
            int frameCommandCount = 0;

            long offset = reader.BaseStream.Position;

            while (RecordingData.TryReadCommand(reader, Allocator.None, out var command))
            {
                var cmd = (PolySpatialCommand)command.Command;

                CommandInfo info = default;
                info.offset = offset;
                info.cmd = cmd;

                m_Commands.Add(info);
                frameCommandCount += 1;

                if (cmd == PolySpatialCommand.BeginAppFrame)
                {
                    lastFrameIndex = m_Commands.Count - 1;
                    frameCommandCount = 0;
                }
                else if (cmd == PolySpatialCommand.EndAppFrame)
                {
                    var frameInfo = m_Commands[lastFrameIndex];
                    frameInfo.childCount = frameCommandCount;
                    m_Commands[lastFrameIndex] = frameInfo;
                }

                offset = reader.BaseStream.Position;
            }

            m_TreeItems = new();
            int childrenLeft = 0;
            int lastTopLevelParentIndex = -1;
            List<TreeViewItemData<int>> children = null;

            for (var i = 0; i < m_Commands.Count; i++)
            {
                var info = m_Commands[i];
                if (childrenLeft > 0)
                {
                    children.Add(new TreeViewItemData<int>(i, i));
                    childrenLeft -= 1;

                    if (childrenLeft == 0)
                    {
                        m_TreeItems.Add(new TreeViewItemData<int>(lastTopLevelParentIndex, lastTopLevelParentIndex, children));
                        children = null;
                    }
                }
                else if (info.childCount > 0)
                {
                    childrenLeft = info.childCount;
                    children = new();
                    lastTopLevelParentIndex = i;
                }
                else
                {
                    m_TreeItems.Add(new TreeViewItemData<int>(i, i));
                }
            }

            m_TreeView.SetRootItems(m_TreeItems);
        }

        private int m_NextId;
        private TreeView m_TreeView;
        private ScrollView m_ScrollView;
        private HashSet<int> m_BoundIndices = new HashSet<int>();

        public RecordingAnalysis()
        {
        }

        void CreateGUI()
        {
            Func<VisualElement> makeItem = () =>
            {
                var box = new VisualElement();
                box.style.flexDirection = FlexDirection.Row;
                box.style.flexGrow = 1f;
                box.style.flexShrink = 0f;
                box.style.flexBasis = 0f;

                var labelId = new Label() { name = s_LabelIdName };
                var labelCmd = new Label() { name = s_LabelCommandName };
                var labelExtra = new Label() { name = s_LabelExtraName };

                box.Add(labelId);
                box.Add(labelCmd);
                box.Add(labelExtra);

                return box;
            };

            m_TreeView = new TreeView();
            m_TreeView.fixedItemHeight = 20;
            m_TreeView.makeItem = makeItem;
            m_TreeView.bindItem = BindItem;
            m_TreeView.unbindItem = UnbindItem;

            m_ScrollView = m_TreeView.Q<ScrollView>();

            m_TreeView.selectionType = SelectionType.Single;
            m_TreeView.style.width = 400;

            rootVisualElement.Add(m_TreeView);
        }

        void BindItem(VisualElement e, int index)
        {
            var id = m_TreeView.GetIdForIndex(index);
            var info = m_Commands[id];

            e.Q<Label>(s_LabelIdName).text = id.ToString();
            e.Q<Label>(s_LabelCommandName).text = Enum.GetName(typeof(PolySpatialCommand), info.cmd);
            e.Q<Label>(s_LabelExtraName).text = DescribeCommand(info);
        }

        void UnbindItem(VisualElement item, int index)
        {
        }

        unsafe string DescribeCommand(CommandInfo info)
        {
            switch (info.cmd)
            {
                case PolySpatialCommand.AddEntitiesWithTransforms:
                case PolySpatialCommand.SetEntityTransforms:
                {
                    using var data = GetTempCommandDataFor(info);
                    var count = *(int*)data[0];
                    return $"Count: {count}";
                }

                case PolySpatialCommand.BeginAppFrame:
                    return $"({info.childCount} commands)";
            }

            return "";
        }

        RecordingData.CommandData GetTempCommandDataFor(CommandInfo info)
        {
            var offset = info.offset;
            var cmd = info.cmd;

            m_Reader.BaseStream.Position = offset;
            RecordingData.TryReadCommand(m_Reader, Allocator.Temp, out var data);
            return data;
        }
    }
}
