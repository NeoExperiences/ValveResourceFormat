using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeR32F : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = BitConverter.ToSingle(input[offset..(offset + sizeof(float))]);
                offset += sizeof(float);

                span[i] = new SKColorF(r, 0f, 0f);
            }
        }
        public void Decode(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = BitConverter.ToSingle(input.Slice(offset, sizeof(float)));
                offset += sizeof(float);

                span[i] = new SKColorF(r, 0, 0);
            }
        }
    }
}
