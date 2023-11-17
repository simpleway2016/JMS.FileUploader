﻿import { v4 as uuidv4 } from 'uuid';
export class JmsUploader {
    file: Blob;
    headers: any = undefined;
    jsonObject: any = undefined;
    url = "";
    tranId = "";

    allFiles: Blob[];
    totalFilesLength = 0;

    onUploading: (percent: number) => void = <any>undefined;
    private completed = 0;
    private currentIndex = 0;
    private fileItemIndex = 0;
    private maxIndex = 0;
    private blockSize = 102400;
    private canceled = false;
    completedSize = 0;

    constructor(url: string, file: File | File[] | FileList, headers: any, jsonObject: any) {
        if (file instanceof FileList) {
            this.allFiles = [];
            for (var i = 0; i < file.length; i++) {
                this.allFiles[i] = file[i];
            }
            this.allFiles.forEach(f => this.totalFilesLength += f.size);
        }
        else if (Array.isArray(file)) {
            this.allFiles = [];
            for (var i = 0; i < file.length; i++) {
                var arrItem = file[i];
                if (arrItem instanceof FileList) {
                    for (var j = 0; j < arrItem.length; j++) {
                        this.allFiles.push(arrItem[j]);
                    }
                }
                else if ("size" in arrItem) {
                    this.allFiles.push(arrItem);
                }
            }
            file.forEach(f => this.totalFilesLength += f.size);
        }
        else if ("size" in file) {
            this.allFiles = [file];
            this.totalFilesLength = file.size;
        }
        else {
            throw "file无法识别";
        }
        this.url = url;


        if (headers && typeof headers != "function") {
            this.headers = JSON.parse(JSON.stringify(headers));
        }
        else {
            this.headers = headers;
        }

        if (jsonObject) {
            this.jsonObject = JSON.parse(JSON.stringify(jsonObject));
        }

       
    }

    private onCompleted = async () => {
        var headers = <any>{
            'Content-Type': 'application/json',
        };

        if (this.headers) {
            var curHeaders = this.headers;

            if (typeof curHeaders == "function") {
                curHeaders = curHeaders();
            }
            for (var p in curHeaders) {
                headers[p] = curHeaders[p];
            }
        }

        headers["Jack-Upload-Length"] = this.file.size;
        headers["Name"] = encodeURIComponent((<any>this.file).name);
        headers["Upload-Id"] = this.tranId;

        var ret = await fetch(`${this.url}`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(this.jsonObject)
        });

        var text = await ret.text();
        if (ret.status >= 300 || ret.status < 200) {
            if (text)
                this.uploadReject(text);
            else
                this.uploadReject({ statusCode: ret.status });
        }
        this.uploadResolve(text);

    }

    private next = (uploadedSize: number) => {
        if (this.canceled)
            return;

        this.completed++;
        this.completedSize += uploadedSize;
        if (this.completed == this.maxIndex + 1) {
            this.fileItemIndex++;

            if (this.fileItemIndex >= this.allFiles.length) {
                this.onCompleted();
            }
            else {
                this.upload();
            }
            return;
        }

        if (this.onUploading) {
            this.onUploading(parseInt(<any>(this.completedSize * 100 / this.totalFilesLength)));
        }

        if (this.currentIndex == this.maxIndex) {
            return;
        }

        this.currentIndex++;

        var size = this.blockSize;
        if (this.currentIndex == this.maxIndex) {
            size = this.file.size - this.blockSize * this.maxIndex;
        }
        new BlockHandler(this, this.currentIndex * this.blockSize, size).upload().then(size => {
            this.next(size);
        });
    }

    private uploadResolve: any = undefined;
    private uploadReject: any = undefined;
    upload = (): Promise<any> => {
        this.file = this.allFiles[this.fileItemIndex];
        this.maxIndex = parseInt(<any>(this.file.size / this.blockSize));
        if (this.file.size % this.blockSize > 0) {
            this.maxIndex++;
        }
        this.maxIndex--;

        this.completed = 0;
        this.completedSize = 0;

        if (this.fileItemIndex == 0) {
            this.tranId = uuidv4();
        }
        this.currentIndex = Math.min(5, this.maxIndex);

        return new Promise((resolve, reject) => {
            if (this.fileItemIndex == 0) {
                this.uploadResolve = resolve;
                this.uploadReject = reject;
            }
            for (var i = 0; i <= 5 && i <= this.maxIndex; i++) {
                this.handleItem(i);
            }

        });
    }


    cancel = () => {
        this.fileItemIndex = 0;
        this.canceled = true;
        if (this.uploadReject) {
            this.uploadReject("canceled");
        }
    }

    private handleItem(index: number) {
        var size = this.blockSize;
        if (index == this.maxIndex) {
            size = this.file.size - this.blockSize * this.maxIndex;
        }
        new BlockHandler(this, index * this.blockSize, size).upload().then(size => {
            this.next(size);
        }).catch(reason => {
            window.setTimeout(() => this.handleItem(index), 1000);
        });
    }
}

class BlockHandler {
    uploader: JmsUploader;
    position = 0;
    size = 0;
    constructor(uploader: JmsUploader, position: number, size: number) {
        this.uploader = uploader;
        this.position = position;
        this.size = size;
    }

    upload = (): Promise<number> => {
        return new Promise(async (resolve, reject) => {
            // 创建一个 ArrayBuffer，这里假设您已经有了二进制数据
            const binaryData = this.uploader.file.slice(this.position, this.position + this.size);

            var headers = <any>{
                'Content-Type': 'application/json',
            };

            if (this.uploader.headers) {
                var curHeaders = this.uploader.headers;

                if (typeof curHeaders == "function") {
                    curHeaders = curHeaders();
                }
                for (var p in curHeaders) {
                    headers[p] = curHeaders[p];
                }
            }

            headers["Jack-Upload-Length"] = `${this.uploader.file.size},${this.position},${this.size}`;
            if ((<any>this.uploader.file).name) {
                headers["Name"] = encodeURIComponent((<any>this.uploader.file).name);
            }
            else {
                headers["Name"] = "none";
            }
            
            headers["Upload-Id"] = this.uploader.tranId;

            var ret: Response;
            try {
                ret = await fetch(`${this.uploader.url}`, {
                    method: 'POST',
                    headers: headers,
                    body: binaryData
                });
            } catch (e) {
                console.error(e);
                reject(e);
                return;
            }

            var text = await ret.text();
            if (ret.status >= 300 || ret.status < 200) {
                if (text)
                    reject(text);
                else
                    reject({ statusCode: ret.status });
            }
            else if (text == "ok") {

                resolve(this.size);
            }
            else
                reject(text);
        });
    }
}