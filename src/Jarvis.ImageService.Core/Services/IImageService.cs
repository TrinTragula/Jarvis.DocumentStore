﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jarvis.ImageService.Core.Http;
using Jarvis.ImageService.Core.Model;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.Storage;
using MongoDB.Driver;

namespace Jarvis.ImageService.Core.Services
{
    public interface IImageService
    {
        ImageInfo GetById(string id);
        void Save(ImageInfo imageInfo);
        void Create(string id, string filename, ImageSizeInfo[] getDefaultImageSizes);
        void LinkImage(string id, string size, string imageId);
        Task<string> ReadFromHttp(HttpContent httpContent, string fileId);
    }

    public class MongoDbImageService : IImageService
    {
        readonly IPipelineScheduler _scheduler;
        readonly MongoCollection<ImageInfo> _collection;
        readonly IFileStore _fileStore;
        readonly ConfigService _config;

        public MongoDbImageService(
            MongoDatabase db, 
            IPipelineScheduler scheduler, 
            IFileStore fileStore, 
            ConfigService config
        )
        {
            _scheduler = scheduler;
            _fileStore = fileStore;
            _config = config;
            _collection = db.GetCollection<ImageInfo>("fileinfo");
        }

        public ImageInfo GetById(string id)
        {
            return _collection.FindOneById(id.ToLowerInvariant());
        }

        public void Save(ImageInfo imageInfo)
        {
            _collection.Save(imageInfo);
        }

        public void Create(string id, string filename, ImageSizeInfo[] imageSizes)
        {
            var fi = new ImageInfo(id, filename);
            foreach (var sizeInfo in imageSizes)
            {
                fi.LinkSize(sizeInfo.Name, null);
            }
            Save(fi);
            _scheduler.QueueThumbnail(id, imageSizes);
        }

        public void LinkImage(string id, string size, string imageId)
        {
            var fi = GetById(id);
            fi.LinkSize(size, imageId);
            Save(fi);
        }

        public async Task<string> ReadFromHttp(HttpContent httpContent, string fileId)
        {
            if (httpContent == null || !httpContent.IsMimeMultipartContent())
            {
                return "Attachment not found!";
            }

            var provider = await httpContent.ReadAsMultipartAsync(
                new FileStoreMultipartStreamProvider(_fileStore, fileId)
            );

            if (provider.Filename == null)
            {
                return "Attachment not found!";
            }

            if (provider.UnsupportedExtension != null)
            {
                return string.Format("Unsupported file {0}", provider.UnsupportedExtension);
            }

            Create(
                fileId,
                provider.Filename,
                _config.GetDefaultSizes()
            );

            return null;
        }
    }
}
