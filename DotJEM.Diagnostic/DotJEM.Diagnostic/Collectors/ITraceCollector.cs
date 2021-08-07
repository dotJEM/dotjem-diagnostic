using System.Threading.Tasks;

namespace DotJEM.Diagnostic.Collectors
{
    public interface ITraceCollector<in TEvent>
    {
        Task Collect(TEvent trace);
    }
}