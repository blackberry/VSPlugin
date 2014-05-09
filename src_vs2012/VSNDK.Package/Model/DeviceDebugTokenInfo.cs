using System;
using System.Text;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Description of the debug-token stored on the device.
    /// </summary>
    internal sealed class DeviceDebugTokenInfo
    {
        private string _description;

        #region Properties

        public DeviceDebugTokenInfo(string author, DateTime expiryDate, bool isValid, bool isInstalled, string validationErrorMessage, uint validationErrorCode)
        {
            // error code must be provided with a suitable message:
            if (validationErrorCode > 0 && string.IsNullOrEmpty(validationErrorMessage))
                throw new ArgumentOutOfRangeException("validationErrorMessage", "Expected non empty message for non-zero error code");

            Author = author;
            ExpiryDate = expiryDate;
            IsValid = isValid;
            IsInstalled = isInstalled;
            ValidationErrorMessage = validationErrorMessage;
            ValidationErrorCode = validationErrorCode;
        }

        public string Author
        {
            get;
            private set;
        }

        public DateTime ExpiryDate
        {
            get;
            private set;
        }

        public bool IsValid
        {
            get;
            private set;
        }

        public bool IsInstalled
        {
            get;
            private set;
        }

        public string ValidationErrorMessage
        {
            get;
            private set;
        }

        public uint ValidationErrorCode
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            if (_description == null)
                _description = GetDescription();

            return _description;
        }

        private string GetDescription()
        {
            var result = new StringBuilder();

            if (ValidationErrorCode > 0)
            {
                result.Append(ValidationErrorMessage);
                result.Append(" (code: ").Append(ValidationErrorCode).Append(")");
            }
            else
            {
                result.Append(Author);
                result.Append(" (").Append(ExpiryDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")).Append(")");
            }

            return result.ToString();
        }
    }
}
