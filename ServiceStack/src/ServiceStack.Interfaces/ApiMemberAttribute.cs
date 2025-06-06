﻿#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Document a short description for an API Property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ApiMemberAttribute : AttributeBase
{
    /// <summary>
    /// Gets or sets verb to which applies attribute. By default applies to all verbs.
    /// </summary>
    public string? Verb { get; set; }

    /// <summary>
    /// Gets or sets parameter type: It can be only one of the following: path, query, body, form, or header.
    /// </summary>
    public string? ParameterType { get; set; }

    /// <summary>
    /// Gets or sets unique name for the parameter. Each name must be unique, even if they are associated with different paramType values. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Other notes on the name field:
    /// If paramType is body, the name is used only for UI and code generation.
    /// If paramType is path, the name field must correspond to the associated path segment from the path field in the api object.
    /// If paramType is query, the name field corresponds to the query param name.
    /// </para>
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description for the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// For path, query, and header paramTypes, this field must be a primitive. For body, this can be a complex or container datatype.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Fine-tuned primitive type definition.  
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// For path, this is always true. Otherwise, this field tells the client whether or not the field must be supplied.
    /// </summary>
    public bool IsRequired { get; set; }
        
    /// <summary>
    /// Explicitly declare a property to be optional
    /// </summary>
    public bool IsOptional { get; set; } // workaround as bool? not allowed in attributes

    /// <summary>
    /// For query params, this specifies that a comma-separated list of values can be passed to the API. For path and body types, this field cannot be true.
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets route to which applies attribute, matches using StartsWith. By default applies to all routes. 
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Whether to exclude this property from being included in the ModelSchema
    /// </summary>
    public bool ExcludeInSchema { get; set; }
}