using UnityEditor;

namespace BaitSwitch.EditorTools
{
    /// <summary>
    /// Import rules for the Blender kit: thumbnails become Sprites (PartCatalogBuilder loads them as Sprite),
    /// FBX parts import at file scale with no cameras, lights, animation or colliders.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        const string Thumbs = "Assets/_Game/Art/Thumbs";
        const string Parts = "Assets/_Game/Art/Parts";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Thumbs)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Parts)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.addCollider = false;
            mi.useFileScale = true;
            mi.globalScale = 1f;
            mi.isReadable = false;
            mi.importBlendShapes = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            // smooth the generator's facets (organic fish read hand-sculpted); creases sharper than 60 deg stay crisp
            mi.importNormals = ModelImporterNormals.Calculate;
            mi.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            mi.normalSmoothingAngle = 60f;
        }

        /// <summary>Synthesized clips (tools/audio/synth.py): music and ambience stream, effects decompress on load.</summary>
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio")) return;
            var ai = (AudioImporter)assetImporter;
            string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool longLoop = n.StartsWith("music_") || n.StartsWith("amb_") || n == "tension";
            var st = ai.defaultSampleSettings;
            st.loadType = longLoop ? UnityEngine.AudioClipLoadType.Streaming : UnityEngine.AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
            st.quality = longLoop ? 0.6f : 0.8f;
            ai.defaultSampleSettings = st;
            ai.forceToMono = true;
            ai.loadInBackground = longLoop;
        }
    }
}
