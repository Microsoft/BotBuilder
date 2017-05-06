using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// A message in a conversation
    /// </summary>
    public interface IMessageActivity : IActivity
    {
        /// <summary>
        /// The language code of the Text field
        /// </summary>
        /// <remarks>
        /// See https://msdn.microsoft.com/en-us/library/hh456380.aspx for a list of valid language codes
        /// </remarks>
        string Locale { get; set; }

        /// <summary>
        /// Content for the message
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Text to display if the channel cannot render cards
        /// </summary>
        string Summary { get; set; }

        /// <summary>
        /// Format of text fields [plain|markdown] Default:markdown
        /// </summary>
        string TextFormat { get; set; }

        /// <summary>
        /// Hint for how to deal with multiple attachments: [list|carousel] Default:list
        /// </summary>
        string AttachmentLayout { get; set; }

        /// <summary>
        /// Attachments
        /// </summary>
        IList<Attachment> Attachments { get; set; }

        /// <summary>
        /// SuggestedActions are used to express actions for interacting with a card like keyboards/quickReplies
        /// </summary>
        IList<CardAction> SuggestedActions { get; set; }

        /// <summary>
        /// Collection of Entity objects, each of which contains metadata about this activity. Each Entity object is typed.
        /// </summary>
        IList<Entity> Entities { get; set; }

        /// <summary>
        /// True if this activity has text, attachments, or channelData
        /// </summary>
        bool HasContent();

        /// <summary>
        /// Get channeldata as typed structure
        /// </summary>
        /// <typeparam name="TypeT">type to use</typeparam>
        /// <returns>typed object or default(TypeT)</returns>
        TypeT GetChannelData<TypeT>();

        /// <summary>
        /// Get mentions
        /// </summary>
        Mention[] GetMentions();
    }
}
