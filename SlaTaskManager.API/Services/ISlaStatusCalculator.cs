using SlaTaskManager.API.Entities;
using SlaTaskManager.API.Models;

namespace SlaTaskManager.API.Services;

public interface ISlaStatusCalculator
{
    SlaStatus Calculate(Entities.Task task, DateTime? asOf = null);
}
