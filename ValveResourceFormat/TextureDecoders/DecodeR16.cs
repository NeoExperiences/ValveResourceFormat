using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeR16 : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var hr = BitConverter.ToUInt16(input[offset..(offset + 2)]) / 256f;
                offset += 2;

                span[i] = new SKColorF(hr, 0f, 0f);
            }
        }

        public void Decode(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColor>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = BitConverter.ToUInt16(input.Slice(offset, sizeof(ushort)));
                offset += sizeof(ushort);

                span[i] = new SKColor((byte)(r / 256), 0, 0, 255);
            }
        }
    }
}
