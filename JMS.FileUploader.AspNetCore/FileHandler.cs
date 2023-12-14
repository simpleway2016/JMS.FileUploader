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
        public string UploadId;
        public int TotalReceived;
        public long FileLength;
        private IUploadFilter _uploadFilter;
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
        int _initFlag = 0;
        /// <summary>
        /// 记录已经接收了哪些position
        /// </summary>
        ConcurrentDictionary<long, bool> _positionCaches = new ConcurrentDictionary<long, bool>();

        internal static string RootFolder;
        bool _inited = false;
        public FileHandler(string name, string uploadId, int fileItemIndex, long fileLength, IUploadFilter uploadFilter)
        {
            if(RootFolder == null)
            {
                RootFolder = AppDomain.CurrentDomain.BaseDirectory + "$$JmsUploaderTemps";
            }

            Name = name;
            UploadId = uploadId;
            FileItemIndex = fileItemIndex;
            FileLength = fileLength;
            _uploadFilter = uploadFilter;

        }



        public async ValueTask Init(HttpContext context, string uploadId, string fileName,long fileSize,int fileItemIndex)
        {
            if (_initFlag == 2)
                return;

            if (Interlocked.CompareExchange(ref _initFlag, 1, 0) == 0)
            {
                try
                {
                    if (_fileStream == null && _uploadFilter == null)
                    {
                        if (Directory.Exists(RootFolder) == false)
                        {
                            Directory.CreateDirectory(RootFolder);
                        }

                        _tempFilePath = Path.Combine(RootFolder, Guid.NewGuid().ToString("N"));
                        _fileStream = File.Create(_tempFilePath);
                    }
                    else if (_uploadFilter != null)
                    {
                        await _uploadFilter.OnUploadBeginAsync(context, uploadId, fileName , fileSize , fileItemIndex);
                    }
                }
                catch(Exception ex)
                {
                    throw;
                }
                finally
                {
                    _initFlag = 2;
                }
            }
            else
            {
                while(_initFlag != 2)
                {
                    await Task.Delay(10);
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
                            try
                            {
                                await _uploadFilter.OnReceivedAsync(context,stream,  position, blockSize);
                                TotalReceived += blockSize;
                            }
                            catch (Exception)
                            {
                                _positionCaches.Remove(position,out _);
                                throw;
                            }
                            finally
                            {
                                _lockFlag = 0;
                            }
                           
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
                        await _uploadFilter.OnUploadCompletedAsync(context);
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
                        try
                        {
                            _fileStream.Seek(position, SeekOrigin.Begin);
                            _fileStream.Write(data);

                            TotalReceived += data.Length;
                        }
                        catch (Exception)
                        {
                            _positionCaches.Remove(position, out _);
                            throw;
                        }
                        finally
                        {
                            _lockFlag = 0;
                        }
                       
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
            _positionCaches.Clear();
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;

            }

            if (_uploadFilter != null && !this.Completed)
            {
                _uploadFilter.OnUploadError();
                _uploadFilter = null;

            }
        }
    }
}
