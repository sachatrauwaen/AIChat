using System;

namespace Satrabel.AIChat.History
{
    /// <summary>
    /// Configuration for how much chat history should be included
    /// when building the model context.
    /// </summary>
    public class ChatHistoryPolicy
    {
        /// <summary>
        /// Approximate maximum number of tokens to keep in the
        /// replayed context. Uses the same rough heuristic as
        /// <see cref="ChatConversation.GetTokenCount"/> (4 chars ~= 1 token).
        /// Set to 0 or negative to disable this limit.
        /// </summary>
        public int MaxContextTokens { get; set; }

        /// <summary>
        /// Maximum number of user messages (turns) to keep in the
        /// replayed context. Set to 0 or negative to disable this limit.
        /// </summary>
        public int MaxUserTurns { get; set; }

        /// <summary>
        /// Minimum number of most recent messages that should always
        /// be kept, even if token/turn limits would otherwise trim more.
        /// </summary>
        public int MinMessagesToKeep { get; set; }

        /// <summary>
        /// Weight multiplier applied to tool messages when estimating
        /// their token cost. Values greater than 1 will cause
        /// conversations with many tool calls to keep less distant
        /// history for the same budget.
        /// </summary>
        public double ToolTokenWeight { get; set; }

        /// <summary>
        /// Creates a policy with conservative, reusable defaults.
        /// Callers are expected to override values as needed.
        /// </summary>
        public static ChatHistoryPolicy CreateDefault()
        {
            return new ChatHistoryPolicy
            {
                MaxContextTokens = 4096,
                MaxUserTurns = 20,
                MinMessagesToKeep = 4,
                ToolTokenWeight = 4.0
            };
        }

        internal int GetEffectiveMaxContextTokens()
        {
            return MaxContextTokens > 0 ? MaxContextTokens : int.MaxValue;
        }

        internal int GetEffectiveMaxUserTurns()
        {
            return MaxUserTurns > 0 ? MaxUserTurns : int.MaxValue;
        }

        internal int GetEffectiveMinMessagesToKeep()
        {
            return MinMessagesToKeep > 0 ? MinMessagesToKeep : 0;
        }
    }
}

