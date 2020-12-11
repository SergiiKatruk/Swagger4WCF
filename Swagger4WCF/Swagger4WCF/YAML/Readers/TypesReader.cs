using System.Collections.Generic;
using System.Linq;
using Swagger4WCF.Data;

namespace Swagger4WCF.YAML.Readers
{
    public sealed class TypesReader
    {
        private static TypesReader instance;
        
        private TypesReader() { }
        
        public static TypesReader Instance => instance ?? (instance = new TypesReader());

        public List<TypeData> GetUsedTypes(List<MethodData> methods)
        {
            Dictionary<string, TypeData> typeDatas = new Dictionary<string, TypeData>();

            Dictionary<string, TypeData> parameterTypes = methods.
                SelectMany(method => method.Parameters.
                    Where(param => !param.IsValueType).
                        Select(parameter => parameter.TypeData)).
                GroupBy(typeData => typeData.Type.FullName).
                ToDictionary(key => key.Key, value => value.First());
            
            Dictionary<string, TypeData> returnTypes = methods.
                Select(method => method.ReturnType).
                Where(type => !type.IsValueType).
                GroupBy(t => t.Type.FullName).
                ToDictionary(key => key.Key, value => value.First());

            Dictionary<string, TypeData> methodTypes = parameterTypes.
                Concat(returnTypes.
                    Where(type => !parameterTypes.ContainsKey(type.Key))).
                ToDictionary(key => key.Key, value => value.Value);

            Dictionary<string, TypeData> propertyTypes = new Dictionary<string, TypeData>();
            Dictionary<string, TypeData> currentTypes = methodTypes;
            do
            {
                propertyTypes = currentTypes.
                    SelectMany(type => type.Value.Properties.Where(property => !property.TypeData.IsValueType)).
                    GroupBy(property => property.TypeData.Type.FullName).
                    ToDictionary(key => key.Key, value => value.First().TypeData);

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
