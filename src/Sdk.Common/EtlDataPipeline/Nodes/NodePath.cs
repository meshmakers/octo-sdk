namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes;

/// <summary>
/// Represents a path to a node in the data pipeline.
/// </summary>
public readonly struct NodePath : IConvertible, IComparable<NodePath> 
{
    private readonly string _path;
    
    /// <summary>
    /// Creates a new instance of <see cref="NodePath"/>
    /// </summary>
    /// <param name="path">Path of node</param>
    public NodePath(string path)
    {
        _path = path;
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="NodePath"/>
    /// </summary>
    public NodePath()
    {
        _path = string.Empty;
    }
    
    /// <summary>
    ///     Creates a new <see cref="NodePath" /> from the given <paramref name="value" />.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator NodePath(string value)
    {
        return new NodePath(value);
    }
    
    /// <summary>
    ///    Implicitly converts the <see cref="NodePath" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="nodePath"></param>
    /// <returns></returns>
    public static implicit operator string(NodePath nodePath)
    {
        return nodePath._path;
    }

    /// <summary>
    /// Appends the node information to the path.
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the node</param>
    /// <param name="description">Description of the node</param>
    /// <returns>New path</returns>
    public NodePath Append(string qualifiedName, string? description)
    {
        var desc = "";
        // if (!string.IsNullOrWhiteSpace(description))
        // {
        //     desc = $"({description})";
        // }
        var delimiter = string.IsNullOrWhiteSpace(_path) ? "" : "/";
        
        var path = $"{_path}{delimiter}{qualifiedName}{desc}";
        return path;
    }
    
    #region IConvertible implementation
    
    /// <inheritdoc />
    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    /// <inheritdoc />
    public bool ToBoolean(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public byte ToByte(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public char ToChar(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public double ToDouble(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public short ToInt16(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public int ToInt32(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public long ToInt64(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public float ToSingle(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public string ToString(IFormatProvider? provider)
    {
        return _path;
    }

    /// <inheritdoc />
    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        switch (Type.GetTypeCode(conversionType))
        {
            case TypeCode.String:
                return ToString(provider);
            case TypeCode.Object:
                if (conversionType == typeof(object) || conversionType == typeof(NodePath))
                {
                    return this;
                }

                break;
        }

        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    /// <inheritdoc />
    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }
    
    #endregion IConvertible implementation
    

    /// <inheritdoc />
    public override string ToString()
    {
        return _path;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 19;
            hash = hash * 25 + _path.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public int CompareTo(NodePath other)
    {
        return string.Compare(_path, other._path, StringComparison.Ordinal);
    }
}