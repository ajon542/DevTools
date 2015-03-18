// Guids.cs
// MUST match guids.h
using System;

namespace ProtoBufGenerator
{
    static class GuidList
    {
        public const string guidProtoBufGeneratorPkgString = "172afbf5-191d-40c1-958a-d24506294202";
        public const string guidProtoBufGeneratorCmdSetString = "76093c8e-6908-4eaa-9e76-f86799bcb553";

        public static readonly Guid guidProtoBufGeneratorCmdSet = new Guid(guidProtoBufGeneratorCmdSetString);
    };
}