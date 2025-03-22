using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DameChanceSV2.Utilities
{
    public class PasswordHelper
    {
        // Genera un hash seguro para una contraseña.
        public static string HashPassword(string password)
        {
            // Generar una sal de 128 bits (16 bytes)
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Generar el hash utilizando PBKDF2 con 10,000 iteraciones y 32 bytes de salida.
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            // Combinar la sal y el hash en una cadena separada por ':'
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        // Verifica que la contraseña ingresada coincide con el hash almacenado.
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedPasswordHash = Convert.FromBase64String(parts[1]);

            byte[] enteredPasswordHash = KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            // Comparación en tiempo fijo para prevenir ataques de temporización.
            return CryptographicOperations.FixedTimeEquals(enteredPasswordHash, storedPasswordHash);
        }
    }
}
