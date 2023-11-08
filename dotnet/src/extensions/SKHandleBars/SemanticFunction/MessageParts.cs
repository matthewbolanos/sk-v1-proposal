

using System.Collections;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.Handlebars;
public class MessageParts : IList<object>
{
    private List<object> Parts { get; set; }

    public int Count => Parts!.Count;

    public bool IsReadOnly => throw new NotImplementedException();

    public object this[int index] { get => Parts[index]; set => Parts[index] = value; }

    public MessageParts(List<object>? parts = default)
    {
        if (parts is null)
        {
            Parts = new List<object>();
        }
        else
        {
            Parts = parts;
        }
    }
    
    public override string ToString()
    {
        return string.Join("", Parts!);
    }

    public IEnumerator<object> GetEnumerator()
    {
        return Parts!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(object item)
    {
        return Parts.IndexOf(item);
    }

    public void Insert(int index, object item)
    {
        Parts.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        Parts.RemoveAt(index);
    }

    public void Add(object item)
    {
        Parts.Add(item);
    }

    public void Clear()
    {
        Parts.Clear();
    }

    public bool Contains(object item)
    {
        return Parts.Contains(item);
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
        Parts.CopyTo(array, arrayIndex);
    }

    public bool Remove(object item)
    {
        return Parts.Remove(item);
    }
}