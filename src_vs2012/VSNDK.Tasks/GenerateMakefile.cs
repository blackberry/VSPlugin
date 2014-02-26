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
using System.Collections;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Runtime.InteropServices;

namespace VSNDK.Tasks
{
    /// <summary>
    /// MSBuild Task for generating a make file for packaging the bar file. 
    /// </summary>
    public class GenerateMakefile : Task
    {
        #region Member Variables and Constants
        private string _projectDir;
        private string _intDir;
        private string _outDir;

        private const string INITIAL_DEFINITIONS =
            "RM := rm -rf\n\n" +

            "# Empty variable definitions.\n" +
            "O_SRCS := \n" +
            "CPP_SRCS := \n" +
            "C_UPPER_SRCS := \n" +
            "C_SRCS := \n" +
            "S_UPPER_SRCS := \n" +
            "OBJ_SRCS := \n" +
            "II_SRCS := \n" +
            "ASM_SRCS := \n" +
            "CXX_SRCS := \n" +
            "I_SRCS := \n" +
            "CC_SRCS := \n" +
            "OBJS := \n" +
            "C_DEPS := \n" +
            "CC_DEPS := \n" +
            "ARCHIVES := \n" +
            "CPP_DEPS := \n" +
            "I_DEPS := \n" +
            "CXX_DEPS := \n" +
            "C_UPPER_DEPS := \n" +
            "II_DEPS := \n\n" +

            "USER_OBJS := \n" +
            "LIBS := \n\n";

        private const string DEPENDENCY_INCLUDES =
            "# Include all dependency files\n" +
            "ifneq ($(MAKECMDGOALS),clean)\n" +
            "ifneq ($(strip $(C_DEPS)),)\n" +
            "-include $(C_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(CC_DEPS)),)\n" +
            "-include $(CC_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(CPP_DEPS)),)\n" +
            "-include $(CPP_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(I_DEPS)),)\n" +
            "-include $(I_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(CXX_DEPS)),)\n" +
            "-include $(CXX_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(C_UPPER_DEPS)),)\n" +
            "-include $(C_UPPER_DEPS)\n" +
            "endif\n" +
            "ifneq ($(strip $(II_DEPS)),)\n" +
            "-include $(II_DEPS)\n" +
            "endif\n" +
            "endif\n\n";
        #endregion

        #region properties

        /// <summary>
        /// Getter/Setter for CompileItems property
        /// </summary>
        public ITaskItem[] CompileItems
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for LinkItems
        /// </summary>
        public ITaskItem[] LinkItems
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for ProjectDir property
        /// </summary>
        public string ProjectDir
        {
            set { _projectDir = value.Replace('\\', '/'); }
            get { return _projectDir; }
        }

        /// <summary>
        /// Getter/Setter for IntDir property
        /// </summary>
        public string IntDir
        {
            set { _intDir = value.Replace('\\', '/'); }
            get { return _intDir; }
        }

        /// <summary>
        /// Getter/Setter for OutDir property
        /// </summary>
        public string OutDir
        {
            set { _outDir = value.Replace('\\', '/'); }
            get { return _outDir; }
        }

        /// <summary>
        /// Getter/Setter for AdditionalIncludeDirectories property 
        /// </summary>
        public string[] AdditionalIncludeDirectories
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for AdditionLibraryDirectories
        /// </summary>
        public string[] AdditionalLibraryDirectories
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for ExcludeDirectories
        /// </summary>
        public string[] ExcludeDirectories
        {
            set;
            get;
        }


        /// <summary>
        /// Getter/Setter for TargetName property
        /// </summary>
        public string TargetName
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for TargetExtension property
        /// </summary>
        public string TargetExtension
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for ConfigurationType property
        /// </summary>
        public string ConfigurationType
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for CompilerVersion property
        /// </summary>
        public string CompilerVersion
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for Platform property
        /// </summary>
        public string Platform
        {
            set;
            get;
        }

        #endregion

