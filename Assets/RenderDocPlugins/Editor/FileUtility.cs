using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RenderDocPlugins
{
    public static class FileUtility
    {
        /// <summary>
        /// path is system path style
        /// last charact is '/'
        /// </summary>
        private static string s_ProjectSysPath = string.Empty;
        private static string ProjectSysPath
        {
            get
            {
                if (s_ProjectSysPath == string.Empty)
                {
                    s_ProjectSysPath = System.IO.Directory.GetParent(Application.dataPath).FullName;
                    if (s_ProjectSysPath[s_ProjectSysPath.Length - 1] != System.IO.Path.DirectorySeparatorChar)
                    {
                        s_ProjectSysPath += System.IO.Path.DirectorySeparatorChar;
                    }
                }
                return s_ProjectSysPath;
            }
        }

        public static bool IsAssetPath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }

        private static string UnityToSysPathStyle(string sysPath)
        {
            sysPath = sysPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            return sysPath;
        }

        private static string SysToUnityPathStyle(string unityPath)
        {
            if (System.IO.Path.DirectorySeparatorChar != '/')
            {
                unityPath = unityPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
            }
            else if (System.IO.Path.AltDirectorySeparatorChar != '/')
            {
                unityPath = unityPath.Replace(System.IO.Path.AltDirectorySeparatorChar, '/');
            }
            return unityPath;
        }

        public static bool TryFullSysPathToAssetPath(string sysPath, out string assetPath)
        {
            sysPath = UnityToSysPathStyle(sysPath);
            assetPath = string.Empty;
            if (sysPath.StartsWith(ProjectSysPath, System.StringComparison.OrdinalIgnoreCase))
            {
                assetPath = assetPath.Substring(ProjectSysPath.Length);
                assetPath = SysToUnityPathStyle(assetPath);
                return true;
            }
            return false;
        }

        public static string AssetPathToFullSysPath(string assetPath)
        {
            assetPath = UnityToSysPathStyle(assetPath);
            var sysPath = System.IO.Path.Combine(ProjectSysPath, assetPath);
            return sysPath;
        }

        public static bool TryAnyPathToFullSysPath(string assetOrSysPath, out string fullSysPath)
        {
            if (!string.IsNullOrEmpty(assetOrSysPath))
            {
                if (System.IO.Path.IsPathRooted(assetOrSysPath))
                {
                    fullSysPath = UnityToSysPathStyle(assetOrSysPath);
                    return true;
                }
                else if (IsAssetPath(assetOrSysPath))
                {
                    fullSysPath = AssetPathToFullSysPath(assetOrSysPath);
                    return true;
                }
                else
                {
                    try
                    {
                        System.IO.FileInfo info = new System.IO.FileInfo(assetOrSysPath);
                        fullSysPath = info.FullName;
                        return true;
                    }
                    catch (System.Exception)
                    {
                        // nothing
                    }
                }
            }
            fullSysPath = string.Empty;
            return false;
        }


        public static bool TryAnyPathToAssetPath(string assetOrSysPath, out string assetPath)
        {
            if (!string.IsNullOrEmpty(assetOrSysPath))
            {
                if (System.IO.Path.IsPathRooted(assetOrSysPath))
                {
                    return TryFullSysPathToAssetPath(assetOrSysPath, out assetPath);
                }
                else if (IsAssetPath(assetOrSysPath))
                {
                    assetPath = SysToUnityPathStyle(assetOrSysPath);
                    return true;
                }
                else
                {
                    try
                    {
                        System.IO.FileInfo info = new System.IO.FileInfo(assetOrSysPath);
                        var fullPath = info.FullName;
                        return TryFullSysPathToAssetPath(fullPath, out assetPath);
                    }
                    catch (System.Exception)
                    {
                        // nothing
                    }
                }
            }
            assetPath = string.Empty;
            return false;
        }

        public static string AddFullPathFileName(string _fullPath, string _add)
        {
            var _index = _fullPath.LastIndexOf('.');
            if (_index != -1)
            {
                return _fullPath.Insert(_index, _add);
            }
            return _fullPath + _add;
        }

        public static void MakeSureDirectoryExist(string _path)
        {
            string _folder = System.IO.Path.GetDirectoryName(_path);
            if (!System.IO.Directory.Exists(_folder))
            {
                System.IO.Directory.CreateDirectory(_folder);
            }
        }

        public static void MakeSureAssetDirectoryExist(string assetPath)
        {
            string sysPath = AssetPathToFullSysPath(assetPath);
            string dir = System.IO.Path.GetDirectoryName(sysPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
        }
    }
}