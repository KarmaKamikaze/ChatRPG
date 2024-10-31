using LangChain.Abstractions.Schema;

namespace ChatRPG.API;

public class ReActChainNoFinalAnswerReachedException(string message, IChainValues chainValues) : Exception(message)
{
    public IChainValues ChainValues { get; set; } = chainValues;
}
