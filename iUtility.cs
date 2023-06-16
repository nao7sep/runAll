﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;
using System.Configuration;
using System.IO;

namespace runAll
{
    internal static class iUtility
    {
        private static string [] iLoadLines (string fileName)
        {
            string xFilePath = nApplication.MapPath (fileName);

            if (nFile.Exists (xFilePath))
            {
                List <string> xLines = new List <string> ();

                foreach (string xLine in nFile.ReadAllLines (xFilePath))
                {
                    if (xLine.Length > 0 &&
                            xLine.nStartsWith ("//") == false &&
                            // Sat, 05 May 2018 18:34:41 GMT
                            // 正規表現の方では大文字・小文字を区別するが、ここで区別しないことによる影響はまずない
                            xLines.Contains (xLine, StringComparer.OrdinalIgnoreCase) == false)
                        xLines.Add (xLine);
                }

                return xLines.ToArray ();
            }

            // Sat, 05 May 2018 18:35:52 GMT
            // そのまま問題なくループがまわるようにしておく
            else return new string [0];
        }

        private static string [] mTargetPaths = null;

        public static string [] TargetPaths
        {
            get
            {
                if (mTargetPaths == null)
                    mTargetPaths = iLoadLines ("TargetPaths.txt");

                return mTargetPaths;
            }
        }

        private static string [] mExcludedPathPatterns = null;

        public static string [] ExcludedPathPatterns
        {
            get
            {
                if (mExcludedPathPatterns == null)
                    mExcludedPathPatterns = iLoadLines ("ExcludedPathPatterns.txt");

                return mExcludedPathPatterns;
            }
        }

        public static List <string> AllPaths { get; private set; } = new List <string> ();

        public static void HandleFile (FileInfo file)
        {
            if (AllPaths.Contains (file.FullName, StringComparer.OrdinalIgnoreCase) == false)
                AllPaths.Add (file.FullName);
        }

        public static void HandleDirectory (DirectoryInfo directory)
        {
            // Sat, 05 May 2018 18:36:35 GMT
            // スキャン時に問題があれば、呼び出し側でザクッと例外を表示する

            foreach (DirectoryInfo xSubdirectory in directory.GetDirectories ())
                HandleDirectory (xSubdirectory);

            foreach (FileInfo xFile in directory.GetFiles ("*.exe"))
                HandleFile (xFile);
        }

        public static nNameValueCollection AppSettings { get; private set; } =
            new nNameValueCollection (ConfigurationManager.AppSettings);

        private static bool? mIgnoresEmptyTaskLists = null;

        public static bool IgnoresEmptyTaskLists
        {
            get
            {
                if (mIgnoresEmptyTaskLists == null)
                    mIgnoresEmptyTaskLists = AppSettings.GetBoolOrDefault ("IgnoresEmptyTaskLists", true);

                return mIgnoresEmptyTaskLists.Value;
            }
        }

        private static bool? mDisplaysEmptyTaskLists = null;

        public static bool DisplaysEmptyTaskLists
        {
            get
            {
                if (mDisplaysEmptyTaskLists == null)
                    mDisplaysEmptyTaskLists = AppSettings.GetBoolOrDefault ("DisplaysEmptyTaskLists", false);

                return mDisplaysEmptyTaskLists.Value;
            }
        }

        public static void PauseAtEnd ()
        {
            Console.Write ("Press any key to close: ");
            Console.ReadKey (true);
            Console.WriteLine ();
        }
    }
}
