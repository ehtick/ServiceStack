﻿#nullable enable

using System;

namespace ServiceStack;

public static class GenerateBodyParameter
{
    /// <summary>
    /// Generates body DTO parameter only if `DisableAutoDtoInBodyParam = false`
    /// </summary>
    public const int IfNotDisabled = 0;
    /// <summary>
    /// Always generate body DTO for request
    /// </summary>
    public const int Always = 1;
    /// <summary>
    /// Never generate body DTO for request
    /// </summary>
    public const int Never = 2;
}

/// <summary>
/// Document a short description for an API Type
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ApiAttribute : AttributeBase
{
    /// <summary>
    /// The overall description of an API. Used by Swagger.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Create or not body param for request type when verb is POST or PUT.
    /// Value can be one of the constants of `GenerateBodyParam` class:
    /// `GenerateBodyParam.IfNotDisabled` (default value), `GenerateBodyParam.Always`, `GenerateBodyParam.Never`
    /// </summary>
    public int BodyParameter { get; set; }
    
    /// <summary>
    /// Preferred Content-Type, e.g: application/json, multipart/form-data, application/x-www-form-urlencoded
    /// or [MimeTypes.Json, MimeTypes.MultiPartFormData, MimeTypes.FormUrlEncoded]
    /// </summary>
    public string? RequestContentType { get; set; }

    /// <summary>
    /// Tells if body param is required
    /// </summary>
    public bool IsRequired { get; set; }

    public ApiAttribute() { }

    public ApiAttribute(string description) : this(description, GenerateBodyParameter.IfNotDisabled) { }

    public ApiAttribute(string description, int generateBodyParameter) : this(description, generateBodyParameter, false) {}

    public ApiAttribute(string description, int generateBodyParameter, bool isRequired)
    {
        Description = description;
        BodyParameter = generateBodyParameter;
        IsRequired = isRequired;
    }
}