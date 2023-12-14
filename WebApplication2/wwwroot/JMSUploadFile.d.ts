export declare class JmsUploader {
    file: Blob;
    headers: any;
    jsonObject: any;
    url: string;
    tranId: string;
    uploadFilter: string;
    private allFiles;
    private totalFilesLength;
    onUploading: (percent: number, uploadedSize: number, totalSize: number) => void;
    private completed;
    private currentIndex;
    private fileItemIndex;
    private maxIndex;
    private blockSize;
    private canceled;
    completedSize: number;
    constructor(url: string, file: File | File[] | FileList, headers: any, jsonObject: any);
    /**
     * 设置每小分片的大小，默认100K
     * @param size
     */
    setPartSize(size: number): void;
    setUploadFilter(filter: string): void;
    private onCompleted;
    private next;
    private uploadResolve;
    private uploadReject;
    upload: () => Promise<any>;
    cancel: () => void;
    private handleItem;
}
