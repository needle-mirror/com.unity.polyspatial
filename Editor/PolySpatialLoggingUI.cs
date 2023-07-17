using System;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    class PolySpatialLoggingUI : EditorWindow
    {
        private static GUIContent s_Title = new GUIContent("PolySpatial Stack Logging UI");
        [MenuItem("Window/PolySpatial/Logging")]
        static void Init()
        {
            var win = (PolySpatialLoggingUI) EditorWindow.GetWindow(typeof(PolySpatialLoggingUI));
            win.Show();
        }

        private bool categoriesVisible = true;
        private bool stackTraceVisible = true;

        private static GUIContent k_CategoriesContent = new GUIContent("Categories (Enabled / Stack Trace)", null, "Toggle logging categories to change what is output to the console log.");
        private static GUIContent k_StackTraceContent = new GUIContent("Levels (Stack Trace)", null, "Toggle stack trace logging for different log types.");

        private void OnGUI()
        {
            titleContent = s_Title;

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            categoriesVisible =
                EditorGUILayout.BeginFoldoutHeaderGroup(categoriesVisible, k_CategoriesContent);

            if (categoriesVisible)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginVertical();

                foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
                {
                    GUILayout.BeginHorizontal();

                    bool catEnabled = Logging.IsCategoryEnabled(cat);
                    bool stackEnabled = Logging.IsStackTraceEnabled(cat);
                    var name = Enum.GetName(typeof(LogCategory), cat);

                    var newCat = EditorGUILayout.Toggle(catEnabled, GUILayout.Width(30));
                    var newStack = EditorGUILayout.ToggleLeft(name, stackEnabled);

                    if (newStack != stackEnabled)
                    {
                        Logging.SetStackTraceEnabled(cat, newStack);
                    }

                    if (newCat != catEnabled)
                    {
                        Logging.SetCategoryEnabled(cat, newCat);
                    }

                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            stackTraceVisible = EditorGUILayout.BeginFoldoutHeaderGroup(stackTraceVisible, k_StackTraceContent);

            if (stackTraceVisible)
            {
                EditorGUILayout.BeginVertical();

                EditorGUI.indentLevel++;

                foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                {
                    bool isLoggingEnabled = Application.GetStackTraceLogType(logType) != StackTraceLogType.None;
                    string logTypeName = Enum.GetName(typeof(LogType), logType);
                    bool newValue = EditorGUILayout.ToggleLeft(logTypeName, isLoggingEnabled);
                    if (newValue != isLoggingEnabled)
                    {
                        Application.SetStackTraceLogType(logType, newValue ? StackTraceLogType.ScriptOnly : StackTraceLogType.None);
                    }
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndVertical();
        }
    }
}
