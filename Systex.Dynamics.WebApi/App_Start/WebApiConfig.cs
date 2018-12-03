using System.Web.Http;
using Microsoft.Owin.Security.OAuth;

namespace Systex.Dynamics.WebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务
            // 将 Web API 配置为仅使用不记名令牌身份验证。
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional /*, namespaces = "Systex.Dynamics.WebApi" */}
            );

           // config.Routes.MapHttpRoute(
           //    name: "Extension",
           //    routeTemplate: "api/{controller}/{id}",
           //    defaults: new { id = RouteParameter.Optional, namespaces = "Systex.Dynamics.Api.Extension" }
           //);
        }
    }
}
