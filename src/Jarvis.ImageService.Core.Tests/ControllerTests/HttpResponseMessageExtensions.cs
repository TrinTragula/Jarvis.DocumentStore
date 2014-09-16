﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jarvis.ImageService.Core.Tests.ControllerTests
{
    public static class HttpResponseMessageExtensions
    {
        public static T UnWrap<T>(this HttpResponseMessage message)
        {
            var content = (ObjectContent<T>)message.EnsureSuccessStatusCode().Content;
            return (T)content.Value;
        }

        public static HttpError GetError(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
                throw new Exception(string.Format("Expected failure status code, found: {0}", (int)message.StatusCode));
            var content = (HttpError)((ObjectContent<HttpError>)message.Content).Value;
            return content;
        }
    }
}
