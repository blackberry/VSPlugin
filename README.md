#BlackBerry Native Plug-in for Microsoft Visual Studio
The BlackBerry Native Plug-in for Microsoft Visual Studio integrates with your Microsoft Visual Studio development environment. You can use the BlackBerry Native Plug-in for Microsoft Visual Studio to develop C and C++ applications that target the BlackBerry 10 OS.

##License
All assets in this repository, unless otherwise stated through sub-directory LICENSE or NOTICE files, are subject to the Apache Software License v.2.0.

##Build requirements
Install
* [Microsoft Visual Studio Professional 2013 SDK] (http://www.microsoft.com/en-us/download/details.aspx?id=40758)

Note: Currently, there is a bug in DIA SDK of Visual Studio 2013.
If Microsoft Visual Studio 2013 is installed on a machine where Microsoft Visual Studio 2012 was installed, the DIA SDK folder for Microsoft Visual Studio 2013 is installed in the DIA SDK folder for Microsoft Visual Studio 2012.
More info [here] (http://connect.microsoft.com/VisualStudio/feedback/details/814147/dia-sdk-installed-into-wrong-directory).

##Build commands
**build.bat** – A batch file that builds the various components of the BlackBerry Native Plug-in for Microsoft Visual Studio to the "_BuildResults" folder.

**setup.bat** – A batch file that installs the newly built components of the BlackBerry Native Plug-in for Microsoft Visual Studio to the correct locations on your computer. 

Examples:  

 **build.bat** - default, builds everything  
 **build.bat vs2010** - builds only for Microsoft Visual Studio 2010  
 **build.bat vs2012 "/out:D:\Shared folder\\_BuildResults"** - builds only for Microsoft Visual Studio 2012 into the specified directory  
 
 **setup.bat** - installs the plug-in for all Microsoft Visual Studio versions  
 **setup.bat vs2010** - installs the plug-in for only Microsoft Visual Studio 2010  
 **setup.bat vs2012 /no-tools** - installs the plug-in for Microsoft Visual Studio 2012 without copying the bbndk_vs and qnxtools folders  
 **setup.bat vs2012 /msbuild-only** - updates only the local MSBuild for Microsoft Visual Studio 2012 and adds the 'BlackBerry' and 'BlackBerrySimulator' target platforms

##Contributing
The BlackBerry Native Plug-in for Microsoft Visual Studio project currently contains the following code branches: 

**Master Branch** - This branch contains the latest production release of the source code. This code is considered stable and is fully tested by the test team. You can [download the binaries] (http://developer.blackberry.com/native/downloads/) from the Downloads page on the BlackBerry developer website.   

**Next Branch** - This branch contains the latest beta release of the source code. This code may be unstable and is not fully tested because it has passed unit tests only. At some point, a full regression test cycle is performed on the next branch and the code is promoted to the master branch.

**Feature Branch** - This branch contains the code changes required to implement specific features or issues being worked on for the next release. This code is considered unstable. These branches are denoted by the following naming scheme in the repository: next-### (where ### is the related issue number). When implementation is completed, the branch is unit and integration tested and then merged into the next branch.

**To contribute code to this repository you must be [signed up as an official contributor](http://blackberry.github.com/howToContribute.html).**

1. Fork the **VSPlugin** repository.
2. Make the changes/additions to your fork.
3. Send a pull request from your fork back to the **VSPlugin** repository.
4. If you made changes to code which you own, contact one of the Committers listed below to have your code merged.

## Code guidelines
* All functions should be prefaced by a standard Microsoft Visual Studio comment block detailing what the function does and including definitions of the parameters.
* Code lines should be indented four spaces per editor tab.
* Variable names should be in CamelCase format. For example, backColor.

## Committers
* [David Burgess](http://github.com/dbrgss)
* [Gustavo Arnold](http://github.com/guarnold)

## Reference material
* [Using Microsoft Visual Studio] (https://developer.blackberry.com/native/documentation/core/vs_using_microsoft_visual_studio.html)
* [GDB Commands] (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI.html)
