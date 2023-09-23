/*
 * Exchange Web Services Managed API
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System.ComponentModel;

using JetBrains.Annotations;

namespace Microsoft.Exchange.WebServices.Data;

/// <content>
///     Contains nested type Recurrence.IntervalPattern.
/// </content>
public abstract partial class Recurrence
{
    /// <summary>
    ///     Represents a recurrence pattern where each occurrence happens at a specific interval after the previous one.
    /// </summary>
    [PublicAPI]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class IntervalPattern : Recurrence
    {
        private int _interval = 1;

        /// <summary>
        ///     Gets or sets the interval between occurrences.
        /// </summary>
        public int Interval
        {
            get => _interval;

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.IntervalMustBeGreaterOrEqualToOne);
                }

                SetFieldValue(ref _interval, value);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntervalPattern" /> class.
        /// </summary>
        internal IntervalPattern()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntervalPattern" /> class.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="interval">The interval.</param>
        internal IntervalPattern(DateTime startDate, int interval)
            : base(startDate)
        {
            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), Strings.IntervalMustBeGreaterOrEqualToOne);
            }

            Interval = interval;
        }

        /// <summary>
        ///     Write properties to XML.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal override void InternalWritePropertiesToXml(EwsServiceXmlWriter writer)
        {
            base.InternalWritePropertiesToXml(writer);

            writer.WriteElementValue(XmlNamespace.Types, XmlElementNames.Interval, Interval);
        }

        /// <summary>
        ///     Tries to read element from XML.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>True if appropriate element was read.</returns>
        internal override bool TryReadElementFromXml(EwsServiceXmlReader reader)
        {
            if (base.TryReadElementFromXml(reader))
            {
                return true;
            }

            switch (reader.LocalName)
            {
                case XmlElementNames.Interval:
                {
                    _interval = reader.ReadElementValue<int>();
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Checks if two recurrence objects are identical.
        /// </summary>
        /// <param name="otherRecurrence">The recurrence to compare this one to.</param>
        /// <returns>true if the two recurrences are identical, false otherwise.</returns>
        public override bool IsSame(Recurrence? otherRecurrence)
        {
            return base.IsSame(otherRecurrence) && _interval == ((IntervalPattern)otherRecurrence)._interval;
        }
    }
}
