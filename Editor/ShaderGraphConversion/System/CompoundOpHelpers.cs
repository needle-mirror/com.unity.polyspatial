using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class CompoundOpContext
    {
        internal readonly AbstractMaterialNode node;
        internal readonly MtlxGraphData graph;
        internal readonly ExternalEdgeMap externals;
        internal string hint;
        internal Dictionary<string, NodeDef> nodeDefs;
        internal Dictionary<string, MtlxNodeData> nodeData = new();
        internal Dictionary<string, MtlxNodeData> externalDotNodes = new();

        internal CompoundOpContext(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals,
            string hint, Dictionary<string, NodeDef> nodeDefs)
        {
            this.node = node;
            this.graph = graph;
            this.externals = externals;
            this.hint = hint;
            this.nodeDefs = nodeDefs;
        }
    }

    internal class NodeDef
    {
        readonly string nodeType;
        readonly string outputType;
        readonly Dictionary<string, InputDef> inputs;

        internal NodeDef(string nodeType, string outputType, Dictionary<string, InputDef> inputs)
        {
            this.nodeType = nodeType;
            this.outputType = outputType;
            this.inputs = inputs;
        }

        internal MtlxNodeData AddNodesAndEdges(CompoundOpContext ctx, string key)
        {
            if (!ctx.nodeData.TryGetValue(key, out var nodeDatum))
            {
                nodeDatum = ctx.graph.AddNode(
                    NodeUtils.GetNodeName(ctx.node, $"{ctx.hint}_{key}"), nodeType, outputType);
                ctx.nodeData.Add(key, nodeDatum);

                var outputSlot = NodeUtils.GetOutputByName(ctx.node, key);
                if (outputSlot != null)
                    ctx.externals.AddExternalPort(outputSlot.slotReference, nodeDatum.name);
                
                foreach (var (inputName, inputDef) in inputs)
                {
                    inputDef.AddPortsAndEdges(ctx, nodeDatum, key, inputName);
                }
            }
            return nodeDatum;
        }
    }

    internal abstract class InputDef
    {
        internal abstract void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey);
    }

    // An input that resolves to a constant numeric value.
    internal class FloatInputDef : InputDef
    {
        readonly string portType;
        readonly float[] values;

        internal FloatInputDef(string portType, params float[] values)
        {
            this.portType = portType;
            this.values = values;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            nodeDatum.AddPortValue(inputKey, portType, values);
        }
    }

    // An input that resolves to a constant string value.
    internal class StringInputDef : InputDef
    {
        readonly string value;

        internal StringInputDef(string value)
        {
            this.value = value;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            nodeDatum.AddPortString(inputKey, MtlxDataTypes.String, value);
        }
    }

    // An input that resolves to the output of a node defined and mapped in the compound op.
    internal class InternalInputDef : InputDef
    {
        readonly string source;

        internal InternalInputDef(string source)
        {
            this.source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var sourceNode = ctx.nodeDefs[source].AddNodesAndEdges(ctx, source);
            ctx.graph.AddPortAndEdge(sourceNode.name, nodeDatum.name, inputKey, sourceNode.datatype);
        }
    }

    // An input that resolves to one of the inputs of the original shader graph node.
    internal class ExternalInputDef : InputDef
    {
        readonly string source;

        internal ExternalInputDef(string source)
        {
            this.source = source;
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            if (!ctx.externalDotNodes.TryGetValue(source, out var dotNode))
            {
                var slot = NodeUtils.GetInputByName(ctx.node, source);
                var dataType = SlotUtils.GetDataTypeName(slot);

                // "Dot" is the identity function, not a dot product; we create a new node to represent
                // the external input because ExternalEdgeMap can only map a slot to a single port, but we
                // assume that this will be optimized out in the final shader code.
                dotNode = ctx.graph.AddNode(
                    NodeUtils.GetNodeName(ctx.node, $"{ctx.hint}_{source}"), MtlxNodeTypes.Dot, dataType);
                ctx.externalDotNodes.Add(source, dotNode);
                
                // Unconnected UV inputs need a GeomTexCoord node (flipping the v coord)
                if (!slot.isConnected && slot is UVMaterialSlot uvSlot)
                {
                    var texCoordNode = ctx.graph.AddNode($"{dotNode.name}UV", MtlxNodeTypes.GeomTexCoord, dataType);
                    texCoordNode.AddPortValue("index", MtlxDataTypes.Integer, new[] { (float)uvSlot.channel });
                    
                    var multiplyNode = ctx.graph.AddNode($"{dotNode.name}Multiply", MtlxNodeTypes.Multiply, dataType);
                    ctx.graph.AddPortAndEdge(texCoordNode.name, multiplyNode.name, "in1", dataType);
                    multiplyNode.AddPortValue("in2", dataType, new[] { 1.0f, -1.0f });

                    var addNode = ctx.graph.AddNode($"{dotNode.name}Add", MtlxNodeTypes.Add, dataType);
                    ctx.graph.AddPortAndEdge(multiplyNode.name, addNode.name, "in1", dataType);
                    addNode.AddPortValue("in2", dataType, new[] { 0.0f, 1.0f });

                    ctx.graph.AddPortAndEdge(addNode.name, dotNode.name, "in", dataType);
                }
                else
                {
                    QuickNode.AddInputPortAndEdge(ctx.externals, dotNode, slot, "in", dataType);
                }
            }
            ctx.graph.AddPortAndEdge(dotNode.name, nodeDatum.name, inputKey, dotNode.datatype);
        }
    }

    // An input that resolves to the output of an unmapped node described inline in the constructor.
    internal class InlineInputDef : InputDef
    {
        readonly NodeDef source;

        internal InlineInputDef(string nodeType, string outputType, Dictionary<string, InputDef> inputs)
        {
            this.source = new NodeDef(nodeType, outputType, inputs);
        }

        internal override void AddPortsAndEdges(
            CompoundOpContext ctx, MtlxNodeData nodeDatum, string nodeKey, string inputKey)
        {
            var sourceNode = source.AddNodesAndEdges(ctx, $"{nodeKey}_{inputKey}");
            ctx.graph.AddPortAndEdge(sourceNode.name, nodeDatum.name, inputKey, sourceNode.datatype);
        }
    }
}