using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Systex.Dynamics.Api.Extension
{
    /// <summary>
    /// 重构保存操作
    /// </summary>
    public class RenamingMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public string Root { get; set; }
        //public Func<FileUpload.PostedFile, string> OnGetLocalFileName { get; set; }

        public RenamingMultipartFormDataStreamProvider(string root)
            : base(root)
        {
            Root = root;
        }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            string filePath = headers.ContentDisposition.FileName;

            // Multipart requests with the file name seem to always include quotes.
            if (filePath.StartsWith(@"""") && filePath.EndsWith(@""""))
                filePath = filePath.Substring(1, filePath.Length - 2);

            var filename = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);
            var contentType = headers.ContentType.MediaType;

            return filename;
        }
    }
}
