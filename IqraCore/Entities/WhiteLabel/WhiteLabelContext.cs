namespace IqraCore.Entities.WhiteLabel
{
    public class WhiteLabelContext
    {
        public bool IsWhiteLabelRequest { get; set; } = false;
        public WhiteLabelRequestContext? RequestContext { get; set; } = null;
    }
}
