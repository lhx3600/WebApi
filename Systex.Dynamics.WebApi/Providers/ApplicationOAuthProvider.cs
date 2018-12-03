using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using System.Text;
using Systex.Dynamics.Api.Extension;

namespace Systex.Dynamics.WebApi.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 角色类型
        /// </summary>
        public string RoleType { get; set; }

        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            //context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });  //跨域设置
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            string newpassword = context.Password; //获取新密码
            //if (CheckUserPassword(ref newpassword))
            //{
            //    context.SetError("invalid_grant", "请求非法。");
            //    return;
            //}

            var crmService = new CRMService().GetCrmService(context.UserName, newpassword);
            if (crmService == null)
            {
                context.SetError("invalid_grant", "用户名或密码不正确。");
                return;
            }
            identity.AddClaim(new Claim("UserName", context.UserName));
            identity.AddClaim(new Claim("Password", newpassword));
            //UserIdentityManager.AddUserIdentity(context.UserName, crmService);
            
            var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());
            context.Validated(ticket);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        #region client模式
        /// <summary>
        /// 客户端验证模式
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            var crmServiceClient = new CRMService();

            UserName = context.Parameters["username"];
            Password = context.Parameters["password"];
            RoleType = context.Parameters["type"];

            string clientId = string.Empty; //最终客户端唯一凭证

            ////资源验证
            //if (UserName != null && Password != null && RoleType != null)
            //{
            //    #region
            //    //第三方用户验证
            //    if (RoleType == "1")
            //    {
            //        var crmService = crmServiceClient.OrgService;
            //        clientId = crmServiceClient.GetPartUserId(UserName, Password);

            //        if (string.IsNullOrEmpty(clientId))
            //            context.SetError("invalid_grant", "用户名或密码不正确。");

            //        else {
            //            context.Validated(clientId);
            //            UserIdentityManager.AddUserIdentity(UserName, crmService);
            //        }
            //    }
            //    else {
            //        var crmService = new CRMService().GetCrmService(UserName, Password);
            //        if (crmService == null)
            //            context.SetError("invalid_grant", "用户名或密码不正确。");

            //        else {
            //            context.Validated(clientId);
            //            UserIdentityManager.AddUserIdentity(UserName, crmService);
            //        }
            //    }
            //    #endregion
            //}
            //else {
            //    context.SetError("invalid_grant", "请输入用户名和密码。");
            //}
            //context.Validated(clientId);
            return base.ValidateClientAuthentication(context);
        }

        /// <summary>
        /// 分派客户端验证值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim("UserName", UserName));
            identity.AddClaim(new Claim("Password", Password));
            identity.AddClaim(new Claim("RoleType", RoleType));
            identity.AddClaim(new Claim("ClientId", context.ClientId));

            var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());
            context.Validated(ticket);

            return base.GrantClientCredentials(context);
        }

        #endregion


        public static AuthenticationProperties CreateProperties(string userName)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }

        /// <summary>
        /// 验证输入的密码
        /// </summary>
        /// <param name="password">加密密码</param>
        /// <returns></returns>
        private static bool CheckUserPassword(ref string password)
        {
            bool blnError = false;
            try
            {
                string uncode = Encoding.UTF8.GetString(Convert.FromBase64String(password));

                string ts1 = uncode.Substring(uncode.Length - 16);
                string ts2 = ((double.Parse(ts1) * 100000).ToString() + "0000000").Remove(17);
                TimeSpan ts = new TimeSpan(long.Parse(ts2));
                DateTime dtNow = DateTime.Parse("1970-01-01").Add(ts);

                if ((DateTime.Now - dtNow).TotalMinutes > 0 && (DateTime.Now - dtNow).TotalMinutes <= 5)
                {
                    password = uncode.Replace(ts1, "").Remove(0, 3);
                    password = password.Remove(password.Length - 3, 3);
                }
                else {
                    blnError = true;
                }
            }
            catch (Exception ex)
            {
                blnError = true;
            }
            return blnError;
        }
    }
}