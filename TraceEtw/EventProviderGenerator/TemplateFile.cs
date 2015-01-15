﻿//
// Original code from the MSBuild Community Tasks:
// https://github.com/loresoft/msbuildtasks/blob/master/Source/MSBuild.Community.Tasks/TemplateFile/TemplateFile.cs
//
// License:
// https://github.com/loresoft/msbuildtasks/blob/master/LICENSE
//
//Copyright (c) 2014, LoreSoft
//All rights reserved.
//
//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met:
//
//* Redistributions of source code must retain the above copyright notice, this
//  list of conditions and the following disclaimer.
//
//* Redistributions in binary form must reproduce the above copyright notice,
//  this list of conditions and the following disclaimer in the documentation
//  and/or other materials provided with the distribution.
//
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
//AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
//IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
//OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace EventProviderGenerator
{
    /// <summary>
    /// MSBuild task that replaces tokens in a template file and writes out a new file.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// <ItemGroup>
    ///     <Tokens Include="Name">
    ///         <ReplacementValue>MSBuild Community Tasks</ReplacementValue>
    ///     </Tokens>
    /// </ItemGroup>
    /// 
    /// <TemplateFile Template="ATemplateFile.template" OutputFilename="ReplacedFile.txt" Tokens="@(Tokens)" />
    /// ]]></code>
    /// </example>
    /// <remarks>Tokens in the template file are formatted using ${var} syntax and names are not 
    /// case-sensitive, so ${Token} and ${TOKEN} are equivalent.</remarks>
    public class TemplateFile : Task
    {
        /// <summary>
        /// Meta data tag used for token replacement
        /// </summary>
        public static readonly string MetadataValueTag = "ReplacementValue";
        private ITaskItem _outputFile;
        private string _outputFilename;
        private Regex _regex;
        private ITaskItem _templateFile;
        private Dictionary<string, string> _tokenPairs;
        private ITaskItem[] _tokens;
        private static readonly string DefaultExt = ".out";

        /// <summary>
        /// Default constructor. Creates a new TemplateFile task.
        /// </summary>
        public TemplateFile()
        {
            _regex = new Regex(@"(?<token>\$\{(?<identifier>[^}]*)\})", RegexOptions.Singleline | RegexOptions.Compiled
                | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// The token replaced template file.
        /// </summary>
        [Output]
        public ITaskItem OutputFile
        {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The full path to the output file name.  If no filename is specified (the default) the
        /// output file will be the Template filename with a .out extension.
        /// </summary>
        public string OutputFilename
        {
            get { return _outputFilename; }
            set { _outputFilename = value; }
        }

        /// <summary>
        /// The template file used.  Tokens with values of ${Name} are replaced by name.
        /// </summary>
        [Required]
        public ITaskItem Template
        {
            get { return _templateFile; }
            set { _templateFile = value; }
        }

        /// <summary>
        /// List of tokens to replace in the template.  Token name is taken from the TaskItem.ItemSpec and the
        /// replacement value comes from the ReplacementValue metadata of the item.
        /// </summary>
        public ITaskItem[] Tokens
        {
            get { return _tokens; }
            set { _tokens = value; }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>Success or failure of the task.</returns>
        public override bool Execute()
        {
            bool result = false;
            if (File.Exists(_templateFile.ItemSpec))
            {
                ParseTokens();
                using (StreamReader reader = new StreamReader(_templateFile.ItemSpec))
                {
                    string text2 = _regex.Replace(reader.ReadToEnd(), new MatchEvaluator(MatchEval));
                    using (StreamWriter w = new StreamWriter(GetOutputFilename()))
                    {
                        w.Write(text2);
                        w.Flush();
                        Log.LogMessage("Template replaced and written to '{0}'", _outputFilename);
                        result = true;
                    }
                }
            }
            else
            {
                Log.LogError("Template File '{0}' cannot be found", _templateFile.ItemSpec);
            }
            return result;
        }

        private string GetOutputFilename()
        {
            if (string.IsNullOrEmpty(_outputFilename))
            {
                _outputFilename = Path.ChangeExtension(_templateFile.ItemSpec, DefaultExt);
            }
            _outputFilename = Path.IsPathRooted(_outputFilename) ? _outputFilename :
                Path.Combine(Path.GetDirectoryName(_templateFile.ItemSpec), _outputFilename);
            _outputFile = new TaskItem(_outputFilename);
            return _outputFilename;
        }

        private string MatchEval(Match match)
        {
            string result = match.Value;
            if (_tokenPairs.ContainsKey(match.Groups[2].Value))
            {
                result = _tokenPairs[match.Groups[2].Value];
            }
            return result;
        }

        private void ParseTokens()
        {
            _tokenPairs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (_tokens != null)
            {
                foreach (ITaskItem token in _tokens)
                {
                    if (!string.IsNullOrEmpty(token.ItemSpec))
                    {
                        _tokenPairs.Add(token.ItemSpec, token.GetMetadata(MetadataValueTag));
                    }
                }
            }
        }
    }
}
