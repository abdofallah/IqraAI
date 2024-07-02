namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessApp
    {
        public BusinessAppContext Context { get; set; }
        public List<BusinessAppTool> Tools { get; set; }

        public List<BusinessAppAgent> Agents { get; set; }

        public List<BusinessAppRoute> Routings { get; set; }
    }
}
