using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JMS.FileUploader.AspNetCore
{
    class FileHandler : IDisposable
    {
        public string Name;
        public string TranId;
        public int TotalReceived;
        public long FileLength;
        private readonly IUploadFilter _uploadFilter;
        public bool Completed;

        public string FilePath
        {
            get
            {
                return _tempFilePath;
            }
        }

        public int FileItemIndex { get; }

        string _tempFilePath;
        FileStream _fileStream;
        internal DateTime _lastReceiveTime = DateTime.Now;
        int _lockFlag = 0;
        /// <summary>
        /// 记录已经接收了哪些position
        /// </summary>
        ConcurrentDictionary<long, bool> _positionCaches = new ConcurrentDictionary<long, bool>();

        internal const string RootFolder = "./$$JmsUploaderTemps";

        public FileHandler(string name, string tranId, int fileItemIndex, long fileLength, IUploadFilter uploadFilter)
        {
            Name = name;
            TranId = tranId;
            FileItemIndex = fileItemIndex;
            FileLength = fileLength;
            _uploadFilter = uploadFilter;
        }



        public void Init()
        {
            if (_fileStream != null)
                return;

            lock (this)
            {
                if (_fileStream == null)
                {
                    if (Directory.Exists(RootFolder) == false)
                    {
                        Directory.CreateDirectory(RootFolder);
                    }

                    _tempFilePath = Path.Combine(RootFolder, Guid.NewGuid().ToString("N"));
                    _fileStream = File.Create(_tempFilePath);
                }
            }
        }

   
        public async Task Receive(HttpContext context, string uploadId, string fileName, int fileItemIndex, long fileSize, long position, Stream stream, int blockSize)
        {
            _lastReceiveTime = DateTime.Now;
            if (_uploadFilter != null)
            {
                if (_positionCaches.TryAdd(position, true))
                {
                    while (true)
                    {
                        if (Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0)
                        {
                            await _uploadFilter.OnReceived(context, uploadId, fileName, fileItemIndex, stream, fileSize, position, blockSize);
                            TotalReceived += blockSize;

                            _lockFlag = 0;
                            break;
                        }
                        else
                        {
                            await Task.Delay(10);
                        }
                    }

                    
                    if (TotalReceived >= FileLength)
                    {
                        this.Completed = true;
                        //接收完毕
                        await _uploadFilter.OnUploadCompleted(context, uploadId, fileName);
                    }
                }
                return;
            }


            var data = new byte[blockSize];
            int readed = 0;
            while (blockSize > 0)
            {
                readed = await stream.ReadAsync(data, data.Length - blockSize, blockSize);
                if (readed == 0)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("连接中断");
                    return;
                }
                blockSize -= readed;
            }


            if (_positionCaches.TryAdd(position, true))
            {

                while (true)
                {
                    if (Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0)
                    {
                        _fileStream.Seek(position, SeekOrigin.Begin);
                        _fileStream.Write(data);
                        TotalReceived += data.Length;

                        _lockFlag = 0;
                        break;
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                }

               
                if (TotalReceived >= FileLength)
                {
                    _fileStream.Close();
                    _fileStream.Dispose();
                    this.Completed = true;
                    //接收完毕
                }
            }
            else
            {
                return;
            }


        }

        public void DeleteFile()
        {
            _lastReceiveTime = DateTime.Now.AddYears(-1);
        }

        public void Dispose()
        {

            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;

            }
        }
    }
}
