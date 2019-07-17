using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetStudio;
using static AssetStudioCmd.Exporter;
using Object = AssetStudio.Object;

namespace AssetStudioCmd
{
    class Program
    {
        public delegate void ProcessDelegate(string src, string dst);
        static void Main(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && args[0].Contains("help")))
            {
                System.Console.WriteLine("AssetStudioCmd By Amarillys");
                System.Console.WriteLine("Based on AssetStudio");
                System.Console.WriteLine("AssetStudioCmd [unity3d file] [output destination] [options]");
                System.Console.ReadKey();
            }
            if (args.Length > 0 && !args[0].Contains("help"))
            {
                exportAsset(args);
            }
        }

        static void exportAsset(string[] args)
        {
            var assets = getAssetsFile(args[0]);
            if (assets.Count == 0)
            {
                return;
            }
            var asset = assets[0];
            var dstPath = args.Length > 1 ? args[1] : "";
            if (dstPath == ".")
            {
                dstPath = asset.Asset.assetsFile.originalPath.Substring(0, asset.Asset.assetsFile.originalPath.LastIndexOf('/') + 1);
            }
            var format  = args.Length > 2 ? args[2] : "original";
            var export  = new Dictionary<ClassIDType, Func<bool>>
            {
                { ClassIDType.Texture2D,     () => ExportTexture2D(asset, dstPath, format) },
                { ClassIDType.AudioClip,     () => ExportAudioClip(asset, dstPath, format) },
                { ClassIDType.TextAsset,     () => ExportTextAsset(asset, dstPath, format) },
                { ClassIDType.VideoClip,     () => ExportVideoClip(asset, dstPath, format) },
                { ClassIDType.MovieTexture,  () => { return true;/* ExportMovieTexture(asset, dstPath)  */}},
                { ClassIDType.Shader,        () => { return true;/* ExportShader(asset, dstPath, args[2]) */} },
                { ClassIDType.MonoBehaviour, () => { return true;/* ExportMonoBehaviour(asset, dstPath, args[2]) */} },
                { ClassIDType.Font,          () => { return true;/* ExportFont(asset, dstPath, args[2]) */} },
                { ClassIDType.Mesh,          () => { return true;/* ExportMesh(asset, dstPath, args[2]) */} },
                { ClassIDType.AnimationClip, () => { return true;/* ExportAnimationClip(asset, dstPath, args[2]) */} },
                { ClassIDType.Animator,      () => { return true;/* ExportAnimator(asset, dstPath, args[2]) */} },
                { ClassIDType.Sprite,        () => ExportSprite(asset, dstPath, format) },
            };

            export[asset.Type]();
        }

        public static List<AssetItem> getAssetsFile(string file)
        {
            var assetsManager = new AssetsManager();
            assetsManager.LoadFiles(new string[]{ file });
            List<AssetItem> retAssets = new List<AssetItem>();
            var assetsNameHash = new HashSet<string>();
            var progressCount = assetsManager.assetsFileList.Sum(x => x.Objects.Count);
            var tempDic = new Dictionary<Object, AssetItem>();
            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                AssetBundle ab = null;
                foreach (var asset in assetsFile.Objects.Values)
                {
                    var assetItem = new AssetItem(asset);
                    var ignore = false;
                    switch (asset)
                    {
                        case GameObject m_GameObject:
                            assetItem.Text = m_GameObject.m_Name;
                            break;
                        case Texture2D m_Texture2D:
                            if (!string.IsNullOrEmpty(m_Texture2D.m_StreamData?.path))
                                assetItem.FullSize = asset.byteSize + m_Texture2D.m_StreamData.size;
                            assetItem.Text = m_Texture2D.m_Name;
                            break;
                        case AudioClip m_AudioClip:
                            if (!string.IsNullOrEmpty(m_AudioClip.m_Source))
                                assetItem.FullSize = asset.byteSize + m_AudioClip.m_Size;
                            assetItem.Text = m_AudioClip.m_Name;
                            break;
                        case VideoClip m_VideoClip:
                            if (!string.IsNullOrEmpty(m_VideoClip.m_OriginalPath))
                                assetItem.FullSize = asset.byteSize + (long)m_VideoClip.m_Size;
                            assetItem.Text = m_VideoClip.m_Name;
                            break;
                        case Shader m_Shader:
                            assetItem.Text = m_Shader.m_ParsedForm?.m_Name ?? m_Shader.m_Name;
                            break;
                        case Mesh _:
                        case TextAsset _:
                        case AnimationClip _:
                        case Font _:
                        case MovieTexture _:
                        case Sprite _:
                            assetItem.Text = ((NamedObject)asset).m_Name;
                            break;
                        case Animator m_Animator:
                            if (m_Animator.m_GameObject.TryGet(out var gameObject))
                            {
                                assetItem.Text = gameObject.m_Name;
                            }
                            break;
                        case MonoBehaviour m_MonoBehaviour:
                            if (m_MonoBehaviour.m_Name == "" && m_MonoBehaviour.m_Script.TryGet(out var m_Script))
                            {
                                assetItem.Text = m_Script.m_ClassName;
                            }
                            else
                            {
                                assetItem.Text = m_MonoBehaviour.m_Name;
                            }
                            break;
                        case PlayerSettings m_PlayerSettings:
                            // productName = m_PlayerSettings.productName;
                            ignore = true;
                            break;
                        case AssetBundle m_AssetBundle:
                            ab = m_AssetBundle;
                            assetItem.Text = ab.m_Name;
                            ignore = true;
                            break;
                        case SpriteAtlas m_SpriteAtlas:
                            foreach (var m_PackedSprite in m_SpriteAtlas.m_PackedSprites)
                            {
                                if (m_PackedSprite.TryGet(out var m_Sprite))
                                {
                                    if (m_Sprite.m_SpriteAtlas.IsNull())
                                    {
                                        m_Sprite.m_SpriteAtlas.Set(m_SpriteAtlas);
                                    }
                                }
                            }
                            break;
                        case NamedObject m_NamedObject:
                            assetItem.Text = m_NamedObject.m_Name;
                            break;
                    }
                    if (assetItem.Text == "")
                    {
                        assetItem.Text = assetItem.TypeString + assetItem.UniqueID;
                    }
                    //处理同名文件
                    if (!assetsNameHash.Add((assetItem.TypeString + assetItem.Text).ToUpper()))
                    {
                        assetItem.Text += assetItem.UniqueID;
                    }
                    //处理非法文件名
                    assetItem.Text = FixFileName(assetItem.Text);
                    if (!ignore)
                    {
                        retAssets.Add(assetItem);
                    }
                }
            }
            assetsNameHash.Clear();
            return retAssets;
        }

        public static string FixFileName(string str)
        {
            if (str.Length >= 260) return Path.GetRandomFileName();
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }
    }
}
