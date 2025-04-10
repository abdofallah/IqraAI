using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Telephony;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessConversationsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly CallQueueRepository _callQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationAudioRepository _conversationAudioRepository;

        public BusinessConversationsManager(
            BusinessManager businessManager,
            CallQueueRepository callQueueRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationAudioRepository conversationAudioRepository
        )
        {
            _parentBusinessManager = businessManager;

            _callQueueRepository = callQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _conversationAudioRepository = conversationAudioRepository;
        }


    }
}
