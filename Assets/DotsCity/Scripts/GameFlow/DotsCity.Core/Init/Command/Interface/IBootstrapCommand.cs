using System.Threading.Tasks;

namespace Spirit604.DotsCity.Core.Bootstrap
{
    public interface IBootstrapCommand
    {
        Task Execute();
    }
}