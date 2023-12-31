﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace JMS.FileUploader.AspNetCore
{
    internal static class UploadReception
    {
        internal static ConcurrentDictionary<string, Type> Filters = new ConcurrentDictionary<string, Type>();

        internal const int MaxBlockSize = 1024000;
        internal static long MaxFileSize;
        static ConcurrentDictionary<string, FileHandler> _ReceivingDict = new ConcurrentDictionary<string, FileHandler>();

        static UploadReception()
        {
            try
            {
                var files = Directory.GetFiles(FileHandler.RootFolder);
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception)
            {

            }
            new Thread(CheckOutTime).Start();
        }
        static void CheckOutTime()
        {
            while (true)
            {
                Thread.Sleep(60000);
                try
                {
                    foreach (var pair in _ReceivingDict)
                    {
                        if ((DateTime.Now - pair.Value._lastReceiveTime).TotalMinutes > 10)
                        {
                            if (_ReceivingDict.TryRemove(pair.Key, out FileHandler o))
                            {
                                o.Dispose();
                                try
                                {
                                    if (File.Exists(o.FilePath))
                                    {
                                        File.SetAttributes(o.FilePath, FileAttributes.Normal);
                                        File.Delete(o.FilePath);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        static IUploadFilter getUploadFilter(HttpContext httpContext)
        {
            if (Filters.Count == 0)
                return null;

            var desc = httpContext.Request.Headers["Upload-Filter"].ToString();
            if (string.IsNullOrWhiteSpace(desc))
                desc = "";

            if (Filters.TryGetValue(desc, out Type type))
            {
                return (IUploadFilter)httpContext.RequestServices.GetService(type);
            }

            return null;
        }


        internal static FileHandler[] GetUploadingInfo(HttpContext context)
        {
            string uploadId = context.Request.Headers["Upload-Id"].FirstOrDefault();

            return _ReceivingDict.Where(m => m.Value.UploadId == uploadId).Select(m => m.Value).ToArray();
        }


        public static async Task HandleUpload(HttpContext context, StringValues filelen)
        {
            

            try
            {
                string fileName = HttpUtility.UrlDecode(context.Request.Headers["Name"].FirstOrDefault(), System.Text.Encoding.UTF8);
                string uploadId = context.Request.Headers["Upload-Id"].FirstOrDefault();
                int fileItemIndex = 0;
                int.TryParse(context.Request.Headers["File-Index"], out fileItemIndex);


                var arr = filelen.ToString().Split(',');
                var length = long.Parse(arr[0]);
                var position = long.Parse(arr[1]);
                var blockSize = int.Parse(arr[2]);

                if (blockSize > MaxBlockSize || context.Request.ContentLength > MaxBlockSize || context.Request.ContentLength == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("文件大小不正确");
                    return;
                }
                if (length > MaxFileSize)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("文件大小超出限制");
                    return;
                }

                var uploadingInfo = _ReceivingDict.GetOrAdd($"{fileName},{uploadId},{fileItemIndex}", k => new FileHandler(fileName, uploadId, fileItemIndex, length, getUploadFilter(context)));

                if (uploadingInfo.Completed)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("无效的文件块");
                    return;
                }

                await uploadingInfo.Init(context, uploadId, fileName, length, fileItemIndex);

                await uploadingInfo.Receive(context, uploadId, fileName, fileItemIndex, length, position, context.Request.Body, blockSize);
                await context.Response.WriteAsync("ok");
            }
            catch(UploadFilterException ex)
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync(ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(ex.Message);
            }
        }
    }




}
