﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Swagger4WCF.Core.DocumentedItems;

namespace Swagger4WCF.YAML
{
	public class Documentation
    {
        private Dictionary<string, string> m_Dictionary = new Dictionary<string, string>();

        static private string Arrange(string summary)
        {
            var _summary = summary.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            while (_summary.Contains("  ")) { _summary = _summary.Replace("  ", " "); }
            return _summary.Trim();
        }

        static public Documentation Load(string location, string assemblyName)
        {
            var _location = string.Concat(new Uri(Path.GetDirectoryName(location)).LocalPath, @"\", assemblyName, ".xml");
            if (File.Exists(_location)) { return new Documentation(_location); }
            return new Documentation();
        }

        static public Documentation Empty()
        {
            return new Documentation();
        }

        private Documentation()
        {
        }

        private Documentation(string location)
        {
            foreach (var _member in XDocument.Load(location).Descendants("member"))
            {
                var _xAttribute = _member.Attribute("name");
                if (_xAttribute != null)
                {
                    var _name = _xAttribute.Value;
                    var _xElement = _member.Element("summary");
                    if (_xElement != null) { this.m_Dictionary.Add(_name, Arrange(_xElement.Value)); }
                    if (!_name.StartsWith("M:")) { continue; }
                    var _element = _member.Element("returns");
                    if (_element != null) { this.m_Dictionary.Add(string.Concat("R", _name.Substring(1)), Arrange(_element.Value)); }
                    foreach (var _parameter in _member.Elements("param"))
                    {
                        var _attribute = _parameter.Attribute("name");
                        if (_attribute != null) { this.m_Dictionary.Add(string.Concat("A", _name.Substring(1), ".", _attribute.Value), Arrange(_parameter.Value)); }
                    }
                }
            }
        }

        public struct Method
        {
            public string Summary;
            public string Response;
        }

        public string this[TypeItem type]
        {
            get { return this[string.Concat("T:", type.FullName).Replace('<', '{').Replace('>', '}').Replace("`1", "")]; }
        }

        public Method this[MethodItem method]
        {
            get
            {
                return new Method()
                {
                    Summary = this[string.Concat("M:", method.DeclaringType.FullName, ".", method.Name, this.GetMethodParametersInfo(method)).Replace('<', '{').Replace('>', '}').Replace("`1", "")],
                    Response = this[string.Concat("R:", method.DeclaringType.FullName, ".", method.Name, this.GetMethodParametersInfo(method)).Replace('<', '{').Replace('>', '}').Replace("`1", "")]
                };
            }
        }

        public string GetMethodParametersInfo(MethodItem method)
		{
            if (!method.Parameters.Any())
                return string.Empty;

            return string.Concat("(", string.Join(",", method.Parameters.Select(_Parameter => _Parameter.Type.FullName)), ")");
        }

        public string this[PropertyItem property]
        {
            get { return this[string.Concat("P:", property.DeclaringType.FullName, ".", property.Name).Replace('<', '{').Replace('>', '}').Replace("`1", "").Replace('<', '{').Replace('>', '}').Replace("`1", "")]; }
        }

        public string this[Mono.Cecil.FieldDefinition field]
        {
            get { return this[string.Concat("F:", field.DeclaringType.FullName, ".", field.Name).Replace('<', '{').Replace('>', '}').Replace("`1", "").Replace('<', '{').Replace('>', '}').Replace("`1", "")]; }
        }

        public string this[MethodItem method, ParameterItem parameter]
        {
            get { return this[string.Concat("A:", method.DeclaringType.FullName, ".", method.Name, "(", string.Join(",", method.Parameters.Select(_Parameter => _Parameter.Type.FullName)), ").", parameter.Name).Replace('<', '{').Replace('>', '}').Replace("`1", "")]; }
        }

        public string this[string identity]
        {
            get
            {
                if (this.m_Dictionary.ContainsKey(identity)) { return this.m_Dictionary[identity]; }
                return null;
            }
        }
    }
}
