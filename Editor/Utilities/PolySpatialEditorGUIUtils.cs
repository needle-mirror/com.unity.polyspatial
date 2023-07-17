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
        /// Draw a help box with the Fix button and returns whether the user clicked in the button.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="buttonTooltip">The button tooltip.</param>
        /// <returns>Returns <see langword="true"/> when the user clicks the Fix button. Otherwise, returns <see langword="false"/>.</returns>
        internal static bool DrawFixMeBox(string message, MessageType messageType, string buttonLabel, string buttonTooltip)
        {
            var messageContent = EditorGUIUtility.TrTextContentWithIcon(message, styles.GetMessageTypeIcon(messageType));
            var buttonContent = EditorGUIUtility.TrTextContent(buttonLabel, buttonTooltip);
            return DrawFixMeBox(messageContent, buttonContent);
        }

        /// <summary>
        /// Draw a help box with the Fix button and returns whether the user clicked in the button.
        /// </summary>
        /// <param name="message">The message with icon if needed.</param>
        /// <param name="buttonContent">The button content.</param>
        /// <returns>Returns <see langword="true"/> when the user clicks the Fix button. Otherwise, returns <see langword="false"/>.</returns>
        static bool DrawFixMeBox(GUIContent message, GUIContent buttonContent)
        {
            EditorGUILayout.BeginHorizontal();

            var indent = EditorGUI.indentLevel * k_IndentMargin - EditorStyles.helpBox.margin.left;
            GUILayoutUtility.GetRect(indent, EditorGUIUtility.singleLineHeight, EditorStyles.helpBox, GUILayout.ExpandWidth(false));

            var leftRect = GUILayoutUtility.GetRect(buttonContent, EditorStyles.miniButton, GUILayout.MinWidth(60));
            var rect = GUILayoutUtility.GetRect(message, EditorStyles.helpBox);
            var boxRect = new Rect(leftRect.x, rect.y, rect.xMax - leftRect.xMin, rect.height);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (Event.current.type == EventType.Repaint)
                EditorStyles.helpBox.Draw(boxRect, false, false, false, false);

            var labelRect = new Rect(boxRect.x + 4, boxRect.y + 3, rect.width - 8, rect.height);
            EditorGUI.LabelField(labelRect, message, styles.fixMeBox);

            var buttonRect = leftRect;
            buttonRect.x += rect.width - 2;
            buttonRect.y = rect.yMin + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            var clicked = GUI.Button(buttonRect, buttonContent);

            EditorGUI.indentLevel = oldIndent;
            EditorGUILayout.EndHorizontal();

            return clicked;
        }

        /// <summary>
        /// Draw a message box with the given message and type.
        /// </summary>
        /// <param name="message">The message text, supports rich text.</param>
        /// <param name="messageType">The type of the message.</param>
        internal static void DrawMessageBox(string message, MessageType messageType)
        {
            var messageContent = EditorGUIUtility.TrTextContentWithIcon(message, styles.GetMessageTypeIcon(messageType));
            EditorGUILayout.LabelField(GUIContent.none, messageContent, styles.messageBox);
        }

        /// <summary>
        /// Draws a message box with the given message and type, and adds a link at the end.
        /// </summary>
        /// <param name="message">The message text, supports rich text.</param>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="linkTitle">The link title that the user will click on</param>
        /// <param name="linkUrl">The link url</param>
        internal static void DrawMessageBoxWithLink (string message, MessageType messageType, string linkTitle, string linkUrl)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(styles.GetMessageTypeIcon(messageType), GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label(message,EditorGUIUtils.Styles.WordWrapMiniLabel);
            EditorGUIUtils.DrawLink(new GUIContent(linkTitle), linkUrl);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
