using LangChain.Memory;
using LangChain.Schema;

namespace ChatRPG.API.Memory;

public class ChatRPGConversationMemory : BaseChatMemory
{
    public override OutputValues LoadMemoryVariables(InputValues? inputValues)
    {
        throw new NotImplementedException();
    }

    public override List<string> MemoryVariables { get; }
}
