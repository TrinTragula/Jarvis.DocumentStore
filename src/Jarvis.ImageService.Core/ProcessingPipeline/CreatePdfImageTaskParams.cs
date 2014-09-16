﻿using System;
using System.IO;
using GraphicsMagick;

namespace Jarvis.ImageService.Core.ProcessinPipeline
{
    public class CreatePdfImageTaskParams
    {
        public enum ImageFormat
        {
            Png
        }

        public CreatePdfImageTaskParams()
        {
            FromPage = 1;
            Pages = 1;
            Dpi = 72;
            Format = ImageFormat.Png;
        }

        public int FromPage { get; set; }
        public int Pages { get; set; }
        public int Dpi { get; set; }
        public ImageFormat Format { get; set; }
    }
}