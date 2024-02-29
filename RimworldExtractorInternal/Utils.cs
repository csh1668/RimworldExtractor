using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using DocumentFormat.OpenXml.Office.Word;
using RimworldExtractorInternal.Records;

namespace RimworldExtractorInternal
{
    internal static class Utils
    {
        public static XmlNode Append(this XmlNode parent, Action<XmlNode> work)
        {
            work(parent);
            return parent;
        }
        public static XmlElement Append(this XmlElement parent, Action<XmlElement> work)
        {
            work(parent);
            return parent;
        }

        public static XmlElement AppendElement(this XmlNode parent, string name, string? innerText = null)
        {
            var child = (XmlElement?)parent.AppendChild(
                (parent.NodeType == XmlNodeType.Document ? (XmlDocument)parent : parent.OwnerDocument!)
                .CreateElement(name)) ?? throw new NullReferenceException();
            if (innerText != null)
            {
                child.InnerText = innerText;
            }

            return child;
        }
        public static XmlElement AppendElement(this XmlElement parent, string name, string? innerText = null)
        {
            var child = (XmlElement?)parent.AppendChild(parent.OwnerDocument.CreateElement(name)) ??
                        throw new NullReferenceException();

            if (innerText != null)
            {
                child.InnerText = innerText;
            }
            return child;
        }

        public static XmlElement AppendElement(this XmlNode parent, string name, Action<XmlNode> work)
        {
            var child = parent.AppendElement(name);
            work(child);
            return child;
        }
        public static XmlElement AppendElement(this XmlElement parent, string name, Action<XmlElement> work)
        {
            var child = parent.AppendElement(name);
            work(child);
            return child;
        }

        public static XmlAttribute? AppendAttribute(this XmlNode parent, string name, string? value)
        {
            if (parent is XmlElement e)
                return e.AppendAttribute(name, value);
            else
                return null;
        }
        public static XmlAttribute AppendAttribute(this XmlElement parent, string name, string? value)
        {
            var attr = parent.Attributes.Append(parent.OwnerDocument.CreateAttribute(name));
            if (value != null)
            {
                attr.Value = value;
            }
            return attr;
        }

        public static XmlComment AppendComment(this XmlElement parent, string comment)
        {
            var child = (XmlComment)parent.AppendChild(parent.OwnerDocument.CreateComment(comment))!;
            return child;
        }

        public static List<T> Combine<T>(this List<T>? first, List<T>? second)
        {
            var newList = new List<T>();
            if (first != null)
            {
                newList.AddRange(first);
            }

            if (second != null)
            {
                newList.AddRange(second);
            }
            return newList;
        }

        public static IEnumerable<XmlNode> Where(this XmlNodeList nodes, Predicate<XmlNode> predicate)
        {
            return nodes.OfType<XmlNode>().Where(x => predicate(x));
        }

        public static IEnumerable<T> Select<T>(this XmlNodeList nodes, Func<XmlNode, T> selector)
        {
            return nodes.OfType<XmlNode>().Select(selector);
        }

        public static XmlNode? FirstOrDefault(this XmlNodeList nodes, Predicate<XmlNode> predicate)
        {
            return nodes.OfType<XmlNode>().FirstOrDefault(x => predicate(x));
        }

        public static bool HasSameElements<T>(this List<T> node1, List<T>? node2)
        {
            if (node2 == null)
                return false;
            return !node1.Except(node2).Any() && !node2.Except(node1).Any();
        }

        public static bool HasAttribute(this XmlNode node, string attributeName)
        {
            return node.Attributes?[attributeName] != null;
        }

        public static bool HasAttribute(this XmlNode node, string attributeName, string value)
        {
            return node.Attributes?[attributeName]?.Value == value;
        }

        public static string GetXpath(string className, string nodeName)
        {
            var defName = nodeName.Split('.')[0];
            var token = nodeName[(defName.Length + 1)..].Split('.');
            for (int i = 0; i < token.Length; i++)
            {
                if (int.TryParse(token[i], out var k))
                {
                    token[i] = $"li[{k + 1}]";
                }
                else if (char.IsUpper(token[i][0]))
                {
                    token[i] = $"*[text()='{token[i]}']";
                }
            }

            nodeName = $"/Defs/{className}[defName=\"{defName}\"]/";
            nodeName += string.Join('/', token);
            return nodeName;
        }

        public static string StrVal(this IXLCell cell)
        {
            var value = cell.Value;
            if (value.TryGetText(out string str))
                return str;
            return string.Empty;
        }
    }
}
