using System;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeRG1616 : ITextureDecoder, IHdrDecoder
    {
        public void DecodeHdr(SKBitmap imageInfo, Span<byte> input)
        {
            using var pixels = imageInfo.PeekPixels();
            var span = pixels.GetPixelSpan<SKColorF>();

            for (int i = 0, j = 0; j < span.Length; i += 4, j++)
            {
                var hr = BitConverter.ToUInt16(input.Slice(i, 2)) / 256f;
                var hg = BitConverter.ToUInt16(input.Slice(i + 2, 2)) / 256f;

                span[j] = new SKColorF(hr, hg, 0f);
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
                var b = BitConverter.ToUInt16(input.Slice(offset, sizeof(ushort)));
                offset += sizeof(ushort);

                span[i] = new SKColor((byte)(r / 256), (byte)(b / 256), 0, 255);
            }
        }
    }
}
