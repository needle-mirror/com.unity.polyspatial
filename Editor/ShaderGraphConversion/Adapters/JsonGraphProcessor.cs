using System;
using System.Linq;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class JsonGraphProcessor : IGraphProcessor
    {
        public string FileExtension => "json";

        public bool IsEnabled() => true;

        public bool GenerateIntermediateFile() => true;

        public string ProcessGraph(MtlxGraphData graph)
        {
            return JsonUtility.ToJson(new PolySpatialShaderMetadata()
            {
                properties = graph.Inputs.Concat(graph.SystemInputs).Select(name => new PolySpatialShaderProperty()
                {
                    name = name,
                    type = graph.GetNode(name).datatype switch
                    {
                        MtlxDataTypes.Boolean => PolySpatialShaderPropertyType.Boolean,
                        MtlxDataTypes.Integer => PolySpatialShaderPropertyType.Integer,
                        MtlxDataTypes.Float => PolySpatialShaderPropertyType.Float,
                        MtlxDataTypes.Vector2 => PolySpatialShaderPropertyType.Vector,
                        MtlxDataTypes.Vector3 => PolySpatialShaderPropertyType.Vector,
                        MtlxDataTypes.Vector4 => PolySpatialShaderPropertyType.Vector,
                        MtlxDataTypes.Color3 => PolySpatialShaderPropertyType.Color,
                        MtlxDataTypes.Color4 => PolySpatialShaderPropertyType.Color,
                        MtlxDataTypes.Matrix22 => PolySpatialShaderPropertyType.Matrix,
                        MtlxDataTypes.Matrix33 => PolySpatialShaderPropertyType.Matrix,
                        MtlxDataTypes.Matrix44 => PolySpatialShaderPropertyType.Matrix,
                        MtlxDataTypes.Filename => PolySpatialShaderPropertyType.Texture,
                        var dataType => throw new NotSupportedException($"Unsupported property type: {dataType}"),
                    },
                }).ToArray(),
            });
        }
    }
}