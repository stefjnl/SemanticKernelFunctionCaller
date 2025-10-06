using SemanticKernelFunctionCaller.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace SemanticKernelFunctionCaller.Infrastructure.Interfaces;

public interface IChatClientProvider : ISemanticKernelFunctionCaller
{
    IChatClient GetChatClient();
}