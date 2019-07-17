﻿using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using AssetStudio;

namespace AssetStudioCmd
{
    internal static class Exporter
    {
        public static bool ExportTexture2D(AssetItem item, string exportPath, string format)
        {
            var converter = new Texture2DConverter((Texture2D)item.Asset);
            ImageFormat dstFormat = ImageFormat.Png;
            if (format == "recommanded" || format == "png")
            {
                format = "png";
                dstFormat = ImageFormat.Png;
            }
            var oriPath = item.Asset.assetsFile.originalPath;
            var exportFullName = exportPath + Path.GetFileNameWithoutExtension(oriPath) + "." + format;
            if (format != "original")
            {
                var bitmap = converter.ConvertToBitmap(true);
                if (bitmap == null)
                    return false;
                if (ExportFileExists(exportFullName))
                    return false;
                bitmap.Save(exportFullName, dstFormat);
                bitmap.Dispose();
                return true;
            }
            else
            {
                File.WriteAllBytes(exportFullName, converter.ConvertToContainer());
                return true;
            }
        }

        public static bool ExportAudioClip(AssetItem item, string exportPath, string format)
        {
            var m_AudioClip = (AudioClip)item.Asset;
            var m_AudioData = m_AudioClip.m_AudioData.Value;
            if (m_AudioData == null || m_AudioData.Length == 0)
                return false;
            var converter = new AudioClipConverter(m_AudioClip);
            if (format == "recommanded" || format == "wav")
            {
                format = "wav";
            }
            var oriPath = item.Asset.assetsFile.originalPath;
            var exportFullName = exportPath + Path.GetFileNameWithoutExtension(oriPath) + "." + format;
            if (format == "wav" && converter.IsFMODSupport)
            {
                if (ExportFileExists(exportFullName))
                    return false;
                var buffer = converter.ConvertToWav();
                if (buffer == null)
                    return false;
                File.WriteAllBytes(exportFullName, buffer);
            }
            else
            {
                File.WriteAllBytes(exportFullName, m_AudioData);
            }
            return true;
        }

        public static bool ExportShader(AssetItem item, string exportPath)
        {
            var exportFullName = exportPath + item.Text + ".shader";
            if (ExportFileExists(exportFullName))
                return false;
            var m_Shader = (Shader)item.Asset;
            if (m_Shader.compressedBlob != null) //5.5 and up
            {
                var strs = ShaderConverter.ConvertMultiple(m_Shader);
                for (int i = 0; i < strs.Length; i++)
                {
                    var platformName = ShaderConverter.GetPlatformString(m_Shader.platforms[i]);
                    File.WriteAllText($"{exportPath}{item.Text}_{platformName}.shader", strs[i]);
                }
            }
            else
            {
                var str = ShaderConverter.Convert(m_Shader);
                File.WriteAllText(exportFullName, str);
            }
            return true;
        }

        public static bool ExportTextAsset(AssetItem item, string exportPath, string format)
        {
            if (format == "recommanded" || format == "txt")
            {
                format = "txt";
            }
            var oriPath = item.Asset.assetsFile.originalPath;
            var exportFullName = exportPath + Path.GetFileNameWithoutExtension(oriPath) + "." + format;
            var m_TextAsset = (TextAsset)(item.Asset);
            File.WriteAllBytes(exportFullName, m_TextAsset.m_Script);
            return true;
        }

        /*
        public static bool ExportMonoBehaviour(AssetItem item, string exportPath)
        {
            var exportFullName = exportPath + item.Text + ".txt";
            if (ExportFileExists(exportFullName))
                return false;
            var m_MonoBehaviour = (MonoBehaviour)item.Asset;
            var str = m_MonoBehaviour.Dump() ?? Studio.GetScriptString(item.Asset.reader);
            File.WriteAllText(exportFullName, str);
            return true;
        }
        */

        public static bool ExportFont(AssetItem item, string exportPath)
        {
            var m_Font = (Font)item.Asset;
            if (m_Font.m_FontData != null)
            {
                var extension = ".ttf";
                if (m_Font.m_FontData[0] == 79 && m_Font.m_FontData[1] == 84 && m_Font.m_FontData[2] == 84 && m_Font.m_FontData[3] == 79)
                {
                    extension = ".otf";
                }
                var exportFullName = exportPath + item.Text + extension;
                if (ExportFileExists(exportFullName))
                    return false;
                File.WriteAllBytes(exportFullName, m_Font.m_FontData);
                return true;
            }
            return false;
        }

        public static bool ExportMesh(AssetItem item, string exportPath)
        {
            var m_Mesh = (Mesh)item.Asset;
            if (m_Mesh.m_VertexCount <= 0)
                return false;
            var exportFullName = exportPath + item.Text + ".obj";
            if (ExportFileExists(exportFullName))
                return false;
            var sb = new StringBuilder();
            sb.AppendLine("g " + m_Mesh.m_Name);
            #region Vertices
            if (m_Mesh.m_Vertices == null || m_Mesh.m_Vertices.Length == 0)
            {
                return false;
            }
            int c = 3;
            if (m_Mesh.m_Vertices.Length == m_Mesh.m_VertexCount * 4)
            {
                c = 4;
            }
            for (int v = 0; v < m_Mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("v {0} {1} {2}\r\n", -m_Mesh.m_Vertices[v * c], m_Mesh.m_Vertices[v * c + 1], m_Mesh.m_Vertices[v * c + 2]);
            }
            #endregion

            #region UV
            if (m_Mesh.m_UV0 != null && m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 2)
            {
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vt {0} {1}\r\n", m_Mesh.m_UV0[v * 2], m_Mesh.m_UV0[v * 2 + 1]);
                }
            }
            else if (m_Mesh.m_UV1 != null && m_Mesh.m_UV1.Length == m_Mesh.m_VertexCount * 2)
            {
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vt {0} {1}\r\n", m_Mesh.m_UV1[v * 2], m_Mesh.m_UV1[v * 2 + 1]);
                }
            }
            #endregion

