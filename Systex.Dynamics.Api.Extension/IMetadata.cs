using System.Web.Http;

namespace Systex.Dynamics.Api.Extension
{
    interface IMetadata
    {
        /// <summary>
        /// 查询指定实体的列表
        /// </summary>
        /// <param name="model">请求模型</param>
        /// <returns>返回列表信息</returns>
        [Route("list")]
        IHttpActionResult GetEntityList([FromBody]MetadataEntityModel model);
    }
}
