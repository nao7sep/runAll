using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nekote;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace runAll
{
    class Program
    {
        // Thu, 14 Nov 2019 06:39:21 GMT
        // どんどん汚れていくが、他で設定する必要性が生じた
        internal static bool mPausesAtEnd = false;

        static void Main (string [] args)
        {
            try
            {
                foreach (string xPath in args)
                {
                    if (nDirectory.Exists (xPath))
                        iUtility.HandleDirectory (new DirectoryInfo (xPath));
                    else if (nFile.Exists (xPath))
                        iUtility.HandleFile (new FileInfo (xPath));
                }

                foreach (string xPath in iUtility.TargetPaths)
                {
                    if (nDirectory.Exists (xPath))
                        iUtility.HandleDirectory (new DirectoryInfo (xPath));
                    else if (nFile.Exists (xPath))
                        iUtility.HandleFile (new FileInfo (xPath));
                }

                // Sat, 05 May 2018 18:30:54 GMT
                // 連番や年月日でタスクリストを作るようなこともあり得なくはないため、数値としての文字列の比較を行っておく
                iUtility.AllPaths.Sort ((first, second) => nString.CompareNumerically (first, second, true));

                foreach (string xPath in iUtility.AllPaths)
                {
                    bool xIsExcluded = false;

                    // Sat, 05 May 2018 18:31:19 GMT
                    // 扱うパスが膨大なら、単一の文字列とし、単一のパターンを適用してからの行分割なども選択肢だが、
                    // 現行の実装では *.exe に限ってのスキャンなので、パターンの照合もベタなループでよい

                    foreach (string xPattern in iUtility.ExcludedPathPatterns)
                    {
                        if (Regex.Match (xPath, xPattern, RegexOptions.Compiled) != Match.Empty)
                        {
                            xIsExcluded = true;
                            break;
                        }
                    }

                    if (xIsExcluded == false)
                    {
                        // Mon, 28 Jan 2019 10:02:29 GMT
                        // 今のところ、このプログラムは taskKiller だけに使われていて、
                        // 実行中のタスクリストを開こうとしてのエラーメッセージが毎回わずらわしい
                        // そのため、仕様として偏るが、taskKiller のみ、実行中かどうかを見るようにした

                        string xName = nPath.GetName (xPath);

                        if (nIgnoreCase.Compare (xName, "taskKiller.exe") == 0)
                        {
                            // Mon, 23 Sep 2019 10:53:29 GMT
                            // Running.txt があって開かれなかったタスクリストの数をチェックする手間が生じていた
                            // スペインは回線が遅く、Dropbox が不安定で、閉じているのに Running.txt が残ることがある
                            // そこで、開かれているものがあったなら、ユーザーに明らかだろうとコンソールを閉じないようにした

                            string xDirectoryPath = nPath.GetDirectoryPath (xPath),
                                xRunningFilePath = nPath.Combine (xDirectoryPath, "Running.txt"),
                                xDirectoryName = nPath.GetName (xDirectoryPath);

                            if (nFile.Exists (xRunningFilePath))
                            {
                                mPausesAtEnd = true;
                                // Mon, 23 Sep 2019 11:02:11 GMT
                                // Settings.txt の Title を読もうとしたが、独自実装の設定ファイルであり、
                                // KVP だとみなしての Nekote での読み込みに失敗したため、
                                // 一部が _ にエスケープされている可能性のあるディレクトリー名で妥協

                                // Empty は常に正常だが、Running は異常終了などにより Running.txt が残っているだけの可能性も含む
                                // また、Empty はこれからどんどん増えていく
                                // そのため、Running を目立つようにしておく
                                // たいてい黄色背景に白文字だが、それだと飽きるのでアイコンの色に合わせてマゼンタに

                                Console.BackgroundColor = ConsoleColor.Magenta;
                                Console.WriteLine ("Running: " + xDirectoryName);
                                Console.ResetColor ();

                                // Sun, 13 Oct 2019 02:09:15 GMT
                                // Running.txt がない場合のみバックアップを行う条件分岐だが、なぜかそのまま起動していた
                                // そのため、コンソールに表示が行われながらも各プロセスの Already running も表示されていて不便だった
                                // ここで continue するのはきれいでないが、その後の処理が増えない限り、これで問題ない
                                continue;
                            }

                            else
                            {
                                bool xHasTask = false;
                                string xTasksDirectoryPath = Path.Combine (xDirectoryPath, "Tasks");

                                if (nDirectory.Exists (xTasksDirectoryPath))
                                {
                                    foreach (FileInfo xFile in nDirectory.GetFiles (xTasksDirectoryPath, "*.txt"))
                                    {
                                        try
                                        {
                                            string xFileContents = nFile.ReadAllText (xFile.FullName),
                                                xFirstChunk = xFileContents.nSplitIntoParagraphs () [0];

                                            nDictionary xDictionary = xFirstChunk.nKvpToDictionary ();
                                            string xStateString = xDictionary.GetString ("State").ToLowerInvariant ();

                                            if (xStateString == "queued" || xStateString == "later" || xStateString == "soon" || xStateString == "now")
                                            {
                                                xHasTask = true;
                                                break;
                                            }
                                        }

                                        catch
                                        {
                                            // 不正なフォーマットにはここでは対処しない
                                        }
                                    }
                                }

                                if (xHasTask == false)
                                {
                                    if (iUtility.DisplaysEmptyTaskLists)
                                    {
                                        mPausesAtEnd = true;
                                        Console.WriteLine ("Empty: " + xDirectoryName);
                                    }

                                    continue;
                                }
                            }
                        }

                        // Sat, 30 Mar 2019 07:43:06 GMT
                        // エクスプローラーで実行ファイルをダブルクリックしての起動と異なり、
                        // runAll で Process.Start を呼ぶのでは、カレントディレクトリーが runAll の実行ファイルのあるところになる
                        // その場合、taskKiller がテーマのファイルの絶対パスを取得できず、ロードに失敗するということがあった
                        // テーマのファイルは相対 URI で指定しなければ例外が飛ぶため、そこだけカレントディレクトリーに依存するのを避けられない
                        // taskKiller 側でカレントディレクトリーを自ら設定するようにしたため、もう問題はないが、
                        // runAll で他のプログラムを呼ぶようになる可能性もあるため、念のため runAll 側でも対応しておく

                        ProcessStartInfo xStartInfo = new ProcessStartInfo ();
                        xStartInfo.FileName = xPath;
                        xStartInfo.WorkingDirectory = nPath.GetDirectoryPath (xPath);

                        // Sat, 30 Mar 2019 07:45:51 GMT
                        // Dispose してしまってよいのか疑問に思ったが、fire-and-forget 型なので問題がないようだ
                        // Dispose できると知ったものを Dispose しないのは余計に気になるため、一応やっておく

                        using (Process xProcess = new Process ())
                        {
                            xProcess.StartInfo = xStartInfo;
                            xProcess.Start ();
                        }
                    }
                }

                if (mPausesAtEnd)
                    iUtility.PauseAtEnd ();
            }

            catch (Exception xException)
            {
                // Sat, 05 May 2018 18:33:54 GMT
                // 一人用だし、一回動いたらずっと動く類いのプログラムなので適当
                Console.WriteLine (xException.ToString ().nNormalizeLegacy ());
                iUtility.PauseAtEnd ();
            }
        }
    }
}
