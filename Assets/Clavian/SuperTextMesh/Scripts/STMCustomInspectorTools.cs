//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq; //for checking keywords array

static class STMCustomInspectorTools {
	public static void DrawTitleBar(UnityEngine.Object myObject, SuperTextMesh stm){
		if(myObject != null){
			EditorGUILayout.BeginHorizontal();
		//ping button:
			if(GUILayout.Button("Ping")){
				//EditorUtility.FocusProjectWindow(); this doesn't work for some reason
				EditorGUIUtility.PingObject(myObject); //select this object
			}
		//name:
			EditorGUI.BeginChangeCheck();
			myObject.name = EditorGUILayout.DelayedTextField(myObject.name);
			if(EditorGUI.EndChangeCheck()){
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(myObject), myObject.name);
				//Undo.RecordObject (myObject, "Change Asset Name");
				AssetDatabase.Refresh();
				stm.data = null;
			}
		//delete button:
			if(GUILayout.Button("X")){
				//AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(myObject));
				AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(myObject));
				//Undo.DestroyObjectImmediate(myObject);
				AssetDatabase.Refresh();
				stm.data = null; //make this refresh, too
			}
			EditorGUILayout.EndHorizontal();
		}
	}
	public static string ClavianPath
    {
        get
        {
            string searchValue = "Clavian/SuperTextMesh/";
            string returnPath = "";
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allPaths.Length; i++)
            {
                if (allPaths[i].Contains(searchValue))
                {
                    // This is the path we want! Let's strip out everything after the searchValue
                    returnPath = allPaths[i];
                    returnPath = returnPath.Remove(returnPath.IndexOf(searchValue));
                    returnPath += searchValue;
					break;
                }
            }

            return returnPath;
        }
    }
	/*
	public static void OnUndoRedo(){
		AssetDatabase.Refresh();
	}
	*/
	public static void FinishItem(UnityEngine.Object myObject){

	}
	public static void DrawCreateFolderButton(string buttonText, string parentFolder, string newFolder, SuperTextMesh stm){
		if(GUILayout.Button(buttonText)){
			AssetDatabase.CreateFolder(ClavianPath + "Resources/" + parentFolder, newFolder);
			AssetDatabase.Refresh();
			stm.data = null;
		}
	}
	public static void DrawCreateNewButton(string buttonText, string folderName, string typeName, SuperTextMesh stm){
		if(GUILayout.Button(buttonText)){
			ScriptableObject newData = NewData(typeName);
			if(newData != null){
				AssetDatabase.CreateAsset(newData,AssetDatabase.GenerateUniqueAssetPath(ClavianPath + "Resources/" + folderName)); //save to file
				//Undo.undoRedoPerformed += OnUndoRedo; //subscribe to event
				//Undo.RegisterCreatedObjectUndo(newData, buttonText);
				AssetDatabase.Refresh();
				stm.data = null;
			}
		}
	}
	public static ScriptableObject NewData(string myType){
		switch(myType){
			case "STMAudioClipData": return ScriptableObject.CreateInstance<STMAudioClipData>();
			case "STMAutoClipData": return ScriptableObject.CreateInstance<STMAutoClipData>();
			case "STMColorData": return ScriptableObject.CreateInstance<STMColorData>();
			case "STMDelayData": return ScriptableObject.CreateInstance<STMDelayData>();
			case "STMDrawAnimData": return ScriptableObject.CreateInstance<STMDrawAnimData>();
			case "STMFontData": return ScriptableObject.CreateInstance<STMFontData>();
			case "STMGradientData": return ScriptableObject.CreateInstance<STMGradientData>();
			case "STMJitterData": return ScriptableObject.CreateInstance<STMJitterData>();
			case "STMMaterialData": return ScriptableObject.CreateInstance<STMMaterialData>();
			case "STMQuadData": return ScriptableObject.CreateInstance<STMQuadData>();
			case "STMSoundClipData": return ScriptableObject.CreateInstance<STMSoundClipData>();
			case "STMTextureData": return ScriptableObject.CreateInstance<STMTextureData>();
			case "STMVoiceData": return ScriptableObject.CreateInstance<STMVoiceData>();
			case "STMWaveData": return ScriptableObject.CreateInstance<STMWaveData>();
			default: Debug.Log("New data type unknown."); return null;
		}
	}
	public static void DrawMaterialEditor(Material mat){
		//Just set these directly, why not. It's a custom inspector already, no need to bog this down even more
		Undo.RecordObject(mat, "Changed Super Text Mesh Material");
		//name changer
		EditorGUI.BeginChangeCheck();
		mat.name = EditorGUILayout.TextField("Material Name", mat.name);
		if(EditorGUI.EndChangeCheck()){
			AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(mat), mat.name);
			//Undo.RecordObject (myObject, "Change Asset Name");
			AssetDatabase.Refresh();
			//stm.data = null;
		}

		int originalQueue = mat.renderQueue;
		mat.shader = (Shader)EditorGUILayout.ObjectField("Shader", mat.shader, typeof(Shader), false);
		
		//set to correct value
		if(mat.HasProperty("_Cutoff")){
			mat.SetFloat("_Cutoff",0.0001f);
		}
		//set to correct value
		if(mat.HasProperty("_ShadowCutoff")){
			mat.SetFloat("_ShadowCutoff",0.5f);
		}

		//culling mode
		if(mat.HasProperty("_CullMode")){
			UnityEngine.Rendering.CullMode cullMode = (UnityEngine.Rendering.CullMode)mat.GetInt("_CullMode");
			cullMode = (UnityEngine.Rendering.CullMode)EditorGUILayout.EnumPopup("Cull Mode", cullMode);
			mat.SetInt("_CullMode", (int)cullMode);
		}
		//draw on top?
		if(mat.HasProperty("_ZTestMode")){
			int zTestMode = mat.GetInt("_ZTestMode");
			bool onTop = zTestMode == 6;
			onTop = EditorGUILayout.Toggle("Render On Top", onTop);
			//Always or LEqual
			mat.SetInt("_ZTestMode", onTop ? 6 : 2);
		}
		/* 
		//masking
		if(mat.HasProperty("_MaskMode")){
			int maskMode = mat.GetInt("_MaskMode");
			//bool masked = maskMode == 1;
			//masked = EditorGUILayout.Toggle("Masked", masked);
			maskMode = EditorGUILayout.Popup("Mask Mode", maskMode, new string[]{"Outside","Inside"});
			//Always or LEqual
			mat.SetInt("_MaskMode", maskMode);
		}
		*/

		//if this is the multishader
		if(mat.GetTag("STMUberShader", true, "Null") == "Yes")
		{

		//toggle SDF
			bool sdfMode = mat.IsKeywordEnabled("SDF_MODE");
			EditorGUI.BeginChangeCheck();
			sdfMode = EditorGUILayout.Toggle("SDF Mode", sdfMode);//show the toggle
			if(EditorGUI.EndChangeCheck())
			{
				mat.SetFloat("_SDFMode", sdfMode ? 1 : 0); //call this too so newer unity versions don't break
				if(sdfMode)
				{
					mat.EnableKeyword("SDF_MODE");
				}
				else
				{
					mat.DisableKeyword("SDF_MODE");
				}
				//EditorUtility.SetDirty(mat);
			}
			//#endif

			if(sdfMode)
			{//draw SDF-related properties
				if(mat.HasProperty("_Blend")){
					//EditorGUILayout.PropertyField(shaderBlend);
					mat.SetFloat("_Blend",EditorGUILayout.Slider("Blend",mat.GetFloat("_Blend"),0.0001f,1f));
				}
				if(mat.HasProperty("_SDFCutoff")){
					mat.SetFloat("_SDFCutoff",EditorGUILayout.Slider("SDF Cutoff",mat.GetFloat("_SDFCutoff"),0f,1f));
				}
			}
			
			//toggle Pixel Snap
			bool pixelSnap = mat.IsKeywordEnabled("PIXELSNAP_ON");
			EditorGUI.BeginChangeCheck();
			pixelSnap = EditorGUILayout.Toggle("Pixel Snap", pixelSnap);//show the toggle
			if(EditorGUI.EndChangeCheck())
			{
				mat.SetFloat("PixelSnap", pixelSnap ? 1 : 0); //call this too so newer unity versions don't break
				if(pixelSnap)
				{
					mat.EnableKeyword("PIXELSNAP_ON");
				}
				else
				{
					mat.DisableKeyword("PIXELSNAP_ON");
				}
				//EditorUtility.SetDirty(mat);
			}
			//#endif
		}
		else{
			if(mat.HasProperty("_Blend")){
				//EditorGUILayout.PropertyField(shaderBlend);
				mat.SetFloat("_Blend",EditorGUILayout.Slider("Blend",mat.GetFloat("_Blend"),0f,1f));
			}
			if(mat.HasProperty("_SDFCutoff")){
				mat.SetFloat("_SDFCutoff",EditorGUILayout.Slider("SDF Cutoff",mat.GetFloat("_SDFCutoff"),0f,1f));
			}
		}

		if(mat.HasProperty("_ShadowColor")){
			//EditorGUILayout.PropertyField(shadowColor);
			mat.SetColor("_ShadowColor",EditorGUILayout.ColorField("Shadow Color",mat.GetColor("_ShadowColor")));
		}
		if(mat.HasProperty("_ShadowDistance")){
			//EditorGUILayout.PropertyField(shadowDistance);
			mat.SetFloat("_ShadowDistance",EditorGUILayout.FloatField("Shadow Distance",mat.GetFloat("_ShadowDistance")));
		}

		if(mat.GetTag("STMUberShader", true, "Null") == "Yes" && mat.HasProperty("_Vector3Dropshadow"))
		{
			//toggle use vector 3
			bool useVector3 = mat.IsKeywordEnabled("VECTOR3_DROPSHADOW");
			EditorGUI.BeginChangeCheck();
			useVector3 = EditorGUILayout.Toggle("Vector3 Dropshadow", useVector3);//show the toggle
			if(EditorGUI.EndChangeCheck())
			{
				mat.SetFloat("_Vector3Dropshadow", useVector3 ? 1 : 0); //call this too so newer unity versions don't break
				if(useVector3)
				{
					mat.EnableKeyword("VECTOR3_DROPSHADOW");
				}
				else
				{
					mat.DisableKeyword("VECTOR3_DROPSHADOW");
				}
				//EditorUtility.SetDirty(mat);
			}
			if(useVector3 && mat.HasProperty("_ShadowAngle3"))
			{
				mat.SetVector("_ShadowAngle3",EditorGUILayout.Vector3Field("Shadow Angle3", mat.GetVector("_ShadowAngle3")));
			}
			else
			{
				//same as before
				if(mat.HasProperty("_ShadowAngle")){
					//EditorGUILayout.PropertyField(shadowAngle);
					mat.SetFloat("_ShadowAngle",EditorGUILayout.Slider("Shadow Angle",mat.GetFloat("_ShadowAngle"),0f,360f));
				}
			}
		}
		else
		{
			if(mat.HasProperty("_ShadowAngle")){
				//EditorGUILayout.PropertyField(shadowAngle);
				mat.SetFloat("_ShadowAngle",EditorGUILayout.Slider("Shadow Angle",mat.GetFloat("_ShadowAngle"),0f,360f));
			}
		}
		if(mat.HasProperty("_OutlineColor")){
			//EditorGUILayout.PropertyField(outlineColor);
			mat.SetColor("_OutlineColor",EditorGUILayout.ColorField("Outline Color",mat.GetColor("_OutlineColor")));
		}
		if(mat.HasProperty("_OutlineWidth")){
			//EditorGUILayout.PropertyField(outlineWidth);
			mat.SetFloat("_OutlineWidth",EditorGUILayout.FloatField("Outline Width",mat.GetFloat("_OutlineWidth")));
		}
		if(mat.HasProperty("_SquareOutline"))
		{
			bool squareOutline = mat.IsKeywordEnabled("SQUARE_OUTLINE");
			EditorGUI.BeginChangeCheck();
			squareOutline = EditorGUILayout.Toggle("Square Outline", squareOutline);//show the toggle
			if(EditorGUI.EndChangeCheck())
			{
				mat.SetFloat("_SquareOutline", squareOutline ? 1 : 0);
				if(squareOutline)
				{
					mat.EnableKeyword("SQUARE_OUTLINE");
				}
				else
				{
					mat.DisableKeyword("SQUARE_OUTLINE");
				}
			}
		}

		EditorGUILayout.BeginHorizontal();
		mat.renderQueue = EditorGUILayout.IntField("Render Queue", originalQueue);
		if(GUILayout.Button("Reset"))
		{
			mat.renderQueue = mat.shader.renderQueue;
		}
		EditorGUILayout.EndHorizontal();
	}
}
#endif
