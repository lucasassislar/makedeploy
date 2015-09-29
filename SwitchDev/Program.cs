using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        static void Main(string[] args)
        {
            string root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            DirectoryInfo rootDir = new DirectoryInfo(root);
            List<FileInfo> allfiles = new List<FileInfo>();
            GetList("*.html", allfiles, rootDir);

            for (int i = 0; i < allfiles.Count; i++)
            {
                FileInfo f = allfiles[i];
                Console.WriteLine("Updating " + f.Name);

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
                        fileName = fileName.Remove(fileName.Length - 4, 4);
                        fileName += ".js";
                            //jsSrc = jsSrc.Replace(jsSrc, fileName);
                            allHtml = allHtml.Replace(jsSrc, fileName);
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
                    int end2 = allHtml.IndexOf("'", index + 6);// 5 chars + 1
                    if (end2 < end && end2 != -1)
                    {
                        end = end2;
                    }

                    string cssSrc = allHtml.Substring(index + 6, end - index - 6);
                    if (cssSrc.StartsWith("http") || !cssSrc.EndsWith(".css")) 
                    {
                        // cant cahnge to mini version of already online resources
                        index = end + 1;
                        continue;
                    }

                    string fileName = cssSrc.Remove(cssSrc.Length - 4, 4);
                    if (fileName.EndsWith("min"))
                    {
                        index = end + 1;
                        fileName = fileName.Remove(fileName.Length - 4, 4);
                        fileName += ".css";
                            //cssSrc = cssSrc.Replace(cssSrc, fileName);
                            allHtml = allHtml.Replace(cssSrc, fileName);
                    }

                    index = end + 1;
                }

                File.WriteAllText(f.FullName, allHtml);
            }

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
