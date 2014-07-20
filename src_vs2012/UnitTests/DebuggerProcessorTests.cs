using System;
using System.Collections.Generic;
using System.Diagnostics;
using BlackBerry.NativeCore.Debugger;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class DebuggerProcessorTests
    {
        #region Helper Classes

        class FakeCommandSink : IGdbSender
        {
            private const string FakeBreakCommand = "~~BREAK~~";

            private readonly List<string> _expectations;
            private readonly List<string[]> _responses;
            private int _expectationIndex;

            public event Action<string[]> Received;

            public FakeCommandSink()
            {
                _expectations = new List<string>();
                _responses = new List<string[]>();
                _expectationIndex = -1;
            }

            public void RedirectResponses(GdbProcessor processor)
            {
                Received += responseLines =>
                    {
                        foreach (var line in responseLines)
                            processor.Receive(line);
                    };
            }

            /// <summary>
            /// Adds an expected command to be called on that object.
            /// </summary>
            public void ExpectRequest(string command, string[] response)
            {
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentNullException("command");
                if (response == null || response.Length == 0)
                    throw new ArgumentNullException("response");

                _expectations.Add(command);

                // if it doesn't end with 'GDB prompt', add it:
                if (string.Compare(response[response.Length - 1], GdbProcessor.Prompt, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    Array.Resize(ref response, response.Length + 1);
                    response[response.Length - 1] = GdbProcessor.Prompt;
                }

                _responses.Add(response);
            }

            /// <summary>
            /// Adds an expected command to be called on that object.
            /// </summary>
            public void ExpectRequest(string command, string response)
            {
                ExpectRequest(command, new[] { response, GdbProcessor.Prompt });
            }

            /// <summary>
            /// Adds expectation of Break() call.
            /// </summary>
            public void ExpectBreak()
            {
                _expectations.Add(FakeBreakCommand);
            }

            public void Break()
            {
                if (!CheckExpected(FakeBreakCommand))
                    throw new InvalidOperationException("Unexpected Break() command sent");
            }

            public bool Send(string command)
            {
                Assert.IsFalse(string.IsNullOrEmpty(command), "Unexpected empty command");

                Trace.WriteLine("::: Sending command: ::::::::::::::::::::");
                Trace.WriteLine(command);

                // skip the ID and verify received command:
                int i = 0;
                while (i < command.Length && char.IsDigit(command[i]))
                {
                    i++;
                }

                Assert.IsTrue(i != command.Length, "Missing command text");
                if (!CheckExpected(command.Substring(i)))
                    throw new InvalidOperationException("Unexpected command sent");

                // and send response asynchronously:

                var responseHandler = Received;
                if (responseHandler != null)
                {
                    var id = i > 0 ? command.Substring(0, i) : null;
                    var response = PrepareResponse(id, _responses[_expectationIndex]);

                    Trace.WriteLine("::: Received response: ::::::::::::::::::");
                    foreach (var line in response)
                        Trace.WriteLine(line);
                    Trace.WriteLine(":::::::::::::::::::::::::::::::::::::::::");

                    responseHandler.BeginInvoke(response, null, null);
                }
                return true;
            }

            private string[] PrepareResponse(string id, string[] lines)
            {
                if (string.IsNullOrEmpty(id))
                    return lines;

                // append ID into the response result:
                var result = new string[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i][0] == '^')
                    {
                        result[i] = id + lines[i];
                    }
                    else
                    {
                        result[i] = lines[i];
                    }
                }

                return result;
            }

            private bool CheckExpected(string command)
            {
                var expected = GetNextExpectedCommand();

                // expected command and given command must be equal:
                return string.Compare(expected, command, StringComparison.OrdinalIgnoreCase) == 0;
            }

            private string GetNextExpectedCommand()
            {
                _expectationIndex++;
                Assert.IsTrue(_expectationIndex >= 0 && _expectationIndex < _expectations.Count, "Invalid request, expected command, while buffer is empty");

                return _expectations[_expectationIndex];
            }

            /// <summary>
            /// Checks if all requests has been called in an expected order.
            /// </summary>
            public bool CheckIfMetAllExpectations()
            {
                // all expected requests has been processed:
                return _expectationIndex == _expectations.Count - 1;
            }
        }

        sealed class UnitTestRequest : Request
        {
            /// <summary>
            /// Init constructor.
            /// </summary>
            public UnitTestRequest(string command)
                : base(command)
            {
            }

            public override uint ID
            {
                get { return 0; }
            }
        }

        #endregion

        private static Request CreateRequest(string command)
        {
            return new UnitTestRequest(command); // create request with disabled ID check, when receiving response
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SendUnexpectedRequest()
        {
            var sender = new FakeCommandSink();
            var processor = new GdbProcessor(sender);

            // configure expectations:
            sender.RedirectResponses(processor);
            sender.ExpectRequest("-sync", "^done");

            // execute actions:
            processor.Send(new Request("-target-select xyz 1.2.3.4:8000"));

            // exception should already been thrown
            Assert.Fail("Missing exception on improper request sent");
        }

        [Test]
        public void CheckOrderOfSendCommands()
        {
            var sender = new FakeCommandSink();
            var processor = new GdbProcessor(sender);
            Response response;

            // configure expectations:
            sender.RedirectResponses(processor);
            sender.ExpectRequest("-sync", "^done");

            // execute actions:
            processor.Send(CreateRequest("-sync"));
            bool result = processor.Wait(out response);
            
            // verify:
            Assert.IsTrue(result, "Unable to receive response");
            Assert.AreEqual("done", response.Name, "Invalid response received");
            Assert.IsTrue(sender.CheckIfMetAllExpectations(), "Not all requests were processed");
        }

        [Test]
        public void CheckIfHandledRequestGivesNoCommandToWaitForProcessor()
        {
            var sender = new FakeCommandSink();
            var processor = new GdbProcessor(sender);
            Request request;
            Response response;

            // configure expectations:
            sender.RedirectResponses(processor);
            sender.ExpectRequest("-sync", "^done");
            processor.Received += (s, e) => e.Handled = true; // since we setup the 

            // execute actions:
            request = CreateRequest("-sync");
            processor.Send(request);
            bool result = processor.Wait(500, out response); // wait 500ms for the response, that should never arrive (as we setup a Received handler marking all all as processed)

            // verify:
            Assert.IsFalse(result, "Should never receive any response");
            Assert.IsNull(response);
            Assert.IsNotNull(request.Response, "Response should be received by request");
            Assert.AreEqual("done", request.Response.Name, "Invalid response received");
            Assert.IsTrue(sender.CheckIfMetAllExpectations(), "Not all requests were processed");
        }

        [Test]
        public void ParseCString()
        {
            
        }
    }
}
