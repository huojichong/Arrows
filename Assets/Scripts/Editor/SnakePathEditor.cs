using System;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(SnakePath))]
[CanEditMultipleObjects]
public class SnakePathEditor : UnityEditor.Editor
{
    
    SerializedProperty filletRadius;
    
    private void OnEnable()
    {
        filletRadius = serializedObject.FindProperty("filletRadius");
    }

    override public void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SnakePath path = (SnakePath)target;
        path.filletRadius = EditorGUILayout.Slider("Fillet Radius", path.filletRadius, 0, 1);
        if (GUILayout.Button("Update Path"))
        {
            path.UpdateSplineFromWaypoints();
        }
    }
    
}

