using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace CydiaMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            RebuildCydiaFolderStructure("testDebPath", "packagePath");
            Console.Read();
        }
        private static void RebuildCydiaFolderStructure(string debPath, string packagePath)
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            var tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
            var files = Directory.GetFiles(debPath, "*.deb", SearchOption.AllDirectories);
            var packageFilePath = packagePath + "\\Packages";
            if (File.Exists(packageFilePath))
            {
                File.Delete(packageFilePath);
            }
            foreach (string file in files)
            {
                string extractFolder = tempFolder;
                try
                {

                    if (!Directory.Exists(extractFolder))
                    {
                        Directory.CreateDirectory(extractFolder);
                    }
                    extractFolder = Path.Combine(extractFolder, Path.GetFileNameWithoutExtension(file));
                    StringBuilder content = new StringBuilder();

                    content.Append(Util.GetDebInfo(file, extractFolder));
                    var fi = new FileInfo(file);
                    string md5 = null;
                    using (FileStream fs = new FileStream(fi.FullName, FileMode.Open))
                    {
                        md5 = Util.ComputeMD5(fs);
                        fs.Close();
                    }
                    content.Append(string.Format("Filename: {0}\nSize: {1}\nMD5sum: {2}\n\n",
                            Path.GetFullPath(file).Replace(packagePath, "").Replace("\\", "/").TrimStart('/'),
                            fi.Length, md5));

                    var sb = new StringBuilder();
                    sb.Append(content);
                    using (TextWriter tw = new StreamWriter(packageFilePath, true, utf8WithoutBom))
                    {
                        tw.Write(sb.ToString());
                        tw.Close();
                    }
                }
                catch (Exception exxx)
                {
                    Console.WriteLine("Error:" + file + Environment.NewLine + exxx.ToString());
                }
                try
                {
                    Directory.Delete(extractFolder, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            BZip2.Compress(File.OpenRead(packageFilePath), File.Create(packageFilePath + ".bz2"), true, 4096);

            string strOupFileAllPath = Path.GetDirectoryName(packageFilePath) + "\\Package.gz";
            using (var outTmpStream = new FileStream(strOupFileAllPath, FileMode.OpenOrCreate))
            {
                using (var outStream = new GZipOutputStream(outTmpStream))
                {
                    using (TarArchive archive = TarArchive.CreateOutputTarArchive(outStream, TarBuffer.DefaultBlockFactor))
                    {
                        TarEntry entry = TarEntry.CreateEntryFromFile(packageFilePath);
                        archive.WriteEntry(entry, true);
                        archive.Close();
                    }
                    outTmpStream.Close();
                    outStream.Close();
                }
            }



        }
    }
}
