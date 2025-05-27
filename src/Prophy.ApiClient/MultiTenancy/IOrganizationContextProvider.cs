using System.Threading.Tasks;

namespace Prophy.ApiClient.MultiTenancy
{
    /// <summary>
    /// Defines the contract for providing and managing organization context.
    /// </summary>
    public interface IOrganizationContextProvider
    {
        /// <summary>
        /// Gets the current organization context.
        /// </summary>
        /// <returns>The current organization context, or null if no context is set.</returns>
        OrganizationContext? GetCurrentContext();

        /// <summary>
        /// Sets the current organization context.
        /// </summary>
        /// <param name="context">The organization context to set.</param>
        void SetCurrentContext(OrganizationContext? context);

        /// <summary>
        /// Resolves an organization context by organization code.
        /// </summary>
        /// <param name="organizationCode">The organization code to resolve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the resolved organization context, or null if not found.</returns>
        Task<OrganizationContext?> ResolveContextAsync(string organizationCode);

        /// <summary>
        /// Clears the current organization context.
        /// </summary>
        void ClearCurrentContext();
    }
} 