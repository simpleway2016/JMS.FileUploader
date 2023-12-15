using System;
using System.Collections.Generic;
using System.Text;

namespace JMS.FileUploader.AspNetCore
{
    internal class UploadFilterException : Exception
    {
        public UploadFilterException(Exception ex):base(null , ex) { }
    }
}
