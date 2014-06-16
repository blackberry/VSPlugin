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
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class represents a document context to the debugger. A document context represents a location within a source file. 
    /// (http://msdn.microsoft.com/en-us/library/bb145572.aspx)
    /// </summary>
    public class AD7DocumentContext : IDebugDocumentContext2
    {
        /// <summary>
        /// Long path file name
        /// </summary>
        string m_fileName;

        /// <summary>
        ///  Start position. In VSNDK debug engine, both begPos and endPos have the same value.
        /// </summary>
        TEXT_POSITION m_begPos;

        /// <summary>
        ///  End position. In VSNDK debug engine, both begPos and endPos have the same value.
        /// </summary>
        TEXT_POSITION m_endPos;

        /// <summary>
        ///  An address in a program's execution stream. 
        /// </summary>
        AD7MemoryAddress m_codeContext;


        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the long path name for a given short one. </summary>
        /// <param name="path"> Short path name. </param>
        /// <param name="longPath"> Returns this long path name. </param>
        /// <param name="longPathLength"> Lenght of this long path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength
            );


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName"> Short path file name. </param>
        /// <param name="begPos"> Start position. </param>
        /// <param name="endPos"> End position. In VSNDK debug engine, both begPos and endPos have the same value. </param>
        /// <param name="codeContext"> An address in a program's execution stream. </param>
        public AD7DocumentContext(string fileName, TEXT_POSITION begPos, TEXT_POSITION endPos, AD7MemoryAddress codeContext)
        {
            // Need to lengthen the path used by Visual Studio.
            StringBuilder documentNameSB = new StringBuilder(1024);
            GetLongPathName(fileName, documentNameSB, documentNameSB.Capacity);
            m_fileName = documentNameSB.ToString();

            m_begPos = begPos;
            m_endPos = endPos;
            m_codeContext = codeContext;
        }


        #region IDebugDocumentContext2 Members


        /// <summary>
        /// Compares this document context to a given array of document contexts. (http://msdn.microsoft.com/en-us/library/bb145338.aspx)
        /// </summary>
        /// <param name="Compare"> A value from the DOCCONTEXT_COMPARE enumeration that specifies the type of comparison. </param>
        /// <param name="rgpDocContextSet"> An array of IDebugDocumentContext2 objects that represent the document contexts being compared to. </param>
        /// <param name="dwDocContextSetLen"> The length of the array of document contexts to compare. </param>
        /// <param name="pdwDocContext"> Returns the index into the rgpDocContextSet array of the first document context that satisfies the comparison. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugDocumentContext2.Compare(enum_DOCCONTEXT_COMPARE Compare, IDebugDocumentContext2[] rgpDocContextSet, uint dwDocContextSetLen, out uint pdwDocContext)
        {
            dwDocContextSetLen = 0;
            pdwDocContext = 0;

            return VSConstants.E_NOTIMPL;
        }

        
        /// <summary>
        /// Retrieves a list of all code contexts associated with this document context. The VSNDK Debug Engine only supports one code context per document 
        /// context and the code contexts are always memory addresses. (http://msdn.microsoft.com/en-us/library/bb146273.aspx)
        /// </summary>
        /// <param name="ppEnumCodeCxts"> Returns an IEnumDebugCodeContexts2 object that contains a list of code contexts. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugDocumentContext2.EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
        {
            ppEnumCodeCxts = null;
            try
            {
                AD7MemoryAddress[] codeContexts = new AD7MemoryAddress[1];
                codeContexts[0] = m_codeContext;
                ppEnumCodeCxts = new AD7CodeContextEnum(codeContexts);
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Gets the document that contains this document context. This method is for those debug engines that supply documents directly 
        /// to the IDE. Since the VSNDK Debug Engine does not do this, this method returns E_FAIL. 
        /// (http://msdn.microsoft.com/en-us/library/bb161759.aspx)
        /// </summary>
        /// <param name="ppDocument"> Returns an IDebugDocument2 object that represents the document that contains this document context. </param>
        /// <returns> E_FAIL. </returns>
        int IDebugDocumentContext2.GetDocument(out IDebugDocument2 ppDocument)
        {           
            ppDocument = null;
            return VSConstants.E_FAIL;
        }


        /// <summary>
        /// Gets the language associated with this document context. The language for this sample is always C++.
        /// (http://msdn.microsoft.com/en-us/library/bb146340.aspx)
        /// </summary>
        /// <param name="pbstrLanguage"> Returns the name of the language that implements the code at this document context. </param>
        /// <param name="pguidLanguage"> Returns the GUID of the language that implements the code at this document context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugDocumentContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = "C++";
            pguidLanguage = AD7Guids.guidLanguageCpp;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the displayable name of the document that contains this document context. 
        /// (http://msdn.microsoft.com/en-us/library/bb146162.aspx)
        /// </summary>
        /// <param name="gnType"> A value from the GETNAME_TYPE enumeration that specifies the type of name to return. </param>
        /// <param name="pbstrFileName"> Returns the name of the file. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugDocumentContext2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            pbstrFileName = m_fileName;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the source code range of this document context. A source range is the entire range of source code, from the current 
        /// statement back to just after the previous statement that contributed code. The source range is typically used for mixing 
        /// source statements, including comments, with code in the disassembly window. Since this engine does not support the 
        /// disassembly window, this is not implemented. (http://msdn.microsoft.com/en-us/library/bb146190.aspx)
        /// </summary>
        /// <param name="pBegPosition"> A TEXT_POSITION structure that is filled in with the starting position. Set to a null value if it is not needed. </param>
        /// <param name="pEndPosition"> A TEXT_POSITION structure that is filled in with the ending position. Set to a null value if it is not needed. </param>
        /// <returns> Not implemented. </returns>
        int IDebugDocumentContext2.GetSourceRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            throw new NotImplementedException("This method is not implemented");
        }


        /// <summary>
        /// Gets the file statement range of the document context. A statement range is the range of the lines that contributed the code 
        /// to which this document context refers. (http://msdn.microsoft.com/en-us/library/bb161669.aspx)
        /// </summary>
        /// <param name="pBegPosition"> A TEXT_POSITION structure that is filled in with the starting position. Set to a null value if it is not needed. </param>
        /// <param name="pEndPosition"> A TEXT_POSITION structure that is filled in with the ending position. Set to a null value if it is not needed. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugDocumentContext2.GetStatementRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            try
            {
                pBegPosition[0].dwColumn = m_begPos.dwColumn;
                pBegPosition[0].dwLine = m_begPos.dwLine;

                pEndPosition[0].dwColumn = m_endPos.dwColumn;
                pEndPosition[0].dwLine = m_endPos.dwLine;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Moves the document context by a given number of statements or lines. This is used primarily to support the Autos window in 
        /// discovering the proximity statements around  this document context. Not implemented. 
        /// (http://msdn.microsoft.com/en-us/library/bb146603.aspx)
        /// </summary>
        /// <param name="nCount"> The number of statements or lines to move ahead, depending on the document context. </param>
        /// <param name="ppDocContext"> Returns a new IDebugDocumentContext2 object with the new position. </param>
        /// <returns> Not implemented. </returns>
        int IDebugDocumentContext2.Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
        {
            ppDocContext = null;
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }
}
