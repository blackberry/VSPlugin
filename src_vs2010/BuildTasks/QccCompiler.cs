//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System.Collections.Generic;
using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
using System.Collections;

namespace BlackBerry.BuildTasks
{
    public sealed class QccCompiler : QccTask
    {
        #region Fields

        // Fields
        private ITaskItem[] _preprocessOutput;

        //constant fields
        private const string ADDITIONAL_INCLUDEDIRECTORIES = "AdditionalIncludeDirectories";
        private const string PREPROCESSOR_DEFINITIONS = "PreprocessorDefinitions";
        private const string UNDEFINE_PREPROCESSOR_DEFINITION = "UndefinePreprocessorDefinition";
        private const string PREPROCESS_TO_FILE = "PreprocessToFile";
        private const string PREPROCESS_TO_STDOUT = "PreprocessToStdout";
        private const string PREPROCESS_KEEP_COMMENTS = "PreprocessKeepComments";
        private const string RUNTIME_TYPE_INFO = "RuntimeTypeInfo";
        private const string OBJECT_FILE_NAME = "ObjectFileName";
        private const string ANSI = "Ansi";

        #endregion

        /// <summary>
        /// QccCompiler default constructor
        /// </summary>
        public QccCompiler()
            : base(Resources.ResourceManager)
        {
            _preprocessOutput = new ITaskItem[0];
            _switchOrderList.Add(ADDITIONAL_INCLUDEDIRECTORIES);
            _switchOrderList.Add(ANSI);
            _switchOrderList.Add(PREPROCESSOR_DEFINITIONS);
            _switchOrderList.Add(UNDEFINE_PREPROCESSOR_DEFINITION);
            _switchOrderList.Add(PREPROCESS_TO_FILE);
            _switchOrderList.Add(PREPROCESS_TO_STDOUT);
            _switchOrderList.Add(PREPROCESS_KEEP_COMMENTS);
            _switchOrderList.Add(RUNTIME_TYPE_INFO);
            _switchOrderList.Add(OBJECT_FILE_NAME);
            _switchOrderList.Add(SOURCES);
        }

        #region Overrides

        /// <summary>
        /// Getter/Setter for the AlwaysAppend property
        /// </summary>
        protected override string AlwaysAppend
        {
            get
            {
                return LinkSharedLibrary ? "-c -shared" : "-c";
            }
        }

        /// <summary>
        /// Getter/Setter for the AttributeFileTracking property
        /// </summary>
        public override bool AttributeFileTracking
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Getter/Setter for the TrackedInputFiles property
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get
            {
                return Sources;
            }
        }

