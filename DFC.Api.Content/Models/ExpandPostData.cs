namespace DFC.Api.Content.Models
{
    public class ExpandPostData
    {
        public bool MultiDirectional { get; set; } = false;
        public int MaxDepth { get; set; } = 0;
        public string[] TypesToInclude { get; set; } = {};
    }
}