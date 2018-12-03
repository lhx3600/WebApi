// =====================================================================
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================
//<snippetCrmServiceHelper>
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.DirectoryServices.AccountManagement;

// These namespaces are found in the Microsoft.Xrm.Sdk.dll assembly
// located in the SDK\bin folder of the SDK download.
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Crm.Services.Utility;


namespace SYSTEX.Data
{
    /// <summary>
    /// Provides server connection information.
    /// </summary>
    public class ServerConnection
    {
        #region Inner classes
        /// <summary>
        /// Stores Microsoft Dynamics CRM server configuration information.
        /// </summary>
        public class Configuration
        {
            public String ServerAddress;
            public String OrganizationName;
            public Uri DiscoveryUri;
            public Uri OrganizationUri;
            public Uri HomeRealmUri = null;
            public ClientCredentials DeviceCredentials = null;
            public ClientCredentials Credentials = null;
            public AuthenticationProviderType EndpointType;
            public String UserPrincipalName;
            #region internal members of the class
            internal IServiceManagement<IOrganizationService> OrganizationServiceManagement;
            internal SecurityTokenResponse OrganizationTokenResponse;            
            internal Int16 AuthFailureCount = 0;
            #endregion

            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if (obj == null || GetType() != obj.GetType()) return false;

                Configuration c = (Configuration)obj;

