﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.Events;
using Jarvis.Framework.Kernel.Events;
using Jarvis.Framework.Kernel.ProjectionEngine.RecycleBin;

namespace Jarvis.DocumentStore.Core.EventHandlers
{
    public class RecycleBinProjection : AbstractProjection
        ,IEventHandler<DocumentDescriptorDeleted>
    {
        private readonly IRecycleBin _recycleBin;

        public RecycleBinProjection(IRecycleBin recycleBin)
        {
            _recycleBin = recycleBin;
        }

        public override void Drop()
        {
            _recycleBin.Drop();
        }

        public override void SetUp()
        {
        }

        public void On(DocumentDescriptorDeleted e)
        {
            var files = e.BlobFormatsId.Concat(new []{ e.BlobId}).ToArray();
            _recycleBin.Delete(e.AggregateId, "Jarvis", e.CommitStamp, new { files });
        }
    }
}
