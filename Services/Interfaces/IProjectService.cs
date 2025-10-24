using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementApp.Models;

namespace TaskManagementApp.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages project-related operations.
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Retrieves all projects.
        /// </summary>
        /// <returns>A collection of all projects.</returns>
        Task<IEnumerable<Project>> GetAllProjectsAsync();

        /// <summary>
        /// Retrieves a specific project by its ID, including its tasks.
        /// </summary>
        /// <param name="id">The ID of the project to retrieve.</param>
        /// <returns>The project with the specified ID, or null if not found.</returns>
        Task<Project> GetProjectByIdAsync(int id);

        /// <summary>
        /// Creates a new empty project.
        /// </summary>
        /// <param name="project">The project to create.</param>
        /// <returns>The created project.</returns>
        Task<Project> CreateProjectAsync(Project project);

        /// <summary>
        /// Creates a new project from a template, including all its hierarchical tasks.
        /// </summary>
        /// <param name="templateId">The ID of the project template.</param>
        /// <param name="projectName">The name for the new project.</param>
        /// <param name="projectDescription">The description for the new project.</param>
        /// <returns>The newly created project.</returns>
        Task<Project> CreateProjectFromTemplateAsync(int templateId, string projectName, string projectDescription);

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        /// <param name="project">The project with updated information.</param>
        Task UpdateProjectAsync(Project project);

        /// <summary>
        /// Deletes a project and all its associated tasks and data.
        /// </summary>
        /// <param name="id">The ID of the project to delete.</param>
        Task DeleteProjectAsync(int id);

        /// <summary>
        /// Deletes multiple projects and their associated data in a single operation.
        /// </summary>
        /// <param name="projectIds">An array of project IDs to delete.</param>
        Task DeleteMultipleProjectsAsync(int[] projectIds);

        /// <summary>
        /// Checks if a project with the specified ID exists.
        /// </summary>
        /// <param name="id">The ID of the project to check.</param>
        /// <returns>True if the project exists; otherwise, false.</returns>
        Task<bool> ProjectExistsAsync(int id);

        /// <summary>
        /// Retrieves a project template preview, formatted for UI display.
        /// </summary>
        /// <param name="id">The ID of the project template.</param>
        /// <returns>An object representing the hierarchical structure of the template.</returns>
        Task<object> GetTemplatePreviewAsync(int id);

        /// <summary>
        /// Retrieves all available project templates.
        /// </summary>
        /// <returns>A collection of all project templates.</returns>
        Task<IEnumerable<ProjectTemplate>> GetAllProjectTemplatesAsync();
    }
}