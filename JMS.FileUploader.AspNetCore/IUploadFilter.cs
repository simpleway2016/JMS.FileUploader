using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JMS.FileUploader.AspNetCore
{
    public interface IUploadFilter
    {
        void OnUploadError();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="uploadId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileItemIndex">同时上传多个文件时，此变量表示文件的排序号</param>
        /// <returns></returns>
        Task OnUploadBeginAsync(HttpContext context, string uploadId, string fileName, long fileSize, int fileItemIndex);
        /// <summary>
        /// 收到上传的文件数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="inputStream"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        Task OnReceivedAsync(HttpContext context, Stream inputStream, long position, int size);
        Task<string> OnUploadCompletedAsync(HttpContext context);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UploadFilterDescriptionAttribute : Attribute
    {
        public UploadFilterDescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// 名称描述
        /// </summary>
        public string Description { get; }
    }
}
