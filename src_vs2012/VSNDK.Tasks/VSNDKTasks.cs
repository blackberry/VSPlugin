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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.CPPTasks;
using System.Resources;
using System.Reflection;
using Microsoft.Build.Framework;
using System.Collections;
using System.IO;

namespace VSNDK.Tasks
{
    public abstract class VSNDKTasks : TrackedVCToolTask
    {
        #region fields
        // Fields
        protected ArrayList switchOrderList;

        //constant fields
        protected const string GENERATE_DEBUG_INFORMATION = "GenerateDebugInformation";
        protected const string WARING_LEVEL = "WarningLevel";
        protected const string TREAT_WARNING_AS_ERROR = "TreatWarningAsError";
        protected const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";
        protected const string OPTIMIZATION = "Optimization";
        protected const string GCC_EXCEPTION_HANDLING = "GccExceptionHandling";
        protected const string SOURCES = "Sources";
        protected const string ADDITIONAL_OPTIONS = "AdditionalOptions";
        protected const string BUILDING_IN_IDE = "BuildingInIDE";
        protected const string ENHANCED_SECURITY = "EnhancedSecurity";
        protected const string POSITION_INDEPENDENT_EXECUTABLE = "PIE";
        protected const string COMPILER_VERSION_TARGET = "CompilerVersionTarget";
        protected const string VERBOSE = "Verbose";
        protected const string COMPILE_AS = "CompileAs";

        private static string ERROR = " error:";
        private static string WARNING = " warning:";
        #endregion

        #region ctors

        /// <summary>
        /// VSNDKTasks Constructor
        /// </summary>
        /// <param name="res"></param>
        public VSNDKTasks(ResourceManager res)
            : base(res)
        {
            this.switchOrderList = new ArrayList();
            this.switchOrderList.Add(COMPILER_VERSION_TARGET);
            this.switchOrderList.Add("AlwaysAppend");
            this.switchOrderList.Add(GENERATE_DEBUG_INFORMATION);
            this.switchOrderList.Add(VERBOSE);
            this.switchOrderList.Add(WARING_LEVEL);
            this.switchOrderList.Add(TREAT_WARNING_AS_ERROR);
            this.switchOrderList.Add(TRACKER_LOG_DIRECTORY);
            this.switchOrderList.Add(OPTIMIZATION);
            this.switchOrderList.Add(GCC_EXCEPTION_HANDLING);
            this.switchOrderList.Add(ADDITIONAL_OPTIONS);
            this.switchOrderList.Add(BUILDING_IN_IDE);
            this.switchOrderList.Add(ENHANCED_SECURITY);
            this.switchOrderList.Add(POSITION_INDEPENDENT_EXECUTABLE);
            this.switchOrderList.Add(COMPILE_AS);
        }
        #endregion

        #region overrides
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
            base.ActiveToolSwitches[SOURCES].TaskItemArray = sources;
            return sources;
        }

