using System.Windows.Forms;
using SimpleHMS.Services;
using Xunit;

namespace SimpleHMS.Tests.Unit
{
    public class ValidatorsTests
    {
        [Theory]
        [InlineData("Ann Mary", true)]
        [InlineData("Dr. Smith", true)]        // dots are allowed
        [InlineData("A", false)]               // too short
        [InlineData("Bob1", false)]            // digits rejected
        [InlineData("", false)]
        [InlineData("   ", false)]             // blanks collapse to nothing
        [InlineData("Anna-Marie", false)]      // hyphens are not in the name rules
        public void IsValidName_FollowsNameRules(string value, bool expected)
        {
            Assert.Equal(expected, Validators.IsValidName(value));
        }

        [Theory]
        [InlineData("Cardiology", true)]
        [InlineData("Eye-Nose", true)]         // hyphen allowed here
        [InlineData("Eye2", false)]
        [InlineData("AB", false)]              // min 3 chars
        [InlineData("", false)]
        public void IsValidSpecialization_FollowsSpecializationRules(string value, bool expected)
        {
            Assert.Equal(expected, Validators.IsValidSpecialization(value));
        }

        [Theory]
        [InlineData("0771234567", true)]
        [InlineData("+94771234567", true)]     // plus allowed
        [InlineData("077 123 456 7", true)]    // spaces allowed
        [InlineData("077-123-4567", true)]     // dashes allowed
        [InlineData("12345", false)]           // too short
        [InlineData("phone123", false)]        // letters rejected
        [InlineData("", false)]
        public void IsValidPhone_FollowsPhoneRules(string value, bool expected)
        {
            Assert.Equal(expected, Validators.IsValidPhone(value));
        }

        [Theory]
        [InlineData("a@b.com", true)]
        [InlineData("p.j@sub.medic.co", true)]
        [InlineData("a@b", false)]             // no dot after the @
        [InlineData("a b@c.com", false)]       // spaces rejected
        [InlineData("@b.com", false)]
        [InlineData("", false)]
        public void IsValidEmail_FollowsEmailRules(string value, bool expected)
        {
            Assert.Equal(expected, Validators.IsValidEmail(value));
        }

        [Theory]
        [InlineData("", true)]                 // email is optional for patients
        [InlineData("   ", true)]
        [InlineData("a@b.com", true)]
        [InlineData("bad", false)]
        public void IsOptionalEmailValid_TreatsEmailAsOptional(string value, bool expected)
        {
            Assert.Equal(expected, Validators.IsOptionalEmailValid(value));
        }

        [Fact]
        public void AllowDigits_KeyPress_BlocksNonDigitsButKeepsEditKeys()
        {
            var letter = new KeyPressEventArgs('a');
            Validators.AllowDigits_KeyPress(null, letter);
            Assert.True(letter.Handled);

            var digit = new KeyPressEventArgs('7');
            Validators.AllowDigits_KeyPress(null, digit);
            Assert.False(digit.Handled);

            var backspace = new KeyPressEventArgs('\b');
            Validators.AllowDigits_KeyPress(null, backspace);
            Assert.False(backspace.Handled);
        }

        [Fact]
        public void AllowDecimal_KeyPress_BlocksLettersButKeepsSeparatorsAndEditKeys()
        {
            foreach (char key in new[] { 'a', 'e', '-', '%' })
            {
                var pressed = new KeyPressEventArgs(key);
                Validators.AllowDecimal_KeyPress(null, pressed);
                Assert.True(pressed.Handled, $"'{key}' should be blocked");
            }

            foreach (char key in new[] { '9', '.', ',', '\b', (char)24 })  // digits, separators, edit keys
            {
                var pressed = new KeyPressEventArgs(key);
                Validators.AllowDecimal_KeyPress(null, pressed);
                Assert.False(pressed.Handled, $"'{key}' should pass");
            }
        }
    }
}
