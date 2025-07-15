﻿using System.Reflection;

namespace Assistant.Api.Resources;

/// <summary>
/// Reads embedded resources from the assembly.
/// </summary>
public static class EmbeddedResource
{
    private static readonly string? _namespace = typeof(EmbeddedResource).Namespace;

    internal static string Read(string fileName)
    {
        // Get the current assembly. Note: this class is in the same assembly where the embedded resources are stored.
        Assembly assembly =
            typeof(EmbeddedResource).GetTypeInfo().Assembly ??
            throw new InvalidOperationException($"[{_namespace}] {fileName} assembly not found");

        // Resources are mapped like types, using the namespace and appending "." (dot) and the file name
        var resourceName = $"{_namespace}." + fileName;
        using Stream resource =
            assembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException($"{resourceName} resource not found");

        // Return the resource content, in text format.
        using var reader = new StreamReader(resource);

        return reader.ReadToEnd();
    }
}
