/*******************************************************************************
 * Copyright © 2018 SYSTEXUCOM 版权所有
 * Author: SYSTEX R&D Team
 * Description: SYSTEX通用接口
 * Website：http://www.systexucom.com
*********************************************************************************/

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Systex.Dynamics.Api.Extension
{
    public partial class MetadataController
    {
        //控制返回扩展值
        private bool _showName = false;
        private bool _showType = false;
        private bool _showLabel = false;

        /// <summary>
        /// 将对应的实体属性添加到数据字典中
        /// </summary>
        /// <param name="keys">实体属性键值对</param>
        /// <param name="values">数据字典</param>
        /// <param name="formatted">格式化数据集合</param>
        private void AppendToValue(KeyValuePair<string, object> keys,
            Dictionary<string, object> values,
            FormattedValueCollection formatted)
        {
            string strKey = keys.Key;
            object objValue = keys.Value;

            if (objValue.GetType() == typeof(AliasedValue))
            {
                objValue = ((AliasedValue)objValue).Value;
                strKey = strKey.Replace(".", "_");
            }

            if (objValue.GetType() == typeof(OptionSetValue))
            {
                values.Add(strKey, ((OptionSetValue)objValue).Value);

                if (_showLabel) //是否显示标签
                    values.Add(strKey + "name", formatted[keys.Key]);

                return;
            }
            else if (objValue.GetType() == typeof(EntityCollection))
            {
                if (((EntityCollection)objValue).EntityName == "activityparty")
                {
                    var activity = ((EntityCollection)objValue).Entities.Select(a => a["partyid"] as EntityReference);
                    values.Add(keys.Key, string.Join("|", activity.Select(a => a.Id)));

                    if (_showName) //是否显示名称
                        values.Add(strKey + "name", string.Join("|", activity.Select(a => a.Name)));

                    if (_showType) //是否显示类型
                        values.Add(strKey + "type", string.Join("|", activity.Select(a => a.LogicalName)));

                    return;
                }
                else {
                    objValue = "";
                }
            }
            else if (objValue.GetType() == typeof(DateTime))
                objValue = ((DateTime)objValue).ToLocalTime().ToString();

            else if (objValue.GetType() == typeof(Money))
                objValue = ((Money)objValue).Value;

            else if (objValue.GetType() == typeof(EntityReference))
            {
                values.Add(strKey, ((EntityReference)objValue).Id);

                if (_showName)
                    values.Add(strKey + "name", ((EntityReference)objValue).Name);

                if (_showType)
                    values.Add(strKey + "type", ((EntityReference)objValue).LogicalName);

                return;
            }

            values.Add(strKey, objValue);
        }

        /// <summary>
        /// 转换为指定的CRM类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static object ConvertToCrmType(string type, string value)
        {
            /*
            * 备注："statuscode":"status","statecode":"state"
            * 说明：以上两种类型未作控制.
            */

            //如果值为空，则直接返回null
            if (string.IsNullOrEmpty(value)) return null;

            //处理查找类型的值
            string lookupType = string.Empty;
            if (type.Contains("|"))
            {
                lookupType = type.Split('|')[1];
                type = type.Split('|')[0];
            }

            object _object;
            switch (type.ToLower().Trim())
            {
                case Constants.CRM_TYPE_STRING:
                case Constants.CRM_TYPE_MEMO:
                    _object = value;
                    break;
                case Constants.CRM_TYPE_INTEGER:
                    _object = int.Parse(value);
                    break;
                case Constants.CRM_TYPE_MONEY:
                    _object = new Money(Convert.ToDecimal(value));
                    break;
                case Constants.CRM_TYPE_PICKLIST:
                    _object = new OptionSetValue(Convert.ToInt32(value));
                    break;
                case Constants.CRM_TYPE_CUSTOMER:
                    _object = new EntityReference("account", Guid.Parse(value));
                    break;
                case Constants.CRM_TYPE_OWNER:
                    _object = new EntityReference("systemuser", Guid.Parse(value));
                    break;
                case Constants.CRM_TYPE_TEAM:
                    _object = new EntityReference("team", Guid.Parse(value));
                    break;
                case Constants.CRM_TYPE_LOOKUP:
                    _object = new EntityReference(lookupType, Guid.Parse(value));
                    break;
                case Constants.CRM_TYPE_DATETIME:
                    _object = Convert.ToDateTime(value);
                    break;
                case Constants.CRM_TYPE_DECIMAL:
                    _object = Convert.ToDecimal(value);
                    break;
                case Constants.CRM_TYPE_DOUBLE:
                    _object = Convert.ToDouble(value);
                    break;
                case Constants.CRM_TYPE_UNIQUE:
                    _object = Guid.Parse(value);
                    break;
                case Constants.CRM_TYPE_BOOLEAN:
                    if (value == "1") value = "true";
                    else if (value == "0") value = "false";
                    _object = Boolean.Parse(value);
                    break;
                default:
                    _object = value;
                    break;
            }
            return _object;
        }


        /// <summary>
        /// 获取字段对应的标签值
        /// </summary>
        private static string GetValue(string strKey, AttributeMetadata[] attributes, bool IsExecute = false)
        {
            string value = attributes.ToList().Find(a => a.DisplayName.UserLocalizedLabel != null
           && a.LogicalName == strKey).DisplayName.UserLocalizedLabel.Label;

            if (IsExecute) value += "名称";
            return value;
        }

        /// <summary>
        /// 通过名称查询对应的接口配置信息
        /// </summary>
        /// <param name="service">CRM服务</param>
        /// <param name="name">名称</param>
        /// <returns>返回配置信息</returns>
        private static Entity GetConfigByName(IOrganizationService service, string name)
        {
            //查询对应的接口配置表
            QueryByAttribute query = new QueryByAttribute("su_interface_config");
            query.AddAttributeValue("su_name", name);
            query.ColumnSet = new ColumnSet(true);
            var result = service.RetrieveMultiple(query);

            if (result != null && result.Entities.Count > 0)
                return result.Entities[0];

            return null;
        }
    }
}