        /// <summary>
        /// Getter/Setter for SwitchOrderList property
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get
            {
                return this.switchOrderList;
            }
        }

        /// <summary>
        /// Getter/Setter for TrackedInputFiles property
        /// </summary>
        protected override Microsoft.Build.Framework.ITaskItem[] TrackedInputFiles
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
                if (this.TrackerLogDirectory != null)
                {
                    return this.TrackerLogDirectory;
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
            if (parts != null && parts.Length > 1)
            {
                string[] segments = parts[0].Split(new Char[]{':'}, StringSplitOptions.RemoveEmptyEntries);
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
                return this.BuildingInIDE;
            }
        }

        #endregion //override

        #region properties

        /// <summary>
        /// Getter/Setter for GenerateDebugInformation property
        /// </summary>
        public virtual bool GenerateDebugInformation
        {
            get
            {
                if (base.IsPropertySet(GENERATE_DEBUG_INFORMATION))
                {
                    return base.ActiveToolSwitches[GENERATE_DEBUG_INFORMATION].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove(GENERATE_DEBUG_INFORMATION);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Generate Debug Info",
                    Description = "Generate Debug Info (-g)",
                    ArgumentRelationList = new ArrayList()
                };
                switch2.SwitchValue = "-g";
                switch2.Name = GENERATE_DEBUG_INFORMATION;
                switch2.BooleanValue = value;
                base.ActiveToolSwitches.Add(GENERATE_DEBUG_INFORMATION, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for WarningLevel property
        /// </summary>
        public virtual string WarningLevel
        {
            get
            {
                if (base.IsPropertySet(WARING_LEVEL))
                {
                    return base.ActiveToolSwitches[WARING_LEVEL].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(WARING_LEVEL);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Warning Level",
                    Description = "Select how strict you want the compiler to be about code errors.     (-w0 - -w4)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] 
                { new string[] { "TurnOffAllWarnings", "-w0" }, 
                    new string[] { "Level1", "-w1" }, 
                    new string[] { "Level2", "-w2" }, 
                    new string[] { "Level3", "-w3" }, 
                    new string[] { "Level4", "-w4" }, 
                    new string[] { "Level5", "-w5" }, 
                    new string[] { "Level6", "-w6" }, 
                    new string[] { "Level7", "-w7" }, 
                    new string[] { "Level8", "-w8" }, 
                    new string[] { "Level9", "-w9" }, 
                    new string[] { "EnableAllWarnings", "-Wall" } };
                switch2.SwitchValue = base.ReadSwitchMap(WARING_LEVEL, switchMap, value);
                switch2.Name = WARING_LEVEL;
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add(WARING_LEVEL, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for TreatWarningAsError property
        /// </summary>
        public virtual bool TreatWarningAsError
        {
            get
            {
                if (base.IsPropertySet(TREAT_WARNING_AS_ERROR))
                {
                    return base.ActiveToolSwitches[TREAT_WARNING_AS_ERROR].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove(TREAT_WARNING_AS_ERROR);
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
                base.ActiveToolSwitches.Add(TREAT_WARNING_AS_ERROR, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildingInIDE property
        /// </summary>
        public virtual bool BuildingInIDE
        {
            get
            {
                return (base.IsPropertySet(BUILDING_IN_IDE) && base.ActiveToolSwitches[BUILDING_IN_IDE].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(BUILDING_IN_IDE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    ArgumentRelationList = new ArrayList(),
                    Name = BUILDING_IN_IDE,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(BUILDING_IN_IDE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for GccExceptHandling property
        /// </summary>
        public virtual string GccExceptionHandling
        {
            get
            {
                if (base.IsPropertySet("ExceptionHandling"))
                {
                    return base.ActiveToolSwitches["ExceptionHandling"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ExceptionHandling");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Enable C++ Exceptions",
                    Description = "Specifies the model of exception handling to be used by the compiler.",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] { new string[] { "Async", "/EHa" }, new string[] { "Sync", "/EHsc" }, new string[] { "SyncCThrow", "/EHs" }, new string[] { "false", "" } };
                switch2.SwitchValue = base.ReadSwitchMap("ExceptionHandling", switchMap, value);
                switch2.Name = "ExceptionHandling";
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add("ExceptionHandling", switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for Optimization property
        /// </summary>
        public virtual string Optimization
        {
            get
            {
                if (base.IsPropertySet(OPTIMIZATION))
                {
                    return base.ActiveToolSwitches[OPTIMIZATION].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(OPTIMIZATION);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = OPTIMIZATION,
                    Description = "Select option for code optimization; choose Custom to use specific optimization options.     (-O0, -O1, -O2, -O3)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] { new string[] { "Disabled", "-O0" }, new string[] { "MinSpace", "-O1" }, 
                    new string[] { "MaxSpeed", "-O2" }, new string[] { "Full", "-O3" } };
                switch2.SwitchValue = base.ReadSwitchMap(OPTIMIZATION, switchMap, value);
                switch2.Name = OPTIMIZATION;
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add(OPTIMIZATION, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }   
    
        /// <summary>
        /// Getter/Setter for PlatformToolset property
        /// </summary>
        public virtual string PlatformToolset
        {
            get
            {
                return "qcc";
            }
        }

        /// <summary>
        /// Getter/Setter for TrackerLogDirectory
        /// </summary>
        public virtual string TrackerLogDirectory
		{
			get
			{
				if ( base.IsPropertySet( TRACKER_LOG_DIRECTORY ) )
				{
					return base.ActiveToolSwitches[TRACKER_LOG_DIRECTORY].Value;
				}
				return null;
			}
			set
			{
				base.ActiveToolSwitches.Remove( TRACKER_LOG_DIRECTORY );
				ToolSwitch switch2 = new ToolSwitch( ToolSwitchType.Directory )
				{
					DisplayName = "Tracker Log Directory",
					Description = "Tracker Log Directory.",
					ArgumentRelationList = new ArrayList(),
					Value = VCToolTask.EnsureTrailingSlash( value )
				};
				base.ActiveToolSwitches.Add( TRACKER_LOG_DIRECTORY, switch2 );
				base.AddActiveSwitchToolValue( switch2 );
			}
		}

        /// <summary>
        /// Getter/Setter for TargetMachine property
        /// </summary>
        public virtual string TargetMachine
        {
            get
            {
                if (base.IsPropertySet("TargetMachine"))
                {
                    return base.ActiveToolSwitches["TargetMachine"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TargetMachine");
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Target Machine",
                    Description = "The /MACHINE option specifies the target platform for the program.",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] { new string[] { "MachineARM", "/MACHINE:ARM" }, new string[] { "MachineEBC", "/MACHINE:EBC" }, new string[] { "MachineIA64", "/MACHINE:IA64" }, new string[] { "MachineMIPS", "/MACHINE:MIPS" }, new string[] { "MachineMIPS16", "/MACHINE:MIPS16" }, new string[] { "MachineMIPSFPU", "/MACHINE:MIPSFPU" }, new string[] { "MachineMIPSFPU16", "/MACHINE:MIPSFPU16" }, new string[] { "MachineSH4", "/MACHINE:SH4" }, new string[] { "MachineTHUMB", "/MACHINE:THUMB" }, new string[] { "MachineX64", "/MACHINE:X64" }, new string[] { "MachineX86", "/MACHINE:X86" } };
                switch2.SwitchValue = base.ReadSwitchMap("TargetMachine", switchMap, value);
                switch2.Name = "TargetMachine";
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add("TargetMachine", switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for CompileAs property
        /// </summary>
        public virtual string CompileAs
        {
            get
            {
                if (base.IsPropertySet(COMPILE_AS))
                {
                    return base.ActiveToolSwitches[COMPILE_AS].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(COMPILE_AS);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Compile As",
                    Description = "Select compile language option for .c and .cpp files.     (-lang-c, -lang-c++)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] { new string[] { "CompileAsC", "-lang-c" }, new string[] { "CompileAsCpp", "-lang-c++" } };
                switch2.SwitchValue = base.ReadSwitchMap(COMPILE_AS, switchMap, value);
                switch2.Name = COMPILE_AS;
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add(COMPILE_AS, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for Verbose property
        /// </summary>
        public bool Verbose
        {
            get
            {
                return (base.IsPropertySet(VERBOSE) && base.ActiveToolSwitches[VERBOSE].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(VERBOSE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Verbose",
                    Description = "Verbose",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-v",
                    Name = VERBOSE,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(VERBOSE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for EnhancedSecutiry property
        /// </summary>
        public virtual bool EnhancedSecurity
        {
            get
            {
                return (base.IsPropertySet(ENHANCED_SECURITY) && base.ActiveToolSwitches[ENHANCED_SECURITY].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(ENHANCED_SECURITY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Enhanced Security(-fstack-protector-all)",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = GetEnhancedSecuritySwitchValue(),
                    Name = ENHANCED_SECURITY,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(ENHANCED_SECURITY, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Return the Get Enhanced Security string.  
        /// </summary>
        /// <returns></returns>
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
                return (base.IsPropertySet(POSITION_INDEPENDENT_EXECUTABLE) && base.ActiveToolSwitches[POSITION_INDEPENDENT_EXECUTABLE].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(POSITION_INDEPENDENT_EXECUTABLE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Position Independent Executable(fPIE)",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-fPIE",
                    Name = POSITION_INDEPENDENT_EXECUTABLE,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(POSITION_INDEPENDENT_EXECUTABLE, switch2);
                base.AddActiveSwitchToolValue(switch2);
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
                if (base.IsPropertySet(COMPILER_VERSION_TARGET))
                {
                    return base.ActiveToolSwitches[COMPILER_VERSION_TARGET].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(COMPILER_VERSION_TARGET);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Compiler version and target",
                    Description = " ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-V",
                };
                switch2.Name = COMPILER_VERSION_TARGET;
                switch2.Value = value;
                
                base.ActiveToolSwitches.Add(COMPILER_VERSION_TARGET, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the LinkSharedLibrary property
        /// </summary>
        public virtual bool LinkSharedLibrary { get; set; }

        /// <summary>
        /// Getter/Setter for the Sources property
        /// </summary>
        [Required]
        public virtual ITaskItem[] Sources
        {
            get
            {
                if (base.IsPropertySet(SOURCES))
                {
                    return base.ActiveToolSwitches[SOURCES].TaskItemArray;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(SOURCES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                base.ActiveToolSwitches.Add(SOURCES, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }
        #endregion //properties
    }
}
