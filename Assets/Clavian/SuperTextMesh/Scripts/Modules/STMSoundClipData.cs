//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Sound Clip Data", menuName = "Super Text Mesh/Sound Clip Data", order = 1)]
public class STMSoundClipData : ScriptableObject{ //for auto-clips. replacing text sounds
	#if UNITY_EDITOR
	public bool showFoldout = true;
	#endif
	//[TextArea(2,3)]
	//public string character;
	[System.Serializable]
	public class AutoClip{ //the same as an autoclip
		public string name;
		//public bool ignoreCase;
		public AudioClip clip;
	}
	public List<AutoClip> clips = new List<AutoClip>();

	#if UNITY_EDITOR
	public void DrawCustomInspector(SuperTextMesh stm){
		Undo.RecordObject(this, "Edited STM Sound Clip Data");
		var serializedData = new SerializedObject(this);
		serializedData.Update();
	//gather parts for this data:
		SerializedProperty clips = serializedData.FindProperty("clips");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm);
	//the rest:
		EditorGUILayout.PropertyField(clips, true);
		if(this != null)serializedData.ApplyModifiedProperties(); //since break; cant be called
	}
	#endif
}