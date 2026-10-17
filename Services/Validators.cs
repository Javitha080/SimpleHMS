using System.Text.RegularExpressions;

namespace SimpleHMS.Services
{
    // Single source of truth for the validation rules the forms share,
    // plus KeyPress guards that keep number-only fields clean.
    public static class Validators
    {
        private static readonly Regex NameRegex = new(@"^[a-zA-Z\s\.]+$", RegexOptions.Compiled);
        private static readonly Regex SpecializationRegex = new(@"^[a-zA-Z\s\-]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"^[\d\s\-\+]{10,15}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // Names: letters/spaces only, min 3 chars; dots allowed (Dr. style strings).
        public static bool IsValidName(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3 && NameRegex.IsMatch(value);

        public static bool IsValidSpecialization(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3 && SpecializationRegex.IsMatch(value);

        public static bool IsValidPhone(string value) =>
            !string.IsNullOrWhiteSpace(value) && PhoneRegex.IsMatch(value);

        public static bool IsValidEmail(string value) =>
            !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value);

        // Email is optional on patients; when present it must be well-formed.
        public static bool IsOptionalEmailValid(string value) =>
            string.IsNullOrWhiteSpace(value) || IsValidEmail(value);

        // KeyPress guard: digits and edit keys only (age, quantity, ...).
        public static void AllowDigits_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        // KeyPress guard: digits plus the decimal separator both locales write
        // ('.' and ','), edit keys pass through (fee, unit price, discount, ...).
        public static void AllowDecimal_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                e.Handled = true;
        }
    }
}
