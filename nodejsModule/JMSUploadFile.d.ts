export declare class JmsUploader {
    file: Blob;
    fileLength: number;
    headers: any;
    jsonObject: any;
    url: string;
    tranId: string;
    fileName: string;
    onUploading: (percent: number) => void;
    private completed;
    private currentIndex;
    private maxIndex;
    private blockSize;
    private canceled;
    completedSize: number;
    constructor(url: string, file: File, headers: any, jsonObject: any);
    private onCompleted;
    private next;
    private uploadResolve;
    private uploadReject;
    upload: () => Promise<any>;
    cancel: () => void;
    private handleItem;
}
