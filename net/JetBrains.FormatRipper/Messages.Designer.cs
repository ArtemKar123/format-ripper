﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JetBrains.SignatureVerifier {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("JetBrains.SignatureVerifier.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Certificate has been revoked at {0}. {1}.
        /// </summary>
        internal static string certificate_revoked {
            get {
                return ResourceManager.GetString("certificate_revoked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid OCSP response.
        /// </summary>
        internal static string invalid_ocsp_response {
            get {
                return ResourceManager.GetString("invalid_ocsp_response", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Signer&apos;s certificate not found.
        /// </summary>
        internal static string signer_cert_not_found {
            get {
                return ResourceManager.GetString("signer_cert_not_found", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to determine certificate revocation status.
        /// </summary>
        internal static string unable_determin_certificate_revocation_status {
            get {
                return ResourceManager.GetString("unable_determin_certificate_revocation_status", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown certificate revocation status.
        /// </summary>
        internal static string unknown_certificate_revocation_status {
            get {
                return ResourceManager.GetString("unknown_certificate_revocation_status", resourceCulture);
            }
        }
    }
}