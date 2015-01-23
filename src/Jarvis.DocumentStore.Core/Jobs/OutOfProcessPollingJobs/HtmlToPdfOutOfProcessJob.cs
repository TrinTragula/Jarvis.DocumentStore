﻿using System.Collections.Generic;
using System.IO;
using Castle.MicroKernel.Registration;
using CQRS.Kernel.ProjectionEngine.Client;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Commands;
using Jarvis.DocumentStore.Core.Jobs.PollingJobs;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using Jarvis.DocumentStore.Core.Processing.Conversions;
using Jarvis.DocumentStore.Core.Processing.Tools;
using Jarvis.DocumentStore.Core.Storage;
using Jarvis.DocumentStore.Core.Support;
using DocumentFormat = Jarvis.DocumentStore.Client.Model.DocumentFormat;
using DocumentFormats = Jarvis.DocumentStore.Core.Processing.DocumentFormats;

namespace Jarvis.DocumentStore.Core.Jobs.OutOfProcessPollingJobs
{
    public class HtmlToPdfOutOfProcessJob : AbstractOutOfProcessPollerFileJob
    {
        public HtmlToPdfOutOfProcessJob()
        {
            base.PipelineId = new PipelineId("htmlzip");
            base.QueueName = "htmlzip";
        }

        protected async override System.Threading.Tasks.Task<bool> OnPolling(PollerJobParameters parameters, string workingFolder)
        {
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.InputBlobId, workingFolder);

            var converter = new HtmlToPdfConverterFile(pathToFile, ConfigService)
            {
                Logger = Logger
            };

            var pdfConvertedFileName = converter.Run(parameters.TenantId, parameters.InputBlobId);
            await AddFormatToDocumentFromFile(
                parameters.TenantId,
                parameters.InputDocumentId, 
                new  DocumentFormat(DocumentFormats.Pdf), 
                pdfConvertedFileName, 
                new Dictionary<string, object>());
            return true;
        }
    }
}
