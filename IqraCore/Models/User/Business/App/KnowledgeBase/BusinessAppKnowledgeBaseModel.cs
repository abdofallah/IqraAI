using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration;

namespace IqraCore.Models.User.Business.App.KnowledgeBase
{
    public class BusinessAppKnowledgeBaseModel
    {
        public BusinessAppKnowledgeBaseModel(BusinessAppKnowledgeBase data)
        {
            Id = data.Id;
            General = data.General;
            Configuration = data.Configuration;
            Documents = data.Documents;
        }

        public string Id { get; set; }
        public BusinessAppKnowledgeBaseGeneral General { get; set; }
        public BusinessAppKnowledgeBaseConfiguration Configuration { get; set; }
        public List<long> Documents { get; set; }
    }
}
