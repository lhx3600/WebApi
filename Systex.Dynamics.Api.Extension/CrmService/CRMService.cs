using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Systex.Dynamics.Api.Extension
{
    public class CRMService : DataAccessBase
    {
        public CRMService()
        {
            if (string.IsNullOrEmpty(CRMMode)) InitializeConfig();
        }

        private IOrganizationService _crmService;
        public IOrganizationService OrgService
        {
            get
            {
                if (_crmService == null) InitializeCrmService();
                return _crmService;
            }
        }
        
        /// <summary>
        /// 初始化系统服务
        /// </summary>
        private void InitializeCrmService()
        {
            try
            {
                Uri orgServiceUri = null;
                ClientCredentials credentials = new ClientCredentials();
                if (CRMMode.ToLower().Equals("on-premise"))
                {
                    if (string.IsNullOrEmpty(CRMTransportProtocol)) CRMTransportProtocol = "http";
                    orgServiceUri = new Uri(CRMTransportProtocol + "://" + CRMServerHost + ":" + CRMServerPort + "/" + CRMOrganizationName + "/XRMServices/2011/Organization.svc");
                    credentials.Windows.ClientCredential = new System.Net.NetworkCredential(CRMUserName, CRMUserPassword, CRMDomainName);
                }
                else
                {
                    if (string.IsNullOrEmpty(CRMTransportProtocol)) CRMTransportProtocol = "https";
                    orgServiceUri = new Uri(CRMTransportProtocol + "://" + CRMServerHost + ":" + CRMServerPort + "/XRMServices/2011/Organization.svc");
                    credentials.UserName.UserName = CRMDomainName + "\\" + CRMUserName;
                    credentials.UserName.Password = CRMUserPassword;
                };

                OrganizationServiceProxy crmServiceProxy = new OrganizationServiceProxy(orgServiceUri, null, credentials, null);
                crmServiceProxy.EnableProxyTypes();
                crmServiceProxy.Timeout = new TimeSpan(0, 15, 0);
                if (!string.IsNullOrEmpty(CurrentUserId)) { crmServiceProxy.CallerId = new Guid(CurrentUserId); }
                _crmService = (IOrganizationService)crmServiceProxy;
            }
            catch (Exception ex)
            {
                //LogHelper.error(ex.ToString());
            }
        }

        /// <summary>
        /// 初始化系统服务
        /// </summary>
        public IOrganizationService GetCrmService(string username,string password)
        {
            IOrganizationService orgService = null;
            try
            {
                Uri orgServiceUri = null;
                ClientCredentials credentials = new ClientCredentials();
                if (CRMMode.ToLower().Equals("on-premise"))
                {
                    if (string.IsNullOrEmpty(CRMTransportProtocol)) CRMTransportProtocol = "http";
                    orgServiceUri = new Uri(CRMTransportProtocol + "://" + CRMServerHost + ":" + CRMServerPort + "/" + CRMOrganizationName + "/XRMServices/2011/Organization.svc");
                    credentials.Windows.ClientCredential = new System.Net.NetworkCredential(username, password, CRMDomainName);
                }
                else
                {
                    if (string.IsNullOrEmpty(CRMTransportProtocol)) CRMTransportProtocol = "https";
                    orgServiceUri = new Uri(CRMTransportProtocol + "://" + CRMServerHost + ":" + CRMServerPort + "/XRMServices/2011/Organization.svc");
                    credentials.UserName.UserName = CRMDomainName + "\\" + username;
                    credentials.UserName.Password = password;
                };

                OrganizationServiceProxy crmServiceProxy = new OrganizationServiceProxy(orgServiceUri, null, credentials, null);
                crmServiceProxy.EnableProxyTypes();
                crmServiceProxy.Timeout = new TimeSpan(0, 15, 0);
                if (!string.IsNullOrEmpty(CurrentUserId)) { crmServiceProxy.CallerId = new Guid(CurrentUserId); }

                var IsThrowError = crmServiceProxy.ServiceChannel;
                orgService = (IOrganizationService)crmServiceProxy;
            }
            catch (Exception ex)   {    }
            return orgService;
        }

        /// <summary>
        /// 获取三方用户信息
        /// </summary>
        public string GetPartUserId(string username, string password)
        {
            string userid = string.Empty;
            try
            {
                QueryByAttribute query = new QueryByAttribute("emo_partylibrary");
                query.ColumnSet = new ColumnSet(false);
                query.AddAttributeValue("emo_mobile", username);
                query.AddAttributeValue("emo_password", password);

                var result = this.OrgService.RetrieveMultiple(query);
                if (result.Entities.Count > 0)
                    userid = result.Entities[0].Id.ToString();
            }
            catch (Exception ex) { }
            return userid;
        }


        /// <summary>
        /// 事务批量提交
        /// </summary>
        private ExecuteTransaction _transaction;

        public ExecuteTransaction TransActionCollection
        {
            get
            {
                if (_transaction == null)
                    _transaction = new ExecuteTransaction(this.OrgService);
                return _transaction;
            }
        }

        /// <summary>
        /// 批量执行请求,包含事务
        /// </summary>
        public class ExecuteTransaction
        {
            private ExecuteTransactionRequest requestMultiple = null;
            private IOrganizationService _service;

            public ExecuteTransaction(IOrganizationService Service)
            {
                if (requestMultiple == null)
                {
                    requestMultiple = new ExecuteTransactionRequest();
                    requestMultiple.Requests = new OrganizationRequestCollection();
                    requestMultiple.ReturnResponses = true;
                }
                this._service = Service;
            }


            /// <summary>
            /// 添加请求类
            /// </summary>
            public void Add(OrganizationRequest request)
            {
                this.requestMultiple.Requests.Add(request);
            }

            public ExecuteTransactionRequest GetRequest()
            {
                return this.requestMultiple;     
            }

            /// <summary>
            /// 执行批量提交
            /// </summary>
            public ExecuteTransactionResponse Execute()
            {
                if (this.requestMultiple.Requests.Count <= 0)
                    throw new ArgumentNullException("成员不能为空!");

                return (ExecuteTransactionResponse)this._service.Execute(requestMultiple);
            }
        }
    }
}
