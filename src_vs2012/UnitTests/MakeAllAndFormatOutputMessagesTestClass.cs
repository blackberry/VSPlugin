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

using BlackBerry.BuildTasks;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class MakeAllAndFormatOutputMessagesTestClass
    {
        [TestCase]
        public void MakeAllAndFormatOutputMessagesConstructorTest()
        {
            MakeAllAndFormatOutputMessages target = new MakeAllAndFormatOutputMessages();
            Assert.IsNotNull(target);
        }

        private void ValidateError(string textToTest, MakeAllAndFormatOutputMessages.MessageType type, string message, string fileName, int line, int column)
        {
            string extractedFileName;
            int extractedLine;
            int extractedColumn;
            string extractedMessage;

            var extractedType = MakeAllAndFormatOutputMessages.ProcessErrorOutput(textToTest, out extractedMessage, out extractedFileName, out extractedLine, out extractedColumn);
            Assert.AreEqual(type, extractedType, "Invalid result type");
            Assert.AreEqual(message, extractedMessage, "Invalid message");
            Assert.AreEqual(fileName, extractedFileName, "Invalid file name");
            Assert.AreEqual(line, extractedLine, "Invalid line number");
            Assert.AreEqual(column, extractedColumn, "Invalid column number");
        }

        [TestCase]
        public void ProcessError1()
        {
            ValidateError("../src/main.cpp(56): error: 'appui' was not declared in this scope", MakeAllAndFormatOutputMessages.MessageType.Error, "'appui' was not declared in this scope", "../src/main.cpp", 56, 1);
            ValidateError("../src/main.cpp(56, 12): warning: 'appui' was not declared in this scope", MakeAllAndFormatOutputMessages.MessageType.Warning, "'appui' was not declared in this scope", "../src/main.cpp", 56, 12);
            ValidateError("T:/temp/zzz/CASCAD~3/OPENGL~1/src/main.c:48:14: error: 'ABC123' undeclared (first use in this function)", MakeAllAndFormatOutputMessages.MessageType.Error, "'ABC123' undeclared (first use in this function)", "T:\\temp\\zzz\\CascadesCardApp3\\OpenGL2App1\\src\\main.c", 48, 14);
            ValidateError("ttssample.c:(123): undefined reference to `RequestFile'", MakeAllAndFormatOutputMessages.MessageType.Error, "undefined reference to `RequestFile'", "ttssample.c", 123, 1);
            ValidateError("T:/temp/zzz/CASCAD~3/OPENGL~1/src/bbutil.c:296: undefined reference to `eglDestroyContext'", MakeAllAndFormatOutputMessages.MessageType.Error, "undefined reference to `eglDestroyContext'", "T:\\temp\\zzz\\CascadesCardApp3\\OpenGL2App1\\src\\bbutil.c", 296, 1);
        }

        private void ValidateFileName(string textToTest, string fileName, int line, int column)
        {
            int extractedLine;
            int extractedColumn;

            var extractedFileName = MakeAllAndFormatOutputMessages.ExtractFileName(textToTest, out extractedLine, out extractedColumn);
            Assert.AreEqual(fileName, extractedFileName, "Invalid file name");
            Assert.AreEqual(line, extractedLine, "Invalid line number");
            Assert.AreEqual(column, extractedColumn, "Invalid column number");
        }

        [TestCase]
        public void ExtractFileNames()
        {
            ValidateFileName("/abc/main.cpp(10)", "/abc/main.cpp", 10, 1);
            ValidateFileName("/abc/main.cpp( 12)", "/abc/main.cpp", 12, 1);
            ValidateFileName("/abc/main.cpp(10,5)", "/abc/main.cpp", 10, 5);
            ValidateFileName("/abc/main.cpp (10", "/abc/main.cpp", 10, 1);
            ValidateFileName("/abc/main.cpp ( 14", "/abc/main.cpp", 14, 1);
            ValidateFileName("/abc/main.cpp (10,12", "/abc/main.cpp", 10, 12);

            ValidateFileName("/abc/main.cpp: 14", "/abc/main.cpp", 14, 1);
            ValidateFileName("/abc/main.cpp:10:12", "/abc/main.cpp", 10, 12);

        }
    }
}
