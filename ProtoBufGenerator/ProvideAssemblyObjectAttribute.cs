
namespace ProtoBufGenerator
{
    using System;
    using Microsoft.VisualStudio.Shell;
    using VSLangProj80;

    /// <summary>
    /// Attribute for VSPackage registration. Allows new registration information without
    /// changing the registration tools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ProvideAssemblyObjectAttribute : RegistrationAttribute
    {
        /// <summary>
        /// Gets or sets the file extension. Default is .cs
        /// </summary>
        public string FileExtension { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the tool to register.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Gets the object type of the tool to register.
        /// </summary>
        public Type ObjectType { get; private set; }

        /// <summary>
        /// Gets a value that specifies how the assembly should be located.
        /// </summary>
        public RegistrationMethod RegistrationMethod { get; private set; }

        /// <summary>
        /// Gets the globally unique identifier for a COM class object.
        /// </summary>
        private string ClsidRegKey
        {
            get { return string.Format(@"CLSID\{0}", ObjectType.GUID.ToString("B")); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvideAssemblyObjectAttribute"/> class.
        /// </summary>
        /// <param name="objectType">The object type of the tool to register.</param>
        public ProvideAssemblyObjectAttribute(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException("objectType");
            }

            ObjectType = objectType;
        }

        #region RegistrationAttribute Members

        /// <summary>
        /// Register the object using the given registration context.
        /// </summary>
        /// <param name="context">Context information for the registration attribute.</param>
        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(ClsidRegKey))
            {
                key.SetValue(string.Empty, ObjectType.FullName);
                key.SetValue("InprocServer32", context.InprocServerPath);
                key.SetValue("Class", ObjectType.FullName);
                if (context.RegistrationMethod != RegistrationMethod.Default)
                {
                    RegistrationMethod = context.RegistrationMethod;
                }

                switch (RegistrationMethod)
                {
                    case RegistrationMethod.Default:
                    case RegistrationMethod.Assembly:
                        key.SetValue("Assembly", ObjectType.Assembly.FullName);
                        break;

                    case RegistrationMethod.CodeBase:
                        // Explicitly specify the $PackageFolder$ string as the context.CodeBase
                        // actually points to a path specific to my local filesystem.
                        //key.SetValue("CodeBase", context.CodeBase);
                        key.SetValue("CodeBase", "$PackageFolder$\\ProtoBufGenerator.dll");
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                key.SetValue("ThreadingModel", "Both");
            }

            using (Key shellRegistryFileKey = context.CreateKey(
                string.Format(@"Generators\{0}\{1}",
                vsContextGuids.vsContextGuidVCSProject,
                FileExtension)))
            {
                shellRegistryFileKey.SetValue(string.Empty, ToolName);
            }
        }

        /// <summary>
        /// Unregister the object using the given registration context.
        /// </summary>
        /// <param name="context">Context information for the registration attribute.</param>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(ClsidRegKey);
        }

        #endregion
    }
}
