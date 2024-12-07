using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main()
    {
        try
        {
            string inputPath = "valeo.jpg";
            string outputPath = @"Sources\NoSleep\valeo.ico";

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("Error: Input file {0} not found!", inputPath);
                return;
            }

            using (var original = Image.FromFile(inputPath))
            {
                // 创建不同尺寸的图标
                var sizes = new[] { 16, 32, 48, 256 };
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        // 写入ICO头部
                        writer.Write((short)0);      // 保留字段
                        writer.Write((short)1);      // 图标类型
                        writer.Write((short)sizes.Length);  // 图像数量

                        var dataOffset = 6 + sizes.Length * 16;  // 数据偏移量

                        // 写入图标目录
                        for (int i = 0; i < sizes.Length; i++)
                        {
                            using (var resized = new Bitmap(sizes[i], sizes[i]))
                            {
                                using (var g = Graphics.FromImage(resized))
                                {
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.DrawImage(original, 0, 0, sizes[i], sizes[i]);
                                }

                                using (var ms = new MemoryStream())
                                {
                                    resized.Save(ms, ImageFormat.Png);
                                    var imageData = ms.ToArray();

                                    // 写入图标目录项
                                    writer.Write((byte)sizes[i]);  // 宽度
                                    writer.Write((byte)sizes[i]);  // 高度
                                    writer.Write((byte)0);         // 调色板大小
                                    writer.Write((byte)0);         // 保留字段
                                    writer.Write((short)1);        // 颜色平面数
                                    writer.Write((short)32);       // 每像素位数
                                    writer.Write((int)imageData.Length); // 图像大小
                                    writer.Write(dataOffset);      // 图像数据偏移量

                                    dataOffset += imageData.Length;
                                }
                            }
                        }

                        // 写入图像数据
                        for (int i = 0; i < sizes.Length; i++)
                        {
                            using (var resized = new Bitmap(sizes[i], sizes[i]))
                            {
                                using (var g = Graphics.FromImage(resized))
                                {
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.DrawImage(original, 0, 0, sizes[i], sizes[i]);
                                }

                                using (var ms = new MemoryStream())
                                {
                                    resized.Save(ms, ImageFormat.Png);
                                    var imageData = ms.ToArray();
                                    writer.Write(imageData);
                                }
                            }
                        }
                    }

                    // 保存ICO文件
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, stream.ToArray());
                    Console.WriteLine("Icon created successfully: {0}", outputPath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error creating icon: {0}", ex.Message);
        }
    }
} 