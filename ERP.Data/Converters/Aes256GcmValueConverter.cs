using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP.Data.Converters
{
    /// <summary>
    /// ValueConverter para cifrado AES-256-GCM de campos sensibles en base de datos.
    /// Cumple con Privacy by Design (RGPD Art.25) - cifrado en reposo.
    /// </summary>
    public class Aes256GcmValueConverter : ValueConverter<string, string>
    {
        private static readonly byte[] _key;
        private static readonly int _keySize = 32; // 256 bits
        private static readonly int _nonceSize = 12; // 96 bits para GCM
        private static readonly int _tagSize = 16; // 128 bits

        static Aes256GcmValueConverter()
        {
            var keyBase64 = Environment.GetEnvironmentVariable("DB_ENCRYPTION_KEY") 
                ?? throw new InvalidOperationException("DB_ENCRYPTION_KEY environment variable no configurada. Generar con: openssl rand -base64 32");
            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != _keySize)
                throw new InvalidOperationException($"DB_ENCRYPTION_KEY debe ser 32 bytes (256 bits) en Base64. Actual: {_key.Length} bytes");
        }

        public Aes256GcmValueConverter() : base(
            v => Encrypt(v),
            v => Decrypt(v))
        {
        }

        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = new AesGcm(_key);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length + 16]; // +16 para tag
            var tag = new byte[16];

            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Combinar: nonce (12) + ciphertext + tag (16)
            var result = new byte[12 + cipherBytes.Length + 16];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(cipherBytes, 0, result, 12, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, 12 + cipherBytes.Length, 16);

            return Convert.ToBase64String(result);
        }

        private static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var data = Convert.FromBase64String(cipherText);
                if (data.Length < 12 + 16) return cipherText; // Mínimo: nonce + tag

                var nonce = new byte[12];
                Buffer.BlockCopy(data, 0, nonce, 0, 12);
                var cipherBytes = new byte[data.Length - 12 - 16];
                Buffer.BlockCopy(data, 12, cipherBytes, 0, cipherBytes.Length);
                var tag = new byte[16];
                Buffer.BlockCopy(data, data.Length - 16, tag, 0, 16);

                using var aes = new AesGcm(_key);
                var plainBytes = new byte[cipherBytes.Length];
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Si falla el descifrado, devolver el valor original (compatibilidad hacia atrás)
                return cipherText;
            }
        }
    }
}