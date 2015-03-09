﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Shared.Model;
using Jarvis.DocumentStore.Shared.Serialization;
using Newtonsoft.Json;
using Jarvis.DocumentStore.Shared;

namespace Jarvis.DocumentStore.Client
{
    /// <summary>
    /// DocumentStore client
    /// </summary>
    public class DocumentStoreServiceClient
    {
        public string Tenant { get; private set; }
        readonly Uri _documentStoreUri;
        public string TempFolder { get; set; }

        /// <summary>
        /// Create a new DocumentStore Client
        /// </summary>
        /// <param name="documentStoreUri">base uri</param>
        /// <param name="tenant">tenantId</param>
        public DocumentStoreServiceClient(Uri documentStoreUri, string tenant)
        {
            Tenant = tenant;
            _documentStoreUri = documentStoreUri;
            TempFolder = Path.Combine(Path.GetTempPath(), "jarvis.client");
        }

        /// <summary>
        /// Zip an html page with images / scripts subfolder
        /// </summary>
        /// <param name="pathToFile">path to html file</param>
        /// <returns>path to zipped file</returns>
        public string ZipHtmlPage(string pathToFile)
        {
            if (!Directory.Exists(TempFolder))
                Directory.CreateDirectory(TempFolder);

            string pathToZip = Path.ChangeExtension(Path.Combine(
                TempFolder,
                Path.GetFileName(pathToFile)
            ), ".htmlzip");

            File.Delete(pathToZip);

            using (ZipArchive zip = ZipFile.Open(pathToZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(pathToFile, Path.GetFileName(pathToFile));
                var attachmentFolder = FindAttachmentFolder(pathToFile);
                if (attachmentFolder != null)
                {
                    var subfolderName = Path.GetFileName(attachmentFolder);
                    foreach (var fname in Directory.GetFiles(attachmentFolder))
                    {
                        // filter some extensions?
                        zip.CreateEntryFromFile(
                            fname,
                            subfolderName + "/" + Path.GetFileName(fname)
                        );
                    }
                }
            }

            return pathToZip;
        }

        /// <summary>
        /// Stategy for attachment folder identification
        /// </summary>
        /// <param name="pathToFile">path to html file</param>
        /// <returns>path to html attachments</returns>
        static string FindAttachmentFolder(string pathToFile)
        {
            var attachmentFolder = Path.Combine(
                Path.GetDirectoryName(pathToFile),
                Path.GetFileNameWithoutExtension(pathToFile) + "_files"
                );

            if (Directory.Exists(attachmentFolder))
            {
                return attachmentFolder;
            }

            return null;
        }

        /// <summary>
        /// upload a document
        /// </summary>
        /// <param name="fileNameWithExtension">File name with extension</param>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="inputStream">Input stream</param>
        /// <param name="customData">Custom Data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAsync(
            string fileNameWithExtension,
            DocumentHandle documentHandle,
            Stream inputStream,
            IDictionary<string, object> customData = null)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle);
            return await DoUpload(endPoint, fileNameWithExtension, inputStream, customData);
        }

        /// <summary>
        /// Create a new document as an attach of existing document
        /// </summary>
        /// <param name="fileNameWithExtension">File name with extension</param>
        /// <param name="fatherDocumentHandle">Document handle</param>
        /// <param name="inputStream">Input stream</param>
        /// <param name="attachSource"></param>
        /// <param name="customData">Custom Data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
            string fileNameWithExtension,
            DocumentHandle fatherDocumentHandle,
            Stream inputStream,
            String attachSource,
            IDictionary<string, object> customData = null)
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData["source"] = attachSource;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + fatherDocumentHandle + "/attach/" + attachSource);
            return await DoUpload(endPoint, fileNameWithExtension, inputStream, customData);
        }

        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
           string pathToFile,
           DocumentHandle fatherDocumentHandle,
           String attachSource,
           IDictionary<string, object> customData = null) 
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData["source"] = attachSource;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + fatherDocumentHandle + "/attach/" + attachSource);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        public async Task<UploadedDocumentResponse> UploadAttachmentAsync(
           string pathToFile,
           String queueName,
           String jobId,
           String attachSource,
           IDictionary<string, object> customData = null)
        {
            if (customData == null) customData = new Dictionary<String, Object>();
            customData["source"] = attachSource;
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/jobs/attach/" + queueName + "/" + jobId + "/" + attachSource);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        /// <summary>
        /// Upload a document
        /// </summary>
        /// <param name="pathToFile">Path to local document</param>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="customData">Custom data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        public async Task<UploadedDocumentResponse> UploadAsync(
            string pathToFile,
            DocumentHandle documentHandle, 
            IDictionary<string, object> customData = null)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle);
            return await UploadFromFile(endPoint, pathToFile, customData);
        }

        private async Task<UploadedDocumentResponse> UploadFromFile(Uri endPoint, string pathToFile, IDictionary<string, object> customData)
        {

            var fileExt = Path.GetExtension(pathToFile).ToLowerInvariant();
            if (fileExt == ".html" || fileExt == ".htm")
            {
                var zippedFile = ZipHtmlPage(pathToFile);
                try
                {
                    return await InnerUploadAsync(endPoint, zippedFile, customData);

                }
                finally
                {
                    File.Delete(zippedFile);
                }
            }

            return await InnerUploadAsync(endPoint, pathToFile, customData);
        }

        /// <summary>
        /// Utility method for uploads
        /// </summary>
        /// <param name="endPoint">The endopoint used to upload the file</param>
        /// <param name="pathToFile">Path to local document</param>
        /// <param name="customData">Custom data</param>
        /// <returns>MD5 of the uploaded file. MD5 is calculated by the DocumentStore</returns>
        private async Task<UploadedDocumentResponse> InnerUploadAsync(
            Uri endPoint,
            string pathToFile,
            IDictionary<string, object> customData
        )
        {
            string fileNameWithExtension = Path.GetFileName(pathToFile);
            using (var sourceStream = File.OpenRead(pathToFile))
            {
                return await DoUpload(endPoint, fileNameWithExtension, sourceStream, customData);
            }
        }

        async Task<UploadedDocumentResponse> DoUpload(
            Uri endPoint, 
            string fileNameWithExtension, 
            Stream inputStream, 
            IDictionary<string, object> customData)
        {
            string fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            using (var client = new HttpClient())
            {
                using (
                    var content =
                        new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    content.Add(
                        new StreamContent(inputStream),
                        fileName, fileNameWithExtension
                    );

                    if (customData != null)
                    {
                        var stringContent = new StringContent(await ToJsonAsync(customData));
                        content.Add(stringContent, "custom-data");
                    }

                    using (var message = await client.PostAsync(endPoint, content))
                    {
                        var json = await message.Content.ReadAsStringAsync();
                        message.EnsureSuccessStatusCode();
                        return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                    }
                }
            }
        }

        public async Task<UploadedDocumentResponse> AddFormatToDocument(
            AddFormatFromFileToDocumentModel model, 
            IDictionary<string, object> customData = null)
        {
            using (var sourceStream = File.OpenRead(model.PathToFile))
            {
                using (var client = new HttpClient())
                {
                    using (
                        var content =
                            new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    {

                        var fileInfo = new FileInfo(model.PathToFile);
                        content.Add(
                            new StreamContent(sourceStream),
                            "stream",
                            fileInfo.Name
                        );

                        customData = customData ?? new Dictionary<String, Object>();
                        customData.Add(AddFormatToDocumentParameters.CreatedBy, model.CreatedById);
                        customData.Add(AddFormatToDocumentParameters.DocumentHandle, model.DocumentHandle);
                        customData.Add(AddFormatToDocumentParameters.JobId, model.JobId);
                        customData.Add(AddFormatToDocumentParameters.QueueName, model.QueueName); 
                        customData.Add(AddFormatToDocumentParameters.Format, model.Format);

                        var stringContent = new StringContent(JsonConvert.SerializeObject(customData));
                        content.Add(stringContent, "custom-data");

                        var modelFormat = model.Format == null ? "null" : model.Format.ToString();
                        var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/addformat/" + modelFormat);

                        using (var message = await client.PostAsync(endPoint, content))
                        {
                            var json = await message.Content.ReadAsStringAsync();
                            message.EnsureSuccessStatusCode();
                            return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                        }
                    }
                }
            }
        }

        public async Task<UploadedDocumentResponse> AddFormatToDocument(
            AddFormatFromObjectToDocumentModel model, 
            IDictionary<string, object> customData = null)
        {
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes((model.StringContent))))
            {

                using (var client = new HttpClient())
                {
                    using (
                        var content =
                            new MultipartFormDataContent("Upload----" +
                                                         DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                    {
                        content.Add(
                            new StreamContent(sourceStream),
                            "stream",
                            model.FileName
                        );

                        customData = customData ?? new Dictionary<String, Object>();
                        customData.Add(AddFormatToDocumentParameters.CreatedBy, model.CreatedById);
                        customData.Add(AddFormatToDocumentParameters.DocumentHandle, model.DocumentHandle);
                        customData.Add(AddFormatToDocumentParameters.JobId, model.JobId);
                        customData.Add(AddFormatToDocumentParameters.QueueName, model.QueueName); 
                        customData.Add(AddFormatToDocumentParameters.Format, model.Format);

                        var stringContent = new StringContent(JsonConvert.SerializeObject(customData));
                        content.Add(stringContent, "custom-data");

                        var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/addformat/" + model.Format);

                        using (var message = await client.PostAsync(endPoint, content))
                        {
                            var json = await message.Content.ReadAsStringAsync();
                            message.EnsureSuccessStatusCode();
                            return JsonConvert.DeserializeObject<UploadedDocumentResponse>(json);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Serialize custom data to json string
        /// </summary>
        /// <param name="data">custom data</param>
        /// <returns>json representation of custom data</returns>
        private Task<string> ToJsonAsync(IDictionary<string, object> data)
        {
            return Task.Factory.StartNew(() => JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// Deserialize custom data from json string
        /// </summary>
        /// <param name="data">json representation of custom data</param>
        /// <param name="settings">serializer settings</param>
        /// <returns>custom data</returns>
        private Task<T> FromJsonAsync<T>(string data, JsonSerializerSettings settings = null)
        {
            return Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(data, settings));
        }

        /// <summary>
        /// Retrieve custom data from DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <returns>Custom data</returns>
        public async Task<IDictionary<string, object>> GetCustomDataAsync(DocumentHandle documentHandle)
        {
            using (var client = new HttpClient())
            {
                var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle + "/@customdata");

                var json = await client.GetStringAsync(endPoint);
                return await FromJsonAsync<IDictionary<string, object>>(json);
            }
        }

        /// <summary>
        /// Open a file on DocumentStore
        /// </summary>
        /// <param name="documentHandle">Document handle</param>
        /// <param name="format">Document format</param>
        /// <returns>A document format reader</returns>
        public DocumentFormatReader OpenRead(DocumentHandle documentHandle, DocumentFormat format = null)
        {
            format = format ?? new DocumentFormat("original");
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + documentHandle + "/" + format);
            return new DocumentFormatReader(endPoint);
        }

        /// <summary>
        /// Open a binary content, it is necessary for workers out of process that does not care
        /// about <see cref="DocumentHandle" /> but have a reference to a job id related to a blob
        /// </summary>
        /// <param name="queueName">The name of the queue that is executing the job</param>
        /// <param name="jobId">The id of the job.</param>
        /// <returns></returns>
        public DocumentFormatReader OpenBlobIdForRead(String queueName, String jobId)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/jobs/blob/" + queueName + "/" + jobId);
            return new DocumentFormatReader(endPoint);
        }

        /// <summary>
        /// Delete a document from Document Store
        /// </summary>
        /// <param name="handle">Document handle</param>
        /// <returns>Task</returns>
        public async Task DeleteAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + handle);
            using (var client = new HttpClient())
            {
                await client.DeleteAsync(resourceUri);
            }
        }

        /// <summary>
        /// Delete all attachment of a given handle, specifying the source.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task DeleteAttachmentsAsync(DocumentHandle handle, String source)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments/" + handle + "/" + source);
            using (var client = new HttpClient())
            {
                await client.DeleteAsync(resourceUri);
            }
        }

        /// <summary>
        /// Get the formats available for a document handle
        /// </summary>
        /// <param name="handle">document handles</param>
        /// <returns>Document formats</returns>
        public async Task<DocumentFormats> GetFormatsAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<IDictionary<DocumentFormat, Uri>>(json);
                return new DocumentFormats(d);
            }
        }

        public async Task<DocumentAttachments> GetAttachmentsAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<IDictionary<DocumentHandle, Uri>>(json);
                return new DocumentAttachments(d);
            }
        }

        public async Task<DocumentAttachmentsFat> GetAttachmentsFatAsync(DocumentHandle handle)
        {
            var resourceUri = new Uri(_documentStoreUri, Tenant + "/documents/attachments_fat/" + handle);
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(resourceUri);
                var d = await FromJsonAsync<List<DocumentAttachmentsFat.AttachmentInfo>>(json);
                return new DocumentAttachmentsFat(d);
            }
        }

        /// <summary>
        /// Get document content (typed)
        /// </summary>
        /// <param name="handle">document handle</param>
        /// <returns><see cref="DocumentFormat"/>document content</returns>
        public async Task<DocumentContent> GetContentAsync(DocumentHandle handle)
        {
            var endPoint = new Uri(_documentStoreUri, Tenant + "/documents/" + handle + "/content");
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(endPoint);
                return await FromJsonAsync<DocumentContent>(json, PocoSerializationSettings.Default);
            }
        }


    }
}