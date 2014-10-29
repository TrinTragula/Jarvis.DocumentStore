﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Engine;
using CQRS.Shared.ValueObjects;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document
{
    public class Document : AggregateRoot<DocumentState>
    {
        public Document(DocumentState initialState)
            : base(initialState)
        {
        }

        public Document()
        {
        }

        public void Create(DocumentId id, FileId fileId, DocumentHandle handle, FileNameWithExtension fileName, IDictionary<string, object> customData = null)
        {
            ThrowIfDeleted();

            if (HasBeenCreated)
                throw new DomainException((IIdentity)id, "Already created");

            RaiseEvent(new DocumentCreated(id, fileId, handle, fileName, customData));
        }

        public void AddFormat(DocumentFormat documentFormat, FileId fileId, PipelineId createdBy)
        {
            ThrowIfDeleted();
            if (InternalState.HasFormat(documentFormat))
            {
                RaiseEvent(new DocumentFormatHasBeenUpdated(documentFormat, fileId, createdBy));
            }
            else
            {
                RaiseEvent(new FormatAddedToDocument(documentFormat, fileId, createdBy));
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

        public void Delete(DocumentHandle handle)
        {
            if (!InternalState.IsValidHandle(handle))
                throw new DomainException(this.Id, string.Format("Document handle \"{0}\" is invalid",handle));

            RaiseEvent(new DocumentHandleDetached(handle));

            if (InternalState.Handles.Count == 0)
            {
                RaiseEvent(new DocumentDeleted(
                    InternalState.FileId,
                    InternalState.Formats.Select(x => x.Value).ToArray()
                ));
            }
        }

        public void Deduplicate(DocumentId documentId, DocumentHandle handle, FileNameWithExtension fileName)
        {
            ThrowIfDeleted();
            RaiseEvent(new DocumentHandleAttached(handle, fileName));
            RaiseEvent(new DocumentHasBeenDeduplicated(documentId,handle));
        }

        void ThrowIfDeleted()
        {
            if(InternalState.HasBeenDeleted)
                throw new DomainException(this.Id, "Document has been deleted");
        }
    }
}
