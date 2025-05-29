using System.Web.Mvc;
using System.Web.Routing;

namespace AspNet48.Sample
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Manuscript routes
            routes.MapRoute(
                name: "ManuscriptUpload",
                url: "manuscript/upload",
                defaults: new { controller = "Manuscript", action = "Upload" }
            );

            routes.MapRoute(
                name: "ManuscriptResults",
                url: "manuscript/results/{originId}",
                defaults: new { controller = "Manuscript", action = "Results", originId = UrlParameter.Optional }
            );

            // Author Group routes
            routes.MapRoute(
                name: "AuthorGroupManage",
                url: "authorgroup/manage/{groupId}",
                defaults: new { controller = "AuthorGroup", action = "Manage", groupId = UrlParameter.Optional }
            );

            // Journal routes
            routes.MapRoute(
                name: "JournalRecommendations",
                url: "journal/recommendations/{originId}",
                defaults: new { controller = "Journal", action = "Recommendations", originId = UrlParameter.Optional }
            );

            // Default route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
} 