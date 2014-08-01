using System;
using System.Text;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Debugger.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class ResponseParsingTests
    {
        [Test]
        public void ParseListOfProcesses()
        {
            var data = new[]
            {
                "&\"info pidlist\\n",
                "~\"usr/sbin/qconn - 76595423/1\\n",
                "~\"usr/sbin/qconn - 76595423/2\\n",
                "~\"usr/bin/pdebug - 76611814/1\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/1\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/2\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/3\\n",
                "~\"accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/FallingBlocks - 75714795/4\\n"
            };
            var response = new Response(data, 0, null, null, null, data);
            var request = RequestsFactory.ListProcesses();
            var result = request.Complete(response);

            Assert.IsTrue(result);
            Assert.IsNotNull(request.Processes);
            Assert.AreEqual(3, request.Processes.Length);
        }

        [Test]
        public void ParseProcessNames()
        {
            var process = new ProcessInfo(0, "just name");
            Assert.AreEqual("just name", process.Name);

            process = new ProcessInfo(0, "just name/");
            Assert.AreEqual("just name", process.Name);

            process = new ProcessInfo(0, "just.name.exe");
            Assert.AreEqual("just.name.exe", process.Name);

            process = new ProcessInfo(0, "/path/to/executable");
            Assert.AreEqual("executable", process.Name);

            process = new ProcessInfo(0, "/path/to/executable/");
            Assert.AreEqual("executable", process.Name);

            process = new ProcessInfo(0, "path\\to\\executable");
            Assert.AreEqual("executable", process.Name);
        }

        [Test]
        public void GetNextChar()
        {
            Assert.AreEqual(0, Instruction.GetNextChar("abcdefghij", 'a', 0), "Error: char 'a', string 'abcdefghij', int '0'");
            Assert.AreEqual(10, Instruction.GetNextChar("abcdefghij", 'a', 1), "Error: char 'a', string 'abcdefghij', int '1'");
            Assert.AreEqual(3, Instruction.GetNextChar("abcdefghij", 'd', 0), "Error: char 'd', string 'abcdefghij', int '0'");
            Assert.AreEqual(3, Instruction.GetNextChar("abcdefghij", 'd', 1), "Error: char 'd', string 'abcdefghij', int '1'");
            Assert.AreEqual(10, Instruction.GetNextChar("abcdefghij", 'z', 1), "Error: char 'z', string 'abcdefghij', int '1'");
            Assert.AreEqual(0, Instruction.GetNextChar("", 'd', 1), "Error: char 'd', string '', int '1'");
            Assert.AreEqual(10, Instruction.GetNextChar("abcdefghij", 'd', 12), "Error: char 'd', string 'abcdefghij', int '12'");
            Assert.AreEqual(10, Instruction.GetNextChar("abcdefghij", '\0', 1), "Error: char NULL, string 'abcdefghij', int '1'");
        }


        [Test]
        public void FindClosing()
        {
            Assert.AreEqual(1, Instruction.FindClosing("()cdefghij", '(', ')', 0), "Error: char '(', char '), string '()cdefghij', int '0'");
            Assert.AreEqual(10, Instruction.FindClosing("(bcdefghij", '(', ')', 2), "Error: char '(', char '), string '(bcdefghij', int '2'");
            Assert.AreEqual(10, Instruction.FindClosing("(bcdefghij", '(', ')', 0), "Error: char '(', char '), string '(bcdefghij', int '0'");
            Assert.AreEqual(5, Instruction.FindClosing("(()())ghij", '(', ')', 0), "Error: char '(', char '), string '(()())ghij', int '0'");
            Assert.AreEqual(7, Instruction.FindClosing("((\\()())hij", '(', ')', 0), "Error: char '(', char '), string '((\\()())hij', int '0'");
            Assert.AreEqual(2, Instruction.FindClosing("(()())ghij", '(', ')', 1), "Error: char '(', char '), string '(()())ghij', int '1'");
            Assert.AreEqual(4, Instruction.FindClosing("((\\()())hij", '(', ')', 1), "Error: char '(', char '), string '((\\()())hij', int '1'");
            Assert.AreEqual(4, Instruction.FindClosing("(()())ghij", '(', ')', 3), "Error: char '(', char '), string '(()())ghij', int '3'");
            Assert.AreEqual(6, Instruction.FindClosing("(()(())hij", '(', ')', 3), "Error: char '(', char '), string '(()(())hij', int '3'");
            Assert.AreEqual(1, Instruction.FindClosing("()())fghij", '(', ')', 0), "Error: char '(', char '), string '()())fghij', int '0'");
            Assert.AreEqual(3, Instruction.FindClosing("()())fghij", '(', ')', 2), "Error: char '(', char '), string '()())fghij', int '2'");
            Assert.AreEqual(10, Instruction.FindClosing("((())fghij", '(', ')', 0), "Error: char '(', char '), string '(()())ghij', int '0'");
            Assert.AreEqual(4, Instruction.FindClosing("((())fghij", '(', ')', 1), "Error: char '(', char '), string '((())fghij', int '1'");
        }

        [Test]
        public void SearchResponse()
        {
            // Add the values you want to test.
            Assert.AreEqual(3, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 0, 1, true, '?'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '0', int '1', bool 'TRUE', char '?'");
            Assert.AreEqual(1, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 0, 1, true, '@'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '0', int '1', bool 'TRUE', char '@'");
            Assert.AreEqual(14, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 0, 3, true, '?'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '0', int '3', bool 'TRUE', char '?'");
            Assert.AreEqual(12, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 0, 3, true, '@'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '0', int '3', bool 'TRUE', char '@'");
            Assert.AreEqual(19, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 10, 3, true, '?'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '10', int '3', bool 'TRUE', char '?'");
            Assert.AreEqual(17, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 10, 3, true, '@'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '10', int '3', bool 'TRUE', char '@'");
            Assert.AreEqual(24, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 24, 1, false, '?'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '24', int '1', bool 'TRUE', char '?'");
            Assert.AreEqual(24, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 24, 1, false, '@'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '24', int '1', bool 'TRUE', char '@'");
            Assert.AreEqual(19, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 24, 3, false, '?'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '24', int '3', bool 'TRUE', char '?'");
            Assert.AreEqual(19, Instruction.SearchResponse("aabbbcaabaccabcababaababc", "ab", 24, 3, false, '@'), "Error: string 'aabbbcaabaccabcababaababc', string 'ab', int '24', int '3', bool 'TRUE', char '@'");
        }

        [Test]
        public void SubstituteVariables()
        {
            string[] v = new string[10];
            v[0] = "The";
            v[1] = "text";
            v[2] = "";
            v[3] = "";
            v[4] = "";
            v[5] = "the";
            v[6] = "";
            v[7] = "";
            v[8] = "";
            v[9] = "number";
            // Add the values you want to test.
            StringAssert.AreEqualIgnoringCase("The number of the words in the following text is smaller than the number of the letters in the same text\r\n", Instruction.SubstituteVariables("$0$ $9$ of $5$ words in $5$ following $1$ is smaller $3$than $5$ $9$ of $5$ letters in $5$ same $1$$EOL$", v), "Error: string '', Variables array not printed.");
        }

        [Test]
        public void GetInstructionCode()
        {
            var instructions = InstructionCollection.Load();

            // The instruction code returned from get_Instruction_Code() function depends on the position the associated parsing instruction
            // was stored in the respective data structure. That's why the following unit tests just compares the instruction code with -1
            // (-1 means that the instruction was not found).
            string param = "";
            // Add the values you want to test.
            Assert.AreEqual(null, instructions.Find("", out param), "Error: string '', string ''");
            StringAssert.AreEqualIgnoringCase("", param, "Param should be empty.");

            param = "";
            Assert.AreNotEqual(null, instructions.Find("-exec-continue --thread-group i1", out param), "Error: string '-exec-continue --thread-group i1', string ''");
            StringAssert.AreEqualIgnoringCase("", param, "Param should be empty for command '-exec-continue --thread-group i1'.");

            param = "";
            Assert.AreNotEqual(null, instructions.Find("-break-delete 3", out param), "Error: string '-break-delete 3', string ''");
            StringAssert.AreEqualIgnoringCase(";3", param, "Param should not be empty for command '-break-delete 3'.");

            param = "";
            Assert.AreNotEqual(null, instructions.Find("-break-after 4 10", out param), "Error: string '-break-after 4 10', string ''");
            StringAssert.AreEqualIgnoringCase(";4;10", param, "Param should not be empty for command '-break-after 4 10'.");
        }

        public static string ParseGDB(string response, string parsingInstruction)
        {
            if (string.IsNullOrEmpty(parsingInstruction))
                return response;

            var instruction = Instruction.Load(0, parsingInstruction);
            return ParseGDB(response, instruction);
        }

        public static string ParseGDB(string response, Instruction instruction)
        {
            Assert.IsNotNull(instruction);

            var parsingResult = instruction.Parse(response);
            return parsingResult;
        }

        public static string ParseGDB(string response, string parsingInstruction, int respBegin, bool repeat, string[] variables, string separator)
        {
            if (string.IsNullOrEmpty(parsingInstruction))
                return response;

            var instruction = Instruction.Load(0, parsingInstruction);
            Assert.IsNotNull(instruction);

            return instruction.Parse(response);
        }

        [Test]
        public void ParseGDB1()
        {
            string asyncInst = "??=thread-exited,id;{#51\\;;??\";@@\";#$EOL$;};??thread-created,id;{#40\\;;??\";@@\";#\\;;??pid ;@@ ;#$EOL$;};0;??*running;{#41\\;;??thread-id=\"all;{#0;}{??thread-id=\";@@\";};#$EOL$;};0;??*stopped;{??breakpoint-hit;{#27\\;;??bkptno=\";@@\";#\\;;??file=\";@@\";#\\;;??line=\";@@\";}{??end-stepping-range;{#45;??file=\";{#\\;;@@\";0;#\\;;??line=\";@@\";};}{??function-finished;{#46;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};}{??exited-normally;{#42;}{??exit-code;{#43\\;;??\";@@\";}{??signal-received;{??signal-meaning=\"Killed\";{#47;}{??signal-meaning=\"Segmentation fault\";{#54\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};}{#44\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};};};}{??exited-signalled;{#55\\;??signal-name=\";@@\";#\\;;??signal-meaning=\";@@\";}{#44\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};};};};};};};};#\\;;??thread-id=;{??\";@@\";};#$EOL$;};0;??=breakpoint-modified,bkpt=\\{number=\";{0;($EOR$;$1=?<?=breakpoint-modified,bkpt=\\{number=\";@@\";$$;$EOR$;?<?=breakpoint-modified,bkpt=\\{number=\"$1$;#21\\;$1$\\;;??enabled=\";@@\";#\\;;??addr=\";@@\";#\\;;??func=\";@@\";#\\;;??file=\";@@\";#\\;;??line=\";@@\";#\\;;??times=\";@@\";0;(??=breakpoint-modified,bkpt=\\{number=\"$1$;%?<?$EOL$;@@\"\\};);):$EOL$;};0;??=breakpoint-deleted,id=;{0;(??=breakpoint-deleted,id=\";#22\\;;@@\";):$EOL$;};0;??@\";{0;#81\\;\";(??@\";?<?@;@@$EOL$;):$EOL$;#\"!81$EOL$;};0;(??~\";#80\\;\";?<?~;@@$EOL$;#\"!80;):$EOL$;??Error in testing breakpoint condition;{#29\\;$EOL$;};??Quit (expect signal SIGINT when the program is resumed);{#50\\;$EOL$;};??2374: internal-error: frame_cleanup_after_sniffer: Assertion;{#52\\;$EOL$;};??^error,msg=\"Remote communication error: No error.;{#53\\;$EOL$;};??: internal-error: handle_inferior_event: Assertion;{#56\\;$EOL$;};0;(??&\";#80\\;\";?<?&;@@$EOL$;#\"!80;):$EOL$;(??$EOL$=;#80\\;\";?<?=;@@$EOL$;#\"!80;):$EOL$;";
            // Add the values you want to test.
            /*
            StringAssert.AreEqualIgnoringCase("23;1", ParseGDB("12^done;1\r\n", "??^done;{#23;@@$EOL$};"), "Error: string '12^done;', string '??^done;{#23;@@$EOL$};'");
            StringAssert.AreEqualIgnoringCase("26;1;100", ParseGDB("4^done;1;100\r\n", "??^done;{#26;@@$EOL$};"), "Error: string '4^done;1;100', string '??^done;{#26;@@$EOL$};'");
            StringAssert.AreEqualIgnoringCase("20;1;y;0x08048564;main;myprog.c;68;0", ParseGDB("73^done,bkpt={number=\"1\",type=\"breakpoint\",disp=\"keep\",enabled=\"y\",addr=\"0x08048564\",func=\"main\",file=\"myprog.c\",fullname=\"/home/nickrob/myprog.c\",line=\"68\",thread-groups=[\"i1\"],times=\"0\"}", "??^done;{#20\\;;??number=\";{@@\";};#\\;;??enabled=\";{@@\";};#\\;;??addr=\";{@@\";};#\\;;??func=\";{@@\";};#\\;;??file=\";{@@\";};#\\;;??line=\";{@@\";};#\\;;??times=\";{@@\";};}{#Function not found!;};"), "Error: string '-break-insert', string ''");
            StringAssert.AreEqualIgnoringCase("27;1;myprog.c;68;\r\n", ParseGDB("*stopped,reason=\"breakpoint-hit\",disp=\"keep\",bkptno=\"1\",thread-id=\"0\",frame={addr=\"0x08048564\",func=\"main\",args=[{name=\"argc\",value=\"1\"},{name=\"argv\",value=\"0xbfc4d4d4\"}],file=\"myprog.c\",fullname=\"/home/nickrob/myprog.c\",line=\"68\"}", asyncInst), "Error: string 'breakpoint hit.', string 'Asynchronous instruction.'");
             */
            StringAssert.AreEqualIgnoringCase("27;1;S:/temp/FALLIN~1/FALLIN~1/main.c;156;1\r\n80;\"\"[Switching to pid 88756470 tid 1]\"\"!80\r\n", ParseGDB("~\"[Switching to pid 88756470 tid 1]\"\r\n*stopped,reason=\"breakpoint-hit\",disp=\"keep\",bkptno=\"1\",frame={addr=\"0x78bcf450\",func=\"update\",args=[],file=\"S:/temp/FALLIN~1/FALLIN~1/main.c\",fullname=\"S:\\temp\\FALLIN~1\\FALLIN~1\\main.c\",line=\"156\"},thread-id=\"1\",stopped-threads=\"all\"\r\n=thread-selected,id=\"1\"", asyncInst), "Error: string 'breakpoint hit.', string 'Asynchronous instruction.'");
            StringAssert.AreEqualIgnoringCase("42;\r\n", ParseGDB("*stopped,reason=\"exited-normally\"", asyncInst), "Error: string '*stopped,reason=\"exited-normally\"', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("45;../../../devo/gdb/testsuite/gdb.threads/linux-dp.c;187;\r\n", ParseGDB("*stopped,reason=\"end-stepping-range\",thread-id=\"2\",line=\"187\",file=\"../../../devo/gdb/testsuite/gdb.threads/linux-dp.c\"", asyncInst), "Error: string 'end-stepping-range.', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("46;hello.c;7;\r\n", ParseGDB("*stopped,reason=\"function-finished\",frame={func=\"main\",args=[],file=\"hello.c\",fullname=\"/home/foo/bar/hello.c\",line=\"7\"}", asyncInst), "Error: string 'Function finished.', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("44;0x00010140;foo;try.c;13;\r\n", ParseGDB("111*stopped,signal-name=\"SIGINT\",signal-meaning=\"Interrupt\",frame={addr=\"0x00010140\",func=\"foo\",args=[],file=\"try.c\",fullname=\"/home/foo/bar/try.c\",line=\"13\"}", asyncInst), "Error: string '-exec-interrupt', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("", ParseGDB("", ""), "Error: string '', string ''");
        }

        [Test]
        public void ParseGDB2()
        {
            string[] v = new string[10];
            v[0] = "";
            v[1] = "";
            v[2] = "";
            v[3] = "";
            v[4] = "";
            v[5] = "";
            v[6] = "";
            v[7] = "";
            v[8] = "";
            v[9] = "";
            string sep = "#"; // default separator.

            string asyncInst = "??=thread-exited,id;{#51\\;;??\";@@\";#$EOL$;};??thread-created,id;{#40\\;;??\";@@\";#\\;;??pid ;@@ ;#$EOL$;};0;??*running;{#41\\;;??thread-id=\"all;{#0;}{??thread-id=\";@@\";};#$EOL$;};0;??*stopped;{??breakpoint-hit;{#27\\;;??bkptno=\";@@\";#\\;;??file=\";@@\";#\\;;??line=\";@@\";}{??end-stepping-range;{#45;??file=\";{#\\;;@@\";0;#\\;;??line=\";@@\";};}{??function-finished;{#46;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};}{??exited-normally;{#42;}{??exit-code;{#43\\;;??\";@@\";}{??signal-received;{??signal-meaning=\"Killed\";{#47;}{??signal-meaning=\"Segmentation fault\";{#54\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};}{#44\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};};};}{??exited-signalled;{#55\\;??signal-name=\";@@\";#\\;;??signal-meaning=\";@@\";}{#44\\;;??addr=\";@@\";#\\;;??func=\";@@\";0;??file=;{#\\;;??\";@@\";#\\;;??line=\";@@\";};};};};};};};};#\\;;??thread-id=;{??\";@@\";};#$EOL$;};0;??=breakpoint-modified,bkpt=\\{number=\";{0;($EOR$;$1=?<?=breakpoint-modified,bkpt=\\{number=\";@@\";$$;$EOR$;?<?=breakpoint-modified,bkpt=\\{number=\"$1$;#21\\;$1$\\;;??enabled=\";@@\";#\\;;??addr=\";@@\";#\\;;??func=\";@@\";#\\;;??file=\";@@\";#\\;;??line=\";@@\";#\\;;??times=\";@@\";0;(??=breakpoint-modified,bkpt=\\{number=\"$1$;%?<?$EOL$;@@\"\\};);):$EOL$;};0;??=breakpoint-deleted,id=;{0;(??=breakpoint-deleted,id=\";#22\\;;@@\";):$EOL$;};0;??@\";{0;#81\\;\";(??@\";?<?@;@@$EOL$;):$EOL$;#\"!81$EOL$;};0;(??~\";#80\\;\";?<?~;@@$EOL$;#\"!80;):$EOL$;??Error in testing breakpoint condition;{#29\\;$EOL$;};??Quit (expect signal SIGINT when the program is resumed);{#50\\;$EOL$;};??2374: internal-error: frame_cleanup_after_sniffer: Assertion;{#52\\;$EOL$;};??^error,msg=\"Remote communication error: No error.;{#53\\;$EOL$;};??: internal-error: handle_inferior_event: Assertion;{#56\\;$EOL$;};0;(??&\";#80\\;\";?<?&;@@$EOL$;#\"!80;):$EOL$;(??$EOL$=;#80\\;\";?<?=;@@$EOL$;#\"!80;):$EOL$;";
            // Add the values you want to test.
            StringAssert.AreEqualIgnoringCase("23;1", ParseGDB("12^done;1\r\n", "??^done;{#23;@@$EOL$};", 0, false, v, sep), "Error: string '12^done;', string '??^done;{#23;@@$EOL$};'");
            StringAssert.AreEqualIgnoringCase("26;1;100", ParseGDB("4^done;1;100\r\n", "??^done;{#26;@@$EOL$};", 0, false, v, sep), "Error: string '4^done;1;100', string '??^done;{#26;@@$EOL$};'");
            StringAssert.AreEqualIgnoringCase("20;1;y;0x08048564;main;myprog.c;68;0", ParseGDB("73^done,bkpt={number=\"1\",type=\"breakpoint\",disp=\"keep\",enabled=\"y\",addr=\"0x08048564\",func=\"main\",file=\"myprog.c\",fullname=\"/home/nickrob/myprog.c\",line=\"68\",thread-groups=[\"i1\"],times=\"0\"}", "??^done;{#20\\;;??number=\";{@@\";};#\\;;??enabled=\";{@@\";};#\\;;??addr=\";{@@\";};#\\;;??func=\";{@@\";};#\\;;??file=\";{@@\";};#\\;;??line=\";{@@\";};#\\;;??times=\";{@@\";};}{#Function not found!;};", 0, false, v, sep), "Error: string '-break-insert', string ''");
            StringAssert.AreEqualIgnoringCase("27;1;myprog.c;68;\r\n", ParseGDB("*stopped,reason=\"breakpoint-hit\",disp=\"keep\",bkptno=\"1\",thread-id=\"0\",frame={addr=\"0x08048564\",func=\"main\",args=[{name=\"argc\",value=\"1\"},{name=\"argv\",value=\"0xbfc4d4d4\"}],file=\"myprog.c\",fullname=\"/home/nickrob/myprog.c\",line=\"68\"}", asyncInst, 0, false, v, sep), "Error: string 'breakpoint hit.', string 'Asynchronous instruction.'");
            StringAssert.AreEqualIgnoringCase("42;\r\n", ParseGDB("*stopped,reason=\"exited-normally\"", asyncInst, 0, false, v, sep), "Error: string '*stopped,reason=\"exited-normally\"', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("45;../../../devo/gdb/testsuite/gdb.threads/linux-dp.c;187;\r\n", ParseGDB("*stopped,reason=\"end-stepping-range\",thread-id=\"2\",line=\"187\",file=\"../../../devo/gdb/testsuite/gdb.threads/linux-dp.c\"", asyncInst, 0, false, v, sep), "Error: string 'end-stepping-range.', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("46;hello.c;7;\r\n", ParseGDB("*stopped,reason=\"function-finished\",frame={func=\"main\",args=[],file=\"hello.c\",fullname=\"/home/foo/bar/hello.c\",line=\"7\"}", asyncInst, 0, false, v, sep), "Error: string 'Function finished.', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("44;0x00010140;foo;try.c;13;\r\n", ParseGDB("111*stopped,signal-name=\"SIGINT\",signal-meaning=\"Interrupt\",frame={addr=\"0x00010140\",func=\"foo\",args=[],file=\"try.c\",fullname=\"/home/foo/bar/try.c\",line=\"13\"}", asyncInst, 0, false, v, sep), "Error: string '-exec-interrupt', string 'AsyncInstruction'");
            StringAssert.AreEqualIgnoringCase("", ParseGDB("", "", 0, false, v, sep), "Error: string '', string ''");
        }

        [Test]
        public void ParseGDB3()
        {
            var instructions = InstructionCollection.Load();
            string param;

            //02-data-evaluate-expression "gravity_x"
            //02^done,value="-0"
            var asyncInst = instructions.Find("-data-evaluate-expression", out param);
            Assert.IsNotNull(asyncInst);
            StringAssert.AreEqualIgnoringCase("60;-0.123", ParseGDB("^done,value=\"-0.123\"", asyncInst));

            //=breakpoint-modified,bkpt={number="1",type="breakpoint",disp="keep",enabled="y",addr="0x78665450",func="update",file="S:/temp/FALLIN~1/FALLIN~1/main.c",fullname="S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c",line="156",times="1",original-location="S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c:156"}
            asyncInst = instructions.Find("=breakpoint-modified", out param);
            Assert.IsNotNull(asyncInst);
            StringAssert.AreEqualIgnoringCase("21;1;y;0x78bcf450;update;S:/temp/FALLIN~1/FALLIN~1/main.c;156;1", ParseGDB("=breakpoint-modified,bkpt={number=\"1\",type=\"breakpoint\",disp=\"keep\",enabled=\"y\",addr=\"0x78bcf450\",func=\"update\",file=\"S:/temp/FALLIN~1/FALLIN~1/main.c\",fullname=\"S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c\",line=\"156\",times=\"1\",original-location=\"S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c:156\"}\r\n", asyncInst), "Error: string 'breakpoint hit.', string 'Asynchronous instruction.'");
            StringAssert.AreEqualIgnoringCase("21;2;y;0x78687450;update;S:/temp/FALLIN~1/FALLIN~1/main.c;156;1", ParseGDB("=breakpoint-modified,bkpt={number=\"2\",type=\"breakpoint\",disp=\"keep\",enabled=\"y\",addr=\"0x78687450\",func=\"update\",file=\"S:/temp/FALLIN~1/FALLIN~1/main.c\",fullname=\"S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c\",line=\"156\",times=\"1\",original-location=\"S:\\\\temp\\\\FALLIN~1\\\\FALLIN~1\\\\main.c:156\"}\r\n", asyncInst), "Error: string 'breakpoint hit.', string 'Asynchronous instruction.'");
        }
    }
}
