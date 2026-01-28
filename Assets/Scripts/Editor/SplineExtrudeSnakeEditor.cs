using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineExtrudeSnake))]
    public class SplineExtrudeSnakeEditor : Editor
    {
        private bool showCustomPath = false;

        override public void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SplineExtrudeSnake snake = (SplineExtrudeSnake)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Arrow Data (Runtime/Loaded Content)", EditorStyles.boldLabel);

            if (snake.ArrowData != null)
            {
                EditorGUILayout.BeginVertical("box");
                
                // ID
                EditorGUILayout.TextField("ID", snake.ArrowData.id.ToString());
                
                // Start Position
                EditorGUILayout.Vector3IntField("Start Position", snake.ArrowData.header);
                
                // Direction
                EditorGUILayout.Vector3IntField("Direction", snake.ArrowData.direction);
                
                // Block Color
                EditorGUILayout.ColorField("Block Color", snake.ArrowData.blockColor);
                
                // Path Length
                EditorGUILayout.IntField("Path Length", snake.ArrowData.pathLength);

                // Custom Path
                if (snake.ArrowData.customPath != null)
                {
                    showCustomPath = EditorGUILayout.Foldout(showCustomPath, $"Custom Path (Count: {snake.ArrowData.customPath.Count})");
                    if (showCustomPath)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < snake.ArrowData.customPath.Count; i++)
                        {
                            EditorGUILayout.Vector3IntField($"Point {i}", snake.ArrowData.customPath[i]);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Custom Path", "Null");
                }
                
                EditorGUILayout.EndVertical();
                
                if (GUI.changed)
                {
                    EditorUtility.SetDirty(snake);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("ArrowData is currently null. It might be set during initialization or loading.", MessageType.Info);
            }
        }

    }
