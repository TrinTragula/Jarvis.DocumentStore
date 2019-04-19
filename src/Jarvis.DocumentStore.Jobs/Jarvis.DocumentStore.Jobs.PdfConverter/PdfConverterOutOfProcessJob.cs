﻿using Jarvis.DocumentStore.Jobs.PdfConverter.Converters;
using Jarvis.DocumentStore.JobsHost.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Jarvis.DocumentStore.Client.Model;

namespace Jarvis.DocumentStore.Jobs.PdfConverter
{
    public class PdfConverterOutOfProcessJob : AbstractOutOfProcessPollerJob
    {
        public IPdfConverter[] Converters { get; set; }

        public PdfConverterOutOfProcessJob()
        {
            base.PipelineId = "pdfConverter";
            base.QueueName = "pdfConverter";
        }

        protected async override Task<ProcessResult> OnPolling(
            Shared.Jobs.PollerJobParameters parameters,
            string workingFolder)
        {
            Logger.DebugFormat("Downloaded file {0} to be converted to pdf", parameters.FileName);
            var converter = Converters.FirstOrDefault(c => c.CanConvert(parameters.FileName));
            if (converter == null)
            {
                Logger.InfoFormat("No converter for extension {0}", Path.GetExtension(parameters.FileName));
                return ProcessResult.Ok;
            }

            //Download file only if we have one converter that can generate pdf.
            string pathToFile = await DownloadBlob(parameters.TenantId, parameters.JobId, parameters.FileName, workingFolder).ConfigureAwait(false);
            string outFile = Path.Combine(workingFolder, Guid.NewGuid() + ".pdf");
            if (!converter.Convert(pathToFile, outFile))
            {
                Logger.ErrorFormat("Error converting file {0} to pdf", pathToFile);
                return ProcessResult.Fail($"Error converting file {pathToFile} to pdf");
            }

            await AddFormatToDocumentFromFile(
               parameters.TenantId,
               parameters.JobId,
               new DocumentFormat(DocumentFormats.Pdf),
               outFile,
               new Dictionary<string, object>()).ConfigureAwait(false);

            return ProcessResult.Ok;
        }
    }
}
