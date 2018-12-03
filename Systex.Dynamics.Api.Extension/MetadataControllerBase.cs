/*******************************************************************************
 * Copyright © 2018 SYSTEXUCOM 版权所有
 * Author: SYSTEX R&D Team
 * Description: SYSTEX通用接口
 * Website：http://www.systexucom.com
*********************************************************************************/
#region  using dll files
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Xml.Linq;
using System.Xml.XPath;
#endregion 

namespace Systex.Dynamics.Api.Extension
{
    [RoutePrefix("api/data")]
    /// <summary>
    /// 元数据控制
    /// </summary>
    public sealed partial class MetadataController : ApiControllerBase, IMetadata
    {
        [Route("list")]
        [HttpPost]
        public IHttpActionResult GetEntityList([FromBody]MetadataEntityModel model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null) return BadRequest("service 无效!");

            //通过名称查询对应的配置信息
            var config = GetConfigByName(service, model.Code);
            if (config == null) return BadRequest("参数无效!");

            //将查询结果集拼接返回成指定的格式
            List<Dictionary<string, object>> collection = new
                List<Dictionary<string, object>>();

            try
            {
                #region 获取系统可配置参数

                //获取查询语句和搜索条件
                string strFetchXml = config.GetAttributeValue<string>("su_fetchxml");
                bool blnSearch = config.GetAttributeValue<bool>("su_issearch");
                string keyword = config.GetAttributeValue<string>("su_keyword");
                string parameter = config.GetAttributeValue<string>("su_parameter");

                //是否使用空变量
                bool blnEmptyVar = config.GetAttributeValue<bool>("su_emptyvariable");
                string emptyword = config.GetAttributeValue<string>("su_emptyword");

                //设置返回值
                _showName = config.GetAttributeValue<bool>("su_showname");
                _showType = config.GetAttributeValue<bool>("su_showtype");
                _showLabel = config.GetAttributeValue<bool>("su_showlabel");

                #endregion 获取系统参数

                //判断是否需要指定的参数
                if (strFetchXml.Contains("{#id}"))
                {
                    if (string.IsNullOrEmpty(model.Id)) return BadRequest("参数不能为空!");
                    strFetchXml = strFetchXml.Replace("{#id}", model.Id); //替换参数值
                }

                var reader = new System.IO.StringReader(strFetchXml);
                XDocument doc = XDocument.Load(reader);
                doc.Element("fetch").SetAttributeValue("no-lock", "true");

                //如果启用查询且输入关键字,则拼接对应的查询条件
                if (blnSearch && !string.IsNullOrEmpty(model.KeyWord))
                {
                    var eleFilterOr = new XElement("filter", new XAttribute("type", "or"));

                    keyword.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList<string>().ForEach(key =>
                        {
                            var element = new XElement("condition",
                             new XAttribute("attribute", key),
                             new XAttribute("operator", "like"),
                             new XAttribute("value", "%" + model.KeyWord + "%"));

                            eleFilterOr.Add(element);
                        });

                    doc.XPathSelectElement("/fetch/entity").Add(eleFilterOr);
                }

                //添加分页效果
                if (model.PageCount != 0 && model.PageNumber != 0)
                {
                    doc.Element("fetch").SetAttributeValue("page", model.PageNumber);
                    doc.Element("fetch").SetAttributeValue("count", model.PageCount);
                }
                else doc.Element("fetch").SetAttributeValue("top", model.Count);

                //使用fetchxml替换.内置ID参数
                string fetchXml = doc.ToString();
                fetchXml = fetchXml.Replace("{#clientid}",
                    UserIdentityManager.FindClientId(User.Identity as ClaimsIdentity));

                //加载传入参数的值
                fetchXml = string.Format(fetchXml,
                    model.P1, model.P2, model.P3, model.P4, model.P5, model.P6,
                    model.P7, model.P8, model.P9, model.P10);

                //如果开启空变量处理,参数中包含指定的替换符号,则移除对应的节点
                if (blnEmptyVar)
                {
                    var d = XDocument.Load(new StringReader(fetchXml));
                    var eleEqualAttrs = d.XPathSelectElements("//condition[@value]");
                    eleEqualAttrs.ToList().ForEach(e =>
                    {
                        if (e.Attribute("value").Value == emptyword)
                            e.Remove();
                    });
                    fetchXml = d.ToString();
                }

                var ecResult = service.RetrieveMultiple(new FetchExpression(fetchXml));
                ecResult.Entities.ToList().ForEach(en =>
                {
                    var values = new Dictionary<string, object>();
                    values.Add("id", en.Id.ToString());

                    en.Attributes.ToList().ForEach(item =>
                    {
                        if (item.Key != en.LogicalName + "id")
                            AppendToValue(item, values, en.FormattedValues);
                    });

                    collection.Add(values);
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Json<List<Dictionary<string, object>>>(collection);
        }

        /// <summary>
        /// 查询视图对应的信息
        /// </summary>
        /// <param name="model">请求模型</param>
        /// <returns>返回列表信息</returns>
        [Route("view")]
        [HttpPost]
        public IHttpActionResult GetEntityListByView([FromBody]MetadataEntityModel model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null) return BadRequest("service 无效!");

            if (string.IsNullOrEmpty(model.Id) || string.IsNullOrEmpty(model.Code))
                return BadRequest("参数不能为空!");

            //将查询结果集拼接返回成指定的格式
            var collection = new List<Dictionary<string, object>>();

            try
            {
                string name = model.Code == "0" ? "savedquery" : "userquery";
                var response = service.Retrieve(name, Guid.Parse(model.Id), new ColumnSet("fetchxml"));

                //获取查询语句和搜索条件
                string strFetchXml = response.GetAttributeValue<string>("fetchxml");

                //设置返回值
                _showName = true;
                _showType = true;
                _showLabel = true;

                var reader = new System.IO.StringReader(strFetchXml);
                XDocument doc = XDocument.Load(reader);
                doc.Element("fetch").SetAttributeValue("no-lock", "true");

                //如果启用查询且输入关键字,则拼接对应的查询条件
                if (!string.IsNullOrEmpty(model.KeyWord) && !string.IsNullOrEmpty(model.Search))
                {
                    var eleFilterOr = new XElement("filter", new XAttribute("type", "or"));

                    model.Search.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList<string>().ForEach(key =>
                        {
                            var element = new XElement("condition",
                             new XAttribute("attribute", key),
                             new XAttribute("operator", "like"),
                             new XAttribute("value", "%" + model.KeyWord + "%"));

                            eleFilterOr.Add(element);
                        });

                    doc.XPathSelectElement("/fetch/entity").Add(eleFilterOr);
                }

                //添加分页效果
                if (model.PageCount != 0 && model.PageNumber != 0)
                {
                    doc.Element("fetch").SetAttributeValue("page", model.PageNumber);
                    doc.Element("fetch").SetAttributeValue("count", model.PageCount);
                }
                else doc.Element("fetch").SetAttributeValue("top", model.Count);

                var ecResult = service.RetrieveMultiple(new FetchExpression(doc.ToString()));

                ecResult.Entities.ToList().ForEach(en =>
                {
                    var values = new Dictionary<string, object>();
                    values.Add("id", en.Id.ToString());

                    en.Attributes.ToList().ForEach(item =>
                    {
                        if (item.Key != en.LogicalName + "id")
                            AppendToValue(item, values, en.FormattedValues);
                    });

                    collection.Add(values);
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Json<List<Dictionary<string, object>>>(collection);
        }

        /// <summary>
        /// 查询指定的实体数据
        /// </summary>
        /// <param name="model">实体信息 (实体名称，实体ID, 字段集合为必须项)</param>
        /// <returns></returns>
        [Route("form")]
        [HttpPost]
        public IHttpActionResult GetEntityById([FromBody]MetadataEntityModel model)
        {
            Dictionary<string, string> collection = new Dictionary<string, string>();
            //获取当前上下文服务
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null) return BadRequest("service 无效!");

            //通过名称查询对应的配置信息
            var config = GetConfigByName(service, model.Code);
            if (config == null) return BadRequest("无法找到对应的配置文件!");

            //使用内置主键值
            bool blnInternalKey = config.GetAttributeValue<bool>("su_useinternalkey");
            string strFetchXml = config.GetAttributeValue<string>("su_fetchxml");

            string id = model.Id; //传入主键Id
            if (string.IsNullOrEmpty(id) && !blnInternalKey)
                return BadRequest("参数不能为空!");

            if (blnInternalKey) id = UserIdentityManager.FindClientId(User.Identity as ClaimsIdentity);
            strFetchXml = strFetchXml.Replace("{#id}", id); //替换参数值

            //设置返回值
            _showName = config.GetAttributeValue<bool>("su_showname");
            _showType = config.GetAttributeValue<bool>("su_showtype");
            _showLabel = config.GetAttributeValue<bool>("su_showlabel");

            var entitycollection = service.RetrieveMultiple(new FetchExpression(strFetchXml));

            Dictionary<string, object> values = new Dictionary<string, object>();
            if (entitycollection.Entities.Count > 0)
            {
                var entity = entitycollection.Entities[0];
                if (entity != null)
                {
                    values.Add("id", entity.Id.ToString());
                    entity.Attributes.ToList().ForEach(item =>
                    {
                        if (item.Key != entity.LogicalName + "id")
                            AppendToValue(item, values, entity.FormattedValues);
                    });
                }
            }
            return Json<Dictionary<string, object>>(values);
        }

        /// <summary>
        /// 创建指定的数据
        /// </summary>
        /// <param name="model">实体模型</param>
        /// <returns>返回添加的产品信息</returns>
        [Route("add")]
        [HttpPost]
        public IHttpActionResult Create([FromBody]dynamic model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null || model["code"] == null) return BadRequest("参数不能为空!");

            //通过名称查询对应的配置信息
            Entity config = GetConfigByName(service, model.code.ToString());
            if (config == null) return BadRequest("无法找到对应的配置文件!");
            LogHelper.Initialization(config.Id);

            //获取字段和类型的Json字符串
            string output = config.GetAttributeValue<string>("su_output");
            string entityname = config.GetAttributeValue<string>("su_entityname");
            var data = JSONHelper.JSONToObject<dynamic>(output);

            if (model != null && ((Newtonsoft.Json.Linq.JContainer)model).Count > 0)
            {
                try
                {
                    Entity enCreate = new Entity(entityname);
                    foreach (var item in model)
                    {
                        //移除不可用项目
                        if (item.Name == "code")
                            continue;

                        if (((Dictionary<string, object>)data).Keys.Contains((string)item.Name))
                        {
                            //为实体添加指定的属性
                            enCreate[item.Name.ToLower()] =
                              ConvertToCrmType
                              (
                                  data[item.Name],
                                  item.Value.ToString()
                              );
                            if (item.Name.ToLower() == "ownerid" && data[item.Name] == "team")
                            {
                                enCreate["owneridtype"] = 9;
                            }
                        }
                    }

                    if (enCreate.Attributes.Count == 0)
                        return BadRequest("属性为不能为空!");

                    Guid id = service.Create(enCreate);
                    return Ok<string>(id.ToString());
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex, entityname + "创建接口执行有误.");
                    return BadRequest(ex.Message);
                }
            }
            return BadRequest("传入无效的参数!");
        }
        
        /// <summary>
        /// 修改指定的数据
        /// </summary>
        /// <param name="model">修改请求模型</param>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("update")]
        [HttpPost]
        public IHttpActionResult Update([FromBody]dynamic model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null || model["id"] == null) return BadRequest("参数不能为空!");

            //通过名称查询对应的配置信息
            Entity config = GetConfigByName(service, model.code.ToString());
            if (config == null) return BadRequest("无法找到对应的配置文件!");
            LogHelper.Initialization(config.Id);

            //获取字段和类型的Json字符串
            string output = config.GetAttributeValue<string>("su_output");
            var data = JSONHelper.JSONToObject<dynamic>(output);
            string entityname = config.GetAttributeValue<string>("su_entityname");

            if (model != null && ((Newtonsoft.Json.Linq.JContainer)model).Count > 0)
            {
                try
                {
                    Entity enUpdate = new Entity(entityname);
                    foreach (var item in model)
                    {
                        if (item.Name == "id")
                        {
                            enUpdate.Id = Guid.Parse(item.Value.ToString());
                            continue;
                        }

                        //移除不可用项目
                        if (item.Name == "code")
                            continue;

                        if (((Dictionary<string, object>)data).Keys.Contains((string)item.Name))
                        {
                            //为实体添加指定的属性
                            enUpdate[item.Name.ToLower()] =
                                ConvertToCrmType
                                (
                                    data[item.Name],
                                    item.Value.ToString()
                                );
                            if (item.Name.ToLower() == "ownerid" && data[item.Name] == "team")
                            {
                                enUpdate["owneridtype"] = 9;
                            }
                        }
                    }
                    if (enUpdate.Attributes.Count == 0)
                        return BadRequest("请提交修改的属性值!");

                    service.Update(enUpdate);
                    return Ok<string>("Success!");
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex, entityname + "修改接口执行有误.");
                    return BadRequest(ex.Message);
                }
            }
            return BadRequest("传入无效的参数");
        }

