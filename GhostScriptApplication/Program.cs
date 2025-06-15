using Ghostscript.NET;
using Ghostscript.NET.Processor;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostScriptApplication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Document started.....");

            string fileList = null;
            fileList = "D:\\Data\\686274\\686274\\BGE130525049.pdf,D:\\Data\\686274\\686274\\ID\\KYC_BGE130525049.pdf";

            List<String> files = new List<string>();
            List<string> Newfile = new List<string>();
            files = fileList.Split(new char[] { ',' }).ToList();

            Newfile = ConvertImagesToPdfs(files, "D:\\Data", 500, 500);

            byte[] document = MergeFilesWithGhostscript(Newfile, 100, 100);
            File.WriteAllBytes("abc.pdf", document); //filename


            Console.WriteLine("Document completed.....");

            Console.ReadLine();

        }

        private static List<string> ConvertImagesToPdfs(List<string> filePaths, string tempFolder, float pageWidth, float pageHeight)
        {

            var pdfFiles = new List<string>();

            foreach (var path in filePaths)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();

                if (new[] { ".bmp", ".gif", ".jpeg", ".jpg", ".png" }.Contains(ext))
                {
                    var pdfPath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(path) + ".pdf");

                    using (var doc = new Document(new Rectangle(pageWidth, pageHeight)))
                    using (var fs = new FileStream(pdfPath, FileMode.Create, FileAccess.Write))
                    {
                        var writer = PdfWriter.GetInstance(doc, fs);
                        doc.Open();

                        var image = iTextSharp.text.Image.GetInstance(path);

                        // Scale image to fit page with same logic as your original code
                        float scaleX = pageWidth / image.Width;
                        float scaleY = pageHeight / image.Height;
                        float scale = Math.Min(scaleX, scaleY);
                        image.ScalePercent(scale * 100);

                        float posX = (pageWidth - image.ScaledWidth) / 2;
                        float posY = (pageHeight - image.ScaledHeight) / 2;
                        image.SetAbsolutePosition(posX, posY);

                        doc.Add(image);
                        doc.Close();
                    }

                    pdfFiles.Add(pdfPath);
                }
                else if (ext == ".pdf")
                {
                    pdfFiles.Add(path); // already PDF
                }
                else
                {
                    throw new NotSupportedException($"File format not supported: {path}");
                }
            }

            return pdfFiles;
        }

        // Calls Ghostscript to merge the given PDF files into one PDF, returns byte[] of merged PDF
        public static byte[] MergeFilesWithGhostscript(List<string> filePaths, float pageWidth, float pageHeight)
        {
            Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            var path = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
            string ghostscriptDllPath = path + @"\binary\gsdll32.dll";


            // Create temp directory to store image->pdf conversions
            string tempFolder = Path.Combine(Path.GetTempPath(), "GhostscriptMergeTemp");
            Directory.CreateDirectory(tempFolder);

            // Convert images to PDFs
            var pdfFiles = ConvertImagesToPdfs(filePaths, tempFolder, pageWidth, pageHeight);

            // Output merged PDF temporary file path
            string outputPdfPath = Path.Combine(tempFolder, "merged.pdf");
            

            List<string> gsArgs = new List<string>()
                {

                    "-dCompatibilityLevel=1.4",
                    "-dPDFSETTINGS=/prepress",
                    "-dEmbedAllFonts=true",
                    "-dSubsetFonts=true",
                    "-dDetectDuplicateImages=true",
                    "-dCompressFonts=true",
                    "-dAutoRotatePages=/None",
                    "-dColorImageDownsampleType=/Bicubic",
                    "-dColorImageResolution=300",
                    "-dGrayImageResolution=300",
                    "-dMonoImageResolution=300",
                    "-sProcessColorModel=DeviceRGB",
                    "-dPDFA=1",
                    "-dPDFACompatibilityPolicy=1",
                    "-sDEVICE=pdfwrite",
                    "-dDEVICEWIDTHPOINTS=500",  
                    "-dDEVICEHEIGHTPOINTS=500", 
                    "-dFIXEDMEDIA",
                    "-dPDFFitPage",
                    $"-sOutputFile={outputPdfPath}",
                    "-dBATCH",
                    "-dNOPAUSE"

                };


            foreach (var pdf in pdfFiles)
            {
                gsArgs.Add(pdf);
            }

            

            var ghostscriptVersion = new GhostscriptVersionInfo(ghostscriptDllPath);

            using (var processor = new GhostscriptProcessor(ghostscriptVersion, true))
            {
                processor.StartProcessing(gsArgs.ToArray(), null);
            }

            // Read merged PDF to memory
            byte[] mergedPdfBytes = File.ReadAllBytes(outputPdfPath);

            // Clean up temp files and folder
            foreach (var pdf in pdfFiles)
            {
                if (pdf.StartsWith(tempFolder))
                {
                    try { File.Delete(pdf); } catch { }
                }
            }
            try { File.Delete(outputPdfPath); } catch { }
            try { Directory.Delete(tempFolder); } catch { }

            return mergedPdfBytes;
        }

    }
}
