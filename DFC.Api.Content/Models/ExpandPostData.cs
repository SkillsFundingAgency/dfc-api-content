namespace DFC.Api.Content.Models
{
    public class ExpandPostData
    {
        public bool MultiDirectional { get; set; }
        public bool CheckAncestryById { get; set; }
        public int MaxDepth { get; set; }
        public string[] TypesToInclude { get; set; } = {};
    }
}