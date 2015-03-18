
namespace ProtoBufGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using VSLangProj;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.VisualStudio.Shell;


    /// <summary>
    /// Code generator for the .proto file extension.
    /// </summary>
    /// <remarks>
    /// When a .proto file is added or saved in the project, the code generator
    /// will be triggered to generate the csharp file.
    /// </remarks>
    [ComVisible(true)]
    public class ProtoBufGenerator : BaseCodeGeneratorWithSite
    {
        private string workingDirectory;
        private string protogenExe;
        private string protoBufDll;
        private const string FileExtension = ".cs";

        #region Overrides

        /// <summary>
        /// The method that does the actual work of generating code given the input file.
        /// </summary>
        /// <param name="inputFileContent">File contents as a string.</param>
        /// <returns>The generated code file as a byte-array.</returns>
        protected override byte[] GenerateCode(string inputFileContent)
        {
            try
            {
                string assembly = Assembly.GetAssembly(typeof(ProtoBufGenerator)).Location;
                workingDirectory = Path.GetDirectoryName(assembly);
                protogenExe = Path.Combine(workingDirectory, "Resources", "protogen.exe");
                protoBufDll = Path.Combine(workingDirectory, "Resources", "protobuf-net.dll");

                // Create a temporary file for the generated content.
                using (var tempfile = new TempFile())
                {
                    string path = tempfile.Path;

                    if (!File.Exists(protogenExe))
                    {
                        GeneratorError(4, "Missing: " + protogenExe, 1, 1);
                        return null;
                    }

                    // Run the protogen.exe to generate the cs file.
                    List<string> args = GenerateProtoGenArgs(InputFilePath, path, FileNameSpace);
                    int result = Execute(protogenExe, args, workingDirectory);

                    // Display the errors if needed.
                    if (result != 0)
                    {
                        GenerateProtobufErrors(path, result);
                        return null;
                    }

                    // Add the protobuf-net.dll reference to the project.
                    AddReference(protoBufDll);

                    // Return the bytes from the generated file.
                    return tempfile.GetBytes();
                }
            }
            catch (Exception e)
            {
                GeneratorError(4, e.ToString(), 1, 1);
                return null;
            }
        }

        /// <summary>
        /// Gets the default extension of the generated file.
        /// </summary>
        /// <returns>The default extension (.cs).</returns>
        protected override string GetDefaultExtension()
        {
            return FileExtension;
        }

        #endregion

        /// <summary>
        /// Generate the arguments to the protogen.exe.
        /// </summary>
        /// <param name="inputPath">The input file path (.proto).</param>
        /// <param name="outputPath">The output file path (.cs).</param>
        /// <param name="namespaceOptions">The namespace.</param>
        /// <returns>The list of arguments.</returns>
        private List<string> GenerateProtoGenArgs(string inputPath, string outputPath, string namespaceOptions)
        {
            List<string> args = new List<string>();

            // Include any errors in the output file.
            args.Add("-writeErrors");

            // Specify the input file.
            args.Add("-i:" + inputPath);

            // Specify the working directory.
            args.Add("-w:" + Path.GetDirectoryName(inputPath));

            // Current output language template.
            string language = "csharp";
            args.Add("-t:" + language);

            // Specify the output file.
            args.Add("-o:" + outputPath);

            // Quiet.
            args.Add("-q");

            // Specify namespace.
            string[] parts = namespaceOptions.Split(';');
            if (parts.Length > 0)
            {
                args.Add("-ns:" + parts[0]);
            }
            for (int i = 1; i < parts.Length; i++)
            {
                args.Add("-p:" + parts[i]);
            }
            return args;
        }

        /// <summary>
        /// Generate any errors that are specified in the given file.
        /// </summary>
        /// <param name="filename">The file generated from protogen.exe.</param>
        /// <param name="result">The result from the program execution. Used for error display only.</param>
        private void GenerateProtobufErrors(string filename, int result)
        {
            using (var lineReader = File.OpenText(filename))
            {
                string line;
                bool hasErrorText = false;

                // Obtain the error messages.
                while ((line = lineReader.ReadLine()) != null)
                {
                    GeneratorError(4, line, 1, 1);
                    hasErrorText = true;
                }

                // There was no error specified, something else went wrong.
                if (!hasErrorText)
                {
                    GeneratorError(4, "Code generation failed with exit-code " + result, 1, 1);
                }
            }
        }

        /// <summary>
        /// Execute the given application. This will block until the
        /// application exits.
        /// TODO: Find a better place for this method.
        /// </summary>
        /// <param name="application">The application to execute.</param>
        /// <param name="args">The arguments to the application.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>The result.</returns>
        private int Execute(string application, List<string> args, string workingDirectory)
        {
            // Build the process start information.
            ProcessStartInfo psi = new ProcessStartInfo(application);
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = workingDirectory;
            StringBuilder sb = new StringBuilder();
            foreach (string arg in args)
            {
                sb.Append("\"" + arg + "\" ");
            }
            psi.Arguments = sb.ToString();
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            // Start the process.
            using (Process proc = Process.Start(psi))
            {
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        /// <summary>
        /// Attempt to add a reference to the given dll. If this fails for whatever reason,
        /// it's not the end of the world, the user can add it manually.
        /// </summary>
        /// <param name="dll">The path and name of the dll.</param>
        private void AddReference(string dll)
        {
            // TODO: This should probably be in a VS SDK helper class.
            try
            {
                // Check if the project contains a reference to the dll already.
                VSProject project = GetVSProject();
                bool hasRef = project.References.Cast<Reference>()
                    .Any(r => r != null && string.Equals(r.Name, "protobuf-net", StringComparison.InvariantCultureIgnoreCase));

                if (!hasRef)
                {
                    // Add the reference.
                    Reference dllRef = project.References.Add(dll);
                    dllRef.CopyLocal = true;
                }
            }
            catch (Exception e)
            {
                GeneratorError(4, "Failed to add reference to " + dll + ":" + e.Message, 1, 1);
            }
        }
    }
}
