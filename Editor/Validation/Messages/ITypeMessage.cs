using System;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// An interface that represents a message to be displayed in the Inspector view.
    /// </summary>
    interface ITypeMessage
    {
        /// <summary>
        /// The message text.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// The message type.
        /// </summary>
        MessageType MessageType { get; }

        /// <summary>
        /// The message's link, if it has one.
        /// </summary>
        LinkData Link { get; }

        struct LinkData
        {
            internal string LinkUrl;
            internal string LinkTitle;

            internal LinkData(string linkTitle, string linkUrl)
            {
                LinkTitle = linkTitle;
                LinkUrl = linkUrl;
            }
        }
    }
}
