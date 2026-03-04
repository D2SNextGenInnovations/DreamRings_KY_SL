using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class LicenseService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        private const string SECRET = "MY_SUPER_SECRET_123";

        public bool IsLicenseValid()
        {
            var license = _unitOfWork.Context.Licenses
                            .FirstOrDefault(l => l.IsActive);

            if (license == null)
                return false;

            return license.ExpiryDate >= DateTime.UtcNow;
        }

        public License GetActiveLicense()
        {
            return _unitOfWork.Context.Licenses
                .FirstOrDefault(l => l.IsActive);
        }

        public bool ActivateLicense(string productKey)
        {
            DateTime expiry;

            if (!ValidateProductKey(productKey, out expiry))
                return false;

            var existing = _unitOfWork.Context.Licenses
                            .FirstOrDefault(l => l.IsActive);

            if (existing != null)
            {
                existing.ProductKey = productKey;
                existing.ActivatedOn = DateTime.UtcNow;
                existing.ExpiryDate = expiry;
            }
            else
            {
                _unitOfWork.Context.Licenses.Add(new License
                {
                    ProductKey = productKey,
                    ActivatedOn = DateTime.UtcNow,
                    ExpiryDate = expiry,
                    IsActive = true
                });
            }

            _unitOfWork.Save();
            return true;
        }

        private bool ValidateProductKey(string productKey, out DateTime expiry)
        {
            expiry = DateTime.MinValue;

            try
            {
                var parts = productKey.Split('|');

                if (parts.Length != 3)
                    return false;

                var company = parts[0];
                var expiryStr = parts[1];
                var signature = parts[2];

                var data = company + "|" + expiryStr;

                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET)))
                {
                    var computedHash = Convert.ToBase64String(
                        hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));

                    if (computedHash != signature)
                        return false;
                }

                expiry = DateTime.Parse(expiryStr);

                if (expiry < DateTime.UtcNow)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
