"use strict";
exports.__esModule = true;
exports.uuid = void 0;
var uuid = (function () {
    function uuid() {
        this.v4 = function () {
            return new Date().getTime().toString();
        };
    }
    return uuid;
}());
exports.uuid = uuid;
