using System.Threading.Tasks;

using Generator;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using Tests;

using Xunit;

namespace Test;

using GeneratorTest = CSharpSourceGeneratorTest<Adapter<AutoNotifyGenerator>, XUnitVerifier>;

public class AutoNotifyGeneratorTests
{
    [Fact]
    public async Task TestCodeFileIsGenerated()
    {
        const string codeFile = @"
using System;
using AutoNotify;

namespace GeneratedDemo
{
    // The view model we'd like to augment
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string _text = ""private field text"";

        [AutoNotify(PropertyName=""Count"")]
        private int _amount = 5;
    }

    public static class UseAutoNotifyGenerator
    {
        public static void Run()
        {
            ExampleViewModel vm = new ExampleViewModel();

            // we didn't explicitly create the 'Text' property, it was generated for us 
            string text = vm.Text;
            Console.WriteLine($""Text = {text}"");

            // Properties can have differnt names generated based on the PropertyName argument of the attribute
            int count = vm.Count;
            Console.WriteLine($""Count = {count}"");

            // the viewmodel will automatically implement INotifyPropertyChanged
            vm.PropertyChanged += (o, e) => Console.WriteLine($""Property {e.PropertyName} was changed"");
            vm.Text = ""abc"";
            vm.Count = 123;

            // Try adding fields to the ExampleViewModel class above and tagging them with the [AutoNotify] attribute
            // You'll see the matching generated properties visibile in IntelliSense in realtime
        }
    }
}
";
        const string generatedCode = @"namespace GeneratedDemo
{
    public partial class ExampleViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = """")
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        public virtual string Text
        {
            get => this._text;
            set
            {
                if (!System.Collections.Generic.EqualityComparer<string>.Default.Equals(this._text, value))
                {
                    this._text = value;
                    NotifyPropertyChanged(nameof(Text));
                }
            }
        }

        public virtual int Count
        {
            get => this._amount;
            set
            {
                if (!System.Collections.Generic.EqualityComparer<int>.Default.Equals(this._amount, value))
                {
                    this._amount = value;
                    NotifyPropertyChanged(nameof(Count));
                }
            }
        }
    }
}
";
        const string attributeText = @"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
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
                        (typeof(Adapter<AutoNotifyGenerator>), "AutoNotifyAttribute.cs", attributeText),
                        (typeof(Adapter<AutoNotifyGenerator>), "ExampleViewModel_autoNotify.cs", generatedCode),
                    },
                },
        }.RunAsync();
    }
}