using System;

namespace AkiGames.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DontSerialize : Attribute {public DontSerialize(){}}
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HideInInspector : Attribute {public HideInInspector(){}}
}