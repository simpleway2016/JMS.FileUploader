大文件上传处理中间件，支持断点续传

引用 nuget 包：JMS.UploadFile.AspNetCore

先定义一个接收类：
``` cs
    //[Authorize] // 如果要进行身份验证，可以加入Microsoft.AspNetCore.Authorization.Authorize特性，并通过header.User获取用户信息
    public class MyUploadReception : IUploadFileReception
    {
        FileStream fs;
        public void OnBeginUploadFile(UploadHeader header, bool isContinue)
        {
            fs = new FileStream($"./{header.FileName}",FileMode.OpenOrCreate , FileAccess.Write , FileShare.ReadWrite);
            if (isContinue)
            {
                fs.Seek(header.Position, SeekOrigin.Begin);
            }
        }

        public void OnError(UploadHeader header)
        {
            fs.Close();
            fs.Dispose();
        }

        public void OnReceivedFileContent(UploadHeader header, byte[] data, int length, long filePosition)
        {
            fs.Write(data, 0, length);
        }

        public void OnUploadCompleted(UploadHeader header)
        {
            fs.Close();
            fs.Dispose();
        }
    }
```
然后在 app.Run 之前注册这个接收类
``` cs
            app.UseJmsUploadFile<MyUploadReception>(new JMS.UploadFile.AspNetCore.Option("uploadtest")
            {
                MaxFileLength = 1024 * 1024 * 100  //文件限制在100m
            }) ;

```

**Html页面的使用**

``` html
<body>
    <input id="file" type="file" />
    <button onclick="obj.upload()">
        upload
    </button>
    <div id="info"></div>
</body>
<script lang="ja">
    var info = document.body.querySelector("#info");

    //引用nodejs模块
    var JMSUploadFile = require("jms-uploadfile");
    var fileObj = document.body.querySelector("#file");
    var obj = new JMSUploadFile(fileObj , "uploadtest");
    //如果js和asp.net不在同一个站点，那么需要传一下asp.net的基本url=>  var obj = new JMSUploadFile(fileObj , "uploadtest" , "http://127.0.0.1:5000");
    obj.onProgress = function (sender, total, sended) {
        info.innerHTML = sended + "," + total;
    }
    obj.onCompleted = function (sender) {
        info.innerHTML = "ok";
    }
    obj.onError = function (sender, err) {
        info.innerHTML = JSON.stringify( err );
        //如果断点续传，这里直接调用obj.upload()即可
    }
</script>
```

***TypeScript in webpack***
tsconfig.json
```
{
  "compilerOptions": {
    "outDir": "./dist/",
    "sourceMap": true,
    "noImplicitAny": false,
    "module": "es2015",
    "moduleResolution": "node",
    "target": "es5",
    "allowJs": true,
    "types": [
      "./node_modules/jms-uploadfile",
    ]
  }
}

```
**import**
```
import JMSUploadFile from "jms-uploadfile"

```
