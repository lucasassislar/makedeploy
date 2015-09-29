using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SwitchDeploy
{
    class Program
    {
        private static bool USEREST = false;
        private static string root;

        private static void minify(string path, string output)
        {
            string name = Path.GetFileName(output);
            path = path.Replace(root + "\\", "");

            string cmd = string.Format("/C java -jar yuicompressor.jar {0} -o {1} --charset utf-8", path, name);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = cmd;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            FileInfo f = new FileInfo(name);
            f.MoveTo(output);
        }

        private static void minify(string path)
        {
            path = path.Replace(root + "\\", "");
            string cmd = string.Format("/C java -jar yuicompressor.jar {0} --charset utf-8", path);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = cmd;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        static void Main(string[] args)
        {
            long totalSize = 0;
            long oriSize = 0;

            root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string backupDir = Path.Combine(root, "backup");
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
            }
            Directory.CreateDirectory(backupDir);

            DirectoryInfo rootDir = new DirectoryInfo(root);
            List<FileInfo> allfiles = new List<FileInfo>();
            GetList("*.js", allfiles, rootDir);

            for (int i = 0; i < allfiles.Count; i++)
            {
                FileInfo f = allfiles[i];
                oriSize += f.Length;

                string file = f.FullName.Remove(f.FullName.Length - 3, 3);
                if (file.EndsWith("min"))
                {
                    continue;
                }
                else
                {
                    file = file + ".min.js";
                }

                string relative = f.FullName.Replace(root + "\\", "");
                string dir = Path.Combine(root, "backup", Path.GetDirectoryName(relative));
                Directory.CreateDirectory(dir);
                File.Copy(f.FullName, Path.Combine(dir, Path.GetFileName(relative)));

                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                Console.WriteLine("Minifying JS: " + f.Name);

                //java -jar yuicompressor-x.y.z.jar [options] [input file]
                if (USEREST)
                {
                    using (WebClient client = new WebClient())
                    {
                        string allJs = File.ReadAllText(f.FullName);

                        NameValueCollection col = new NameValueCollection();
                        col.Add("input", allJs);

                        byte[] data = client.UploadValues("http://javascript-minifier.com/raw", col);

                        string newJs = Encoding.UTF8.GetString(data);
                        File.WriteAllText(file, newJs);

                        totalSize += new FileInfo(file).Length;
                    }
                }
                else
                {
                    minify(f.FullName);
                }
            }

            allfiles = new List<FileInfo>();
            GetList("*.css", allfiles, rootDir);

            for (int i = 0; i < allfiles.Count; i++)
            {
                FileInfo f = allfiles[i];
                oriSize += f.Length;

                string file = f.FullName.Remove(f.FullName.Length - 4, 4);
                if (file.EndsWith("min"))
                {
                    continue;
                }
                else
                {
                    file = file + ".min.css";
                }

                string relative = f.FullName.Replace(root + "\\", "");
                string dir = Path.Combine(root, "backup", Path.GetDirectoryName(relative));
                Directory.CreateDirectory(dir);
                File.Copy(f.FullName, Path.Combine(dir, Path.GetFileName(relative)));

                if (File.Exists(file))
                {
                    File.Delete(file); // delete the minified so we can minify it again
                }
                Console.WriteLine("Minifying CSS: " + f.Name);

                if (USEREST)
                {
                    using (WebClient client = new WebClient())
                    {
                        string allJs = File.ReadAllText(f.FullName);

                        NameValueCollection col = new NameValueCollection();
                        col.Add("input", allJs);

                        byte[] data = client.UploadValues("http://cssminifier.com/raw", col);

                        string newCSS = Encoding.UTF8.GetString(data);
                        File.WriteAllText(file, newCSS);

                        totalSize += new FileInfo(file).Length;
                    }
                }
                else
                {
                    minify(f.FullName);
                }
            }

            allfiles = new List<FileInfo>();
            GetList("*.html", allfiles, rootDir);

            for (int i = 0; i < allfiles.Count; i++)
            {
                break;
                FileInfo f = allfiles[i];
                oriSize += f.Length;

                Console.WriteLine("Updating " + f.Name);

                string relative = f.FullName.Replace(root + "\\", "");
                string dir = Path.Combine(root, "backup", Path.GetDirectoryName(relative));
                Directory.CreateDirectory(dir);
                File.Copy(f.FullName, Path.Combine(dir, Path.GetFileName(relative)));

                string allHtml = File.ReadAllText(f.FullName);

                int index = 0;
                for (;;)
                {
                    index = allHtml.IndexOf("src=", index);
                    if (index == -1)
                    {
                        break;
                    }

                    int end = allHtml.IndexOf("\"", index + 5);
                    int end2 = allHtml.IndexOf("'", index + 5);// 4 chars + 1
                    if (end2 < end && end2 != -1)
                    {
                        end = end2;
                    }

                    string jsSrc = allHtml.Substring(index + 5, end - index - 5);
                    if (jsSrc.StartsWith("http"))
                    {
                        // cant cahnge to mini version of already online resources
                        index = end + 1;
                        continue;
                    }

                    string fileName = jsSrc.Remove(jsSrc.Length - 3, 3);
                    if (fileName.EndsWith("min"))
                    {
                        index = end + 1;
                    }
                    else if (jsSrc.EndsWith(".js")) // needs to end on js
                    {
                        fileName += ".min.js";
                        allHtml = allHtml.Replace(jsSrc, fileName);
                        // change to minify version
                    }
                    index = end + 1;
                }

                index = 0;
                for (;;)
                {
                    index = allHtml.IndexOf("href=", index);
                    if (index == -1)
                    {
                        break;
                    }

                    int end = allHtml.IndexOf("\"", index + 6);
                    int end2 = allHtml.IndexOf("'", index + 6);// 4 chars + 1
                    if (end2 < end && end2 != -1)
                    {
                        end = end2;
                    }

                    string cssSrc = allHtml.Substring(index + 6, end - index - 6);
                    if (cssSrc.StartsWith("http") || cssSrc.Length < 5)
                    {
                        // cant cahnge to mini version of already online resources
                        index = end + 1;
                        continue;
                    }

                    string fileName = cssSrc.Remove(cssSrc.Length - 4, 4);
                    if (fileName.EndsWith("min"))
                    {
                        index = end + 1;
                    }
                    else if (cssSrc.EndsWith(".css")) // needs to end in css
                    {
                        fileName += ".min.js";
                        allHtml = allHtml.Replace(cssSrc, fileName);
                        // change to minify version
                    }
                    index = end + 1;
                }
                File.WriteAllText(f.FullName, allHtml);
                totalSize += f.Length;
            }

            Console.WriteLine();
            Console.WriteLine("Original Size: " + (oriSize / 1024) + "kb");
            Console.WriteLine("New Size: " + (totalSize / 1024) + "kb");
            Console.WriteLine("Difference: " + ((oriSize - totalSize) / 1024) + "kb");
            Console.ReadLine();
        }

        private static void GetList(string pattern, List<FileInfo> list, DirectoryInfo parent)
        {
            list.AddRange(parent.GetFiles(pattern));

            DirectoryInfo[] dirs = parent.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                GetList(pattern, list, dirs[i]);
            }
        }
    }
}
