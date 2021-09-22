#define LUA_LOAD_LOCAL_SRC
#define LUA_LOAD_LOCAL_RES

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

[XLua.LuaCallCSharp]
public static class LuaHelper
{

    public static string GetBuildEnv()
    {
#if BUILD_ENV_DEV
        return "dev";
#elif BUILD_ENV_TEST
        return "test";
#elif BUILD_ENV_STAGING
        return "staging";
#elif BUILD_ENV_PRODUCTION
        return "production";
#else
        return "dev";
#endif
    }

    public static bool UseLocalSrc()
    {
#if UNITY_EDITOR && LUA_LOAD_LOCAL_SRC
        return true;
#else
		return false;
#endif
    }

    public static bool UseLocalRes()
    {
#if UNITY_EDITOR && LUA_LOAD_LOCAL_RES
        return true;
#else
        return false;
#endif
    }

    public static UnityEngine.Object LoadLocalAsset(string res)
    {
#if UNITY_EDITOR && LUA_LOAD_LOCAL_RES
        return UnityEditor.AssetDatabase.LoadMainAssetAtPath(res);
#else
        return null;
#endif
    }

    public static UnityEngine.Object LoadLocalAsset(string res, Type type)
    {
#if UNITY_EDITOR && LUA_LOAD_LOCAL_RES
        return UnityEditor.AssetDatabase.LoadAssetAtPath(res, type);
#else
        return null;
#endif
    }

    public static void GC()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }

    public static bool IsNull(UnityEngine.Object o)
    {
        return (o == null);
    }
    public static bool IsNull(object o)
    {
        return (o == null);
    }

    /// 检测文件是否存在
    public static bool IsFileExists(string fileName)
    {
        return File.Exists(fileName);
    }

    /// 删除文件
    public static void DeleteFile(string fileName)
    {
        if (IsFileExists(fileName))
            File.Delete(fileName);

    }

    /// 复制文件
    public static void CopyFile(string fromFileName, string destFileName)
    {
        if (File.Exists(fromFileName) && !fromFileName.Equals(destFileName))
        {
            string filePath = Path.GetDirectoryName(destFileName);
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            File.Copy(fromFileName, destFileName, true);
        }
    }

    public static bool MoveFile(string fromFileName, string destFileName)
    {
        try
        {
            if (File.Exists(fromFileName) && !fromFileName.Equals(destFileName))
            {
                if (File.Exists(destFileName))
                    File.Delete(destFileName);
                File.Move(fromFileName, destFileName);
            }
            return true;
        } catch (Exception)
        {
            return false;
        }
    }

    public static string[] ListFile(string dir, string pattern)
    {
        return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
    }

    public static byte[] ReadFile(string fileName)
    {
        if (!File.Exists(fileName))
            return null;

        try
        {
            return File.ReadAllBytes(fileName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool SaveFile(string fileName, byte[] data)
    {
        try
        {
            File.WriteAllBytes(fileName, data);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool SaveImage(string file, UnityEngine.Texture2D tex, int quality = 75)
    {
        CreateFolder(Path.GetDirectoryName(file));

        string ext = Path.GetExtension(file);
        bool result = false;
        switch(ext.ToLower())
        {
            case ".png":
                result = SaveFile(file, UnityEngine.ImageConversion.EncodeToPNG(tex));
                break;
            case ".jpg":
                result = SaveFile(file, UnityEngine.ImageConversion.EncodeToJPG(tex, quality));
                break;
            case ".exr":
                result = SaveFile(file, UnityEngine.ImageConversion.EncodeToEXR(tex));
                break;
            default:
                result = false;
                break;
        }
        return result;
    }


    /// 检测是否存在文件夹
    public static bool IsFolderExists(string folderPath)
    {
        return Directory.Exists(folderPath);
    }

    /// 删除文件夹
    public static void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);

    }

    /// 创建文件夹
    public static void CreateFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

    }

    /// 复制文件夹
    public static void CopyFolder(string fromFolderPath, string destFolderPath)
    {

        if (!Directory.Exists(fromFolderPath))
            return;
        if (!Directory.Exists(destFolderPath))
            Directory.CreateDirectory(destFolderPath);

        // 创建所有的对应目录
        foreach (string dirPath in Directory.GetDirectories(fromFolderPath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(fromFolderPath, destFolderPath));


        // 复制原文件夹下所有内容到目标文件夹，直接覆盖
        foreach (string newPath in Directory.GetFiles(fromFolderPath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(fromFolderPath, destFolderPath), true);

    }


    public static string[] ListFolder(string dir, string pattern)
    {
        return Directory.GetDirectories(dir, pattern, SearchOption.AllDirectories);
    }


    public static string HttpWebRequest(string url)
    {
        string responseContent = null;

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.ContentType = "application/text";
        request.Method = "GET";

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using (Stream resStream = response.GetResponseStream())
        {
            using (StreamReader reader = new StreamReader(resStream, Encoding.UTF8))
            {
                responseContent = reader.ReadToEnd().ToString();
            }
        }
        return responseContent;
    }


    //计算和服务器接口api中的签名
    public static string GetAPIHash(string input)
    {
        if (input == null)
        {
            return null;
        }
        string ret = GetMd5Hash(input);
        ret = GetSha256Hash(ret);
        return ret;
    }

    //计算md5
    public static string GetMd5Hash(string input)
    {
        if (input == null)
        {
            return null;
        }

        MD5 md5Hash = MD5.Create();

        // 将输入字符串转换为字节数组并计算哈希数据
        byte[] hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // 创建一个 Stringbuilder 来收集字节并创建字符串
        StringBuilder sBuilder = new StringBuilder();
        // 循环遍历哈希数据的每一个字节并格式化为小写字母十六进制字符串
        for (int i = 0; i < hash.Length; i++)
        {
            sBuilder.Append(hash[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }

    public static void ExecuteEventsExecute(UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData data, string name)
    {
        if (name == "pointerDown")
        {
            UnityEngine.EventSystems.ExecuteEvents.Execute(target, data, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        }
        else if (name == "pointerUp")
        {
            UnityEngine.EventSystems.ExecuteEvents.Execute(target, data, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
        }
        else if (name == "pointerClick")
        {
            UnityEngine.EventSystems.ExecuteEvents.Execute(target, data, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }
        else
        {
            UnityEngine.Debug.Log(string.Format("unknown param {0}", name));
        }
    }


    //计算sha256
    public static string GetSha256Hash(string input)
    {
        if (input == null)
        {
            return null;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256Managed.Create().ComputeHash(bytes);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }
        return builder.ToString();
    }


};







