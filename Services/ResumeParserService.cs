using UglyToad.PdfPig;

namespace SmartResumeAnalyzer.Services
{
    public class ResumeParserService
    {
        // Extract text from uploaded PDF
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            // Temporary file path
            var tempFile = Path.GetTempFileName();

            // Save uploaded file
            using (var stream = new FileStream(tempFile, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string extractedText = "";

            // Read PDF
            using (PdfDocument document = PdfDocument.Open(tempFile))
            {
                foreach (var page in document.GetPages())
                {
                    extractedText += page.Text + " ";
                }
            }

            // Delete temp file
            File.Delete(tempFile);

            return extractedText;
        }
    }
}