                if (!this.ServerAddress.Equals(c.ServerAddress, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (!this.OrganizationName.Equals(c.OrganizationName, StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (this.EndpointType != c.EndpointType)
                    return false;
                if (null != this.Credentials && null != c.Credentials)
                {
                    if (this.EndpointType == AuthenticationProviderType.ActiveDirectory)
                    {

                        if (!this.Credentials.Windows.ClientCredential.Domain.Equals(
                            c.Credentials.Windows.ClientCredential.Domain, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!this.Credentials.Windows.ClientCredential.UserName.Equals(
                            c.Credentials.Windows.ClientCredential.UserName, StringComparison.InvariantCultureIgnoreCase))
                            return false;

                    }
                    else if (this.EndpointType == AuthenticationProviderType.LiveId)
                    {
                        if (!this.Credentials.UserName.UserName.Equals(c.Credentials.UserName.UserName,
                            StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!this.DeviceCredentials.UserName.UserName.Equals(
                            c.DeviceCredentials.UserName.UserName, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                        if (!this.DeviceCredentials.UserName.Password.Equals(
                            c.DeviceCredentials.UserName.Password, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }
                    else
                    {

                        if (!this.Credentials.UserName.UserName.Equals(c.Credentials.UserName.UserName,
                            StringComparison.InvariantCultureIgnoreCase))
                            return false;

                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                int returnHashCode = this.ServerAddress.GetHashCode() 
                    ^ this.OrganizationName.GetHashCode() 
                    ^ this.EndpointType.GetHashCode();
                if (null != this.Credentials)
                {
                    if (this.EndpointType == AuthenticationProviderType.ActiveDirectory)
                        returnHashCode = returnHashCode
                            ^ this.Credentials.Windows.ClientCredential.UserName.GetHashCode()
                            ^ this.Credentials.Windows.ClientCredential.Domain.GetHashCode();
                    else if (this.EndpointType == AuthenticationProviderType.LiveId)
                        returnHashCode = returnHashCode
                            ^ this.Credentials.UserName.UserName.GetHashCode()
                            ^ this.DeviceCredentials.UserName.UserName.GetHashCode()
                            ^ this.DeviceCredentials.UserName.Password.GetHashCode();
                    else
                        returnHashCode = returnHashCode
                            ^ this.Credentials.UserName.UserName.GetHashCode();
                }
                return returnHashCode;
            }

        }
        #endregion Inner classes

        #region Public properties

        public Configuration configurations = null;

        #endregion Public properties

        #region Private properties

        private Configuration config = new Configuration();

        #endregion Private properties

        #region Public methods
        /// <summary>
        /// Obtains the server connection information including the target organization's
        /// Uri and user logon credentials from the user.
        /// </summary>
        public virtual Configuration GetServerConfiguration()
        {
            // Read the configuration from the disk
            Boolean isConfigExist = ReadConfigurations();

            if (isConfigExist)
            {
                config = configurations;
            }
            else
                throw new InvalidOperationException("The specified server configuration does not exist.");
            
            return config;
        }

        /// <summary>
        /// Discovers the organizations that the calling user belongs to.
        /// </summary>
        /// <param name="service">A Discovery service proxy instance.</param>
        /// <returns>Array containing detailed information on each organization that 
        /// the user belongs to.</returns>
        public OrganizationDetailCollection DiscoverOrganizations(IDiscoveryService service)
        {
            if (service == null) throw new ArgumentNullException("service");
            RetrieveOrganizationsRequest orgRequest = new RetrieveOrganizationsRequest();
            RetrieveOrganizationsResponse orgResponse =
                (RetrieveOrganizationsResponse)service.Execute(orgRequest);

            return orgResponse.Details;
        }

        /// <summary>
        /// Finds a specific organization detail in the array of organization details
        /// returned from the Discovery service.
        /// </summary>
        /// <param name="orgFriendlyName">The friendly name of the organization to find.</param>
        /// <param name="orgDetails">Array of organization detail object returned from the discovery service.</param>
        /// <returns>Organization details or null if the organization was not found.</returns>
        /// <seealso cref="DiscoveryOrganizations"/>
        public OrganizationDetail FindOrganization(string orgFriendlyName,
            OrganizationDetail[] orgDetails)
        {
            if (String.IsNullOrWhiteSpace(orgFriendlyName))
                throw new ArgumentNullException("orgFriendlyName");
            if (orgDetails == null)
                throw new ArgumentNullException("orgDetails");
            OrganizationDetail orgDetail = null;

            foreach (OrganizationDetail detail in orgDetails)
            {
                if (String.Compare(detail.FriendlyName, orgFriendlyName,
                    StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    orgDetail = detail;
                    break;
                }
            }
            return orgDetail;
        }

        /// <summary>
        /// Reads a server configuration file.
        /// Read the configuration from disk, if it exists, at C:\Users\YourUserName\AppData\Roaming\CrmServer\Credentials.xml.
        /// </summary>
        /// <returns>Is configuration settings already available on disk.</returns>
        public Boolean ReadConfigurations()
        {
            Boolean isConfigExist = false;
            
            if (File.Exists(System.Web.HttpContext.Current.Server.MapPath(CrmServiceHelperConstants.ServerCredentialsFile)))
            {
                XElement configurationsFromFile =
                    XElement.Load(System.Web.HttpContext.Current.Server.MapPath(CrmServiceHelperConstants.ServerCredentialsFile));
                foreach (XElement config in configurationsFromFile.Nodes())
                {
                    Configuration newConfig = new Configuration();
                    var serverAddress = config.Element("ServerAddress");
                    if (serverAddress != null)
                        if (!String.IsNullOrEmpty(serverAddress.Value))
                            newConfig.ServerAddress = serverAddress.Value;
                    var organizationName = config.Element("OrganizationName");
                    if (organizationName != null)
                        if (!String.IsNullOrEmpty(organizationName.Value))
                            newConfig.OrganizationName = organizationName.Value;
                    var discoveryUri = config.Element("DiscoveryUri");
                    if (discoveryUri != null)
                        if (!String.IsNullOrEmpty(discoveryUri.Value))
                            newConfig.DiscoveryUri = new Uri(discoveryUri.Value);
                    var organizationUri = config.Element("OrganizationUri");
                    if (organizationUri != null)
                        if (!String.IsNullOrEmpty(organizationUri.Value))
                            newConfig.OrganizationUri = new Uri(organizationUri.Value);
                    var homeRealmUri = config.Element("HomeRealmUri");
                    if (homeRealmUri != null)
                        if (!String.IsNullOrEmpty(homeRealmUri.Value))
                            newConfig.HomeRealmUri = new Uri(homeRealmUri.Value);

                    var vendpointType = config.Element("EndpointType");
                    if (vendpointType != null)
                        newConfig.EndpointType =
                                RetrieveAuthenticationType(vendpointType.Value);
                    if (config.Element("Credentials").HasElements)
                    {
                        newConfig.Credentials = new ClientCredentials();
                        newConfig.Credentials.UserName.UserName = config.Element("Credentials").Element("UserName").Value;
                        newConfig.Credentials.UserName.Password = config.Element("Credentials").Element("Password").Value;
                            //ParseInCredentials(config.Element("Credentials"),
                            //newConfig.EndpointType,
                            //newConfig.ServerAddress + ":" + newConfig.OrganizationName + ":" + );
                    }
                    if (newConfig.EndpointType == AuthenticationProviderType.LiveId)
                    {
                        newConfig.DeviceCredentials = GetDeviceCredentials();
                    }
                    var userPrincipalName = config.Element("UserPrincipalName");
                    if (userPrincipalName != null)
                        if (!String.IsNullOrWhiteSpace(userPrincipalName.Value))
                            newConfig.UserPrincipalName = userPrincipalName.Value;

                    configurations = newConfig;
                }
            }

            if (configurations != null)
                isConfigExist = true;

            return isConfigExist;
        }

       

         //<summary>
         //Obtains the user's logon credentials for the target server.
         //</summary>
         //<param name = "config" > An instance of the Configuration.</param>
         //<returns>Logon credentials of the user.</returns>
        public static ClientCredentials GetUserLogonCredentials(ServerConnection.Configuration config)
        {
            ClientCredentials credentials = new ClientCredentials();
            String userName;
            SecureString password;
            String domain;
            Boolean isCredentialExist = (config.Credentials != null) ? true : false;
            switch (config.EndpointType)
            {
                // An on-premises Microsoft Dynamics CRM server deployment. 
                case AuthenticationProviderType.ActiveDirectory:
                    // Uses credentials from windows credential manager for earlier saved configuration.
                    if (isCredentialExist && !String.IsNullOrWhiteSpace(config.OrganizationName))
                    {
                        domain = config.Credentials.Windows.ClientCredential.Domain;
                        userName = config.Credentials.Windows.ClientCredential.UserName;
                        if (String.IsNullOrWhiteSpace(config.Credentials.Windows.ClientCredential.Password))
                        {
                            Console.Write("\nEnter domain\\username: ");
                            Console.WriteLine(
                            config.Credentials.Windows.ClientCredential.Domain + "\\"
                            + config.Credentials.Windows.ClientCredential.UserName);

                            Console.Write("       Enter Password: ");
                            password = ReadPassword();
                        }
                        else
                        {
                            password = config.Credentials.Windows.ClientCredential.SecurePassword;
                        }
                    }
                    // Uses default credentials saved in windows credential manager for current organization.
                    else if (!isCredentialExist && !String.IsNullOrWhiteSpace(config.OrganizationName))
                    {
                        return null;
                    }
                    // Prompts users to enter credential for current organization.
                    else
                    {
                        String[] domainAndUserName;
                        do
                        {
                            Console.Write("\nEnter domain\\username: ");
                            domainAndUserName = Console.ReadLine().Split('\\');

                            // If user do not choose to enter user name, 
                            // then try to use default credential from windows credential manager.
                            if (domainAndUserName.Length == 1 && String.IsNullOrWhiteSpace(domainAndUserName[0]))
                            {
                                return null;
                            }
                        }
                        while (domainAndUserName.Length != 2 || String.IsNullOrWhiteSpace(domainAndUserName[0])
                            || String.IsNullOrWhiteSpace(domainAndUserName[1]));

                        domain = domainAndUserName[0];
                        userName = domainAndUserName[1];

                        Console.Write("       Enter Password: ");
                        password = ReadPassword();
                    }
                    if (null != password)
                    {
                        credentials.Windows.ClientCredential =
                            new System.Net.NetworkCredential(userName, password, domain);
                    }
                    else
                    {
                        credentials.Windows.ClientCredential = null;
                    }

                    break;
                // A Microsoft Dynamics CRM Online server deployment. 
                case AuthenticationProviderType.LiveId:
                // An internet-facing deployment (IFD) of Microsoft Dynamics CRM.          
                case AuthenticationProviderType.Federation:
                // Managed Identity/Federated Identity users using Microsoft Office 365.
                case AuthenticationProviderType.OnlineFederation:
                    // Use saved credentials.
                    if (isCredentialExist)
                    {
                        userName = config.Credentials.UserName.UserName;
                        if (String.IsNullOrWhiteSpace(config.Credentials.UserName.Password))
                        {
                            Console.Write("\n Enter Username: ");
                            Console.WriteLine(config.Credentials.UserName.UserName);

                            Console.Write(" Enter Password: ");
                            password = ReadPassword();
                        }
                        else
                        {
                            password = ConvertToSecureString(config.Credentials.UserName.Password);
                        }
                    }
                    // For OnlineFederation environments, initially try to authenticate with the current UserPrincipalName
                    // for single sign-on scenario.
                    else if (config.EndpointType == AuthenticationProviderType.OnlineFederation
                        && config.AuthFailureCount == 0
                        && !String.IsNullOrWhiteSpace(UserPrincipal.Current.UserPrincipalName))
                    {
                        config.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;
                        return null;
                    }
                    // Otherwise request username and password.
                    else
                    {
                        config.UserPrincipalName = String.Empty;
                        if (config.EndpointType == AuthenticationProviderType.LiveId)
                            Console.Write("\n Enter Microsoft account: ");
                        else
                            Console.Write("\n Enter Username: ");
                        userName = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            return null;
                        }

                        Console.Write(" Enter Password: ");
                        password = ReadPassword();
                    }
                    credentials.UserName.UserName = userName;
                    credentials.UserName.Password = ConvertToUnsecureString(password);
                    break;
                default:
                    credentials = null;
                    break;
            }
            return credentials;
        }

        /// <summary>
        /// Prompts user to enter password in console window 
        /// and capture the entered password into SecureString.
        /// </summary>
        /// <returns>Password stored in a secure string.</returns>
        public static SecureString ReadPassword()
        {
            SecureString ssPassword = new SecureString();

            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key == ConsoleKey.Backspace)
                {
                    if (ssPassword.Length != 0)
                    {
                        ssPassword.RemoveAt(ssPassword.Length - 1);
                        Console.Write("\b \b");     // erase last char
                    }
                }
                else if (info.KeyChar >= ' ')           // no control chars
                {
                    ssPassword.AppendChar(info.KeyChar);
                    Console.Write("*");
                }
                info = Console.ReadKey(true);
            }

            Console.WriteLine();
            Console.WriteLine();

            // Lock the secure string password.
            ssPassword.MakeReadOnly();

            return ssPassword;
        }

        /// <summary>
        /// Convert SecureString to unsecure string.
        /// </summary>
        /// <param name="securePassword">Pass SecureString for conversion.</param>
        /// <returns>unsecure string</returns>
        public static String ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Convert unsecure string to SecureString.
        /// </summary>
        /// <param name="password">Pass unsecure string for conversion.</param>
        /// <returns>SecureString</returns>
        public static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();
            return securePassword;
        }
       #endregion Public methods

        #region Protected methods

        /// <summary>
        /// Obtains the name and port of the server running the Microsoft Dynamics CRM
        /// Discovery service.
        /// </summary>
        /// <returns>The server's network name and optional TCP/IP port.</returns>
        protected virtual String GetServerAddress(out bool ssl)
        {
            ssl = false;

            Console.Write("Enter a CRM server name and port [crm.dynamics.com]: ");
            String server = Console.ReadLine();

            if (server.EndsWith(".dynamics.com") || String.IsNullOrWhiteSpace(server))
            {
                ssl = true;
            }
            else
            {
                Console.Write("Is this server configured for Secure Socket Layer (https) (y/n) [n]: ");
                String answer = Console.ReadLine();

                if (answer == "y" || answer == "Y")
                    ssl = true;
            }

            return server;
        }

        /// <summary>
        /// Is this organization provisioned in Microsoft Office 365?
        /// </summary>
        /// <param name="server">The server's network name.</param>
        protected virtual Boolean GetOrgType(String server)
        {
            Boolean isO365Org = false;
            if (String.IsNullOrWhiteSpace(server))
                return isO365Org;
            if (server.IndexOf('.') == -1)
                return isO365Org;

            Console.Write("Is this organization provisioned in Microsoft Office 365 (y/n) [y]: ");
            String answer = Console.ReadLine();

            if (answer == "y" || answer == "Y" || answer.Equals(String.Empty))
                isO365Org = true;

            return isO365Org;
        }
        
        /// <summary>
        /// Get the device credentials by either loading from the local cache 
        /// or request new device credentials by registering the device.
        /// </summary>
        /// <returns>Device Credentials.</returns>
        protected virtual ClientCredentials GetDeviceCredentials()
        {
            return Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
        }
        
        /// <summary>
        /// Verify passed strings with the supported AuthenticationProviderType.
        /// </summary>
        /// <param name="authType">String AuthenticationType</param>
        /// <returns>Supported AuthenticatoinProviderType</returns>
        private AuthenticationProviderType RetrieveAuthenticationType(String authType)
        {
            switch (authType)
            {
                case "ActiveDirectory":
                    return AuthenticationProviderType.ActiveDirectory;
                case "LiveId":
                    return AuthenticationProviderType.LiveId;
                case "Federation":
                    return AuthenticationProviderType.Federation;
                case "OnlineFederation":
                    return AuthenticationProviderType.OnlineFederation;
                default:
                    throw new ArgumentException(String.Format("{0} is not a valid authentication type", authType));
            }
        }

      
        #endregion Private methods

        #region Private Classes
        /// <summary>
        /// private static class to store constants required by the CrmServiceHelper class.
        /// </summary>
        private static class CrmServiceHelperConstants
        {
            /// <summary>
            /// Credentials file path.
            /// </summary>
            public static readonly string ServerCredentialsFile = "../../Configs/Credentials.xml";
        }
        #endregion        
    }
}
//</snippetCrmServiceHelper>
