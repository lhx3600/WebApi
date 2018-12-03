using Microsoft.Xrm.Sdk;
using System;

namespace Systex.Dynamics.Api.Extension
{
    /// <summary>
    /// 错误处理类
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// 接口配置实体ID
        /// </summary>
        private static Guid ConfigId { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Initialization(Guid configid)
        {
            LogHelper.ConfigId = configid;
        }

        /// <summary>
        /// 消息
        /// </summary>
        public static void Info(Exception ex = null, string message = null)
        {
            WriteCrmLog(10, message, ex);
        }

        /// <summary>
        /// 调试
        /// </summary>
        public static void Debug(string message = null)
        {
            WriteCrmLog(20, message);
        }

        /// <summary>
        /// 警告
        /// </summary>
        public static void Warn(Exception ex = null, string message = null)
        {
            WriteCrmLog(30, message, ex);
        }

        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(Exception ex = null, string message = null)
        {
            WriteCrmLog(40, message, ex);
        }

        /// <summary>
        ///  创建日志数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        private static void WriteCrmLog(int type, string message = "", Exception ex = null)
        {
            //创建接口日志
            var interfaceLog = new Entity("su_interface_log");
            interfaceLog["su_type"] = new OptionSetValue(type);
            interfaceLog["su_name"] = message;

            if (ex != null)
                interfaceLog["su_message"] = InputMessage(ex);

            //创建关联关系
            if (LogHelper.ConfigId != null)
                interfaceLog["su_interface_configid"] = new EntityReference(
                    "su_interface_config", LogHelper.ConfigId);

            //UserIdentityManager.OrgService.Create(interfaceLog);
        }

        /// <summary>
        /// 收集错误详细信息
        /// </summary>
        private static string InputMessage(Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Message: " + ex.Message);
            sb.AppendLine("StackTrace: " + ex.StackTrace);
            if (ex.Data != null)
            {
                foreach (var key in ex.Data.Keys)
                {
                    sb.AppendLine("key: " + key + ",value: " + ex.Data[key]);
                }
            }
            Exception parentEx = ex.InnerException;
            while (parentEx != null)
            {
                if (parentEx.InnerException != null)
                {
                    parentEx = parentEx.InnerException;
                }
                else
                {
                    break;
                }
            }
            if (parentEx != null)
            {
                sb.AppendLine("StackTrace: " + parentEx.StackTrace);
                if (parentEx.Data != null)
                {
                    foreach (var key in parentEx.Data.Keys)
                    {
                        sb.AppendLine("key: " + key + ",value: " + parentEx.Data[key]);
                    }
                }
            }
            return sb.ToString();
        }
    }
}