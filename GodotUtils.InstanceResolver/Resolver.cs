using Godot;
using GodotUtils.InstanceResolver.Internal;
using System;

namespace GodotUtils.InstanceResolver;

public static class Resolver
{
    public static TNode Init<TNode>(this PackedScene packedScene)
        where TNode : Node, INoParamsResolveNode<TNode>
    {
        return packedScene.Instantiate<TNode>();
    }

    public static TNode Init<TNode>(
        this PackedScene packedScene,
        Func<TNode, IParameters<TNode>> requiredMappingFunction
    )
        where TNode : Node, IResolveNode<TNode>
    {
        var node = packedScene.Instantiate<TNode>();
        requiredMappingFunction.Invoke(node);
        return node;
    }
}