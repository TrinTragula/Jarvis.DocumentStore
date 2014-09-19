﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.Storage;

namespace Jarvis.ImageService.Core.Services
{
    /// <summary>
    /// File related operations service
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Associate an image file to a file thumbnail
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="size">Image size</param>
        /// <param name="imageId">Id of the image file</param>
        void LinkImage(FileId fileId, string size, FileId imageId);

        /// <summary>
        /// Upload a file sent in an http request
        /// </summary>
        /// <param name="httpContent">request's content</param>
        /// <param name="fileId">Id of the new file</param>
        /// <returns>Error message or null</returns>
        Task<string> UploadFromHttpContent(HttpContent httpContent, FileId fileId);
        
        /// <summary>
        /// Get the file descriptor for the required thumbnail size
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="size">Thumnbail size</param>
        /// <returns>File description for the associated thumbnail</returns>
        IFileStoreHandle GetImageDescriptor(FileId fileId, string size);
        

        /// <summary>
        /// Get the file info
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <returns>the <see cref="T:Jarvis.ImageService.Core.Model.FileInfo"/> </returns>
        FileInfo GetById(FileId fileId);
    }
}
