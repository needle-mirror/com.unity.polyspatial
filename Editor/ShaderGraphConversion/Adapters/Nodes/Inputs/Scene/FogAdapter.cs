namespace UnityEditor.ShaderGraph.MaterialX
{
    class FogAdapter : ANodeAdapter<FogNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // See:
            // https://github.cds.internal.unity3d.com/unity/unity/blob/e837d3a2b4d8abf7605e479584d89ca0722967b5/Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl#L95
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Fog", @"
Color = unity_FogColor;
float viewZ = -mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(Position, 1))).z;
float4 row = mul(float4(0, 0, 1, 0), UNITY_MATRIX_P);
// https://stackoverflow.com/questions/56428880/how-to-extract-camera-parameters-from-projection-matrix
float nearZ = row.w / (row.z - 1);
float z = max(viewZ - nearZ, 0);
// https://github.cds.internal.unity3d.com/unity/unity/blob/e50be50723c5036ddb12a6b48e5ae7131337f2ee/Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl#L304
Density =
    FOG_LINEAR ? 1 - saturate(z * unity_FogParams.z + unity_FogParams.w) :
    FOG_EXP ? 1 - saturate(exp2(-unity_FogParams.x * z)) :
    FOG_EXP2 ? 1 - saturate(exp2(-unity_FogParams.y * z * z)) :
    0;
");
        }
    }
}