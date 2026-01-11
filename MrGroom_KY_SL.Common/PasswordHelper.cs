using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Common
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit
        private const int Iterations = 20000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return null;

            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[SaltSize];
                rng.GetBytes(salt);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    var key = pbkdf2.GetBytes(KeySize);
                    var hashBytes = new byte[SaltSize + KeySize];
                    Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
                    Buffer.BlockCopy(key, 0, hashBytes, SaltSize, KeySize);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash))
                return false;

            var hashBytes = Convert.FromBase64String(storedHash);
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);
            var storedKey = new byte[KeySize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedKey, 0, KeySize);

            using (var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, Iterations))
            {
                var enteredKey = pbkdf2.GetBytes(KeySize);
                return FixedTimeEquals(storedKey, enteredKey);
            }
        }

        // Replacement for CryptographicOperations.FixedTimeEquals (not available in .NET Framework)
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
    }
}
