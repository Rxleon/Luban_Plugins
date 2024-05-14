using System.Text;
using Luban.CSharp.TypeVisitors;
using Luban.Defs;
using Luban.Utils;
using Scriban.Runtime;

namespace Luban.DataTarget.MemoryPack;

public partial class CsharpMemoryPackTemplateExtension : ScriptObject
{
    public static string GenUnion(DefBean bean)
    {
        if(bean is null)
        {
            return string.Empty;
        }

        if(!string.IsNullOrEmpty(bean.Parent))
        {
            return string.Empty;
        }

        if(bean.Children is null || bean.Children.Count <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        int index = 0;

        foreach(var child in bean.Children)
        {
            sb.AppendLine($"[MemoryPackUnion({index++}, typeof({child.FullName}))]");
        }

        return sb.ToString().TrimEnd('\n');
    }

    public static string GenCategoryInit(DefTable table, string function)
    {
        if(table.Name.StartsWith("Localize"))
        {
            return string.Empty;
        }

        return$"{table.Name}.Instance.{function}";
    }

    public static string GenConfigConstructor(DefBean bean)
    {
        var constructor = new StringBuilder();
        var parentConstructor = new StringBuilder();
        var body = new StringBuilder();
        var baseConstructor = new StringBuilder();

        foreach(var field in bean.Fields)
        {
            var declareType = _GetDeclareType(field);
            if(!field.NeedExport())
            {
                continue;
            }

            if(field.CType.HasTag("text"))
            {
                constructor.Append($"{declareType} _{field.Name}_key,");
                body.AppendLine($"\tthis._{field.Name}_key = _{field.Name}_key;");
            }
            else
            {
                constructor.Append($"{declareType} {field.Name},");
                body.AppendLine($"\tthis.{field.Name} = {field.Name};");
            }
        }

        DefBean currentBean = bean;
        while (currentBean.ParentDefType != null && !string.IsNullOrEmpty(currentBean.Parent))
        {
            currentBean = currentBean.ParentDefType;
            
            foreach(var field in currentBean.Fields)
            {
                var declareType = _GetDeclareType(field);
                if(!field.NeedExport())
                {
                    continue;
                }

                if(field.CType.HasTag("text"))
                {
                    parentConstructor.Append($"{declareType} _{field.Name}_key,");
                    baseConstructor.Append($"_{field.Name}_key,");
                }
                else
                {
                    parentConstructor.Append($"{declareType} {field.Name},");
                    baseConstructor.Append($"{field.Name},");
                }
            }
        }

        if(baseConstructor.Length <= 0)
        {
            return$$"""
                    [MemoryPackConstructor]
                    public {{bean.Name}}({{constructor.ToString().TrimEnd(',')}}) {{baseConstructor}}
                    {
                    {{body.ToString().TrimEnd('\n')}}
                    }
                    """;
        }
        else
        {
            return$$"""
                    [MemoryPackConstructor]
                    public {{bean.Name}}({{constructor.ToString() + parentConstructor.ToString().TrimEnd(',')}}) : base({{baseConstructor.ToString().TrimEnd(',')}})
                    {
                    {{body.ToString().TrimEnd('\n')}}
                    }
                    """;
        }
    }

    private static string _GetDeclareType(DefField field)
    {
        switch(field.CType.TypeName)
        {
            case "list": return$"IReadOnlyList<{field.CType.ElementType.Apply(EditorDeclaringTypeNameVisitor.Ins)}>";
        }

        return field.CType.Apply(EditorDeclaringTypeNameVisitor.Ins);
    }
}