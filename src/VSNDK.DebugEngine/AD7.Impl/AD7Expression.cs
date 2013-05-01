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
using System.Text;
using VSNDK.Parser;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;
using System.Threading;

namespace VSNDK.DebugEngine
{
    // This class represents a succesfully parsed expression to the debugger. 
    // It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
    // It allows the debugger to obtain the values of an expression in the debuggee. 
    // For the purposes of this sample, this means obtaining the values of locals and parameters from a stack frame.
    public class AD7Expression : IDebugExpression2
    {
        //private VariableInformation m_var;
        private string exp;
        private EventDispatcher m_eventDispatcher;
        private AD7StackFrame m_frame;

        public AD7Expression(string exp, AD7StackFrame frame, EventDispatcher dispatcher)
        {
            this.exp = exp;
            this.m_eventDispatcher = dispatcher;
            this.m_frame = frame;
        }

        #region IDebugExpression2 Members

        // This method cancels asynchronous expression evaluation as started by a call to the IDebugExpression2::EvaluateAsync method.
        int IDebugExpression2.Abort()
        {
            throw new NotImplementedException();
        }

        public void evaluatingAsync()
        {
            VariableInfo vi = VariableInfo.get(exp, m_eventDispatcher, m_frame);
            AD7Property ppResult = new AD7Property(vi);

            m_frame.m_engine.Callback.Send(new AD7ExpressionEvaluationCompleteEvent(this, ppResult), AD7ExpressionEvaluationCompleteEvent.IID, m_frame.m_engine, m_frame.m_thread);
        }

        // This method evaluates the expression asynchronously.
        // This method should return immediately after it has started the expression evaluation. 
        // When the expression is successfully evaluated, an IDebugExpressionEvaluationCompleteEvent2 
        // must be sent to the IDebugEventCallback2 event callback
        //
        // This is primarily used for the immediate window which this engine does not currently support.
        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            Thread m_processingThread;
            m_processingThread = new Thread(evaluatingAsync);
            m_processingThread.Start();            

            return VSConstants.S_OK;
        }

        // This method evaluates the expression synchronously.
        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            VariableInfo vi = VariableInfo.get(exp, m_eventDispatcher, m_frame);
            ppResult = new AD7Property(vi);
            return VSConstants.S_OK;
        }

        #endregion
    }
}