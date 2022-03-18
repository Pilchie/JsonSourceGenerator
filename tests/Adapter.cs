
using Microsoft.CodeAnalysis;

namespace Tests;
public class Adapter<TIncrementalGenerator> : ISourceGenerator, IIncrementalGenerator
    where TIncrementalGenerator : IIncrementalGenerator, new()
{
    private readonly TIncrementalGenerator _internalGenerator;

    public Adapter()
    {
        _internalGenerator = new TIncrementalGenerator();
    }

    public void Execute(GeneratorExecutionContext context)
    {
        throw new System.NotImplementedException();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        throw new System.NotImplementedException();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        _internalGenerator.Initialize(context);
    }
}
