#if !EMBEDDED
using UnityEditor;

#if UNITY_2020_2_OR_NEWER
using SCRIPTED_IMPORTER_EDITOR = UnityEditor.AssetImporters.ScriptedImporterEditor;
#else
using SCRIPTED_IMPORTER_EDITOR = UnityEditor.Experimental.AssetImporters.ScriptedImporterEditor;
#endif

namespace VisualDesignCafe.Nature.Editor.Importers
{
    /// <summary>
    /// The base class for the Scripted Importer Editor was changed in Unity 2020.2
    /// This causes an error during import because Unity's API Updater can
    /// not correctly change the base class in an assembly.
    /// The updated assembly runs correctly, but errors are shown in the console
    /// and the shaders do not import during the first import pass.
    /// So, this wrapper class is used for the Scripted Importer Editor and then
    /// the NatureAssetImporterEditor is created to actually draw the editor.
    /// </summary>
    [CustomEditor( typeof( NatureAssetImporterWrapper ) )]
    [CanEditMultipleObjects]
    public class NatureAssetImporterEditorWrapper : SCRIPTED_IMPORTER_EDITOR
    {

#if UNITY_2019_2_OR_NEWER
        protected override bool needsApplyRevert => true;
#endif

        public override bool showImportedObject => false;

        private NatureAssetEditor _editor;

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            CreateEditor();
            _editor.OnInspectorGUI();
            ApplyRevertGUI();
            if( _editor.ApplyAndImport )
                ApplyAndImport();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            CreateEditor();
            _editor.OnEnable();
        }

        public override void OnDisable()
        {
            CreateEditor();
            _editor.OnDisable();
            base.OnDisable();
        }

        private void CreateEditor()
        {
            if( _editor == null )
                _editor = 
                    new NatureAssetEditor(
                        this, 
                        ((NatureAssetImporterWrapper) target).Importer );
        }
    }
}
#endif