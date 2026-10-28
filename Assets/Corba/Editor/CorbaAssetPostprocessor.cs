using UnityEditor;
using UnityEngine;

namespace Corba.EditorTools
{
    public class CorbaAssetPostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/Corba/Resources/Corba/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root + "Models")) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool rigged = file.StartsWith("chef_") || file.StartsWith("u_");
            importer.importAnimation = rigged;
            importer.animationType = rigged ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            if (rigged)
            {
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.optimizeGameObjects = false;
            }
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (assetPath.StartsWith(Root + "UI"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = SpriteBorder(file);
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = file.StartsWith("fx_") || file.StartsWith("ui_");
            importer.wrapMode = file.StartsWith("ground_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.anisoLevel = file.StartsWith("ground_") ? 4 : 1;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        static Vector4 SpriteBorder(string file)
        {
            if (file.StartsWith("ui_panel")) return new Vector4(56, 56, 56, 56);
            if (file.StartsWith("ui_banner")) return new Vector4(90, 50, 90, 50);
            if (file.StartsWith("ui_ribbon")) return new Vector4(200, 40, 60, 40);
            if (file.StartsWith("ui_button")) return new Vector4(40, 30, 40, 30);
            if (file.StartsWith("ui_card")) return new Vector4(30, 30, 30, 30);
            if (file.StartsWith("ui_chip")) return new Vector4(24, 20, 24, 20);
            if (file.StartsWith("ui_field")) return new Vector4(24, 20, 24, 20);
            if (file.StartsWith("ui_bar")) return new Vector4(20, 12, 20, 12);
            return Vector4.zero;
        }
    }
}
