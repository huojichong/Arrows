
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    [CustomEditor(typeof(SplineExtrudeSnake))]
    public class SplineExtrudeSnakeEditor : UnityEditor.Editor
    {
        private bool showCustomPath = false;

        private void OnEnable()
        {
            
        }

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
                snake.ArrowData.id = EditorGUILayout.TextField("ID", snake.ArrowData.id);
                
                // Start Position
                snake.ArrowData.startPosition = EditorGUILayout.Vector3Field("Start Position", snake.ArrowData.startPosition);
                
                // Direction
                snake.ArrowData.direction = EditorGUILayout.Vector2IntField("Direction", snake.ArrowData.direction);
                
                // Block Color
                snake.ArrowData.blockColor = EditorGUILayout.ColorField("Block Color", snake.ArrowData.blockColor);
                
                // Path Length
                snake.ArrowData.pathLength = EditorGUILayout.IntField("Path Length", snake.ArrowData.pathLength);

                // Custom Path
                if (snake.ArrowData.customPath != null)
                {
                    showCustomPath = EditorGUILayout.Foldout(showCustomPath, $"Custom Path (Count: {snake.ArrowData.customPath.Count})");
                    if (showCustomPath)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < snake.ArrowData.customPath.Count; i++)
                        {
                            snake.ArrowData.customPath[i] = EditorGUILayout.Vector3Field($"Point {i}", snake.ArrowData.customPath[i]);
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
