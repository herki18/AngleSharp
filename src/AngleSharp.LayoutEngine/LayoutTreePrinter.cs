using System;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace AngleSharp.LayoutEngine
{
    using Core;

    /// <summary>
    /// Utility class to print the layout tree structure in a readable format.
    /// Provides multiple output formats for debugging and visualization.
    /// </summary>
    public class LayoutTreePrinter
    {
        /// <summary>
        /// Prints the layout tree to a string with custom formatting.
        /// </summary>
        /// <param name="tree">The layout tree to print.</param>
        /// <param name="includeDetails">Whether to include detailed box information.</param>
        /// <param name="maxDepth">Maximum depth to print (-1 for unlimited).</param>
        /// <returns>A formatted string representation of the layout tree.</returns>
        public static string PrintToString(LayoutTree tree, bool includeDetails = true, int maxDepth = -1)
        {
            if (tree == null || tree.Root == null)
                return "Empty Layout Tree";

            var sb = new StringBuilder();
            PrintNode(sb, tree.Root, 0, includeDetails, maxDepth);
            return sb.ToString();
        }

        /// <summary>
        /// Prints the layout tree to the console with custom formatting.
        /// </summary>
        public static void PrintToConsole(LayoutTree tree, bool includeDetails = true, int maxDepth = -1)
        {
            Console.WriteLine(PrintToString(tree, includeDetails, maxDepth));
        }

        /// <summary>
        /// Prints the layout tree filtered by a predicate.
        /// </summary>
        /// <param name="tree">The layout tree to print.</param>
        /// <param name="predicate">A function that determines whether a node should be included.</param>
        /// <param name="includeDetails">Whether to include detailed box information.</param>
        /// <param name="maxDepth">Maximum depth to print (-1 for unlimited).</param>
        /// <returns>A formatted string representation of the filtered layout tree.</returns>
        public static string PrintFiltered(LayoutTree tree, Func<LayoutNode, bool> predicate, bool includeDetails = true, int maxDepth = -1)
        {
            if (tree == null || tree.Root == null)
                return "Empty Layout Tree";

            var sb = new StringBuilder();
            PrintFilteredNode(sb, tree.Root, 0, includeDetails, maxDepth, predicate);
            return sb.ToString();
        }

        /// <summary>
        /// Prints the layout tree to HTML for visual inspection.
        /// </summary>
        public static string PrintToHtml(LayoutTree tree, int maxDepth = -1)
        {
            if (tree == null || tree.Root == null)
                return "<div>Empty Layout Tree</div>";

            var sb = new StringBuilder();
            sb.AppendLine("<div class='layout-tree'>");
            PrintNodeHtml(sb, tree.Root, 0, maxDepth);
            sb.AppendLine("</div>");

            // Add CSS for styling
            sb.AppendLine("<style>");
            sb.AppendLine(".layout-tree { font-family: monospace; }");
            sb.AppendLine(".layout-node { margin-left: 20px; padding: 5px; border-left: 1px solid #ccc; }");
            sb.AppendLine(".node-type { font-weight: bold; }");
            sb.AppendLine(".node-details { color: #666; font-size: 0.9em; }");
            sb.AppendLine(".node-box { color: #0066cc; }");
            sb.AppendLine("</style>");

            return sb.ToString();
        }

        /// <summary>
        /// Exports the layout tree to a specified file format.
        /// </summary>
        /// <param name="tree">The layout tree to export.</param>
        /// <param name="format">The format to export to.</param>
        /// <param name="maxDepth">Maximum depth to export (-1 for unlimited).</param>
        /// <returns>A string representation of the layout tree in the specified format.</returns>
        public static string ExportToFormat(LayoutTree tree, ExportFormat format, int maxDepth = -1)
        {
            if (tree == null || tree.Root == null)
                return format == ExportFormat.Xml ? "<LayoutTree/>" : "{}";

            switch (format)
            {
                case ExportFormat.Json:
                    return ExportToJson(tree.Root, maxDepth);
                case ExportFormat.Xml:
                    return ExportToXml(tree.Root, maxDepth);
                case ExportFormat.TreeView:
                    return ExportToTreeView(tree.Root, maxDepth);
                default:
                    return PrintToString(tree, true, maxDepth);
            }
        }

        private static void PrintNode(StringBuilder sb, LayoutNode node, int level, bool includeDetails, int maxDepth)
        {
            if (maxDepth >= 0 && level > maxDepth)
                return;

            string indent = new string(' ', level * 2);

            // Print node type and basic info
            string nodeType = node.DomNode?.GetType().Name ?? "Unknown";
            string nodeId = node.Id ?? "";

            sb.Append($"{indent}+ {nodeType}");
            if (!string.IsNullOrEmpty(nodeId))
                sb.Append($" [{nodeId}]");
            sb.AppendLine();

            // Print display and position types
            sb.AppendLine($"{indent}  Display: {node.Display}, Position: {node.Position}");

            // Print box details if available
            if (node.Box != null && includeDetails)
            {
                var box = node.Box;
                sb.AppendLine($"{indent}  Box: X={box.X:F1}, Y={box.Y:F1}, Width={box.Width:F1}, Height={box.Height:F1}");
                sb.AppendLine($"{indent}  Margin: {box.MarginTop:F1} {box.MarginRight:F1} {box.MarginBottom:F1} {box.MarginLeft:F1}");
                sb.AppendLine($"{indent}  Border: {box.BorderTop:F1} {box.BorderRight:F1} {box.BorderBottom:F1} {box.BorderLeft:F1}");
                sb.AppendLine($"{indent}  Padding: {box.PaddingTop:F1} {box.PaddingRight:F1} {box.PaddingBottom:F1} {box.PaddingLeft:F1}");
            }

            // Print formatting context info
            if (node.FormattingContext != null)
            {
                sb.AppendLine($"{indent}  Formatting: {node.FormattingContext.GetType().Name}");
            }

            // Print children
            foreach (var child in node.Children)
            {
                PrintNode(sb, child, level + 1, includeDetails, maxDepth);
            }
        }

        private static void PrintFilteredNode(StringBuilder sb, LayoutNode node, int level, bool includeDetails, int maxDepth, Func<LayoutNode, bool> predicate)
        {
            if (maxDepth >= 0 && level > maxDepth)
                return;

            if (predicate(node))
            {
                string indent = new string(' ', level * 2);

                // Print node type and basic info
                string nodeType = node.DomNode?.GetType().Name ?? "Unknown";
                string nodeId = node.Id ?? "";

                sb.Append($"{indent}+ {nodeType}");
                if (!string.IsNullOrEmpty(nodeId))
                    sb.Append($" [{nodeId}]");
                sb.AppendLine();

                // Print display and position types
                sb.AppendLine($"{indent}  Display: {node.Display}, Position: {node.Position}");

                // Print box details if available
                if (node.Box != null && includeDetails)
                {
                    var box = node.Box;
                    sb.AppendLine($"{indent}  Box: X={box.X:F1}, Y={box.Y:F1}, Width={box.Width:F1}, Height={box.Height:F1}");
                    sb.AppendLine($"{indent}  Margin: {box.MarginTop:F1} {box.MarginRight:F1} {box.MarginBottom:F1} {box.MarginLeft:F1}");
                    sb.AppendLine($"{indent}  Border: {box.BorderTop:F1} {box.BorderRight:F1} {box.BorderBottom:F1} {box.BorderLeft:F1}");
                    sb.AppendLine($"{indent}  Padding: {box.PaddingTop:F1} {box.PaddingRight:F1} {box.PaddingBottom:F1} {box.PaddingLeft:F1}");
                }

                // Print formatting context info
                if (node.FormattingContext != null)
                {
                    sb.AppendLine($"{indent}  Formatting: {node.FormattingContext.GetType().Name}");
                }
            }

            // Print children
            foreach (var child in node.Children)
            {
                PrintFilteredNode(sb, child, level + (predicate(node) ? 1 : 0), includeDetails, maxDepth, predicate);
            }
        }

        private static void PrintNodeHtml(StringBuilder sb, LayoutNode node, int level, int maxDepth)
        {
            if (maxDepth >= 0 && level > maxDepth)
                return;

            // Print node type and basic info
            string nodeType = WebUtility.HtmlEncode(node.DomNode?.GetType().Name ?? "Unknown");
            string nodeId = WebUtility.HtmlEncode(node.Id ?? "");

            sb.AppendLine("<div class='layout-node'>");
            sb.Append($"<div class='node-type'>{nodeType}");
            if (!string.IsNullOrEmpty(nodeId))
                sb.Append($" [{nodeId}]");
            sb.AppendLine("</div>");

            // Print display and position types
            sb.AppendLine($"<div class='node-details'>Display: {node.Display}, Position: {node.Position}</div>");

            // Print box details if available
            if (node.Box != null)
            {
                var box = node.Box;
                sb.AppendLine("<div class='node-box'>");
                sb.AppendLine($"Box: X={box.X:F1}, Y={box.Y:F1}, Width={box.Width:F1}, Height={box.Height:F1}<br>");
                sb.AppendLine($"Margin: {box.MarginTop:F1} {box.MarginRight:F1} {box.MarginBottom:F1} {box.MarginLeft:F1}<br>");
                sb.AppendLine($"Border: {box.BorderTop:F1} {box.BorderRight:F1} {box.BorderBottom:F1} {box.BorderLeft:F1}<br>");
                sb.AppendLine($"Padding: {box.PaddingTop:F1} {box.PaddingRight:F1} {box.PaddingBottom:F1} {box.PaddingLeft:F1}");
                sb.AppendLine("</div>");
            }

            // Print formatting context info
            if (node.FormattingContext != null)
            {
                sb.AppendLine($"<div class='node-details'>Formatting: {node.FormattingContext.GetType().Name}</div>");
            }

            // Print children
            foreach (var child in node.Children)
            {
                PrintNodeHtml(sb, child, level + 1, maxDepth);
            }

            sb.AppendLine("</div>");
        }

        private static string ExportToJson(LayoutNode node, int maxDepth, int currentDepth = 0)
        {
            if (maxDepth >= 0 && currentDepth > maxDepth)
                return "{}";

            var sb = new StringBuilder();
            sb.Append("{\n");

            // Basic node information
            sb.AppendLine($"  \"type\": \"{(node.DomNode?.GetType().Name ?? "Unknown")}\",");
            sb.AppendLine($"  \"id\": \"{(node.Id ?? "")}\",");
            sb.AppendLine($"  \"display\": \"{node.Display}\",");
            sb.AppendLine($"  \"position\": \"{node.Position}\",");

            // Box details
            if (node.Box != null)
            {
                var box = node.Box;
                sb.AppendLine("  \"box\": {");
                sb.AppendLine($"    \"x\": {box.X:F1},");
                sb.AppendLine($"    \"y\": {box.Y:F1},");
                sb.AppendLine($"    \"width\": {box.Width:F1},");
                sb.AppendLine($"    \"height\": {box.Height:F1},");
                sb.AppendLine("    \"margin\": [" + $"{box.MarginTop:F1}, {box.MarginRight:F1}, {box.MarginBottom:F1}, {box.MarginLeft:F1}" + "],");
                sb.AppendLine("    \"border\": [" + $"{box.BorderTop:F1}, {box.BorderRight:F1}, {box.BorderBottom:F1}, {box.BorderLeft:F1}" + "],");
                sb.AppendLine("    \"padding\": [" + $"{box.PaddingTop:F1}, {box.PaddingRight:F1}, {box.PaddingBottom:F1}, {box.PaddingLeft:F1}" + "]");
                sb.AppendLine("  },");
            }

            // Formatting context
            if (node.FormattingContext != null)
            {
                sb.AppendLine($"  \"formattingContext\": \"{node.FormattingContext.GetType().Name}\",");
            }

            // Children
            if (node.Children.Count > 0 && (maxDepth < 0 || currentDepth < maxDepth))
            {
                sb.AppendLine("  \"children\": [");

                for (int i = 0; i < node.Children.Count; i++)
                {
                    bool isLast = i == node.Children.Count - 1;
                    var childJson = ExportToJson(node.Children[i], maxDepth, currentDepth + 1);

                    // Indent the child JSON
                    childJson = childJson.Replace("\n", "\n    ");
                    sb.Append("    " + childJson);

                    if (!isLast)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }

                sb.AppendLine("  ]");
            }
            else
            {
                // No children, remove trailing comma from last property
                sb.Length -= 2; // Remove the trailing comma and newline
                sb.AppendLine(); // Add back the newline
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string ExportToXml(LayoutNode node, int maxDepth)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            ExportNodeToXml(sb, node, 0, maxDepth);
            return sb.ToString();
        }

        private static void ExportNodeToXml(StringBuilder sb, LayoutNode node, int level, int maxDepth)
        {
            if (maxDepth >= 0 && level > maxDepth)
                return;

            string indent = new string(' ', level * 2);
            string nodeType = WebUtility.HtmlEncode(node.DomNode?.GetType().Name ?? "Unknown");
            string nodeId = node.Id ?? "";

            sb.Append($"{indent}<Node type=\"{nodeType}\"");
            if (!string.IsNullOrEmpty(nodeId))
                sb.Append($" id=\"{WebUtility.HtmlEncode(nodeId)}\"");
            sb.Append($" display=\"{node.Display}\" position=\"{node.Position}\"");

            if (node.Children.Count == 0 && node.Box == null && node.FormattingContext == null)
            {
                sb.AppendLine(" />");
                return;
            }

            sb.AppendLine(">");

            // Box details
            if (node.Box != null)
            {
                var box = node.Box;
                sb.AppendLine($"{indent}  <Box x=\"{box.X:F1}\" y=\"{box.Y:F1}\" width=\"{box.Width:F1}\" height=\"{box.Height:F1}\">");
                sb.AppendLine($"{indent}    <Margin top=\"{box.MarginTop:F1}\" right=\"{box.MarginRight:F1}\" bottom=\"{box.MarginBottom:F1}\" left=\"{box.MarginLeft:F1}\" />");
                sb.AppendLine($"{indent}    <Border top=\"{box.BorderTop:F1}\" right=\"{box.BorderRight:F1}\" bottom=\"{box.BorderBottom:F1}\" left=\"{box.BorderLeft:F1}\" />");
                sb.AppendLine($"{indent}    <Padding top=\"{box.PaddingTop:F1}\" right=\"{box.PaddingRight:F1}\" bottom=\"{box.PaddingBottom:F1}\" left=\"{box.PaddingLeft:F1}\" />");
                sb.AppendLine($"{indent}  </Box>");
            }

            // Formatting context
            if (node.FormattingContext != null)
            {
                string contextType = WebUtility.HtmlEncode(node.FormattingContext.GetType().Name);
                sb.AppendLine($"{indent}  <FormattingContext type=\"{contextType}\" />");
            }

            // Children
            if (node.Children.Count > 0 && (maxDepth < 0 || level < maxDepth))
            {
                sb.AppendLine($"{indent}  <Children>");
                foreach (var child in node.Children)
                {
                    ExportNodeToXml(sb, child, level + 2, maxDepth);
                }
                sb.AppendLine($"{indent}  </Children>");
            }

            sb.AppendLine($"{indent}</Node>");
        }

        private static string ExportToTreeView(LayoutNode node, int maxDepth)
        {
            var sb = new StringBuilder();
            ExportTreeViewNode(sb, node, "", true, maxDepth, 0);
            return sb.ToString();
        }

        private static void ExportTreeViewNode(StringBuilder sb, LayoutNode node, string prefix, bool isLast, int maxDepth, int level)
        {
            if (maxDepth >= 0 && level > maxDepth)
                return;

            // Print the node with appropriate prefix
            sb.Append(prefix);
            sb.Append(isLast ? "└── " : "├── ");

            string typeAndId = node.DomNode?.GetType().Name ?? "Unknown";
            if (!string.IsNullOrEmpty(node.Id))
                typeAndId += $" [{node.Id}]";

            sb.AppendLine(typeAndId);

            // Prepare prefix for children
            string childPrefix = prefix + (isLast ? "    " : "│   ");

            // Process children
            int lastIndex = node.Children.Count - 1;
            for (int i = 0; i < node.Children.Count; i++)
            {
                bool childIsLast = i == lastIndex;
                ExportTreeViewNode(sb, node.Children[i], childPrefix, childIsLast, maxDepth, level + 1);
            }
        }
    }

    /// <summary>
    /// Export formats supported by the layout tree printer.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Export as plain text.
        /// </summary>
        Text,

        /// <summary>
        /// Export as JSON.
        /// </summary>
        Json,

        /// <summary>
        /// Export as XML.
        /// </summary>
        Xml,

        /// <summary>
        /// Export as a tree view.
        /// </summary>
        TreeView
    }
}