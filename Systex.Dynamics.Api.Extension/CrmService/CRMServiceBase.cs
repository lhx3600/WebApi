using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SYSTEX.Data
{
    public class CrmServiceBase
    {
        private static OrganizationServiceProxy _serviceProxy;
        public static OrganizationServiceProxy ServiceProxy
        {
            get
            {
                if (_serviceProxy == null)
                {
                    ServerConnection serverConnect = new ServerConnection();
                    ServerConnection.Configuration serverConfig = serverConnect.GetServerConfiguration();

                    using (_serviceProxy = new OrganizationServiceProxy(serverConfig.OrganizationUri,
                        serverConfig.HomeRealmUri,
                        serverConfig.Credentials,
                        serverConfig.DeviceCredentials))
                    {
                        _serviceProxy.EnableProxyTypes();
                    }
                }
                return _serviceProxy;
            }
        }
    }
}
