# JSon Wrapper Source Generator

## What
This source generator takes interfaces decorated with the `[JsonWrapper]` attribute and generates an extension method 
and wrapper class to allow accesing items in a strongly typed fashion directly out of the JsonElement without 
deserializing.

## Example

Given an interface like: 

``` C#
[JsonWrapper]
interface IPerson
{
    string FirstName { get; }
    string LastName { get; }
    int Age { get; }
```

This source generator would generate an `AsIPerson()` extension method on `System.Text.Json.JsonElement` and an implementation of `IPerson` that wraps the `JsonElement` and extracts each property on access.

## Why?

This explores an idea of reducing the conversions and DTO types necessary for implementing Json HTTP APIs.  Also, I haven't ever really looked at implementing a source generator, so I thought I'd give it a go.

## Future ideas

1. Handle unwrapping array properties
2. Handle `Nullable<T>` properties.
3. Handle a few more basic values `System.Text.Json.JsonElement` has accessors for (eg `Guid`, `DateTimeOffset`, `Base64Bytes`)
4. Support properties that are themselves interfaces with `[JsonWrapper]` so that you can have a hierarchy.
