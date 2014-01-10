using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentGenerator
{
    /// <summary>
    /// This class generates code which can be later copy-pasted into ContentPaths.cs for convenience. Basically,
    /// it takes everything from the content directory, and converts it into a compile-time class that can be used with
    /// intellisense
    /// </summary>
    public class ContentPathGenerator
    {
        private static string contentRootDirectory = "C:\\Users\\Mklingen\\Documents\\Visual Studio 2010\\Projects\\VoxelTest\\VoxelTest\\VoxelTestContent";
        private static string contentDirName = "VoxelTestContent";

        public class ContentDir
        {
            public string Name { get; set; }
            public List<string> FullPath { get; set; }
            public List<ContentDir> Children { get; set; }

            public ContentDir()
            {
                Name = "";
                FullPath = new List<string>();
                Children = new List<ContentDir>();
            }

            public string GetTab(int level)
            {
                string toReturn = "";
                for(int i = 0; i < level; i++)
                {
                    toReturn += "    ";
                }

                return toReturn;
            }

            public string WriteCodeRecursive(int tabLevel)
            {
                if(Children.Count > 0)
                {
                    string toReturn = GetTab(tabLevel) + "public class " + Name + "\n" + GetTab(tabLevel) +"{\n";

                    toReturn = Children.Aggregate(toReturn, (current, child) => current + child.WriteCodeRecursive(tabLevel + 1));

                    toReturn += "\n" + GetTab(tabLevel) + "}\n";

                    return toReturn;
                }
                else
                {
                    string toReturn = GetTab(tabLevel + 1) + "public static string " + Name + " = Program.CreatePath(";

                    for(int i = 0; i < FullPath.Count; i++)
                    {
                        toReturn += "\"" + FullPath[i] + "\"";

                        if(i < FullPath.Count - 1)
                        {
                            toReturn += ", ";
                        }
                    }
                    toReturn += ");\n";

                    return toReturn;
                }
            }
        }

        public static string GenerateCode()
        {
            
            List<string> ignoreDirectories = new List<string>
            {
                "obj",
                "bin"
            };

            List<string> ignoreExtensions = new List<string>
            {
                "psd",
                "tmp"
            };

            Dictionary<string, List<string>> paths = new Dictionary<string, List<string>>();

            AddPathsRecursive(paths, ignoreExtensions, ignoreDirectories, contentRootDirectory);
            List<ContentDir> contentDirs = CreateContentDirs(paths);


            string code = "public class ContentPaths\n{\n";
            foreach(ContentDir dir in contentDirs)
            {
                code +=  dir.WriteCodeRecursive(1);
            }
            code += "\n}";

            Console.Out.WriteLine(code);
            return code;
        }

        public static ContentDir AddOrCreate(string path, List<ContentDir> existingDirs)
        {
            ContentDir existingPath = existingDirs.FirstOrDefault(dir => dir.Name == path);

            if(existingPath == null)
            {
               ContentDir newDir = new ContentDir
                {
                    Name = path
                };
                existingDirs.Add(newDir);

                return newDir;
            }
            else
            {
                return existingPath;
            }
        }

        public static List<ContentDir> CreateContentDirs(Dictionary<string, List<string>> paths)
        {
            List<ContentDir> toReturn = new List<ContentDir>();

            foreach(var pair in paths)
            {
                ContentDir existingPath = AddOrCreate(pair.Value[0], toReturn);
                existingPath.FullPath = new List<string>{pair.Value[0]};

                for(int i = 1; i < pair.Value.Count; i++)
                {
                    List<string> path = existingPath.FullPath;
                    existingPath = AddOrCreate(pair.Value[i], existingPath.Children);
                    existingPath.FullPath.Clear();
                    existingPath.FullPath.AddRange(path);
                    existingPath.FullPath.Add(pair.Value[i]); 
                }

                List<string> finalPath = existingPath.FullPath;
                existingPath = AddOrCreate(pair.Key, existingPath.Children);
                existingPath.FullPath.AddRange(finalPath);
                existingPath.FullPath.Add(existingPath.Name);
            }

            return toReturn;
        }

        public static void AddPathsRecursive(Dictionary<string, List<string>> paths, List<string> ignoreExtensions, List<string> ignoreDirectories, string rootDirectory)
        {

            System.IO.DirectoryInfo directoryInfo = new DirectoryInfo(rootDirectory);

            foreach (System.IO.DirectoryInfo info in directoryInfo.EnumerateDirectories())
            {
                if (ignoreDirectories.Contains(info.Name))
                {
                    continue;
                }
                else
                {
                    AddPathsRecursive(paths, ignoreExtensions, ignoreDirectories, info.FullName);
                }

                foreach(System.IO.FileInfo file in info.EnumerateFiles())
                {
                    if(ignoreExtensions.Contains(file.Extension))
                    {
                        continue;
                    }
                    else
                    {
                        List<string> stringList = new List<string>();
                        string[] tokens = file.FullName.Split('\\');

                        bool startAdding = false;
                        foreach(string toek in tokens)
                        {
                            if(startAdding)
                            {
                                stringList.Add(toek);
                            }
                            if(toek == contentDirName)
                            {
                                startAdding = true;
                            }
                        }

                        stringList.RemoveAt(stringList.Count - 1);

                        paths[file.Name.Substring(0, file.Name.LastIndexOf('.'))] = stringList;
                    }
                }
            }
        }
    }
}
