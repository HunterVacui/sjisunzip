# This is a Fork of kjerk's "sjisunzip" utility.

[See kjerk's original version here](https://github.com/kjerk/sjisunzip)

# Basic Usage

If you've attempted to unzip a file that was created on a Japanese computer, you might see files with completely garbled text.

In some cases, this might make the files unusuable (eg. If the contents of the files contain the names of other files, those references won't work anymore)


This can happen because the original file was created on a machine that has one default "encoding", and you've tried to unzip it on your own machine which has a different default "encoding", which gives you bad results.

Use this utility to unzip your file, using an appropriate encoding translation.

```
Usage:
  sjisunzip [flags] someFile.zip [toFolder]
  sjisunzip someFolderContainingFilesWithBadNames
  
Examples:
  sjisunzip aFile.zip
  sjisunzip aFile.zip MyNewFolder
```

# Advanced Usage

```
Flags:
      -r                  Instead of unzipping to a folder, make a new zip file with UTF8 uncoding, named {filename}_utf8.zip,
                          which you should then be able to open with any standard zip program without naming problems
      --find_best_target  Compares the results of different target encodings to try to find the best results.
      -s:<number>         Specify an override source encoding. (What the file was created with)
                               Default is 932 (Shift JIS, AKA Windows-932, AKA CP932, AKA Windows-31J)
      -t:<number>         Specify an override target encoding. (What you the new file to be encoded with)
                               Default is 65001 (UTF8)
                          using --find_best_target causes this parameter to be ignored
      --all_sources       Special utility to help with -s command line option. Does not create a new file or folder. Instead,
                          takes your input file, runs it through the encoders available on your machine, and prints out a
			  list of the encoders along with a score representing how likely it is that the encoder was used to
			  generate the file.
      -p                  pause on exit
      
Usage:
      sjisunzip some_folder_with_corrupt_filenames
      sjisunzip --all_sources someText [outputFile.txt]
          Attempts to use different source encoding schemes to decode the text, prints the output to a given file
          
      
Examples:
  Unusual Case: Unzip a file that doesn't seem to have been encoding with Shift JIS (default sjisunzip didn't work)
      sjisunzip --find_best_target SomeZipThatDidntWorkWithTheAboveSteps.zip
      
  Unusual Case: Same case as above but --find_best_target didn't work
      sjisunzip --all_sources "C:\tmp\iƒeƒLƒXƒgƒtƒ@ƒCƒ‹j.zip"
      sjisunzip -s:<pick_a_value_from_the_output_of_the_previous_step> SomeZipFileEncodedWithEncoding1234.zip
      
  Unusual Case: Fixing a folder with bad file names. Presumably, the contents of a zip file you already unzipped before,
                which now has messed up file names, but you don't have the original zip anymore and you're desperate
      sjisunzip -s:65001 -t:932 YourSpecialFolder       -- put the file names back into Shift-JIS, from your mangled UTF-8
      sjisunzip -s:932 -t:65001 YourSpecialFolder       -- properly translate the file from Shift-JIS to UTF-8 this time
```


In some less common cases, the options above may not be enough to fix your file names. This is because we have to assume what encoding the file made with, and we may have guessed wrong.

If your folder still contains the wrong file names, try the `--find_best_target` command ling option, which will attempt to decode the target files with all the encoders available on the user's machine, rate the results, and then commit the operation using the "best" version. Useful for when the default UTF8 doesn't return correct results

If nothing else is working, you can use the `--all_sources` command line option, as a way to print out all the encodings available on your machine as well as some information about the sjisunzip's "score" for each encoding. You can then use the `-s:` command line option to try out one of those encodings as the "source encoding" (the encoding that you think the file was made with

For power users, you can also use `-t:` as the "Target encoding" (the encoding that you want to write the file to)

The command line option `-p` causes the program to halt upon completion, which can be useful for vieweing the output if you're running the script through a process where the window automatically closes when the program exits (such as if you set the program up as a "send to" windows target)

