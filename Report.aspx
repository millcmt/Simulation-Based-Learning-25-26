<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="Simulation_Based_Learning.Report" %>

<!DOCTYPE html>
<link rel="stylesheet" type="text/css" href="Main.css" />
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="report-container">

    <h2 class="report-title">Simulation Report</h2>

    <asp:Repeater ID="rptClusters" runat="server">
        <ItemTemplate>

            <div class="cluster-card">

                <div class="cluster-header">
                    <h3><%# Eval("ClusterName") %></h3>
                    <span class='band <%# Eval("Band").ToString().ToLower() %>'>
                        <%# Eval("Band") %>
                    </span>
                </div>

                <div class="cluster-body">

                    <div class="score">
                        Score: <%# Eval("NormalizedScore", "{0:P0}") %>
                    </div>

                    <div class="raw">
                        Raw Score: <%# Eval("RawScore") %>
                    </div>

                    <div class="narrative">
                        <%# Eval("OutputNarrative") %>
                    </div>

                </div>

            </div>

        </ItemTemplate>
    </asp:Repeater>

</div>
    </form>
</body>
</html>
