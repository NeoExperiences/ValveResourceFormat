using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeR16F : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = (float)BitConverter.ToHalf(input[offset..(offset + 2)]);
                offset += 2;

                span[i] = new SKColorF(r, 0f, 0f);
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

                span[i] = new SKColor((byte)(r * 255), 0, 0, 255);
            }
        }
    }
}
