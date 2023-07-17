using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class SwizzleAdapter : ANodeAdapter<SwizzleNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is SwizzleNode snode)
            {
                var portMap = new Dictionary<string, string>();
                portMap.Add("In", "in");
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Swizzle, node, graph, externals, "Swizzle", portMap);

                var value = snode.maskInput;
                var inputType = SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node));

                StringBuilder sb = new();
                if (MtlxDataTypes.IsColor(inputType) && value.Any(c => "xyzw".Contains(c)))
                {
                    foreach (char c in value)
                    {
                        switch(c)
                        {
                            case 'x': sb.Append('r'); break;
                            case 'y': sb.Append('g'); break;
                            case 'z': sb.Append('b'); break;
                            case 'w': sb.Append('a'); break;
                        }
                    }
                    value = sb.ToString();
                }
                else if (MtlxDataTypes.IsVector(inputType) && value.Any(c => "rgba".Contains(c)))
                {
                    foreach (char c in value)
                    {
                        switch (c)
                        {
                            case 'r': sb.Append('x'); break;
                            case 'g': sb.Append('y'); break;
                            case 'b': sb.Append('z'); break;
                            case 'a': sb.Append('w'); break;
                        }
                    }
                    value = sb.ToString();
                }

                nodeData.AddPortString("channels", MtlxDataTypes.String, value);
            }
        }
    }
}