        /// <summary>
        /// 执行批量操作
        /// </summary>
        /// <param name="models">修改请求模型</param>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("execute")]
        [HttpPost]
        public IHttpActionResult Execute([FromBody]dynamic models)
        {
            try
            {
                if ((int)models.Count <= 0) return BadRequest("参数不能为空!");
            }
            catch (Exception) { return BadRequest("参数无效!"); }

            //初始化服务对象
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null) return BadRequest("参数不能为空!");

            //初始化批量操作请求
            var requestMultiple = new ExecuteTransactionRequest();
            requestMultiple.Requests = new OrganizationRequestCollection();
            requestMultiple.ReturnResponses = true;

            try
            {
                for (int i = 0; i < (int)models.Count; i++)
                {
                    var model = models[i];

                    //通过名称查询对应的配置信息
                    Entity config = GetConfigByName(service, model.code.ToString());
                    if (config == null) return BadRequest("无法找到对应的配置文件!");
                    LogHelper.Initialization(config.Id);

                    //获取字段和类型的Json字符串
                    string output = config.GetAttributeValue<string>("su_output");
                    var data = JSONHelper.JSONToObject<dynamic>(output);
                    string entityname = config.GetAttributeValue<string>("su_entityname");
                    int type = config.GetAttributeValue<OptionSetValue>("su_type").Value;

                    if (type == 40 && model["id"] == null) return BadRequest("参数不能为空!");

                    if (model != null && ((Newtonsoft.Json.Linq.JContainer)model).Count > 0)
                    {

                        Entity entity = new Entity(entityname);
                        foreach (var item in model)
                        {
                            if (item.Name == "id" && type == 40)
                            {
                                entity.Id = Guid.Parse(item.Value.ToString());
                                continue;
                            }

                            //移除不可用项目
                            if (item.Name == "code")
                                continue;

                            if (((Dictionary<string, object>)data).Keys.Contains((string)item.Name))
                            {
                                //为实体添加指定的属性
                                entity[item.Name.ToLower()] =
                                    ConvertToCrmType
                                    (
                                        data[item.Name],
                                        item.Value.ToString()
                                    );
                            }
                        }
                        if (entity.Attributes.Count == 0)
                            return BadRequest("成员属性不能为空!");

                        if (type == 30)
                        {
                            CreateRequest request = new CreateRequest();
                            request.Target = entity;
                            requestMultiple.Requests.Add(request);
                        }
                        else if (type == 40)
                        {
                            UpdateRequest request = new UpdateRequest();
                            request.Target = entity;
                            requestMultiple.Requests.Add(request);
                        }
                    }
                }

                if (requestMultiple.Requests.Count > 0)
                {
                    List<string> result = new List<string>();

                    //执行批量操作
                    var response = (ExecuteTransactionResponse)service.Execute(requestMultiple);
                    if (response.Responses.Count > 0)
                    {
                        response.Responses.Where(a => a.Results.Count > 0).ToList().ForEach(a => {
                            result.Add(a.Results["id"].ToString());
                        });
                    }

                    return Ok<List<string>>(result);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "批量请求接口执行有误.");
                return BadRequest(ex.Message);
            }

