﻿using Microsoft.AspNet.Identity;
using System.Web.Http;
using Systex.Dynamics.Api.Extension;

namespace Systex.Dynamics.WebApi.Controllers
{
    /// <summary>
    /// 销售管理控制
    /// </summary>
    [RoutePrefix("api/ext")]
    [Authorize]
    public class SalesController : ApiControllerBase
    {
        /// <summary>
        /// 查询指定实体的列表
        /// </summary>
        /// <param name="model">请求模型</param>
        /// <returns>返回列表信息</returns>
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("search")]
        [HttpPost]
        public IHttpActionResult GetEntityListInfo([FromBody]MetadataEntityModel model)
        {
            return null;
        }

        /// <summary>
        /// 查询视图对应的信息
        /// </summary>
        /// <param name="model">请求模型</param>
        /// <returns>返回列表信息</returns>
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("lookup")]
        [HttpPost]
        public IHttpActionResult GetEntityListByViewInfo([FromBody]MetadataEntityModel model)
        {
            return null;
        }

        /// <summary>
        /// 查询指定的实体数据
        /// </summary>
        /// <param name="model">实体信息 (实体名称，实体ID, 字段集合为必须项)</param>
        /// <returns></returns>
        [Route("share")]
        [HttpPost]
        public IHttpActionResult GetEntityByIdInfo([FromBody]MetadataEntityModel model)
        {
            return null;
        }
    }
}

