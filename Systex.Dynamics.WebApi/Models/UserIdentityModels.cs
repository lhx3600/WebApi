using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace SYSTEX.Dynamics.WebApi.Models
{
    /// <summary>
    /// 用户验证信息存储模型
    /// </summary>
    public class UserIdentityManager
    {
        private static List<ClaimInfo> _UserIdentityManager =
            new List<ClaimInfo>();

        /// <summary>
        /// 用于全局获取的组织服务.
        /// </summary>
        public static IOrganizationService OrgService { get; private set; }

        /// <summary>
        /// 添加验证信息
        /// </summary>
        public static void AddUserIdentity(string username, IOrganizationService service)
        {
            if (_UserIdentityManager != null)
                _UserIdentityManager.Add(new ClaimInfo(username, service));
        }

        /// <summary>
        /// 通过用户名查询对应的初始化服务信息
        /// </summary>
        public static IOrganizationService FindCrmService(ClaimsIdentity identity)
        {
            IOrganizationService service = null;
            try
            {
                if (identity.Claims != null && identity.Claims.Count() > 0)
                {
                    string roleType = identity.Claims.First(a => a.Type == "RoleType").Value;
                    string userName = identity.Claims.First(a => a.Type == "UserName").Value;

                    if (_UserIdentityManager != null && _UserIdentityManager.Count > 0)
                        service = _UserIdentityManager.Find(a => a.UserName == userName).Service;

                    if (service == null)
                    {
                        //如果会话失效，则重新添加.
                        string name = identity.Claims.First(a => a.Type == "UserName").Value;
                        string pwd = identity.Claims.First(a => a.Type == "Password").Value;

                        if (roleType == "1")
                            service = new CRMService().OrgService;
                        else service = new CRMService().GetCrmService(name, pwd);

                        UserIdentityManager.AddUserIdentity(name, service);
                    }

                    //全局服务赋值
                    UserIdentityManager.OrgService = service;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return service;
        }

        /// <summary>
        /// 通过用户名查询对应的初始化服务信息
        /// </summary>
        public static string FindClientId(ClaimsIdentity identity)
        {
            string clientid = string.Empty;
            if (identity.Claims != null && identity.Claims.Count() > 0)
            {
                string roleType = identity.Claims.First(a => a.Type == "RoleType").Value;
                clientid = identity.Claims.First(a => a.Type == "ClientId").Value;
            }
            return clientid;
        }

        /// <summary>
        /// 清空存储信息
        /// </summary>
        public static void Clear()
        {
            _UserIdentityManager.Clear();
        }

        private class ClaimInfo
        {
            public string UserName { get; set; }

            public IOrganizationService Service { get; set; }

            public ClaimInfo() { }

            public ClaimInfo(string username, IOrganizationService service)
            {
                this.UserName = username;
                this.Service = service;
            }
        }
    }
}