            return BadRequest("传入无效的参数");
        }

        /// <summary>
        /// 删除指定的数据
        /// </summary>
        /// <param name="model">删除请求模型(仅需要EntityName,Id)</param>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("delete")]
        [HttpPost]
        public IHttpActionResult Delete([FromBody]MetadataEntityModel model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);
            if (service == null || string.IsNullOrEmpty(model.Id) || string.IsNullOrEmpty(model.Name))
                return BadRequest("参数不能为空!");

            try
            {
                service.Delete(model.Name, Guid.Parse(model.Id));
                return Ok<string>("Success!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 批量删除指定的数据
        /// </summary>
        /// <param name="model">删除请求模型(仅需要EntityName,Id集合)</param>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("batchdelete")]
        [HttpPost]
        public IHttpActionResult DeleteALL([FromBody]dynamic model)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);

            try
            {
                if (model == null || model.name == null || model.id == null ||
                JSONHelper.JSONToObject<string[]>(model.id.ToString()).Length <= 0)
                    return BadRequest("参数不能为空!");
            }
            catch (Exception ex)
            {
                return BadRequest("参数无效！");
            }

            try
            {
                string name = model.name.ToString();
                var requestMultiple = new ExecuteTransactionRequest();
                requestMultiple.Requests = new OrganizationRequestCollection();
                requestMultiple.ReturnResponses = true;

                string[] parameter = JSONHelper.JSONToObject<string[]>(model.id.ToString());
                parameter.ToList().ForEach(p =>
                {
                    var request = new DeleteRequest();
                    request.Target = new EntityReference(name, Guid.Parse(p));
                    requestMultiple.Requests.Add(request);
                });

                var response = (ExecuteTransactionResponse)service.Execute(requestMultiple);
                return Ok<string>("Success!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 附件上传
        /// </summary>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("upload")]
        [HttpPost]
        public async Task<HttpResponseMessage> Upload()
        {
            // 检查是否是 multipart/form-data
            if (!Request.Content.IsMimeMultipartContent("form-data"))
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            // var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);

            string root = HttpContext.Current.Server.MapPath("~/App_Data");//指定要将文件存入的服务器物理位置
            var provider = new RenamingMultipartFormDataStreamProvider(root); //通过重命名获取实际信息
            try
            {
                //读取提交文件，保存至指定目录下
                await Request.Content.ReadAsMultipartAsync(provider);

                //获取已接收文件的信息
                foreach (MultipartFileData file in provider.FileData)
                {
                    //获取上传文件实际的文件名
                    string filename = file.Headers.ContentDisposition.FileName;
                    //获取上传文件在服务上默认的文件名
                    string path = "Server file path: " + file.LocalFileName;
                }
            }
            catch
            {
                throw;
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// 下载指定的文件
        /// </summary>
        /// <returns>返回成功的影响(包含传入值)</returns>
        [Route("download/{name}")]
        [HttpGet]
        public HttpResponseMessage Download(string name)
        {
            var service = UserIdentityManager.FindCrmService(User.Identity as ClaimsIdentity);

            HttpResponseMessage result = null;

            DirectoryInfo directoryInfo = new DirectoryInfo(HostingEnvironment.MapPath("~/App_Data"));
            FileInfo foundFileInfo = directoryInfo.GetFiles().Where(x => x.Name == name).FirstOrDefault();
            if (foundFileInfo != null)
            {
                FileStream fs = new FileStream(foundFileInfo.FullName, FileMode.Open);

                result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new StreamContent(fs);
                result.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(MimeMapping.GetMimeMapping(name));
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                result.Content.Headers.ContentDisposition.FileName = foundFileInfo.Name;
                result.Content.Headers.ContentLength = fs.Length;
                result.ReasonPhrase = "/files/" + name;
            }
            else
            {
                result = new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return result;
        }
    }
}

