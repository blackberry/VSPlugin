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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Threading;

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This class represents a parsed expression ready for binding and evaluating. 
    /// It is returned as a result of a successful call to IDebugExpressionContext2.ParseText
    /// It allows the debugger to obtain the values of an expression in the debuggee. 
    /// (http://msdn.microsoft.com/en-ca/library/bb162308.aspx)
    /// </summary>
    public class AD7Expression : IDebugExpression2
    {
        /// <summary>
        ///  The expression to be evaluated. 
        /// </summary>
        private string exp;

        /// <summary>
        /// The class that manages debug events for the debug engine.
        /// </summary>
        private EventDispatcher m_eventDispatcher;

        /// <summary>
        /// Current stack frame.
        /// </summary>
        private AD7StackFrame m_frame;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exp"> The expression to be evaluated. </param>
        /// <param name="frame"> Current stack frame. </param>
        /// <param name="dispatcher"> Represents the class that manages debug events for the debug engine. </param>
        public AD7Expression(string exp, AD7StackFrame frame, EventDispatcher dispatcher)
        {
            this.exp = exp;
            this.m_eventDispatcher = dispatcher;
            this.m_frame = frame;
        }

        #region IDebugExpression2 Members


        /// <summary>
        /// This method cancels asynchronous expression evaluation as started by a call to the IDebugExpression2::EvaluateAsync method.
        /// Not implemented yet because it was not needed till now. (http://msdn.microsoft.com/en-ca/library/bb145924.aspx)
        /// </summary>
        /// <returns> Not implemented. </returns>
        int IDebugExpression2.Abort()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Thread responsible for evaluating expressions asynchronously.
        /// </summary>
        public void evaluatingAsync()
        {
            VariableInfo vi = VariableInfo.get(exp, m_eventDispatcher, m_frame);
            AD7Property ppResult = new AD7Property(vi);

            m_frame.m_engine.Callback.Send(new AD7ExpressionEvaluationCompleteEvent(this, ppResult), AD7ExpressionEvaluationCompleteEvent.IID, m_frame.m_engine, m_frame.m_thread);
        }

        
        /// <summary>
        /// This method evaluates the expression asynchronously.
        /// This is primarily used for the immediate window. (http://msdn.microsoft.com/en-ca/library/bb146752.aspx)
        /// </summary>
        /// <param name="dwFlags"> A combination of flags from the EVALFLAGS enumeration that control expression evaluation. </param>
        /// <param name="pExprCallback"> This parameter is always a null value. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugExpression2.EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            // Creating a thread to evaluate the expression asynchronously.
            Thread m_processingThread;
            m_processingThread = new Thread(evaluatingAsync);
            m_processingThread.Start();            

            return VSConstants.S_OK;
        }


        /// <summary>
        /// This method evaluates the expression synchronously. (http://msdn.microsoft.com/en-ca/library/bb146982.aspx)
        /// </summary>
        /// <param name="dwFlags"> A combination of flags from the EVALFLAGS enumeration that control expression evaluation. </param>
        /// <param name="dwTimeout"> Maximum time, in milliseconds, to wait before returning from this method. Use INFINITE to wait 
        /// indefinitely. </param>
        /// <param name="pExprCallback"> This parameter is always a null value. </param>
        /// <param name="ppResult"> Returns the IDebugProperty2 object that contains the result of the expression evaluation. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugExpression2.EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            VariableInfo vi = VariableInfo.get(exp, m_eventDispatcher, m_frame);
            ppResult = new AD7Property(vi);
            m_frame._lastEvaluatedExpression = vi;
            return VSConstants.S_OK;
        }

        #endregion
    }
}