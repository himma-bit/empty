#if !EMBEDDED
#if UNITY_2020_2_OR_NEWER
using SCRIPTED_IMPORTER = UnityEditor.AssetImporters.ScriptedImporter;
using SCRIPTED_IMPORTER_ATTRIBUTE = UnityEditor.AssetImporters.ScriptedImporterAttribute;
using ASSET_IMPORT_CONTEXT = UnityEditor.AssetImporters.AssetImportContext;
#else
using ASSET_IMPORT_CONTEXT = UnityEditor.Experimental.AssetImporters.AssetImportContext;
using SCRIPTED_IMPORTER = UnityEditor.Experimental.AssetImporters.ScriptedImporter;
using SCRIPTED_IMPORTER_ATTRIBUTE = UnityEditor.Experimental.AssetImporters.ScriptedImporterAttribute;
#endif
using UnityEngine;

namespace VisualDesignCafe.Nature.Editor.Importers
{
    /// <summary>
    /// The base class for the Scripted Importer was changed in Unity 2020.2
    /// This causes an error during import because Unity's API Updater can
    /// not correctly change the base class in an assembly.
    /// The updated assembly runs correctly, but errors are shown in the console
    /// and the assets do not import during the first import pass.
    /// So, this wrapper class is used for the Scripted Importer and then
    /// the NatureAssetImporter is created to actually import the asset.
    /// </summary>
    [SCRIPTED_IMPORTER_ATTRIBUTE( 3, "nature", 100 )]
    public class NatureAssetImporterWrapper : SCRIPTED_IMPORTER, ISerializationCallbackReceiver
    {
        public NatureAssetImporter Importer
        {
            get
            {
                if( _importer == null )
                {
                    if( _importSettings == null )
                        _importSettings = new NatureAssetImportSettings();
                    
                    _importer = new NatureAssetImporter( this, _importSettings );
                }

                return _importer;
            }
        }

        private NatureAssetImporter _importer;

        #region Obsolete

        [SerializeField]
        private GameObject _source;

        [SerializeField]
        private MeshFormat _format;

        [SerializeField]
        private MaterialImportSettings _materialImportSettings;

        [SerializeField]
        private TextureImportSettings _textureImportSettings;

        [SerializeField]
        private MeshImportSettings _meshImportSettings;

        #endregion

        [SerializeField]
        private NatureAssetImportSettings _importSettings;

        public override void OnImportAsset( ASSET_IMPORT_CONTEXT c )
        {
            ConvertOldSerializedData();

            var context = new ShaderX.Editor.AssetImportContext( c.assetPath );
            Importer.OnImportAsset( this, context );

            foreach( var obj in context.Objects )
                c.AddObjectToAsset( obj.Identifier, obj.Object, obj.Icon );

            c.SetMainObject( context.MainObject );

            foreach( var path in context.Dependencies )
                c.DependsOnSourceAsset( path );
        }

        private void ConvertOldSerializedData()
        {
            if( _source != null )
            {
                _importSettings.Source = _source;
                _importSettings.Format = _format;
                _importSettings.MaterialSettings = _materialImportSettings;
                _importSettings.TextureSettings = _textureImportSettings;
                _importSettings.MeshSettings = _meshImportSettings;

                _source = null;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            ConvertOldSerializedData();
        }
    }
}
#endif