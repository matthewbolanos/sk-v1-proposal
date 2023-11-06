
namespace Microsoft.SemanticKernel.Handlebars
{
    public interface IModelContext<T> where T : IMessageContent
    {
        public List<T> GetContext();
    }
}