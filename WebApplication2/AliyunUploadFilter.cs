﻿using Aliyun.OSS;
using JMS.FileUploader.AspNetCore;
using System.Collections.Concurrent;
using System.Security.AccessControl;
using Way.Lib;

namespace WebApplication2
{
    [UploadFilterDescription("Aliyun")]
    public class AliyunUploadFilter : IUploadFilter
    {
        const string BucketName = "domainconfig";

        string _uploadId;
        string _ossUploadId;
        
        string _objectKey;
        OssClient _ossClient;
        public async Task OnUploadBeginAsync(HttpContext context, string uploadId, string fileName, long fileSize, int fileItemIndex)
        {
            _uploadId = uploadId;

            _objectKey = $"domain/a{fileItemIndex}.zip";
            var content = File.ReadAllText("a.txt").FromJson<Info>();

            _ossClient = new OssClient(content.Url, content.Key, content.Id);

            var ret = _ossClient.InitiateMultipartUpload(new InitiateMultipartUploadRequest(BucketName, _objectKey));
            if (ret.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(ret.HttpStatusCode.ToString());

            _ossUploadId = ret.UploadId;
        }

        public async Task OnReceivedAsync(HttpContext context, Stream inputStream, long position, int size)
        {

            var data = new byte[size];
            await inputStream.ReadAtLeastAsync(data, size);

            using var ms = new MemoryStream(data);

         
            var num = (int)(position / 102400) + 1;
            var ret = _ossClient.UploadPart(new UploadPartRequest(BucketName, _objectKey, _ossUploadId) { 
                InputStream = ms,
                
                PartSize = size,
                PartNumber = num
            });

            if (ret.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(ret.HttpStatusCode.ToString());
        }


        public async Task<string> OnUploadCompletedAsync(HttpContext context)
        {


            // 列出所有分块。
            var listPartsRequest = new ListPartsRequest(BucketName, _objectKey, _ossUploadId);
            var partList = _ossClient.ListParts(listPartsRequest);

            // 创建CompleteMultipartUploadRequest对象。
            var completeRequest = new CompleteMultipartUploadRequest(BucketName, _objectKey, _ossUploadId);

            // 设置分块列表。
            foreach (var part in partList.Parts)
            {
                completeRequest.PartETags.Add(new PartETag(part.PartNumber, part.ETag));
            }

            // 完成上传。
            var ret = _ossClient.CompleteMultipartUpload(completeRequest);

           
            if (ret.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(ret.HttpStatusCode.ToString());

            //设置访问权限
            _ossClient.SetObjectAcl(BucketName, _objectKey, CannedAccessControlList.PublicRead);

            //获得下载的url路径
            var downloadUrl = ret.Location;


            return downloadUrl;
        }

        public void OnUploadError()
        {
           
        }

    }

    class UploadingOss
    {
        public OssClient Client { get; set; }
        public string UploadId { get; set; }
    }

    class Info
    {
        public string Url;
        public string Key;
        public string Id;
    }
}
