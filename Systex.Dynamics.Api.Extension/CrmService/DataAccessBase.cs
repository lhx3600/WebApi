using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Systex.Dynamics.Api.Extension
{
    public class DataAccessBase
    {
        #region Variables && Constants

        protected string CurrentUserId = string.Empty;       //当前用户
        protected string CRMMode = string.Empty;             //CRM部署模式(On-Premise、IFD)
        protected string CRMTransportProtocol = string.Empty;//传输协议
        protected string CRMOrganizationName = string.Empty; //组织名称
        protected string CRMServerHost = string.Empty;       //主机
        protected string CRMServerPort = string.Empty;       //端口
        protected string CRMDomainName = string.Empty;       //域
        protected string CRMUserName = string.Empty;         //用户
        protected string CRMUserPassword = string.Empty;     //密码
        protected string DBConstr = string.Empty;            //连接字符串
        protected string EmailSendServerType = string.Empty; //邮件服务器类型
        protected string EmailSendServer = string.Empty;     //邮件服务器地址
        protected string EmailFromAccount = string.Empty;    //邮箱账号
        protected string EmailFromAccountPassword = string.Empty; //密码
        protected string EmailServerDomain = string.Empty;   //邮件服务器域
     
        #endregion Variables && Constants

        #region Initialization

        public DataAccessBase()
        {
            if (string.IsNullOrEmpty(this.CRMDomainName)
                && string.IsNullOrEmpty(this.CRMMode))
                InitializeConfig();
        }

        /// <summary>
        /// 初始化系统配置参数
        /// </summary>
        public void InitializeConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(System.Web.HttpContext.Current.Server.MapPath("/Configs/Credentials.xml"));

                XmlNode xmlnode = doc.SelectSingleNode("CRMExtensionConfig/CustomList");

                CRMMode = xmlnode.SelectSingleNode("CRMMode").InnerText;
                CRMOrganizationName = xmlnode.SelectSingleNode("CRMOrganizationName").InnerText;
                CRMTransportProtocol = xmlnode.SelectSingleNode("CRMTransportProtocol").InnerText;
                CRMServerHost = xmlnode.SelectSingleNode("CRMServerHost").InnerText;
                CRMServerPort = xmlnode.SelectSingleNode("CRMServerPort").InnerText;
                CRMDomainName = xmlnode.SelectSingleNode("CRMDomainName").InnerText;
                CRMUserName = xmlnode.SelectSingleNode("CRMUserName").InnerText;
                CRMUserPassword = xmlnode.SelectSingleNode("CRMUserPassword").InnerText;
                DBConstr = xmlnode.SelectSingleNode("CRMSQLConnStr").InnerText;
                EmailSendServerType = xmlnode.SelectSingleNode("EmailSendServerType").InnerText;
                EmailSendServer = xmlnode.SelectSingleNode("EmailSendServer").InnerText;
                EmailFromAccount = xmlnode.SelectSingleNode("EmailFromAccount").InnerText;
                EmailFromAccountPassword = xmlnode.SelectSingleNode("EmailFromAccountPassword").InnerText;
                EmailServerDomain = xmlnode.SelectSingleNode("EmailServerDomain").InnerText;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //获取指定名称的appSettings的值
        private string GetAppKeyValue(string strKey)
        {
            return ConfigurationManager.AppSettings[strKey];
        }

        #endregion Initialization
    }
}
