shader_type canvas_item;

uniform float healthPercent = 1;

void fragment() {
	if (UV.y > 1.0 - healthPercent && COLOR.a == 1.0)
	{
		COLOR = vec4(0, 0, 0, 1);
	}
	else
	{
		COLOR = vec4(0, 0, 0, 0);
	}
}