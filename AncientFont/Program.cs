using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AncientFont
{
    public class Program
    {
        public static void Main(string[] args)
        {
            foreach (string inputFile in args)
            {
                try
                {
                    FonToPng(inputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error on '" + Path.GetFileName(inputFile) + "': " + ex + "\r\n");
                    Console.WriteLine("Press any key to continue.");
                    Console.ReadKey();
                }
            }
        }

        public static void FonToPng(string inputFile)
        {
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) + ".png");

            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int version = br.ReadInt16();
                    if (version != 1)
                        throw new NotSupportedException("Version not supported.");

                    int unknown1 = br.ReadInt32(); // always 0xE0 / 224, symbol count?
                    int height = br.ReadByte();
                    byte[] unknown2 = br.ReadBytes(7); // always 0

                    IList<int> widths = new List<int>();
                    for (int i = 0; i < 256; i++)
                        widths.Add(br.ReadByte());

                    int columnCount = 28;
                    int rowCount = 8;
                    int tileWidth = widths.Max(x => x);
                    int tileHeight = height;
                    int gridWidth = (tileWidth * columnCount) + columnCount;
                    int gridHeight = (tileHeight * rowCount) + rowCount;
                    int bmpWidth = (int)Math.Pow(2, Math.Ceiling(Math.Log(gridWidth, 2)));
                    int bmpHeight = (int)Math.Pow(2, Math.Ceiling(Math.Log(gridHeight, 2)));

                    Bitmap bmp = new Bitmap(bmpWidth, bmpHeight, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        Pen redPen = new Pen(Color.Red);

                        for (int h = 0; h <= rowCount; h++)
                        {
                            g.DrawLine(redPen, 0, h * tileHeight + h, gridWidth, h * tileHeight + h);

                            for (int w = 0; w <= columnCount; w++)
                            {
                                g.DrawLine(redPen, w * tileWidth + w, 0, w * tileWidth + w, gridHeight);
                            }
                        }
                    }

                    IList<Color> palette = new List<Color>();
                    for (int i = 0; i < 512; i++)
                    {
                        // TODO: Is this really a palette?
                        // 1st byte is somewhat random, 2nd byte increments (but not strictly), 3rd and 4th are nearly always 0
                        // could be read as an incrementing number
                        Color color = ReadRgb565(br, Color.Black);
                        palette.Add(color);
                    }

                    for (int i = 0; i < 256; i++)
                    {
                        int width = widths[i];
                        int column = i % columnCount;
                        int row = i / columnCount;
                        int offsetX = (column * tileWidth) + column + (Math.Abs(tileWidth - width) / 2) + 1;
                        int offsetY = (row * tileHeight) + row + (Math.Abs(tileHeight - height) / 2) - tileHeight;

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (br.BaseStream.Position == br.BaseStream.Length)
                                {
                                    throw new Exception("Oops, end of file.");
                                }

                                Color color = ReadRgb565(br, Color.Transparent);

                                // do not render non-printable characters   
                                if (i >= 28)
                                    bmp.SetPixel(offsetX + x, offsetY + y, color);
                            }
                        }
                    }

                    bmp.Save(outputFile, ImageFormat.Png);
                }
            }
        }

        public static Color ReadRgb565(BinaryReader br, Color defaultValue)
        {
            short val = br.ReadInt16();
            int red = (val & 0xF800) >> 11;
            int green = (val & 0x7E0) >> 5;
            int blue = val & 0x1F;
            Color color = (val == 0) ? defaultValue : Color.FromArgb(255, red << 3, green << 2, blue << 3);
            return color;
        }
    }
}
