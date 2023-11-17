using JMS;
using System.IO;
using System.Net.Http;
using System.Web;
using Way.Lib;

namespace JMS
{
    public class JmsUploadClient
    {
        public event EventHandler<int> UploadProgress;
        private readonly IHttpClientFactory _httpClientFactory;
        public long FileLength { get; private set; }
        const int BlockSize = 102400;
        int _completed;
        int _currentIndex;
        int _maxIndex;
        string _url;
        TaskCompletionSource _taskCompletionSource;
        public string FilePath { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string TranId { get; private set; }
        public string FileName { get; private set; }
        public JmsUploadClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> Upload(string url, string filePath, Dictionary<string, string> headers, object state)
        {
            this.FilePath = filePath;
            this.Headers = headers;
            _url = url;
            this.TranId = Guid.NewGuid().ToString("N");
            this.FileName = Path.GetFileName(filePath);
            this.FileLength = new FileInfo(filePath).Length;

            _maxIndex = (int)(this.FileLength / BlockSize);
            if (this.FileLength % BlockSize > 0)
            {
                _maxIndex++;
            }
            _maxIndex--;

            _currentIndex = Math.Min(5, _maxIndex);
            _taskCompletionSource = new TaskCompletionSource();

            for (var i = 0; i <= 5 && i <= _maxIndex; i++)
            {
                this.handleItem(i);
            }
            await _taskCompletionSource.Task;

            HttpContent content = new StringContent(state?.ToJsonString());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var client = _httpClientFactory.CreateClient("");
            client.DefaultRequestHeaders.Add("Jack-Upload-Length", this.FileLength.ToString());
            client.DefaultRequestHeaders.Add("Name",HttpUtility.UrlEncode( this.FileName, System.Text.Encoding.UTF8));
            client.DefaultRequestHeaders.Add("Upload-Id", this.TranId);

            using var response = await client.PostAsync(_url, content);//改成自己的
            var msg = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {

                throw new Exception(msg);
            }
            return msg;

        }

        async void handleItem(int index)
        {
            var size = BlockSize;
            if (index == _maxIndex)
            {
                size = (int)(this.FileLength - BlockSize * _maxIndex);
            }
            var upload = new UploadTask(_httpClientFactory, _url, this, index * BlockSize, size);
            await upload.Run();

            Interlocked.Increment(ref _completed);

            if (_completed == _maxIndex + 1)
            {
                _taskCompletionSource.SetResult();
                return;
            }

            if (this.UploadProgress != null)
            {
                this.UploadProgress(this, (int)(_completed * 100 / _maxIndex));
            }

            if (_currentIndex == _maxIndex)
            {
                return;
            }

            index = Interlocked.Increment(ref _currentIndex);
            handleItem(index);
        }
    }

    class UploadTask
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _url;
        private readonly JmsUploadClient _jackUploadClient;
        private readonly long _position;
        private readonly long _size;

        public UploadTask(IHttpClientFactory httpClientFactory, string url, JmsUploadClient jackUploadClient, long position, int size)
        {
            _httpClientFactory = httpClientFactory;
            _url = url;
            _jackUploadClient = jackUploadClient;
            _position = position;
            _size = size;
        }

        public async Task Run()
        {

            byte[] data = new byte[_size];
            using (var fs = new FileStream(_jackUploadClient.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(_position, SeekOrigin.Begin);
                fs.ReadAtLeast(data, data.Length, true);
            }

            ByteArrayContent content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            while (true)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("");
                    if (_jackUploadClient.Headers != null)
                    {
                        foreach (var pair in _jackUploadClient.Headers)
                        {
                            client.DefaultRequestHeaders.Add(pair.Key, pair.Value);
                        }
                    }
                    client.DefaultRequestHeaders.Add("Jack-Upload-Length", $"{_jackUploadClient.FileLength},{_position},{_size}");
                    client.DefaultRequestHeaders.Add("Name", _jackUploadClient.FileName);
                    client.DefaultRequestHeaders.Add("Upload-Id", _jackUploadClient.TranId);

                    using var response = await client.PostAsync(_url, content);//改成自己的
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await Task.Delay(1000);
                        continue;
                    }

                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                    continue;
                }
            }
        }
    }
}
