using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MMCharacterController))]
public class ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw normal inspector fields
        DrawDefaultInspector();

        GUILayout.Space(10);

        MMCharacterController controller = (MMCharacterController)target;

        if (GUILayout.Button("Bake Database"))
        {
            controller.BakeDatabase();
        }
    }
}
