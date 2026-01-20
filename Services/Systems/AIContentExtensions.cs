using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// AI Content Extensions for handling AI content and chat message conversions
    /// </summary>
    public static class AIContentExtensions
    {
        /// <summary>
        /// Creates a sampling handler that satisfies sampling requests using the specified <see cref="IChatClient"/>.
        /// </summary>
        /// <param name="chatClient">The <see cref="IChatClient"/> with which to satisfy sampling requests.</param>
        /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use for serializing user-provided objects. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>The created handler delegate that can be assigned to <see cref="McpClientHandlers.SamplingHandler"/>.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a handler delegate that processes sampling requests by converting them to chat client arguments,
        /// sending them to the specified chat client, and converting the responses back to the expected format.
        /// </para>
        /// <para>
        /// The handler supports streaming responses and will report progress through the provided <see cref="IProgress{T}"/>
        /// instance as updates become available from the chat client.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> is <see langword="null"/>.</exception>
        public static Func<CreateMessageRequestParams?, IProgress<ProgressNotificationValue>, CancellationToken, ValueTask<CreateMessageResult>> CreateSamplingHandler(
            this IChatClient chatClient,
            JsonSerializerOptions? serializerOptions = null)
        {
            Throw.IfNull(chatClient);

            serializerOptions ??= McpJsonUtilities.DefaultOptions;

            return async (requestParams, progress, cancellationToken) =>
            {
                Throw.IfNull(requestParams);

                var (messages, options) = ToChatClientArguments(requestParams, serializerOptions);
                var progressToken = requestParams.ProgressToken;

                List<ChatResponseUpdate> updates = new List<ChatResponseUpdate>();

                // Create the chat completion request
                var chatRequest = new ChatCompletionRequest
                {
                    Messages = messages,
                    Options = options,
                    ProgressToken = progressToken
                };

                // Send the request to the chat client
                var chatResponse = await chatClient.CompleteChatAsync(chatRequest, progress, cancellationToken);

                // Process the response
                var lastMessage = chatResponse.Messages.LastOrDefault();
                var contents = lastMessage?.Contents ?? new List<AIContent>();

                // Convert contents to the expected format
                var resultContents = contents.Select(c => c.ToContentBlock(serializerOptions)).ToList();

                // Create the result
                var result = new CreateMessageResult
                {
                    StopReason = chatResponse.FinishReason == ChatFinishReason.Stop ? CreateMessageResult.StopReasonStop :
                                chatResponse.FinishReason == ChatFinishReason.Length ? CreateMessageResult.StopReasonMaxTokens :
                                chatResponse.FinishReason == ChatFinishReason.ToolCalls ? CreateMessageResult.StopReasonToolUse :
                                chatResponse.FinishReason.ToString(),
                    Meta = chatResponse.AdditionalProperties?.ToJsonObject(serializerOptions),
                    Role = lastMessage?.Role == ChatRole.User ? Role.User : Role.Assistant,
                    Content = resultContents,
                };

                return result;
            };

            static (IList<ChatMessage> Messages, ChatOptions? Options) ToChatClientArguments(CreateMessageRequestParams requestParams, JsonSerializerOptions serializerOptions)
            {
                ChatOptions? options = null;

                if (requestParams.Options != null)
                {
                    options = new ChatOptions
                    {
                        Temperature = requestParams.Options.Temperature,
                        TopP = requestParams.Options.TopP,
                        MaxTokens = requestParams.Options.MaxTokens,
                        PresencePenalty = requestParams.Options.PresencePenalty,
                        FrequencyPenalty = requestParams.Options.FrequencyPenalty,
                        StopSequences = requestParams.Options.StopSequences?.ToList(),
                        Seed = requestParams.Options.Seed,
                        User = requestParams.Options.User,
                        Tools = requestParams.Options.Tools?.Select(t => t.ToChatTool()).ToList(),
                        ToolChoice = requestParams.Options.ToolChoice?.ToChatToolChoice(),
                        ResponseFormat = requestParams.Options.ResponseFormat?.ToChatResponseFormat(),
                        AdditionalProperties = requestParams.Options.AdditionalProperties?.ToAdditionalProperties()
                    };
                }

                List<ChatMessage> messages = new List<ChatMessage>();
                foreach (var sm in requestParams.Messages)
                {
                    if (sm.Content?.Select(b => b.ToAIContent(serializerOptions)).OfType<AIContent>().ToList() is { Count: > 0 } aiContents)
                    {
                        ChatRole role =
                            aiContents.All(static c => c is FunctionResultContent) ? ChatRole.Tool :
                            aiContents.All(static c => c is FunctionCallContent) ? ChatRole.Assistant :
                            ChatRole.User;

                        messages.Add(new ChatMessage(role, aiContents));
                    }
                }

                return (messages, options);
            }
        }

        /// <summary>Converts the specified dictionary to a <see cref="JsonObject"/>.</summary>
        internal static JsonObject? ToJsonObject(this IReadOnlyDictionary<string, object?> properties, JsonSerializerOptions options)
        {
            return JsonSerializer.SerializeToNode(properties, options.GetTypeInfo(typeof(IReadOnlyDictionary<string, object?>))) as JsonObject;
        }

        internal static AdditionalPropertiesDictionary ToAdditionalProperties(this JsonObject obj)
        {
            if (obj == null)
            {
                return new AdditionalPropertiesDictionary();
            }

            var result = new AdditionalPropertiesDictionary();
            foreach (var property in obj)
            {
                result[property.Key] = property.Value?.ToString() ?? string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Converts a <see cref="PromptMessage"/> to a <see cref="ChatMessage"/> object.
        /// </summary>
        /// <param name="promptMessage">The prompt message to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>A <see cref="ChatMessage"/> object created from the prompt message.</returns>
        /// <remarks>
        /// This method transforms a protocol-specific <see cref="PromptMessage"/> from the Model Context Protocol
        /// into a standard <see cref="ChatMessage"/> object that can be used with AI client libraries.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="promptMessage"/> is <see langword="null"/>.</exception>
        public static ChatMessage ToChatMessage(this PromptMessage promptMessage, JsonSerializerOptions? options = null)
        {
            Throw.IfNull(promptMessage);

            AIContent? content = promptMessage.Content.ToAIContent(options);

            return new ChatMessage()
            {
                Role = promptMessage.Role == Role.User ? ChatRole.User :
                      promptMessage.Role == Role.Assistant ? ChatRole.Assistant :
                      ChatRole.System,
                Contents = content != null ? new List<AIContent> { content } : new List<AIContent>()
            };
        }

        /// <summary>
        /// Converts a <see cref="CallToolResult"/> to a <see cref="ChatMessage"/> object.
        /// </summary>
        /// <param name="result">The tool result to convert.</param>
        /// <param name="callId">The identifier for the function call request that triggered the tool invocation.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>A <see cref="ChatMessage"/> object created from the tool result.</returns>
        /// <remarks>
        /// This method transforms a protocol-specific <see cref="CallToolResult"/> from the Model Context Protocol
        /// into a standard <see cref="ChatMessage"/> object that can be used with AI client libraries.
        /// The result is serialized as a <see cref="JsonElement"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="callId"/> is <see langword="null"/>.</exception>
        public static ChatMessage ToChatMessage(this CallToolResult result, string callId, JsonSerializerOptions? options = null)
        {
            Throw.IfNull(result);
            Throw.IfNull(callId);

            options ??= McpJsonUtilities.DefaultOptions;

            return new ChatMessage(ChatRole.Tool, new List<AIContent> { new FunctionResultContent(callId, JsonSerializer.SerializeToElement(result, options.GetTypeInfo<CallToolResult>()))
            {
                RawRepresentation = result,
            }});
        }

        /// <summary>
        /// Converts a <see cref="ChatMessage"/> to a list of <see cref="PromptMessage"/> objects.
        /// </summary>
        /// <param name="chatMessage">The chat message to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>A list of <see cref="PromptMessage"/> objects created from the chat message.</returns>
        public static IList<PromptMessage> ToPromptMessages(this ChatMessage chatMessage, JsonSerializerOptions? options = null)
        {
            options ??= McpJsonUtilities.DefaultOptions;

            var role = chatMessage.Role switch
            {
                ChatRole.User => Role.User,
                ChatRole.Assistant => Role.Assistant,
                ChatRole.System => Role.System,
                ChatRole.Tool => Role.Tool,
                _ => Role.User
            };

            var messages = new List<PromptMessage>();

            foreach (var content in chatMessage.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = Role.Assistant,
                        Content = new ContentBlock[]
                        {
                            new ToolUseContentBlock
                            {
                                Id = functionCall.CallId,
                                Name = functionCall.FunctionName,
                                Input = JsonSerializer.SerializeToElement(functionCall.Arguments, options.GetTypeInfo<IDictionary<string, object?>>())
                            }
                        }
                    });
                }
                else if (content is FunctionResultContent functionResult)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = Role.Tool,
                        Content = new ContentBlock[]
                        {
                            new ToolResultContentBlock
                            {
                                ToolUseId = functionResult.CallId,
                                Content = new List<ContentBlock> { new TextContentBlock { Text = functionResult.Result.ToString() } },
                                IsError = functionResult.Exception != null
                            }
                        }
                    });
                }
                else if (content is TextContent textContent)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = role,
                        Content = new List<ContentBlock> { new TextContentBlock { Text = textContent.Text } }
                    });
                }
                else if (content is ImageContent imageContent)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = role,
                        Content = new List<ContentBlock> { new ImageContentBlock { ImageUrl = imageContent.ImageUrl } }
                    });
                }
                else if (content is AudioContent audioContent)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = role,
                        Content = new List<ContentBlock> { new AudioContentBlock { AudioUrl = audioContent.AudioUrl } }
                    });
                }
                else if (content is ResourceContent resourceContent)
                {
                    messages.Add(new PromptMessage
                    {
                        Role = role,
                        Content = new List<ContentBlock> { new EmbeddedResourceBlock { Resource = resourceContent.Resource.ToResourceContents() } }
                    });
                }
            }

            return messages;
        }

        /// <summary>Creates a new <see cref="AIContent"/> from the content of a <see cref="ContentBlock"/>.</summary>
        /// <param name="content">The <see cref="ContentBlock"/> to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>
        /// The created <see cref="AIContent"/>. If the content can't be converted (such as when it's a resource link), <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method converts protocol-specific <see cref="ContentBlock"/> objects from the Model Context Protocol
        /// into standard <see cref="AIContent"/> objects that can be used with AI client libraries.
        /// </para>
        /// <para>
        /// The conversion preserves the semantic meaning of the content while adapting it to the target library's
        /// representation of different content types, enabling seamless integration between the protocol and AI client libraries.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
        public static AIContent? ToAIContent(this ContentBlock content, JsonSerializerOptions? options = null)
        {
            Throw.IfNull(content);

            options ??= McpJsonUtilities.DefaultOptions;

            AIContent? ac = content switch
            {
                TextContentBlock textContent => new TextContent(textContent.Text),
                ImageContentBlock imageContent => new ImageContent(imageContent.ImageUrl),
                AudioContentBlock audioContent => new AudioContent(audioContent.AudioUrl),
                EmbeddedResourceBlock resourceContent => resourceContent.Resource.ToAIContent(),

                ToolUseContentBlock toolUse => FunctionCallContent.CreateFromParsedArguments(toolUse.Input, toolUse.Id, toolUse.Name,
                    json => JsonSerializer.Deserialize(json, options.GetTypeInfo<IDictionary<string, object?>>())),

                ToolResultContentBlock toolResult => new FunctionResultContent(
                    toolResult.ToolUseId,
                    toolResult.Content.Count == 1 ? toolResult.Content[0].ToAIContent(options) : toolResult.Content.Select(c => c.ToAIContent(options)).OfType<AIContent>().ToList())
                {
                    Exception = toolResult.IsError is true ? new() : null,
                },

                _ => null
            };

            return ac;
        }

        /// <summary>Creates a new <see cref="AIContent"/> from the content of a <see cref="ResourceContents"/>.</summary>
        /// <param name="content">The <see cref="ResourceContents"/> to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>
        /// The created <see cref="AIContent"/>. If the content can't be converted (such as when it's a resource link), <see langword="null"/> is returned.
        /// </returns>
        public static AIContent ToAIContent(this ResourceContents content, JsonSerializerOptions? options = null)
        {
            options ??= McpJsonUtilities.DefaultOptions;

            return content switch
            {
                TextResourceContents textResource => new TextContent(textResource.Text),
                ImageResourceContents imageResource => new ImageContent(imageResource.ImageUrl),
                AudioResourceContents audioResource => new AudioContent(audioResource.AudioUrl),
                _ => new TextContent($"Resource: {content.GetType().Name}")
            };
        }

        /// <summary>Creates a list of <see cref="AIContent"/> from a sequence of <see cref="ContentBlock"/>.</summary>
        /// <param name="contents">The <see cref="ContentBlock"/> instances to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>The created <see cref="AIContent"/> instances.</returns>
        /// <remarks>
        /// <para>
        /// This method converts a sequence of protocol-specific <see cref="ContentBlock"/> objects from the Model Context Protocol
        /// into standard <see cref="AIContent"/> objects that can be used with AI client libraries.
        /// </para>
        /// <para>
        /// Each <see cref="ContentBlock"/> object is converted using <see cref="ToAIContent(ContentBlock, JsonSerializerOptions?)"/>,
        /// preserving the type-specific conversion logic for text, images, audio, and resources.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="contents"/> is <see langword="null"/>.</exception>
        public static IList<AIContent> ToAIContents(this IEnumerable<ContentBlock> contents, JsonSerializerOptions? options = null)
        {
            Throw.IfNull(contents);

                return contents.Select(c => c.ToAIContent(options)).OfType<AIContent>().ToList();
        }

        /// <summary>Creates a list of <see cref="AIContent"/> from a sequence of <see cref="ResourceContents"/>.</summary>
        /// <param name="contents">The <see cref="ResourceContents"/> instances to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>The created <see cref="AIContent"/> instances.</returns>
        public static IList<AIContent> ToAIContents(this IEnumerable<ResourceContents> contents, JsonSerializerOptions? options = null)
        {
            Throw.IfNull(contents);

                return contents.Select(c => c.ToAIContent(options)).OfType<AIContent>().ToList();
        }

        /// <summary>Creates a new <see cref="ContentBlock"/> from the content of an <see cref="AIContent"/>.</summary>
        /// <param name="content">The <see cref="AIContent"/> to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>The created <see cref="ContentBlock"/>.</returns>
        public static ContentBlock ToContentBlock(this AIContent content, JsonSerializerOptions? options = null)
        {
            options ??= McpJsonUtilities.DefaultOptions;

            return content switch
            {
                TextContent textContent => new TextContentBlock { Text = textContent.Text },
                ImageContent imageContent => new ImageContentBlock { ImageUrl = imageContent.ImageUrl },
                AudioContent audioContent => new AudioContentBlock { AudioUrl = audioContent.AudioUrl },
                ResourceContent resourceContent => new EmbeddedResourceBlock { Resource = resourceContent.Resource.ToResourceContents() },

                FunctionCallContent functionCall => new ToolUseContentBlock
                {
                    Id = functionCall.CallId,
                    Name = functionCall.FunctionName,
                    Input = JsonSerializer.SerializeToElement(functionCall.Arguments, options.GetTypeInfo<IDictionary<string, object?>>())
                },

                FunctionResultContent functionResult => new ToolResultContentBlock
                {
                    ToolUseId = functionResult.CallId,
                    Content = functionResult.Result is IList<AIContent> list
                        ? list.Select(c => c.ToContentBlock(options)).ToList()
                        : new List<ContentBlock> { functionResult.Result.ToContentBlock(options) },
                    IsError = functionResult.Exception != null
                },

                _ => new TextContentBlock { Text = content.ToString() }
            };
        }

        /// <summary>Creates a list of <see cref="ContentBlock"/> from a sequence of <see cref="AIContent"/>.</summary>
        /// <param name="contents">The <see cref="AIContent"/> instances to convert.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization. If <see langword="null"/>, <see cref="McpJsonUtilities.DefaultOptions"/> is used.</param>
        /// <returns>The created <see cref="ContentBlock"/> instances.</returns>
        public static IList<ContentBlock> ToContentBlocks(this IEnumerable<AIContent> contents, JsonSerializerOptions? options = null)
        {
            options ??= McpJsonUtilities.DefaultOptions;

                return contents.Select(c => c.ToContentBlock(options)).ToList();
        }
    }

    // Supporting classes and interfaces
    public interface IChatClient
    {
        Task<ChatResponse> CompleteChatAsync(ChatCompletionRequest request, IProgress<ProgressNotificationValue> progress, CancellationToken cancellationToken);
    }

    public class ChatCompletionRequest
    {
        public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ChatOptions? Options { get; set; }
        public string? ProgressToken { get; set; }
    }

    public class ChatResponse
    {
        public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ChatFinishReason FinishReason { get; set; }
        public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; set; }
    }

    public class ChatMessage
    {
        public ChatMessage(ChatRole role, IList<AIContent> contents)
        {
            Role = role;
            Contents = contents;
        }

        public ChatRole Role { get; set; }
        public IList<AIContent> Contents { get; set; } = new List<AIContent>();
    }

    public class ChatOptions
    {
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public float? PresencePenalty { get; set; }
        public float? FrequencyPenalty { get; set; }
        public IList<string>? StopSequences { get; set; }
        public int? Seed { get; set; }
        public string? User { get; set; }
        public IList<ChatTool>? Tools { get; set; }
        public ChatToolChoice? ToolChoice { get; set; }
        public ChatResponseFormat? ResponseFormat { get; set; }
        public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    }

    public enum ChatRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    public enum ChatFinishReason
    {
        Stop,
        Length,
        ToolCalls,
        ContentFilter,
        FunctionCall
    }

    public class ChatTool
    {
        public string Type { get; set; } = "function";
        public ChatFunction Function { get; set; } = new ChatFunction();
    }

    public class ChatFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    }

    public class ChatToolChoice
    {
        public string Type { get; set; } = "function";
        public ChatFunctionChoice Function { get; set; } = new ChatFunctionChoice();
    }

    public class ChatFunctionChoice
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ChatResponseFormat
    {
        public string Type { get; set; } = "text";
    }

    public class AdditionalPropertiesDictionary : Dictionary<string, string>
    {
    }

    public class ProgressNotificationValue
    {
        public string? ProgressToken { get; set; }
        public string? Content { get; set; }
        public int? ContentIndex { get; set; }
        public ChatFinishReason? FinishReason { get; set; }
    }

    // AI Content classes
    public abstract class AIContent
    {
    }

    public class TextContent : AIContent
    {
        public TextContent(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
    }

    public class ImageContent : AIContent
    {
        public ImageContent(string imageUrl)
        {
            ImageUrl = imageUrl;
        }

        public string ImageUrl { get; set; }
    }

    public class AudioContent : AIContent
    {
        public AudioContent(string audioUrl)
        {
            AudioUrl = audioUrl;
        }

        public string AudioUrl { get; set; }
    }

    public class ResourceContent : AIContent
    {
        public ResourceContent(Resource resource)
        {
            Resource = resource;
        }

        public Resource Resource { get; set; }
    }

    public class FunctionCallContent : AIContent
    {
        public FunctionCallContent(string callId, string functionName, IDictionary<string, object?> arguments)
        {
            CallId = callId;
            FunctionName = functionName;
            Arguments = arguments;
        }

        public string CallId { get; set; }
        public string FunctionName { get; set; }
        public IDictionary<string, object?> Arguments { get; set; }

        public static FunctionCallContent CreateFromParsedArguments(JsonElement input, string id, string name, Func<JsonElement, IDictionary<string, object?>> parseArguments)
        {
            return new FunctionCallContent(id, name, parseArguments(input));
        }
    }

    public class FunctionResultContent : AIContent
    {
        public FunctionResultContent(string callId, object result)
        {
            CallId = callId;
            Result = result;
        }

        public string CallId { get; set; }
        public object Result { get; set; }
        public Exception? Exception { get; set; }
    }

    // Protocol classes
    public class CreateMessageRequestParams
    {
        public IList<PromptMessage> Messages { get; set; } = new List<PromptMessage>();
        public CreateMessageOptions? Options { get; set; }
        public string? ProgressToken { get; set; }
    }

    public class CreateMessageOptions
    {
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public float? PresencePenalty { get; set; }
        public float? FrequencyPenalty { get; set; }
        public IList<string>? StopSequences { get; set; }
        public int? Seed { get; set; }
        public string? User { get; set; }
        public IList<CreateMessageTool>? Tools { get; set; }
        public CreateMessageToolChoice? ToolChoice { get; set; }
        public CreateMessageResponseFormat? ResponseFormat { get; set; }
        public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; set; }
    }

    public class CreateMessageTool
    {
        public string Type { get; set; } = "function";
        public CreateMessageFunction Function { get; set; } = new CreateMessageFunction();
    }

    public class CreateMessageFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    }

    public class CreateMessageToolChoice
    {
        public string Type { get; set; } = "function";
        public CreateMessageFunctionChoice Function { get; set; } = new CreateMessageFunctionChoice();
    }

    public class CreateMessageFunctionChoice
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreateMessageResponseFormat
    {
        public string Type { get; set; } = "text";
    }

    public class CreateMessageResult
    {
        public const string StopReasonStop = "stop";
        public const string StopReasonMaxTokens = "max_tokens";
        public const string StopReasonToolUse = "tool_use";

        public string? StopReason { get; set; }
        public JsonObject? Meta { get; set; }
        public Role Role { get; set; }
        public IList<ContentBlock> Content { get; set; } = new List<ContentBlock>();
    }

    public class PromptMessage
    {
        public Role Role { get; set; }
        public IList<ContentBlock> Content { get; set; } = new List<ContentBlock>();
    }

    public enum Role
    {
        System,
        User,
        Assistant,
        Tool
    }

    public class ContentBlock
    {
    }

    public class TextContentBlock : ContentBlock
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ImageContentBlock : ContentBlock
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class AudioContentBlock : ContentBlock
    {
        public string AudioUrl { get; set; } = string.Empty;
    }

    public class EmbeddedResourceBlock : ContentBlock
    {
        public ResourceContents Resource { get; set; } = new ResourceContents();
    }

    public class ToolUseContentBlock : ContentBlock
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public JsonElement Input { get; set; }
    }

    public class ToolResultContentBlock : ContentBlock
    {
        public string ToolUseId { get; set; } = string.Empty;
        public IList<ContentBlock> Content { get; set; } = new List<ContentBlock>();
        public bool IsError { get; set; }
    }

    public class ResourceContents
    {
    }

    public class TextResourceContents : ResourceContents
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ImageResourceContents : ResourceContents
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class AudioResourceContents : ResourceContents
    {
        public string AudioUrl { get; set; } = string.Empty;
    }

    public class Resource
    {
        public ResourceContents ToResourceContents()
        {
            return new ResourceContents();
        }
    }

    // Utility classes
    public static class McpJsonUtilities
    {
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public static class Throw
    {
        public static void IfNull(object? argument, string? paramName = null)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}