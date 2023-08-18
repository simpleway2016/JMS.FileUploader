大文件上传处理中间件，支持断点续传

引用 nuget 包：JMS.FileUploader.AspNetCore

``` cs
            app.UseJmsFileUploader();

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
    async function upload() {
        var headers = function(){
	return { "TestHeader" : "abc" };
        };

        var submitBody = {
            value1 : "abc"
        };
        var uploader = new JmsUploader("Main/Test2", document.querySelector("#file").files[0], headers, submitBody);
        uploader.onUploading = function (percent) {
            document.querySelector("#info").innerHTML = percent + "%";
        };

        var ret = await uploader.upload();
        alert(ret);
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
      "./node_modules/jms-uploader",
    ]
  }
}

```
**import**
```
import JmsUploader from "jms-uploader"

```
