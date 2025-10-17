shader_type canvas_item;

uniform float percentFull = 0;

void fragment() {
	if (COLOR.a == 1.0)
	{
		if (UV.y * UV.y + UV.x * UV.x > percentFull * 2.0 && COLOR.r != 1.0)
		{
			COLOR = vec4(0, 0, 0, 1);
		}
		else
		{
			COLOR = vec4(1, 1, 1, 1);
		}
	}
}
