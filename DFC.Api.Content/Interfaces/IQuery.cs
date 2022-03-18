namespace DFC.Api.Content.Interfaces
{
    public interface IQuery<out TRecord>
    {
        public string QueryText { get; }
        public string ContentType { get; }
    }
}