        /// <summary>
        /// Execute the specified tool for building
        /// </summary>
        /// <param name="pathToTool"></param>
        /// <param name="responseFileCommands"></param>
        /// <param name="commandLineCommands"></param>
        /// <returns></returns>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            foreach (ITaskItem item in Sources)
            {
                string source = item.ItemSpec;
                base.LogEventsFromTextOutput(source, MessageImportance.High);
            }
            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        /// <summary>
        /// Getter/Setter for the WriteTLogNames property
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "qcc_compiler.write.1.tlog", "qcc_compiler.*.write.1.tlog" };
            }
        }

        /// <summary>
        /// Getter/Setter for the CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get
            {
                return "qcc_compiler.command.1.tlog";
            }
        }

        /// <summary>
        /// Getter/Setter for the ReadTLogNames property
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get
            {
                return new[] { "qcc_compiler.read.1.tlog", "qcc_compiler.*.read.1.tlog" };
            }
        }
        #endregion

        #region Properties

        /// <summary>
        /// GetterSetter for the AdditionalIncludeDirectories property.
        /// </summary>
        public string[] AdditionalIncludeDirectories
        {
            get
            {
                if (IsPropertySet(ADDITIONAL_INCLUDEDIRECTORIES))
                {
                    return ActiveToolSwitches[ADDITIONAL_INCLUDEDIRECTORIES].StringList;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(ADDITIONAL_INCLUDEDIRECTORIES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Additional Include Directories",
                    Description = "Specifies one or more directories to add to the include path; separate with semi-colons if more than one.     (-I[path])",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-I",
                    Name = ADDITIONAL_INCLUDEDIRECTORIES,
                    StringList = value
                };
                ActiveToolSwitches.Add(ADDITIONAL_INCLUDEDIRECTORIES, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the Ansi property
        /// </summary>
        public bool Ansi
        {
            get
            {
                return (IsPropertySet(ANSI) && ActiveToolSwitches[ANSI].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(ANSI);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Compile ANSI code",
                    Description = "Compile ansi code",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-ansi",
                    Name = ANSI,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(ANSI, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the MinimalRebuild property
        /// </summary>
        public bool MinimalRebuild
        {
            get
            {
                return (IsPropertySet("MinimalRebuild") && ActiveToolSwitches["MinimalRebuild"].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove("MinimalRebuild");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Enable Minimal Rebuild",
                    Description = "Enables minimal rebuild, which determines whether C++ source files that include changed C++ class definitions (stored in header (.h) files) need to be recompiled.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/Gm",
                    ReverseSwitchValue = "/Gm-",
                    Name = "MinimalRebuild",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add("MinimalRebuild", switch2);
                AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("Gm", "Gm-"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("Gm-", "Gm"));
            }
        }

        /// <summary>
        /// Getter/Setter for the ObjectFileName property 
        /// </summary>
        public string ObjectFileName
        {
            get
            {
                if (IsPropertySet(OBJECT_FILE_NAME))
                {
                    return ActiveToolSwitches[OBJECT_FILE_NAME].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(OBJECT_FILE_NAME);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Object File Name",
                    Description = "Specifies a name to override the default object file name; can be file or directory name.     (-o[name])",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-o",
                    Name = OBJECT_FILE_NAME,
                    Value = value
                };
                ActiveToolSwitches.Add(OBJECT_FILE_NAME, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the ExpandAttributedSource property 
        /// </summary>
        public bool ExpandAttributedSource
        {
            get
            {
                return (IsPropertySet("ExpandAttributedSource") && ActiveToolSwitches["ExpandAttributedSource"].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove("ExpandAttributedSource");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Expand Attributed Source",
                    Description = "Create listing file with expanded attributes injected into source file.     (/Fx)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/Fx",
                    Name = "ExpandAttributedSource",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add("ExpandAttributedSource", switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the FavorSizeOrSpeed property
        /// </summary>
        public string FavorSizeOrSpeed
        {
            get
            {
                if (IsPropertySet("FavorSizeOrSpeed"))
                {
                    return ActiveToolSwitches["FavorSizeOrSpeed"].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove("FavorSizeOrSpeed");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Favor Size Or Speed",
                    Description = "Whether to favor code size or code speed; 'Global Optimization' must be turned on.     (/Ot, /Os)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "Size", "/Os" },
                    new[] { "Speed", "/Ot" },
                    new[] { "Neither", "" }
                };
                switch2.SwitchValue = ReadSwitchMap("FavorSizeOrSpeed", switchMap, value);
                switch2.Name = "FavorSizeOrSpeed";
                switch2.Value = value;
                switch2.MultipleValues = true;
                ActiveToolSwitches.Add("FavorSizeOrSpeed", switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the PreprocessKeepComments property
        /// </summary>
        public bool PreprocessKeepComments
        {
            get
            {
                return (IsPropertySet(PREPROCESS_KEEP_COMMENTS) && ActiveToolSwitches[PREPROCESS_KEEP_COMMENTS].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(PREPROCESS_KEEP_COMMENTS);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Keep Comments",
                    Description = "Suppresses comment strip from source code; requires that one of the 'Preprocessing' options be set.     (-C)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-C",
                    Name = PREPROCESS_KEEP_COMMENTS,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(PREPROCESS_KEEP_COMMENTS, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for hte PreprocessorDefinitions property
        /// </summary>
        public string[] PreprocessorDefinitions
        {
            get
            {
                if (IsPropertySet(PREPROCESSOR_DEFINITIONS))
                {
                    return ActiveToolSwitches[PREPROCESSOR_DEFINITIONS].StringList;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PREPROCESSOR_DEFINITIONS);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Preprocessor Definitions",
                    Description = "Defines a preprocessing symbols for your source file.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-D",
                    Name = PREPROCESSOR_DEFINITIONS,
                    StringList = value
                };
                ActiveToolSwitches.Add(PREPROCESSOR_DEFINITIONS, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the PreprocessOutput property
        /// </summary>
        [Output]
        public ITaskItem[] PreprocessOutput
        {
            get
            {
                return _preprocessOutput;
            }
            set
            {
                _preprocessOutput = value;
            }
        }

        /// <summary>
        /// Getter/Setter for the PreprocessOutputPath
        /// </summary>
        public string PreprocessOutputPath
        {
            get
            {
                if (IsPropertySet(PREPROCESS_TO_STDOUT))
                {
                    return ActiveToolSwitches[PREPROCESS_TO_STDOUT].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PREPROCESS_TO_STDOUT);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Preprocess Output Path",
                    Description = "Specify the output path for the preprocessor. The default location is the same as the source file(s).",
                    ArgumentRelationList = new ArrayList(),
                    Name = PREPROCESS_TO_STDOUT,
                    Value = value,
                    SwitchValue = "E"
                };
                ActiveToolSwitches.Add(PREPROCESS_TO_STDOUT, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the PreprocessSuppressLineNumbers property
        /// </summary>
        public bool PreprocessSuppressLineNumbers
        {
            get
            {
                return (IsPropertySet("PreprocessSuppressLineNumbers") && ActiveToolSwitches["PreprocessSuppressLineNumbers"].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove("PreprocessSuppressLineNumbers");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Preprocess Suppress Line Numbers",
                    Description = "Preprocess without #line directives.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/EP",
                    Name = "PreprocessSuppressLineNumbers",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add("PreprocessSuppressLineNumbers", switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the PreprocessToFile property
        /// </summary>
        public bool PreprocessToFile
        {
            get
            {
                return (IsPropertySet(PREPROCESS_TO_FILE) && ActiveToolSwitches[PREPROCESS_TO_FILE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(PREPROCESS_TO_FILE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Preprocess to a File",
                    Description = "Preprocesses C and C++ source files and writes the preprocessed output to a file. This option suppresses compilation, thus it does not produce an .obj file.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-P",
                    Name = PREPROCESS_TO_FILE,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(PREPROCESS_TO_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the UndefinePreprocessorDefinition property
        /// </summary>
        public string[] UndefinePreprocessorDefinition
        {
            get
            {
                if (IsPropertySet(UNDEFINE_PREPROCESSOR_DEFINITION))
                {
                    return ActiveToolSwitches[UNDEFINE_PREPROCESSOR_DEFINITION].StringList;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(UNDEFINE_PREPROCESSOR_DEFINITION);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Undefine Preprocessor Definitions",
                    Description = "Specifies one or more preprocessor undefines.     (-U[macro])",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-U",
                    Name = UNDEFINE_PREPROCESSOR_DEFINITION,
                    StringList = value
                };
                ActiveToolSwitches.Add(UNDEFINE_PREPROCESSOR_DEFINITION, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        public bool RuntimeTypeInfo { get; set; }
        public bool ShortEnums { get; set; }

        #endregion
    }
}
