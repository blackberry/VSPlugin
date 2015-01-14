
This is a prototype of Qt4 application running on PlayBook and BB10 devices.

Starting from NDK 2.1+ and continuing in NDK 10.0+ BlackBerry delivers compiled set of Qt libraries
and matching headers. Unfortunately those libraries don't load at all on the device.

Please download custom build of Qt binaries from https://github.com/phofman/vs-plugin/releases
and extract it into the "QtLibs" folder inside the project. Otherwise deployment will fail.
That location can be changed directly inside bar-descriptor.xml.

Yet still, for compilation purposes the ones delivered by BlackBerry are sufficient.