        /// <summary>
        /// Interface to unmanaged code for getting the SHortPathName for a given directory.
        /// </summary>
        /// <param name="path">Path to be converted</param>
        /// <param name="shortPath">Returned ShortPathName</param>
        /// <param name="shortPathLength">Length of the ShortPathName</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength
                 );

        /// <summary>
        /// Execute MSBuild Task
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            string targetString = TargetName + TargetExtension;
            targetString = targetString.Replace(".exe", "");
            string compilerFlag = "-V\"" + CompilerVersion;

            if (Platform == "BlackBerry")
                compilerFlag += ",gcc_ntoarmv7le\"";
            else if (Platform == "BlackBerrySimulator")
                compilerFlag += ",gcc_ntox86\"";

            using (StreamWriter outFile = new StreamWriter(IntDir + "makefile"))
            {
                outFile.Write(INITIAL_DEFINITIONS);
          //      System.Diagnostics.Debugger.Launch();
                foreach (ITaskItem compileItem in CompileItems)
                {
                    // Get the metadata we need from this compile item.
                    string id = compileItem.GetMetadata("Identity").Replace('\\', '/');
                    string filename = compileItem.GetMetadata("Filename");
                    string extension = compileItem.GetMetadata("Extension");
                    string relativeDir = compileItem.GetMetadata("RelativeDir").Replace('\\', '/');
                    string compileAs = compileItem.GetMetadata("CompileAs");
                    string warningLevel = compileItem.GetMetadata("WarningLevel");
                    string ansi = compileItem.GetMetadata("Ansi");
                    string optimizationLevel = compileItem.GetMetadata("OptimizationLevel");
                    string generateDebugInfo = compileItem.GetMetadata("GenerateDebugInformation");
                    string runtimeTypeInfo = compileItem.GetMetadata("RuntimeTypeInfo");
                    string enhancedSecurity = compileItem.GetMetadata("EnhancedSecurity");
                    string additionalOptions = compileItem.GetMetadata("AdditionalOptions");
                    string fullPath = compileItem.GetMetadata("FullPath").Replace('\\', '/');

                    /// if CompileItem is in the ExcludedPath then continue.
                    if (isExcludedPath(fullPath))
                    {

                        continue;
                    }

                    StringBuilder shortPath = new StringBuilder(1024);
                    GetShortPathName(fullPath, shortPath, shortPath.Capacity);
                    string fullPathShortened = shortPath.ToString();
                    
                    string handleExceptions = compileItem.GetMetadata("GccExceptionHandling");

                    string[] preprocessorDefs = compileItem.GetMetadata("PreprocessorDefinitions").Split(';');
                    string[] preprocessorUndefs = compileItem.GetMetadata("UndefinePreprocessorDefinitions").Split(';');

                    string[] additionalIncludeDirs = compileItem.GetMetadata("AdditionalIncludeDirectories").Split(';');
                    for (int i = 0; i < additionalIncludeDirs.Length; i++)
                    {
                        // Prepend ProjectDir to relative paths -- otherwise they will be interpreted
                        // as being relative to the build dir, e.g. ProjectDir/BlackBerry/Debug
                        if (additionalIncludeDirs[i] != "" && !additionalIncludeDirs[i].Contains(':'))
                            additionalIncludeDirs[i] = ProjectDir + additionalIncludeDirs[i];
                    }

                    // Add this compile item to the source definition.
                    if (compileAs == "CompileAsC")
                        outFile.Write("C_SRCS += ");
                    else if (compileAs == "CompileAsCpp")
                        outFile.Write("CPP_SRCS += ");

                    // Source file location.
                    outFile.WriteLine(fullPathShortened);

                    // Add this compile item's dependency file to the DEPS definition.
                    if (compileAs == "CompileAsC")
                        outFile.Write("C_DEPS += ");
                    else if (compileAs == "CompileAsCpp")
                        outFile.Write("CPP_DEPS += ");

                    outFile.WriteLine("./" + filename + ".d");

                    // Add the object file to the OBJS definition.
                    outFile.WriteLine("OBJS += ./" + filename + ".o\n");

                    // Now add a compile rule for this item.
                    outFile.WriteLine(filename + ".o: " + fullPathShortened);
                    outFile.Write("\tqcc -o $@ " + fullPathShortened + " " + compilerFlag + " -c -Wp,-MMD,$(basename $@).d -Wp,-MT,$@ ");

                    if (generateDebugInfo == "true")
                        outFile.Write("-g ");

                    Dictionary<string, string> warningMap = new Dictionary<string,string>();
                    warningMap.Add("TurnOffAllWarnings", "-w0 ");
                    warningMap.Add("Level1", "-w1 ");
                    warningMap.Add("Level2", "-w2 ");
                    warningMap.Add("Level3", "-w3 ");
                    warningMap.Add("Level4", "-w4 ");
                    warningMap.Add("Level5", "-w5 ");
                    warningMap.Add("Level6", "-w6 ");
                    warningMap.Add("Level7", "-w7 ");
                    warningMap.Add("Level8", "-w8 ");
                    warningMap.Add("Level9", "-w9 ");
                    warningMap.Add("EnableAllWarnings", "-Wall ");

                    string warningLevelSwitch;
                    warningMap.TryGetValue(warningLevel, out warningLevelSwitch);
                    outFile.Write(warningLevelSwitch);

                    if (compileAs == "CompileAsC")
                        outFile.Write("-lang-c ");
                    else if (compileAs == "CompileAsCpp")
                        outFile.Write("-lang-c++ ");

                    if (runtimeTypeInfo == "false" && compileAs == "CompileAsCpp")
                        outFile.Write("-fno-rtti ");

                    if (handleExceptions == "true")
                        outFile.Write("-fexceptions ");
                    else
                        outFile.Write("-fno-exceptions ");

                    if (enhancedSecurity == "true")
                        outFile.Write("-fstack-protector-all ");

                    // Note: Consider letting user decide between "-fpic" and "-fPIC"
                    // For now we use the safe, cross-platform "-fPIC"
                    if (ConfigurationType == "DynamicLibrary")
                        outFile.Write("-fPIC ");

                    foreach (string includeDir in AdditionalIncludeDirectories)
                    {
                        if (includeDir != "")
                            outFile.Write("-I\"" + includeDir + "\" ");
                    }

                    foreach (string includeDir in additionalIncludeDirs)
                    {
                        if (includeDir != "")
                            outFile.Write("-I\"" + includeDir + "\" ");
                    }

                    foreach (string def in preprocessorDefs)
                    {
                        if (def != "")
                            outFile.Write("-D" + def + " ");
                    }

                    foreach (string undef in preprocessorUndefs)
                    {
                        if (undef != "")
                            outFile.Write("-U" + undef + " ");
                    }

                    outFile.WriteLine(additionalOptions);
                    outFile.WriteLine();
                }

                outFile.Write(DEPENDENCY_INCLUDES);

                // Now write out all the targets.
                outFile.WriteLine("all: " + targetString);
                outFile.WriteLine();
                outFile.Write(targetString + ": $(OBJS) $(USER_OBJS) $(LIB_DEPS) ");
                outFile.WriteLine();

                string rootedOutDir = (Path.IsPathRooted(OutDir)) ? OutDir : ProjectDir + OutDir;

                StringBuilder shortOutPath = new StringBuilder(1024);
                GetShortPathName(rootedOutDir, shortOutPath, shortOutPath.Capacity);
                rootedOutDir = shortOutPath.ToString();

                if (ConfigurationType == "StaticLibrary")
                {
                    outFile.Write("\tqcc -A " + rootedOutDir + targetString + " $(OBJS) $(USER_OBJS) $(LIBS) " + compilerFlag + " -w1");
                }
                else if (ConfigurationType == "DynamicLibrary")
                {
                    // In case of reverse dependencies, may need to add "-Wl,-export-dynamic"
                    // Also note that the soname is currently the same as the target name, and no version information is added.
                    outFile.Write("\tqcc -shared -Wl,-soname," + targetString +
                                  " -o " + rootedOutDir + targetString +
                                  " $(OBJS) $(USER_OBJS) $(LIBS) " + compilerFlag);
                }
                else if (ConfigurationType == "Utility")
                {
                    // As far as I know, we don't support utilities.
                }
                else if (ConfigurationType == "Application")
                {
                    // For now, collect linker metadata from the first link item.
                    // (Why do we have multiple link items anyhow?)
                    string[] libs = LinkItems[0].GetMetadata("AdditionalDependencies").Split(';');
                    string generateDebugInfo = LinkItems[0].GetMetadata("GenerateDebugInformation");
                    string compileAs = LinkItems[0].GetMetadata("CompileAs");

                    string[] libDirs = LinkItems[0].GetMetadata("AdditionalLibraryDirectories").Split(';');
                    for (int i = 0; i < libDirs.Length; i++)
                    {
                        // Prepend ProjectDir to relative paths.
                        if (libDirs[i] != "" && !libDirs[i].Contains(':'))
                            libDirs[i] = ProjectDir + libDirs[i];
                    }

                    // This is the linker's tool invocation.
                    outFile.Write("\tqcc -o " + rootedOutDir + targetString + " $(OBJS) $(USER_OBJS) $(LIBS) " + compilerFlag + " ");

                    if (compileAs == "CompileAsCpp")
                        outFile.Write("-lang-c++ ");
                    else if (compileAs == "CompileAsC")
                        outFile.Write("-lang-c ");

                    if (generateDebugInfo == "true")
                        outFile.Write("-g ");

                    // For added security, remap some sections of ELF as read-only.
                    outFile.Write("-Wl,-z,relro,-z,now ");

                    // Visual Studio doesn't have separate lists for static vs. dynamic dependencies.
                    // To differentiate between the two, include the extension in the list.
                    // (.a for static, .so for dynamic)
                    // If no extension is included in the name it will be treated as the same type
                    // as the last declared type, and the default type is static.  This is the way
                    // qcc would work if you were using it from the command line with the
                    // "-Bstatic" and "-Bdynamic" flags.
                    for (int i = 0; i < libs.Length; i++)
                    {
                        if (libs[i].EndsWith(".so"))
                        {
                            outFile.Write("-Bdynamic ");
                            libs[i] = libs[i].Remove(libs[i].Length - 3);
                        }
                        else if (libs[i].EndsWith(".a"))
                        {
                            outFile.Write("-Bstatic ");
                            libs[i] = libs[i].Remove(libs[i].Length - 2);
                        }

                        if (libs[i] != "")
                            outFile.Write("-l" + libs[i] + " ");
                    }

                    foreach (string libDir in libDirs)
                    {
                        if (libDir != "")
                            outFile.Write("-L\"" + libDir + "\" ");
                    }

                    foreach (string libDir in AdditionalLibraryDirectories)
                    {
                        if (libDir != "")
                            outFile.Write("-L\"" + libDir + "\" ");
                    }
                }

                outFile.WriteLine("\n");
                outFile.WriteLine("clean:");
                outFile.Write("\t-$(RM) $(OBJS)$(C_DEPS)$(CC_DEPS)$(COM_QNX_QCC_OUTPUTTYPE_LINKER_OUTPUTS)$(CPP_DEPS)$(I_DEPS)$(CXX_DEPS)$(C_UPPER_DEPS)$(II_DEPS) " + rootedOutDir + targetString);

                if (ConfigurationType == "Application")
                {
                    outFile.WriteLine(" " + rootedOutDir + TargetName + ".bar");
                }

                outFile.WriteLine();
                outFile.WriteLine(".PHONY: all clean dependents\n");
            }

            return true;
        }

        /// <summary>
        /// Check to see if path is in the excluded list
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool isExcludedPath(string path)
        {
            if (ExcludeDirectories != null)
            {
                foreach (string exDir in ExcludeDirectories)
                {
                    if (Path.GetFullPath(path).Contains(Path.GetFullPath(exDir)))
                    {
                        return true;
                    }
/*                    else
                    {
                        return false;
                    }*/
                }
            }
            return false;
        }
    }
}
