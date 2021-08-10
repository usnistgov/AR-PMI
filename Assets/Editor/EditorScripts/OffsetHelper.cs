
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AutoOffset))]
public class OffsetHelper : Editor
{
 public override void OnInspectorGUI(){
    base.OnInspectorGUI();
//TODO: Create an editor button that edits the boolean value in the AutoOffset script.

  //   if (GUILayout.Button("Center Origin", EditorStyles.miniButton)){
    
  //     serializedObject.FindProperty("centerOrigin").boolValue = true;
  //     Debug.Log(serializedObject.FindProperty("centerOrigin").boolValue);
  //  }
 
 }
 
}