            #region Normals
            if (m_Mesh.m_Normals != null && m_Mesh.m_Normals.Length > 0)
            {
                if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                else if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 4)
                {
                    c = 4;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vn {0} {1} {2}\r\n", -m_Mesh.m_Normals[v * c], m_Mesh.m_Normals[v * c + 1], m_Mesh.m_Normals[v * c + 2]);
                }
            }
            #endregion

            #region Face
            int sum = 0;
            for (var i = 0; i < m_Mesh.m_SubMeshes.Length; i++)
            {
                sb.AppendLine($"g {m_Mesh.m_Name}_{i}");
                int indexCount = (int)m_Mesh.m_SubMeshes[i].indexCount;
                var end = sum + indexCount / 3;
                for (int f = sum; f < end; f++)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n", m_Mesh.m_Indices[f * 3 + 2] + 1, m_Mesh.m_Indices[f * 3 + 1] + 1, m_Mesh.m_Indices[f * 3] + 1);
                }
                sum = end;
            }
            #endregion

            sb.Replace("NaN", "0");
            File.WriteAllText(exportFullName, sb.ToString());
            return true;
        }

        public static bool ExportVideoClip(AssetItem item, string exportPath, string format)
        {
            if (format == "recommanded" || format == "mp4")
            {
                format = "mp4";
            }
            var oriPath = item.Asset.assetsFile.originalPath;
            var exportFullName = exportPath + Path.GetFileNameWithoutExtension(oriPath) + "." + format;
            var m_TextAsset = (TextAsset)(item.Asset);
            var m_VideoClip = (VideoClip)item.Asset;
            var m_VideoData = m_VideoClip.m_VideoData.Value;
            if (m_VideoData != null && m_VideoData.Length != 0)
            {
                File.WriteAllBytes(exportFullName, m_VideoData);
                return true;
            }
            return false;
        }

        public static bool ExportMovieTexture(AssetItem item, string exportPath)
        {
            var m_MovieTexture = (MovieTexture)item.Asset;
            var exportFullName = exportPath + item.Text + ".ogv";
            if (ExportFileExists(exportFullName))
                return false;
            File.WriteAllBytes(exportFullName, m_MovieTexture.m_MovieData);
            return true;
        }

        public static bool ExportSprite(AssetItem item, string exportPath, string format)
        {
            ImageFormat dstFormat = null;
            if (format == "recommanded")
            {
                format = "png";
            }
            var oriPath = item.Asset.assetsFile.originalPath;
            var exportFullName = exportPath + Path.GetFileNameWithoutExtension(oriPath) + "." + format;
            var m_TextAsset = (TextAsset)(item.Asset);
            switch (format)
            {
                case "bmp":
                    dstFormat = ImageFormat.Bmp;
                    break;
                case "png":
                    dstFormat = ImageFormat.Png;
                    break;
                case "jpg":
                    dstFormat = ImageFormat.Jpeg;
                    break;
            }
            if (ExportFileExists(exportFullName))
                return false;
            var bitmap = SpriteHelper.GetImageFromSprite((Sprite)item.Asset);
            if (bitmap != null)
            {
                bitmap.Save(exportFullName, dstFormat);
                bitmap.Dispose();
                return true;
            }
            return false;
        }

        public static bool ExportRawFile(AssetItem item, string exportPath)
        {
            var exportFullName = exportPath + item.Text + ".dat";
            if (ExportFileExists(exportFullName))
                return false;
            File.WriteAllBytes(exportFullName, item.Asset.GetRawData());
            return true;
        }

        private static bool ExportFileExists(string filename)
        {
            if (File.Exists(filename))
            {
                return true;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            return false;
        }

        /*
        public static bool ExportAnimator(AssetItem item, string exportPath, List<AssetItem> animationList = null)
        {
            var m_Animator = (Animator)item.Asset;
            var convert = animationList != null ? new ModelConverter(m_Animator, animationList.Select(x => (AnimationClip)x.Asset).ToArray()) : new ModelConverter(m_Animator);
            exportPath = $"{exportPath}{item.Text}\\{item.Text}.fbx";
            return ExportFbx(convert, exportPath);
        }

        public static bool ExportGameObject(GameObject gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var convert = animationList != null ? new ModelConverter(gameObject, animationList.Select(x => (AnimationClip)x.Asset).ToArray()) : new ModelConverter(gameObject);
            exportPath = exportPath + Studio.FixFileName(gameObject.m_Name) + ".fbx";
            return ExportFbx(convert, exportPath);
        }

        private static bool ExportFbx(IImported convert, string exportPath)
        {
            var eulerFilter = (bool)Properties.Settings.Default["eulerFilter"];
            var filterPrecision = (float)(decimal)Properties.Settings.Default["filterPrecision"];
            var allFrames = (bool)Properties.Settings.Default["allFrames"];
            var allBones = (bool)Properties.Settings.Default["allBones"];
            var skins = (bool)Properties.Settings.Default["skins"];
            var boneSize = (int)(decimal)Properties.Settings.Default["boneSize"];
            var scaleFactor = (float)(decimal)Properties.Settings.Default["scaleFactor"];
            var fbxVersion = (int)Properties.Settings.Default["fbxVersion"];
            var fbxFormat = (int)Properties.Settings.Default["fbxFormat"];
            ModelExporter.ExportFbx(exportPath, convert, eulerFilter, filterPrecision, allFrames, allBones, skins, boneSize, scaleFactor, fbxVersion, fbxFormat == 1);
            return true;
        }
        */
    }
}
