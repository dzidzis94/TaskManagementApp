using TaskManagementApp.Models;
using System.Threading.Tasks;

namespace TaskManagementApp.Services.Interfaces
{
    public interface ITemplateService
    {
        Task<Project> GenerateProjectFromTemplateAsync(int templateId, Project targetProject);
        Task<Project> CloneProjectAsync(int sourceProjectId, string newProjectName, string newProjectDescription, List<int>? excludedTaskIds = null);
    }
}
