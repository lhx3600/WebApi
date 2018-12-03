using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SYSTEX.Dynamics.WebApi.Models
{
    /// <summary>
    /// 请求元数据模型
    /// </summary>
    public class MetadataEntityModel
    {
        /// <summary>
        /// 方法名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 方法代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 搜索字段
        /// </summary>
        public string Search { get; set; }

        /// <summary>
        /// 实体ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 关键字
        /// </summary>
        public string KeyWord { get; set; }

        /// <summary>
        /// 页数 (仅在列表查询时使用，默认显示50条)
        /// </summary>
        public int Count { get; set; } = 50;

        /// <summary>
        /// 每页显示的数量
        /// </summary>
        public int PageCount { get; set; } = 0;

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; } = 0;

        /// <summary>
        /// 参数1
        /// </summary>
        public string P1 { get; set; } = "";
        /// <summary>
        /// 参数2
        /// </summary>
        public string P2 { get; set; } = "";
        /// <summary>
        /// 参数3
        /// </summary>
        public string P3 { get; set; } = "";
        /// <summary>
        /// 参数4
        /// </summary>
        public string P4 { get; set; } = "";
        /// <summary>
        /// 参数5
        /// </summary>
        public string P5 { get; set; } = "";
        /// <summary>
        /// 参数6
        /// </summary>
        public string P6 { get; set; } = "";
        /// <summary>
        /// 参数7
        /// </summary>
        public string P7 { get; set; } = "";
        /// <summary>
        /// 参数8
        /// </summary>
        public string P8 { get; set; } = "";
        ///<summary>
        /// 参数9
        /// </summary>
        public string P9 { get; set; } = "";
        /// <summary>
        /// 参数10
        /// </summary>
        public string P10 { get; set; } = "";

    }
}
