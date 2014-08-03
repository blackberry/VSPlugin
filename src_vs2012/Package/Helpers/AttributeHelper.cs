using System;

namespace BlackBerry.Package.Helpers
{
    /// <summary>
    /// Helper class for custom attributes.
    /// </summary>
    internal static class AttributeHelper
    {
        /// <summary>
        /// Returns the GUID from an object.
        /// </summary>
        public static Guid GetGuidFrom(object guidObject)
        {
            // figure out what type of object they passed in and get the GUID from it
            var strObject = guidObject as string;
            if (strObject != null)
                return new Guid(strObject);

            var typeObject = guidObject as Type;
            if (typeObject != null)
                return typeObject.GUID;

            var byteObject = guidObject as byte[];
            if (byteObject != null && byteObject.Length == 16)
                return new Guid(byteObject);

            if (guidObject is Guid)
                return (Guid)guidObject;

            throw new ArgumentException("Could not determine Guid from supplied object.", "guidObject");
        }

        /// <summary>
        /// Gets the string representation of the GUID valid for registry settings.
        /// </summary>
        public static string Format(Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }

        /// <summary>
        /// Gets the string representation of the GUID valid for registry settings.
        /// GUID is passed anyhow (string, type or GUID itself).
        /// </summary>
        public static string Format(object guidObject)
        {
            return Format(GetGuidFrom(guidObject));
        }

        /// <summary>
        /// Gets the array of GUIDs from specified argument.
        /// Argument can be any type (string, type or GUID itself).
        /// </summary>
        public static Guid[] GetArrayFrom(object guidArray)
        {
            var strObject = guidArray as string;
            if (strObject != null)
            {
                return Convert(strObject.Split(',', ';', '|'));
            }

            var strArrayObject = guidArray as string[];
            if (strArrayObject != null)
            {
                return Convert(strArrayObject);
            }

            if (guidArray is Guid)
                return new[] { (Guid) guidArray };

            var guidArrayObject = guidArray as Guid[];
            if (guidArrayObject != null)
                return guidArrayObject;

            var typeObject = guidArray as Type;
            if (typeObject != null)
                return new[] { typeObject.GUID };

            var typeArrayObject = guidArray as Type[];
            if (typeArrayObject != null)
            {
                return Convert(typeArrayObject);
            }

            var byteObject = guidArray as byte[];
            if (byteObject != null && byteObject.Length == 16)
                return new[] { new Guid(byteObject) };

            throw new ArgumentException("Invalid GUID array", "guidArray");
        }

        private static Guid[] Convert(string[] stringArray)
        {
            if (stringArray == null || stringArray.Length == 0)
                return null;

            var result = new Guid[stringArray.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Guid(stringArray[i]);
            }

            return result;
        }

        private static Guid[] Convert(Type[] typeArray)
        {
            if (typeArray == null || typeArray.Length == 0)
                return null;

            var result = new Guid[typeArray.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = typeArray[i].GUID;
            }

            return result;
        }
    }
}
