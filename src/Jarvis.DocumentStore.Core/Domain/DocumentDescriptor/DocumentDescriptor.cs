﻿using System;
using System.Linq;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Kernel.Engine;
using Jarvis.NEventStoreEx.CommonDomainEx;
using Jarvis.NEventStoreEx.CommonDomainEx.Core;
using Jarvis.DocumentStore.Core.Support;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{
    public class DocumentDescriptor : AggregateRoot<DocumentDescriptorState>
    {
        public DocumentDescriptor()
        {
        }

        public IDocumentFormatTranslator DocumentFormatTranslator { get; set; }

        public void Initialize(BlobId blobId, DocumentHandleInfo handleInfo, FileHash hash, String fileName)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException(Id, "Already initialized");

            RaiseEvent(new DocumentDescriptorInitialized(blobId, handleInfo, hash));

            var knownFormat = DocumentFormatTranslator.GetFormatFromFileName(fileName);
            if (knownFormat != null)
                RaiseEvent(new FormatAddedToDocumentDescriptor(knownFormat, blobId, null));
        }

        public void AddFormat(DocumentFormat documentFormat, BlobId blobId, PipelineId createdBy)
        {
            ThrowIfDeleted();
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(documentFormat, blobId, createdBy));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocumentDescriptor(documentFormat, blobId, createdBy));
            }
        }

        public void DeleteFormat(DocumentFormat documentFormat)
        {
            ThrowIfDeleted();
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenDeleted(documentFormat));
            }
        }

        void Attach(DocumentHandle handle)
        {
            if (!InternalState.IsValidHandle(handle))
                RaiseEvent(new DocumentHandleAttached(handle));
        }

        public void Delete(DocumentHandle handle)
        {
            if (handle != DocumentHandle.Empty)
            {
                if (!InternalState.IsValidHandle(handle))
                {
                    throw new DomainException(this.Id, string.Format("Document handle \"{0}\" is invalid", handle));
                }

                RaiseEvent(new DocumentHandleDetached(handle));
            }

            if (!InternalState.HasActiveHandles())
            {
                RaiseEvent(new DocumentDescriptorDeleted(
                    InternalState.BlobId,
                    InternalState.Formats.Select(x => x.Value).ToArray()
                ));
            }
        }

        /// <summary>
        /// This DocumentDescriptor has the same content of another <see cref="DocumentDescriptor"/>
        /// this operation mark the current document as owner of the handle of duplicated document
        /// descriptor
        /// </summary>
        /// <param name="otherDocumentDescriptorId"></param>
        /// <param name="handle"></param>
        /// <param name="fileName"></param>
        public void Deduplicate(DocumentDescriptorId otherDocumentDescriptorId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentDescriptorHasBeenDeduplicated(otherDocumentDescriptorId, handle, fileName));
            Attach(handle);
        }

        void ThrowIfDeleted()
        {
            if (InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }

        public void Create(DocumentHandle handle)
        {
            if (InternalState.Created)
                throw new DomainException(this.Id, "Already created");
            RaiseEvent(new DocumentDescriptorCreated(InternalState.BlobId, handle));
            Attach(handle);
        }


    }
}
