using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Provides static functions to set global shader properties and transfer the updated values to the renderer via PolySpatial.
    /// </summary>
    public static class PolySpatialShaderGlobals
    {
        /// <summary>
        /// The name of the global shader property that represents time since level load (t/20, t, t*2, t*3).
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string Time = "_Time";

        /// <summary>
        /// The name of the global shader property that represents the sine of time since level load (t/8, t/4, t/2, t).
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string SinTime = "_SinTime";

        /// <summary>
        /// The name of the global shader property that represents the cosine of time since level load (t/8, t/4, t/2, t).
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string CosTime = "_CosTime";

        /// <summary>
        /// The name of the global shader property that represents the time delta (dt, 1/dt, smoothDt, 1/smoothDt).
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string DeltaTime = "unity_DeltaTime";

        /// <summary>
        /// The name of the global shader property that represents the world space camera position.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string WorldSpaceCameraPos = "_WorldSpaceCameraPos";

        /// <summary>
        /// The name of the global shader property that represents the world space camera direction.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string WorldSpaceCameraDir = "_WorldSpaceCameraDir";

        /// <summary>
        /// The name of the global shader property that represents the orthographic camera parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string OrthoParams = "unity_OrthoParams";

        /// <summary>
        /// The name of the global shader property that represents the camera's projection parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string ProjectionParams = "_ProjectionParams";

        /// <summary>
        /// The name of the global shader property that represents the screen parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string ScreenParams = "_ScreenParams";

        /// <summary>
        /// The name of the global shader property that represents the orthographic camera parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string ViewMatrix = "UNITY_MATRIX_V";

        /// <summary>
        /// The name of the global shader property that represents the camera's projection matrix.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string ProjectionMatrix = "UNITY_MATRIX_P";

        /// <summary>
        /// The name of the global shader property that represents the ambient sky parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string AmbientSkyColor = "unity_AmbientSky";

        /// <summary>
        /// The name of the global shader property that represents the ambient equator parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string AmbientEquatorColor = "unity_AmbientEquator";

        /// <summary>
        /// The name of the global shader property that represents the ambient ground parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string AmbientGroundColor = "unity_AmbientGround";

        /// <summary>
        /// The name of the global shader property that represents the fog color.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string FogColor = "unity_FogColor";

        /// <summary>
        /// The name of the global shader property that represents the fog parameters.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string FogParams = "unity_FogParams";

        /// <summary>
        /// The number of lights that can be accounted for in a ShaderGraph converted into MaterialX.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const int LightCount = 4;

        /// <summary>
        /// The name of the global shader property that represents the light color.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string LightColorPrefix = "polySpatial_LightColor";

        /// <summary>
        /// The name of the global shader property that represents the light position.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string LightPositionPrefix = "polySpatial_LightPosition";

        /// <summary>
        /// The name of the global shader property that represents the light direction.
        /// </summary>
        ///
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string SpotDirectionPrefix = "polySpatial_SpotDirection";

        /// <summary>
        /// The name of the global shader property that represents the light attenuation.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string LightAttenPrefix = "polySpatial_LightAtten";

        /// <summary>
        /// The name of the global shader property that represents the glossy environment color.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.0.")]
        public const string GlossyEnvironmentColor = "polySpatial_GlossyEnvironmentColor";

        internal const string k_Time = "_Time";
        internal const string k_SinTime = "_SinTime";
        internal const string k_CosTime = "_CosTime";
        internal const string k_DeltaTime = "unity_DeltaTime";
        internal const string k_WorldSpaceCameraPos = "_WorldSpaceCameraPos";
        internal const string k_WorldSpaceCameraDir = "_WorldSpaceCameraDir";
        internal const string k_OrthoParams = "unity_OrthoParams";
        internal const string k_ProjectionParams = "_ProjectionParams";
        internal const string k_ScreenParams = "_ScreenParams";
        internal const string k_ViewMatrix = "UNITY_MATRIX_V";
        internal const string k_ProjectionMatrix = "UNITY_MATRIX_P";
        internal const string k_AmbientSkyColor = "unity_AmbientSky";
        internal const string k_AmbientEquatorColor = "unity_AmbientEquator";
        internal const string k_AmbientGroundColor = "unity_AmbientGround";
        internal const string k_FogColor = "unity_FogColor";
        internal const string k_FogParams = "unity_FogParams";

        internal const int k_LightCount = 4;
        internal const string k_LightColorPrefix = "polySpatial_LightColor";
        internal const string k_LightPositionPrefix = "polySpatial_LightPosition";
        internal const string k_SpotDirectionPrefix = "polySpatial_SpotDirection";
        internal const string k_LightAttenPrefix = "polySpatial_LightAtten";

        internal const string k_GlossyEnvironmentColor = "polySpatial_GlossyEnvironmentColor";

        internal static readonly int TimeID = Shader.PropertyToID(k_Time);
        internal static readonly int SinTimeID = Shader.PropertyToID(k_SinTime);
        internal static readonly int CosTimeID = Shader.PropertyToID(k_CosTime);
        internal static readonly int DeltaTimeID = Shader.PropertyToID(k_DeltaTime);
        internal static readonly int WorldSpaceCameraPosID = Shader.PropertyToID(k_WorldSpaceCameraPos);
        internal static readonly int WorldSpaceCameraDirID = Shader.PropertyToID(k_WorldSpaceCameraDir);
        internal static readonly int OrthoParamsID = Shader.PropertyToID(k_OrthoParams);
        internal static readonly int ProjectionParamsID = Shader.PropertyToID(k_ProjectionParams);
        internal static readonly int ScreenParamsID = Shader.PropertyToID(k_ScreenParams);
        internal static readonly int ViewMatrixID = Shader.PropertyToID(k_ViewMatrix);
        internal static readonly int ProjectionMatrixID = Shader.PropertyToID(k_ProjectionMatrix);
        internal static readonly int AmbientSkyColorID = Shader.PropertyToID(k_AmbientSkyColor);
        internal static readonly int AmbientEquatorColorID = Shader.PropertyToID(k_AmbientEquatorColor);
        internal static readonly int AmbientGroundColorID = Shader.PropertyToID(k_AmbientGroundColor);
        internal static readonly int FogColorID = Shader.PropertyToID(k_FogColor);
        internal static readonly int FogParamsID = Shader.PropertyToID(k_FogParams);
        internal static readonly int GlossyEnvironmentColorID = Shader.PropertyToID(k_GlossyEnvironmentColor);

        internal static readonly int[] LightColorIDs = GetLightPropertyIDs(k_LightColorPrefix);
        internal static readonly int[] LightPositionIDs = GetLightPropertyIDs(k_LightPositionPrefix);
        internal static readonly int[] SpotDirectionIDs = GetLightPropertyIDs(k_SpotDirectionPrefix);
        internal static readonly int[] LightAttenIDs = GetLightPropertyIDs(k_LightAttenPrefix);

        /// <summary>
        /// Enumerates the data types of the shader global properties that you can transfer to a renderer via PolySpatial.
        /// </summary>
        /// <remarks>
        /// Use with <see cref="[PolySpatialShaderGlobals.TryAdd](xref:Unity.PolySpatial.PolySpatialShaderGlobals.TryAdd(System.String,Unity.PolySpatial.PolySpatialShaderGlobals.PropertyType))"/>.
        /// </remarks>
        public enum PropertyType
        {
            /// <summary>
            /// Float global property type.
            /// </summary>
            Float,
            /// <summary>
            /// Integer global property type.
            /// </summary>
            Integer,
            /// <summary>
            /// Vector global property type.
            /// </summary>
            Vector,
            /// <summary>
            /// Color global property type.
            /// </summary>
            Color,
            /// <summary>
            /// Matrix global property type.
            /// </summary>
            Matrix,
            /// <summary>
            /// Texture global property type.
            /// </summary>
            Texture,
        }

        static Dictionary<string, PropertyType> s_PropertyTypes = new()
        {
            [k_Time] = PropertyType.Vector,
            [k_SinTime] = PropertyType.Vector,
            [k_CosTime] = PropertyType.Vector,
            [k_DeltaTime] = PropertyType.Vector,
            [k_WorldSpaceCameraPos] = PropertyType.Vector,
            [k_WorldSpaceCameraDir] = PropertyType.Vector,
            [k_OrthoParams] = PropertyType.Vector,
            [k_ProjectionParams] = PropertyType.Vector,
            [k_ScreenParams] = PropertyType.Vector,
            [k_ViewMatrix] = PropertyType.Matrix,
            [k_ProjectionMatrix] = PropertyType.Matrix,
            [k_AmbientSkyColor] = PropertyType.Vector,
            [k_AmbientEquatorColor] = PropertyType.Vector,
            [k_AmbientGroundColor] = PropertyType.Vector,
            [k_FogColor] = PropertyType.Vector,
            [k_FogParams] = PropertyType.Vector,
            [k_GlossyEnvironmentColor] = PropertyType.Vector,
        };

        static Dictionary<PropertyType, string[]> s_Names = new();

        static PolySpatialShaderGlobals()
        {
            for (var i = 0; i < k_LightCount; ++i)
            {
                s_PropertyTypes.Add(k_LightColorPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(k_LightPositionPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(k_SpotDirectionPrefix + i, PropertyType.Vector);
                s_PropertyTypes.Add(k_LightAttenPrefix + i, PropertyType.Vector);
            }
        }

        static int[] GetLightPropertyIDs(string prefix)
        {
            var propertyIDs = new int[k_LightCount];
            for (var i = 0; i < k_LightCount; ++i)
            {
                propertyIDs[i] = Shader.PropertyToID(prefix + i);
            }
            return propertyIDs;
        }

        internal static int GetCount()
        {
            return s_PropertyTypes.Count;
        }

        internal static string[] GetNames(PropertyType type)
        {
            if (s_Names.TryGetValue(type, out var names))
                return names;

            names = s_PropertyTypes
                .Where(entry => entry.Value == type)
                .Select(entry => entry.Key)
                .ToArray();

            s_Names.Add(type, names);

            return names;
        }

        /// <summary>
        /// Sets the value of a float shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetFloat(string name, float value)
        {
            if (TryAdd(name, PropertyType.Float))
                Shader.SetGlobalFloat(name, value);
        }

        /// <summary>
        /// Sets the value of an integer shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetInteger(string name, int value)
        {
            if (TryAdd(name, PropertyType.Integer))
                Shader.SetGlobalInteger(name, value);
        }

        /// <summary>
        /// Sets the value of a vector shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetVector(string name, Vector4 value)
        {
            if (TryAdd(name, PropertyType.Vector))
                Shader.SetGlobalVector(name, value);
        }

        /// <summary>
        /// Sets the value of a color shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetColor(string name, Color value)
        {
            if (TryAdd(name, PropertyType.Color))
                Shader.SetGlobalColor(name, value);
        }

        /// <summary>
        /// Sets the value of a matrix shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetMatrix(string name, Matrix4x4 value)
        {
            if (TryAdd(name, PropertyType.Matrix))
                Shader.SetGlobalMatrix(name, value);
        }

        /// <summary>
        /// Sets the value of a texture shader global and adds it to the list of globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to set.</param>
        /// <param name="value">The new value for the shader global.</param>
        public static void SetTexture(string name, Texture value)
        {
            if (TryAdd(name, PropertyType.Texture))
                Shader.SetGlobalTexture(name, value);
        }

        /// <summary>
        /// Attempts to add a property to the list of shader globals to transfer via PolySpatial.
        /// </summary>
        /// <param name="name">The name of the shader global to add.</param>
        /// <param name="type">The type of the shader global.</param>
        /// <returns>True if added or already present with the same type, false if already present
        /// with a different type (in which case an error will be logged).</returns>
        public static bool TryAdd(string name, PropertyType type)
        {
            if (!s_PropertyTypes.TryGetValue(name, out var currentType))
            {
                s_PropertyTypes.Add(name, type);

                // Force the list of names to be regenerated next time it is requested.
                s_Names.Remove(type);

                return true;
            }
            if (currentType == type)
                return true;

            Debug.LogError($"Global {name} already exists with a different type: {currentType}.");
            return false;
        }
    }
}
