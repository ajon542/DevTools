﻿/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

namespace ProtoBufGenerator
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio;

    /// <summary>
    /// A managed wrapper for VS's concept of an IVsSingleFileGenerator which is
    /// a custom tool invoked at design time which can take any file as an input
    /// and provide any file as output.
    /// </summary>
    [ComVisible(true)]
    public abstract class BaseCodeGenerator : IVsSingleFileGenerator
    {
        /// <summary>
        /// Get a namespace for the file.
        /// </summary>
        protected string FileNameSpace { get; private set; }

        /// <summary>
        /// Gets a file-path for the input file.
        /// </summary>
        protected string InputFilePath { get; private set; }

        /// <summary>
        /// Gets the interface to the VS shell object we use to tell
        /// our progress while we are generating.
        /// </summary>
        internal IVsGeneratorProgress CodeGeneratorProgress { get; private set; }

        #region IVsSingleFileGenerator Members

        /// <summary>
        /// Implements the IVsSingleFileGenerator.DefaultExtension method. 
        /// Returns the extension of the generated file
        /// </summary>
        /// <param name="defaultExtension">
        /// Out parameter, will hold the extension that is to be given to the output
        /// file name. The returned extension must include a leading period
        /// </param>
        /// <returns>S_OK if successful, E_FAIL if not</returns>
        int IVsSingleFileGenerator.DefaultExtension(out string defaultExtension)
        {
            try
            {
                defaultExtension = GetDefaultExtension();
                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                //Debug.WriteLine(Strings.GetDefaultExtensionFailed);
                Debug.WriteLine(e.ToString());
                defaultExtension = string.Empty;
                return VSConstants.E_FAIL;
            }
        }

        /// <summary>
        /// Implements the IVsSingleFileGenerator.Generate method.
        /// Executes the transformation and returns the newly generated output file,
        /// whenever a custom tool is loaded, or the input file is saved
        /// </summary>
        /// <param name="inputFilePath">
        /// The full path of the input file. May be a null reference (Nothing in Visual Basic)
        /// in future releases of Visual Studio, so generators should not rely on this value.
        /// </param>
        /// <param name="inputFileContents">
        /// The contents of the input file. This is either a UNICODE BSTR (if the input file is text)
        /// or a binary BSTR (if the input file is binary). If the input file is a text file,
        /// the project system automatically converts the BSTR to UNICODE.
        /// </param>
        /// <param name="defaultNamespace">
        /// This parameter is meaningful only for custom tools that generate code. It represents the
        /// namespace into which the generated code will be placed. If the parameter is not a null
        /// reference (Nothing in Visual Basic) and not empty, the custom tool can use the following
        /// syntax to enclose the generated code.
        /// </param>
        /// <param name="outputFileContents">
        /// [out] Returns an array of bytes to be written to the generated file. You must
        /// include UNICODE or UTF-8 signature bytes in the returned byte array, as this
        /// is a raw stream. The memory for rgbOutputFileContents must be allocated using
        /// the .NET Framework call, System.Runtime.InteropServices.AllocCoTaskMem, or the
        /// equivalent Win32 system call, CoTaskMemAlloc. The project system is responsible
        /// for freeing this memory.
        /// </param>
        /// <param name="output">
        /// [out] Returns the count of bytes in the rgbOutputFileContent array.
        /// </param>
        /// <param name="generateProgress">
        /// A reference to the IVsGeneratorProgress interface through which the generator can report
        /// its progress to the project system.
        /// </param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns E_FAIL</returns>
        int IVsSingleFileGenerator.Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IntPtr[] outputFileContents, out uint output, IVsGeneratorProgress generateProgress)
        {
            if (inputFileContents == null)
            {
                throw new ArgumentNullException(inputFileContents);
            }

            InputFilePath = inputFilePath;
            FileNameSpace = defaultNamespace;
            CodeGeneratorProgress = generateProgress;

            byte[] bytes = GenerateCode(inputFileContents);

            if (bytes == null)
            {
                // This signals that GenerateCode() has failed. Tasklist items have been put up in GenerateCode().
                outputFileContents = null;
                output = 0;

                // Return E_FAIL to inform Visual Studio that the generator has failed (so that no file gets generated).
                return VSConstants.E_FAIL;
            }
            else
            {
                // The contract between IVsSingleFileGenerator implementors and consumers is that 
                // any output returned from IVsSingleFileGenerator.Generate() is returned through  
                // memory allocated via CoTaskMemAlloc(). Therefore, we have to convert the 
                // byte[] array returned from GenerateCode() into an unmanaged blob.  

                int outputLength = bytes.Length;
                outputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
                Marshal.Copy(bytes, 0, outputFileContents[0], outputLength);
                output = (uint)outputLength;
                return VSConstants.S_OK;
            }
        }

        #endregion

        /// <summary>
        /// Gets the default extension for this generator.
        /// </summary>
        /// <returns>String with the default extension for this generator.</returns>
        protected abstract string GetDefaultExtension();

        /// <summary>
        /// The method that does the actual work of generating code given the input file.
        /// </summary>
        /// <param name="inputFileContent">File contents as a string.</param>
        /// <returns>The generated code file as a byte-array.</returns>
        protected abstract byte[] GenerateCode(string inputFileContent);

        /// <summary>
        /// Method that will communicate an error via the shell callback mechanism.
        /// </summary>
        /// <param name="level">Level or severity.</param>
        /// <param name="message">Text displayed to the user.</param>
        /// <param name="line">Line number of error.</param>
        /// <param name="column">Column number of error.</param>
        protected virtual void GeneratorError(uint level, string message, uint line, uint column)
        {
            IVsGeneratorProgress progress = CodeGeneratorProgress;
            if (progress != null)
            {
                progress.GeneratorError(0, level, message, line, column);
            }
        }

        /// <summary>
        /// Method that will communicate a warning via the shell callback mechanism.
        /// </summary>
        /// <param name="level">Level or severity.</param>
        /// <param name="message">Text displayed to the user.</param>
        /// <param name="line">Line number of warning.</param>
        /// <param name="column">Column number of warning.</param>
        protected virtual void GeneratorWarning(uint level, string message, uint line, uint column)
        {
            IVsGeneratorProgress progress = CodeGeneratorProgress;
            if (progress != null)
            {
                progress.GeneratorError(1, level, message, line, column);
            }
        }
    }
}
