namespace BillingBackend.Validation
{
    /// <summary>
    /// Centralized Indian + general input validation patterns.
    /// Use with [RegularExpression(ValidationPatterns.Gstin, ...)] etc.
    /// Frontend projects mirror these same patterns (see validation.ts / validators.dart).
    /// </summary>
    public static class ValidationPatterns
    {
        /// <summary>Indian 10-digit mobile starting 6-9.</summary>
        public const string PhoneIn = @"^[6-9]\d{9}$";

        /// <summary>Generic international phone (E.164-ish, 8-15 digits).</summary>
        public const string PhoneIntl = @"^\+?[1-9]\d{7,14}$";

        /// <summary>Indian GSTIN: 2 digits + 5 letters + 4 digits + 1 letter + 1 alphanum + Z + 1 alphanum.</summary>
        public const string Gstin = @"^\d{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

        /// <summary>Indian PAN: 5 letters + 4 digits + 1 letter.</summary>
        public const string Pan = @"^[A-Z]{5}[0-9]{4}[A-Z]$";

        /// <summary>Indian 6-digit PIN code (not starting with 0).</summary>
        public const string PincodeIn = @"^[1-9][0-9]{5}$";

        /// <summary>Indian IFSC: 4 letters + 0 + 6 alphanumerics.</summary>
        public const string Ifsc = @"^[A-Z]{4}0[A-Z0-9]{6}$";

        /// <summary>UPI VPA: name@bank.</summary>
        public const string UpiVpa = @"^[\w.\-]{2,256}@[a-zA-Z]{2,64}$";

        /// <summary>Indian vehicle number: MH12AB1234.</summary>
        public const string VehicleNo = @"^[A-Z]{2}[0-9]{2}[A-Z]{1,3}[0-9]{4}$";

        /// <summary>Username: letters, digits, underscore.</summary>
        public const string Username = @"^[a-zA-Z0-9_]+$";

        /// <summary>Hex color: #RRGGBB.</summary>
        public const string HexColor = @"^#([A-Fa-f0-9]{6})$";

        /// <summary>2-digit GST state code: 01-37.</summary>
        public const string GstStateCode = @"^([0-2][0-9]|3[0-7])$";
    }
}
