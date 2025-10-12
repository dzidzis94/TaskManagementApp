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
        /// Creates a new project, optionally applying a template to generate tasks.
        /// </summary>
        /// <param name="project">The project to create.</param>
        /// <param name="templateId">The optional ID of the template to apply.</param>
        Task CreateProjectAsync(Project project, int? templateId);

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