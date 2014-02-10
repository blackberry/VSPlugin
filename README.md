#BlackBerry Native Plug-in for Microsoft Visual Studio
The BlackBerry Native Plug-in for Microsoft Visual Studio integrates with your Microsoft Visual Studio development environment. You can use the BlackBerry Native Plug-in for Microsoft Visual Studio to develop C and C++ applications that target the BlackBerry 10 OS.

##License
All assets in this repository, unless otherwise stated through sub-directory LICENSE or NOTICE files, are subject to the Apache Software License v.2.0.

##Build Requirements
1. Install Microsoft Visual Studio 2010
2. Install [Visual Studio SP1 SDK] (http://www.microsoft.com/en-us/download/details.aspx?id=21835)

##Build Commands
1. VSNDKPluginBuild.bat – A batch file that builds the various components of the BlackBerry Native Plug-in to the ..\buildresults folder.
2. VSNDKPluginSetup.bat – A batch file that installs the newly built components of the BlackBerry Native Plug-in to the correct locations on your computer. 

##Contributing
The BlackBerry Native Plug-in for Microsoft Visual Studio project currently contains the following code branches; 

**Master Branch** - Contains the latest production release (stable) of the source code.  This branch is fully tested by the Test team and is the same code that is used to build the officially released version available on the BlackBerry website.   

**Next Branch** - Contains the latest working release (possibly unstable) of the source code.  The code is not have been fully tested and has passed unit tests only.  At some point a full regression test cycle will be performed on the ‘next’ branch and the code will be promoted to the ‘master’ branch.

**Feature Branch** - Contain the code changes required to implement specific features or issues being worked on for the ‘next’ release. (unstable)  These branches are denoted by the following naming scheme in the repository: next-### where ### is the related Issue number.   When implementation is complete, the branch is unit and integration tested and then merged into the ‘next’ branch.

**To contribute code to this repository you must be [signed up as an official contributor](http://blackberry.github.com/howToContribute.html).**

1. Fork the **VSPlugin** repository
2. Make the changes/additions to your fork
3. Send a pull request from your fork back to the **VSPlugin** repository
4. If you made changes to code which you own, send a message via github messages to one of the Committers listed below to have your code merged.

## Code Guidelines
* All functions should be prefaced by a standard Microsoft Visual Studio comment block detailing what the function does and including definitions of the parameters.
* Code lines should be indented four spaces per editor tab.
* Variable names should be in CamelCase format. For example, backColor.

## Committers
* [David Burgess](http://github.com/dbrgss)
* [Gustavo Arnold](http://github.com/guarnold)

## Reference Material
* [Using Microsoft Visual Studio] (https://developer.blackberry.com/native/documentation/core/vs_using_microsoft_visual_studio.html)
