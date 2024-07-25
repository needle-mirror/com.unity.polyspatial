namespace UnityEditor.PolySpatial.Validation
{
    class ReflectionProbeMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>Reflection Probe</b> component is only supported for shaders using the <b>PolySpatial Lighting Node</b>." +
                                       "For more information, see: ";

        string ITypeMessage.Message => k_MessageFormat;
        MessageType ITypeMessage.MessageType => MessageType.Info;
        ITypeMessage.LinkData ITypeMessage.Link { get; } = new ITypeMessage.LinkData("Documentation",
            "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/PolySpatialLighting.html#reflection-probes");
    }
}
