using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeRG3232F : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap imageInfo, Span<byte> input)
        {
            using var pixels = imageInfo.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = BitConverter.ToSingle(input.Slice(offset, sizeof(float)));
                offset += sizeof(float);
                var g = BitConverter.ToSingle(input.Slice(offset, sizeof(float)));
                offset += sizeof(float);

                span[i] = new SKColorF(r, g, 1.0f);
            }
        }

        public void Decode(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var span = pixels.GetPixelSpan<SKColor>();
            var offset = 0;

            for (var i = 0; i < span.Length; i++)
            {
                var r = BitConverter.ToSingle(input.Slice(offset, sizeof(float)));
                offset += sizeof(float);
                var g = BitConverter.ToSingle(input.Slice(offset, sizeof(float)));
                offset += sizeof(float);

                span[i] = new SKColor((byte)(r * 255), (byte)(g * 255), 0, 255);
            }
        }
    }
}
