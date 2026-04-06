using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MyCourierSA.Models;
using MyCourierSA.Constants;

namespace MyCourierSA.Services
{
    public class PricingService
    {
        private readonly ApplicationDbContext _db;

        public PricingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<decimal> CalculatePriceAsync(decimal weight, string type, string option)
        {
            // 1. Fetch live rates from DB with fallbacks
            var settings = await _db.SystemSettings
                .Where(s => s.Key == "BasePriceFee" || s.Key == "PerKgRateFee")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            decimal baseFee = ParseSetting(settings, "BasePriceFee", 40.00m);
            decimal perKg = ParseSetting(settings, "PerKgRateFee", 12.00m);

            // 2. Size Fee (Rewritten for C# 7.3 compatibility)
            decimal sizeFee = 0;
            switch (type)
            {
                case AppConstants.ParcelTypes.Small:
                    sizeFee = AppConstants.Pricing.SizeFeeSmall;
                    break;
                case AppConstants.ParcelTypes.Medium:
                    sizeFee = AppConstants.Pricing.SizeFeeMedium;
                    break;
                case AppConstants.ParcelTypes.Large:
                    sizeFee = AppConstants.Pricing.SizeFeeLarge;
                    break;
                default:
                    sizeFee = 0;
                    break;
            }

            // 3. Multiplier (Rewritten for C# 7.3 compatibility)
            decimal multiplier = AppConstants.DeliveryOptions.MultiplierStandard;
            switch (option)
            {
                case AppConstants.DeliveryOptions.Express:
                    multiplier = AppConstants.DeliveryOptions.MultiplierExpress;
                    break;
                case AppConstants.DeliveryOptions.Overnight:
                    multiplier = AppConstants.DeliveryOptions.MultiplierOvernight;
                    break;
                default:
                    multiplier = AppConstants.DeliveryOptions.MultiplierStandard;
                    break;
            }

            return (baseFee + (weight * perKg) + sizeFee) * multiplier;
        }

        private decimal ParseSetting(Dictionary<string, string> settings, string key, decimal defaultValue)
        {
            if (settings.TryGetValue(key, out string val))
            {
                // Clean the string (handle commas and dots)
                string cleanVal = val.Replace(",", ".");
                if (decimal.TryParse(cleanVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
            }
            return defaultValue;
        }
    }
}