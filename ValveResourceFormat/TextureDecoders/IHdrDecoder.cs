using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal interface IHdrDecoder
    {
        public abstract void DecodeHdr(SKBitmap bitmap, Span<byte> input);
    }
}
