﻿// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using Amazon.DynamoDBv2;

namespace ServiceStack.Aws.DynamoDb;

public interface IPocoDynamoFactory
{
    IPocoDynamo GetClient();
}

public class PocoDynamoFactory(Func<IAmazonDynamoDB> factory) : IPocoDynamoFactory
{
    public IPocoDynamo GetClient()
    {
        return new PocoDynamo(factory());
    }
}