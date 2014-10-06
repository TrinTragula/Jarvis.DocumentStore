﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQRS.Kernel.Events;
using CQRS.Kernel.ProjectionEngine;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Document.Events;
using Jarvis.DocumentStore.Core.ReadModel;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class DocumentProjection : AbstractProjection,
        IEventHandler<DocumentCreated>,
        IEventHandler<FormatAddedToDocument>,
        IEventHandler<DocumentDeleted>,
        IEventHandler<DocumentHasBeenDeduplicated>
    {
        private readonly ICollectionWrapper<DocumentReadModel, DocumentId> _documents;

        public DocumentProjection(ICollectionWrapper<DocumentReadModel, DocumentId> documents)
        {
            _documents = documents;
            _documents.Attach(this,false);
        }

        public override void Drop()
        {
            _documents.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentCreated e)
        {
            _documents.Insert(e, new DocumentReadModel(
                (DocumentId)e.AggregateId,
                e.FileId,
                e.Alias,
                e.FileName
            ));
        }

        public void On(FormatAddedToDocument e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d => {
                d.AddFormat(e.DocumentFormat, e.FileId);
            });
        }

        public void On(DocumentDeleted e)
        {
            _documents.Delete(e, (DocumentId) e.AggregateId);
        }

        public void On(DocumentHasBeenDeduplicated e)
        {
            _documents.FindAndModify(e, (DocumentId)e.AggregateId, d =>
            {
                d.AddAlias(e.OtherFileAlias, e.OtherFileName);
            });
        }
    }
}
