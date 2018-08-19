using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace SjisUnzip
{
	public class SjisUnzipApp
	{
        protected const int sjisEncoding = 932; // Windows-932, AKA CP932, AKA Windows-31J, AKA "extended variant of Shift JIS"

        bool mbRecode, mbVerbose, mbAllSourceEncodings, mbFindTargetEncoding, mbPauseOnExit;
        Encoding mSourceEncoding, mTargetEncoding;
        string mIn, mOut;
        bool mError = false;
        private bool ParseFlags(ref string[] args)
        {
            mbRecode = args.Any((s) => s.Equals("-r"));
            mbVerbose = args.Any((s) => s.Equals("-v"));
            mbPauseOnExit = args.Any((s) => s.Equals("-p"));
            mbAllSourceEncodings = args.Any((s) => s.Equals("--all_sources"));
            mbFindTargetEncoding = args.Any((s) => s.Equals("--find_best_target"));

            string useEncoding = args.FirstOrDefault((s) => s.StartsWith("-s:"));
            int defaultEncoding = sjisEncoding;
            if (useEncoding != null)
            {
                defaultEncoding = int.Parse(useEncoding.Substring(3));
            }
            mSourceEncoding = Encoding.GetEncoding(defaultEncoding);

            useEncoding = args.FirstOrDefault((s) => s.StartsWith("-t:"));
            defaultEncoding = Encoding.UTF8.CodePage;
            if (useEncoding != null)
            {
                defaultEncoding = int.Parse(useEncoding.Substring(3));
            }
            mTargetEncoding = Encoding.GetEncoding(defaultEncoding);

            if (args.Length != 1)
            {
                args = args.Where((arg) => !arg.StartsWith("-")).ToArray();
            }

            if (args.Length == 0)
            {
                return false;
            }

            mIn = args[0];
            mOut = null;
            if (args.Length > 1)
                mOut = args[1];

            args = null;

            return true;
        }

        public void Main(string[] args)
        {
            if (ParseFlags(ref args))
            {
                try
                {
                    Run();
                }
                catch (Exception e)
                {
                    mError = true;
                    e.ToString().wl();
                    string wait = System.Console.ReadLine();
                }
            }
            else
            {
                mError = true;
            }

            if (mError)
            {
                printUsage();
                mbPauseOnExit = true;
            }

            if (mbPauseOnExit)
                for (; ; ) { }
        }

        protected void Run()
        {
            int encodingScore = 0;
            if (mbFindTargetEncoding)
            {
                List<ConversionInfo> results;
                if (Directory.Exists(mIn))
                {
                    EncodingInfo sourceEncodingInfo = null;
                    foreach (EncodingInfo ei in Encoding.GetEncodings())
                    {
                        if (ei.CodePage == mSourceEncoding.CodePage)
                        {
                            sourceEncodingInfo = ei;
                            break;
                        }
                    }

                    results = new List<ConversionInfo>();
                    foreach (EncodingInfo targetEncodingInfo in Encoding.GetEncodings())
                    {
                        mTargetEncoding = targetEncodingInfo.GetEncoding();
                        int newScore = 0;

                        ActOnCorruptFilenames(mIn, true,
                            fi => newScore += fi.Name.ReEncode(mSourceEncoding, mTargetEncoding).JapaneseScore(),
                            di => newScore += di.Name.ReEncode(mSourceEncoding, mTargetEncoding).JapaneseScore());

                        if (newScore != 0)
                            results.Add(new ConversionInfo(sourceEncodingInfo, targetEncodingInfo, newScore));
                    }
                }
                else
                {
                    string seed = mIn;
                    if (File.Exists(mIn))
                        seed = Path.GetFileNameWithoutExtension(mIn);
                     results = FindMostLikelyConversions(seed);
                }

                ConversionInfo best = results.OrderBy(x => x.mScore).Last();
                mSourceEncoding = best.mOriginal.GetEncoding();
                mTargetEncoding = best.mTarget.GetEncoding();
                encodingScore = best.mScore;
            }

            if (mbRecode)
            {
                if (File.Exists(mIn))
                    recodeFile(mIn);
                else
                    mError = true;
            }
            else if (mbAllSourceEncodings)
            {
                IterateAllSourceEncodings();
            }
            else if (mOut == null)
            {
                if (Directory.Exists(mIn))
                {
                    ActOnCorruptFilenames(mIn, true,
                        fi => fi.Rename(fi.Name.ReEncode(mSourceEncoding, mTargetEncoding)),
                        di => di.Rename(di.Name.ReEncode(mSourceEncoding, mTargetEncoding)));
                }
                else if (File.Exists(mIn))
                {
                    if (mIn.EndsWith(".zip", true, CultureInfo.CurrentCulture))
                        extractSjisZip(mIn);
                    else
                    {
                        FileInfo fileInfo = new FileInfo(mIn);
                        fileInfo.Rename(fileInfo.Name.ReEncode(mSourceEncoding, mTargetEncoding));
                    }
                }
                else
                    mError = true;
            }
            else if (File.Exists(mIn) && mIn.EndsWith(".zip", true, CultureInfo.CurrentCulture))
            {
                var folderPath = Path.GetDirectoryName(mIn);
                var newFolderPath = Path.Combine(folderPath, mOut);
                Directory.CreateDirectory(newFolderPath);
                extractSjisZip(mIn, newFolderPath);
            }
            else
                mError = true;

            String.Format("Converted using {0} ({1}) => {2} ({3})", mSourceEncoding.HeaderName, mSourceEncoding.CodePage, mTargetEncoding.HeaderName, mTargetEncoding.CodePage).wl();
            String.Format("({0} => {1})", mSourceEncoding.EncodingName, mTargetEncoding.EncodingName).wl();
            if (mbFindTargetEncoding)
                String.Format("(score: {0})", encodingScore).wl();
        }

        protected void IterateAllSourceEncodings()
        {
            string outDir = null;
            string outFile = null;
            if (mOut != null)
            {
                if (mOut.Contains("."))
                    outFile = mOut;
                else
                    outDir = mOut;
            }
            else
            {
                if (File.Exists(mIn) || Directory.Exists(mIn))
                {
                    outFile = Path.Combine(Path.GetDirectoryName(mIn), Path.GetFileNameWithoutExtension(mIn) + "_encodings.txt");
                    mIn = System.IO.Path.GetFileNameWithoutExtension(mIn);
                }
            }

            if (outDir != null)
            {
                if (Directory.Exists(outDir))
                    Directory.Delete(outDir);
                Directory.CreateDirectory(outDir);
            }

            Encoding toEncoding = Encoding.Unicode;
            using (FileStream outStream = outFile != null ? File.OpenWrite(outFile) : null)
            {
                StreamWriter sw = outStream != null ? new StreamWriter(outStream, toEncoding) : null;
                foreach (EncodingInfo ei in Encoding.GetEncodings())
                {
                    Convert(mIn, ei.CodePage, toEncoding, outDir, sw);
                }
            }
        }

		static void printUsage()
		{
			"Usage: sjisunzip someFile.zip [toFolder]".wl();
			"Usage: sjisunzip [flags] someFile.zip".wl();
			"    -r: Recode file to {filename}_utf8.zip".wl();
			"Usage: sjisunzip ./some_folder_with_corrupt_filenames".wl();
            "Usage: sjisunzip --all_sources someText [outputFile.txt]".wl();
            "         Attempts to use different source encoding schemes to decode the text, prints the output to a given file".wl();
            "Flags:".wl();
            "    --find_best_target  Compares the results of different target encodings to try to find the best results.".wl();
            "    -s:<number>         Specify an override source encoding. Default is 932 (shift_jis).".wl();
            "                        use --all_sources to find a good source encoding".wl();
            "    -t:<number>         Specify an override target encoding. Default is 65001 (UTF8).".wl();
            "                        using --find_best_target causes this parameter to be ignored".wl();
            "    -p                  pause on exit".wl();
            "".wl();
            "Examples:".wl();
			"    sjisunzip aFile.zip".wl();
			"    sjisunzip aFile.zip MyNewFolder".wl();
            "    sjisunzip -s:932 aFile.zip".wl();
            "    sjisunzip -s:932 aFolder".wl();
            "    sjisunzip --all_sources \"C:\\tmp\\iƒeƒLƒXƒgƒtƒ@ƒCƒ‹j.zip\"".wl();
        }

		private void extractSjisZip(string fileName, string toFolder = "./")
		{
			"Writing to folder {0}...".wl(toFolder);

			using (var zipFile = new ZipArchive(new FileStream(fileName, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read, false, mSourceEncoding))
			{
				zipFile.ExtractToDirectory(toFolder);
			}

			"Done.".wl();
		}

		private void recodeFile(string srcFile)
		{
			var zipFile = new ZipArchive(new FileStream(srcFile, FileMode.Open), ZipArchiveMode.Read, false, mSourceEncoding);
			var newFilePath = Path.Combine(Path.GetDirectoryName(srcFile), Path.GetFileNameWithoutExtension(srcFile) + "_" + mTargetEncoding.HeaderName + ".zip");

			using (var newZip = new ZipArchive(new FileStream(newFilePath, FileMode.CreateNew), ZipArchiveMode.Create, false, mTargetEncoding))
			{
				foreach (var zipEntry in zipFile.Entries)
				{
					var newFile = newZip.CreateEntry(zipEntry.FullName, CompressionLevel.Fastest);

					newFile.LastWriteTime = zipEntry.LastWriteTime;

					using (Stream newStream = newFile.Open(), oldStream = zipEntry.Open())
					{
						"Moved {0}".wl(newFile.FullName);
						oldStream.CopyTo(newStream);
					}
				}
			}

			"Finished recoding {0} entries.".wl(zipFile.Entries.Count);
        }

		readonly Func<char, bool> dirSeparatorComparator = c => c == Path.DirectorySeparatorChar;

		private void ActOnCorruptFilenames(string directoryPath, bool recurse, Action<FileInfo> fileAction, Action<DirectoryInfo> dirAction)
		{
			var rootDir = new DirectoryInfo(directoryPath);
			var dirs = rootDir.GetDirectories("*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
			var files = rootDir.GetFiles("*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            try
            {
                files.Where(fi => fi.Name.ContainsNonAscii() && !fi.Name.ContainsJapanese())
                    .ToList()
                    .ForEach(fileAction);
            }
            catch (Exception ex)
            {
                ex.ToString().wl();
                mError = true;
            }
			
			// Sort reversed based on directory depth, rename deepest first and this won't rename a root before a leaf.
			dirs.Sort((d2, d1) => d1.FullName.Count(dirSeparatorComparator).CompareTo(d2.FullName.Count(dirSeparatorComparator)));

            try
            {
                dirs.Where(di => di.Name.ContainsNonAscii() && !di.Name.ContainsJapanese())
				.ToList()
				.ForEach(dirAction);
            }
            catch (Exception ex)
            {
                ex.ToString().wl();
                mError = true;
            }

            if (rootDir.Name.ContainsNonAscii() && !rootDir.Name.ContainsJapanese())
			{
                dirAction.Invoke(rootDir);
			}
        }

        struct ConversionInfo
        {
            public ConversionInfo(EncodingInfo original, EncodingInfo target, int score)
            {
                mScore = score;
                mOriginal = original;
                mTarget = target;
            }

            public int mScore;
            public EncodingInfo mOriginal, mTarget;
        }

        static List<ConversionInfo> FindMostLikelyConversions(string str)
        {
            List<ConversionInfo> results = new List<ConversionInfo>();

            string bestResult = str;
            ConversionInfo result;
            result.mScore = str.JapaneseScore();
            result.mOriginal = null;
            result.mTarget = null;

            foreach (EncodingInfo originalEncodingInfo in Encoding.GetEncodings())
            {
                Encoding originalEncoding = originalEncodingInfo.GetEncoding();
                foreach (EncodingInfo targetEncodingInfo in Encoding.GetEncodings())
                {
                    string testStr = str.ReEncode(originalEncoding, targetEncodingInfo.GetEncoding());
                    int newScore = testStr.JapaneseScore();
                    if (newScore != 0)
                        results.Add(new ConversionInfo(originalEncodingInfo, targetEncodingInfo, newScore));
                }
            }

            return results;
        }

        static void Convert(string str, int fromEncodingPage, Encoding to, string outDir, StreamWriter outFile)
        {
            string decoded = "CANNOT_DECODE";
            string fromName = "NOT_SUPPORTED_ON_THIS_COMPUTER";
            string fromHeaderName = string.Format("({0})", fromEncodingPage);
            try
            {
                Encoding from = Encoding.GetEncoding(fromEncodingPage);
                fromName = from.EncodingName;
                decoded = str.Decode(from);
            }
            catch (Exception)
            {
            }

            string outStr = String.Format("({0,6}){1,40}: {2}", fromEncodingPage, fromName, decoded);
            outStr.wl();

            if (outFile != null)
            {
                outFile.WriteLine(outStr);
            }

            if (outDir != null)
            {
                byte[] bytes = to.GetBytes(decoded);
                string fileName = string.Format("({0}){1}_to_({2}){3}.txt", fromEncodingPage, fromHeaderName, to.WindowsCodePage, to.HeaderName);
                try
                {
                    File.OpenWrite(outDir + "\\" + fileName).Write(bytes, 0, bytes.Count());
                }
                catch (Exception)
                {
                    try
                    {
                        fileName = string.Format("{0}_to_{1}.txt", fromEncodingPage, to.WindowsCodePage);
                        File.OpenWrite(outDir + "\\" + fileName).Write(bytes, 0, bytes.Count());
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
