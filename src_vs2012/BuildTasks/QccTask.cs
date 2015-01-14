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

using System;
using System.Collections;
using System.Resources;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    public abstract class QccTask : TrackedVCToolTask
    {
        #region Fields

        // Fields
        protected readonly ArrayList _switchOrderList;

        //constant fields
        private const string GENERATE_DEBUG_INFORMATION = "GenerateDebugInformation";
        private const string WARING_LEVEL = "WarningLevel";
        private const string TREAT_WARNING_AS_ERROR = "TreatWarningAsError";
        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";
        private const string OPTIMIZATION = "Optimization";
        private const string GCC_EXCEPTION_HANDLING = "GccExceptionHandling";
        protected const string SOURCES = "Sources";
        private const string ADDITIONAL_OPTIONS = "AdditionalOptions";
        private const string BUILDING_IN_IDE = "BuildingInIDE";
        private const string ENHANCED_SECURITY = "EnhancedSecurity";
        private const string POSITION_INDEPENDENT_EXECUTABLE = "PIE";
        private const string COMPILER_VERSION_TARGET = "CompilerVersionTarget";
        private const string VERBOSE = "Verbose";
        private const string COMPILE_AS = "CompileAs";

        private static string ERROR = " error:";
        private static string WARNING = " warning:";

        #endregion

        /// <summary>
        /// QnxTasks default constructor
        /// </summary>
        /// <param name="res"></param>
        public QccTask(ResourceManager res)
            : base(res)
        {
            _switchOrderList = new ArrayList();
            _switchOrderList.Add(COMPILER_VERSION_TARGET);
            _switchOrderList.Add("AlwaysAppend");
            _switchOrderList.Add(GENERATE_DEBUG_INFORMATION);
            _switchOrderList.Add(VERBOSE);
            _switchOrderList.Add(WARING_LEVEL);
            _switchOrderList.Add(TREAT_WARNING_AS_ERROR);
            _switchOrderList.Add(TRACKER_LOG_DIRECTORY);
            _switchOrderList.Add(OPTIMIZATION);
            _switchOrderList.Add(GCC_EXCEPTION_HANDLING);
            _switchOrderList.Add(ADDITIONAL_OPTIONS);
            _switchOrderList.Add(BUILDING_IN_IDE);
            _switchOrderList.Add(ENHANCED_SECURITY);
            _switchOrderList.Add(POSITION_INDEPENDENT_EXECUTABLE);
            _switchOrderList.Add(COMPILE_AS);
        }

        #region Overrides

        /// <summary>
        /// don't use response file for msbuild because it is removed before qcc to run GCC compiler 
        /// </summary>
        /// <param name="responseFilePath"></param>
        /// <returns></returns>
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        /// <summary>
        /// instead pass the response file to command line commands
        /// </summary>
        /// <returns></returns>
        protected override string GenerateCommandLineCommands()
        {
            string cmd = GenerateResponseFileCommands();
            return cmd;
        }

        /// <summary>
        /// Function to assign out of date sources
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        protected override ITaskItem[] AssignOutOfDateSources(ITaskItem[] sources)
        {
            ActiveToolSwitches[SOURCES].TaskItemArray = sources;
            return sources;
        }

        /// <summary>
        /// Getter/Setter for SwitchOrderList property
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get
            {
                return _switchOrderList;
            }
        }

        /// <summary>
        /// Getter/Setter for TrackedInputFiles property
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get
            {
                return Sources;
            }
        }

        /// <summary>
        /// Getter/Setter for TrackerIntermediateDirectory
        /// </summary>
        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Getter/Setter for ToolName property
        /// </summary>
        protected  override string ToolName
        {
            get { return QccExe; }
        }

        /// <summary>
        /// Getter/Setter for LogEventsFromTextOutput
        /// </summary>
        /// <param name="singleLine"></param>
        /// <param name="messageImportance"></param>
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            base.LogEventsFromTextOutput(Reformat(singleLine), messageImportance);
        }



        /// <summary>
        /// Reformat the qcc compiler error to msbuild known error format
        /// singeLine: main.c:53: error: expected '=', ',', ';', 'asm' or '__attribute__' before 'int'
        /// or        main.c:38:19: error: hello.h: No such file or directory
        /// output: main.c(53): error QCC001: expected '=', ',', ';', 'asm' or '__attribute__' before 'int'
        /// TODO: this method need to be updated in case any more error formats of qcc compiler 
        /// </summary>
        /// <param name="singleLine"></param>
        /// <returns></returns>
        private string Reformat(string singleLine)
        {
            string msbuilError = singleLine;
            string[] separators = { ERROR, WARNING };
            string errCode = singleLine.Contains(ERROR) ? "error qcc" : "warning qcc";
            string[] parts = singleLine.Split(separators, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                string[] segments = parts[0].Split(new[]{':'}, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 3)
                    msbuilError = String.Format("{0}({1}-{2}): {3}: {4}", segments[0], segments[1], segments[2], errCode, parts[1]);
                if ( segments.Length == 2 )
                    msbuilError = String.Format("{0}({1}): {2}: {3}", segments[0], segments[1], errCode, parts[1]);
            }
            return msbuilError;
        }

        /// <summary>
        /// getter/setter for UseUnicodeOutput
        /// </summary>
        protected override bool UseUnicodeOutput
        {
            get
            {
                return BuildingInIDE;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for GenerateDebugInformation property
        /// </summary>
        public bool GenerateDebugInformation
        {
            get
            {
                if (IsPropertySet(GENERATE_DEBUG_INFORMATION))
                {
                    return ActiveToolSwitches[GENERATE_DEBUG_INFORMATION].BooleanValue;
                }
                return false;
            }
            set
            {
                ActiveToolSwitches.Remove(GENERATE_DEBUG_INFORMATION);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Generate Debug Info",
                    Description = "Generate Debug Info (-g)",
                    ArgumentRelationList = new ArrayList()
                };
                switch2.SwitchValue = "-g";
                switch2.Name = GENERATE_DEBUG_INFORMATION;
                switch2.BooleanValue = value;
                ActiveToolSwitches.Add(GENERATE_DEBUG_INFORMATION, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for WarningLevel property
        /// </summary>
        public string WarningLevel
        {
            get
            {
                if (IsPropertySet(WARING_LEVEL))
                {
                    return ActiveToolSwitches[WARING_LEVEL].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(WARING_LEVEL);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Warning Level",
                    Description = "Select how strict you want the compiler to be about code errors.     (-w0 - -w4)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "TurnOffAllWarnings", "-w0" }, 
                    new[] { "Level1", "-w1" }, 
                    new[] { "Level2", "-w2" }, 
                    new[] { "Level3", "-w3" }, 
                    new[] { "Level4", "-w4" }, 
                    new[] { "Level5", "-w5" }, 
                    new[] { "Level6", "-w6" }, 
                    new[] { "Level7", "-w7" }, 
                    new[] { "Level8", "-w8" }, 
                    new[] { "Level9", "-w9" }, 
                    new[] { "EnableAllWarnings", "-Wall" }
                };
                switch2.SwitchValue = ReadSwitchMap(WARING_LEVEL, switchMap, value);
                switch2.Name = WARING_LEVEL;
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add(WARING_LEVEL, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for TreatWarningAsError property
        /// </summary>
        public bool TreatWarningAsError
        {
            get
            {
                if (IsPropertySet(TREAT_WARNING_AS_ERROR))
                {
                    return ActiveToolSwitches[TREAT_WARNING_AS_ERROR].BooleanValue;
                }
                return false;
            }
            set
            {
                ActiveToolSwitches.Remove(TREAT_WARNING_AS_ERROR);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Treat Warnings as Errors",
                    Description = "Treat Warnings as Errors",
                    ArgumentRelationList = new ArrayList()
                };
                switch2.ArgumentRelationList.Add(new ArgumentRelation(TREAT_WARNING_AS_ERROR, "true", false, ""));
                switch2.SwitchValue = "-Werror";
                switch2.Name = TREAT_WARNING_AS_ERROR;
                switch2.BooleanValue = value;
                ActiveToolSwitches.Add(TREAT_WARNING_AS_ERROR, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildingInIDE property
        /// </summary>
        public bool BuildingInIDE
        {
            get
            {
                return (IsPropertySet(BUILDING_IN_IDE) && ActiveToolSwitches[BUILDING_IN_IDE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(BUILDING_IN_IDE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    ArgumentRelationList = new ArrayList(),
                    Name = BUILDING_IN_IDE,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(BUILDING_IN_IDE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for GccExceptHandling property
        /// </summary>
        public string GccExceptionHandling
        {
            get
            {
                if (IsPropertySet("ExceptionHandling"))
                {
                    return ActiveToolSwitches["ExceptionHandling"].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove("ExceptionHandling");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Enable C++ Exceptions",
                    Description = "Specifies the model of exception handling to be used by the compiler.",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "Async", "/EHa" },
                    new[] { "Sync", "/EHsc" },
                    new[] { "SyncCThrow", "/EHs" },
                    new[] { "false", "" }
                };
                switch2.SwitchValue = ReadSwitchMap("ExceptionHandling", switchMap, value);
                switch2.Name = "ExceptionHandling";
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add("ExceptionHandling", switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for Optimization property
        /// </summary>
        public string Optimization
        {
            get
            {
                if (IsPropertySet(OPTIMIZATION))
                {
                    return ActiveToolSwitches[OPTIMIZATION].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(OPTIMIZATION);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = OPTIMIZATION,
                    Description = "Select option for code optimization; choose Custom to use specific optimization options.     (-O0, -O1, -O2, -O3)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "Disabled", "-O0" },
                    new[] { "MinSpace", "-O1" },
                    new[] { "MaxSpeed", "-O2" },
                    new[] { "Full", "-O3" }
                };
                switch2.SwitchValue = ReadSwitchMap(OPTIMIZATION, switchMap, value);
                switch2.Name = OPTIMIZATION;
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add(OPTIMIZATION, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }   
    
        /// <summary>
        /// Getter/Setter for PlatformToolset property
        /// </summary>
        public string PlatformToolset
        {
            get
            {
                return "qcc";
            }
        }

        /// <summary>
        /// Getter/Setter for TrackerLogDirectory
        /// </summary>
        public string TrackerLogDirectory
        {
            get
            {
                if (IsPropertySet(TRACKER_LOG_DIRECTORY))
                {
                    return ActiveToolSwitches[TRACKER_LOG_DIRECTORY].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(TRACKER_LOG_DIRECTORY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
                {
                    DisplayName = "Tracker Log Directory",
                    Description = "Tracker Log Directory.",
                    ArgumentRelationList = new ArrayList(),
                    Value = EnsureTrailingSlash(value)
                };
                ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for TargetMachine property
        /// </summary>
        public string TargetMachine
        {
            get
            {
                if (IsPropertySet("TargetMachine"))
                {
                    return ActiveToolSwitches["TargetMachine"].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove("TargetMachine");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Target Machine",
                    Description = "The /MACHINE option specifies the target platform for the program.",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "MachineARM", "/MACHINE:ARM" },
                    new[] { "MachineEBC", "/MACHINE:EBC" },
                    new[] { "MachineIA64", "/MACHINE:IA64" },
                    new[] { "MachineMIPS", "/MACHINE:MIPS" },
                    new[] { "MachineMIPS16", "/MACHINE:MIPS16" },
                    new[] { "MachineMIPSFPU", "/MACHINE:MIPSFPU" },
                    new[] { "MachineMIPSFPU16", "/MACHINE:MIPSFPU16" },
                    new[] { "MachineSH4", "/MACHINE:SH4" },
                    new[] { "MachineTHUMB", "/MACHINE:THUMB" },
                    new[] { "MachineX64", "/MACHINE:X64" },
                    new[] { "MachineX86", "/MACHINE:X86" }
                };
                switch2.SwitchValue = ReadSwitchMap("TargetMachine", switchMap, value);
                switch2.Name = "TargetMachine";
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add("TargetMachine", switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for CompileAs property
        /// </summary>
        public string CompileAs
        {
            get
            {
                if (IsPropertySet(COMPILE_AS))
                {
                    return ActiveToolSwitches[COMPILE_AS].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(COMPILE_AS);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Compile As",
                    Description = "Select compile language option for .c and .cpp files.     (-lang-c, -lang-c++)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "CompileAsC", "-lang-c" },
                    new[] { "CompileAsCpp", "-lang-c++" }
                };
                switch2.SwitchValue = ReadSwitchMap(COMPILE_AS, switchMap, value);
                switch2.Name = COMPILE_AS;
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add(COMPILE_AS, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for Verbose property
        /// </summary>
        public bool Verbose
        {
            get
            {
                return (IsPropertySet(VERBOSE) && ActiveToolSwitches[VERBOSE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(VERBOSE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Verbose",
                    Description = "Verbose",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-v",
                    Name = VERBOSE,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(VERBOSE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for EnhancedSecurity property
        /// </summary>
        public bool EnhancedSecurity
        {
            get
            {
                return (IsPropertySet(ENHANCED_SECURITY) && ActiveToolSwitches[ENHANCED_SECURITY].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(ENHANCED_SECURITY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Enhanced Security(-fstack-protector-all)",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = GetEnhancedSecuritySwitchValue(),
                    Name = ENHANCED_SECURITY,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(ENHANCED_SECURITY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Return the Get Enhanced Security string.  
        /// </summary>
        protected virtual string GetEnhancedSecuritySwitchValue()
        {
            return "-fstack-protector-all";
        }

        /// <summary>
        /// Getter/Setter for the PIE property
        /// </summary>
        public bool PIE
        {
            get
            {
                return (IsPropertySet(POSITION_INDEPENDENT_EXECUTABLE) && ActiveToolSwitches[POSITION_INDEPENDENT_EXECUTABLE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(POSITION_INDEPENDENT_EXECUTABLE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Position Independent Executable(fPIE)",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-fPIE",
                    Name = POSITION_INDEPENDENT_EXECUTABLE,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(POSITION_INDEPENDENT_EXECUTABLE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the ProfilingCall property
        /// </summary>
        public bool ProfilingCall { get; set; }

        /// <summary>
        /// Getter/Setter for the CodeCoverage property
        /// </summary>
        public bool CodeCoverage { get; set; }

        /// <summary>
        /// Getter/Setter for the Mudflap property
        /// </summary>
        public bool Mudflap { get; set; }

        /// <summary>
        /// Getter/Setter for the QccExe property
        /// </summary>
        public string QccExe { get; set; }

        /// <summary>
        /// Getter/Setter for the CompilerVersionTarget property
        /// </summary>
        public string CompilerVersionTarget
        {
            get
            {
                if (IsPropertySet(COMPILER_VERSION_TARGET))
                {
                    return ActiveToolSwitches[COMPILER_VERSION_TARGET].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(COMPILER_VERSION_TARGET);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Compiler version and target",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-V",
                };
                switch2.Name = COMPILER_VERSION_TARGET;
                switch2.Value = value;
                
                ActiveToolSwitches.Add(COMPILER_VERSION_TARGET, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the LinkSharedLibrary property
        /// </summary>
        public bool LinkSharedLibrary { get; set; }

        /// <summary>
        /// Getter/Setter for the Sources property
        /// </summary>
        [Required]
        public ITaskItem[] Sources
        {
            get
            {
                if (IsPropertySet(SOURCES))
                {
                    return ActiveToolSwitches[SOURCES].TaskItemArray;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(SOURCES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                ActiveToolSwitches.Add(SOURCES, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }
        #endregion
    }
}
