namespace UnityEditor.ShaderGraph.MaterialX
{
    class FlipbookAdapter : AbstractUVNodeAdapter<FlipbookNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is not FlipbookNode flipbookNode)
                return;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Flipbook-Node.html
            QuickNode.CompoundOp(node, graph, externals, "Flipbook", new()
            {
                // Tile = fmod(Tile, Width * Height);
                ["WrappedTile"] = new(MtlxNodeTypes.Modulo, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new ExternalInputDef("Tile"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("Width"),
                        ["in2"] = new ExternalInputDef("Height"),
                    }),
                }),
                // float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
                ["TileCount"] = new(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Vector2, 1.0f, 1.0f),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new ExternalInputDef("Width"),
                        ["in2"] = new ExternalInputDef("Height"),
                    }),
                }),
                ["TileFloor"] = new(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InternalInputDef("WrappedTile"),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InternalInputDef("TileCount"),
                            ["channels"] = new StringInputDef("x"),
                        }),
                    }),
                }),
                // float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
                ["TileY"] = new(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                {
                    ["in"] = flipbookNode.invertY.isOn
                        ? new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("Height"),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new InternalInputDef("TileFloor"),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        })
                        : new InternalInputDef("TileFloor"),
                }),
                // float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
                ["TileX"] = new(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                {
                    ["in"] = flipbookNode.invertX.isOn
                        ? new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("Width"),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new InternalInputDef("WrappedTile"),
                                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                    {
                                        ["in1"] = new ExternalInputDef("Width"),
                                        ["in2"] = new InternalInputDef("TileFloor"),
                                    }),
                                }),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        })
                        : new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new ExternalInputDef("Width"),
                                ["in2"] = new InternalInputDef("TileFloor"),
                            }),
                            ["in2"] = new InternalInputDef("WrappedTile"),
                        }),
                }),
                // Out = (UV + float2(tileX, tileY)) * tileCount;
                ["Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new ExternalInputDef("UV"),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new InternalInputDef("TileX"),
                            ["in2"] = new InternalInputDef("TileY"),
                        }),
                    }),
                    ["in2"] = new InternalInputDef("TileCount"),
                }),
            });
        }
    }
}