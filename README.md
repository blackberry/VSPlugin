#BlackBerry Native Plug-in for Microsoft Visual Studio
The BlackBerry Native Plug-in for Microsoft Visual Studio integrates with your Microsoft Visual Studio development environment. You can use the BlackBerry Native Plug-in for Microsoft Visual Studio to develop C and C++ applications that target the BlackBerry 10 OS.

##License
All assets in this repository, unless otherwise stated through sub-directory LICENSE or NOTICE files, are subject to the Apache Software License v.2.0.

##Build requirements
Install
* [Microsoft Visual Studio Professional 2012 SDK] (http://www.microsoft.com/en-us/download/details.aspx?id=30668)

or

* [Microsoft Visual Studio Professional 2013 SDK] (http://www.microsoft.com/en-us/download/details.aspx?id=40758)

Please note, that currently there is a bug in DIA SDK of Visual Studio 2013.
If Visual Studio 2013 is installed on a machine, where Visual Studio 2012 did already existed, DIA SDK will be by placed inside it.
More info [here] (http://connect.microsoft.com/VisualStudio/feedback/details/814147/dia-sdk-installed-into-wrong-directory).

##Build commands
1. build.bat – A batch file that builds the various components of the BlackBerry Native Plug-in to the ..\_BuildResults folder.
2. setup.bat – A batch file that installs the newly built components of the BlackBerry Native Plug-in to the correct locations on your computer. 

##Contributing
The BlackBerry Native Plug-in for Microsoft Visual Studio project currently contains the following code branches: 

**Master Branch** - The master branch contains the latest production release of the source code. This code is considered stable and is fully tested by the Test team. You can [download the binaries] (http://developer.blackberry.com/native/downloads/) from the Downloads page on the BlackBerry developer website.   

**Next Branch** - The next branch contains the latest beta release of the source code. This code may be unstable and is not fully tested because it has passed unit tests only. At some point, a full regression test cycle is performed on the next branch and the code is promoted to the master branch.

**Feature Branch** - Feature branches contain the code changes required to implement specific features or issues being worked on for the next release. This code is considered unstable. These branches are denoted by the following naming scheme in the repository: next-### (where ### is the related issue number). When implementation is completed, the branch is unit and integration tested and then merged into the next branch.

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
* Although Microsoft Visual Studio 2010 is no longer supported, you can still [download the plug-in and the docs] (http://developer.blackberry.com/native/downloads/) from the Downloads page. 
