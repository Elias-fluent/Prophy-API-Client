<%@ Page Language="C#" %>
<%@ Import Namespace="Prophy.ApiClient" %>
<%@ Import Namespace="System" %>

<!DOCTYPE html>
<html>
<head>
    <title>Prophy API Client Test</title>
</head>
<body>
    <h1>Prophy API Client Test</h1>
    
    <%
        try
        {
            // Try to instantiate the ProphyApiClient
            var client = new ProphyApiClient("test-api-key", "test-org");
            Response.Write("<p style='color: green;'>✅ SUCCESS: ProphyApiClient instantiated successfully!</p>");
            Response.Write("<p>The Polly.Core dependency issue has been resolved.</p>");
        }
        catch (Exception ex)
        {
            Response.Write("<p style='color: red;'>❌ ERROR: " + Server.HtmlEncode(ex.Message) + "</p>");
            Response.Write("<p style='color: red;'>Exception Type: " + Server.HtmlEncode(ex.GetType().FullName) + "</p>");
            
            if (ex.InnerException != null)
            {
                Response.Write("<p style='color: red;'>Inner Exception: " + Server.HtmlEncode(ex.InnerException.Message) + "</p>");
            }
        }
    %>
    
    <hr />
    <p><a href="/">Back to Home</a></p>
</body>
</html> 