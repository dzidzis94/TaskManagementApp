using TaskManagementApp.Models;
using System.Threading.Tasks;

namespace TaskManagementApp.Services.Interfaces
{
    public interface ITemplateService
    {
        Task<Project> GenerateProjectFromTemplateAsync(int templateId, Project targetProject);
    }
}
