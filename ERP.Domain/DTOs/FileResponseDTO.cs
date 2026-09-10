namespace ERP.Domain.DTOs
{
    public class FileResponseDTO
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Cadena Base64
    }
}