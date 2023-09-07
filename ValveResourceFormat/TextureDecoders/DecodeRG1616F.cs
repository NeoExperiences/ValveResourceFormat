using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeRG1616F : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap imageInfo, Span<byte> input)
        {
            using var pixels = imageInfo.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            const int sizeOfHalf = 2;
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = (float)BitConverter.ToHalf(input.Slice(offset, sizeOfHalf));
                offset += sizeOfHalf;
                var g = (float)BitConverter.ToSingle(input.Slice(offset, sizeOfHalf));
                offset += sizeOfHalf;

                span[i] = new SKColorF(r, g, 0f);
            }
        }

        public void Decode(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColor>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = (float)BitConverter.ToHalf(input.Slice(offset, 2));
                offset += 2;
                var g = (float)BitConverter.ToHalf(input.Slice(offset, 2));
                offset += 2;

                span[i] = new SKColor((byte)(r * 255), (byte)(g * 255), 0, 255);
            }
        }
    }
}
