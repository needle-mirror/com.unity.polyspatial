namespace UnityEditor.PolySpatial.Validation
{
    class RendererShaderMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) do not support shaders other than the built-in pipeline shaders and shaders authored using Unity's Shader graph.";

        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;
        public ITypeMessage.LinkData Link { get; } = new ITypeMessage.LinkData("Documentation",
            "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/PortingUnityProjectsToPolySpatialXR.html%23rendering");
    }
}
