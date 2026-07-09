using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

string assemblyPath = args.Length > 0
    ? args[0]
    : @"D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";

string filter = args.Length > 1 ? args[1] : "Mod";

using FileStream stream = File.OpenRead(assemblyPath);
using PEReader peReader = new(stream);
MetadataReader reader = peReader.GetMetadataReader();

foreach (TypeDefinitionHandle typeHandle in reader.TypeDefinitions)
{
    TypeDefinition typeDefinition = reader.GetTypeDefinition(typeHandle);
    string typeName = reader.GetString(typeDefinition.Name);
    string typeNamespace = reader.GetString(typeDefinition.Namespace);
    string fullName = string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";

    if (!fullName.Contains(filter, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    Console.WriteLine($"TYPE {fullName}");

    foreach (PropertyDefinitionHandle propertyHandle in typeDefinition.GetProperties())
    {
        PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
        Console.WriteLine($"  PROP {reader.GetString(property.Name)}");
    }

    foreach (FieldDefinitionHandle fieldHandle in typeDefinition.GetFields())
    {
        FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
        Console.WriteLine($"  FIELD {reader.GetString(field.Name)}");
    }

    foreach (MethodDefinitionHandle methodHandle in typeDefinition.GetMethods())
    {
        MethodDefinition method = reader.GetMethodDefinition(methodHandle);
        Console.WriteLine($"  METHOD {reader.GetString(method.Name)} PARAMS {FormatParameters(method)}");
    }

    foreach (CustomAttributeHandle customAttributeHandle in typeDefinition.GetCustomAttributes())
    {
        Console.WriteLine($"  ATTR {FormatAttribute(customAttributeHandle)}");
    }

    Console.WriteLine();
}

Console.WriteLine("CUSTOM ATTRIBUTE USAGE");
foreach (TypeDefinitionHandle typeHandle in reader.TypeDefinitions)
{
    TypeDefinition typeDefinition = reader.GetTypeDefinition(typeHandle);
    string typeName = reader.GetString(typeDefinition.Name);
    string typeNamespace = reader.GetString(typeDefinition.Namespace);
    string fullName = string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";

    foreach (MethodDefinitionHandle methodHandle in typeDefinition.GetMethods())
    {
        MethodDefinition method = reader.GetMethodDefinition(methodHandle);
        foreach (CustomAttributeHandle attributeHandle in method.GetCustomAttributes())
        {
            string attribute = FormatAttribute(attributeHandle);
            if (attribute.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"METHOD {fullName}.{reader.GetString(method.Name)} ATTR {attribute}");
            }
        }
    }
}

string FormatParameters(MethodDefinition method)
{
    List<string> names = [];
    foreach (ParameterHandle parameterHandle in method.GetParameters())
    {
        Parameter parameter = reader.GetParameter(parameterHandle);
        names.Add(reader.GetString(parameter.Name));
    }

    return $"({string.Join(", ", names)})";
}

string FormatAttribute(CustomAttributeHandle attributeHandle)
{
    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
    EntityHandle ctor = attribute.Constructor;

    if (ctor.Kind == HandleKind.MemberReference)
    {
        MemberReference member = reader.GetMemberReference((MemberReferenceHandle)ctor);
        return $"{FormatEntityType(member.Parent)}.{reader.GetString(member.Name)}";
    }

    if (ctor.Kind == HandleKind.MethodDefinition)
    {
        MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)ctor);
        TypeDefinition parent = reader.GetTypeDefinition(method.GetDeclaringType());
        return $"{reader.GetString(parent.Namespace)}.{reader.GetString(parent.Name)}.{reader.GetString(method.Name)}";
    }

    return ctor.Kind.ToString();
}

string FormatEntityType(EntityHandle handle)
{
    return handle.Kind switch
    {
        HandleKind.TypeReference => FormatTypeReferenceHandle((TypeReferenceHandle)handle),
        HandleKind.TypeDefinition => FormatTypeDefinition((TypeDefinitionHandle)handle),
        _ => handle.Kind.ToString()
    };
}

string FormatTypeReferenceHandle(TypeReferenceHandle handle)
{
    TypeReference reference = reader.GetTypeReference(handle);
    string ns = reader.GetString(reference.Namespace);
    string name = reader.GetString(reference.Name);
    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
}

string FormatTypeDefinition(TypeDefinitionHandle handle)
{
    TypeDefinition definition = reader.GetTypeDefinition(handle);
    string ns = reader.GetString(definition.Namespace);
    string name = reader.GetString(definition.Name);
    return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
}
