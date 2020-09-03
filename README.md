# This is a Fork of kjerk's "sjisunzip" utility.

[See kjerk's original version here](https://github.com/kjerk/sjisunzip)

# Basic Usage

If you've attempted to unzip a file that was created on a Japanese computer, you might see files with completely garbled text.

In some cases, this might make the files unusuable (eg. If the contents of the files contain the names of other files, those references won't work anymore)


This can happen because the original file was created on a machine that has one default "encoding", and you've tried to unzip it on your own machine which has a different default "encoding", which gives you bad results.

Use this utility to unzip your file, using an appropriate encoding translation.

```
Usage:
  sjisunzip someFile.zip [toFolder]
  sjisunzip [-r] someFile.zip
    -r: Recode file to {filename}_utf8.zip
Examples:
  sjisunzip aFile.zip
  sjisunzip aFile.zip MyNewFolder
```

# Advanced Usage

```
Examples:
   sjisunzip SomeZipThatDidntWorkWithTheAboveSteps.zip --find_best_target
   sjisunzip SomeZipFileEncodedWithEncoding1234.zip -s:1234
   sjisunzip NothingElseIsWorkingWithThisZipFile.zip --all_sources -p
```

In some less common cases, the options above may not be enough to fix your file names. This is because we have to assume what encoding the file made with, and we may have guessed wrong.

If your folder still contains the wrong file names, try the `--find_best_target` command ling option, which will attempt to decode the target files with all the encoders available on the user's machine, rate the results, and then commit the operation using the "best" version. Useful for when the default UTF8 doesn't return correct results

If nothing else is working, you can use the `--all_sources` command line option, as a way to print out all the encodings available on your machine as well as some information about the sjisunzip's "score" for each encoding. You can then use the `-s:` command line option to try out one of those encodings as the "source encoding" (the encoding that you think the file was made with

For power users, you can also use `-t:` as the "Target encoding" (the encoding that you want to write the file to)

The command line option `-p` causes the program to halt upon completion, which can be useful for vieweing the output if you're running the script through a process where the window automatically closes when the program exits (such as if you set the program up as a "send to" windows target)

