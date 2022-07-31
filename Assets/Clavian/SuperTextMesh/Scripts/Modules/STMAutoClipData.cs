//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Auto Clip Data", menuName = "Super Text Mesh/Audo Clip Data", order = 1)]
public class STMAutoClipData : ScriptableObject{ //for auto-clips. replacing text sounds
	#if UNITY_EDITOR
	public bool showFoldout = true;
	#endif
	//[TextArea(2,3)]
	//public string character;
	//public bool ignoreCase = true; //lets just make it always ignore case
	public AudioClip clip;

	#if UNITY_EDITOR
	public void DrawCustomInspector(SuperTextMesh stm){
		Undo.RecordObject(this, "Edited STM Auto Clip Data");
		var serializedData = new SerializedObject(this);
		serializedData.Update();
	//gather parts for this data:
		SerializedProperty clip = serializedData.FindProperty("clip");
		//SerializedProperty ignoreCase = serializedData.FindProperty("ignoreCase");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm);
	//the rest:
		EditorGUILayout.PropertyField(clip);
		//EditorGUILayout.PropertyField(ignoreCase);
		EditorGUILayout.Space(); //////////////////SPACE
		if(this != null)serializedData.ApplyModifiedProperties(); //since break; cant be called
	}
	#endif
}