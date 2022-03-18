using System.Threading.Tasks;

using Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using Tests;

using Xunit;

namespace Test;

using GeneratorTest = CSharpSourceGeneratorTest<Adapter<JsonElementWrapperGenerator>, XUnitVerifier>;

public class JsonElementWrapperGeneratorTests
{
    [Fact]
    public async Task TestCodeFileIsGenerated()
    {
        const string codeFile = @"
using JsonElementWrapper;
using System;
using System.Text.Json;

namespace GeneratedDemo
{
    [JsonElementWrapper]
    public interface IIntrinsicsExample
    {
            bool BoolProp { get; }
            sbyte SByteProp { get; }
            byte ByteProp { get; }
            short Int16Prop { get; }
            ushort UInt16Prop { get; }
            int Int32Prop { get; }
            uint UInt32Prop { get; }
            long Int64Prop { get; }
            ulong UInt64Prop { get; }
            decimal DecimalProp { get; }
            float SingleProp { get; }
            double DoubleProp { get; }
            string StringProp { get; }
            DateTime DateTimeProp { get; }
    }

    public static class UseJsonWrapper
    {
        public static void Run()
        {
            JsonElement element = default;
            IIntrinsicsExample wrapper = element.AsIIntrinsicsExample();
            bool propVal = wrapper.BoolProp;
            string stringVal = wrapper.StringProp;
        }
    }
}
";
        const string generatedCode = @"namespace GeneratedDemo
{
    public static class IIntrinsicsExample_JsonWrapperExtensions
    {
        public static IIntrinsicsExample AsIIntrinsicsExample(this System.Text.Json.JsonElement element)
        {
            return new IIntrinsicsExample_JsonWrapper(element);
        }
    }

    internal class IIntrinsicsExample_JsonWrapper : IIntrinsicsExample
    {
        private readonly System.Text.Json.JsonElement _element;

        public IIntrinsicsExample_JsonWrapper(System.Text.Json.JsonElement element)
        {
            _element = element;
        }

        public bool BoolProp
        {
            get
            {
                return _element.GetProperty(""BoolProp"").GetBoolean();
            }
        }

        public sbyte SByteProp
        {
            get
            {
                return _element.GetProperty(""SByteProp"").GetSByte();
            }
        }

        public byte ByteProp
        {
            get
            {
                return _element.GetProperty(""ByteProp"").GetByte();
            }
        }

        public short Int16Prop
        {
            get
            {
                return _element.GetProperty(""Int16Prop"").GetInt16();
            }
        }

        public ushort UInt16Prop
        {
            get
            {
                return _element.GetProperty(""UInt16Prop"").GetUInt16();
            }
        }

        public int Int32Prop
        {
            get
            {
                return _element.GetProperty(""Int32Prop"").GetInt32();
            }
        }

        public uint UInt32Prop
        {
            get
            {
                return _element.GetProperty(""UInt32Prop"").GetUInt32();
            }
        }

        public long Int64Prop
        {
            get
            {
                return _element.GetProperty(""Int64Prop"").GetInt64();
            }
        }

        public ulong UInt64Prop
        {
            get
            {
                return _element.GetProperty(""UInt64Prop"").GetUInt64();
            }
        }

        public decimal DecimalProp
        {
            get
            {
                return _element.GetProperty(""DecimalProp"").GetDecimal();
            }
        }

        public float SingleProp
        {
            get
            {
                return _element.GetProperty(""SingleProp"").GetSingle();
            }
        }

        public double DoubleProp
        {
            get
            {
                return _element.GetProperty(""DoubleProp"").GetDouble();
            }
        }

        public string StringProp
        {
            get
            {
                return _element.GetProperty(""StringProp"").GetString();
            }
        }

        public System.DateTime DateTimeProp
        {
            get
            {
                return _element.GetProperty(""DateTimeProp"").GetDateTime();
            }
        }
    }
}
";
        const string attributeText = @"
using System;
namespace JsonElementWrapper
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""JsonElementWrapper_DEBUG"")]
    sealed class JsonElementWrapperAttribute : Attribute
    {
        public JsonElementWrapperAttribute()
        {
        }
    }
}
";

        await new GeneratorTest
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            TestState =
                {
                    Sources = { codeFile },
                    GeneratedSources =
                    {
                        (typeof(Adapter<JsonElementWrapperGenerator>), "JsonElementWrapper.cs", attributeText),
                        (typeof(Adapter<JsonElementWrapperGenerator>), "IIntrinsicsExample_JsonElementWrapper.cs", generatedCode),
                    },
                },
        }.RunAsync();
    }
}