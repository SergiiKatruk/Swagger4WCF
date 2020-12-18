using Swagger4WCF.Core.DocumentedItems;
using System.Collections.Generic;
using System.Linq;

namespace Swagger4WCF.Core.Readers
{
	public sealed class TypesReader
    {
        private static TypesReader instance;
        
        private TypesReader() { }
        
        public static TypesReader Instance => instance ?? (instance = new TypesReader());

        public List<TypeItem> GetUsedTypes(List<MethodItem> methods)
        {
            Dictionary<string, TypeItem> typeDatas = new Dictionary<string, TypeItem>();

            Dictionary<string, TypeItem> parameterTypes = methods.
                SelectMany(method => method.Parameters.
                    Where(param => !param.Type.IsValueType).
                        Select(parameter => parameter.Type)).
                GroupBy(typeData => typeData.FullName).
                ToDictionary(key => key.Key, value => value.First());
            
            Dictionary<string, TypeItem> returnTypes = methods.
                Select(method => method.ReturnType).
                Where(type => !type.IsValueType).
                GroupBy(t => t.FullName).
                ToDictionary(key => key.Key, value => value.First());

            Dictionary<string, TypeItem> methodTypes = parameterTypes.
                Concat(returnTypes.
                    Where(type => !parameterTypes.ContainsKey(type.Key))).
                ToDictionary(key => key.Key, value => value.Value);

            Dictionary<string, TypeItem> propertyTypes = new Dictionary<string, TypeItem>();
            Dictionary<string, TypeItem> currentTypes = methodTypes;
            do
            {
                propertyTypes = currentTypes.
                    SelectMany(type => type.Value.Properties.Where(property => !property.Type.IsValueType)).
                    GroupBy(property => property.Type.FullName).
                    ToDictionary(key => key.Key, value => value.First().Type);

                methodTypes = methodTypes.
                    Concat(propertyTypes.
                        Where(type => !parameterTypes.ContainsKey(type.Key))).
                    ToDictionary(key => key.Key, value => value.Value);
                currentTypes = propertyTypes;
            }
            while (propertyTypes.Any());

            return methodTypes.Select(d => d.Value).ToList();
        }
    }
}
