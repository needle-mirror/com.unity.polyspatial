
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ObjectAdapter : ANodeAdapter<ObjectNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectPosition, MtlxDataTypes.Vector3, node, graph, externals, "Position");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectScale, MtlxDataTypes.Vector3, node, graph, externals, "Scale");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsMin, MtlxDataTypes.Vector3, node, graph, externals, "World Bounds Min");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsMax, MtlxDataTypes.Vector3, node, graph, externals, "World Bounds Max");
            QuickNode.AddImplicitPropertyFromNode(MtlxImplicitProperties.ObjectBoundsSize, MtlxDataTypes.Vector3, node, graph, externals, "Bounds Size");
#else
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Object-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Object", @"
float4x4 objectToWorld = unity_ObjectToWorld;
Position = mul(objectToWorld, float4(0, 0, 0, 1)).xyz;
Scale = float3(
    length(mul(objectToWorld, float4(1, 0, 0, 0))),
    length(mul(objectToWorld, float4(0, 1, 0, 0))),
    length(mul(objectToWorld, float4(0, 0, 1, 0))));
float3 worldBoundsCenter = mul(objectToWorld, float4(polySpatial_ObjectBoundsCenter, 1)).xyz;
// See TransformAABB:
// https://github.cds.internal.unity3d.com/unity/unity/blob/93a364f095f55c0e7616dc8d1638d6c6c37b5ad5/Runtime/Geometry/AABB.cpp#L217
// (This variation assumes that the extents will be positive, which should always be true for our purposes.)
float3 worldBoundsExtents = float3(
    dot(polySpatial_ObjectBoundsExtents, abs(mul(float4(1, 0, 0, 0), objectToWorld).xyz)),
    dot(polySpatial_ObjectBoundsExtents, abs(mul(float4(0, 1, 0, 0), objectToWorld).xyz)),
    dot(polySpatial_ObjectBoundsExtents, abs(mul(float4(0, 0, 1, 0), objectToWorld).xyz)));
WorldBoundsMin = worldBoundsCenter - worldBoundsExtents;
WorldBoundsMax = worldBoundsCenter + worldBoundsExtents;
BoundsSize = worldBoundsExtents * 2;");
#endif
        }
    }
}
