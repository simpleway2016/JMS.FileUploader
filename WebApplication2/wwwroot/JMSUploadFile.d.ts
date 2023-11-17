export declare class JmsUploader {
    file: Blob;
    headers: any;
    jsonObject: any;
    url: string;
    tranId: string;
    allFiles: Blob[];
    totalFilesLength: number;
    onUploading: (percent: number) => void;
    private completed;
    private currentIndex;
    private fileItemIndex;
    private maxIndex;
    private blockSize;
    private canceled;
    completedSize: number;
    constructor(url: string, file: File | File[] | FileList, headers: any, jsonObject: any);
    private onCompleted;
    private next;
    private uploadResolve;
    private uploadReject;
    upload: () => Promise<any>;
    cancel: () => void;
    private handleItem;
}
