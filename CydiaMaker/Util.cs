using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ICSharpCode.SharpZipLib.BZip2;
using SevenZip;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using System.Text.RegularExpressions;

namespace CydiaMaker
{
    public class Util
    {
        private static readonly Regex fwRegex = new Regex("firmware [(](?<fw>[^)]+.)[)]");

        private static readonly Regex verRegex = new Regex("(?<s1>[\\d*\\.]+\\w*)-(?<s2>[\\d]+\\w*)");

        private static readonly Regex dependRegex = new Regex("[(](?<version1>[^)]+.)[)]");

        static Util() { }




        private static bool CheckFile(string filepath, params string[] keyword)
        {
            bool result = false;
            if (File.Exists(filepath))
            {
                using (StreamReader sr = new StreamReader(filepath, Encoding.UTF8))
                {
                    string nextLine;
                    while ((nextLine = sr.ReadLine()) != null)
                    {
                        foreach (var item in keyword)
                        {
                            if (nextLine.IndexOf(item, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                result = true;
                            }
                        }
                    }
                    sr.Close();
                }
            }
            return result;
        }

        private static void UncompressGZip(string file, string dir)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(dir)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dir));
                }
                string filename = Path.GetFileNameWithoutExtension(file);
                using (GZipInputStream inputStream = new GZipInputStream(File.OpenRead(file)))
                {
                    using (FileStream streamWriter = File.Create(Path.Combine(dir, filename)))
                    {
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = inputStream.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }

                        streamWriter.Close();
                        inputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="aimpath"></param>
        public static void DeleteDirectory(string aimpath)
        {
            try
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加之
                if (aimpath[aimpath.Length - 1] != Path.DirectorySeparatorChar)
                    aimpath += Path.DirectorySeparatorChar;
                // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
                // 如果你指向delete目标文件下面的文件而不包含目录请使用下面的方法
                // string[] filelist = directory.getfiles(aimpath);
                string[] filelist = Directory.GetFileSystemEntries(aimpath);
                // 遍历所有的文件和目录
                foreach (string file in filelist)
                {
                    // 先当作目录处理如果存在这个目录就递归delete该目录下面的文件
                    if (Directory.Exists(file))
                    {
                        DeleteDirectory(aimpath + Path.GetFileName(file));
                    }
                    // 否则直接delete文件
                    else
                    {
                        File.Delete(aimpath + Path.GetFileName(file));
                    }
                }
                //删除文件夹
                Directory.Delete(aimpath, true);
            }
            catch (Exception e)
            {

            }
        }



        public static string GetDebInfo(string file, string extractFolder)
        {
            if (Directory.Exists(extractFolder))
            {
                DeleteDirectory(extractFolder);
            }
            Directory.CreateDirectory(extractFolder);
            var extractor = new SevenZipExtractor(file);

            extractor.ExtractArchive(extractFolder);


            string[] files2 = Directory.GetFiles(extractFolder, "*.gz");
            for (int j = 0; j < files2.Length; j++)
            {
                UncompressGZip(files2[j], extractFolder);
            }



            string[] files3 = Directory.GetFiles(extractFolder, "*.tar");
            for (int j = 0; j < files3.Length; j++)
            {
                var zipExtractor = new SevenZipExtractor(files3[j]);
                zipExtractor.ExtractArchive(extractFolder);
            }

            StringBuilder raw = new StringBuilder();
            //读取control 其他信息
            using (StreamReader sr = new StreamReader(Path.Combine(extractFolder, "control"), Encoding.UTF8))
            {
                string nextLine;
                while ((nextLine = sr.ReadLine()) != null)
                {
                    raw.Append(nextLine + "\n");
                }
                sr.Close();
            }
            return raw.ToString();
        }

        public static string ComputeMD5(Stream stream)
        {
            try
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

    }
}
