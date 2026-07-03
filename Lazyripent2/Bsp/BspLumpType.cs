namespace Lazyripent2.Bsp;

public enum LumpType
{
	//vhlt-v34-ish limits
	[LumpMaxLength(0x40000)] Entities = 0, //256kb
	[LumpMaxLength(32768, 20)] Planes,
	[LumpMaxLength(0x400000)] Textures, //4mb
	[LumpMaxLength(65535, 12)] Vertices,
	[LumpMaxLength(0x800000)] Visibility, //8mb
	[LumpMaxLength(32767, 24)] Nodes,
	[LumpMaxLength(32767, 40)] TexInfo,
	[LumpMaxLength(65535, 20)] Faces,
	[LumpMaxLength(0x600000)] Lighting, //6mb
	[LumpMaxLength(32767, 8)] ClipNodes,
	[LumpMaxLength(8192, 28)] Leaves,
	[LumpMaxLength(65535, 2)] MarkSurfaces,
	[LumpMaxLength(256000, 4)] Edges,
	[LumpMaxLength(512000, 4)] SurfEdges,
	[LumpMaxLength(512, 64)] Models,
	
	[LumpMaxLength(0)] TotalLumpTypes,
}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class LumpMaxLengthAttribute(int maxLength, int structSize = 1) : System.Attribute
{
    public int MaxLength {get; private set;} = maxLength;
    public int StructSize {get; private set;} = structSize;
}

public static class LumpTypeExtensions
{
	public static int GetMaxByteLength(this LumpType value)
	{
		System.Reflection.FieldInfo? fieldInfo = value.GetType()?.GetField(value.ToString());
		if(fieldInfo is null)
		{
			return 0;
		}

		LumpMaxLengthAttribute[] attributes = (LumpMaxLengthAttribute[])fieldInfo.GetCustomAttributes(typeof(LumpMaxLengthAttribute), false);
		return attributes.Length > 0
			? attributes[0].MaxLength * attributes[0].StructSize
			: 0;
	}
}