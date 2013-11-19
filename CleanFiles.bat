set thisDir=%~dp0
set vs2010="%thisDir%\src"
set vs2012="%thisDir%\src_vs2012"
set vs2013="%thisDir%\src_vs2013"
set samples="%thisDir%\samples"

rd "%vs2010%\Debug" /s /q
rd "%vs2010%\ipch" /s /q
rd "%vs2010%\Release" /s /q
del "%vs2010%\VSNDK.suo" 
del "%vs2010%\VSNDK.vll.suo"

rd "%vs2010%\GDBParser\Debug" /s /q
rd "%vs2010%\GDBParser\Release" /s /q

rd "%vs2010%\GDBParser.UnitTests\bin" /s /q
rd "%vs2010%\GDBParser.UnitTests\obj" /s /q
rd "%vs2010%\GDBParser.UnitTests\Service References" /s /q

rd "%vs2010%\GDBWrapper\Debug" /s /q
rd "%vs2010%\GDBWrapper\Release" /s /q

rd "%vs2010%\VSNDK.AddIn\bin" /s /q
rd "%vs2010%\VSNDK.AddIn\obj" /s /q

rd "%vs2010%\VSNDK.DebugEngine\bin" /s /q
rd "%vs2010%\VSNDK.DebugEngine\obj" /s /q
rd "%vs2010%\VSNDK.DebugEngine\Service References" /s /q

rd "%vs2010%\VSNDK.Package\bin" /s /q
rd "%vs2010%\VSNDK.Package\obj" /s /q

rd "%vs2010%\VSNDK.Package.Test\bin" /s /q
rd "%vs2010%\VSNDK.Package.Test\obj" /s /q

rd "%vs2010%\VSNDK.Tasks\bin" /s /q
rd "%vs2010%\VSNDK.Tasks\obj" /s /q

rd "%vs2010%\VSNDK.Tasks.Test\bin" /s /q
rd "%vs2010%\VSNDK.Tasks.Test\obj" /s /q

rd "%vs2012%\Debug" /s /q
rd "%vs2012%\ipch" /s /q
rd "%vs2012%\Release" /s /q
del "%vs2012%\VSNDK.suo" 
del "%vs2012%\VSNDK.vll.suo"

rd "%vs2012%\GDBParser\Debug" /s /q
rd "%vs2012%\GDBParser\Release" /s /q

rd "%vs2012%\GDBParser.UnitTests\bin" /s /q
rd "%vs2012%\GDBParser.UnitTests\obj" /s /q
rd "%vs2012%\GDBParser.UnitTests\Service References" /s /q

rd "%vs2012%\GDBWrapper\Debug" /s /q
rd "%vs2012%\GDBWrapper\Release" /s /q

rd "%vs2012%\VSNDK.AddIn\bin" /s /q
rd "%vs2012%\VSNDK.AddIn\obj" /s /q

rd "%vs2012%\VSNDK.DebugEngine\bin" /s /q
rd "%vs2012%\VSNDK.DebugEngine\obj" /s /q
rd "%vs2012%\VSNDK.DebugEngine\Service References" /s /q

rd "%vs2012%\VSNDK.Package\bin" /s /q
rd "%vs2012%\VSNDK.Package\obj" /s /q

rd "%vs2012%\VSNDK.Package.Test\bin" /s /q
rd "%vs2012%\VSNDK.Package.Test\obj" /s /q

rd "%vs2012%\VSNDK.Tasks\bin" /s /q
rd "%vs2012%\VSNDK.Tasks\obj" /s /q

rd "%vs2012%\VSNDK.Tasks.Test\bin" /s /q
rd "%vs2012%\VSNDK.Tasks.Test\obj" /s /q

rd "%vs2013%\Debug" /s /q
rd "%vs2013%\ipch" /s /q
rd "%vs2013%\Release" /s /q
del "%vs2013%\VSNDK.suo" 
del "%vs2013%\VSNDK.vll.suo"

rd "%vs2013%\GDBParser\Debug" /s /q
rd "%vs2013%\GDBParser\Release" /s /q

rd "%vs2013%\GDBParser.UnitTests\bin" /s /q
rd "%vs2013%\GDBParser.UnitTests\obj" /s /q
rd "%vs2013%\GDBParser.UnitTests\Service References" /s /q

rd "%vs2013%\GDBWrapper\Debug" /s /q
rd "%vs2013%\GDBWrapper\Release" /s /q

rd "%vs2013%\VSNDK.AddIn\bin" /s /q
rd "%vs2013%\VSNDK.AddIn\obj" /s /q

rd "%vs2013%\VSNDK.DebugEngine\bin" /s /q
rd "%vs2013%\VSNDK.DebugEngine\obj" /s /q
rd "%vs2013%\VSNDK.DebugEngine\Service References" /s /q

rd "%vs2013%\VSNDK.Package\bin" /s /q
rd "%vs2013%\VSNDK.Package\obj" /s /q

rd "%vs2013%\VSNDK.Package.Test\bin" /s /q
rd "%vs2013%\VSNDK.Package.Test\obj" /s /q

rd "%vs2013%\VSNDK.Tasks\bin" /s /q
rd "%vs2013%\VSNDK.Tasks\obj" /s /q

rd "%vs2013%\VSNDK.Tasks.Test\bin" /s /q
rd "%vs2013%\VSNDK.Tasks.Test\obj" /s /q

rd "%samples%\FallingBlocks\FallingBlocks\Device-Debug" /s /q
del "%samples%\FallingBlocks\FallingBlocks.sdf"
del "%samples%\FallingBlocks\FallingBlocks.suo"
del ""%samples%\FallingBlocks\FallingBlocks\CompileRan"


rd "%samples%\HelloWorldDisplay\HelloWorldDisplay\Device-Debug" /s /q
rd "%samples%\HelloWorldDisplay\HelloWorldDisplay\Simulator-Debug" /s /q
del ""%samples%\HelloWorldDisplay\HelloWorldDisplay\CompileRan"