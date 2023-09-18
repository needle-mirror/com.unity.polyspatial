using System;
using Unity.XR.CoreUtils.Editor;
using UnityEngine;

namespace UnityEditor.PolySpatial.Utilities
{
    /// <summary>
    /// Utility class for Editor GUI.
    /// </summary>
    static class PolySpatialEditorGUIUtils
    {
        const float k_IndentMargin = 15.0f;

        class Styles
        {
            internal Styles()
            {
                m_IconInfo = EditorGUIUtility.IconContent("console.infoicon").image as Texture2D;
                m_IconWarn = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                m_IconFail = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;

                fixMeBox = new GUIStyle { imagePosition = ImagePosition.ImageLeft, fontSize = 10, wordWrap = true, richText = true };
                fixMeBox.normal.textColor = EditorStyles.helpBox.normal.textColor;

                messageBox = new GUIStyle(EditorStyles.helpBox) { richText = true };
            }

            readonly Texture2D m_IconWarn;
            readonly Texture2D m_IconInfo;
            readonly Texture2D m_IconFail;

            internal readonly GUIStyle fixMeBox;
            internal readonly GUIStyle messageBox;

            /// <summary>
            /// Gets the icon that describes the <see cref="MessageType"/>
            /// </summary>
            /// <param name="messageType">The <see cref="MessageType"/> to obtain the icon from</param>
            /// <returns>a <see cref="Texture2D"/> with the icon for the <see cref="MessageType"/></returns>
            internal Texture2D GetMessageTypeIcon(MessageType messageType)
            {
                switch (messageType)
                {
                    case MessageType.None:
                        return null;
                    case MessageType.Info:
                        return m_IconInfo;
                    case MessageType.Warning:
                        return m_IconWarn;
                    case MessageType.Error:
                        return m_IconFail;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
                }
            }
        }

        static Styles s_Styles;

        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        /// <summary>
        /// Draw a help box with the Fix button, an optional link and returns whether the user clicked in the button.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="buttonTooltip">The button tooltip.</param>
        /// <param name="helpText">The link title that the user will click on</param>
        /// <param name="helpLink">The link url</param>
        /// <returns>Returns <see langword="true"/> when the user clicks the Fix button. Otherwise, returns <see langword="false"/>.</returns>
        internal static bool DrawFixMeBox(string message, MessageType messageType, string buttonLabel, string buttonTooltip, string helpText, string helpLink)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(styles.GetMessageTypeIcon(messageType), GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label(message, EditorGUIUtils.Styles.WordWrapMiniLabel);
            if (!string.IsNullOrEmpty(helpText) && !string.IsNullOrEmpty(helpLink))
                EditorGUIUtils.DrawLink(new GUIContent(helpText), helpLink);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var buttonContent = EditorGUIUtility.TrTextContent(buttonLabel, buttonTooltip);
            var clicked = GUILayout.Button(buttonContent, GUILayout.MinWidth(60));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            return clicked;
        }

        /// <summary>
        /// Draws a message box with the given message and type and adds an optional link at the end if one is passed.
        /// </summary>
        /// <param name="message">The message text, supports rich text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="linkTittle">The link title that the user will click on</param>
        /// <param name="linkURL">The link url</param>
        internal static void DrawMessageBox(string message, MessageType messageType, string linkTittle = "", string linkURL = "")
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(styles.GetMessageTypeIcon(messageType), GUILayout.ExpandWidth(false));

            GUILayout.BeginVertical();
            GUILayout.Label(message, EditorGUIUtils.Styles.WordWrapMiniLabel);
            if (!string.IsNullOrEmpty(linkTittle) && !string.IsNullOrEmpty(linkURL))
                EditorGUIUtils.DrawLink(new GUIContent(linkTittle), linkURL);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
