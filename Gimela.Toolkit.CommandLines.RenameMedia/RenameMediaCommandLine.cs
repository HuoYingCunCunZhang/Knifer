﻿/*
* [The "BSD Licence"]
* Copyright (c) 2011-2015 Chundong Gao
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met:
* 1. Redistributions of source code must retain the above copyright
*    notice, this list of conditions and the following disclaimer.
* 2. Redistributions in binary form must reproduce the above copyright
*    notice, this list of conditions and the following disclaimer in the
*    documentation and/or other materials provided with the distribution.
* 3. The name of the author may not be used to endorse or promote products
*    derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
* IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
* IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
* NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
* DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
* THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
* THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Gimela.Toolkit.CommandLines.Foundation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Gimela.Toolkit.CommandLines.RenameMedia
{
  public class RenameMediaCommandLine : CommandLine
  {
    #region Fields

    private RenameMediaCommandLineOptions options;
    private readonly string executingFile = Assembly.GetExecutingAssembly().Location;
    private List<string> matchSaltList = new List<string>();

    #endregion

    #region Constructors

    public RenameMediaCommandLine(string[] args)
      : base(args)
    {
    }

    #endregion

    #region ICommandLine Members

    public override void Execute()
    {
      base.Execute();

      List<string> singleOptionList = RenameMediaOptions.GetSingleOptions();
      CommandLineOptions cloptions = CommandLineParser.Parse(Arguments.ToArray<string>(), singleOptionList.ToArray());
      options = ParseOptions(cloptions);
      CheckOptions(options);

      if (options.IsSetHelp)
      {
        RaiseCommandLineUsage(this, RenameMediaOptions.Usage);
      }
      else if (options.IsSetVersion)
      {
        RaiseCommandLineUsage(this, Version);
      }
      else
      {
        StartRename();
      }

      Terminate();
    }

    #endregion

    #region Private Methods

    private void StartRename()
    {
      try
      {
        string path = WildcardCharacterHelper.TranslateWildcardDirectoryPath(options.InputDirectory);
        SearchDirectory(path);
      }
      catch (CommandLineException ex)
      {
        RaiseCommandLineException(this, ex);
      }
    }

    private void SearchDirectory(string path)
    {
      DirectoryInfo directory = new DirectoryInfo(path);
      if (!directory.Exists)
      {
        throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
          "No such directory -- {0}", directory.FullName));
      }
      else
      {
        FileInfo[] files = directory.GetFiles();
        foreach (var file in files.OrderBy(f => f.LastWriteTime).ThenBy(f => f.Name))
        {
          RenameFile(file.FullName);
        }

        if (options.IsSetRecursive)
        {
          DirectoryInfo[] directories = directory.GetDirectories();
          foreach (var item in directories.OrderBy(d => d.Name))
          {
            SearchDirectory(item.FullName);
          }
        }
      }
    }

    private void RenameFile(string path)
    {
      FileInfo file = new FileInfo(path);
      if (!file.Exists)
      {
        throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
          "No such file -- {0}", file.FullName));
      }
      else
      {
        Regex r = new Regex(WildcardCharacterHelper.TranslateWildcardToRegex(options.RegexPattern), RegexOptions.IgnoreCase);
        Match m = r.Match(file.Name);
        if (m.Success)
        {
          string newName = "";
          if (string.IsNullOrWhiteSpace(options.Prefix))
          {
            newName = string.Format(CultureInfo.CurrentCulture,
              "{0}-{1}{2}",
              file.LastWriteTime.ToString(@"yyyyMMddHHmmss"),
              Guid.NewGuid(),
              file.Extension.ToLowerInvariant());
          }
          else
          {
            newName = string.Format(CultureInfo.CurrentCulture,
              "{0}-{1}-{2}{3}",
              options.Prefix,
              file.LastWriteTime.ToString(@"yyyyMMddHHmmss"),
              Guid.NewGuid(),
              file.Extension.ToLowerInvariant());
          }

          string newPath = Path.Combine(file.Directory.FullName, newName);

          file.MoveTo(newPath);

          OutputText(string.Format(CultureInfo.CurrentCulture, "File From: {0}", path));
          OutputText(string.Format(CultureInfo.CurrentCulture, "     To  : {0}", newPath));
        }
      }
    }

    #endregion

    #region Parse Options

    [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private static RenameMediaCommandLineOptions ParseOptions(CommandLineOptions commandLineOptions)
    {
      if (commandLineOptions == null)
        throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
          "Option used in invalid context -- {0}", "must specify a option."));

      RenameMediaCommandLineOptions targetOptions = new RenameMediaCommandLineOptions();

      if (commandLineOptions.Arguments.Count >= 0)
      {
        foreach (var arg in commandLineOptions.Arguments.Keys)
        {
          RenameMediaOptionType optionType = RenameMediaOptions.GetOptionType(arg);
          if (optionType == RenameMediaOptionType.None)
            throw new CommandLineException(
              string.Format(CultureInfo.CurrentCulture, "Option used in invalid context -- {0}",
              string.Format(CultureInfo.CurrentCulture, "cannot parse the command line argument : [{0}].", arg)));

          switch (optionType)
          {
            case RenameMediaOptionType.RegexPattern:
              targetOptions.RegexPattern = commandLineOptions.Arguments[arg];
              break;
            case RenameMediaOptionType.InputDirectory:
              targetOptions.InputDirectory = commandLineOptions.Arguments[arg];
              break;
            case RenameMediaOptionType.Recursive:
              targetOptions.IsSetRecursive = true;
              break;
            case RenameMediaOptionType.Prefix:
              targetOptions.Prefix = commandLineOptions.Arguments[arg];
              break;
            case RenameMediaOptionType.Help:
              targetOptions.IsSetHelp = true;
              break;
            case RenameMediaOptionType.Version:
              targetOptions.IsSetVersion = true;
              break;
          }
        }
      }

      return targetOptions;
    }

    private static void CheckOptions(RenameMediaCommandLineOptions checkedOptions)
    {
      if (!checkedOptions.IsSetHelp && !checkedOptions.IsSetVersion)
      {
        if (string.IsNullOrEmpty(checkedOptions.RegexPattern))
        {
          throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
            "Option used in invalid context -- {0}", "must specify a regex pattern."));
        }
        if (string.IsNullOrEmpty(checkedOptions.InputDirectory))
        {
          throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
            "Option used in invalid context -- {0}", "must specify a input directory."));
        }
        if (!Directory.Exists(checkedOptions.InputDirectory))
        {
          throw new CommandLineException(string.Format(CultureInfo.CurrentCulture,
            "Option used in invalid context -- {0}", "no such input directory."));
        }
      }
    }

    #endregion
  }
}
