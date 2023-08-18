"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
exports.__esModule = true;
exports.JmsUploader = void 0;
var uuid_1 = require("uuid");
var JmsUploader = /** @class */ (function () {
    function JmsUploader(url, file, headers, jsonObject) {
        var _this = this;
        this.fileLength = 0;
        this.headers = undefined;
        this.jsonObject = undefined;
        this.url = "";
        this.tranId = "";
        this.fileName = "";
        this.onUploading = undefined;
        this.completed = 0;
        this.currentIndex = 0;
        this.maxIndex = 0;
        this.blockSize = 102400;
        this.canceled = false;
        this.completedSize = 0;
        this.onCompleted = function () { return __awaiter(_this, void 0, void 0, function () {
            var headers, p, ret, text;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        headers = {
                            'Content-Type': 'application/json'
                        };
                        if (this.headers) {
                            for (p in this.headers) {
                                headers[p] = this.headers[p];
                            }
                        }
                        headers["Jack-Upload-Length"] = this.fileLength;
                        headers["Name"] = this.file.name;
                        headers["Upload-Id"] = this.tranId;
                        return [4 /*yield*/, fetch("" + this.url, {
                                method: 'POST',
                                headers: headers,
                                body: JSON.stringify(this.jsonObject)
                            })];
                    case 1:
                        ret = _a.sent();
                        return [4 /*yield*/, ret.text()];
                    case 2:
                        text = _a.sent();
                        if (ret.status >= 300 || ret.status < 200) {
                            if (text)
                                this.uploadReject(text);
                            else
                                this.uploadReject({ statusCode: ret.status });
                        }
                        this.uploadResolve(text);
                        return [2 /*return*/];
                }
            });
        }); };
        this.next = function (uploadedSize) {
            if (_this.canceled)
                return;
            _this.completed++;
            _this.completedSize += uploadedSize;
            if (_this.completed == _this.maxIndex + 1) {
                _this.onCompleted();
                return;
            }
            if (_this.onUploading) {
                _this.onUploading(parseInt((_this.completedSize * 100 / _this.fileLength)));
            }
            if (_this.currentIndex == _this.maxIndex) {
                return;
            }
            _this.currentIndex++;
            var size = _this.blockSize;
            if (_this.currentIndex == _this.maxIndex) {
                size = _this.fileLength - _this.blockSize * _this.maxIndex;
            }
            new BlockHandler(_this, _this.currentIndex * _this.blockSize, size).upload().then(function (size) {
                _this.next(size);
            });
        };
        this.uploadResolve = undefined;
        this.uploadReject = undefined;
        this.upload = function () {
            _this.completed = 0;
            _this.completedSize = 0;
            _this.tranId = uuid_1.v4();
            _this.currentIndex = Math.min(5, _this.maxIndex);
            return new Promise(function (resolve, reject) {
                _this.uploadResolve = resolve;
                _this.uploadReject = reject;
                for (var i = 0; i <= 5 && i <= _this.maxIndex; i++) {
                    _this.handleItem(i);
                }
            });
        };
        this.cancel = function () {
            _this.canceled = true;
            if (_this.uploadReject) {
                _this.uploadReject("canceled");
            }
        };
        this.file = file;
        this.url = url;
        if (file.size) {
            this.fileLength = file.size;
        }
        if (file.name) {
            this.fileName = file.name;
        }
        if (headers) {
            this.headers = JSON.parse(JSON.stringify(headers));
        }
        if (jsonObject) {
            this.jsonObject = JSON.parse(JSON.stringify(jsonObject));
        }
        this.maxIndex = parseInt((this.fileLength / this.blockSize));
        if (this.fileLength % this.blockSize > 0) {
            this.maxIndex++;
        }
        this.maxIndex--;
    }
    JmsUploader.prototype.handleItem = function (index) {
        var _this = this;
        var size = this.blockSize;
        if (index == this.maxIndex) {
            size = this.fileLength - this.blockSize * this.maxIndex;
        }
        new BlockHandler(this, index * this.blockSize, size).upload().then(function (size) {
            _this.next(size);
        })["catch"](function (reason) {
            window.setTimeout(function () { return _this.handleItem(index); }, 1000);
        });
    };
    return JmsUploader;
}());
exports.JmsUploader = JmsUploader;
var BlockHandler = /** @class */ (function () {
    function BlockHandler(uploader, position, size) {
        var _this = this;
        this.position = 0;
        this.size = 0;
        this.upload = function () {
            return new Promise(function (resolve, reject) { return __awaiter(_this, void 0, void 0, function () {
                var binaryData, headers, p, ret, text;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            binaryData = this.uploader.file.slice(this.position, this.position + this.size);
                            headers = {
                                'Content-Type': 'application/json'
                            };
                            if (this.uploader.headers) {
                                for (p in this.uploader.headers) {
                                    headers[p] = this.uploader.headers[p];
                                }
                            }
                            headers["Jack-Upload-Length"] = [this.uploader.fileLength, this.position, this.size];
                            if (this.uploader.file.name) {
                                headers["Name"] = this.uploader.file.name;
                            }
                            else {
                                headers["Name"] = "none";
                            }
                            headers["Upload-Id"] = this.uploader.tranId;
                            return [4 /*yield*/, fetch("" + this.uploader.url, {
                                    method: 'POST',
                                    headers: headers,
                                    body: binaryData
                                })];
                        case 1:
                            ret = _a.sent();
                            return [4 /*yield*/, ret.text()];
                        case 2:
                            text = _a.sent();
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
                            return [2 /*return*/];
                    }
                });
            }); });
        };
        this.uploader = uploader;
        this.position = position;
        this.size = size;
    }
    return BlockHandler;
}());
