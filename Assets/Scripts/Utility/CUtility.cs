using UnityEditor;
using UnityEngine; 

namespace Assets.Scripts.Utility
{
    public class CUtility
    {
        public static void SetTextureReadble(Texture2D tex, bool bReadble)
        {
            if (tex != null)
            {
                TextureImporterSettings settings = GetTextureImporterSettings(tex);
                if (settings != null)
                {
                    settings.readable = bReadble;
                    SetTextureImporterSettings(tex, settings);
                }
            }
        }


        public static void SetTextureImporterSettings(Texture2D tex, TextureImporterSettings settings)
        {
            if (tex != null
                && settings != null)
            {
                TextureImporter importer = GetTextureImporter(tex);
                if (importer != null)
                {
                    importer.SetTextureSettings(settings);

                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
            }
        }


        public static TextureImporterSettings GetTextureImporterSettings(Texture2D tex)
        {
            TextureImporterSettings settings = null;
            if (tex != null)
            {
                TextureImporter importer = GetTextureImporter(tex);
                if (importer != null)
                {
                    settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                }
            }
            return settings;
        }

        public static TextureImporter GetTextureImporter(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            return AssetImporter.GetAtPath(assetPath) as TextureImporter;
        }
    }
}
