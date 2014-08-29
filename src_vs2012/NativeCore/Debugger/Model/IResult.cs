using System.Collections.Generic;

namespace BlackBerry.NativeCore.Debugger.Model
{
    /// <summary>
    /// Interface describing attached result data returned with any GDB response.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the string value of the field.
        /// </summary>
        string StringValue
        {
            get;
        }

        /// <summary>
        /// Gets the numerical value of the field.
        /// </summary>
        uint UInt32Value
        {
            get;
        }

        /// <summary>
        /// Gets an indication, if the field is enumerable with values (array or object).
        /// </summary>
        bool IsEnumerable
        {
            get;
        }

        /// <summary>
        /// Gets the number of values stored.
        /// </summary>
        int Length
        {
            get;
        }

        /// <summary>
        /// Gets the enumerator of stored values.
        /// </summary>
        IEnumerable<IResult> ArrayItems
        {
            get;
        }

        /// <summary>
        /// Gets the value at given index.
        /// </summary>
        IResult this[int index]
        {
            get;
        }

        int Count
        {
            get;
        }

        /// <summary>
        /// Gets the enumerator of stored values.
        /// </summary>
        IEnumerable<KeyValuePair<string, IResult>> ObjectItems
        {
            get;
        }

        /// <summary>
        /// Gets the value with given name.
        /// </summary>
        IResult this[string name]
        {
            get;
        }
    }
}
