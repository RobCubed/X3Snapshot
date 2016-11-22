using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace X3Snapshot
{
    public enum FileStatus
    {
        Matching,
        Changed,
        New,
        NoMatch
    }

    
    class Program
    {
        public static int totalFiles = 0;
        public static int currentCounter = 1;
        public static string rootDir = @"C:\Program Files (x86)\Steam\steamapps\common\x3 terran conflict";
        public static List<VersionSet> VersionSets;
        public static VersionSet CurrentFileSet;
        public static List<VersionSet> RequiredInstallSet;
        
   
        [STAThread]
        static void Main(string[] args)
        {
            IntroBox();
            Console.WriteLine("Waiting for user to select X3TC/AP directory...");
            bool correct = false;

            while (!correct)
            {
                bool validResponse = true;
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.SelectedPath = rootDir;
                fbd.Description = "Choose the location that contains X3TC.exe or X3AP.exe - NOT your addons folder!";
                var result = fbd.ShowDialog();
                if (result == DialogResult.OK)
                {
                    rootDir = fbd.SelectedPath;
                } else if (result == DialogResult.Cancel)
                {
                    return;
                }
                validResponse = false;

                while (!validResponse)
                {
                    DialogResult dresult = MessageBox.Show(rootDir +"\r\nIs this correct? Yes, No, or press Cancel to quit entirely.", "Warning",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    if (dresult == DialogResult.Yes)
                    {
                        correct = true;
                        validResponse = true;
                        //code for Yes
                    }
                    else if (dresult == DialogResult.No)
                    {
                        correct = false;
                        validResponse = true;
                        //code for No
                    }
                    else if (dresult == DialogResult.Cancel)
                    {
                        return;
                    }
                    
                }

            }
            Thread.Sleep(250);


            SaveFileDialog save = new SaveFileDialog();
            save.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            save.FileName = "X3Snapshot.zip";
            save.Filter = "Zip File | *.zip";
            var saveD = save.ShowDialog();
            Thread.Sleep(100);

            if (saveD != DialogResult.OK) return;
            Thread.Sleep(100);

            Console.WriteLine("Initializing...");
            Initialize();

            Console.WriteLine("Evaluating your game directory: ");
            using (var progress = new ProgressBar())
            {
                GetFileSet(rootDir, progress);
            }
            Console.WriteLine("DONE!");

            Console.WriteLine("Comparing your files and saving. This could take a while if you have large mods!");
            CompareFiles();

            if (saveD == DialogResult.OK)
            {
                SaveFile(save.FileName);
            }

            Console.WriteLine("All done! Press any key to exit.");
            Console.ReadKey();
        }

        static void SaveFile(string fileName)
        {
            List<string> StringList = new List<string>();
            
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (ZipArchive newFile = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                int totalCount = CurrentFileSet.Files.Count();
                int currentFileCount = 0;
                using (var progress = new ProgressBar())
                {
                    foreach (var file in CurrentFileSet.Files)
                    {
                        if (file.CurrentStatus == FileHash.MatchStatus.Modified)
                        {
                            newFile.CreateEntryFromFile(rootDir + file.RelativeFileName, file.RelativeFileName.Substring(1, file.RelativeFileName.Length - 1), CompressionLevel.Optimal);
                            StringList.Add("[MODIFIED FILE] + " + file.RelativeFileName + "\r\n");
                        }
                        else if (file.CurrentStatus == FileHash.MatchStatus.NoMatch)
                        {
                            newFile.CreateEntryFromFile(rootDir + file.RelativeFileName, file.RelativeFileName.Substring(1, file.RelativeFileName.Length - 1), CompressionLevel.Optimal);
                            StringList.Add("[NEW FILE] + " + file.RelativeFileName + "\r\n");
                        }
                        progress.Report((double)currentFileCount++ / totalCount);
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("Packaged by RobCubed's X3Snapshot Tool on  " + DateTime.Now.ToString() + "\r\n");
                sb.Append(" \r\n");
                sb.Append("Warning; This is in ALPHA! Check my Github to see if there is a newer version! https://github.com/RobCubed/X3Snapshot - Link to Egosoft thread will be there.\r\n");
                sb.Append(" \r\n");
                sb.Append("To install this, unzip it to the folder that contains your X3TC.exe or X3AP.exe files. Make sure the following items are installed, as this zip file is expecting them:\r\n");
                foreach (var x in RequiredInstallSet)
                {
                    sb.Append(" --- " + x.VersionName + "\r\n");
                }

                sb.Append("  \r\n");
                sb.Append("List of modified/added files:\r\n");
                foreach (var s in StringList)
                {
                    sb.Append(s);
                }
                string reqFileName = "InstallRequirements.txt";
                File.WriteAllText(reqFileName, sb.ToString());
                newFile.CreateEntryFromFile(reqFileName, reqFileName);
            }
            Console.WriteLine("Packed as " + fileName);
        }


        static void IntroBox()
        {
            Console.WriteLine("Welcome to RobCubed's X3Snapshot Tool (v0.2a - ALPHA!!!)");
            Console.WriteLine("The purpose of this tool is to allow you to save a fully configured X3:TC or X3:AP ");
            Console.WriteLine("modded installation without needing to save the entire game installation.");
            Console.WriteLine("");
            Console.WriteLine("You will be able to transfer your mod setup to a new computer, or just back up ");
            Console.WriteLine("your current mod setup before trying something new.");
            Console.WriteLine("");
            Console.WriteLine("Warning; This is in ALPHA! Check my Github to see if there is a newer version!");
            Console.WriteLine("https://github.com/RobCubed/X3Snapshot - Link to Egosoft thread will be there.");
            Console.WriteLine("");
            Console.WriteLine("Make sure you have available diskspace. For example, my packaged file with Litcubes, IEX, ");
            Console.WriteLine("and a few other small mods was about 3.2GB. Still an improvement over 13GB.");
            Console.WriteLine("");
            Console.WriteLine("This may take some time. On an SSD with X3AP+Litcubes, it took about a minute to evaluate.");
            Console.WriteLine("On a regular hard drive it may take longer.");
            Console.WriteLine("");
            Console.WriteLine("===================================================");

        }

        private static void CompareFiles()
        {
            RequiredInstallSet = new List<VersionSet>();
            foreach (var file in CurrentFileSet.Files)
            {
                foreach (var vset in VersionSets)
                {
                    foreach (var vfileset in vset.Files)
                    {
                        if (file.FileHashMatch(vfileset) == FileHash.MatchStatus.ExactMatch)
                        {
                            if (!RequiredInstallSet.Contains(vset)) RequiredInstallSet.Add(vset);
                            file.CurrentStatus = FileHash.MatchStatus.ExactMatch;
                        } else if (file.FileHashMatch(vfileset) == FileHash.MatchStatus.Modified)
                        {
                            if (!RequiredInstallSet.Contains(vset)) RequiredInstallSet.Add(vset);
                            file.CurrentStatus = FileHash.MatchStatus.Modified;
                        }
                    }
                }
                if (file.CurrentStatus != FileHash.MatchStatus.Modified && file.CurrentStatus != FileHash.MatchStatus.ExactMatch)
                {
                    file.CurrentStatus = FileHash.MatchStatus.NoMatch;
                }
            }
            
        }

        private static void GetFileSet(string rootDir, ProgressBar progress)
        {
            totalFiles = FileCount(rootDir, currentCounter);
            CurrentFileSet = new VersionSet();
            CurrentFileSet.VersionName = "My Package";

            DirSearch(rootDir, progress);
        }

        private static void Initialize()
        {
            VersionSets = new List<VersionSet>();

            string currentDir = Environment.CurrentDirectory;
            DirectoryInfo directory = new DirectoryInfo(currentDir + @"\md5-hashes");
            foreach (string f in Directory.GetFiles(directory.FullName))
            {
                VersionSet set = new VersionSet();
                foreach (string line in File.ReadLines(f))
                {
                    if (line.StartsWith("#"))
                    {
                        set.VersionName = line.Substring(1, line.Length - 1);
                    } else
                    {
                        string[] l = line.Split('\t');
                        set.Files.Add(new FileHash(l[0], l[1]));
                    }
                }
                VersionSets.Add(set);
            }
        }
        
        private static string GetFileHash(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
        }

        static int FileCount(string sDir, int currCount)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    currCount++;
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    currCount = FileCount(d, currCount);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
            return currCount;
        }

        static void DirSearch(string sDir, ProgressBar progress)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    currentCounter++;
                    //Console.WriteLine("Calculating : [" + f + "]");
                    string hash = GetFileHash(f);
                    //Console.WriteLine("[" + currentCounter + "/" + totalFiles + "] " + f + " " + hash);
                    Debug.WriteLine((double)currentCounter / totalFiles);
                    progress.Report((double)currentCounter / totalFiles);
                    string s = f.Substring(rootDir.Length, f.Length - rootDir.Length);
                    CurrentFileSet.Files.Add(new FileHash(s, hash));
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    DirSearch(d, progress);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
    }
}
